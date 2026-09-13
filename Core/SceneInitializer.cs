using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("Порядок активации корневых объектов (кроме критических)")]
    [SerializeField] private List<GameObject> sceneRoots;
    [Header("Критические объекты (активируются в самом конце)")]
    [SerializeField] private List<GameObject> criticalObjects;
    [SerializeField] private ShaderVariantCollection prebakedShaders;

    private GPUInstanceManager _gpuInstancer;
    //private ObjectStreamer _objectStreamer;

    public IEnumerator InitializeWorldRoutine(Action<float> onProgressChanged)
    {
        _gpuInstancer = FindAnyObjectByType<GPUInstanceManager>();
        onProgressChanged?.Invoke(0f);
        yield return null;

        // --- ЭТАП 1: Активация корневых объектов (0% -> 30%) ---
        if (sceneRoots != null && sceneRoots.Count > 0)
        {   for (int i = 0; i < sceneRoots.Count; i++)
            {   if (sceneRoots[i] != null) sceneRoots[i].SetActive(true);
                onProgressChanged?.Invoke(((float)(i + 1) / sceneRoots.Count) * 0.3f);
                yield return null; }}
        onProgressChanged?.Invoke(0.3f);

        // --- ЭТАП 2: GPU Instancer (30% -> 85%) ---
        if (_gpuInstancer != null)
        {   yield return StartCoroutine(_gpuInstancer.LoadFromDataRoutine(progress => 
            onProgressChanged?.Invoke(0.3f + (progress * 0.55f)))); }
        onProgressChanged?.Invoke(0.85f);
        yield return null;

        // --- ЭТАП 3: Критические менеджеры (85% -> 90%) ---
        if (criticalObjects != null && criticalObjects.Count > 0)
        {
            for (int i = 0; i < criticalObjects.Count; i++)
            {
                if (criticalObjects[i] != null) criticalObjects[i].SetActive(true);
                float step = (float)(i + 1) / criticalObjects.Count;
                onProgressChanged?.Invoke(0.85f + (step * 0.05f));
                yield return null;
            }
        }
        onProgressChanged?.Invoke(0.9f);
        yield return null;

        if (prebakedShaders != null && !prebakedShaders.isWarmedUp) prebakedShaders.WarmUp();
        onProgressChanged?.Invoke(0.95f);
        yield return null;

        onProgressChanged?.Invoke(1f);
        yield return null;
    }
}
