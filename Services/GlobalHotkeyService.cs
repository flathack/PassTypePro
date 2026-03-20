using System.Windows.Forms;
using PassTypePro.Models;
using PassTypePro.Native;

namespace PassTypePro.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private readonly NativeWindow _window;
    private readonly Dictionary<int, Action> _callbacks = [];
    private int _nextId = 1;

    public GlobalHotkeyService(NativeWindow window)
    {
        _window = window;
    }

    public int Register(HotkeyDefinition hotkey, Action callback)
    {
        var id = _nextId++;
        var success = NativeMethods.RegisterHotKey(
            _window.Handle,
            id,
            (uint)hotkey.Modifiers,
            (uint)hotkey.Key);

        if (!success)
        {
            throw new InvalidOperationException($"Hotkey registration failed for {hotkey.DisplayText}.");
        }

        _callbacks[id] = callback;
        return id;
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys.ToArray())
        {
            NativeMethods.UnregisterHotKey(_window.Handle, id);
        }

        _callbacks.Clear();
    }

    public bool TryHandle(Message message)
    {
        if (message.Msg != NativeConstants.WmHotkey)
        {
            return false;
        }

        var id = message.WParam.ToInt32();
        if (_callbacks.TryGetValue(id, out var callback))
        {
            callback();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}
