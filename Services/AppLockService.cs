using System.Security.Cryptography;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed class AppLockService
{
    public const int MaxFailedAttempts = 10;

    private AppConfig _config;
    private DateTimeOffset _lastActivity = DateTimeOffset.UtcNow;

    public AppLockService(AppConfig config)
    {
        _config = config;
        IsUnlocked = !IsEnabled;
    }

    public bool IsEnabled =>
        _config.LockEnabled &&
        (HasPin || HasPattern);

    public bool HasPin =>
        !string.IsNullOrWhiteSpace(_config.UnlockPinHash) &&
        !string.IsNullOrWhiteSpace(_config.UnlockPinSalt);

    public bool HasPattern =>
        !string.IsNullOrWhiteSpace(_config.UnlockPatternHash) &&
        !string.IsNullOrWhiteSpace(_config.UnlockPatternSalt);

    public bool IsUnlocked { get; private set; }

    public bool IsLocked => IsEnabled && !IsUnlocked;

    public int FailedAttempts { get; private set; }

    public int RemainingAttempts => Math.Max(0, MaxFailedAttempts - FailedAttempts);

    public void ApplyConfig(AppConfig config, bool keepSessionState = true)
    {
        var wasUnlocked = IsUnlocked;
        _config = config;
        IsUnlocked = !IsEnabled || (keepSessionState && wasUnlocked);
        Touch();
    }

    public void Touch()
    {
        _lastActivity = DateTimeOffset.UtcNow;
    }

    public void Lock()
    {
        if (IsEnabled)
        {
            IsUnlocked = false;
        }
    }

    public bool TryUnlock(string pin)
    {
        if (!IsEnabled || !HasPin)
        {
            return !IsEnabled;
        }

        if (string.IsNullOrEmpty(pin))
        {
            return false;
        }

        var expectedHash = Convert.FromBase64String(_config.UnlockPinHash!);
        var salt = Convert.FromBase64String(_config.UnlockPinSalt!);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, 100_000, HashAlgorithmName.SHA256, 32);

        var success = CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        if (success)
        {
            IsUnlocked = true;
            FailedAttempts = 0;
            Touch();
        }
        else
        {
            FailedAttempts++;
        }

        return success;
    }

    public bool TryUnlockPattern(string pattern)
    {
        if (!IsEnabled || !HasPattern)
        {
            return !IsEnabled;
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var expectedHash = Convert.FromBase64String(_config.UnlockPatternHash!);
        var salt = Convert.FromBase64String(_config.UnlockPatternSalt!);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(pattern, salt, 100_000, HashAlgorithmName.SHA256, 32);

        var success = CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        if (success)
        {
            IsUnlocked = true;
            FailedAttempts = 0;
            Touch();
        }
        else
        {
            FailedAttempts++;
        }

        return success;
    }

    public bool ShouldAutoLock()
    {
        if (!IsEnabled || !IsUnlocked || _config.AutoLockMinutes <= 0)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - _lastActivity >= TimeSpan.FromMinutes(_config.AutoLockMinutes);
    }

    public static void SetPin(AppConfig config, string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, 100_000, HashAlgorithmName.SHA256, 32);

        config.UnlockPinSalt = Convert.ToBase64String(salt);
        config.UnlockPinHash = Convert.ToBase64String(hash);
    }

    public static void ClearPin(AppConfig config)
    {
        config.UnlockPinSalt = null;
        config.UnlockPinHash = null;
        config.UnlockPatternSalt = null;
        config.UnlockPatternHash = null;
        config.LockEnabled = false;
    }

    public static void SetPattern(AppConfig config, string pattern)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pattern, salt, 100_000, HashAlgorithmName.SHA256, 32);

        config.UnlockPatternSalt = Convert.ToBase64String(salt);
        config.UnlockPatternHash = Convert.ToBase64String(hash);
    }

    public void ResetFailedAttempts()
    {
        FailedAttempts = 0;
    }
}
