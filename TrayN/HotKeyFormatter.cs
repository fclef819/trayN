namespace TrayN;

internal static class HotKeyFormatter
{
    public static string Format(HotKeySettings settings)
    {
        var parts = new List<string>(5);
        if (settings.Control)
        {
            parts.Add("Ctrl");
        }

        if (settings.Alt)
        {
            parts.Add("Alt");
        }

        if (settings.Shift)
        {
            parts.Add("Shift");
        }

        if (settings.Win)
        {
            parts.Add("Win");
        }

        if (settings.Key != Keys.None)
        {
            parts.Add(FormatKey(settings.Key));
        }

        return parts.Count == 0 ? "(未設定)" : string.Join(" + ", parts);
    }

    private static string FormatKey(Keys key)
    {
        return key switch
        {
            Keys.Oemtilde => "`",
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            Keys.Space => "Space",
            Keys.Next => "PageDown",
            _ => key.ToString()
        };
    }
}
