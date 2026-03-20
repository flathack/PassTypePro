using System.Windows.Forms;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed class ClipboardHistoryService
{
    private readonly List<ClipboardEntry> _entries = [];
    private readonly int _maxEntries;
    private string? _lastCaptured;

    public ClipboardHistoryService(int maxEntries)
    {
        _maxEntries = Math.Max(1, maxEntries);
    }

    public IReadOnlyList<ClipboardEntry> Entries => _entries;

    public bool CaptureCurrentClipboardText()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                return false;
            }

            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text) || string.Equals(text, _lastCaptured, StringComparison.Ordinal))
            {
                return false;
            }

            _entries.RemoveAll(entry => string.Equals(entry.Content, text, StringComparison.Ordinal));
            _entries.Insert(0, new ClipboardEntry(text, DateTimeOffset.Now));
            _lastCaptured = text;

            if (_entries.Count > _maxEntries)
            {
                _entries.RemoveRange(_maxEntries, _entries.Count - _maxEntries);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void CopyToClipboard(ClipboardEntry entry)
    {
        try
        {
            Clipboard.SetText(entry.Content);
            _lastCaptured = entry.Content;
        }
        catch
        {
        }
    }

    public void Clear()
    {
        _entries.Clear();
        _lastCaptured = null;
    }
}
