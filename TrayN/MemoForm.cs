using System.Drawing;
using System.ComponentModel;

namespace TrayN;

internal sealed class MemoForm : Form
{
    private readonly TextBox memoTextBox;
    private bool allowClose;

    public event EventHandler? MemoTextChanged;

    public MemoForm()
    {
        Text = "trayN";
        Width = 500;
        Height = 350;
        MinimumSize = new Size(320, 220);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;

        memoTextBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10.5f),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true
        };
        memoTextBox.TextChanged += (_, _) => MemoTextChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(memoTextBox);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string MemoText
    {
        get => memoTextBox.Text;
        set
        {
            if (memoTextBox.Text != value)
            {
                memoTextBox.Text = value;
            }
        }
    }

    public void ApplySavedBounds(Rectangle? savedBounds)
    {
        var bounds = savedBounds ?? new Rectangle(0, 0, 500, 350);
        bounds.Width = Math.Max(MinimumSize.Width, bounds.Width);
        bounds.Height = Math.Max(MinimumSize.Height, bounds.Height);

        if (!IsMostlyVisible(bounds))
        {
            var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
            bounds.X = area.X + (area.Width - bounds.Width) / 2;
            bounds.Y = area.Y + (area.Height - bounds.Height) / 2;
        }

        Bounds = bounds;
    }

    public void ShowAndFocusMemo()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Show();
        Activate();
        memoTextBox.Focus();
        memoTextBox.SelectionStart = memoTextBox.TextLength;
        memoTextBox.SelectionLength = 0;
    }

    public void AllowRealClose()
    {
        allowClose = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private static bool IsMostlyVisible(Rectangle bounds)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var intersection = Rectangle.Intersect(screen.WorkingArea, bounds);
            if (intersection.Width >= 80 && intersection.Height >= 80)
            {
                return true;
            }
        }

        return false;
    }
}
