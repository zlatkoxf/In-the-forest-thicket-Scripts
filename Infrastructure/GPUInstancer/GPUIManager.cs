using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;
using System;

/// CPU-инстансный рендерер с фрустум/дистанс куллингом.
/// Архитектура рассчитана на будущий переход на GPU-куллинг (HZB):
/// - все матрицы лежат в одной плоской таблице (кандидат в ComputeBuffer);
/// - один проход куллинга над всей таблицей (будущий compute dispatch);
/// - компактные списки видимых индексов на группу (будущий indirect args).
public class GPUInstanceManager : MonoBehaviour
{
    private static GPUInstanceManager _instance;
    public static GPUInstanceManager Instance => _instance;

    private const int MaxInstancesPerBatch = 1023;

    [Header("Culling")]
    public float maxRenderDistance = 500f;
    [Tooltip("Если пусто, используется Camera.main")]
    public Camera cullingCameraOverride;

    [Header("Data")]
    [Tooltip("ScriptableObject с данными сканирования маркеров. Если задан, менеджер загрузит инстансы отсюда вместо ручной регистрации")]
    public InstanceMarkerData markerData;

    [Header("Shadows")]
    [Tooltip("Дальше этого расстояния инстансы рисуются без отбрасывания теней. Согласуйте с Shadow Distance в URP-ассете")]
    public float shadowCastMaxDistance = 80f;

    [Header("Lighting")]
    [Tooltip("Слой маски света — какие источники света будут освещать инстансы")]
    public LayerMask lightLayerMask = -1;

    private class RenderGroup
    {
        public Mesh mesh;
        public Material material;
        public float minDistance;
        public float maxDistance = -1f;
        public bool receiveShadows = true;
        public float baseRadius = 0.5f;
        public bool radiusInitialized;

        public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
        public NativeList<int> visibleNear;
        public NativeList<int> visibleFar;

        public int tableOffset;
        public int tableCount;
    }

    private struct GroupCullParams
    {
        public float MinDistance;
        public float MaxDistance;
        public float Radius;
    }

    private struct DistanceOverride
    {
        public float min;
        public float max;
        public bool? receiveShadows;
    }

    private readonly Dictionary<(Mesh, Material), RenderGroup> groups = new Dictionary<(Mesh, Material), RenderGroup>();
    private readonly List<RenderGroup> groupList = new List<RenderGroup>();
    private readonly Dictionary<(Mesh, Material), DistanceOverride> pendingOverrides = new Dictionary<(Mesh, Material), DistanceOverride>();

    private readonly Matrix4x4[] batchScratch = new Matrix4x4[MaxInstancesPerBatch];
    private readonly Plane[] unityFrustumPlanes = new Plane[6];
    private NativeArray<float4> frustumPlaneData;
    private bool statsLogged;

    // Глобальная плоская таблица инстансов — будущий ComputeBuffer для GPU-куллинга
    private NativeArray<Matrix4x4> instanceTable;
    private NativeArray<int> instanceGroup;
    private NativeArray<GroupCullParams> groupParams;
    private NativeList<int> visibleNearGlobal;
    private NativeList<int> visibleFarGlobal;
    private bool tableDirty = true;

    void Awake()
    {
        _instance = this;
        frustumPlaneData = new NativeArray<float4>(6, Allocator.Persistent);
        visibleNearGlobal = new NativeList<int>(4096, Allocator.Persistent);
        visibleFarGlobal = new NativeList<int>(4096, Allocator.Persistent);

        //if (markerData != null)
        //    LoadFromData(markerData);
    }

    public void LoadFromData()
    {
        StartCoroutine(LoadFromDataRoutine(null));
        // foreach (var entry in data.entries)
        // {
        //     if (entry.mesh == null || entry.material == null) continue;
        //     Register(entry.mesh, entry.material, entry.transform);
        //     if (entry.minDistance > 0f)
        //         SetGroupMinDistance(entry.mesh, entry.material, entry.minDistance);
        //     if (entry.maxDistance > 0f)
        //         SetGroupMaxDistance(entry.mesh, entry.material, entry.maxDistance);
        //     if (!entry.receiveShadows)
        //         SetGroupReceiveShadows(entry.mesh, entry.material, false);
        // }
        // Debug.Log($"[GPUInstances] Загружено {data.entries.Count} инстансов из '{data.name}'");
    }

