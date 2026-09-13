using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class ScanInstanceMarkersTool
{
    [MenuItem("Tools/GPUInstancer/Scan All Markers")]
    static void ScanAll()
    {
        InstanceMarker[] markers = Object.FindObjectsByType<InstanceMarker>(FindObjectsInactive.Include);
        if (markers.Length == 0)
        {
            Debug.LogWarning("[GPUInstances] В сцене нет объектов с InstanceMarker");
            return;
        }

        Dictionary<(Mesh, Material), int> groups = new Dictionary<(Mesh, Material), int>();
        int total = 0;

        Debug.Log($"[GPUInstances] Найдено объектов с InstanceMarker: {markers.Length}");

        foreach (InstanceMarker m in markers)
        {
            MeshFilter mf = m.GetComponent<MeshFilter>();
            MeshRenderer mr = m.GetComponent<MeshRenderer>();
            if (mf == null || mr == null)
            {
                Debug.LogWarning($"[GPUInstances] '{m.name}' — нет MeshFilter/MeshRenderer, пропущен");
                continue;
            }

            Mesh mesh = mf.sharedMesh;
            Material mat = mr.sharedMaterial;
            if (mesh == null || mat == null)
            {
                Debug.LogWarning($"[GPUInstances] '{m.name}' — нет меша или материала, пропущен");
                continue;
            }

            var key = (mesh, mat);
            groups.TryGetValue(key, out int count);
            groups[key] = count + 1;
            total++;
        }

        Debug.Log($"[GPUInstances] Групп (mesh+material): {groups.Count}, всего инстансов: {total}");
        foreach (var pair in groups)
            Debug.Log($"  - '{pair.Key.Item1.name}' + '{pair.Key.Item2.name}' x {pair.Value}");
    }

    [MenuItem("Tools/GPUInstancer/Scan & Save Markers")]
    static void ScanAndSave()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Сохранить данные маркеров",
            "InstanceMarkerData",
            "asset",
            "Выберите папку для сохранения");

        if (string.IsNullOrEmpty(path)) return;

        InstanceMarker[] markers = Object.FindObjectsByType<InstanceMarker>(FindObjectsInactive.Include);
        if (markers.Length == 0)
        {
            Debug.LogWarning("[GPUInstances] В сцене нет объектов с InstanceMarker");
            return;
        }

        InstanceMarkerData data = ScriptableObject.CreateInstance<InstanceMarkerData>();

        foreach (InstanceMarker m in markers)
        {
            MeshFilter mf = m.GetComponent<MeshFilter>();
            MeshRenderer mr = m.GetComponent<MeshRenderer>();
            if (mf == null || mr == null) continue;

            Mesh mesh = mf.sharedMesh;
            Material mat = mr.sharedMaterial;
            if (mesh == null || mat == null) continue;

            data.entries.Add(new InstanceMarkerData.MarkerEntry
            {
                gameObjectName = m.gameObject.name,
                mesh = mesh,
                material = mat,
                transform = m.transform.localToWorldMatrix,
                minDistance = m.minDistance,
                maxDistance = m.maxDistance,
                receiveShadows = m.receiveShadows
            });
        }

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[GPUInstances] Сохранено {data.entries.Count} маркеров в {path}");
    }
}
