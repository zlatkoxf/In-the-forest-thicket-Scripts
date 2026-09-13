#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectBakerWindow : EditorWindow
{
    [MenuItem("Tools/Bake Objects by Marker")]
    public static void BakeObjects()
    {
        var container = SpawnDataContainer.FindOrCreate();
        if (container == null)
        {
            Debug.LogWarning("[ObjectBaker] Прервано: контейнер не создан.");
            return;
        }

        container.allBakedObjects.Clear();

        SpawnMarker[] markers = Object.FindObjectsByType<SpawnMarker>(FindObjectsInactive.Include);

        Debug.Log($"[ObjectBaker] Найдено маркеров: {markers.Length}");

        // Собираем префабы из маркеров (по их prefabTypeID), чтобы не заполнять вручную
        var prefabsById = new Dictionary<int, GameObject>();
        foreach (var prefab in container.colliderPrefabs)
        {
            if (prefab == null) continue;
            var marker = prefab.GetComponentInChildren<SpawnMarker>();
            if (marker != null && !prefabsById.ContainsKey(marker.prefabTypeID))
                prefabsById[marker.prefabTypeID] = prefab;
        }

        foreach (var marker in markers)
        {
            // Автоматически подхватываем исходный префаб маркера
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(marker.gameObject);
            if (sourcePrefab != null && !prefabsById.ContainsKey(marker.prefabTypeID))
            {
                prefabsById[marker.prefabTypeID] = sourcePrefab;
            }

            container.allBakedObjects.Add(new BakedObjectData
            {
                prefabTypeID = marker.prefabTypeID,
                position = marker.transform.position,
                rotation = marker.transform.rotation,
                scale = marker.transform.localScale
            });
        }

        // Записываем упорядоченный список префабов по возрастанию ID
        container.colliderPrefabs = new List<GameObject>();
        var sortedIds = new List<int>(prefabsById.Keys);
        sortedIds.Sort();
        foreach (var id in sortedIds)
            container.colliderPrefabs.Add(prefabsById[id]);

        EditorUtility.SetDirty(container);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ObjectBaker] Запечено {container.allBakedObjects.Count} объектов и {container.colliderPrefabs.Count} префабов");
    }
}
#endif
