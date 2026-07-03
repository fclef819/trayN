namespace TrayN;

internal static class AppPaths
{
    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "trayn");

    public static string MemoFile { get; } = Path.Combine(DataDirectory, "memo.txt");

    public static string SettingsFile { get; } = Path.Combine(DataDirectory, "settings.json");
}
