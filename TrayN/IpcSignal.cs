using System.Threading;

namespace TrayN;

internal static class IpcSignal
{
    private const string EventName = @"Local\TrayN.ShowMemo";

    public static EventWaitHandle CreateServerEvent() =>
        new(false, EventResetMode.AutoReset, EventName);

    public static void SendShowRequest()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(EventName);
            ev.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
        catch
        {
        }
    }
}
