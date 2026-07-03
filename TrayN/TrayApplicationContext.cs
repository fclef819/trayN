using System.Diagnostics;

namespace TrayN;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SingleInstanceManager singleInstance;
    private readonly MemoStore memoStore = new();
    private readonly SettingsStore settingsStore = new();
    private readonly UpdateService updateService = new();
    private readonly AppSettings settings;
    private readonly MemoForm form;
    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer saveTimer;
    private readonly HotKeyManager hotKeyManager;
    private readonly IpcServer ipcServer;
    private bool savePending;
    private UpdateInfo? latestUpdate;

    public TrayApplicationContext(SingleInstanceManager singleInstance)
    {
        this.singleInstance = singleInstance;
        settings = settingsStore.Load();

        form = new MemoForm();
        form.ApplySavedBounds(settings.SavedBounds);
        form.MemoText = memoStore.Load();
        form.MemoTextChanged += (_, _) => QueueMemoSave();
        form.ResizeEnd += (_, _) => SaveWindowSettings();
        form.Move += (_, _) =>
        {
            if (form.Visible && form.WindowState == FormWindowState.Normal)
            {
                SaveWindowSettings();
            }
        };

        saveTimer = new System.Windows.Forms.Timer { Interval = 500 };
        saveTimer.Tick += (_, _) => FlushMemoSave();

        notifyIcon = CreateNotifyIcon();
        notifyIcon.Visible = true;

        hotKeyManager = new HotKeyManager();
        hotKeyManager.HotKeyPressed += (_, _) => ToggleMemoWindow();
        if (!hotKeyManager.RegisterDefaultHotKey())
        {
            ShowBalloon("Shortcut unavailable", "Ctrl + Alt + M could not be registered: " + HotKeyManager.LastWin32ErrorMessage(), ToolTipIcon.Warning);
        }

        var context = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        ipcServer = new IpcServer(context, ShowMemoWindow);

        _ = CheckForUpdatesOnStartupAsync();
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("メモを表示", null, (_, _) => ShowMemoWindow());
        menu.Items.Add("アップデートを確認", null, async (_, _) => await CheckForUpdatesManualAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitFromMenu());

        var icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "trayN",
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => ShowMemoWindow();
        icon.BalloonTipClicked += async (_, _) =>
        {
            if (latestUpdate is not null)
            {
                await PromptAndApplyUpdateAsync(latestUpdate);
            }
        };
        return icon;
    }

    private void ToggleMemoWindow()
    {
        if (form.Visible)
        {
            form.Hide();
        }
        else
        {
            ShowMemoWindow();
        }
    }

    private void ShowMemoWindow()
    {
        form.ShowAndFocusMemo();
    }

    private void QueueMemoSave()
    {
        savePending = true;
        saveTimer.Stop();
        saveTimer.Start();
    }

    private void FlushMemoSave()
    {
        saveTimer.Stop();
        if (!savePending)
        {
            return;
        }

        savePending = false;
        if (!memoStore.Save(form.MemoText))
        {
            ShowBalloon("Memo save failed", "The current memo could not be saved.", ToolTipIcon.Error);
        }
    }

    private void SaveWindowSettings()
    {
        if (form.WindowState != FormWindowState.Normal)
        {
            return;
        }

        settings.SetBounds(form.Bounds);
        settingsStore.Save(settings);
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        var lastCheck = settings.LastUpdateCheckUtc;
        if (lastCheck is not null && DateTimeOffset.UtcNow - lastCheck.Value < TimeSpan.FromHours(24))
        {
            return;
        }

        await CheckForUpdatesAsync(manual: false);
    }

    private async Task CheckForUpdatesManualAsync()
    {
        await CheckForUpdatesAsync(manual: true);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var update = await updateService.CheckLatestAsync(cts.Token);
            settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
            settingsStore.Save(settings);

            if (update is null)
            {
                if (manual)
                {
                    MessageBox.Show("利用可能な更新はありません。", "trayN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            latestUpdate = update;
            if (manual)
            {
                await PromptAndApplyUpdateAsync(update);
            }
            else
            {
                ShowBalloon("trayN update available", $"Version {update.NewVersion} is available.", ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (manual)
            {
                MessageBox.Show("更新確認に失敗しました。\n\n" + ex.Message, "trayN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private async Task PromptAndApplyUpdateAsync(UpdateInfo update)
    {
        if (!UpdateService.CanWriteApplicationDirectory(out var reason))
        {
            MessageBox.Show("配置先ディレクトリに書き込み権限がないため更新できません。\n\n" + reason, "trayN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var message = $"現在のバージョン: {update.CurrentVersion}\n新しいバージョン: {update.NewVersion}\n\n{update.ReleaseNotes}\n\n更新を適用しますか?";
        var result = MessageBox.Show(message, "trayN update", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            FlushMemoSave();
            SaveWindowSettings();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var downloadedExePath = await updateService.DownloadAndVerifyAsync(update, cts.Token);
            if (UpdateService.TryStartUpdater(downloadedExePath))
            {
                ExitForUpdate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("更新の準備に失敗しました。\n\n" + ex.Message, "trayN", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExitFromMenu()
    {
        ExitThread();
    }

    private void ExitForUpdate()
    {
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        FlushMemoSave();
        SaveWindowSettings();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        saveTimer.Dispose();
        ipcServer.Dispose();
        hotKeyManager.Dispose();
        form.AllowRealClose();
        form.Close();
        form.Dispose();
        singleInstance.Dispose();
        base.ExitThreadCore();
    }

    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        notifyIcon.BalloonTipTitle = title;
        notifyIcon.BalloonTipText = text;
        notifyIcon.BalloonTipIcon = icon;
        notifyIcon.ShowBalloonTip(5000);
    }
}