    public IEnumerator LoadFromDataRoutine(Action<float> onProgressChanged)
    {
        statsLogged = true;
        InstanceMarkerData data = markerData;

        if (data == null || data.entries == null || data.entries.Count == 0)
        { onProgressChanged?.Invoke(1f); yield break; }

        int totalEntries = data.entries.Count;
        int batchSize = 3000; 

        for (int i = 0; i < totalEntries; i++)
        {
            var entry = data.entries[i];
            
            if (entry.mesh != null && entry.material != null)
            {
                Register(entry.mesh, entry.material, entry.transform);
                
                if (entry.minDistance > 0f)
                    SetGroupMinDistance(entry.mesh, entry.material, entry.minDistance);
                if (entry.maxDistance > 0f)
                    SetGroupMaxDistance(entry.mesh, entry.material, entry.maxDistance);
                if (!entry.receiveShadows)
                    SetGroupReceiveShadows(entry.mesh, entry.material, false);
            }

            if (i > 0 && i % batchSize == 0)
            {
                float currentProgress = (float)i / totalEntries;
                onProgressChanged?.Invoke(currentProgress);
                yield return null; 
            }
        }

        onProgressChanged?.Invoke(1f);
        Debug.Log($"[GPUInstances] Плавная загрузка завершена. {totalEntries} инстансов из '{data.name}'");
        statsLogged = false;
    }

    public void Register(Mesh mesh, Material material, Matrix4x4 matrix)
    {
        RenderGroup group = GetOrCreateGroup(mesh, material);
        group.matrices.Add(matrix);
        tableDirty = true;
    }

    public void SetGroupMaxDistance(Mesh mesh, Material material, float maxDistance)
    {
        var key = (mesh, material);
        if (groups.TryGetValue(key, out RenderGroup group))
            group.maxDistance = maxDistance;
        else
            AddPendingOverride(key, float.NaN, maxDistance);
    }

    public void SetGroupMinDistance(Mesh mesh, Material material, float minDistance)
    {
        var key = (mesh, material);
        if (groups.TryGetValue(key, out RenderGroup group))
            group.minDistance = minDistance;
        else
            AddPendingOverride(key, minDistance, float.NaN);
    }

    public void SetGroupReceiveShadows(Mesh mesh, Material material, bool receiveShadows)
    {
        var key = (mesh, material);
        if (groups.TryGetValue(key, out RenderGroup group))
            group.receiveShadows = receiveShadows;
        else
            AddPendingOverride(key, float.NaN, float.NaN, receiveShadows);
    }

    private void AddPendingOverride((Mesh, Material) key, float min, float max, bool? receiveShadows = null)
    {
        pendingOverrides.TryGetValue(key, out DistanceOverride o);
        if (!float.IsNaN(min)) o.min = min;
        if (!float.IsNaN(max)) o.max = max;
        if (receiveShadows.HasValue) o.receiveShadows = receiveShadows;
        pendingOverrides[key] = o;
    }

    private RenderGroup GetOrCreateGroup(Mesh mesh, Material material)
    {
        var key = (mesh, material);
        if (groups.TryGetValue(key, out RenderGroup group)) return group;

        group = new RenderGroup
        {
            mesh = mesh,
            material = material,
            visibleNear = new NativeList<int>(1024, Allocator.Persistent),
            visibleFar = new NativeList<int>(1024, Allocator.Persistent)
        };
        if (pendingOverrides.TryGetValue(key, out DistanceOverride pending))
        {
            if (!float.IsNaN(pending.min)) group.minDistance = pending.min;
            if (!float.IsNaN(pending.max)) group.maxDistance = pending.max;
            if (pending.receiveShadows.HasValue) group.receiveShadows = pending.receiveShadows.Value;
            pendingOverrides.Remove(key);
        }
        groups[key] = group;
        groupList.Add(group);
        return group;
    }

