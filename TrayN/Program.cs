using System.Threading;

namespace TrayN;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstance = new SingleInstanceManager();
        if (!singleInstance.TryAcquire())
        {
            IpcSignal.SendShowRequest();
            return;
        }

        using var context = new TrayApplicationContext(singleInstance);
        Application.Run(context);
    }
}
