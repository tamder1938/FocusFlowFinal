using LiteDB;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FocusFlowFinal.Services.Security;

public static class LiteDbEncryption
{
    private const string KeyFile = "dbkey.bin";

    private static readonly string SecureDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FocusFlow", "Secure");

    /// <summary>
    /// Returns (or generates) the 32-byte hex password for the LiteDB file.
    /// Stored encrypted via DPAPI in %APPDATA%/FocusFlow/Secure/dbkey.bin.
    /// Returns null if DPAPI is unavailable (fallback = unencrypted DB).
    /// </summary>
    public static string? GetOrCreateDbPassword()
    {
        Directory.CreateDirectory(SecureDir);
        var keyPath = Path.Combine(SecureDir, KeyFile);

        try
        {
            if (File.Exists(keyPath))
            {
                var encrypted = File.ReadAllBytes(keyPath);
                var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }

            // First run: generate 32 random bytes → hex string password
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            var password = Convert.ToHexString(key);

            var encryptedNew = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(keyPath, encryptedNew);

            return password;
        }
        catch
        {
            // DPAPI unavailable (running as different user, etc.) — use unencrypted DB
            return null;
        }
    }

    /// <summary>
    /// Opens an encrypted LiteDB. Migrates from unencrypted if needed.
    /// If the password is null, returns a plain LiteDatabase.
    /// </summary>
    public static LiteDatabase Open(string dbPath, string? password)
    {
        if (string.IsNullOrEmpty(password))
            return new LiteDatabase(dbPath);

        var encPath = dbPath; // encrypted DB lives at the same path
        var plainPath = dbPath + ".plain_backup";

        // If unencrypted DB exists and no encrypted version → migrate
        if (File.Exists(dbPath) && !IsEncrypted(dbPath, password))
        {
            if (!TryMigrate(dbPath, plainPath, password))
            {
                // Migration failed — open without password (data safety first)
                return new LiteDatabase(dbPath);
            }
        }

        return new LiteDatabase($"Filename={encPath};Password={password}");
    }

    private static bool IsEncrypted(string path, string password)
    {
        try
        {
            using var db = new LiteDatabase($"Filename={path};Password={password}");
            _ = db.GetCollectionNames(); // triggers read to verify password
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryMigrate(string dbPath, string backupPath, string password)
    {
        var tmpPath = dbPath + ".enc_tmp";
        try
        {
            // Copy old unencrypted data to a temp encrypted DB
            using (var plain = new LiteDatabase(dbPath))
            using (var enc = new LiteDatabase($"Filename={tmpPath};Password={password}"))
            {
                foreach (var name in plain.GetCollectionNames())
                {
                    var src = plain.GetCollection<BsonDocument>(name);
                    var dst = enc.GetCollection<BsonDocument>(name);
                    foreach (var doc in src.FindAll())
                        dst.Upsert(doc);
                }
            }

            // Rename old plain DB to backup, put encrypted in place
            File.Move(dbPath, backupPath, overwrite: true);
            File.Move(tmpPath, dbPath, overwrite: true);
            return true;
        }
        catch
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            return false;
        }
    }
}
