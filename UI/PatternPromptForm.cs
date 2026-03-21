using System.Drawing;
using System.Windows.Forms;

namespace PassTypePro.UI;

public sealed class PatternPromptForm : Form
{
    private readonly PatternCanvas _canvas = new();
    private readonly Label _hintLabel = new() { AutoSize = true, Text = "Entsperrmuster zeichnen" };
    private readonly bool _confirmRequired;
    private string? _firstPattern;

    public string? Pattern { get; private set; }

    public PatternPromptForm(string title, bool confirmRequired)
    {
        _confirmRequired = confirmRequired;

        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        ClientSize = new Size(360, 460);

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancelButton = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(12)
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        _canvas.Dock = DockStyle.Fill;

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(16, 16, 16, 0) };
        topPanel.Controls.Add(_hintLabel);

        Controls.Add(_canvas);
        Controls.Add(buttonPanel);
        Controls.Add(topPanel);

        ApplyDarkTheme(topPanel, buttonPanel, okButton, cancelButton);
        Shown += (_, _) => Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            var currentPattern = _canvas.GetPattern();
            if (string.IsNullOrWhiteSpace(currentPattern) || currentPattern.Split('-').Length < 4)
            {
                _hintLabel.Text = "Bitte mindestens 4 Punkte verbinden";
                e.Cancel = true;
                return;
            }

            if (_confirmRequired)
            {
                if (_firstPattern is null)
                {
                    _firstPattern = currentPattern;
                    _hintLabel.Text = "Muster zur Bestätigung erneut zeichnen";
                    _canvas.ResetPattern();
                    e.Cancel = true;
                    return;
                }

                if (!string.Equals(_firstPattern, currentPattern, StringComparison.Ordinal))
                {
                    _firstPattern = null;
                    _hintLabel.Text = "Muster stimmen nicht überein";
                    _canvas.ResetPattern();
                    e.Cancel = true;
                    return;
                }
            }

            Pattern = currentPattern;
        }

        base.OnFormClosing(e);
    }

    private void ApplyDarkTheme(Panel topPanel, FlowLayoutPanel buttonPanel, Button okButton, Button cancelButton)
    {
        var background = Color.FromArgb(20, 24, 28);
        var panel = Color.FromArgb(35, 42, 50);
        var text = Color.FromArgb(231, 237, 243);
        var accent = Color.FromArgb(88, 166, 255);

        BackColor = background;
        ForeColor = text;
        topPanel.BackColor = background;
        buttonPanel.BackColor = background;
        _hintLabel.ForeColor = text;

        foreach (var button in new[] { okButton, cancelButton })
        {
            button.BackColor = panel;
            button.ForeColor = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = accent;
        }
    }
}
