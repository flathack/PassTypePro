using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PassTypePro.UI;

internal static class PasswordRevealHelper
{
    public static Panel CreateRevealHost(
        TextBox textBox,
        Color backgroundColor,
        Color panelColor,
        Color textColor,
        Color accentColor,
        ToolTip toolTip)
    {
        var toggleButton = new Button
        {
            Dock = DockStyle.Right,
            Width = 34,
            Margin = Padding.Empty,
            FlatStyle = FlatStyle.Flat,
            BackColor = panelColor,
            ForeColor = textColor,
            TabStop = false
        };
        toggleButton.FlatAppearance.BorderSize = 1;
        toggleButton.FlatAppearance.BorderColor = accentColor;

        var host = new Panel
        {
            Width = textBox.Width + toggleButton.Width + 8,
            Height = textBox.PreferredHeight + 2,
            BackColor = accentColor,
            Padding = new Padding(1),
            Margin = textBox.Margin
        };

        textBox.Dock = DockStyle.Fill;
        textBox.Margin = Padding.Empty;
        textBox.BorderStyle = BorderStyle.None;
        textBox.UseSystemPasswordChar = true;

        void ApplyState(bool visible)
        {
            textBox.UseSystemPasswordChar = !visible;
            var previousImage = toggleButton.Image;
            toggleButton.Image = CreateEyeIcon(18, accentColor, visible);
            previousImage?.Dispose();
            toolTip.SetToolTip(toggleButton, visible ? "Zeichen ausblenden" : "Zeichen anzeigen");
        }

        var isVisible = false;
        toggleButton.Click += (_, _) =>
        {
            isVisible = !isVisible;
            ApplyState(isVisible);
        };

        host.Controls.Add(textBox);
        host.Controls.Add(toggleButton);
        ApplyState(isVisible);
        return host;
    }

    private static Bitmap CreateEyeIcon(int size, Color color, bool visible)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var brush = new SolidBrush(color);

        graphics.DrawArc(pen, size * 0.08f, size * 0.26f, size * 0.84f, size * 0.48f, 15, 150);
        graphics.DrawArc(pen, size * 0.08f, size * 0.26f, size * 0.84f, size * 0.48f, 195, 150);
        graphics.FillEllipse(brush, size * 0.39f, size * 0.37f, size * 0.22f, size * 0.22f);

        if (visible)
        {
            graphics.DrawLine(pen, size * 0.18f, size * 0.82f, size * 0.82f, size * 0.18f);
        }

        return bitmap;
    }
}
