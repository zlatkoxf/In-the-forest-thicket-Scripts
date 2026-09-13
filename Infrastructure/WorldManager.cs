using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;
    void Start()
    {
        Instance = this;
        StartCoroutine(LoadVegetationAsync());
    }

    IEnumerator LoadVegetationAsync()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low; 
        AsyncOperation op = SceneManager.LoadSceneAsync("MainScene_Vegetation", LoadSceneMode.Additive);
        //op.allowSceneActivation = false;
        while (!op.isDone)
        {
            Debug.Log($"Загрузка: {op.progress * 100}%");
            yield return null;
        }
        //op.allowSceneActivation = true; 
        Application.backgroundLoadingPriority = ThreadPriority.High; 
    }
}
