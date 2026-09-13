using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BakedObjectData
{
    public int prefabTypeID;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

[CreateAssetMenu(fileName = "SpawnDataContainer", menuName = "Optimizations/Baked Objects Data")]
public class SpawnDataContainer : ScriptableObject
{
    public const string DEFAULT_PATH = "Assets/Scripts/Infrastructure/ObjectStreamer/SpawnDataContainer.asset";

    public List<GameObject> colliderPrefabs;

    [HideInInspector]
    public List<BakedObjectData> allBakedObjects = new List<BakedObjectData>();

#if UNITY_EDITOR
    public static SpawnDataContainer FindOrCreate()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SpawnDataContainer");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<SpawnDataContainer>(path);
        }

        string defaultName = System.IO.Path.GetFileNameWithoutExtension(DEFAULT_PATH);
        string folder = "Assets";
        string savePath = UnityEditor.EditorUtility.SaveFilePanel(
            "Save Baked Data Container", folder, defaultName, "asset");

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.Log("[ObjectBaker] Отменено создание контейнера.");
            return null;
        }

        string dataPath = System.IO.Path.GetFullPath(Application.dataPath).TrimEnd('\\', '/');
        string relative = System.IO.Path.GetFullPath(savePath);
        string assetPath = "Assets" + relative.Substring(dataPath.Length).Replace('\\', '/');

        if (!assetPath.StartsWith("Assets/") || !assetPath.EndsWith(".asset"))
        {
            Debug.LogError("[ObjectBaker] Путь должен лежать в папке Assets проекта.");
            return null;
        }

        var container = CreateInstance<SpawnDataContainer>();
        container.colliderPrefabs = new List<GameObject>();

        UnityEditor.AssetDatabase.CreateAsset(container, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[ObjectBaker] Создан новый SpawnDataContainer: {assetPath}");
        return container;
    }
#endif
}