    void Update()
    {
        if (!statsLogged)
        {
            statsLogged = true;
            int total = 0;
            foreach (var pair in groups)
            {
                total += pair.Value.matrices.Count;
                Debug.Log($"[GPUInstances] '{pair.Key.Item1.name}' x {pair.Value.matrices.Count}");
            }
            Debug.Log($"[GPUInstances] Зарегистрировано инстансов: {total}, групп: {groups.Count}");
        }

        Camera cam = cullingCameraOverride != null ? cullingCameraOverride : Camera.main;
        if (cam == null) return;

        GeometryUtility.CalculateFrustumPlanes(cam, unityFrustumPlanes);
        for (int i = 0; i < 6; i++)
            frustumPlaneData[i] = new float4(unityFrustumPlanes[i].normal, unityFrustumPlanes[i].distance);

        if (tableDirty) RebuildInstanceTable();
        RefreshGroupParams();

        visibleNearGlobal.Clear();
        visibleFarGlobal.Clear();
        new CullJob
        {
            Matrices = instanceTable,
            InstanceGroup = instanceGroup,
            Params = groupParams,
            Planes = frustumPlaneData,
            CamPos = cam.transform.position,
            ShadowMaxDistance = shadowCastMaxDistance,
            Near = visibleNearGlobal,
            Far = visibleFarGlobal
        }.Schedule().Complete();

        for (int gi = 0; gi < groupList.Count; gi++)
        {
            groupList[gi].visibleNear.Clear();
            groupList[gi].visibleFar.Clear();
        }
        Distribute(visibleNearGlobal, true);
        Distribute(visibleFarGlobal, false);

        foreach (RenderGroup g in groupList)
        {
            if (g.tableCount == 0) continue;
            RenderBand(g, g.visibleNear, true);
            RenderBand(g, g.visibleFar, false);
        }
    }

