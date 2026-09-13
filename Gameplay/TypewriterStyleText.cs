using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TypewriterStyleText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float fadeDuration = 1.0f; // Длительность исчезновения в секундах
    // Защита от наложения
    private bool _isTextSending = false;
    private AudioManager _audioManager;
    private Color textColor = new Color(1f,1f,1f,1f);

    private WaitForSeconds _normalDelay;
    private WaitForSeconds _commaDelay;
    private WaitForSeconds _dotDelay;

    void Start()
    {
        _audioManager = AudioManager.Instance;

        _normalDelay = new WaitForSeconds(0.1f);
        _commaDelay = new WaitForSeconds(0.3f);
        _dotDelay = new WaitForSeconds(0.4f);
    }

    public void SendStringMessage(string text) => StartCoroutine(SpliterCoroutine(text));

    /// <summary>
    /// Разделяет длинный текст по символу '/' и запускает отображение фраз по очереди.
    /// </summary>
    private IEnumerator SpliterCoroutine(string text)
    {
        string[] phrases = text.Split('/');
        foreach (string phrase in phrases)
        {
            float totalSeconds = 1.5f;
            foreach (char c in phrase)
            {
                if (c == '.') totalSeconds += 0.4f;
                else totalSeconds += 0.1f;
            }
            yield return StartCoroutine(TextSenderAsync(phrase));
            yield return new WaitForSeconds(totalSeconds);
        }
        yield return new WaitForSeconds(fadeDuration);
        StartCoroutine(FadeToZeroAlpha());
    }

    /// <summary>
    /// Посимвольно выводит строку.
    /// </summary>
    private IEnumerator TextSenderAsync(string phrase)
    {
        if (_isTextSending) { Debug.Log($"[TypewriterStyleText] Ошибка: другое сообщение уже отправляется! [{phrase}]"); yield break; }

        textMeshPro.color = textColor;
        _isTextSending = true;
        string currentDisplay = "";

        foreach (char c in phrase)
        {
            currentDisplay += c;
            textMeshPro.text = currentDisplay;

            if (c != ' ') _audioManager.PlaySound("Tap", 0.5f, Utils.GetRandOfRange(0.8f, 1.2f), group: _audioManager.sfxGroup);
            if (c == '.') yield return _dotDelay;
            else if (c == ',' || c == '!' || c == '?') yield return _commaDelay;
            yield return _normalDelay;
        }
        Debug.Log($"[TypewriterStyleText] Отправка сообщения успешно закончена [{phrase}]");
        _isTextSending = false;
    }

    private IEnumerator FadeToZeroAlpha()
    {
        Color startColor = textColor;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            textMeshPro.color = Color.Lerp(startColor, targetColor, elapsedTime / fadeDuration);
            yield return null; 
        }
        textMeshPro.color = targetColor;
    }
}