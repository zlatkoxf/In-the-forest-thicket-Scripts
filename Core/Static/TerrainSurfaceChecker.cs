using UnityEngine;

public static class TerrainSurfaceChecker
{
    private static Color[][] _cachedPixels;
    private static Texture2D[] _cachedTextures;

    public static void CacheTextures(Texture2D[] textures)
    {
        ClearCache();
        if (textures == null || textures.Length == 0) return;

        int length = textures.Length;
        _cachedPixels = new Color[length][];
        _cachedTextures = new Texture2D[length];

        for (int i = 0; i < length; i++)
        {
            Texture2D tex = textures[i];
            _cachedTextures[i] = tex;
            if (tex != null)
                _cachedPixels[i] = tex.GetPixels();
            else
                _cachedPixels[i] = null;
        }
    }

    public static void ClearCache()
    {
        _cachedPixels = null;
        _cachedTextures = null;
    }

    public static void GetLayerWeightsNonAlloc(Vector2 uv, Texture2D[] maskTextures, float[] outputBuffer, int startIndex)
    {
        if (maskTextures == null || maskTextures.Length == 0 || outputBuffer == null) { Debug.LogError("[TerrainSurfaceChecker] Некорректные входные данные или буфер пуст!"); return; }
        int length = maskTextures.Length;
        if (startIndex + length > outputBuffer.Length) { Debug.LogError("[TerrainSurfaceChecker] Целевой буфер слишком мал для записи всех слоев с указанного индекса!"); return; }

        bool hasCache = _cachedPixels != null && _cachedTextures != null && _cachedPixels.Length == _cachedTextures.Length && _cachedPixels.Length == length;

        for (int i = 0; i < length; i++)
        {
            Texture2D mask = maskTextures[i];
            int targetIndex = startIndex + i;
            if (mask == null) { outputBuffer[targetIndex] = 0f; continue; }

            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * mask.width), 0, mask.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * mask.height), 0, mask.height - 1);

            bool cached = hasCache && _cachedTextures[i] == mask && _cachedPixels[i] != null;
            if (cached)
            {
                int pixelIndex = y * mask.width + x;
                outputBuffer[targetIndex] = _cachedPixels[i][pixelIndex].r;
            }
            else
            {
                outputBuffer[targetIndex] = mask.GetPixel(x, y).r;
            }
        }
    }
}