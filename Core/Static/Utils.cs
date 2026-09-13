using UnityEngine;

/// <summary>
/// Сериализуемый диапазон значений.
/// </summary>
[System.Serializable]
public struct Range
{
    public float Min;
    public float Max;

    public Range(float min, float max)
    {
        Min = Mathf.Min(min, max);
        Max = Mathf.Max(min, max);
    }
}

/// <summary>
/// Набор вспомогательных утилит.
/// </summary>
public static class Utils
{
    private static readonly System.Random _rnd = new System.Random();

    /// <summary>
    /// Возвращает случайный аудиоклип из массива, стараясь не повторять предыдущий.
    /// </summary>
    /// <param name="clipArray">Массив клипов (не должен быть null или пустым).</param>
    /// <param name="previousClip">Предыдущий выбранный клип (может быть null).</param>
    /// <returns>Кортеж (выбранный клип, обновлённый предыдущий клип).</returns>
    /// <exception cref="System.ArgumentNullException">Если clipArray == null.</exception>
    /// <exception cref="System.ArgumentException">Если clipArray пуст.</exception>
    public static (AudioClip selected, AudioClip previous) GetRandClip(AudioClip[] clipArray, AudioClip previousClip = null)
    {
        if (clipArray == null)
            throw new System.ArgumentNullException(nameof(clipArray));
        if (clipArray.Length == 0)
            throw new System.ArgumentException("Массив клипов не может быть пустым.", nameof(clipArray));

        if (clipArray.Length == 1)
            return (clipArray[0], clipArray[0]);

        // Попытка выбрать клип, отличный от предыдущего.
        // Используем UnityEngine.Random для индекса – так мы единообразно работаем со случайностью.
        int selectedIndex = Random.Range(0, clipArray.Length);
        AudioClip selected = clipArray[selectedIndex];

        // Если выбранный совпадает с предыдущим и предыдущий присутствует в массиве – ищем другой.
        if (previousClip != null && selected == previousClip)
        {
            // Пытаемся найти индекс предыдущего клипа (если он есть в массиве).
            int prevIndex = System.Array.IndexOf(clipArray, previousClip);
            if (prevIndex != -1)
            {
                // Выбираем случайный индекс, не равный prevIndex.
                // Поскольку длина > 1, всегда существует хотя бы один другой индекс.
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, clipArray.Length);
                } while (newIndex == prevIndex);
                selected = clipArray[newIndex];
            }
            // Если предыдущий клип не найден в массиве – оставляем выбранный (он всё равно не совпадает).
        }

        return (selected, selected);
    }

    /// <summary>
    /// Вычисляет угол рыскания (Yaw) из кватерниона в диапазоне [-180, 180].
    /// </summary>
    public static float GetYawAngle(Quaternion rotation)
    {
        // Можно использовать встроенное свойство, но оно даёт [0, 360).
        // Ручное вычисление даёт симметричный диапазон, что иногда удобнее.
        float x = rotation.x;
        float y = rotation.y;
        float z = rotation.z;
        float w = rotation.w;

        float angleY = Mathf.Atan2(2 * (w * y + x * z), 1 - 2 * (y * y + z * z)) * Mathf.Rad2Deg;
        return angleY;
    }

    /// <summary>
    /// Возвращает случайное число с плавающей точкой в диапазоне [min, max] (включительно для обоих границ).
    /// При некорректном порядке аргументов они нормализуются.
    /// </summary>
    public static float GetRandOfRange(float min, float max)
    {
        if (min > max) (min, max) = (max, min);
        return (float)(_rnd.NextDouble() * (max - min) + min);
    }
    
    /// <summary>
    /// Возвращает случайное число из заданного диапазона.
    /// </summary>
    public static float GetRandOfRange(Range range)
    {
        return (float)(_rnd.NextDouble() * (range.Max - range.Min) + range.Min);
    }

    /// <summary>
    /// Проверяет, сработал ли шанс в процентах (значение от 0 до 100).
    /// </summary>
    /// <param name="n">Шанс в процентах (0..100). Значения вне диапазона обрезаются.</param>
    public static bool Chance(float n)
    {
        if (n <= 0) return false;
        return (_rnd.NextDouble() * 100) <= n;
    }
}

/// <summary>
/// Расширения для пересчёта значений из одного диапазона в другой.
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// Пересчитывает значение из исходного диапазона в целевой.
    /// </summary>
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (Mathf.Approximately(fromMin, fromMax)) return toMin;
        float normalized = Mathf.InverseLerp(fromMin, fromMax, value);
        return Mathf.Lerp(toMin, toMax, normalized);
    }

    /// <summary>
    /// Пересчитывает обе компоненты вектора из исходного диапазона в целевой.
    /// </summary>
    public static Vector2 Remap(this Vector2 value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (Mathf.Approximately(fromMin, fromMax)) return new Vector2(toMin, toMin);
        return new Vector2(
            value.x.Remap(fromMin, fromMax, toMin, toMax),
            value.y.Remap(fromMin, fromMax, toMin, toMax)
        );
    }

    /// <summary>
    /// Пересчитывает все компоненты вектора из исходного диапазона в целевой.
    /// </summary>
    public static Vector3 Remap(this Vector3 value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (Mathf.Approximately(fromMin, fromMax)) return new Vector3(toMin, toMin, toMin);
        return new Vector3(
            value.x.Remap(fromMin, fromMax, toMin, toMax),
            value.y.Remap(fromMin, fromMax, toMin, toMax),
            value.z.Remap(fromMin, fromMax, toMin, toMax)
        );
    }
}

public static class AudioUtils
{
    /// <summary>
    /// Выбирает случайную строку из массива, гарантируя, что она не совпадет с previousKey.
    /// Автоматически обновляет переменную previousKey через ref.
    /// </summary>
    public static string GetRandomKeyWithoutRepeat(string[] keys, ref string previousKey)
    {
        if (keys == null || keys.Length == 0) return string.Empty;
        if (keys.Length == 1) 
        {
            previousKey = keys[0];
            return keys[0];
        }

        // Выбираем случайный индекс
        int selectedIndex = UnityEngine.Random.Range(0, keys.Length);
        string selected = keys[selectedIndex];

        // Если выбранный ключ совпадает с прошлым, сдвигаем индекс без do-while цикла
        if (selected == previousKey)
        {
            int offset = UnityEngine.Random.Range(1, keys.Length);
            selectedIndex = (selectedIndex + offset) % keys.Length;
            selected = keys[selectedIndex];
        }

        // Обновляем историю для следующего вызова
        previousKey = selected;
        return selected;
    }
}