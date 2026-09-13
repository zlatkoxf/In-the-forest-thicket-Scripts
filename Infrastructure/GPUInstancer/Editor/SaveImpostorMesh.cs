using UnityEngine;
using UnityEditor;

public static class SaveImpostorMeshTool
{
    [MenuItem("Tools/GPUInstancer/Save Selected Impostor Mesh")]
    static void SaveSelected()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogError("[GPUInstances] Выберите объект импостора в сцене");
            return;
        }

        MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("[GPUInstances] У выбранного объекта нет MeshFilter с мешем");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Сохранить меш импостора",
            go.name + "_Mesh",
            "asset",
            "Выберите папку для сохранения меша");
        if (string.IsNullOrEmpty(path)) return;

        Mesh copy = Object.Instantiate(mf.sharedMesh);
        copy.name = go.name + "_ImpostorMesh";
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();

        mf.sharedMesh = copy;
        Debug.Log($"[GPUInstances] Меш сохранён: {path} (вершин: {copy.vertexCount}, треугольников: {copy.triangles.Length / 3}). Теперь объект можно добавлять в префаб — ссылка не порвётся.");
    }
}
