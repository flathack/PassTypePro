namespace PassTypePro.Models;

public sealed class AppConfig
{
    public int ClipboardHistoryLimit { get; set; } = 20;
    public int ClipboardPollingIntervalMs { get; set; } = 1200;
    public string TypePrimarySecretHotkey { get; set; } = "Ctrl+Shift+G";
    public string OpenManagerHotkey { get; set; } = "Ctrl+Shift+P";
    public bool AutoStartEnabled { get; set; }
    public bool LockEnabled { get; set; }
    public bool LockOnSessionLock { get; set; } = true;
    public int AutoLockMinutes { get; set; }
    public bool MainWindowTopMost { get; set; }
    public bool TouchModeEnabled { get; set; }
    public string? UnlockPinHash { get; set; }
    public string? UnlockPinSalt { get; set; }
    public string? UnlockPatternHash { get; set; }
    public string? UnlockPatternSalt { get; set; }
}
