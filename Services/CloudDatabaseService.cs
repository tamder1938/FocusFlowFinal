using FocusFlowFinal.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

/// <summary>
/// ИСПРАВЛЕНО (Часть 3, п.9-10): HTTP-реализация <see cref="ICloudDatabaseService"/>.
///
/// Соответствует REST API из инструкции по серверу:
///   POST /api/sync          — основной эндпоинт синхронизации
///   GET  /api/health         — проверка доступности (ping)
///
/// БЕЗОПАСНОСТЬ (Часть 3, п.10):
///   - все запросы идут по HTTPS (CloudApiBaseUrl должен начинаться с https://);
///   - JWT передаётся в заголовке Authorization: Bearer {token};
///   - HttpClient использует стандартную проверку сертификатов
///     (ServerCertificateCustomValidationCallback НЕ переопределяется —
///     это намеренно, чтобы не отключать проверку TLS).
///
/// РЕЖИМ ПРОТОТИПА: если сервер недоступен (например, CloudApiBaseUrl —
/// заглушка "https://api.focusflow.example.com"), все методы возвращают
/// "пустой успех", чтобы приложение продолжало работать в локальном режиме
/// без падений. Это позволяет тестировать UI без реального бэкенда.
/// </summary>
public class CloudDatabaseService : ICloudDatabaseService
{
    private readonly HttpClient _httpClient;

    public CloudDatabaseService()
    {
        var settings = AppSettings.Load();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.CloudApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<SyncResponse?> PushAndPullChangesAsync(SyncData localChanges, string authToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authToken);

            // POST /api/sync — отправляем локальные изменения, получаем чужие.
            // Тело запроса = SyncData (SinceUtc + списки Tasks/Events/Projects).
            // Тело ответа  = SyncResponse (ServerTimeUtc + списки изменений с других устройств).
            var response = await _httpClient.PostAsJsonAsync("/api/sync", localChanges);

            if (!response.IsSuccessStatusCode)
            {
                // ИСПРАВЛЕНО (Часть 3, п.10): при 401 — токен недействителен,
                // вызывающий код (ISyncService) должен сбросить сессию через IAuthService.LogoutAsync().
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SyncResponse>();
        }
        catch (HttpRequestException)
        {
            // Сервер недоступен (нет сети / заглушка URL) — не ломаем приложение,
            // просто сообщаем вызывающему коду, что синхронизация не выполнена.
            return null;
        }
        catch (TaskCanceledException)
        {
            // Таймаут запроса
            return null;
        }
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
