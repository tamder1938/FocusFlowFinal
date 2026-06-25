using FocusFlowFinal.Models;
using FocusFlowFinal.Services.Security;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services.Supabase;

public class SupabaseAuthService : IAuthService
{
    private readonly SupabaseClientProvider _provider;
    private readonly ISecureStorage _secureStorage;
    private readonly ICurrentWorkspace _workspace;

    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public SupabaseAuthService(SupabaseClientProvider provider, ISecureStorage secureStorage, ICurrentWorkspace workspace)
    {
        _provider = provider;
        _secureStorage = secureStorage;
        _workspace = workspace;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail(LocalizationService.Instance["AuthFillAllFields"]);

        try
        {
            var client = await _provider.GetClientAsync();
            if (client == null)
                return AuthResult.Fail("Supabase не настроен. Заполните appsettings.local.json.");

            var session = await client.Auth.SignIn(email, password);
            if (session?.User == null)
                return AuthResult.Fail(LocalizationService.Instance["AuthInvalidCredentials"]);

            await PersistTokensAsync(session.AccessToken ?? string.Empty, session.RefreshToken ?? string.Empty);
            CurrentUser = BuildProfile(session.User);
            SaveSessionToSettings(email, CurrentUser);
            _workspace.SetOwner(CurrentUser.UserId);

            return AuthResult.Ok(CurrentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string username)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
            return AuthResult.Fail(LocalizationService.Instance["AuthFillAllFields"]);

        try
        {
            var client = await _provider.GetClientAsync();
            if (client == null)
                return AuthResult.Fail("Supabase не настроен. Заполните appsettings.local.json.");

            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object> { ["username"] = username }
            };
            var session = await client.Auth.SignUp(email, password, options);
            if (session?.User == null)
                return AuthResult.Fail("Регистрация не удалась. Проверьте данные или подтвердите email.");

            if (session.AccessToken != null)
                await PersistTokensAsync(session.AccessToken, session.RefreshToken ?? string.Empty);

            CurrentUser = BuildProfile(session.User);
            SaveSessionToSettings(email, CurrentUser);
            _workspace.SetOwner(CurrentUser.UserId);

            return AuthResult.Ok(CurrentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<bool> RestoreSessionAsync()
    {
        try
        {
            var accessToken = await _secureStorage.GetAsync("sb_access_token");
            var refreshToken = await _secureStorage.GetAsync("sb_refresh_token");
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                return false;

            var client = await _provider.GetClientAsync();
            if (client == null) return false;

            // SetSession refreshes the token if expired
            var session = await client.Auth.SetSession(accessToken, refreshToken, false);
            if (session?.User == null) return false;

            await PersistTokensAsync(session.AccessToken ?? accessToken, session.RefreshToken ?? refreshToken);
            CurrentUser = BuildProfile(session.User);
            _workspace.SetOwner(CurrentUser.UserId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var client = await _provider.GetClientAsync();
            if (client != null) await client.Auth.SignOut();
        }
        catch { }

        CurrentUser = null;
        _workspace.SetOwner(CurrentWorkspaceService.LocalOwner);

        await _secureStorage.RemoveAsync("sb_access_token");
        await _secureStorage.RemoveAsync("sb_refresh_token");

        var settings = AppSettings.Load();
        settings.AccountEmail = null;
        settings.AccountUsername = null;
        settings.SyncEnabled = false;
        settings.AuthToken = null;
        settings.Save();
    }

    public async Task<AuthResult> UpdateProfileAsync(string? newUsername, string? newEmail, string? newAvatarPath)
    {
        if (CurrentUser == null)
            return AuthResult.Fail(LocalizationService.Instance["AuthNotLoggedIn"]);

        try
        {
            var client = await _provider.GetClientAsync();
            if (client == null)
                return AuthResult.Fail("Нет соединения с Supabase.");

            var attrs = new UserAttributes();
            if (!string.IsNullOrWhiteSpace(newEmail)) attrs.Email = newEmail;

            var meta = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(newUsername)) meta["username"] = newUsername;
            if (newAvatarPath != null) meta["avatar_path"] = newAvatarPath;
            if (meta.Count > 0) attrs.Data = meta;

            var user = await client.Auth.Update(attrs);
            if (user == null) return AuthResult.Fail("Обновление профиля не удалось.");

            CurrentUser = BuildProfile(user);
            SaveSessionToSettings(CurrentUser.Email, CurrentUser);
            return AuthResult.Ok(CurrentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<AuthResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (CurrentUser == null)
            return AuthResult.Fail(LocalizationService.Instance["AuthNotLoggedIn"]);

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return AuthResult.Fail(LocalizationService.Instance["AuthFillAllFields"]);

        try
        {
            var client = await _provider.GetClientAsync();
            if (client == null)
                return AuthResult.Fail("Нет соединения с Supabase.");

            // Verify current password by re-signing in
            var check = await client.Auth.SignIn(CurrentUser.Email, currentPassword);
            if (check?.User == null)
                return AuthResult.Fail(LocalizationService.Instance["AuthWrongPassword"]);

            var attrs = new UserAttributes { Password = newPassword };
            var user = await client.Auth.Update(attrs);
            if (user == null) return AuthResult.Fail("Смена пароля не удалась.");

            return AuthResult.Ok(CurrentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    private static UserProfile BuildProfile(User user)
    {
        var meta = user.UserMetadata;
        bool isDeveloper = meta != null && meta.TryGetValue("is_developer", out var dev) && dev is true;
        bool hasFreeAccess = meta != null && meta.TryGetValue("has_free_access", out var free) && free is true;
        string username = meta != null && meta.TryGetValue("username", out var un) ? un?.ToString() ?? string.Empty : string.Empty;
        string? avatarPath = meta != null && meta.TryGetValue("avatar_path", out var av) ? av?.ToString() : null;

        var storedExpiry = AppSettings.Load().SubscriptionExpiryDate;
        SubscriptionInfo? sub = storedExpiry.HasValue && storedExpiry.Value > DateTime.UtcNow
            ? new SubscriptionInfo { ExpiresAtUtc = storedExpiry.Value }
            : null;

        return new UserProfile
        {
            UserId = user.Id ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Username = string.IsNullOrEmpty(username) ? (user.Email ?? string.Empty) : username,
            AvatarPath = avatarPath,
            IsDeveloper = isDeveloper,
            HasFreeAccess = hasFreeAccess,
            Subscription = sub
        };
    }

    private async Task PersistTokensAsync(string accessToken, string refreshToken)
    {
        await _secureStorage.SetAsync("sb_access_token", accessToken);
        await _secureStorage.SetAsync("sb_refresh_token", refreshToken);
    }

    private static void SaveSessionToSettings(string email, UserProfile profile)
    {
        var settings = AppSettings.Load();
        settings.AccountEmail = email;
        settings.AccountUsername = profile.Username;
        settings.HasCompletedFirstRun = true;
        settings.IsLocalOnlyMode = false;
        settings.SyncEnabled = profile.HasSyncAccess;
        settings.AuthToken = null; // tokens live in DPAPI, not plain JSON
        settings.Save();
    }
}
