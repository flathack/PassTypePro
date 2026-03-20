using System.Windows.Forms;

namespace PassTypePro.Models;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public sealed class HotkeyDefinition
{
    public HotkeyModifiers Modifiers { get; init; }
    public Keys Key { get; init; }
    public string DisplayText { get; init; } = string.Empty;

    public static bool TryParse(string value, out HotkeyDefinition hotkey)
    {
        hotkey = new HotkeyDefinition();

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        Keys key = Keys.None;

        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "SHIFT":
                case "UMSCHALT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    if (!Enum.TryParse(part, true, out key))
                    {
                        return false;
                    }

                    break;
            }
        }

        if (key == Keys.None)
        {
            return false;
        }

        hotkey = new HotkeyDefinition
        {
            Modifiers = modifiers,
            Key = key,
            DisplayText = value
        };

        return true;
    }
}
