using UnityEngine;

public static class CameraUtils
{
    /// <summary>
    /// Находит случайную 3D-точку перед камерой в заданном диапазоне экранных координат.
    /// </summary>
    /// <param name="camera">Камера, от которой считается обзор.</param>
    /// <param name="minViewport">Минимальные границы экрана (X и Y от 0.0 до 1.0).</param>
    /// <param name="maxViewport">Максимальные границы экрана (X и Y от 0.0 до 1.0).</param>
    /// <param name="distance">Дистанция от камеры в метрах.</param>
    /// <returns>Точка в мировых координатах.</returns>
    public static Vector3 GetRandomPointInViewport(this Camera camera, Vector2 minViewport, Vector2 maxViewport, float distance)
    {
        // 1. Генерируем случайную относительную позицию на экране
        float randomX = Random.Range(minViewport.x, maxViewport.x);
        float randomY = Random.Range(minViewport.y, maxViewport.y);
        
        // 2. Формируем вектор для перевода (Z — это расстояние от линзы камеры)
        Vector3 viewportPoint = new Vector3(randomX, randomY, distance);
        
        // 3. Конвертируем в мировые координаты и возвращаем
        return camera.ViewportToWorldPoint(viewportPoint);
    }
}