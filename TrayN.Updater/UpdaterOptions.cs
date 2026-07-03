namespace TrayN.Updater;

internal sealed class UpdaterOptions
{
    public required int ProcessId { get; init; }
    public required string NewExePath { get; init; }
    public required string CurrentExePath { get; init; }

    public static UpdaterOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {args[i]}.");
            }

            values[args[i]] = args[++i];
        }

        if (!values.TryGetValue("--pid", out var pidValue) || !int.TryParse(pidValue, out var pid))
        {
            throw new ArgumentException("Missing or invalid --pid.");
        }

        if (!values.TryGetValue("--new-exe", out var newExe) || string.IsNullOrWhiteSpace(newExe))
        {
            throw new ArgumentException("Missing --new-exe.");
        }

        if (!values.TryGetValue("--current-exe", out var currentExe) || string.IsNullOrWhiteSpace(currentExe))
        {
            throw new ArgumentException("Missing --current-exe.");
        }

        return new UpdaterOptions
        {
            ProcessId = pid,
            NewExePath = newExe,
            CurrentExePath = currentExe
        };
    }
}
