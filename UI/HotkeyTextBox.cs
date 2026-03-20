using System.Windows.Forms;
using PassTypePro.Models;

namespace PassTypePro.UI;

public sealed class HotkeyTextBox : TextBox
{
    public string HotkeyText { get; private set; } = string.Empty;

    public HotkeyTextBox()
    {
        ReadOnly = true;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;

        if (keyCode is Keys.Back or Keys.Delete)
        {
            SetHotkey(string.Empty);
            return true;
        }

        if (keyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey)
        {
            return true;
        }

        var parts = new List<string>();
        if (modifiers.HasFlag(Keys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(Keys.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(Keys.Alt))
        {
            parts.Add("Alt");
        }

        parts.Add(keyCode.ToString());
        SetHotkey(string.Join("+", parts));
        return true;
    }

    public void SetHotkey(string value)
    {
        HotkeyText = value;
        Text = value;
    }
}
