using System.Drawing;
using System.Windows.Forms;

namespace PassTypePro.UI;

public sealed class PinPromptForm : Form
{
    private readonly Color _backgroundColor = Color.FromArgb(20, 24, 28);
    private readonly Color _surfaceColor = Color.FromArgb(29, 35, 41);
    private readonly Color _panelColor = Color.FromArgb(35, 42, 50);
    private readonly Color _textColor = Color.FromArgb(231, 237, 243);
    private readonly Color _accentColor = Color.FromArgb(88, 166, 255);
    private readonly ToolTip _toolTip = new();
    private readonly TextBox _pinTextBox = new() { Width = 220, UseSystemPasswordChar = true };

    public string Pin => _pinTextBox.Text;

    public PinPromptForm()
    {
        Text = "PassTypePro entsperren";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var okButton = new Button { Text = "Entsperren", DialogResult = DialogResult.OK, AutoSize = true };
        var cancelButton = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var pinHost = PasswordRevealHelper.CreateRevealHost(
            _pinTextBox,
            _backgroundColor,
            _panelColor,
            _textColor,
            _accentColor,
            _toolTip);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12),
            ColumnCount = 1
        };

        layout.Controls.Add(new Label { Text = "PIN eingeben", AutoSize = true }, 0, 0);
        layout.Controls.Add(pinHost, 0, 1);
        layout.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(layout);
        ApplyDarkTheme(layout, buttonPanel, okButton, cancelButton);
        Shown += (_, _) =>
        {
            Activate();
            _pinTextBox.Focus();
        };
    }

    private void ApplyDarkTheme(TableLayoutPanel layout, FlowLayoutPanel buttonPanel, Button okButton, Button cancelButton)
    {
        BackColor = _backgroundColor;
        ForeColor = _textColor;
        _pinTextBox.BackColor = _surfaceColor;
        _pinTextBox.ForeColor = _textColor;
        _pinTextBox.BorderStyle = BorderStyle.FixedSingle;
        layout.BackColor = _backgroundColor;
        layout.ForeColor = _textColor;
        buttonPanel.BackColor = _backgroundColor;

        foreach (Control control in layout.Controls)
        {
            control.BackColor = control is Label ? Color.Transparent : _backgroundColor;
            control.ForeColor = _textColor;
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
