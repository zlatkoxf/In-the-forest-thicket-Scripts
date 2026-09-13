using UnityEngine;

public static class HiddenCamera
{
    private static WebCamTexture _webcamTexture;

    /// <summary>
    /// Включает или выключает физический индикатор веб-камеры.
    /// </summary>
    /// <param name="state">true — включить, false — выключить</param>
    public static bool? Toggle(bool state)
    {
        if (state)
        {
            // Если камера уже работает, ничего не делаем
            if (_webcamTexture != null && _webcamTexture.isPlaying) return null;

            if (WebCamTexture.devices.Length > 0)
            {
                _webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 16, 16, 1);
                _webcamTexture.Play();
                Debug.Log("[HiddenCamera] Индикатор камеры зажжен.");
                return true;
            }
            else
            {
                Debug.LogWarning("[HiddenCamera] Веб-камера на ПК не обнаружена.");
                return false;
            }
        }
        else
        {
            // Выключаем камеру
            if (_webcamTexture != null && _webcamTexture.isPlaying)
            {
                _webcamTexture.Stop();
                _webcamTexture = null; // Освобождаем память
                Debug.Log("[HiddenCamera] Индикатор камеры потушен.");
            }
        }
        return null;
    }
}