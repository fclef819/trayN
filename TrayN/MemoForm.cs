using System.Drawing;
using System.ComponentModel;

namespace TrayN;

internal sealed class MemoForm : Form
{
    private const string IndentText = "\t";

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
        memoTextBox.KeyDown += MemoTextBox_KeyDown;
        memoTextBox.PreviewKeyDown += MemoTextBox_PreviewKeyDown;
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

    private void MemoTextBox_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Tab)
        {
            e.IsInputKey = true;
        }
    }

    private void MemoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Tab || e.Control || e.Alt)
        {
            return;
        }

        if (ApplyLineIndent(memoTextBox, e.Shift))
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    private static bool ApplyLineIndent(TextBox textBox, bool removeIndent)
    {
        if (removeIndent)
        {
            return RemoveLineIndent(textBox);
        }

        if (textBox.SelectionLength == 0)
        {
            textBox.SelectedText = IndentText;
            return true;
        }

        return AddLineIndent(textBox);
    }

    private static bool AddLineIndent(TextBox textBox)
    {
        var text = textBox.Text;
        var selectionStart = textBox.SelectionStart;
        var selectionEnd = selectionStart + textBox.SelectionLength;
        var lineStarts = GetSelectedLineStarts(text, selectionStart, selectionEnd);

        if (lineStarts.Count == 0)
        {
            return false;
        }

        var changes = new List<TextChange>(lineStarts.Count);

        foreach (var lineStart in lineStarts)
        {
            changes.Add(TextChange.Insert(lineStart, IndentText.Length));
        }

        ApplyChangesAndSelection(textBox, selectionStart, selectionEnd, changes);
        return true;
    }

    private static bool RemoveLineIndent(TextBox textBox)
    {
        var text = textBox.Text;
        var selectionStart = textBox.SelectionStart;
        var selectionEnd = selectionStart + textBox.SelectionLength;
        var lineStarts = textBox.SelectionLength == 0
            ? new List<int> { FindLineStart(text, selectionStart) }
            : GetSelectedLineStarts(text, selectionStart, selectionEnd);

        if (lineStarts.Count == 0)
        {
            return false;
        }

        var changes = new List<TextChange>(lineStarts.Count);

        foreach (var lineStart in lineStarts)
        {
            var removeLength = GetIndentRemoveLength(text, lineStart);
            if (removeLength == 0)
            {
                continue;
            }

            changes.Add(TextChange.Remove(lineStart, removeLength));
        }

        if (changes.Count == 0)
        {
            return true;
        }

        ApplyChangesAndSelection(textBox, selectionStart, selectionEnd, changes);
        return true;
    }

    private static void ApplyChangesAndSelection(TextBox textBox, int selectionStart, int selectionEnd, List<TextChange> changes)
    {
        for (var i = changes.Count - 1; i >= 0; i--)
        {
            var change = changes[i];
            textBox.Select(change.Position, change.IsInsertion ? 0 : change.Length);
            textBox.SelectedText = change.IsInsertion ? IndentText : string.Empty;
        }

        var adjustedStart = AdjustSelectionStart(selectionStart, changes);
        var adjustedEnd = AdjustSelectionEnd(selectionEnd, changes);
        adjustedStart = Math.Clamp(adjustedStart, 0, textBox.TextLength);
        adjustedEnd = Math.Clamp(adjustedEnd, adjustedStart, textBox.TextLength);

        textBox.SelectionStart = adjustedStart;
        textBox.SelectionLength = adjustedEnd - adjustedStart;
    }

    private static List<int> GetSelectedLineStarts(string text, int selectionStart, int selectionEnd)
    {
        var lineStart = FindLineStart(text, selectionStart);
        var lastTargetIndex = selectionEnd;

        if (selectionEnd > selectionStart && IsLineStart(text, selectionEnd))
        {
            lastTargetIndex = selectionEnd - 1;
        }

        var endLineStart = FindLineStart(text, Math.Max(selectionStart, lastTargetIndex));
        var starts = new List<int>();

        while (lineStart <= endLineStart)
        {
            starts.Add(lineStart);
            var nextLineStart = FindNextLineStart(text, lineStart);
            if (nextLineStart < 0)
            {
                break;
            }

            lineStart = nextLineStart;
        }

        return starts;
    }

    private static int GetIndentRemoveLength(string text, int lineStart)
    {
        if (lineStart >= text.Length)
        {
            return 0;
        }

        if (text[lineStart] == '\t')
        {
            return 1;
        }

        var spaceCount = 0;
        while (lineStart + spaceCount < text.Length && text[lineStart + spaceCount] == ' ')
        {
            spaceCount++;
        }

        return spaceCount >= 4 ? 4 : spaceCount;
    }

    private static int FindLineStart(string text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);

        if (index == 0)
        {
            return 0;
        }

        var previousNewLine = text.LastIndexOf('\n', index - 1);
        return previousNewLine < 0 ? 0 : previousNewLine + 1;
    }

    private static int FindNextLineStart(string text, int lineStart)
    {
        var newLine = text.IndexOf('\n', lineStart);
        return newLine < 0 ? -1 : newLine + 1;
    }

    private static bool IsLineStart(string text, int index)
    {
        return index <= 0 || index <= text.Length && text[index - 1] == '\n';
    }

    private static int AdjustSelectionStart(int index, List<TextChange> changes)
    {
        return AdjustSelectionIndex(index, changes, includeInsertionAtBoundary: false);
    }

    private static int AdjustSelectionEnd(int index, List<TextChange> changes)
    {
        return AdjustSelectionIndex(index, changes, includeInsertionAtBoundary: true);
    }

    private static int AdjustSelectionIndex(int index, List<TextChange> changes, bool includeInsertionAtBoundary)
    {
        var offset = 0;

        foreach (var change in changes)
        {
            if (change.IsInsertion)
            {
                if (change.Position < index || includeInsertionAtBoundary && change.Position == index)
                {
                    offset += change.Length;
                }

                continue;
            }

            if (index <= change.Position)
            {
                continue;
            }

            var removedEnd = change.Position + change.Length;
            if (index < removedEnd)
            {
                return change.Position + offset;
            }

            offset -= change.Length;
        }

        return index + offset;
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

    private readonly record struct TextChange(int Position, int Length, bool IsInsertion)
    {
        public static TextChange Insert(int position, int length) => new(position, length, true);

        public static TextChange Remove(int position, int length) => new(position, length, false);
    }
}
