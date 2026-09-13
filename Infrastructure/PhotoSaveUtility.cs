using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Threading.Tasks;

public static class PhotoSaveUtility
{
    private static string folderPath;

    static PhotoSaveUtility()
    {
        folderPath = Path.Combine(Application.persistentDataPath, "Photos");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
    }

    public static void SaveAsyncRequestToDisk(AsyncGPUReadbackRequest request, int width, int height)
    {
        if (request.hasError)
        {
            Debug.LogError("[PhotoSaveUtility] Ошибка данных при чтении кадра.");
            return;
        }

        // Забираем сырые байты из GPU и сразу уходим в фоновый поток (Task)
        // Больше никаких Texture2D, GetPixels() и SetPixels() в основном потоке игры!
        byte[] rawBytes = request.GetData<byte>().ToArray();

        Task.Run(() =>
        {
            // Корректируем гамму прямо в сыром массиве байт. 
            // Формат RGB24 идет по очереди: R, G, B, R, G, B... каждый канал занимает 1 байт (0-255)
            for (int i = 0; i < rawBytes.Length; i++)
            {
                // Быстрый математический перевод Linear -> Gamma для байта
                float linearValue = rawBytes[i] / 255f;
                float gammaValue = Mathf.Pow(linearValue, 1f / 2.2f); // Математический аналог pixels[i].gamma
                rawBytes[i] = (byte)(gammaValue * 255f);
            }

            // Асинхронно кодируем массив байт в PNG без создания текстурных объектов в RAM
            byte[] pngBytes = ImageConversion.EncodeArrayToPNG(rawBytes, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8_SRGB, (uint)width, (uint)height);

            // Записываем файл на диск
            string fileName = $"Photo_WithEffects_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(folderPath, fileName);
            File.WriteAllBytes(fullPath, pngBytes);

            Debug.Log($"<color=green>[PhotoSaveUtility]</color> Кадр сохранен асинхронно: {fullPath}");
        });
    }
}
