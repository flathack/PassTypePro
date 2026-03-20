using System.Text.RegularExpressions;
using System.Windows.Forms;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed partial class TypingSequenceService
{
    private readonly KeyboardInjectionService _keyboardInjectionService;
    private readonly TotpService _totpService;

    public TypingSequenceService(KeyboardInjectionService keyboardInjectionService, TotpService totpService)
    {
        _keyboardInjectionService = keyboardInjectionService;
        _totpService = totpService;
    }

    public async Task TypeEntryAsync(SecretEntry entry)
    {
        var template = string.IsNullOrWhiteSpace(entry.SequenceTemplate)
            ? "{SECRET}"
            : entry.SequenceTemplate;

        var matches = TokenRegex().Matches(template);
        var index = 0;

        foreach (Match match in matches)
        {
            if (match.Index > index)
            {
                var text = template[index..match.Index];
                await _keyboardInjectionService.TypeTextAsync(text, initialDelayMs: 0, keystrokeDelayMs: entry.KeystrokeDelayMs);
            }

            await ExecuteTokenAsync(match.Groups["token"].Value, match.Groups["argument"].Value, entry);
            index = match.Index + match.Length;
        }

        if (index < template.Length)
        {
            await _keyboardInjectionService.TypeTextAsync(template[index..], initialDelayMs: 0, keystrokeDelayMs: entry.KeystrokeDelayMs);
        }
    }

    public string GetTotpPreview(SecretEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.TotpSeed))
        {
            return "Kein TOTP konfiguriert";
        }

        var code = _totpService.GenerateCode(entry.TotpSeed);
        var remaining = _totpService.GetRemainingSeconds();
        return $"{code} ({remaining}s)";
    }

    private async Task ExecuteTokenAsync(string token, string argument, SecretEntry entry)
    {
        switch (token.ToUpperInvariant())
        {
            case "USERNAME":
                await _keyboardInjectionService.TypeTextAsync(entry.Username, initialDelayMs: 0, keystrokeDelayMs: entry.KeystrokeDelayMs);
                break;
            case "SECRET":
                await _keyboardInjectionService.TypeTextAsync(entry.Value, initialDelayMs: 0, keystrokeDelayMs: entry.KeystrokeDelayMs);
                break;
            case "TOTP":
                var code = _totpService.GenerateCode(entry.TotpSeed);
                await _keyboardInjectionService.TypeTextAsync(code, initialDelayMs: 0, keystrokeDelayMs: entry.KeystrokeDelayMs);
                break;
            case "TEXT":
                await _keyboardInjectionService.TypeTextAsync(argument, initialDelayMs: 0, keystrokeDelayMs: entry.KeystrokeDelayMs);
                break;
            case "TAB":
                await _keyboardInjectionService.PressKeyAsync(Keys.Tab, entry.KeystrokeDelayMs);
                break;
            case "ENTER":
                await _keyboardInjectionService.PressKeyAsync(Keys.Enter, entry.KeystrokeDelayMs);
                break;
            case "SPACE":
                await _keyboardInjectionService.PressKeyAsync(Keys.Space, entry.KeystrokeDelayMs);
                break;
            case "DELAY":
                if (int.TryParse(argument, out var delayMs) && delayMs > 0)
                {
                    await Task.Delay(delayMs);
                }

                break;
        }
    }

    [GeneratedRegex(@"\{(?<token>[A-Z]+)(:(?<argument>[^}]+))?\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
}
