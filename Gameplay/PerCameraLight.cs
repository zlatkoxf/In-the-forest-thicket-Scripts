using UnityEngine;
using UnityEngine.Rendering;

public class PerCameraLight : MonoBehaviour
{
    [Header("Камера, которая НЕ должна видеть этот свет")]
    [SerializeField] private Camera hiddenFromCamera; 

    private Light targetLight;

    private void OnEnable()
    {
        targetLight = GetComponent<Light>();
        // Подписываемся на события рендеринга URP
        RenderPipelineManager.beginCameraRendering += OnBeginRender;
        RenderPipelineManager.endCameraRendering += OnEndRender;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginRender;
        RenderPipelineManager.endCameraRendering -= OnEndRender;
    }

    private void OnBeginRender(ScriptableRenderContext context, Camera camera)
    {
        // Если сейчас рендерит Камера 1, полностью выключаем источник света
        if (camera == hiddenFromCamera)
        {
            targetLight.enabled = false;
        }
    }

    private void OnEndRender(ScriptableRenderContext context, Camera camera)
    {
        // Как только Камера 1 закончила, включаем свет обратно для остальных камер
        if (camera == hiddenFromCamera)
        {
            targetLight.enabled = true;
        }
    }
}
