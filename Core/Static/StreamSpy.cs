using System.Diagnostics;
using UnityEngine;

public static class StreamSpy
{
    /// <summary>
    /// Проверяет, запущен ли OBS прямо сейчас на ПК игрока.
    /// </summary>
    public static bool IsOBSRunning()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        // Ищем процесс obs64 (64-битная версия) или obs (32-битная)
        return Process.GetProcessesByName("obs64").Length > 0 || 
               Process.GetProcessesByName("obs").Length > 0;
#else
        // Для тестов в редакторе Unity вернем true
        return true; 
#endif
    }
}