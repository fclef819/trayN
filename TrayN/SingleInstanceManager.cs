using System.Threading;

namespace TrayN;

internal sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = @"Local\TrayN.SingleInstance";
    private Mutex? mutex;
    private bool ownsMutex;

    public bool TryAcquire()
    {
        mutex = new Mutex(true, MutexName, out ownsMutex);
        return ownsMutex;
    }

    public void Dispose()
    {
        if (mutex is null)
        {
            return;
        }

        if (ownsMutex)
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        mutex.Dispose();
        mutex = null;
        ownsMutex = false;
    }
}
