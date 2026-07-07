using System.Drawing;
using System.Reflection;

namespace TrayN;

internal static class ResourceIcons
{
    private const string AppIconResourceName = "TrayN.AppIcon.ico";
    private const string TrayIconResourceName = "TrayN.TrayIcon.ico";

    public static Icon LoadAppIcon() => LoadIcon(AppIconResourceName);

    public static Icon LoadTrayIcon() => LoadIcon(TrayIconResourceName);

    private static Icon LoadIcon(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded icon resource was not found: {resourceName}");
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }
}
