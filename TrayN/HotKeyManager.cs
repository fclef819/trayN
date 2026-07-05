using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TrayN;

internal sealed class HotKeyManager : NativeWindow, IDisposable
{
    private const int HotKeyId = 0x4D4E;
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private bool registered;

    public event EventHandler? HotKeyPressed;
    public HotKeySettings? RegisteredHotKey { get; private set; }

    public HotKeyManager()
    {
        CreateHandle(new CreateParams());
    }

    public bool RegisterDefaultHotKey()
    {
        return TryRegister(HotKeySettings.Default(), out _);
    }

    public bool TryRegister(HotKeySettings settings, out string error)
    {
        if (!HotKeyValidator.TryValidate(settings, out error))
        {
            return false;
        }

        if (RegisterHotKey(Handle, HotKeyId, ToModifierFlags(settings), (uint)settings.Key))
        {
            registered = true;
            RegisteredHotKey = settings.Clone();
            error = string.Empty;
            return true;
        }

        registered = false;
        RegisteredHotKey = null;
        error = LastWin32ErrorMessage();
        return false;
    }

    public bool ChangeHotKey(HotKeySettings newSettings, out string error)
    {
        var previous = RegisteredHotKey?.Clone();
        if (registered)
        {
            UnregisterCurrent();
        }

        if (TryRegister(newSettings, out error))
        {
            return true;
        }

        if (previous is not null && !TryRegister(previous, out var rollbackError))
        {
            error += "\n\n以前のホットキーの再登録にも失敗しました: " + rollbackError;
        }

        return false;
    }

    public void UnregisterCurrent()
    {
        if (!registered)
        {
            return;
        }

        UnregisterHotKey(Handle, HotKeyId);
        registered = false;
        RegisteredHotKey = null;
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
            UnregisterCurrent();
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

    private static uint ToModifierFlags(HotKeySettings settings)
    {
        var modifiers = 0u;
        if (settings.Alt)
        {
            modifiers |= ModAlt;
        }

        if (settings.Control)
        {
            modifiers |= ModControl;
        }

        if (settings.Shift)
        {
            modifiers |= ModShift;
        }

        if (settings.Win)
        {
            modifiers |= ModWin;
        }

        return modifiers;
    }
}
