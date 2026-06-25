using System;
using System.Threading;
using System.Threading.Tasks;
using Supabase;

namespace FocusFlowFinal.Services.Supabase;

public class SupabaseClientProvider
{
    private global::Supabase.Client? _client;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsInitialized => _client != null;

    public async Task<global::Supabase.Client?> GetClientAsync()
    {
        if (_client != null) return _client;

        await _lock.WaitAsync();
        try
        {
            if (_client != null) return _client;

            var cfg = SupabaseConfig.Load();
            if (!cfg.IsConfigured) return null;

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true
            };
            _client = new global::Supabase.Client(cfg.Url, cfg.AnonKey, options);
            await _client.InitializeAsync();
        }
        catch (Exception)
        {
            _client = null;
        }
        finally
        {
            _lock.Release();
        }

        return _client;
    }
}
