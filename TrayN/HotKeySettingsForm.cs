using System.Runtime.InteropServices;

namespace TrayN;

internal sealed class HotKeySettingsForm : Form
{
    private readonly Label currentValueLabel;
    private readonly TextBox inputTextBox;
    private readonly Button saveButton;
    private HotKeySettings candidate;

    public HotKeySettingsForm(HotKeySettings current)
    {
        candidate = current.Clone();

        Text = "ホットキー設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 190);

        var currentLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 18),
            Text = "現在のホットキー:"
        };

        currentValueLabel = new Label
        {
            AutoSize = true,
            Location = new Point(130, 18),
            Text = HotKeyFormatter.Format(current)
        };

        var inputLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 58),
            Text = "新しいホットキー:"
        };

        inputTextBox = new TextBox
        {
            Location = new Point(130, 54),
            ReadOnly = true,
            Size = new Size(210, 25),
            TabStop = true,
            Text = HotKeyFormatter.Format(candidate)
        };
        inputTextBox.KeyDown += InputTextBox_KeyDown;

        var defaultButton = new Button
        {
            Location = new Point(16, 105),
            Size = new Size(110, 30),
            Text = "既定値へ戻す"
        };
        defaultButton.Click += (_, _) =>
        {
            candidate = HotKeySettings.Default();
            inputTextBox.Text = HotKeyFormatter.Format(candidate);
            inputTextBox.Focus();
        };

        saveButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(158, 145),
            Size = new Size(85, 30),
            Text = "保存"
        };
        saveButton.Click += SaveButton_Click;

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(255, 145),
            Size = new Size(85, 30),
            Text = "キャンセル"
        };

        Controls.AddRange(new Control[] { currentLabel, currentValueLabel, inputLabel, inputTextBox, defaultButton, saveButton, cancelButton });
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    public HotKeySettings SelectedHotKey => candidate.Clone();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        inputTextBox.Focus();
    }

    private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        var key = NormalizeKey(e.KeyCode);
        candidate = new HotKeySettings
        {
            Control = e.Control,
            Alt = e.Alt,
            Shift = e.Shift,
            Win = IsKeyDown(Keys.LWin) || IsKeyDown(Keys.RWin) || e.KeyCode is Keys.LWin or Keys.RWin,
            Key = HotKeyValidatorKeyIsModifier(key) ? Keys.None : key
        };

        inputTextBox.Text = HotKeyFormatter.Format(candidate);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (HotKeyValidator.TryValidate(candidate, out var error))
        {
            return;
        }

        DialogResult = DialogResult.None;
        MessageBox.Show(this, error, "trayN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        inputTextBox.Focus();
    }

    private static Keys NormalizeKey(Keys key)
    {
        return key switch
        {
            Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => Keys.ControlKey,
            Keys.Menu or Keys.LMenu or Keys.RMenu => Keys.Menu,
            Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => Keys.ShiftKey,
            Keys.LWin or Keys.RWin => key,
            _ => key
        };
    }

    private static bool HotKeyValidatorKeyIsModifier(Keys key)
    {
        return key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LWin or Keys.RWin;
    }

    private static bool IsKeyDown(Keys key)
    {
        return (GetKeyState((int)key) & 0x8000) != 0;
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
}
