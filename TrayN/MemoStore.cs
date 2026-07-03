using System.Text;

namespace TrayN;

internal sealed class MemoStore
{
    public string Load()
    {
        try
        {
            if (!File.Exists(AppPaths.MemoFile))
            {
                return string.Empty;
            }

            return File.ReadAllText(AppPaths.MemoFile, Encoding.UTF8);
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool Save(string text)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.WriteAllText(AppPaths.MemoFile, text, Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
