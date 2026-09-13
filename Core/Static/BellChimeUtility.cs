using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public static class BellChimeUtility
{
    /// <summary>
    /// Запускает серию случайных ударов колокольчика в заданной позиции.
    /// </summary>
    /// <param name="caller">Скрипт, который вызывает метод ('this').</param>
    /// <param name="clipKey">Имя аудио-клипа колокольчика.</param>
    /// <param name="position">Мировая позиция для проигрывания звука.</param>
    /// <param name="count">Количество ударов колокольчика в серии.</param>
    /// <param name="group">Группа микшера (необязательно).</param>
    public static Coroutine PlayChime(MonoBehaviour caller, string clipKey, Vector3 position, int count = 3, AudioMixerGroup group = null)
    {
        return caller.StartCoroutine(ChimeRoutine(clipKey, position, count, group));
    }

    private static IEnumerator ChimeRoutine(string clipKey, Vector3 position, int count, AudioMixerGroup group)
    {
        for (int i = 0; i < count; i++)
        {
            // Случайный питч (от 0.85 до 1.15) и громкость (от 0.7 до 0.9)
            float randomPitch = Random.Range(0.8f, 1.2f);
            float randomVolume = Random.Range(0.9f, 1.0f);

            // Обращаемся к вашему нестатическому менеджеру через синглтон Instance
            AudioManager.Instance.PlaySound(
                clipKey: clipKey,
                pos: position,
                volume: randomVolume,
                minDistance: 20,
                pitch: randomPitch,
                group: group
            );

            // Случайная задержка между ударами (от 0.1 до 0.35 секунды)
            yield return new WaitForSeconds(Random.Range(0.1f, 0.35f));
        }
    }
}
