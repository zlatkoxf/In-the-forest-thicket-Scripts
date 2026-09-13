using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectStreamer : MonoBehaviour
{
    public SpawnDataContainer dataContainer;
    public Transform playerTransform;
    public float activationRadius = 60f;
    public int operationsPerFrame = 150;

    private Dictionary<int, ObjectPool<GameObject>> pools = new Dictionary<int, ObjectPool<GameObject>>();
    private Dictionary<int, GameObject> activeColliders = new Dictionary<int, GameObject>();
    private Dictionary<int, BakedObjectData> dataById = new Dictionary<int, BakedObjectData>();

    private float sqrRadius;
    private int[] dataIndices;
    private bool running;

    public void InitAndStartStreaming()
    {
        sqrRadius = activationRadius * activationRadius;

        InitPools();
        InitDataIndex();
        running = true;
        StartCoroutine(StreamingRoutine());
    }

    void InitPools()
    {
        pools.Clear();

        foreach (var prefab in dataContainer.colliderPrefabs)
        {
            if (prefab == null) continue;

            int typeID = GetTypeID(prefab);
            if (pools.ContainsKey(typeID)) continue;

            pools.Add(typeID, new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 1024,
                maxSize: 60000 // достаточно для одновременных объектов в радиусе
            ));
        }
    }

    int GetTypeID(GameObject prefab)
    {
        var marker = prefab.GetComponentInChildren<SpawnMarker>();
        if (marker != null) return marker.prefabTypeID;

        // Если на префабе нет маркера, используем его индекс в списке как ID
        return dataContainer.colliderPrefabs.IndexOf(prefab);
    }

    void InitDataIndex()
    {
        dataById.Clear();
        dataIndices = new int[dataContainer.allBakedObjects.Count];

        for (int i = 0; i < dataContainer.allBakedObjects.Count; i++)
        {
            dataById[i] = dataContainer.allBakedObjects[i];
            dataIndices[i] = i;
        }
    }

    IEnumerator StreamingRoutine()
    {
        int frameCounter = 0;
        int total = dataIndices.Length;
        int cursor = 0;

        while (running)
        {
            if (playerTransform == null) yield return null;
            Vector3 playerPos = playerTransform.position;

            // Проходим с точки последней остановки, чтобы распределить работу по кадрам
            for (int step = 0; step < total; step++)
            {
                int i = dataIndices[cursor];
                cursor = (cursor + 1) % total;

                BakedObjectData data = dataById[i];
                float sqrDistance = (data.position - playerPos).sqrMagnitude;

                bool inRange = sqrDistance <= sqrRadius;
                bool isSpawned = activeColliders.ContainsKey(i);

                if (inRange && !isSpawned)
                {
                    if (pools.TryGetValue(data.prefabTypeID, out var pool))
                    {
                        GameObject spawned = pool.Get();
                        spawned.transform.position = data.position;
                        spawned.transform.rotation = data.rotation;
                        spawned.transform.localScale = data.scale;
                        activeColliders.Add(i, spawned);
                        frameCounter++;
                    }
                }
                else if (!inRange && isSpawned)
                {
                    GameObject objToRelease = activeColliders[i];
                    if (pools.TryGetValue(data.prefabTypeID, out var pool))
                        pool.Release(objToRelease);

                    activeColliders.Remove(i);
                    frameCounter++;
                }

                if (frameCounter >= operationsPerFrame)
                {
                    frameCounter = 0;
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    void OnDisable()
    {
        running = false;
        StopAllCoroutines();

        // Возвращаем все активные объекты в пулы
        foreach (var kvp in activeColliders)
        {
            var obj = kvp.Value;
            if (obj == null) continue;

            if (dataById.TryGetValue(kvp.Key, out var data) &&
                pools.TryGetValue(data.prefabTypeID, out var pool))
            {
                pool.Release(obj);
            }
        }
        activeColliders.Clear();
    }
}
