using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using PassTypePro.Native;

namespace PassTypePro.Services;

public sealed class KeyboardInjectionService
{
    public async Task TypeTextAsync(string text, int initialDelayMs = 150, int keystrokeDelayMs = 0)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (initialDelayMs > 0)
        {
            await Task.Delay(initialDelayMs);
        }

        try
        {
            await TypeWithSendInputAsync(text, keystrokeDelayMs);
        }
        catch
        {
            // Fallback fuer Apps/Fenster, bei denen SendInput lokal scheitert,
            // obwohl regulare Tastatureingaben funktionieren.
            await TypeWithSendKeysAsync(text, keystrokeDelayMs);
        }
    }

    public async Task PressKeyAsync(Keys key, int keystrokeDelayMs = 0)
    {
        try
        {
            PressWithSendInput(key);
        }
        catch
        {
            SendKeys.SendWait(ToSendKeysToken(key));
        }

        if (keystrokeDelayMs > 0)
        {
            await Task.Delay(keystrokeDelayMs);
        }
    }

    private static async Task TypeWithSendInputAsync(string text, int keystrokeDelayMs)
    {
        foreach (var character in text)
        {
            SendUnicodeKey(character, keyUp: false);
            SendUnicodeKey(character, keyUp: true);

            if (keystrokeDelayMs > 0)
            {
                await Task.Delay(keystrokeDelayMs);
            }
        }
    }

    private static async Task TypeWithSendKeysAsync(string text, int keystrokeDelayMs)
    {
        if (keystrokeDelayMs <= 0)
        {
            SendKeys.SendWait(text);
            return;
        }

        foreach (var character in text)
        {
            SendKeys.SendWait(EscapeForSendKeys(character));
            await Task.Delay(keystrokeDelayMs);
        }
    }

    private static void PressWithSendInput(Keys key)
    {
        SendVirtualKey((ushort)key, keyUp: false);
        SendVirtualKey((ushort)key, keyUp: true);
    }

    private static void SendUnicodeKey(char character, bool keyUp)
    {
        var input = new INPUT
        {
            type = NativeConstants.InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = keyUp
                        ? NativeConstants.KeyeventfUnicode | NativeConstants.KeyeventfKeyup
                        : NativeConstants.KeyeventfUnicode
                }
            }
        };

        Send(input);
    }

    private static void SendVirtualKey(ushort virtualKey, bool keyUp)
    {
        var input = new INPUT
        {
            type = NativeConstants.InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = keyUp ? NativeConstants.KeyeventfKeyup : 0
                }
            }
        };

        Send(input);
    }

    private static void Send(INPUT input)
    {
        var sent = NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (sent == 0)
        {
            var lastError = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(BuildInjectionErrorMessage(lastError));
        }
    }

    private static string ToSendKeysToken(Keys key)
    {
        return key switch
        {
            Keys.Tab => "{TAB}",
            Keys.Enter => "{ENTER}",
            Keys.Space => " ",
            _ => throw new InvalidOperationException($"Kein SendKeys-Fallback fuer Taste {key} vorhanden.")
        };
    }

    private static string EscapeForSendKeys(char character)
    {
        return character switch
        {
            '{' => "{{}",
            '}' => "{}}",
            '+' => "{+}",
            '^' => "{^}",
            '%' => "{%}",
            '~' => "{~}",
            '(' => "{(}",
            ')' => "{)}",
            '[' => "{[}",
            ']' => "{]}",
            _ => character.ToString()
        };
    }

    private static string BuildInjectionErrorMessage(int lastError)
    {
        var message = "Tastatureingabe konnte nicht injiziert werden.";

        if (lastError != 0)
        {
            message += $" Win32-Fehler: {lastError}.";
        }

        message += " Wenn das Zielfenster als Administrator oder mit hoeheren Rechten laeuft, starte PassTypePro ebenfalls als Administrator.";

        if (!IsCurrentProcessElevated())
        {
            message += " PassTypePro laeuft aktuell nicht erhoeht.";
        }

        return message;
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
