using System.Diagnostics;
public static class PostGameNotifier
{
    public static void SendNotification(string title, string message)
    {
        try
        {
            string htaCode = $"javascript:setTimeout(function(){{ " +
                             $"alert('{message}'); window.close(); }}, 20000);";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "mshta.exe",
                Arguments = $"\"{htaCode}\"",
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[PostGameNotifier] Ошибка: {ex.Message}");
        }
    }
}