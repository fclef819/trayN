namespace TrayN;

internal static class HotKeyValidator
{
    public static HotKeySettings NormalizeOrDefault(HotKeySettings? settings)
    {
        if (settings is not null && TryValidate(settings, out _))
        {
            return settings.Clone();
        }

        return HotKeySettings.Default();
    }

    public static bool TryValidate(HotKeySettings settings, out string error)
    {
        if (settings.Key == Keys.None || IsModifierKey(settings.Key))
        {
            error = "修飾キーだけではホットキーにできません。通常キーを1つ押してください。";
            return false;
        }

        if (!settings.Control && !settings.Alt && !settings.Shift && !settings.Win)
        {
            error = "通常キーだけではホットキーにできません。Ctrl、Alt、Shift、Win のいずれかを含めてください。";
            return false;
        }

        if (IsAlwaysInvalidKey(settings.Key))
        {
            error = $"{HotKeyFormatter.Format(settings)} はホットキーにできません。";
            return false;
        }

        if (IsInvalidSingleKey(settings))
        {
            error = $"{HotKeyFormatter.Format(settings)} は単体ではホットキーにできません。";
            return false;
        }

        if (IsReservedWindowsShortcut(settings))
        {
            error = $"{HotKeyFormatter.Format(settings)} は Windows の操作と競合するため使用できません。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsInvalidSingleKey(HotKeySettings settings)
    {
        if (settings.Control || settings.Alt || settings.Shift || settings.Win)
        {
            return false;
        }

        return settings.Key is Keys.Escape or Keys.Enter or Keys.Tab or Keys.Back or Keys.Delete;
    }

    private static bool IsAlwaysInvalidKey(Keys key)
    {
        return key is Keys.PrintScreen or Keys.Pause or Keys.CapsLock or Keys.NumLock or Keys.Scroll;
    }

    private static bool IsReservedWindowsShortcut(HotKeySettings settings)
    {
        return settings.Key switch
        {
            Keys.Tab when settings.Alt && !settings.Control && !settings.Shift && !settings.Win => true,
            Keys.F4 when settings.Alt && !settings.Control && !settings.Shift && !settings.Win => true,
            Keys.Delete when settings.Control && settings.Alt && !settings.Shift && !settings.Win => true,
            Keys.L when settings.Win && !settings.Control && !settings.Alt && !settings.Shift => true,
            Keys.D when settings.Win && !settings.Control && !settings.Alt && !settings.Shift => true,
            Keys.E when settings.Win && !settings.Control && !settings.Alt && !settings.Shift => true,
            Keys.R when settings.Win && !settings.Control && !settings.Alt && !settings.Shift => true,
            Keys.Tab when settings.Win && !settings.Control && !settings.Alt && !settings.Shift => true,
            _ => false
        };
    }

    private static bool IsModifierKey(Keys key)
    {
        return key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LWin or Keys.RWin;
    }
}
