using System.Drawing;
using System.Windows.Forms;

namespace PassTypePro.UI;

public sealed class PassphrasePromptForm : Form
{
    private readonly Color _backgroundColor = Color.FromArgb(20, 24, 28);
    private readonly Color _surfaceColor = Color.FromArgb(29, 35, 41);
    private readonly Color _panelColor = Color.FromArgb(35, 42, 50);
    private readonly Color _textColor = Color.FromArgb(231, 237, 243);
    private readonly Color _accentColor = Color.FromArgb(88, 166, 255);
    private readonly TextBox _passphraseTextBox = new() { Width = 260, UseSystemPasswordChar = true };
    private readonly TextBox _confirmTextBox = new() { Width = 260, UseSystemPasswordChar = true };
    private readonly bool _confirmRequired;

    public string Passphrase => _passphraseTextBox.Text;

    public PassphrasePromptForm(string title, string label, bool confirmRequired)
    {
        _confirmRequired = confirmRequired;

        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancelButton = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12),
            ColumnCount = 2
        };

        layout.Controls.Add(new Label { Text = label, AutoSize = true }, 0, 0);
        layout.Controls.Add(_passphraseTextBox, 1, 0);

        if (_confirmRequired)
        {
            layout.Controls.Add(new Label { Text = "Passphrase bestaetigen", AutoSize = true }, 0, 1);
            layout.Controls.Add(_confirmTextBox, 1, 1);
        }

        var row = _confirmRequired ? 2 : 1;
        var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);
        layout.Controls.Add(buttonPanel, 1, row);

        Controls.Add(layout);
        ApplyDarkTheme(layout, buttonPanel, okButton, cancelButton);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(_passphraseTextBox.Text))
            {
                MessageBox.Show(this, "Eine Passphrase ist erforderlich.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            if (_confirmRequired && !string.Equals(_passphraseTextBox.Text, _confirmTextBox.Text, StringComparison.Ordinal))
            {
                MessageBox.Show(this, "Die Passphrasen stimmen nicht ueberein.", "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
        }

        base.OnFormClosing(e);
    }

    private void ApplyDarkTheme(TableLayoutPanel layout, FlowLayoutPanel buttonPanel, Button okButton, Button cancelButton)
    {
        BackColor = _backgroundColor;
        ForeColor = _textColor;
        layout.BackColor = _backgroundColor;
        layout.ForeColor = _textColor;
        buttonPanel.BackColor = _backgroundColor;

        foreach (var textBox in new[] { _passphraseTextBox, _confirmTextBox })
        {
            textBox.BackColor = _surfaceColor;
            textBox.ForeColor = _textColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        foreach (Control control in layout.Controls)
        {
            if (control is Label label)
            {
                label.BackColor = Color.Transparent;
                label.ForeColor = _textColor;
            }
        }

        foreach (var button in new[] { okButton, cancelButton })
        {
            button.BackColor = _panelColor;
            button.ForeColor = _textColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _accentColor;
        }
    }
}
