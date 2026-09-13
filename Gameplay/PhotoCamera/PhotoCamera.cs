using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;

public class PhotoCamera : MonoBehaviour
{
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private Camera photoCamera;
    [SerializeField] private RenderTexture manualRenderTexture;
    [SerializeField] private Material screenEffectMaterial;
    [SerializeField] private Light flashLight;
    [Space]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float sphereRadius = 0.1f;
    [SerializeField] private float castBackDistance = 0.5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0.2f, -0.1f, 0.31f);
    [SerializeField] private float inspectCameraOffset = 0.15f;
    [Space]
    [SerializeField] private float photoCooldown = 1.0f; // Задержка в секундах

    private float nextPhotoTime = 0.0f; // Время, когда можно будет сделать следующее фото
    private bool _isInspectingPhoto = false;
    public bool IsInspectingPhoto { get => _isInspectingPhoto; }

    [Header("Игровая галерея (БЕЗ ЭФФЕКТОВ)")] public List<byte[]> inGamePhotosData = new List<byte[]>();

    void Start()
    {
        transform.parent = null;
    }

    public void TakePhoto()
    {
        if (Time.time >= nextPhotoTime)
        {
            AudioManager.Instance.PlaySound("TakePhoto", pos: transform.position, volume: 1f, spatialBlend: 1f, group: AudioManager.Instance.sfxGroup);
            StartCoroutine(TakePhotoWithFlashRoutine());
            nextPhotoTime = Time.time + photoCooldown;
        }
    }

    private IEnumerator TakePhotoWithFlashRoutine()
    {
        yield return new WaitForSeconds(0.1f);

        flashLight.enabled = true;
        yield return null;
        //yield return new WaitForEndOfFrame();

        photoCamera.enabled = true;
        photoCamera.Render();
        photoCamera.enabled = false;

        AsyncGPUReadback.Request(manualRenderTexture, 0, TextureFormat.RGB24, OnCompleteInGameReadback);

        RenderTexture processedRT = RenderTexture.GetTemporary(manualRenderTexture.width, manualRenderTexture.height, 0, manualRenderTexture.format);
        Graphics.Blit(manualRenderTexture, processedRT, screenEffectMaterial);

        AsyncGPUReadback.Request(processedRT, 0, TextureFormat.RGB24, (request) => 
        {
            PhotoSaveUtility.SaveAsyncRequestToDisk(request, manualRenderTexture.width, manualRenderTexture.height);
            RenderTexture.ReleaseTemporary(processedRT);
        });

        yield return new WaitForSeconds(0.05f);
        flashLight.enabled = false;
    }

    void OnCompleteInGameReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError) return;

        int width = manualRenderTexture.width;
        int height = manualRenderTexture.height;

        System.Threading.Tasks.Task.Run(() => // Сразу запуск сохранения в фоновом потоке, чтобы разгрузить RAM основного потока
        {
            var rawData = request.GetData<byte>().ToArray();
            byte[] cleanPngBytes = ImageConversion.EncodeArrayToPNG(rawData, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8_SRGB, (uint)width, (uint)height);
            lock (inGamePhotosData) inGamePhotosData.Add(cleanPngBytes);
            Debug.Log($"[Галерея игры] Чистое фото пожато и сохранено асинхронно в фоне!");
        });
    }
    public void InspectingSwitch(bool? @bool = null) => _isInspectingPhoto = @bool ?? !_isInspectingPhoto;
    public void UpdateRotation(Vector3 target) 
    {
        if (_isInspectingPhoto) transform.rotation = playerCamera.GetCameraTransform().rotation;
        else transform.eulerAngles = target;
    }

    public void UpdatePosition(Transform target)
    {
        Transform camTransform = playerCamera.GetCameraTransform();

        if (_isInspectingPhoto) transform.position = target.position + (camTransform.forward * inspectCameraOffset);
        else
        {
            Vector3 desiredPos = target.position +
                (camTransform.right * cameraOffset.x) +
                (camTransform.up * cameraOffset.y) +
                (camTransform.forward * cameraOffset.z);
            Vector3 origin = desiredPos + camTransform.forward * -castBackDistance;
            
            if (Physics.SphereCast(
                origin,
                sphereRadius,
                camTransform.forward,
                out RaycastHit hit,
                castBackDistance,
                collisionLayers
            ))
            {
                castStartPoint = origin;
                castDistancePoint = origin + camTransform.forward * hit.distance;

                desiredPos -= camTransform.forward * (castBackDistance - hit.distance);
            }
            transform.position = desiredPos;
        }
    }

    private Vector3 castStartPoint;
    private Vector3 castDistancePoint;

    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(castStartPoint, 0.01f);
        Gizmos.DrawWireSphere(castDistancePoint, sphereRadius);
        Gizmos.DrawLine(castStartPoint, castDistancePoint);
    }
}
