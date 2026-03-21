using System.Drawing;
using System.Windows.Forms;
using PassTypePro.Models;
using PassTypePro.Services;

namespace PassTypePro.UI;

public sealed class SecretEditForm : Form
{
    private readonly Color _backgroundColor = Color.FromArgb(20, 24, 28);
    private readonly Color _surfaceColor = Color.FromArgb(29, 35, 41);
    private readonly Color _panelColor = Color.FromArgb(35, 42, 50);
    private readonly Color _textColor = Color.FromArgb(231, 237, 243);
    private readonly Color _mutedTextColor = Color.FromArgb(158, 170, 182);
    private readonly Color _accentColor = Color.FromArgb(88, 166, 255);

    private readonly TotpService _totpService = new();
    private readonly TextBox _nameTextBox = new() { Width = 380 };
    private readonly TextBox _usernameTextBox = new() { Width = 380 };
    private readonly TextBox _valueTextBox = new() { Width = 380, UseSystemPasswordChar = true };
    private readonly TextBox _totpSeedTextBox = new() { Width = 380 };
    private readonly TextBox _sequenceTextBox = new() { Width = 380 };
    private readonly HotkeyTextBox _secretHotkeyTextBox = new() { Width = 200 };
    private readonly NumericUpDown _startDelayNumeric = new() { Minimum = 0, Maximum = 10000, Increment = 50, Width = 100 };
    private readonly NumericUpDown _keystrokeDelayNumeric = new() { Minimum = 0, Maximum = 1000, Increment = 10, Width = 100 };
    private readonly CheckBox _primaryCheckBox = new() { Text = "Als Standard-Secret verwenden", AutoSize = true };
    private readonly ToolTip _toolTip = new();

    public SecretEntry? Secret { get; private set; }

    public SecretEditForm(SecretEntry? existing = null)
    {
        Text = existing is null ? "Secret anlegen" : "Secret bearbeiten";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(620, 700);
        Padding = new Padding(1);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "passtypepro-lock-dark.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }

        var okButton = new Button { Text = "Speichern", DialogResult = DialogResult.OK, AutoSize = true };
        var cancelButton = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        if (existing is not null)
        {
            _nameTextBox.Text = existing.Name;
            _usernameTextBox.Text = existing.Username;
            _valueTextBox.Text = existing.Value;
            _totpSeedTextBox.Text = existing.TotpSeed;
            _sequenceTextBox.Text = existing.SequenceTemplate;
            _secretHotkeyTextBox.SetHotkey(existing.SecretHotkey);
            _startDelayNumeric.Value = existing.StartDelayMs;
            _keystrokeDelayNumeric.Value = existing.KeystrokeDelayMs;
            _primaryCheckBox.Checked = existing.IsPrimary;
            Secret = new SecretEntry
            {
                Id = existing.Id,
                Name = existing.Name,
                Username = existing.Username,
                Value = existing.Value,
                TotpSeed = existing.TotpSeed,
                SequenceTemplate = existing.SequenceTemplate,
                SecretHotkey = existing.SecretHotkey,
                StartDelayMs = existing.StartDelayMs,
                KeystrokeDelayMs = existing.KeystrokeDelayMs,
                IsPrimary = existing.IsPrimary
            };
        }
        else
        {
            _sequenceTextBox.Text = "{SECRET}";
            _startDelayNumeric.Value = 150;
            _keystrokeDelayNumeric.Value = 0;
        }
        _totpSeedTextBox.PlaceholderText = "Base32 TOTP-Seed, z. B. JBSWY3DPEHPK3PXP";

        var tokenPanel = BuildTokenPanel();

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            ColumnCount = 2,
            BackColor = _backgroundColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        content.Controls.Add(new Label { Text = "Name", AutoSize = true }, 0, 0);
        content.Controls.Add(_nameTextBox, 1, 0);
        content.Controls.Add(new Label { Text = "Benutzername", AutoSize = true }, 0, 1);
        content.Controls.Add(_usernameTextBox, 1, 1);
        content.Controls.Add(new Label { Text = "Secret", AutoSize = true }, 0, 2);
        content.Controls.Add(_valueTextBox, 1, 2);
        content.Controls.Add(new Label { Text = "TOTP-Seed", AutoSize = true }, 0, 3);
        content.Controls.Add(_totpSeedTextBox, 1, 3);
        content.Controls.Add(new Label { Text = "Secret-Hotkey", AutoSize = true }, 0, 5);
        content.Controls.Add(_secretHotkeyTextBox, 1, 5);
        content.Controls.Add(new Label { Text = "Startverzoegerung", AutoSize = true }, 0, 7);
        content.Controls.Add(_startDelayNumeric, 1, 7);
        content.Controls.Add(new Label { Text = "Zeichenverzoegerung", AutoSize = true }, 0, 9);
        content.Controls.Add(_keystrokeDelayNumeric, 1, 9);
        content.Controls.Add(new Label { Text = "Typing-Sequenz", AutoSize = true }, 0, 11);
        content.Controls.Add(_sequenceTextBox, 1, 11);
        content.Controls.Add(tokenPanel, 1, 12);
        content.Controls.Add(_primaryCheckBox, 1, 13);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            BackColor = _backgroundColor
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);
        content.Controls.Add(buttonPanel, 1, 14);

