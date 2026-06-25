using System;
using System.IO;
using System.Text.Json;

namespace FocusFlowFinal.Services.Supabase;

public record SupabaseConfig(string Url, string AnonKey)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && Url != "YOUR_SUPABASE_URL" &&
        !string.IsNullOrWhiteSpace(AnonKey) && AnonKey != "YOUR_SUPABASE_ANON_KEY";

    public static SupabaseConfig Load()
    {
        string? url = null, anonKey = null;

        // appsettings.local.json wins over appsettings.json (local file is gitignored)
        TryLoad("appsettings.local.json", ref url, ref anonKey);
        TryLoad("appsettings.json", ref url, ref anonKey);

        return new SupabaseConfig(url ?? string.Empty, anonKey ?? string.Empty);
    }

    private static void TryLoad(string filename, ref string? url, ref string? anonKey)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, filename);
            if (!File.Exists(path)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Supabase", out var sb)) return;
            if (url == null && sb.TryGetProperty("Url", out var u))
                url = u.GetString();
            if (anonKey == null && sb.TryGetProperty("AnonKey", out var k))
                anonKey = k.GetString();
        }
        catch { }
    }
}
