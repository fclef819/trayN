using System.Diagnostics;

namespace TrayN.Updater;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var options = UpdaterOptions.Parse(args);
            ApplyUpdate(options);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "trayN updater", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void ApplyUpdate(UpdaterOptions options)
    {
        WaitForTrayNToExit(options.ProcessId);

        var currentExe = Path.GetFullPath(options.CurrentExePath);
        var newExe = Path.GetFullPath(options.NewExePath);
        if (!File.Exists(newExe))
        {
            throw new FileNotFoundException("Downloaded trayN.exe was not found.", newExe);
        }

        var appDirectory = Path.GetDirectoryName(currentExe) ?? throw new InvalidOperationException("Current trayN.exe directory could not be resolved.");
        EnsureWritable(appDirectory);

        var backupPath = Path.Combine(appDirectory, "trayN.exe.bak-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        var replaced = false;

        try
        {
            File.Move(currentExe, backupPath, overwrite: true);
            File.Copy(newExe, currentExe, overwrite: true);
            replaced = true;

            Process.Start(new ProcessStartInfo
            {
                FileName = currentExe,
                WorkingDirectory = appDirectory,
                UseShellExecute = true
            });

            TryDelete(newExe);
            TryDelete(backupPath);
            TryDeleteDirectory(Path.GetDirectoryName(newExe));
        }
        catch (Exception updateError)
        {
            if (replaced)
            {
                TryDelete(currentExe);
            }

            try
            {
                if (File.Exists(backupPath))
                {
                    File.Move(backupPath, currentExe, overwrite: true);
                }
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    "Update failed and the previous trayN.exe could not be restored.\n\n" +
                    $"Update error: {updateError.Message}\nRestore error: {restoreError.Message}\nBackup: {backupPath}",
                    restoreError);
            }

            throw new InvalidOperationException("Update failed. The previous trayN.exe was restored.\n\n" + updateError.Message, updateError);
        }
    }

    private static void WaitForTrayNToExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit(30000))
            {
                throw new TimeoutException("trayN.exe did not exit within 30 seconds. Update was canceled.");
            }
        }
        catch (ArgumentException)
        {
        }
    }

    private static void EnsureWritable(string directory)
    {
        var probe = Path.Combine(directory, ".trayn-updater-write-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllText(probe, string.Empty);
        }
        finally
        {
            TryDelete(probe);
        }
    }

    private static void TryDelete(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch
        {
        }
    }
}
