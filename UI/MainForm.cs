using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PassTypePro.Models;

namespace PassTypePro.UI;

public sealed class MainForm : Form
{
    private readonly Color _backgroundColor = Color.FromArgb(20, 24, 28);
    private readonly Color _surfaceColor = Color.FromArgb(29, 35, 41);
    private readonly Color _panelColor = Color.FromArgb(35, 42, 50);
    private readonly Color _textColor = Color.FromArgb(231, 237, 243);
    private readonly Color _mutedTextColor = Color.FromArgb(158, 170, 182);
    private readonly Color _accentColor = Color.FromArgb(88, 166, 255);
    private readonly Color _selectionColor = Color.FromArgb(44, 71, 107);

    private readonly HotkeyTextBox _primaryHotkeyTextBox = new() { Width = 180 };
    private readonly HotkeyTextBox _managerHotkeyTextBox = new() { Width = 180 };
    private readonly NumericUpDown _historyLimitNumeric = new() { Minimum = 1, Maximum = 100, Width = 80 };
    private readonly CheckBox _autoStartCheckBox = new() { Text = "Mit Windows starten", AutoSize = true };
    private readonly CheckBox _lockEnabledCheckBox = new() { Text = "App-Lock aktivieren", AutoSize = true };
    private readonly CheckBox _lockOnSessionLockCheckBox = new() { Text = "Bei Windows-Sperre sperren", AutoSize = true };
    private readonly CheckBox _topMostCheckBox = new() { Text = "Fenster immer im Vordergrund", AutoSize = true };
    private readonly CheckBox _touchModeCheckBox = new() { Text = "Touch-Mode", AutoSize = true };
    private readonly NumericUpDown _autoLockMinutesNumeric = new() { Minimum = 0, Maximum = 240, Width = 80 };
    private readonly TextBox _newPinTextBox = new() { Width = 180, UseSystemPasswordChar = true };
    private readonly TextBox _secretSearchTextBox = new() { Width = 260 };
    private readonly DataGridView _secretGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, MultiSelect = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoGenerateColumns = false, BackgroundColor = Color.FromArgb(29, 35, 41), BorderStyle = BorderStyle.None };
    private readonly DataGridView _clipboardGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoGenerateColumns = false, BackgroundColor = Color.FromArgb(29, 35, 41), BorderStyle = BorderStyle.None };
    private readonly Label _lockStatusLabel = new() { AutoSize = true };
    private readonly Label _totpPreviewLabel = new() { AutoSize = true };
    private readonly Label _secretCountLabel = new() { AutoSize = true };
    private readonly Button _unlockButton = new() { Width = 42, Height = 34 };
    private readonly Button _lockButton = new() { Width = 42, Height = 34 };
    private readonly Button _topMostButton = new() { Width = 42, Height = 34 };
    private readonly Button _dockToggleButton = new() { Width = 42, Height = 34 };
    private readonly Button _generalTabButton = new() { Text = "Allgemein", Width = 94, Height = 36 };
    private readonly Button _secretsTabButton = new() { Text = "Secrets", Width = 82, Height = 36 };
    private readonly Button _clipboardTabButton = new() { Text = "Clipboard", Width = 90, Height = 36 };
    private readonly Button _unlockTabButton = new() { Text = "Entsperren", Width = 104, Height = 36, Visible = false };
    private readonly Button _aboutButton = new() { Text = "?", Width = 34, Height = 34 };
    private readonly Button _insertUsernameButton = new() { Text = "USERNAME", Width = 126, Height = 78 };
    private readonly Button _insertSecretButton = new() { Text = "SECRET", Width = 126, Height = 78 };
    private readonly Button _insertTotpButton = new() { Text = "TOTP", Width = 126, Height = 78 };
    private readonly Button _insertSequenceButton = new() { Text = "SEQ", Width = 126, Height = 78 };
    private readonly Button _saveSettingsButton = new() { Width = 42, Height = 34 };
    private readonly ToolTip _toolTip = new();
    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill };
    private readonly Panel _generalPage = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Panel _secretsPage = new() { Dock = DockStyle.Fill };
    private readonly Panel _clipboardPage = new() { Dock = DockStyle.Fill };
    private readonly Panel _unlockPage = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Panel _dockPage = new() { Dock = DockStyle.Fill };
    private readonly ListBox _dockSecretList = new() { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
    private readonly ListBox _dockClipboardList = new() { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _dockSearchTextBox = new() { Dock = DockStyle.Fill };
    private readonly Button _dockUsernameButton = new() { Text = "USR", Dock = DockStyle.Fill };
    private readonly Button _dockSecretButton = new() { Text = "PW", Dock = DockStyle.Fill };
    private readonly Button _dockTotpButton = new() { Text = "TOTP", Dock = DockStyle.Fill };
    private readonly Button _dockTypeButton = new() { Text = "SEQ", Dock = DockStyle.Fill };
    private readonly Button _dockClipboardButton = new() { Text = "CLIP", Dock = DockStyle.Fill };
    private readonly Label _dockSecretLabel = new() { Text = "Secrets", AutoSize = true };
    private readonly Label _dockClipboardLabel = new() { Text = "Clips", AutoSize = true };
    private readonly TextBox _unlockPinTextBox = new() { Width = 220, UseSystemPasswordChar = true, PlaceholderText = "PIN" };
    private readonly PatternCanvas _unlockPatternCanvas = new() { Size = new Size(280, 280) };
    private readonly Label _unlockFeedbackLabel = new() { AutoSize = true };
    private readonly Button _unlockSubmitButton = new() { Text = "Entsperren", AutoSize = true };
    private readonly Button _unlockResetPatternButton = new() { Text = "Muster zurücksetzen", AutoSize = true };

    private List<SecretEntry> _allSecrets = [];
    private List<SecretEntry> _currentSecrets = [];
    private List<ClipboardEntry> _currentClipboardEntries = [];
    private List<SecretEntry> _filteredDockSecrets = [];
    private List<ClipboardEntry> _filteredDockClipboardEntries = [];
    private bool _isLocked;
    private bool _supportsPin;
    private bool _supportsPattern;
    private bool _isDocked;
    private Rectangle _normalBounds;
    private bool _restoreTopMostAfterDock;
    private readonly TableLayoutPanel _topPanel;
    private readonly FlowLayoutPanel _tabsPanel;

    public event EventHandler? AddSecretRequested;
    public event Action<SecretEntry>? SetPrimarySecretRequested;
    public event Action<SecretEntry>? EditSecretRequested;
    public event Action<SecretEntry>? DeleteSecretRequested;
    public event Action<IReadOnlyList<SecretEntry>>? DeleteSecretsRequested;
    public event Action<SecretEntry>? TypeSecretRequested;
    public event Action<ClipboardEntry>? ClipboardReuseRequested;
    public event EventHandler? SaveSettingsRequested;
    public event EventHandler? ExportSecretsRequested;
    public event EventHandler? ImportSecretsRequested;
    public event EventHandler? ImportKeePassRequested;
    public event EventHandler? UnlockRequested;
    public event EventHandler? UnlockSubmitRequested;
    public event EventHandler? LockRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? SetUnlockPatternRequested;
    public event Action<SecretEntry, SecretFieldKind>? InsertSecretFieldRequested;

    public MainForm()
    {
        Text = "PassTypePro v0.2.2";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(920, 820);
        Padding = new Padding(1);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "passtypepro-lock-dark.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }

        _unlockButton.Click += (_, _) => UnlockRequested?.Invoke(this, EventArgs.Empty);
        _lockButton.Click += (_, _) => LockRequested?.Invoke(this, EventArgs.Empty);

        _generalTabButton.Click += (_, _) => ShowPage(_generalPage);
        _secretsTabButton.Click += (_, _) => ShowPage(_secretsPage);
        _clipboardTabButton.Click += (_, _) => ShowPage(_clipboardPage);
        _unlockTabButton.Click += (_, _) => ShowPage(_unlockPage);
        _aboutButton.Click += (_, _) => ShowAboutDialog();
        _topMostButton.Click += (_, _) => ToggleTopMost();
        _dockToggleButton.Click += (_, _) => ToggleDockMode();
        _saveSettingsButton.Click += (_, _) => SaveSettingsRequested?.Invoke(this, EventArgs.Empty);
        _insertUsernameButton.Click += (_, _) => TriggerInsertField(SecretFieldKind.Username);
        _insertSecretButton.Click += (_, _) => TriggerInsertField(SecretFieldKind.Secret);
        _insertTotpButton.Click += (_, _) => TriggerInsertField(SecretFieldKind.Totp);
        _insertSequenceButton.Click += (_, _) =>
        {
            var secret = GetSelectedSecret();
            if (secret is not null)
            {
                TypeSecretRequested?.Invoke(secret);
            }
        };
        _unlockSubmitButton.Click += (_, _) => UnlockSubmitRequested?.Invoke(this, EventArgs.Empty);
        _secretSearchTextBox.TextChanged += (_, _) => ApplySecretFilter(GetSelectedSecret()?.Id);
        _unlockPinTextBox.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                UnlockSubmitRequested?.Invoke(this, EventArgs.Empty);
                args.SuppressKeyPress = true;
            }
        };
        _dockSecretList.Format += (_, args) =>
        {
            if (args.ListItem is SecretEntry secret)
            {
                args.Value = secret.IsPrimary ? $"* {secret.Name}" : secret.Name;
            }
        };
        _dockClipboardList.Format += (_, args) =>
        {
            if (args.ListItem is ClipboardEntry entry)
            {
                args.Value = Shorten(entry.Content);
            }
        };
        _dockUsernameButton.Click += (_, _) => TriggerDockInsert(SecretFieldKind.Username);
        _dockSecretButton.Click += (_, _) => TriggerDockInsert(SecretFieldKind.Secret);
        _dockTotpButton.Click += (_, _) => TriggerDockInsert(SecretFieldKind.Totp);
        _dockTypeButton.Click += (_, _) =>
        {
            var secret = GetSelectedDockSecret();
            if (secret is not null)
            {
                TypeSecretRequested?.Invoke(secret);
            }
        };
        _dockClipboardButton.Click += (_, _) =>
        {
            var entry = GetSelectedDockClipboardEntry();
            if (entry is not null)
            {
                ClipboardReuseRequested?.Invoke(entry);
            }
        };
        _dockSearchTextBox.TextChanged += (_, _) => ApplyDockFilter(GetSelectedDockSecret()?.Id, GetSelectedDockClipboardEntry()?.CapturedAt);
        _secretGrid.CellDoubleClick += (_, _) =>
        {
            var selectedSecrets = GetSelectedSecrets();
            if (selectedSecrets.Count == 1)
            {
                EditSecretRequested?.Invoke(selectedSecrets[0]);
            }
        };

        MainMenuStrip = BuildMenuStrip();
        ConfigureGrids();
        BuildPages();

        var lockPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            BackColor = _backgroundColor,
            Margin = Padding.Empty
        };
        lockPanel.Controls.Add(_lockStatusLabel);
        lockPanel.Controls.Add(_unlockButton);
        lockPanel.Controls.Add(_lockButton);
        lockPanel.Controls.Add(_topMostButton);
        lockPanel.Controls.Add(_dockToggleButton);
        lockPanel.Controls.Add(_saveSettingsButton);

        _topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(12, 12, 12, 0),
            BackColor = _backgroundColor
        };
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _topPanel.Controls.Add(lockPanel, 0, 0);
        _topPanel.Controls.Add(_aboutButton, 1, 0);

        _tabsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 62,
            WrapContents = false,
            Padding = new Padding(12, 10, 12, 14),
            BackColor = _backgroundColor
        };
        _tabsPanel.Controls.Add(_generalTabButton);
        _tabsPanel.Controls.Add(_secretsTabButton);
        _tabsPanel.Controls.Add(_clipboardTabButton);
        _tabsPanel.Controls.Add(_unlockTabButton);

        _contentHost.BackColor = _backgroundColor;
        _contentHost.Padding = new Padding(12);

        Controls.Add(_contentHost);
        Controls.Add(_tabsPanel);
        Controls.Add(_topPanel);
        Controls.Add(MainMenuStrip);

        ShowPage(_generalPage);
        ApplyDarkTheme(this);
        ConfigureInsertButtons();
        ConfigureStatusButtons();
        UpdateTabLayout();
    }

    public Func<SecretEntry, string>? TotpPreviewProvider { get; set; }

    public void Bind(
        AppConfig config,
        IReadOnlyList<SecretEntry> secrets,
        IReadOnlyList<ClipboardEntry> clipboardEntries,
        bool isLocked,
        bool supportsPin,
        bool supportsPattern,
        string? unlockFeedback = null)
    {
        _isLocked = isLocked;
        _supportsPin = supportsPin;
        _supportsPattern = supportsPattern;
        _primaryHotkeyTextBox.SetHotkey(config.TypePrimarySecretHotkey);
        _managerHotkeyTextBox.SetHotkey(config.OpenManagerHotkey);
        _historyLimitNumeric.Value = config.ClipboardHistoryLimit;
        _autoStartCheckBox.Checked = config.AutoStartEnabled;
        _lockEnabledCheckBox.Checked = config.LockEnabled;
        _lockOnSessionLockCheckBox.Checked = config.LockOnSessionLock;
        _topMostCheckBox.Checked = config.MainWindowTopMost;
        _touchModeCheckBox.Checked = config.TouchModeEnabled;
        _autoLockMinutesNumeric.Value = Math.Max((int)_autoLockMinutesNumeric.Minimum, config.AutoLockMinutes);
        _newPinTextBox.Text = string.Empty;
        TopMost = config.MainWindowTopMost;

        var selectedSecretId = GetSelectedSecret()?.Id;
        var selectedClipboardTimestamp = GetSelectedClipboardEntry()?.CapturedAt;

        _allSecrets = secrets.ToList();
        _currentClipboardEntries = clipboardEntries.ToList();
        _clipboardGrid.DataSource = null;
        _clipboardGrid.DataSource = _currentClipboardEntries
            .Select(entry => new ClipboardEntry(Shorten(entry.Content), entry.CapturedAt))
            .ToList();
        ApplySecretFilter(selectedSecretId);
        ApplyDockFilter(selectedSecretId, selectedClipboardTimestamp);
        RestoreClipboardSelection(selectedClipboardTimestamp);

        _lockStatusLabel.Text = isLocked ? "Status: gesperrt" : "Status: entsperrt";
        _unlockButton.Enabled = isLocked;
        _lockButton.Enabled = !isLocked && config.LockEnabled;
        _unlockTabButton.Visible = isLocked;
        _unlockFeedbackLabel.Text = unlockFeedback ?? string.Empty;
        _unlockFeedbackLabel.ForeColor = string.IsNullOrWhiteSpace(unlockFeedback) ? _mutedTextColor : Color.FromArgb(255, 134, 134);
        ConfigureUnlockPage();
        UpdateTotpPreview();
        ApplyTouchMode(config.TouchModeEnabled);
        ApplyDarkTheme(this);
        UpdateTabAvailability();
        if (isLocked)
        {
            ShowPage(_unlockPage);
        }
        else if (_isDocked)
        {
            ShowDockedPage();
        }
        else if (_contentHost.Controls.Contains(_unlockPage))
        {
            ShowPage(_generalPage);
            ClearUnlockInput();
        }

        Invalidate();
    }

    public SettingsUpdate BuildSettingsUpdate()
    {
        return new SettingsUpdate
        {
            Config = new AppConfig
            {
                TypePrimarySecretHotkey = _primaryHotkeyTextBox.HotkeyText,
                OpenManagerHotkey = _managerHotkeyTextBox.HotkeyText,
                ClipboardHistoryLimit = Decimal.ToInt32(_historyLimitNumeric.Value),
                AutoStartEnabled = _autoStartCheckBox.Checked,
                LockEnabled = _lockEnabledCheckBox.Checked,
                LockOnSessionLock = _lockOnSessionLockCheckBox.Checked,
                MainWindowTopMost = _topMostCheckBox.Checked,
                TouchModeEnabled = _touchModeCheckBox.Checked,
                AutoLockMinutes = Decimal.ToInt32(_autoLockMinutesNumeric.Value)
            },
            NewPin = string.IsNullOrWhiteSpace(_newPinTextBox.Text) ? null : _newPinTextBox.Text
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyWindowDarkMode();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateTabLayout();
        UpdateUnlockLayout();
    }

    private void ConfigureGrids()
    {
        ConfigureGrid(_secretGrid);
        ConfigureGrid(_clipboardGrid);

        _secretGrid.SelectionChanged += (_, _) =>
        {
            UpdateTotpPreview();
            UpdateInsertAvailability();
        };

        _secretGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(SecretEntry.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 42
        });
        _secretGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Benutzer",
            DataPropertyName = nameof(SecretEntry.Username),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 30
        });
        _secretGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Gruppe",
            DataPropertyName = nameof(SecretEntry.GroupPath),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 26
        });
        _secretGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Typ",
            DataPropertyName = nameof(SecretEntry.EntryType),
            Width = 130
        });

        _clipboardGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Inhalt",
            DataPropertyName = nameof(ClipboardEntry.Content),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 80
        });
        _clipboardGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Zeit",
            DataPropertyName = nameof(ClipboardEntry.CapturedAt),
            Width = 180,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" }
        });
    }

    private void ConfigureGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.RowTemplate.Height = 28;
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _panelColor,
            ForeColor = _textColor,
            SelectionBackColor = _panelColor,
            SelectionForeColor = _textColor,
            Font = new Font(Font, FontStyle.Bold)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _surfaceColor,
            ForeColor = _textColor,
            SelectionBackColor = _selectionColor,
            SelectionForeColor = _textColor,
            WrapMode = DataGridViewTriState.False
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(25, 31, 36),
            ForeColor = _textColor,
            SelectionBackColor = _selectionColor,
            SelectionForeColor = _textColor
        };
        grid.GridColor = _panelColor;
    }

    private MenuStrip BuildMenuStrip()
    {
        var menuStrip = new MenuStrip
        {
            Dock = DockStyle.Top,
            BackColor = _panelColor,
            ForeColor = _textColor,
            Renderer = new DarkToolStripRenderer(_panelColor, _surfaceColor, _accentColor, _textColor)
        };

        var fileMenu = new ToolStripMenuItem("Datei");
        fileMenu.DropDownItems.Add("Secrets exportieren", null, (_, _) => ExportSecretsRequested?.Invoke(this, EventArgs.Empty));
        fileMenu.DropDownItems.Add("Secrets importieren", null, (_, _) => ImportSecretsRequested?.Invoke(this, EventArgs.Empty));
        fileMenu.DropDownItems.Add("KeePass XML importieren", null, (_, _) => ImportKeePassRequested?.Invoke(this, EventArgs.Empty));
        fileMenu.DropDownItems.Add("Entsperrmuster setzen", null, (_, _) => SetUnlockPatternRequested?.Invoke(this, EventArgs.Empty));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Beenden", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        var actionMenu = new ToolStripMenuItem("Aktionen");
        actionMenu.DropDownItems.Add("Entsperren", null, (_, _) => UnlockRequested?.Invoke(this, EventArgs.Empty));
        actionMenu.DropDownItems.Add("Sperren", null, (_, _) => LockRequested?.Invoke(this, EventArgs.Empty));
        actionMenu.DropDownItems.Add("Muster zurücksetzen", null, (_, _) => _unlockPatternCanvas.ResetPattern());
        actionMenu.DropDownItems.Add("Einstellungen speichern", null, (_, _) => SaveSettingsRequested?.Invoke(this, EventArgs.Empty));

        var viewMenu = new ToolStripMenuItem("Ansicht");
        var topMostItem = new ToolStripMenuItem("Immer im Vordergrund") { CheckOnClick = true };
        topMostItem.CheckedChanged += (_, _) =>
        {
            _topMostCheckBox.Checked = topMostItem.Checked;
            TopMost = topMostItem.Checked;
        };
        _topMostCheckBox.CheckedChanged += (_, _) =>
        {
            topMostItem.Checked = _topMostCheckBox.Checked;
            TopMost = _topMostCheckBox.Checked;
            ConfigureStatusButtons();
        };
        viewMenu.DropDownItems.Add(topMostItem);

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(actionMenu);
        menuStrip.Items.Add(viewMenu);

        return menuStrip;
    }

    private void BuildPages()
    {
        BuildGeneralPage();
        BuildSecretsPage();
        BuildClipboardPage();
        BuildUnlockPage();
        BuildDockPage();
    }

    private void BuildGeneralPage()
    {
        var newPinHost = PasswordRevealHelper.CreateRevealHost(
            _newPinTextBox,
            _backgroundColor,
            _panelColor,
            _textColor,
            _accentColor,
            _toolTip);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(12),
            AutoSize = true,
            ColumnCount = 2,
            BackColor = _backgroundColor
        };

        panel.Controls.Add(new Label { Text = "Primary-Hotkey", AutoSize = true }, 0, 0);
        panel.Controls.Add(_primaryHotkeyTextBox, 1, 0);
        panel.Controls.Add(new Label { Text = "Manager-Hotkey", AutoSize = true }, 0, 1);
        panel.Controls.Add(_managerHotkeyTextBox, 1, 1);
        panel.Controls.Add(new Label { Text = "Clipboard-Limit", AutoSize = true }, 0, 2);
        panel.Controls.Add(_historyLimitNumeric, 1, 2);
        panel.Controls.Add(_autoStartCheckBox, 1, 3);
        panel.Controls.Add(_lockEnabledCheckBox, 1, 4);
        panel.Controls.Add(_lockOnSessionLockCheckBox, 1, 5);
        panel.Controls.Add(_topMostCheckBox, 1, 6);
        panel.Controls.Add(_touchModeCheckBox, 1, 7);
        panel.Controls.Add(new Label { Text = "Auto-Lock nach Minuten", AutoSize = true }, 0, 8);
        panel.Controls.Add(_autoLockMinutesNumeric, 1, 8);
        panel.Controls.Add(new Label { Text = "0 = nur bei Windows-Sperre", AutoSize = true }, 1, 9);
        panel.Controls.Add(new Label { Text = "Neue PIN", AutoSize = true }, 0, 10);
        panel.Controls.Add(newPinHost, 1, 10);
        panel.Controls.Add(new Label { Text = "Leer lassen = bestehende PIN behalten", AutoSize = true }, 1, 11);

        _generalPage.Controls.Clear();
        _generalPage.BackColor = _backgroundColor;
        _generalPage.Controls.Add(panel);
    }

    private void BuildSecretsPage()
    {
        var addButton = new Button { Text = "Neu", AutoSize = true };
        var setPrimaryButton = new Button { Text = "Standard", AutoSize = true };
        var editButton = new Button { Text = "Edit", AutoSize = true };
        var deleteButton = new Button { Text = "X", AutoSize = true };
        var importKeePassButton = new Button { Text = "KeePass", AutoSize = true };
        addButton.Click += (_, _) => AddSecretRequested?.Invoke(this, EventArgs.Empty);
        setPrimaryButton.Click += (_, _) =>
        {
            var selectedSecrets = GetSelectedSecrets();
            if (selectedSecrets.Count == 1)
            {
                SetPrimarySecretRequested?.Invoke(selectedSecrets[0]);
            }
        };
        editButton.Click += (_, _) =>
        {
            var selectedSecrets = GetSelectedSecrets();
            if (selectedSecrets.Count == 1)
            {
                EditSecretRequested?.Invoke(selectedSecrets[0]);
            }
        };
        deleteButton.Click += (_, _) =>
        {
            var selectedSecrets = GetSelectedSecrets();
            if (selectedSecrets.Count > 1)
            {
                DeleteSecretsRequested?.Invoke(selectedSecrets);
            }
            else if (selectedSecrets.Count == 1)
            {
                DeleteSecretRequested?.Invoke(selectedSecrets[0]);
            }
        };
        importKeePassButton.Click += (_, _) => ImportKeePassRequested?.Invoke(this, EventArgs.Empty);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = _backgroundColor,
            Padding = new Padding(0, 8, 0, 0)
        };
        buttonPanel.Controls.Add(addButton);
        buttonPanel.Controls.Add(setPrimaryButton);
        buttonPanel.Controls.Add(editButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(importKeePassButton);

        var searchPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            ColumnCount = 3,
            BackColor = _backgroundColor,
            Margin = Padding.Empty,
            Padding = new Padding(0, 0, 0, 6)
        };
        _secretSearchTextBox.Dock = DockStyle.Fill;
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchPanel.Controls.Add(new Label { Text = "Suche", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        searchPanel.Controls.Add(_secretSearchTextBox, 1, 0);
        searchPanel.Controls.Add(_secretCountLabel, 2, 0);

        var detailsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8),
            BackColor = _backgroundColor
        };
        detailsPanel.Controls.Add(new Label { Text = "Aktueller TOTP:", AutoSize = true });
        detailsPanel.Controls.Add(_totpPreviewLabel);

        var insertPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = false,
            Height = 86,
            ColumnCount = 4,
            Padding = new Padding(0, 14, 0, 0),
            BackColor = _backgroundColor
        };
        insertPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        insertPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        insertPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        insertPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        insertPanel.Controls.Add(_insertUsernameButton, 0, 0);
        insertPanel.Controls.Add(_insertSecretButton, 1, 0);
        insertPanel.Controls.Add(_insertTotpButton, 2, 0);
        insertPanel.Controls.Add(_insertSequenceButton, 3, 0);

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = _backgroundColor };
        panel.Controls.Add(_secretGrid);
        panel.Controls.Add(detailsPanel);
        panel.Controls.Add(searchPanel);
        panel.Controls.Add(buttonPanel);
        panel.Controls.Add(insertPanel);

        _secretsPage.Controls.Clear();
        _secretsPage.BackColor = _backgroundColor;
        _secretsPage.Controls.Add(panel);
    }

    private void BuildClipboardPage()
    {
        var reuseClipboardButton = new Button { Text = "In Zwischenablage legen", AutoSize = true };
        reuseClipboardButton.Click += (_, _) =>
        {
            var entry = GetSelectedClipboardEntry();
            if (entry is not null)
            {
                ClipboardReuseRequested?.Invoke(entry);
            }
        };

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = _backgroundColor };
        buttonPanel.Controls.Add(reuseClipboardButton);

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = _backgroundColor };
        panel.Controls.Add(_clipboardGrid);
        panel.Controls.Add(buttonPanel);

        _clipboardPage.Controls.Clear();
        _clipboardPage.BackColor = _backgroundColor;
        _clipboardPage.Controls.Add(panel);
    }

    private void BuildUnlockPage()
    {
        var unlockPinHost = PasswordRevealHelper.CreateRevealHost(
            _unlockPinTextBox,
            _backgroundColor,
            _panelColor,
            _textColor,
            _accentColor,
            _toolTip);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 12),
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = _backgroundColor
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        unlockPinHost.Dock = DockStyle.Top;
        unlockPinHost.Margin = new Padding(0, 0, 0, 10);
        _unlockPatternCanvas.Dock = DockStyle.Fill;
        _unlockPatternCanvas.Margin = Padding.Empty;
        _unlockSubmitButton.Dock = DockStyle.Top;
        _unlockSubmitButton.Height = 48;
        _unlockSubmitButton.Margin = new Padding(0, 12, 0, 0);
        _unlockSubmitButton.BackColor = _accentColor;
        _unlockSubmitButton.ForeColor = Color.FromArgb(14, 20, 28);
        _unlockSubmitButton.FlatStyle = FlatStyle.Flat;
        _unlockSubmitButton.FlatAppearance.BorderColor = _accentColor;
        _unlockSubmitButton.FlatAppearance.BorderSize = 1;

        panel.Controls.Add(unlockPinHost, 0, 0);
        panel.Controls.Add(_unlockPatternCanvas, 0, 1);
        panel.Controls.Add(_unlockFeedbackLabel, 0, 2);
        panel.Controls.Add(_unlockSubmitButton, 0, 3);

        _unlockPage.Controls.Clear();
        _unlockPage.BackColor = _backgroundColor;
        _unlockPage.Controls.Add(panel);
    }

    private void BuildDockPage()
    {
        var secretActions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 170,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = _backgroundColor,
            Padding = new Padding(0, 10, 0, 10)
        };
        secretActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        secretActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        secretActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        secretActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        secretActions.Controls.Add(_dockUsernameButton, 0, 0);
        secretActions.Controls.Add(_dockSecretButton, 1, 0);
        secretActions.Controls.Add(_dockTotpButton, 0, 1);
        secretActions.Controls.Add(_dockTypeButton, 1, 1);

        var searchPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = _backgroundColor,
            Padding = new Padding(0, 0, 0, 8)
        };
        searchPanel.Controls.Add(_dockSearchTextBox);

        var clipboardActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = _backgroundColor,
            Padding = new Padding(0, 8, 0, 0)
        };
        clipboardActions.Controls.Add(_dockClipboardButton);
        _dockClipboardButton.Dock = DockStyle.Fill;

        var secretSection = new Panel { Dock = DockStyle.Top, Height = 320, BackColor = _backgroundColor };
        secretSection.Controls.Add(_dockSecretList);
        secretSection.Controls.Add(searchPanel);
        secretSection.Controls.Add(secretActions);
        secretSection.Controls.Add(new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = _backgroundColor,
            Controls = { _dockSecretLabel }
        });

        var clipboardSection = new Panel { Dock = DockStyle.Fill, BackColor = _backgroundColor };
        clipboardSection.Controls.Add(_dockClipboardList);
        clipboardSection.Controls.Add(clipboardActions);
        clipboardSection.Controls.Add(new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = _backgroundColor,
            Controls = { _dockClipboardLabel }
        });

        _dockPage.Controls.Clear();
        _dockPage.Padding = new Padding(12);
        _dockPage.BackColor = _backgroundColor;
        _dockPage.Controls.Add(clipboardSection);
        _dockPage.Controls.Add(secretSection);
    }

    private void ShowPage(Panel page)
    {
        if (_isDocked && page != _unlockPage && page != _dockPage)
        {
            page = _dockPage;
        }

        if (_isLocked && page != _unlockPage)
        {
            return;
        }

        _contentHost.SuspendLayout();
        _contentHost.Controls.Clear();
        _contentHost.Controls.Add(page);
        _contentHost.ResumeLayout();

        UpdateTabButtonState(_generalTabButton, page == _generalPage);
        UpdateTabButtonState(_secretsTabButton, page == _secretsPage);
        UpdateTabButtonState(_clipboardTabButton, page == _clipboardPage);
        UpdateTabButtonState(_unlockTabButton, page == _unlockPage);

        if (page == _unlockPage)
        {
            FocusUnlockInput();
        }
    }

    private void UpdateTabButtonState(Button button, bool isActive)
    {
        button.BackColor = isActive ? _panelColor : _surfaceColor;
        button.ForeColor = _textColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = isActive ? _accentColor : _panelColor;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font(Font, isActive ? FontStyle.Bold : FontStyle.Regular);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(0, 0, 0, 1);
    }

    private void ApplyDarkTheme(Control root)
    {
        BackColor = _backgroundColor;
        ForeColor = _textColor;

        foreach (Control control in GetAllControls(root))
        {
            switch (control)
            {
                case MenuStrip menuStrip:
                    menuStrip.BackColor = _panelColor;
                    menuStrip.ForeColor = _textColor;
                    break;
                case DataGridView grid:
                    grid.BackgroundColor = _surfaceColor;
                    grid.ForeColor = _textColor;
                    break;
                case TextBox textBox:
                    textBox.BackColor = _surfaceColor;
                    textBox.ForeColor = _textColor;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ListBox listBox:
                    listBox.BackColor = _surfaceColor;
                    listBox.ForeColor = _textColor;
                    break;
                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = _surfaceColor;
                    numericUpDown.ForeColor = _textColor;
                    break;
                case Button button:
                    if (button != _generalTabButton && button != _secretsTabButton && button != _clipboardTabButton && button != _unlockTabButton)
                    {
                        button.BackColor = _panelColor;
                        button.ForeColor = _textColor;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderColor = _accentColor;
                    }

                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = _backgroundColor;
                    checkBox.ForeColor = _textColor;
                    break;
                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = label.Text.Contains("Leer lassen") || label.Text.Contains("0 = ")
                        ? _mutedTextColor
                        : _textColor;
                    break;
                case FlowLayoutPanel flowPanel:
                    flowPanel.BackColor = _backgroundColor;
                    flowPanel.ForeColor = _textColor;
                    break;
                case TableLayoutPanel tableLayoutPanel:
                    tableLayoutPanel.BackColor = _backgroundColor;
                    tableLayoutPanel.ForeColor = _textColor;
                    break;
                case Panel panel:
                    panel.BackColor = _backgroundColor;
                    panel.ForeColor = _textColor;
                    break;
            }
        }

        UpdateTabButtonState(_generalTabButton, _contentHost.Controls.Contains(_generalPage));
        UpdateTabButtonState(_secretsTabButton, _contentHost.Controls.Contains(_secretsPage));
        UpdateTabButtonState(_clipboardTabButton, _contentHost.Controls.Contains(_clipboardPage));
        UpdateTabButtonState(_unlockTabButton, _contentHost.Controls.Contains(_unlockPage));
        ConfigureInsertButtons();
        ConfigureStatusButtons();
        UpdateTabLayout();
    }

    private void ApplyTouchMode(bool enabled)
    {
        var buttonHeight = enabled ? 58 : 38;
        var buttonWidth = enabled ? 132 : 108;
        var rowHeight = enabled ? 44 : 28;
        var font = new Font(Font.FontFamily, enabled ? 11f : 9f, FontStyle.Regular);
        var insertButtonSize = enabled ? new Size(148, 100) : new Size(126, 78);
        var dockButtonFontSize = enabled ? 12f : 10.5f;

        foreach (var button in new[] { _insertUsernameButton, _insertSecretButton, _insertTotpButton, _insertSequenceButton, _generalTabButton, _secretsTabButton, _clipboardTabButton, _unlockTabButton })
        {
            button.Height = buttonHeight;
            button.Font = new Font(Font.FontFamily, enabled ? 11f : 9f, button.Font.Style);
        }

        foreach (var button in new[] { _insertUsernameButton, _insertSecretButton, _insertTotpButton, _insertSequenceButton })
        {
            button.Size = insertButtonSize;
            button.Margin = Padding.Empty;
        }

        foreach (var button in new[] { _generalTabButton, _secretsTabButton, _clipboardTabButton, _unlockTabButton })
        {
            button.Width = button switch
            {
                var current when current == _generalTabButton => enabled ? 110 : 94,
                var current when current == _secretsTabButton => enabled ? 96 : 82,
                var current when current == _unlockTabButton => enabled ? 118 : 104,
                _ => enabled ? 104 : 90
            };
        }

        _tabsPanel.Height = enabled ? 70 : 62;
        _tabsPanel.Padding = enabled ? new Padding(12, 10, 12, 16) : new Padding(12, 10, 12, 14);

        _aboutButton.Size = enabled ? new Size(42, 42) : new Size(34, 34);
        _aboutButton.Font = new Font(Font.FontFamily, enabled ? 12f : 10f, FontStyle.Bold);
        _unlockButton.Size = enabled ? new Size(48, 40) : new Size(42, 34);
        _lockButton.Size = enabled ? new Size(48, 40) : new Size(42, 34);
        _topMostButton.Size = enabled ? new Size(48, 40) : new Size(42, 34);
        _dockToggleButton.Size = enabled ? new Size(48, 40) : new Size(42, 34);
        _saveSettingsButton.Size = enabled ? new Size(48, 40) : new Size(42, 34);
        foreach (var button in new[] { _dockUsernameButton, _dockSecretButton, _dockTotpButton, _dockTypeButton, _dockClipboardButton })
        {
            button.Font = new Font(Font.FontFamily, dockButtonFontSize, FontStyle.Bold);
        }
        foreach (var button in new[] { _dockUsernameButton, _dockSecretButton, _dockTotpButton, _dockTypeButton })
        {
            button.Padding = new Padding(0);
        }
        _dockSecretList.Font = font;
        _dockClipboardList.Font = font;

        _secretGrid.RowTemplate.Height = rowHeight;
        _clipboardGrid.RowTemplate.Height = rowHeight;
        _secretGrid.DefaultCellStyle.Font = font;
        _clipboardGrid.DefaultCellStyle.Font = font;
        _secretGrid.ColumnHeadersHeight = enabled ? 42 : 34;
        _clipboardGrid.ColumnHeadersHeight = enabled ? 42 : 34;
        ConfigureInsertButtons();
        ConfigureStatusButtons();
    }

    private void ConfigureInsertButtons()
    {
        _aboutButton.FlatStyle = FlatStyle.Flat;
        _aboutButton.FlatAppearance.BorderSize = 1;
        _aboutButton.FlatAppearance.BorderColor = _accentColor;
        _aboutButton.BackColor = _panelColor;
        _aboutButton.ForeColor = _textColor;
        _aboutButton.TextAlign = ContentAlignment.MiddleCenter;

        ConfigureInsertButton(_insertUsernameButton, CreateUserIcon(28, _accentColor), "USERNAME");
        ConfigureInsertButton(_insertSecretButton, CreateLockIcon(28, _accentColor), "SECRET");
        ConfigureInsertButton(_insertTotpButton, CreateTotpIcon(28, _accentColor), "TOTP");
        ConfigureInsertButton(_insertSequenceButton, CreateArrowIcon(28, _accentColor, right: true), "SEQ");
    }

    private void ConfigureStatusButtons()
    {
        ConfigureStatusButton(_unlockButton, CreateUnlockIcon(20, _accentColor), "Entsperren");
        ConfigureStatusButton(_lockButton, CreateLockedIcon(20, _accentColor), "Sperren");
        ConfigureStatusButton(
            _topMostButton,
            _topMostCheckBox.Checked ? CreatePinnedIcon(20, _accentColor) : CreatePinIcon(20, _accentColor),
            _topMostCheckBox.Checked ? "Immer im Vordergrund ist aktiv" : "Immer im Vordergrund aktivieren");
        ConfigureStatusButton(
            _dockToggleButton,
            CreateArrowIcon(20, _accentColor, right: !_isDocked),
            _isDocked ? "Docked-Mode verlassen" : "An rechten Rand anheften");
        ConfigureStatusButton(_saveSettingsButton, CreateSaveIcon(20, _accentColor), "Einstellungen speichern");
    }

    private void ConfigureStatusButton(Button button, Image image, string toolTipText)
    {
        var previousImage = button.Image;
        button.Text = string.Empty;
        button.Image = image;
        previousImage?.Dispose();
        button.Padding = new Padding(0);
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = _accentColor;
        button.BackColor = _panelColor;
        button.ForeColor = _textColor;
        button.ImageAlign = ContentAlignment.MiddleCenter;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.TabStop = false;
        _toolTip.SetToolTip(button, toolTipText);
    }

    private void ToggleTopMost()
    {
        _topMostCheckBox.Checked = !_topMostCheckBox.Checked;
        TopMost = _topMostCheckBox.Checked;
        ConfigureStatusButtons();
    }

    public void ShowUnlockPage()
    {
        ShowPage(_unlockPage);
    }

    public void ShowSecretsPage()
    {
        if (_isDocked)
        {
            ShowDockedPage();
            return;
        }

        ShowPage(_secretsPage);
    }

    private void ShowDockedPage()
    {
        ShowPage(_dockPage);
    }

    public string EnteredUnlockPin => _unlockPinTextBox.Text;

    public string EnteredUnlockPattern => _unlockPatternCanvas.GetPattern();

    public void SetUnlockFeedback(string? message)
    {
        _unlockFeedbackLabel.Text = message ?? string.Empty;
        _unlockFeedbackLabel.ForeColor = string.IsNullOrWhiteSpace(message) ? _mutedTextColor : Color.FromArgb(255, 134, 134);
    }

    public void ClearUnlockInput()
    {
        _unlockPinTextBox.Clear();
        _unlockPatternCanvas.ResetPattern();
        _unlockFeedbackLabel.Text = string.Empty;
    }

    private void ConfigureUnlockPage()
    {
        _unlockPinTextBox.Visible = _supportsPin;
        _unlockPatternCanvas.Visible = _supportsPattern;
        UpdateUnlockLayout();
    }

    private void FocusUnlockInput()
    {
        if (_supportsPin)
        {
            _unlockPinTextBox.Focus();
            return;
        }

        if (_supportsPattern)
        {
            _unlockPatternCanvas.Focus();
        }
    }

    private void UpdateTabAvailability()
    {
        _generalTabButton.Enabled = !_isLocked && !_isDocked;
        _secretsTabButton.Enabled = !_isLocked && !_isDocked;
        _clipboardTabButton.Enabled = !_isLocked && !_isDocked;
        _unlockTabButton.Enabled = _isLocked;
        UpdateTabLayout();
    }

    private void UpdateTabLayout()
    {
        if (_isDocked || _tabsPanel is null)
        {
            return;
        }

        var visibleTabs = new List<Button> { _generalTabButton, _secretsTabButton, _clipboardTabButton };
        if (_unlockTabButton.Visible)
        {
            visibleTabs.Add(_unlockTabButton);
        }

        if (visibleTabs.Count == 0 || _tabsPanel.Width <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(120, _tabsPanel.ClientSize.Width - _tabsPanel.Padding.Left - _tabsPanel.Padding.Right);
        var widthPerTab = Math.Max(70, availableWidth / visibleTabs.Count);
        var height = _touchModeCheckBox.Checked ? 44 : 36;

        foreach (var tab in visibleTabs)
        {
            tab.Width = widthPerTab - 2;
            tab.Height = height;
            tab.Margin = Padding.Empty;
        }
    }

    private void UpdateUnlockLayout()
    {
        var contentWidth = Math.Max(180, _contentHost.ClientSize.Width - 24);
        _unlockPatternCanvas.Height = _supportsPattern ? Math.Max(180, contentWidth) : 0;
    }

    private void ToggleDockMode()
    {
        if (_isDocked)
        {
            ExitDockMode();
        }
        else
        {
            EnterDockMode();
        }
    }

    private void EnterDockMode()
    {
        if (_isDocked)
        {
            return;
        }

        _normalBounds = Bounds;
        _restoreTopMostAfterDock = _topMostCheckBox.Checked;
        _isDocked = true;
        _topMostCheckBox.Checked = true;
        TopMost = true;
        if (MainMenuStrip is not null)
        {
            MainMenuStrip.Visible = false;
        }
        _tabsPanel.Visible = false;

        var area = Screen.FromControl(this).WorkingArea;
        var width = Math.Min(280, Math.Max(240, area.Width / 5));
        var height = Math.Max(360, area.Height / 2);
        var top = area.Top + Math.Max(0, (area.Height - height) / 2);
        Bounds = new Rectangle(area.Right - width, top, width, height);
        ShowDockedPage();
        UpdateTabAvailability();
        ConfigureStatusButtons();
    }

    private void ExitDockMode()
    {
        if (!_isDocked)
        {
            return;
        }

        _isDocked = false;
        if (MainMenuStrip is not null)
        {
            MainMenuStrip.Visible = true;
        }
        _tabsPanel.Visible = true;
        if (_normalBounds.Width > 0 && _normalBounds.Height > 0)
        {
            Bounds = _normalBounds;
        }

        _topMostCheckBox.Checked = _restoreTopMostAfterDock;
        TopMost = _restoreTopMostAfterDock;
        if (_isLocked)
        {
            ShowUnlockPage();
        }
        else
        {
            ShowPage(_generalPage);
        }

        UpdateTabAvailability();
        ConfigureStatusButtons();
    }

    private void TriggerDockInsert(SecretFieldKind kind)
    {
        var secret = GetSelectedDockSecret();
        if (secret is not null)
        {
            InsertSecretFieldRequested?.Invoke(secret, kind);
        }
    }

    private SecretEntry? GetSelectedDockSecret()
    {
        return _dockSecretList.SelectedItem as SecretEntry;
    }

    private ClipboardEntry? GetSelectedDockClipboardEntry()
    {
        return _dockClipboardList.SelectedItem as ClipboardEntry;
    }

    private void RestoreDockSelections(Guid? selectedSecretId, DateTimeOffset? selectedTimestamp)
    {
        if (selectedSecretId.HasValue)
        {
            for (var i = 0; i < _dockSecretList.Items.Count; i++)
            {
                if (_dockSecretList.Items[i] is SecretEntry secret && secret.Id == selectedSecretId.Value)
                {
                    _dockSecretList.SelectedIndex = i;
                    break;
                }
            }
        }
        else if (_dockSecretList.Items.Count > 0 && _dockSecretList.SelectedIndex < 0)
        {
            _dockSecretList.SelectedIndex = 0;
        }

        if (selectedTimestamp.HasValue)
        {
            for (var i = 0; i < _dockClipboardList.Items.Count; i++)
            {
                if (_dockClipboardList.Items[i] is ClipboardEntry entry && entry.CapturedAt == selectedTimestamp.Value)
                {
                    _dockClipboardList.SelectedIndex = i;
                    break;
                }
            }
        }
        else if (_dockClipboardList.Items.Count > 0 && _dockClipboardList.SelectedIndex < 0)
        {
            _dockClipboardList.SelectedIndex = 0;
        }
    }

    private void ConfigureInsertButton(Button button, Image image, string text)
    {
        var previousImage = button.Image;
        button.Text = text;
        button.Image = image;
        previousImage?.Dispose();
        button.TextImageRelation = TextImageRelation.ImageAboveText;
        button.ImageAlign = ContentAlignment.MiddleCenter;
        button.TextAlign = ContentAlignment.BottomCenter;
        button.Padding = new Padding(10, 8, 10, 10);
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = _accentColor;
        button.BackColor = _panelColor;
        button.ForeColor = _textColor;
    }

    private static Bitmap CreateUserIcon(int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, size * 0.28f, size * 0.08f, size * 0.44f, size * 0.34f);
        graphics.FillEllipse(brush, size * 0.14f, size * 0.48f, size * 0.72f, size * 0.38f);
        return bitmap;
    }

    private static Bitmap CreateLockIcon(int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.6f);
        using var brush = new SolidBrush(color);
        graphics.DrawArc(pen, size * 0.23f, size * 0.05f, size * 0.54f, size * 0.52f, 200, 140);
        graphics.FillRectangle(brush, size * 0.2f, size * 0.42f, size * 0.6f, size * 0.38f);
        using var innerBrush = new SolidBrush(Color.FromArgb(20, 24, 28));
        graphics.FillEllipse(innerBrush, size * 0.44f, size * 0.53f, size * 0.12f, size * 0.12f);
        return bitmap;
    }

    private static Bitmap CreateLockedIcon(int size, Color color)
    {
        return CreateLockStateIcon(size, color, isUnlocked: false);
    }

    private static Bitmap CreateUnlockIcon(int size, Color color)
    {
        return CreateLockStateIcon(size, color, isUnlocked: true);
    }

    private static Bitmap CreateLockStateIcon(int size, Color color, bool isUnlocked)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.2f);
        using var bodyBrush = new SolidBrush(color);
        using var innerBrush = new SolidBrush(Color.FromArgb(20, 24, 28));

        graphics.FillRectangle(bodyBrush, size * 0.2f, size * 0.44f, size * 0.6f, size * 0.34f);
        graphics.FillEllipse(innerBrush, size * 0.45f, size * 0.54f, size * 0.1f, size * 0.1f);

        if (isUnlocked)
        {
            graphics.DrawArc(pen, size * 0.02f, size * 0.07f, size * 0.58f, size * 0.56f, 200, 138);
            graphics.DrawLine(pen, size * 0.44f, size * 0.27f, size * 0.66f, size * 0.2f);
        }
        else
        {
            graphics.DrawArc(pen, size * 0.22f, size * 0.08f, size * 0.56f, size * 0.52f, 200, 140);
            graphics.DrawLine(pen, size * 0.22f, size * 0.2f, size * 0.78f, size * 0.82f);
        }

        return bitmap;
    }

    private static Bitmap CreatePinIcon(int size, Color color)
    {
        return CreatePushPinIcon(size, color, pinned: false);
    }

    private static Bitmap CreatePinnedIcon(int size, Color color)
    {
        return CreatePushPinIcon(size, color, pinned: true);
    }

    private static Bitmap CreatePushPinIcon(int size, Color color, bool pinned)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.0f);
        using var brush = new SolidBrush(color);

        if (pinned)
        {
            graphics.FillEllipse(brush, size * 0.3f, size * 0.08f, size * 0.4f, size * 0.24f);
            graphics.FillRectangle(brush, size * 0.44f, size * 0.3f, size * 0.12f, size * 0.32f);
            graphics.DrawLine(pen, size * 0.5f, size * 0.62f, size * 0.5f, size * 0.92f);
            graphics.DrawLine(pen, size * 0.24f, size * 0.36f, size * 0.76f, size * 0.36f);
        }
        else
        {
            graphics.TranslateTransform(size * 0.18f, size * 0.22f);
            graphics.RotateTransform(-36f);
            graphics.FillEllipse(brush, size * 0.16f, size * 0.02f, size * 0.34f, size * 0.2f);
            graphics.FillRectangle(brush, size * 0.3f, size * 0.2f, size * 0.12f, size * 0.34f);
            graphics.DrawLine(pen, size * 0.36f, size * 0.54f, size * 0.36f, size * 0.9f);
            graphics.ResetTransform();
        }

        return bitmap;
    }

    private static Bitmap CreateSaveIcon(int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.0f);
        using var brush = new SolidBrush(color);
        using var innerBrush = new SolidBrush(Color.FromArgb(20, 24, 28));

        graphics.FillRectangle(brush, size * 0.16f, size * 0.14f, size * 0.68f, size * 0.72f);
        graphics.FillRectangle(innerBrush, size * 0.28f, size * 0.2f, size * 0.34f, size * 0.18f);
        graphics.FillRectangle(innerBrush, size * 0.28f, size * 0.52f, size * 0.28f, size * 0.18f);
        graphics.DrawRectangle(pen, size * 0.16f, size * 0.14f, size * 0.68f, size * 0.72f);
        graphics.DrawLine(pen, size * 0.62f, size * 0.14f, size * 0.62f, size * 0.38f);
        return bitmap;
    }

    private static Bitmap CreateArrowIcon(int size, Color color, bool right)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        if (right)
        {
            graphics.DrawLine(pen, size * 0.22f, size * 0.5f, size * 0.74f, size * 0.5f);
            graphics.DrawLine(pen, size * 0.5f, size * 0.28f, size * 0.74f, size * 0.5f);
            graphics.DrawLine(pen, size * 0.5f, size * 0.72f, size * 0.74f, size * 0.5f);
        }
        else
        {
            graphics.DrawLine(pen, size * 0.26f, size * 0.5f, size * 0.78f, size * 0.5f);
            graphics.DrawLine(pen, size * 0.5f, size * 0.28f, size * 0.26f, size * 0.5f);
            graphics.DrawLine(pen, size * 0.5f, size * 0.72f, size * 0.26f, size * 0.5f);
        }

        return bitmap;
    }

    private static Bitmap CreateTotpIcon(int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.4f);
        graphics.DrawEllipse(pen, size * 0.14f, size * 0.14f, size * 0.72f, size * 0.72f);
        graphics.DrawLine(pen, size * 0.5f, size * 0.5f, size * 0.5f, size * 0.28f);
        graphics.DrawLine(pen, size * 0.5f, size * 0.5f, size * 0.67f, size * 0.58f);
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, size * 0.44f, size * 0.44f, size * 0.12f, size * 0.12f);
        return bitmap;
    }

    private static IEnumerable<Control> GetAllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;

            foreach (var descendant in GetAllControls(child))
            {
                yield return descendant;
            }
        }
    }

    private void ApplyDockFilter(Guid? selectedSecretId = null, DateTimeOffset? selectedTimestamp = null)
    {
        var search = _dockSearchTextBox.Text.Trim();
        _filteredDockSecrets = string.IsNullOrWhiteSpace(search)
            ? _currentSecrets.ToList()
            : _currentSecrets.Where(secret => MatchesSecretSearch(secret, search)).ToList();
        _filteredDockClipboardEntries = string.IsNullOrWhiteSpace(search)
            ? _currentClipboardEntries.ToList()
            : _currentClipboardEntries.Where(entry => entry.Content.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        _dockSecretList.DataSource = null;
        _dockSecretList.DataSource = _filteredDockSecrets.ToList();
        _dockClipboardList.DataSource = null;
        _dockClipboardList.DataSource = _filteredDockClipboardEntries.ToList();
        RestoreDockSelections(selectedSecretId, selectedTimestamp);
    }

    private void ApplySecretFilter(Guid? selectedSecretId = null)
    {
        var search = _secretSearchTextBox.Text.Trim();
        _currentSecrets = string.IsNullOrWhiteSpace(search)
            ? _allSecrets.ToList()
            : _allSecrets.Where(secret => MatchesSecretSearch(secret, search)).ToList();

        _secretGrid.DataSource = null;
        _secretGrid.DataSource = _currentSecrets;
        _dockSecretList.DataSource = null;
        _dockSecretList.DataSource = _currentSecrets.ToList();

        RestoreSecretSelection(selectedSecretId);
        ApplySecretRowStyles();
        UpdateInsertAvailability();
        UpdateTotpPreview();
        UpdateSecretCount();
    }

    private static bool MatchesSecretSearch(SecretEntry secret, string search)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;
        return (!string.IsNullOrWhiteSpace(secret.Name) && secret.Name.Contains(search, comparison)) ||
               (!string.IsNullOrWhiteSpace(secret.GroupPath) && secret.GroupPath.Contains(search, comparison)) ||
               (!string.IsNullOrWhiteSpace(secret.Username) && secret.Username.Contains(search, comparison)) ||
               (!string.IsNullOrWhiteSpace(secret.Url) && secret.Url.Contains(search, comparison)) ||
               (!string.IsNullOrWhiteSpace(secret.Notes) && secret.Notes.Contains(search, comparison)) ||
               (!string.IsNullOrWhiteSpace(secret.Source) && secret.Source.Contains(search, comparison));
    }

    private void UpdateSecretCount()
    {
        _secretCountLabel.Text = $"{_currentSecrets.Count} / {_allSecrets.Count} Eintraege";
        _secretCountLabel.ForeColor = _mutedTextColor;
    }

    private void ApplySecretRowStyles()
    {
        foreach (DataGridViewRow row in _secretGrid.Rows)
        {
            if (row.DataBoundItem is not SecretEntry secret)
            {
                continue;
            }

            row.DefaultCellStyle.Font = new Font(Font, secret.IsPrimary ? FontStyle.Bold : FontStyle.Regular);
            row.DefaultCellStyle.BackColor = secret.IsPrimary ? Color.FromArgb(32, 41, 53) : _surfaceColor;
        }
    }

    private void RestoreSecretSelection(Guid? selectedSecretId)
    {
        if (!selectedSecretId.HasValue)
        {
            if (_secretGrid.Rows.Count > 0)
            {
                _secretGrid.Rows[0].Selected = true;
                _secretGrid.CurrentCell = _secretGrid.Rows[0].Cells[0];
            }

            return;
        }

        foreach (DataGridViewRow row in _secretGrid.Rows)
        {
            if (row.DataBoundItem is SecretEntry secret && secret.Id == selectedSecretId.Value)
            {
                row.Selected = true;
                _secretGrid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }

    private void RestoreClipboardSelection(DateTimeOffset? selectedTimestamp)
    {
        if (!selectedTimestamp.HasValue)
        {
            return;
        }

        foreach (DataGridViewRow row in _clipboardGrid.Rows)
        {
            if (row.DataBoundItem is ClipboardEntry entry && entry.CapturedAt == selectedTimestamp.Value)
            {
                row.Selected = true;
                _clipboardGrid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }

    private SecretEntry? GetSelectedSecret()
    {
        return GetSelectedSecrets().FirstOrDefault() ?? _secretGrid.CurrentRow?.DataBoundItem as SecretEntry;
    }

    private List<SecretEntry> GetSelectedSecrets()
    {
        return _secretGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem as SecretEntry)
            .Where(secret => secret is not null)
            .Cast<SecretEntry>()
            .DistinctBy(secret => secret.Id)
            .ToList();
    }

    private ClipboardEntry? GetSelectedClipboardEntry()
    {
        if (_clipboardGrid.CurrentRow?.Index is not int index || index < 0 || index >= _currentClipboardEntries.Count)
        {
            return null;
        }

        return _currentClipboardEntries[index];
    }

    private void TriggerInsertField(SecretFieldKind kind)
    {
        var secret = GetSelectedSecret();
        if (secret is not null)
        {
            InsertSecretFieldRequested?.Invoke(secret, kind);
        }
    }

    private void UpdateInsertAvailability()
    {
        var secret = GetSelectedSecret();
        UpdateInsertButtonState(_insertUsernameButton, secret is not null && !string.IsNullOrWhiteSpace(secret.Username));
        UpdateInsertButtonState(_insertSecretButton, secret is not null && !string.IsNullOrWhiteSpace(secret.Value));
        UpdateInsertButtonState(_insertTotpButton, secret is not null && !string.IsNullOrWhiteSpace(secret.TotpSeed));
        UpdateInsertButtonState(_insertSequenceButton, secret is not null);
    }

    private void UpdateInsertButtonState(Button button, bool enabled)
    {
        button.Enabled = enabled;
        button.BackColor = enabled ? _panelColor : Color.FromArgb(72, 78, 86);
        button.ForeColor = enabled ? _textColor : _mutedTextColor;
        button.FlatAppearance.BorderColor = enabled ? _accentColor : Color.FromArgb(92, 98, 108);
    }

    private void UpdateTotpPreview()
    {
        var secret = GetSelectedSecret();
        if (secret is null || TotpPreviewProvider is null)
        {
            _totpPreviewLabel.Text = "-";
            return;
        }

        _totpPreviewLabel.Text = TotpPreviewProvider(secret);
    }

    private void ApplyWindowDarkMode()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int useImmersiveDarkMode = 20;
        const int borderColor = 34;
        const int captionColor = 35;
        const int textColor = 36;

        var enabled = 1;
        var border = ToColorRef(_panelColor);
        var caption = ToColorRef(_backgroundColor);
        var text = ToColorRef(_textColor);

        DwmSetWindowAttribute(Handle, useImmersiveDarkMode, ref enabled, sizeof(int));
        DwmSetWindowAttribute(Handle, borderColor, ref border, sizeof(uint));
        DwmSetWindowAttribute(Handle, captionColor, ref caption, sizeof(uint));
        DwmSetWindowAttribute(Handle, textColor, ref text, sizeof(uint));
    }

    private void ShowAboutDialog()
    {
        var restoreTopMost = _topMostCheckBox.Checked;
        if (restoreTopMost)
        {
            _topMostCheckBox.Checked = false;
            TopMost = false;
        }

        using var dialog = new Form
        {
            Text = "Über PassTypePro",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(360, 220),
            BackColor = _backgroundColor,
            ForeColor = _textColor
        };

        var titleLabel = new Label
        {
            Text = "PassTypePro",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
            ForeColor = _textColor,
            BackColor = Color.Transparent
        };

        var versionLabel = new Label
        {
            Text = "Version: v0.2.2",
            AutoSize = true,
            ForeColor = _textColor,
            BackColor = Color.Transparent
        };

        var authorLabel = new Label
        {
            Text = "Autor: Steven Schödel",
            AutoSize = true,
            ForeColor = _textColor,
            BackColor = Color.Transparent
        };

        var licenseLink = new LinkLabel
        {
            Text = "Lizenz: MIT",
            AutoSize = true,
            LinkColor = _accentColor,
            ActiveLinkColor = Color.FromArgb(128, 195, 255),
            VisitedLinkColor = _accentColor,
            BackColor = Color.Transparent
        };
        licenseLink.Click += (_, _) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://opensource.org/license/MIT",
                UseShellExecute = true
            });
        };

        var urlLabel = new Label
        {
            Text = "https://opensource.org/license/MIT",
            AutoSize = true,
            ForeColor = _mutedTextColor,
            BackColor = Color.Transparent
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            BackColor = _panelColor,
            ForeColor = _textColor,
            FlatStyle = FlatStyle.Flat
        };
        okButton.FlatAppearance.BorderColor = _accentColor;

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = _backgroundColor,
            ColumnCount = 1,
            RowCount = 6
        };
        content.Controls.Add(titleLabel, 0, 0);
        content.Controls.Add(versionLabel, 0, 1);
        content.Controls.Add(authorLabel, 0, 2);
        content.Controls.Add(licenseLink, 0, 3);
        content.Controls.Add(urlLabel, 0, 4);
        content.Controls.Add(okButton, 0, 5);

        dialog.AcceptButton = okButton;
        dialog.Controls.Add(content);
        dialog.Shown += (_, _) => ApplyDialogDarkMode(dialog);

        try
        {
            dialog.ShowDialog(this);
        }
        finally
        {
            if (restoreTopMost)
            {
                _topMostCheckBox.Checked = true;
                TopMost = true;
            }
        }
    }

    private void ApplyDialogDarkMode(Form dialog)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int useImmersiveDarkMode = 20;
        const int borderColor = 34;
        const int captionColor = 35;
        const int textColor = 36;

        var enabled = 1;
        var border = ToColorRef(_panelColor);
        var caption = ToColorRef(_backgroundColor);
        var text = ToColorRef(_textColor);

        DwmSetWindowAttribute(dialog.Handle, useImmersiveDarkMode, ref enabled, sizeof(int));
        DwmSetWindowAttribute(dialog.Handle, borderColor, ref border, sizeof(uint));
        DwmSetWindowAttribute(dialog.Handle, captionColor, ref caption, sizeof(uint));
        DwmSetWindowAttribute(dialog.Handle, textColor, ref text, sizeof(uint));
    }

    private static uint ToColorRef(Color color)
    {
        return (uint)((color.B << 16) | (color.G << 8) | color.R);
    }

    private static string Shorten(string value)
    {
        var normalized = value.Replace(Environment.NewLine, " ");
        return normalized.Length > 100 ? normalized[..100] + "..." : normalized;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref uint attributeValue, int attributeSize);

    private sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _menuColor;
        private readonly Color _hoverColor;
        private readonly Color _borderColor;
        private readonly Color _textColor;

        public DarkToolStripRenderer(Color menuColor, Color hoverColor, Color borderColor, Color textColor)
            : base(new DarkColorTable(menuColor, hoverColor, borderColor))
        {
            _menuColor = menuColor;
            _hoverColor = hoverColor;
            _borderColor = borderColor;
            _textColor = textColor;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = _textColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var color = e.Item.Selected ? _hoverColor : _menuColor;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(_borderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        private readonly Color _menuColor;
        private readonly Color _hoverColor;
        private readonly Color _borderColor;

        public DarkColorTable(Color menuColor, Color hoverColor, Color borderColor)
        {
            _menuColor = menuColor;
            _hoverColor = hoverColor;
            _borderColor = borderColor;
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => _menuColor;
        public override Color MenuItemSelected => _hoverColor;
        public override Color MenuItemSelectedGradientBegin => _hoverColor;
        public override Color MenuItemSelectedGradientEnd => _hoverColor;
        public override Color MenuItemBorder => _borderColor;
        public override Color MenuBorder => _borderColor;
        public override Color ImageMarginGradientBegin => _menuColor;
        public override Color ImageMarginGradientMiddle => _menuColor;
        public override Color ImageMarginGradientEnd => _menuColor;
    }
}
