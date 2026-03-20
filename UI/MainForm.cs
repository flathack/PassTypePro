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
    private readonly DataGridView _secretGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoGenerateColumns = false, BackgroundColor = Color.FromArgb(29, 35, 41), BorderStyle = BorderStyle.None };
    private readonly DataGridView _clipboardGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoGenerateColumns = false, BackgroundColor = Color.FromArgb(29, 35, 41), BorderStyle = BorderStyle.None };
    private readonly Label _lockStatusLabel = new() { AutoSize = true };
    private readonly Label _totpPreviewLabel = new() { AutoSize = true };
    private readonly Button _unlockButton = new() { Text = "Entsperren", AutoSize = true };
    private readonly Button _lockButton = new() { Text = "Sperren", AutoSize = true };
    private readonly Button _generalTabButton = new() { Text = "Allgemein", AutoSize = true };
    private readonly Button _secretsTabButton = new() { Text = "Secrets", AutoSize = true };
    private readonly Button _clipboardTabButton = new() { Text = "Clipboard", AutoSize = true };
    private readonly Button _insertUsernameButton = new() { Text = "USERNAME", Width = 126, Height = 78 };
    private readonly Button _insertSecretButton = new() { Text = "SECRET", Width = 126, Height = 78 };
    private readonly Button _insertTotpButton = new() { Text = "TOTP", Width = 126, Height = 78 };
    private readonly Button _setPatternButton = new() { Text = "Entsperrmuster setzen", AutoSize = true };
    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill };
    private readonly Panel _generalPage = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Panel _secretsPage = new() { Dock = DockStyle.Fill };
    private readonly Panel _clipboardPage = new() { Dock = DockStyle.Fill };

    private List<SecretEntry> _currentSecrets = [];
    private List<ClipboardEntry> _currentClipboardEntries = [];

    public event EventHandler? AddSecretRequested;
    public event Action<SecretEntry>? SetPrimarySecretRequested;
    public event Action<SecretEntry>? EditSecretRequested;
    public event Action<SecretEntry>? DeleteSecretRequested;
    public event Action<SecretEntry>? TypeSecretRequested;
    public event Action<ClipboardEntry>? ClipboardReuseRequested;
    public event EventHandler? SaveSettingsRequested;
    public event EventHandler? ExportSecretsRequested;
    public event EventHandler? ImportSecretsRequested;
    public event EventHandler? UnlockRequested;
    public event EventHandler? LockRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? SetUnlockPatternRequested;
    public event Action<SecretEntry, SecretFieldKind>? InsertSecretFieldRequested;

    public MainForm()
    {
        Text = "PassTypePro";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(920, 760);
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
        _setPatternButton.Click += (_, _) => SetUnlockPatternRequested?.Invoke(this, EventArgs.Empty);
        _insertUsernameButton.Click += (_, _) => TriggerInsertField(SecretFieldKind.Username);
        _insertSecretButton.Click += (_, _) => TriggerInsertField(SecretFieldKind.Secret);
        _insertTotpButton.Click += (_, _) => TriggerInsertField(SecretFieldKind.Totp);

        MainMenuStrip = BuildMenuStrip();
        ConfigureGrids();
        BuildPages();

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12, 12, 12, 0),
            BackColor = _backgroundColor
        };
        topPanel.Controls.Add(_lockStatusLabel);
        topPanel.Controls.Add(_unlockButton);
        topPanel.Controls.Add(_lockButton);

        var tabsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12, 8, 12, 0),
            BackColor = _backgroundColor
        };
        tabsPanel.Controls.Add(_generalTabButton);
        tabsPanel.Controls.Add(_secretsTabButton);
        tabsPanel.Controls.Add(_clipboardTabButton);

        _contentHost.BackColor = _backgroundColor;
        _contentHost.Padding = new Padding(12);

        Controls.Add(_contentHost);
        Controls.Add(tabsPanel);
        Controls.Add(topPanel);
        Controls.Add(MainMenuStrip);

        ShowPage(_generalPage);
        ApplyDarkTheme(this);
        ConfigureInsertButtons();
    }

    public Func<SecretEntry, string>? TotpPreviewProvider { get; set; }

    public void Bind(AppConfig config, IReadOnlyList<SecretEntry> secrets, IReadOnlyList<ClipboardEntry> clipboardEntries, bool isLocked)
    {
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

        _currentSecrets = secrets.ToList();
        _currentClipboardEntries = clipboardEntries.ToList();

        _secretGrid.DataSource = null;
        _secretGrid.DataSource = _currentSecrets;
        _clipboardGrid.DataSource = null;
        _clipboardGrid.DataSource = _currentClipboardEntries
            .Select(entry => new ClipboardEntry(Shorten(entry.Content), entry.CapturedAt))
            .ToList();

        RestoreSecretSelection(selectedSecretId);
        RestoreClipboardSelection(selectedClipboardTimestamp);
        ApplySecretRowStyles();

        _lockStatusLabel.Text = isLocked ? "Status: gesperrt" : "Status: entsperrt";
        _unlockButton.Enabled = isLocked;
        _lockButton.Enabled = !isLocked && config.LockEnabled;
        UpdateTotpPreview();
        ApplyTouchMode(config.TouchModeEnabled);
        ApplyDarkTheme(this);
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

    private void ConfigureGrids()
    {
        ConfigureGrid(_secretGrid);
        ConfigureGrid(_clipboardGrid);

        _secretGrid.SelectionChanged += (_, _) => UpdateTotpPreview();

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
            FillWeight = 33
        });
        _secretGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Hotkey",
            DataPropertyName = nameof(SecretEntry.SecretHotkey),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 25
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
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Beenden", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        var actionMenu = new ToolStripMenuItem("Aktionen");
        actionMenu.DropDownItems.Add("Entsperren", null, (_, _) => UnlockRequested?.Invoke(this, EventArgs.Empty));
        actionMenu.DropDownItems.Add("Sperren", null, (_, _) => LockRequested?.Invoke(this, EventArgs.Empty));
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
    }

    private void BuildGeneralPage()
    {
        var saveSettingsButton = new Button { Text = "Einstellungen speichern", AutoSize = true };
        saveSettingsButton.Click += (_, _) => SaveSettingsRequested?.Invoke(this, EventArgs.Empty);

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
        panel.Controls.Add(_newPinTextBox, 1, 10);
        panel.Controls.Add(new Label { Text = "Leer lassen = bestehende PIN behalten", AutoSize = true }, 1, 11);

        var buttonPanel = new FlowLayoutPanel { AutoSize = true, BackColor = _backgroundColor };
        buttonPanel.Controls.Add(saveSettingsButton);
        buttonPanel.Controls.Add(_setPatternButton);
        panel.Controls.Add(buttonPanel, 1, 12);

        _generalPage.Controls.Clear();
        _generalPage.BackColor = _backgroundColor;
        _generalPage.Controls.Add(panel);
    }

    private void BuildSecretsPage()
    {
        var addButton = new Button { Text = "Neu", AutoSize = true };
        var setPrimaryButton = new Button { Text = "Als Standard setzen", AutoSize = true };
        var editButton = new Button { Text = "Bearbeiten", AutoSize = true };
        var deleteButton = new Button { Text = "Loeschen", AutoSize = true };
        var typeButton = new Button { Text = "Profil tippen", AutoSize = true };

        addButton.Click += (_, _) => AddSecretRequested?.Invoke(this, EventArgs.Empty);
        setPrimaryButton.Click += (_, _) =>
        {
            var secret = GetSelectedSecret();
            if (secret is not null)
            {
                SetPrimarySecretRequested?.Invoke(secret);
            }
        };
        editButton.Click += (_, _) =>
        {
            var secret = GetSelectedSecret();
            if (secret is not null)
            {
                EditSecretRequested?.Invoke(secret);
            }
        };
        deleteButton.Click += (_, _) =>
        {
            var secret = GetSelectedSecret();
            if (secret is not null)
            {
                DeleteSecretRequested?.Invoke(secret);
            }
        };
        typeButton.Click += (_, _) =>
        {
            var secret = GetSelectedSecret();
            if (secret is not null)
            {
                TypeSecretRequested?.Invoke(secret);
            }
        };

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = _backgroundColor };
        buttonPanel.Controls.Add(addButton);
        buttonPanel.Controls.Add(setPrimaryButton);
        buttonPanel.Controls.Add(editButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(typeButton);

        var detailsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8),
            BackColor = _backgroundColor
        };
        detailsPanel.Controls.Add(new Label { Text = "Aktueller TOTP:", AutoSize = true });
        detailsPanel.Controls.Add(_totpPreviewLabel);

        var insertPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(0, 14, 0, 0),
            BackColor = _backgroundColor
        };
        insertPanel.Controls.Add(_insertUsernameButton);
        insertPanel.Controls.Add(_insertSecretButton);
        insertPanel.Controls.Add(_insertTotpButton);

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = _backgroundColor };
        panel.Controls.Add(_secretGrid);
        panel.Controls.Add(detailsPanel);
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

    private void ShowPage(Panel page)
    {
        _contentHost.SuspendLayout();
        _contentHost.Controls.Clear();
        _contentHost.Controls.Add(page);
        _contentHost.ResumeLayout();

        UpdateTabButtonState(_generalTabButton, page == _generalPage);
        UpdateTabButtonState(_secretsTabButton, page == _secretsPage);
        UpdateTabButtonState(_clipboardTabButton, page == _clipboardPage);
    }

    private void UpdateTabButtonState(Button button, bool isActive)
    {
        button.BackColor = isActive ? _panelColor : _surfaceColor;
        button.ForeColor = _textColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = isActive ? _accentColor : _panelColor;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font(Font, isActive ? FontStyle.Bold : FontStyle.Regular);
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
                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = _surfaceColor;
                    numericUpDown.ForeColor = _textColor;
                    break;
                case Button button:
                    if (button != _generalTabButton && button != _secretsTabButton && button != _clipboardTabButton)
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
        ConfigureInsertButtons();
    }

    private void ApplyTouchMode(bool enabled)
    {
        var buttonHeight = enabled ? 64 : 42;
        var buttonWidth = enabled ? 148 : 126;
        var rowHeight = enabled ? 44 : 28;
        var font = new Font(Font.FontFamily, enabled ? 11f : 9f, FontStyle.Regular);
        var insertButtonSize = enabled ? new Size(148, 100) : new Size(126, 78);

        foreach (var button in new[] { _insertUsernameButton, _insertSecretButton, _insertTotpButton, _generalTabButton, _secretsTabButton, _clipboardTabButton, _unlockButton, _lockButton })
        {
            button.Height = buttonHeight;
            button.Font = new Font(Font.FontFamily, enabled ? 11f : 9f, button.Font.Style);
        }

        foreach (var button in new[] { _insertUsernameButton, _insertSecretButton, _insertTotpButton })
        {
            button.Size = insertButtonSize;
            button.Margin = enabled ? new Padding(0, 0, 14, 0) : new Padding(0, 0, 10, 0);
        }

        foreach (var button in new[] { _generalTabButton, _secretsTabButton, _clipboardTabButton, _unlockButton, _lockButton })
        {
            button.Width = Math.Max(buttonWidth, button.PreferredSize.Width + 18);
        }

        _secretGrid.RowTemplate.Height = rowHeight;
        _clipboardGrid.RowTemplate.Height = rowHeight;
        _secretGrid.DefaultCellStyle.Font = font;
        _clipboardGrid.DefaultCellStyle.Font = font;
        _secretGrid.ColumnHeadersHeight = enabled ? 42 : 34;
        _clipboardGrid.ColumnHeadersHeight = enabled ? 42 : 34;
        ConfigureInsertButtons();
    }

    private void ConfigureInsertButtons()
    {
        ConfigureInsertButton(_insertUsernameButton, CreateUserIcon(28, _accentColor), "USERNAME");
        ConfigureInsertButton(_insertSecretButton, CreateLockIcon(28, _accentColor), "SECRET");
        ConfigureInsertButton(_insertTotpButton, CreateTotpIcon(28, _accentColor), "TOTP");
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
        graphics.FillEllipse(new SolidBrush(Color.FromArgb(20, 24, 28)), size * 0.44f, size * 0.53f, size * 0.12f, size * 0.12f);
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
        if (_secretGrid.SelectedRows.Count > 0)
        {
            return _secretGrid.SelectedRows[0].DataBoundItem as SecretEntry;
        }

        return _secretGrid.CurrentRow?.DataBoundItem as SecretEntry;
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