        var hostPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = _backgroundColor
        };
        hostPanel.Controls.Add(content);

        Controls.Add(hostPanel);
        ApplyDarkTheme(content, okButton, cancelButton, tokenPanel, buttonPanel);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            if (!string.IsNullOrWhiteSpace(_totpSeedTextBox.Text) && !_totpService.CanGenerate(_totpSeedTextBox.Text))
            {
                MessageBox.Show(this, "Der TOTP-Seed ist ungueltig.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_secretHotkeyTextBox.HotkeyText) &&
                !HotkeyDefinition.TryParse(_secretHotkeyTextBox.HotkeyText, out _))
            {
                MessageBox.Show(this, "Der Secret-Hotkey ist ungueltig.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            Secret = Secret ?? new SecretEntry();
            Secret.Name = BuildEntryName();
            Secret.Username = _usernameTextBox.Text.Trim();
            Secret.Value = _valueTextBox.Text;
            Secret.TotpSeed = _totpSeedTextBox.Text.Trim();
            Secret.SecretHotkey = _secretHotkeyTextBox.HotkeyText.Trim();
            Secret.StartDelayMs = Decimal.ToInt32(_startDelayNumeric.Value);
            Secret.KeystrokeDelayMs = Decimal.ToInt32(_keystrokeDelayNumeric.Value);
            Secret.SequenceTemplate = string.IsNullOrWhiteSpace(_sequenceTextBox.Text)
                ? BuildDefaultSequence()
                : _sequenceTextBox.Text.Trim();
            Secret.IsPrimary = _primaryCheckBox.Checked;
        }

        base.OnFormClosing(e);
    }

    private string BuildEntryName()
    {
        var name = _nameTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            return _usernameTextBox.Text.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_totpSeedTextBox.Text))
        {
            return "TOTP-Eintrag";
        }

        if (!string.IsNullOrWhiteSpace(_valueTextBox.Text))
        {
            return "Secret-Eintrag";
        }

        return "Neuer Eintrag";
    }

    private string BuildDefaultSequence()
    {
        if (!string.IsNullOrWhiteSpace(_valueTextBox.Text))
        {
            return "{SECRET}";
        }

        if (!string.IsNullOrWhiteSpace(_totpSeedTextBox.Text))
        {
            return "{TOTP}";
        }

        if (!string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            return "{USERNAME}";
        }

        return "{SECRET}";
    }

    private FlowLayoutPanel BuildTokenPanel()
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            BackColor = _backgroundColor,
            Margin = new Padding(0, 4, 0, 8)
        };

        panel.Controls.Add(CreateTokenButton("U", "{USERNAME}", "Benutzername"));
        panel.Controls.Add(CreateTokenButton("P", "{SECRET}", "Secret"));
        panel.Controls.Add(CreateTokenButton("2", "{TOTP}", "TOTP"));
        panel.Controls.Add(CreateTokenButton("->|", "{TAB}", "Tab"));
        panel.Controls.Add(CreateTokenButton(">|", "{ENTER}", "Enter"));
        panel.Controls.Add(CreateTokenButton("_", "{SPACE}", "Space"));
        panel.Controls.Add(CreateTokenButton("T", "{TEXT:abc}", "Text"));
        panel.Controls.Add(CreateTokenButton("D", "{DELAY:500}", "Delay"));

        return panel;
    }

    private Button CreateTokenButton(string caption, string token, string tooltipText)
    {
        var button = new Button
        {
            Text = caption,
            Width = 56,
            Height = 36,
            Margin = new Padding(0, 0, 8, 8),
            Tag = token
        };

        button.Click += (_, _) => InsertToken((string)button.Tag!);
        _toolTip.SetToolTip(button, $"{tooltipText}: {token}");
        return button;
    }

    private void InsertToken(string token)
    {
        var insertionIndex = _sequenceTextBox.SelectionStart;
        var current = _sequenceTextBox.Text;
        _sequenceTextBox.Text = current.Insert(insertionIndex, token);
        _sequenceTextBox.SelectionStart = insertionIndex + token.Length;
        _sequenceTextBox.SelectionLength = 0;
        _sequenceTextBox.Focus();
    }

    private void ApplyDarkTheme(
        TableLayoutPanel content,
        Button okButton,
        Button cancelButton,
        FlowLayoutPanel tokenPanel,
        FlowLayoutPanel buttonPanel)
    {
        BackColor = _backgroundColor;
        ForeColor = _textColor;
        content.BackColor = _backgroundColor;
        content.ForeColor = _textColor;
        buttonPanel.BackColor = _backgroundColor;
        tokenPanel.BackColor = _backgroundColor;

        foreach (var textBox in new Control[] { _nameTextBox, _usernameTextBox, _valueTextBox, _totpSeedTextBox, _sequenceTextBox, _secretHotkeyTextBox })
        {
            textBox.BackColor = _surfaceColor;
            textBox.ForeColor = _textColor;
        }

        foreach (var numeric in new[] { _startDelayNumeric, _keystrokeDelayNumeric })
        {
            numeric.BackColor = _surfaceColor;
            numeric.ForeColor = _textColor;
        }

        _primaryCheckBox.BackColor = _backgroundColor;
        _primaryCheckBox.ForeColor = _textColor;

        foreach (Control control in content.Controls)
        {
            if (control is Label label)
            {
                label.BackColor = Color.Transparent;
                label.ForeColor = _textColor;
            }
        }

        foreach (var button in tokenPanel.Controls.OfType<Button>().Concat(new[] { okButton, cancelButton }))
        {
            button.BackColor = _panelColor;
            button.ForeColor = _textColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _accentColor;
        }
    }
}
