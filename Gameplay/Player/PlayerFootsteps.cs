using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [System.Serializable]
    public class LayerAudioGroup
    {
        [Tooltip("Название для удобства в инспекторе (например, Трава, Грязь)")]
        public string layerName;
        public PhysicsMaterial physicMaterial;
        [Tooltip("Массив строковых ключей для AudioManager (например, Grass_1, Grass_2, Grass_3)")]
        public string[] audioKeys;
        // Хранилище для ref-переменной (скрыто в инспекторе)
        [HideInInspector] public string lastPlayedKey = string.Empty;
    }

    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private DynamicCamera dynamicCamera;
    [SerializeField] private FootstepGenerator footstepGenerator;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private LayerAudioGroup[] layerAudioGroups;
    [SerializeField] private Texture2D[] upperSurfaceMasks;

    private AudioManager _audioManager;

    //private const string KEY_STONE = "Stone";
    
    // Кеширующие буферы (NonAlloc), чтобы избежать GC.Alloc
    private float[] _rawWeightsBuffer;
    private float[] _visibilityBuffer;
    private float[] _audioVolumeBuffer;

    void Start()
    {
        _audioManager = AudioManager.Instance;

        int totalLayersCount = layerAudioGroups.Length;
        _rawWeightsBuffer = new float[totalLayersCount];
        _visibilityBuffer = new float[totalLayersCount];
        _audioVolumeBuffer = new float[totalLayersCount];

        TerrainSurfaceChecker.CacheTextures(upperSurfaceMasks);

        if (footstepGenerator != null) footstepGenerator.OnStepTaken += ProcessFootstepSound;
        if (dynamicCamera != null)
        {
            if (footstepGenerator != null)
            { 
                Debug.LogWarning("[PlayerFootsteps] определен dynamicCamera, footstepGenerator не будет использован.");
                footstepGenerator.OnStepTaken -= ProcessFootstepSound;
            }
            dynamicCamera.OnStepTaken += ProcessFootstepSound;
        }
        if (playerCharacter != null)
        {
            playerCharacter.OnJumped += ProcessJumpSound;
            playerCharacter.OnLanded += ProcessFootstepSound;
        }
    }

    private void OnDestroy()
    {
        if (footstepGenerator != null) footstepGenerator.OnStepTaken -= ProcessFootstepSound;
        if (dynamicCamera != null) dynamicCamera.OnStepTaken -= ProcessFootstepSound;
        TerrainSurfaceChecker.ClearCache();
    }

    private void ProcessFootstepSound(float stepForce)
    {
        var (point, normal, material) = playerCharacter.CollisionHitInfo;
        System.Array.Clear(_rawWeightsBuffer, 0, _rawWeightsBuffer.Length);

        // ============ Звук камня на крутом склоне ============ выключено до пересмотра целесообразности. (на угле >50 звука шагов не должно быть - игрок скатывается.)
        // float slopeAngle = Vector3.Angle(normal, Vector3.up);
        // if (slopeAngle > 45f)
        // {
        //     int rockLayerIndex = -1;
        //     for (int i = 0; i < layerAudioGroups.Length; i++)
        //     { if (layerAudioGroups[i].layerName.Contains(KEY_STONE))
        //         { rockLayerIndex = i; break; } }
        //     if (rockLayerIndex != -1)
        //     {   _rawWeightsBuffer[rockLayerIndex] = 1.0f;
        //         PlayCalculatedFootstep(point, stepForce);
        //         return; }
        // }

        // ============ Проверка физического материала ============
        if (material != null)
        {
            int explicitLayerIndex = -1;
            for (int i = 0; i < layerAudioGroups.Length; i++)
            { if (layerAudioGroups[i].physicMaterial == material) { explicitLayerIndex = i; break; } }
            if (explicitLayerIndex != -1)
            { _rawWeightsBuffer[explicitLayerIndex] = 1.0f; PlayCalculatedFootstep(point, stepForce); return; }
        }

        // ============ raycast для mesh terrain ============
        Vector3 origin = point + (normal * 0.05f);
        Ray ray = new Ray(origin, -normal);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 0.1f, terrainLayer))
        {
            // 1. Задаем базовый слой (всегда 1.0)
            _rawWeightsBuffer[0] = 1.0f;
            // 2. Считываем маски (начиная с индекса 1)
            TerrainSurfaceChecker.GetLayerWeightsNonAlloc(hit.textureCoord, upperSurfaceMasks, _rawWeightsBuffer, startIndex: 1);
            PlayCalculatedFootstep(point, stepForce);
        }
    }

    private void PlayCalculatedFootstep(Vector3 playPosition, float stepForce)
    {
        // 3. Расчет видимости (Alpha Blending)
        LayerCompositor.CalculateVisibilityNonAlloc(_rawWeightsBuffer, _visibilityBuffer);
        // 4. Корректировка громкости (Constant Power)
        LayerCompositor.ApplyConstantPowerCorrection(_visibilityBuffer, _audioVolumeBuffer);

        for (int i = 0; i < _audioVolumeBuffer.Length; i++)
        {
            float layerVolume = _audioVolumeBuffer[i];
            float finalVolume = layerVolume * stepForce;
            if (finalVolume > 0.01f)
            {
                LayerAudioGroup group = layerAudioGroups[i];
                string randomAudioKey = AudioUtils.GetRandomKeyWithoutRepeat(group.audioKeys, ref group.lastPlayedKey);
                if (!string.IsNullOrEmpty(randomAudioKey))
                    _audioManager.PlaySound(randomAudioKey, pos: playPosition, finalVolume, spatialBlend: 1, group: _audioManager.sfxGroup);
            }
        }
    }

    private void ProcessJumpSound() 
    {
        if (playerCharacter.GetGroundingStatus().IsStableOnGround) ProcessFootstepSound(1f);
    }

    // void OnDrawGizmos()
    // {
    //     Vector3 point = playerCharacter.CollisionHitInfo.hitPoint;
    //     Vector3 normal = playerCharacter.CollisionHitInfo.hitNormal;
    //     Vector3 origin = point + (normal * 0.05f);
    //     Vector3 rayEnd = origin + (-normal * 0.1f);
    //     Gizmos.DrawWireSphere(point, 0.03f);
    //     Gizmos.DrawWireSphere(origin, 0.01f);
    //     Gizmos.DrawWireSphere(rayEnd, 0.01f);
    //     Gizmos.DrawLine(origin, rayEnd);
    // }
}
