using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services.Security;

public class DpapiSecureStorage : ISecureStorage
{
    private readonly string _dir;

    public DpapiSecureStorage()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusFlow", "Secure");
        Directory.CreateDirectory(_dir);
    }

    public Task SetAsync(string key, string value)
    {
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath(key), encrypted);
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        var path = FilePath(key);
        if (!File.Exists(path)) return Task.FromResult<string?>(null);
        try
        {
            var encrypted = File.ReadAllBytes(path);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(plain));
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task RemoveAsync(string key)
    {
        var path = FilePath(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string FilePath(string key) =>
        Path.Combine(_dir, $"{Sanitize(key)}.bin");

    private static string Sanitize(string key) =>
        new string(key.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
}
