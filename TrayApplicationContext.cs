using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;
using PassTypePro.Models;
using PassTypePro.Services;
using PassTypePro.UI;

namespace PassTypePro;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly string _appDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PassTypePro");

    private readonly NotifyIcon _notifyIcon;
    private readonly AppConfigService _configService;
    private readonly SecureSecretStore _secretStore;
    private readonly KeyboardInjectionService _keyboardInjectionService;
    private readonly TotpService _totpService;
    private readonly TypingSequenceService _typingSequenceService;
    private readonly SecretImportExportService _secretImportExportService;
    private readonly KeePassImportService _keePassImportService;
    private readonly AutoStartService _autoStartService;
    private readonly ForegroundWindowService _foregroundWindowService;
    private readonly HiddenHotkeyWindow _hiddenWindow;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly System.Windows.Forms.Timer _clipboardTimer;

    private AppConfig _config;
    private AppLockService _appLockService;
    private ClipboardHistoryService _clipboardHistoryService;
    private List<SecretEntry> _secrets;
    private MainForm? _mainForm;
    private string? _unlockFeedback;

    public TrayApplicationContext()
    {
        _configService = new AppConfigService(_appDataPath);
        _secretStore = new SecureSecretStore(_appDataPath);
        _keyboardInjectionService = new KeyboardInjectionService();
        _totpService = new TotpService();
        _typingSequenceService = new TypingSequenceService(_keyboardInjectionService, _totpService);
        _secretImportExportService = new SecretImportExportService();
        _keePassImportService = new KeePassImportService();
        _autoStartService = new AutoStartService();
        _foregroundWindowService = new ForegroundWindowService();

        _config = _configService.Load();
        _config.AutoStartEnabled = _autoStartService.IsEnabled();
        _appLockService = new AppLockService(_config);
        try
        {
            _secrets = _secretStore.Load().ToList();
        }
        catch (InvalidOperationException ex)
        {
            _secrets = [];
            MessageBox.Show(
                ex.Message,
                "Secret-Datei beschÃ¤digt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        _clipboardHistoryService = new ClipboardHistoryService(_config.ClipboardHistoryLimit);

        _hiddenWindow = new HiddenHotkeyWindow();
        _hotkeyService = new GlobalHotkeyService(_hiddenWindow);
        _hiddenWindow.HotkeyPressed += message => _hotkeyService.TryHandle(message);

        _notifyIcon = new NotifyIcon
        {
            Text = "PassTypePro",
            Icon = LoadApplicationIcon(),
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowManager();

        _clipboardTimer = new System.Windows.Forms.Timer
        {
            Interval = _config.ClipboardPollingIntervalMs
        };
        _clipboardTimer.Tick += (_, _) => OnTimerTick();
        _clipboardTimer.Start();
        SystemEvents.SessionSwitch += OnSessionSwitch;

        RegisterHotkeys();
        RebuildContextMenu();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clipboardTimer.Dispose();
            _hotkeyService.Dispose();
            _hiddenWindow.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _mainForm?.Dispose();
            SystemEvents.SessionSwitch -= OnSessionSwitch;
        }

        base.Dispose(disposing);
    }

    private void OnTimerTick()
    {
        if (_config.AutoLockMinutes > 0 && _appLockService.ShouldAutoLock())
        {
            _appLockService.Lock();
            RefreshUi();
            _notifyIcon.ShowBalloonTip(2000, "PassTypePro", "Die App wurde automatisch gesperrt.", ToolTipIcon.Info);
            return;
        }

        if (_clipboardHistoryService.CaptureCurrentClipboardText())
        {
            RefreshUi();
        }

        _foregroundWindowService.CaptureCurrentExternalWindow();
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (!_config.LockEnabled || !_config.LockOnSessionLock)
        {
            return;
        }

        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            LockApp();
        }
    }

    private void RegisterHotkeys()
    {
        _hotkeyService.UnregisterAll();

        if (HotkeyDefinition.TryParse(_config.TypePrimarySecretHotkey, out var typeSecretHotkey))
        {
            _hotkeyService.Register(typeSecretHotkey, () => _ = TypePrimarySecretAsync());
        }

        if (HotkeyDefinition.TryParse(_config.OpenManagerHotkey, out var openManagerHotkey))
        {
            _hotkeyService.Register(openManagerHotkey, ShowManager);
        }

        foreach (var secret in _secrets)
        {
            if (HotkeyDefinition.TryParse(secret.SecretHotkey, out var secretHotkey))
            {
                _hotkeyService.Register(secretHotkey, () => _ = TypeEntryAsync(secret));
            }
        }
    }

    private async Task TypePrimarySecretAsync()
    {
        var primarySecret = _secrets.FirstOrDefault(secret => secret.IsPrimary) ?? _secrets.FirstOrDefault();
        if (primarySecret is null)
        {
            _notifyIcon.ShowBalloonTip(2500, "PassTypePro", "Es ist noch kein Secret hinterlegt.", ToolTipIcon.Warning);
            return;
        }

        await TypeEntryAsync(primarySecret);
    }

    private async Task TypeEntryAsync(SecretEntry entry)
    {
        if (!EnsureUnlocked(null))
        {
            return;
        }

        try
        {
            _appLockService.Touch();
            await Task.Delay(Math.Max(0, entry.StartDelayMs));
            await _typingSequenceService.TypeEntryAsync(entry);
            MarkSecretAsUsed(entry);
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(2500, "PassTypePro", ex.Message, ToolTipIcon.Error);
        }
    }

    private bool EnsureUnlocked(IWin32Window? owner)
    {
        if (!_appLockService.IsLocked)
        {
            _appLockService.Touch();
            return true;
        }

        _unlockFeedback = "Bitte im Hauptfenster entsperren.";
        ShowManager();
        _mainForm?.ShowUnlockPage();
        return false;
    }

    private void LockApp()
    {
        _appLockService.Lock();
        RefreshUi();
    }

    private void RebuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Manager oeffnen", null, (_, _) => ShowManager());
        menu.Items.Add("Primary Secret tippen", null, (_, _) => _ = TypePrimarySecretAsync());
        menu.Items.Add(_appLockService.IsLocked ? "Entsperren" : "Sperren", null, (_, _) =>
        {
            if (_appLockService.IsLocked)
            {
                EnsureUnlocked(_mainForm);
            }
            else
            {
                LockApp();
            }
        });
        menu.Items.Add(new ToolStripSeparator());

        var secretsMenu = new ToolStripMenuItem("Secrets");
        if (_secrets.Count == 0)
        {
            secretsMenu.DropDownItems.Add("Keine Secrets vorhanden");
        }
        else
        {
            foreach (var secret in _secrets)
            {
                var label = secret.ToString();
                if (!string.IsNullOrWhiteSpace(secret.TotpSeed))
                {
                    label += $" | TOTP {_typingSequenceService.GetTotpPreview(secret)}";
                }

                secretsMenu.DropDownItems.Add(label, null, (_, _) => _ = TypeEntryAsync(secret));
            }
        }

        var clipboardMenu = new ToolStripMenuItem("Clipboard");
        if (_clipboardHistoryService.Entries.Count == 0)
        {
            clipboardMenu.DropDownItems.Add("Noch keine Eintraege");
        }
        else
        {
            foreach (var entry in _clipboardHistoryService.Entries.Take(10))
            {
                clipboardMenu.DropDownItems.Add(Shorten(entry.Content), null, (_, _) => _clipboardHistoryService.CopyToClipboard(entry));
            }

            clipboardMenu.DropDownItems.Add(new ToolStripSeparator());
            clipboardMenu.DropDownItems.Add("Verlauf leeren", null, (_, _) =>
            {
                _clipboardHistoryService.Clear();
                RefreshUi();
            });
        }

        menu.Items.Add(secretsMenu);
        menu.Items.Add(clipboardMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitThread());

        var previous = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = menu;
        previous?.Dispose();
    }

    private void ShowManager()
    {
        _foregroundWindowService.CaptureCurrentExternalWindow();

        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm();
            _mainForm.TotpPreviewProvider = secret => _typingSequenceService.GetTotpPreview(secret);
            _mainForm.FormClosed += (_, _) => _mainForm = null;
            _mainForm.AddSecretRequested += (_, _) => AddSecret();
            _mainForm.SetPrimarySecretRequested += secret => SetPrimarySecret(secret);
            _mainForm.ToggleFavoriteRequested += secret => ToggleFavorite(secret);
            _mainForm.EditSecretRequested += secret => EditSecret(secret);
            _mainForm.DeleteSecretRequested += secret => DeleteSecret(secret);
            _mainForm.DeleteSecretsRequested += secrets => DeleteSecrets(secrets);
            _mainForm.TypeSecretRequested += secret => _ = TypeEntryAsync(secret);
            _mainForm.ClipboardReuseRequested += entry => ReuseClipboardEntry(entry);
            _mainForm.SaveSettingsRequested += (_, _) => SaveSettingsFromForm();
            _mainForm.ExportSecretsRequested += (_, _) => ExportSecrets();
            _mainForm.ImportSecretsRequested += (_, _) => ImportSecrets();
            _mainForm.ImportKeePassRequested += (_, _) => ImportKeePass();
            _mainForm.UnlockRequested += (_, _) => _mainForm.ShowUnlockPage();
            _mainForm.UnlockSubmitRequested += (_, _) => SubmitUnlockFromMainForm();
            _mainForm.LockRequested += (_, _) => LockApp();
            _mainForm.ExitRequested += (_, _) => ExitThread();
            _mainForm.SetUnlockPatternRequested += (_, _) => SetUnlockPattern();
            _mainForm.InsertSecretFieldRequested += (secret, field) => _ = InsertSecretFieldAsync(secret, field);
        }

        RefreshMainForm();
        _mainForm.Show();
        _mainForm.BringToFront();
        _mainForm.Activate();
        if (_appLockService.IsLocked)
        {
            _mainForm.ShowUnlockPage();
        }
    }

    private void AddSecret()
    {
        var createdSecret = ShowSecretDialog();
        if (createdSecret is null)
        {
            return;
        }

        var validationError = ValidateSecretHotkeys(createdSecret, createdSecret.Id);
        if (validationError is not null)
        {
            MessageBox.Show(_mainForm, validationError, "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ApplyPrimarySecretSelection(createdSecret);
        _secrets.Add(createdSecret);
        PersistSecrets();
    }

    private void EditSecret(SecretEntry secret)
    {
        var editedSecret = ShowSecretDialog(secret);
        if (editedSecret is null)
        {
            return;
        }

        var validationError = ValidateSecretHotkeys(editedSecret, editedSecret.Id);
        if (validationError is not null)
        {
            MessageBox.Show(_mainForm, validationError, "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = _secrets.First(entry => entry.Id == editedSecret.Id);
        existing.Name = editedSecret.Name;
        existing.GroupPath = editedSecret.GroupPath;
        existing.Username = editedSecret.Username;
        existing.Url = editedSecret.Url;
        existing.Value = editedSecret.Value;
        existing.TotpSeed = editedSecret.TotpSeed;
        existing.Notes = editedSecret.Notes;
        existing.Source = editedSecret.Source;
        existing.SequenceTemplate = editedSecret.SequenceTemplate;
        existing.SecretHotkey = editedSecret.SecretHotkey;
        existing.StartDelayMs = editedSecret.StartDelayMs;
        existing.KeystrokeDelayMs = editedSecret.KeystrokeDelayMs;
        existing.IsPrimary = editedSecret.IsPrimary;

        ApplyPrimarySecretSelection(existing);
        PersistSecrets();
    }

    private SecretEntry? ShowSecretDialog(SecretEntry? existing = null)
    {
        try
        {
            using var dialog = new SecretEditForm(existing)
            {
                TopMost = _config.MainWindowTopMost
            };

            var result = _mainForm is not null && _mainForm.Visible
                ? dialog.ShowDialog(_mainForm)
                : dialog.ShowDialog();

            return result == DialogResult.OK ? dialog.Secret : null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _mainForm,
                $"Der Secret-Dialog konnte nicht geoeffnet werden.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Dialogfehler",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return null;
        }
    }

    private void SetPrimarySecret(SecretEntry secret)
    {
        var existing = _secrets.First(entry => entry.Id == secret.Id);
        existing.IsPrimary = true;
        ApplyPrimarySecretSelection(existing);
        PersistSecrets();
    }

    private void ToggleFavorite(SecretEntry secret)
    {
        var existing = _secrets.First(entry => entry.Id == secret.Id);
        existing.IsFavorite = !existing.IsFavorite;
        PersistSecrets();
    }

    private void DeleteSecret(SecretEntry secret)
    {
        DeleteSecrets([secret]);
    }

    private void DeleteSecrets(IReadOnlyList<SecretEntry> secrets)
    {
        if (secrets.Count == 0)
        {
            return;
        }

        var message = secrets.Count == 1
            ? "Secret wirklich loeschen?"
            : $"{secrets.Count} Secrets wirklich loeschen?";

        var result = MessageBox.Show(
            _mainForm,
            message,
            "PassTypePro",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        var ids = secrets.Select(secret => secret.Id).ToHashSet();
        _secrets.RemoveAll(entry => ids.Contains(entry.Id));
        PersistSecrets();
    }

    private void ReuseClipboardEntry(ClipboardEntry entry)
    {
        var original = _clipboardHistoryService.Entries.FirstOrDefault(current => current.CapturedAt == entry.CapturedAt)
            ?? entry;
        _clipboardHistoryService.CopyToClipboard(original);
    }

    private void SaveSettingsFromForm()
    {
        if (_mainForm is null)
        {
            return;
        }

        var update = _mainForm.BuildSettingsUpdate();
        if (!HotkeyDefinition.TryParse(update.Config.TypePrimarySecretHotkey, out _) ||
            !HotkeyDefinition.TryParse(update.Config.OpenManagerHotkey, out _))
        {
            MessageBox.Show(_mainForm, "Mindestens ein Hotkey ist ungueltig.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.Equals(update.Config.TypePrimarySecretHotkey, update.Config.OpenManagerHotkey, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(_mainForm, "Primary-Hotkey und Manager-Hotkey duerfen nicht identisch sein.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var reservedHotkeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            update.Config.TypePrimarySecretHotkey,
            update.Config.OpenManagerHotkey
        };

        foreach (var secret in _secrets)
        {
            if (string.IsNullOrWhiteSpace(secret.SecretHotkey))
            {
                continue;
            }

            if (!reservedHotkeys.Add(secret.SecretHotkey))
            {
                MessageBox.Show(_mainForm, $"Der Secret-Hotkey '{secret.SecretHotkey}' ist doppelt vergeben.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        update.Config.ClipboardPollingIntervalMs = _config.ClipboardPollingIntervalMs;
        update.Config.UnlockPinHash = _config.UnlockPinHash;
        update.Config.UnlockPinSalt = _config.UnlockPinSalt;
        update.Config.UnlockPatternHash = _config.UnlockPatternHash;
        update.Config.UnlockPatternSalt = _config.UnlockPatternSalt;

        if (!update.Config.LockEnabled)
        {
            AppLockService.ClearPin(update.Config);
        }
        else if (!string.IsNullOrWhiteSpace(update.NewPin))
        {
            AppLockService.SetPin(update.Config, update.NewPin);
        }
        else if ((string.IsNullOrWhiteSpace(update.Config.UnlockPinHash) || string.IsNullOrWhiteSpace(update.Config.UnlockPinSalt)) &&
                 (string.IsNullOrWhiteSpace(update.Config.UnlockPatternHash) || string.IsNullOrWhiteSpace(update.Config.UnlockPatternSalt)))
        {
            MessageBox.Show(_mainForm, "Fuer den App-Lock wird eine PIN oder ein Entsperrmuster benoetigt.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _autoStartService.SetEnabled(update.Config.AutoStartEnabled, Application.ExecutablePath);

        _config = update.Config;
        _configService.Save(_config);
        _clipboardHistoryService = new ClipboardHistoryService(_config.ClipboardHistoryLimit);
        _clipboardTimer.Interval = _config.ClipboardPollingIntervalMs;
        _appLockService.ApplyConfig(_config, keepSessionState: true);
        if (!string.IsNullOrWhiteSpace(update.NewPin) && _config.LockEnabled)
        {
            _appLockService.TryUnlock(update.NewPin);
        }

        RegisterHotkeys();
        RefreshUi();

        MessageBox.Show(_mainForm, "Einstellungen gespeichert.", "PassTypePro", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PersistSecrets()
    {
        _secretStore.Save(_secrets);
        RefreshUi();
    }

    private void SetUnlockPattern()
    {
        if (_mainForm is null)
        {
            return;
        }

        using var dialog = new PatternPromptForm("Entsperrmuster setzen", confirmRequired: true);
        if (dialog.ShowDialog(_mainForm) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Pattern))
        {
            return;
        }

        AppLockService.SetPattern(_config, dialog.Pattern);
        _config.LockEnabled = true;
        _configService.Save(_config);
        _appLockService.ApplyConfig(_config, keepSessionState: true);
        RefreshUi();
        MessageBox.Show(_mainForm, "Entsperrmuster gespeichert.", "PassTypePro", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task InsertSecretFieldAsync(SecretEntry secret, SecretFieldKind fieldKind)
    {
        if (!EnsureUnlocked(_mainForm))
        {
            return;
        }

        var restored = await _foregroundWindowService.TryRestoreLastExternalWindowAsync(Math.Max(140, secret.StartDelayMs));
        if (!restored)
        {
            _notifyIcon.ShowBalloonTip(2000, "PassTypePro", "Kein zuletzt fokussiertes Zielfenster gefunden.", ToolTipIcon.Warning);
            return;
        }

        _appLockService.Touch();

        switch (fieldKind)
        {
            case SecretFieldKind.Username:
                if (!string.IsNullOrWhiteSpace(secret.Username))
                {
                    await _keyboardInjectionService.TypeTextAsync(secret.Username, 0, secret.KeystrokeDelayMs);
                    MarkSecretAsUsed(secret);
                }

                break;
            case SecretFieldKind.Secret:
                await _keyboardInjectionService.TypeTextAsync(secret.Value, 0, secret.KeystrokeDelayMs);
                MarkSecretAsUsed(secret);
                break;
            case SecretFieldKind.Totp:
                if (!string.IsNullOrWhiteSpace(secret.TotpSeed))
                {
                    var code = _totpService.GenerateCode(secret.TotpSeed);
                    await _keyboardInjectionService.TypeTextAsync(code, 0, secret.KeystrokeDelayMs);
                    MarkSecretAsUsed(secret);
                }

                break;
        }
    }

    private void MarkSecretAsUsed(SecretEntry secret)
    {
        var existing = _secrets.FirstOrDefault(entry => entry.Id == secret.Id);
        if (existing is null)
        {
            return;
        }

        existing.LastUsedAt = DateTimeOffset.Now;
        PersistSecrets();
    }

    private void ExportSecrets()
    {
        if (_mainForm is null || !EnsureUnlocked(_mainForm))
        {
            return;
        }

        using var saveDialog = new SaveFileDialog
        {
            Filter = "PassTypePro Export (*.ptpsec)|*.ptpsec|Alle Dateien (*.*)|*.*",
            FileName = $"PassTypePro-Secrets-{DateTime.Now:yyyyMMdd-HHmmss}.ptpsec"
        };

        if (saveDialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            return;
        }

        using var passphraseDialog = new PassphrasePromptForm("Secrets exportieren", "Export-Passphrase", confirmRequired: true);
        if (passphraseDialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _secretImportExportService.Export(saveDialog.FileName, _secrets, passphraseDialog.Passphrase);
            MessageBox.Show(_mainForm, "Export erfolgreich abgeschlossen.", "PassTypePro", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(_mainForm, ex.Message, "Exportfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportSecrets()
    {
        if (_mainForm is null || !EnsureUnlocked(_mainForm))
        {
            return;
        }

        using var openDialog = new OpenFileDialog
        {
            Filter = "PassTypePro Export (*.ptpsec)|*.ptpsec|Alle Dateien (*.*)|*.*"
        };

        if (openDialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            return;
        }

        using var passphraseDialog = new PassphrasePromptForm("Secrets importieren", "Import-Passphrase", confirmRequired: false);
        if (passphraseDialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var importedSecrets = _secretImportExportService.Import(openDialog.FileName, passphraseDialog.Passphrase).ToList();

            foreach (var imported in importedSecrets)
            {
                imported.Id = Guid.NewGuid();
            }

            if (importedSecrets.Any(secret => secret.IsPrimary))
            {
                foreach (var secret in _secrets)
                {
                    secret.IsPrimary = false;
                }
            }

            _secrets.AddRange(importedSecrets);
            PersistSecrets();
            MessageBox.Show(_mainForm, $"{importedSecrets.Count} Secrets importiert.", "PassTypePro", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (CryptographicException)
        {
            MessageBox.Show(_mainForm, "Import fehlgeschlagen. Die Passphrase ist falsch oder die Datei ist beschaedigt.", "Importfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(_mainForm, ex.Message, "Importfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyPrimarySecretSelection(SecretEntry selected)
    {
        if (!selected.IsPrimary)
        {
            return;
        }

        foreach (var secret in _secrets.Where(secret => secret.Id != selected.Id))
        {
            secret.IsPrimary = false;
        }
    }

    private string? ValidateSecretHotkeys(SecretEntry candidate, Guid candidateId)
    {
        if (string.IsNullOrWhiteSpace(candidate.SecretHotkey))
        {
            return null;
        }

        if (!HotkeyDefinition.TryParse(candidate.SecretHotkey, out _))
        {
            return "Der Secret-Hotkey ist ungueltig.";
        }

        if (string.Equals(candidate.SecretHotkey, _config.TypePrimarySecretHotkey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.SecretHotkey, _config.OpenManagerHotkey, StringComparison.OrdinalIgnoreCase))
        {
            return "Der Secret-Hotkey kollidiert mit einem App-Hotkey.";
        }

        var duplicate = _secrets.FirstOrDefault(secret =>
            secret.Id != candidateId &&
            string.Equals(secret.SecretHotkey, candidate.SecretHotkey, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
        {
            return $"Der Hotkey wird bereits von '{duplicate.Name}' verwendet.";
        }

        return null;
    }

    private void RefreshUi()
    {
        RebuildContextMenu();
        RefreshMainForm();
    }

    private void ImportKeePass()
    {
        if (_mainForm is null || !EnsureUnlocked(_mainForm))
        {
            return;
        }

        using var openDialog = new OpenFileDialog
        {
            Filter = "KeePass XML (*.xml)|*.xml|Alle Dateien (*.*)|*.*",
            Title = "KeePass XML importieren"
        };

        if (openDialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var importedSecrets = _keePassImportService.ImportXml(openDialog.FileName).ToList();
            foreach (var imported in importedSecrets)
            {
                imported.Id = Guid.NewGuid();
            }

            _secrets.AddRange(importedSecrets);
            PersistSecrets();

            MessageBox.Show(
                _mainForm,
                $"{importedSecrets.Count} Eintraege aus KeePass importiert.",
                "KeePass-Import",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _mainForm,
                ex.Message,
                "KeePass-Importfehler",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RefreshMainForm()
    {
        _mainForm?.Bind(
            _config,
            _secrets,
            _clipboardHistoryService.Entries,
            _appLockService.IsLocked,
            _appLockService.HasPin,
            _appLockService.HasPattern,
            _unlockFeedback);
    }

    private void SubmitUnlockFromMainForm()
    {
        if (_mainForm is null)
        {
            return;
        }

        var pattern = _mainForm.EnteredUnlockPattern;
        var pin = _mainForm.EnteredUnlockPin;

        var success = false;
        if (_appLockService.HasPattern && !string.IsNullOrWhiteSpace(pattern))
        {
            success = _appLockService.TryUnlockPattern(pattern);
        }

        if (!success && _appLockService.HasPin && !string.IsNullOrWhiteSpace(pin))
        {
            success = _appLockService.TryUnlock(pin);
        }

        if (!success)
        {
            if (_appLockService.FailedAttempts >= AppLockService.MaxFailedAttempts)
            {
                ResetApplicationAfterTooManyFailures();
                return;
            }

            _unlockFeedback = $"Entsperren fehlgeschlagen. Noch {_appLockService.RemainingAttempts} Versuche.";
            RefreshMainForm();
            _mainForm.ShowUnlockPage();
            return;
        }

        _unlockFeedback = null;
        _appLockService.ResetFailedAttempts();
        _mainForm.ClearUnlockInput();
        RefreshUi();
        _mainForm.ShowSecretsPage();
    }

    private void ResetApplicationAfterTooManyFailures()
    {
        _clipboardTimer.Stop();
        _clipboardHistoryService.Clear();
        _secrets.Clear();
        _appLockService.ResetFailedAttempts();
        _configService.Reset();
        _secretStore.Reset();
        _autoStartService.SetEnabled(false, Application.ExecutablePath);

        MessageBox.Show(
            _mainForm,
            "Nach 10 Fehlversuchen wurde PassTypePro aus Sicherheitsgründen zurückgesetzt und wird jetzt beendet.",
            "Sicherheitsreset",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        ExitThread();
    }

    private static string Shorten(string value)
    {
        var normalized = value.Replace(Environment.NewLine, " ");
        return normalized.Length > 40 ? normalized[..40] + "..." : normalized;
    }

    private static Icon LoadApplicationIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "passtypepro-lock-dark.ico");
        return File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Shield;
    }

    private sealed class HiddenHotkeyWindow : NativeWindow, IDisposable
    {
        public event Action<Message>? HotkeyPressed;

        public HiddenHotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            HotkeyPressed?.Invoke(m);
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
