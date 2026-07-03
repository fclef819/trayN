using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TrayN;

internal sealed class HotKeyManager : NativeWindow, IDisposable
{
    private const int HotKeyId = 0x4D4E;
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private bool registered;

    public event EventHandler? HotKeyPressed;

    public HotKeyManager()
    {
        CreateHandle(new CreateParams());
    }

    public bool RegisterDefaultHotKey()
    {
        registered = RegisterHotKey(Handle, HotKeyId, ModControl | ModAlt, (uint)Keys.M);
        return registered;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey && m.WParam.ToInt32() == HotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (registered)
        {
            UnregisterHotKey(Handle, HotKeyId);
            registered = false;
        }

        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static string LastWin32ErrorMessage()
    {
        var error = Marshal.GetLastWin32Error();
        return new Win32Exception(error).Message;
    }
}
