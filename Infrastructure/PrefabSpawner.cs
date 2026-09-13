using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabsList = new List<GameObject>();

    /// <summary>
    /// Метод для спавна префаба по его индексу в списке
    /// </summary>
    /// <param name="index">Индекс объекта в списке prefabsList</param>
    public GameObject SpawnPrefabByIndex(int index, Vector3 pos)
    {
        // 1. Проверяем, что индекс корректен и не выходит за границы списка
        if (index < 0 || index >= prefabsList.Count) { Debug.LogError($"[Spawner] Индекс {index} вне диапазона! В списке всего {prefabsList.Count} префабов."); return null; } 
        // 2. Проверяем, добавлен ли сам префаб в эту ячейку списка
        if (prefabsList[index] == null) { Debug.LogError($"[Spawner] Префаб по индексу {index} равен null (не назначен в инспекторе)!"); return null; }
        GameObject spawnedObject = Instantiate(prefabsList[index], pos, prefabsList[index].transform.rotation);
        Debug.Log($"[PrefabSpawner] Успешно заспавнен: {spawnedObject.name}");
        return spawnedObject;
    }
}
