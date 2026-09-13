using System.Runtime.InteropServices;
using UnityEngine;

public static class MonitorController
{
    [DllImport("user32.dll")]
    private static extern int PostMessage(int hWnd, int hMsg, int wParam, int lParam);

    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;

    public static void SetMonitorState(bool turnOn)
    {
        // 2 — выключить намертво, -1 — включить обратно
        int value = turnOn ? -1 : 2; 
        PostMessage(0xFFFF, WM_SYSCOMMAND, SC_MONITORPOWER, value);
        Debug.Log($"[MonitorController] Монитор переключен в состояние: {turnOn}");
    }
}
