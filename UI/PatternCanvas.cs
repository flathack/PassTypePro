using System.Drawing;
using System.Windows.Forms;

namespace PassTypePro.UI;

public sealed class PatternCanvas : Control
{
    private readonly List<int> _pattern = [];
    private readonly PointF[] _points = new PointF[9];
    private bool _tracking;
    private Point _currentPoint;

    public PatternCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(20, 24, 28);
    }

    public string GetPattern() => string.Join('-', _pattern);

    public void ResetPattern()
    {
        _pattern.Clear();
        _tracking = false;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var spacingX = Width / 4f;
        var spacingY = Height / 4f;

        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                _points[row * 3 + col] = new PointF(spacingX * (col + 1), spacingY * (row + 1));
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _pattern.Clear();
        _tracking = true;
        _currentPoint = e.Location;
        TryAddPoint(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_tracking)
        {
            return;
        }

        _currentPoint = e.Location;
        TryAddPoint(e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _tracking = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var linePen = new Pen(Color.FromArgb(88, 166, 255), 8);
        using var outerBrush = new SolidBrush(Color.FromArgb(35, 42, 50));
        using var activeBrush = new SolidBrush(Color.FromArgb(88, 166, 255));
        using var innerBrush = new SolidBrush(Color.FromArgb(20, 24, 28));

        for (var i = 1; i < _pattern.Count; i++)
        {
            e.Graphics.DrawLine(linePen, _points[_pattern[i - 1]], _points[_pattern[i]]);
        }

        if (_tracking && _pattern.Count > 0)
        {
            e.Graphics.DrawLine(linePen, _points[_pattern[^1]], _currentPoint);
        }

        for (var i = 0; i < _points.Length; i++)
        {
            var point = _points[i];
            var active = _pattern.Contains(i);
            var size = active ? 28 : 22;
            var outerRect = new RectangleF(point.X - size / 2f, point.Y - size / 2f, size, size);
            var innerRect = new RectangleF(point.X - 7, point.Y - 7, 14, 14);
            e.Graphics.FillEllipse(active ? activeBrush : outerBrush, outerRect);
            e.Graphics.FillEllipse(innerBrush, innerRect);
        }
    }

    private void TryAddPoint(Point location)
    {
        for (var i = 0; i < _points.Length; i++)
        {
            if (_pattern.Contains(i))
            {
                continue;
            }

            var point = _points[i];
            var distance = Math.Sqrt(Math.Pow(location.X - point.X, 2) + Math.Pow(location.Y - point.Y, 2));
            if (distance <= 24)
            {
                _pattern.Add(i);
                break;
            }
        }
    }
}
