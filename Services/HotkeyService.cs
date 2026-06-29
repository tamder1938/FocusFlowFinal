using System;
using System.Collections.Generic;
using Avalonia.Input;
using FocusFlowFinal.Models;

namespace FocusFlowFinal.Services;

public static class HotkeyService
{
    public static readonly Dictionary<string, string> Defaults = new()
    {
        ["Day"]     = "Ctrl+D",
        ["Week"]    = "Ctrl+W",
        ["Month"]   = "Ctrl+M",
        ["Year"]    = "Ctrl+Y",
        ["NewTask"] = "Ctrl+N",
        ["Today"]   = "Ctrl+T",
    };

    public static event EventHandler? Changed;

    public static Dictionary<string, string> GetAll()
    {
        var s = AppSettings.Load();
        return new()
        {
            ["Day"]     = Coerce(s.HotkeyDay,     "Day"),
            ["Week"]    = Coerce(s.HotkeyWeek,    "Week"),
            ["Month"]   = Coerce(s.HotkeyMonth,   "Month"),
            ["Year"]    = Coerce(s.HotkeyYear,    "Year"),
            ["NewTask"] = Coerce(s.HotkeyNewTask, "NewTask"),
            ["Today"]   = Coerce(s.HotkeyToday,   "Today"),
        };
    }

    public static string Get(string action)
    {
        var s = AppSettings.Load();
        return action switch
        {
            "Day"     => Coerce(s.HotkeyDay,     action),
            "Week"    => Coerce(s.HotkeyWeek,    action),
            "Month"   => Coerce(s.HotkeyMonth,   action),
            "Year"    => Coerce(s.HotkeyYear,    action),
            "NewTask" => Coerce(s.HotkeyNewTask, action),
            "Today"   => Coerce(s.HotkeyToday,   action),
            _         => string.Empty,
        };
    }

    // Returns null if all bindings are valid and conflict-free, or an error message.
    public static string? Validate(Dictionary<string, string> bindings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (action, gesture) in bindings)
        {
            if (string.IsNullOrWhiteSpace(gesture))
                return $"{action}: empty";
            try { KeyGesture.Parse(gesture); }
            catch { return $"Hotkeys_Invalid: {gesture}"; }
            if (!seen.Add(gesture.Replace(" ", "")))
                return "Hotkeys_Conflict";
        }
        return null;
    }

    public static void SaveAll(Dictionary<string, string> bindings)
    {
        var s = AppSettings.Load();
        if (bindings.TryGetValue("Day",     out var v)) s.HotkeyDay     = v;
        if (bindings.TryGetValue("Week",    out v))     s.HotkeyWeek    = v;
        if (bindings.TryGetValue("Month",   out v))     s.HotkeyMonth   = v;
        if (bindings.TryGetValue("Year",    out v))     s.HotkeyYear    = v;
        if (bindings.TryGetValue("NewTask", out v))     s.HotkeyNewTask = v;
        if (bindings.TryGetValue("Today",   out v))     s.HotkeyToday   = v;
        s.Save();
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void ResetToDefaults() => SaveAll(new Dictionary<string, string>(Defaults));

    private static string Coerce(string? stored, string action)
        => string.IsNullOrWhiteSpace(stored) ? Defaults[action] : stored;
}
