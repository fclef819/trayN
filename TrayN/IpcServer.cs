using System.Threading;

namespace TrayN;

internal sealed class IpcServer : IDisposable
{
    private readonly EventWaitHandle signal;
    private readonly SynchronizationContext uiContext;
    private readonly Action onShowRequested;
    private readonly Thread thread;
    private volatile bool disposed;

    public IpcServer(SynchronizationContext uiContext, Action onShowRequested)
    {
        this.uiContext = uiContext;
        this.onShowRequested = onShowRequested;
        signal = IpcSignal.CreateServerEvent();
        thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "TrayN IPC listener"
        };
        thread.Start();
    }

    private void Run()
    {
        while (!disposed)
        {
            try
            {
                if (signal.WaitOne(250))
                {
                    uiContext.Post(_ => onShowRequested(), null);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }

    public void Dispose()
    {
        disposed = true;
        try
        {
            signal.Set();
        }
        catch
        {
        }

        signal.Dispose();
    }
}