    private void RebuildInstanceTable()
    {
        tableDirty = false;

        int total = 0;
        for (int i = 0; i < groupList.Count; i++) total += groupList[i].matrices.Count;

        if (!instanceTable.IsCreated || instanceTable.Length != total)
        {
            DisposeTable();
            instanceTable = new NativeArray<Matrix4x4>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            instanceGroup = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        int idx = 0;
        for (int gi = 0; gi < groupList.Count; gi++)
        {
            RenderGroup g = groupList[gi];
            EnsureRadius(g);
            g.tableOffset = idx;
            g.tableCount = g.matrices.Count;
            for (int i = 0; i < g.matrices.Count; i++)
            {
                instanceTable[idx] = g.matrices[i];
                instanceGroup[idx] = gi;
                idx++;
            }
        }

        if (groupParams.IsCreated && groupParams.Length != groupList.Count)
        {
            groupParams.Dispose();
            groupParams = default;
        }
        if (!groupParams.IsCreated)
            groupParams = new NativeArray<GroupCullParams>(groupList.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    private void RefreshGroupParams()
    {
        for (int gi = 0; gi < groupList.Count && gi < groupParams.Length; gi++)
        {
            RenderGroup g = groupList[gi];
            groupParams[gi] = new GroupCullParams
            {
                MinDistance = g.minDistance,
                MaxDistance = g.maxDistance > 0f ? g.maxDistance : maxRenderDistance,
                Radius = g.baseRadius
            };
        }
    }

    private void EnsureRadius(RenderGroup g)
    {
        if (g.radiusInitialized) return;
        g.radiusInitialized = true;
        if (g.mesh != null && g.mesh.bounds.extents.sqrMagnitude > 0.000001f)
            g.baseRadius = g.mesh.bounds.extents.magnitude;
    }

    private void Distribute(NativeList<int> globalIndices, bool nearBand)
    {
        for (int k = 0; k < globalIndices.Length; k++)
        {
            int globalIdx = globalIndices[k];
            RenderGroup g = groupList[instanceGroup[globalIdx]];
            if (nearBand) g.visibleNear.Add(globalIdx - g.tableOffset);
            else g.visibleFar.Add(globalIdx - g.tableOffset);
        }
    }

    private void RenderBand(RenderGroup group, NativeList<int> indices, bool castShadows)
    {
        int count = indices.Length;
        if (count == 0) return;

        RenderParams rp = new RenderParams(group.material)
        {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 5000f),
            shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off,
            receiveShadows = group.receiveShadows,
            renderingLayerMask = (uint)(int)lightLayerMask
        };

        for (int start = 0; start < count; start += MaxInstancesPerBatch)
        {
            int batchCount = Mathf.Min(MaxInstancesPerBatch, count - start);
            for (int k = 0; k < batchCount; k++)
                batchScratch[k] = group.matrices[indices[start + k]];
            Graphics.RenderMeshInstanced(rp, group.mesh, 0, batchScratch, batchCount);
        }
    }

    [BurstCompile]
    private struct CullJob : IJob
    {
        [ReadOnly] public NativeArray<Matrix4x4> Matrices;
        [ReadOnly] public NativeArray<int> InstanceGroup;
        [ReadOnly] public NativeArray<GroupCullParams> Params;
        [ReadOnly] public NativeArray<float4> Planes;
        public float3 CamPos;
        public float ShadowMaxDistance;
        public NativeList<int> Near;
        public NativeList<int> Far;

        public void Execute()
        {
            float shadowSq = ShadowMaxDistance * ShadowMaxDistance;

            for (int i = 0; i < Matrices.Length; i++)
            {
                Matrix4x4 m = Matrices[i];
                GroupCullParams p = Params[InstanceGroup[i]];

                float px = m.m03, py = m.m13, pz = m.m23;

                float sx = math.abs(m.m00) + math.abs(m.m01) + math.abs(m.m02);
                float sy = math.abs(m.m10) + math.abs(m.m11) + math.abs(m.m12);
                float sz = math.abs(m.m20) + math.abs(m.m21) + math.abs(m.m22);
                float radius = p.Radius * math.max(sx, math.max(sy, sz));

                float dx = px - CamPos.x, dy = py - CamPos.y, dz = pz - CamPos.z;
                float distSq = dx * dx + dy * dy + dz * dz;
                if (distSq > p.MaxDistance * p.MaxDistance) continue;
                if (distSq < p.MinDistance * p.MinDistance) continue;

                bool inside = true;
                for (int j = 0; j < 6; j++)
                {
                    float4 plane = Planes[j];
                    if (plane.x * px + plane.y * py + plane.z * pz + plane.w < -radius)
                    {
                        inside = false;
                        break;
                    }
                }
                if (!inside) continue;

                if (ShadowMaxDistance <= 0f || distSq <= shadowSq) Near.Add(i);
                else Far.Add(i);
            }
        }
    }

    private void DisposeTable()
    {
        if (instanceTable.IsCreated) instanceTable.Dispose();
        if (instanceGroup.IsCreated) instanceGroup.Dispose();
    }

    void OnDestroy()
    {
        foreach (RenderGroup g in groupList)
        {
            if (g.visibleNear.IsCreated) g.visibleNear.Dispose();
            if (g.visibleFar.IsCreated) g.visibleFar.Dispose();
        }
        groupList.Clear();
        groups.Clear();
        pendingOverrides.Clear();

        DisposeTable();
        if (groupParams.IsCreated) groupParams.Dispose();
        if (visibleNearGlobal.IsCreated) visibleNearGlobal.Dispose();
        if (visibleFarGlobal.IsCreated) visibleFarGlobal.Dispose();
        if (frustumPlaneData.IsCreated) frustumPlaneData.Dispose();
    }
}
