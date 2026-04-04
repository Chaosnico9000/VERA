using System.Net.Http.Headers;
using System.Net.Http.Json;
using VERA.Shared;
using VERA.Shared.Dto;

namespace VERA.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime _accessTokenExpiry;

        private const string PrefKeyRefresh  = "vera_refresh_token";
        private const string PrefKeyUsername = "vera_username";

        public ApiClient()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            // Refresh-Token aus sicherem Speicher laden
            _refreshToken = Preferences.Default.Get(PrefKeyRefresh, string.Empty);
            if (string.IsNullOrEmpty(_refreshToken)) _refreshToken = null;
        }

        public void SetBaseUrl(string url)
        {
            _http.BaseAddress = new Uri(url.TrimEnd('/') + '/');
        }

        public bool HasSession => _refreshToken != null;
        public string? Username => Preferences.Default.Get(PrefKeyUsername, string.Empty);

        // ── Auth ──────────────────────────────────────────────────────────────

        public async Task<(bool Ok, string Error)> RegisterAsync(string username, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("api/auth/register",
                    new RegisterRequest(username, password));
                if (resp.IsSuccessStatusCode) return (true, string.Empty);
                var err = await resp.Content.ReadFromJsonAsync<ApiError>();
                return (false, err?.Message ?? "Registrierung fehlgeschlagen.");
            }
            catch (Exception ex) { return (false, $"Verbindungsfehler: {ex.Message}"); }
        }

        public async Task<(LoginResult Result, string Error)> LoginAsync(string username, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("api/auth/login",
                    new LoginRequest(username, password));
                if (resp.IsSuccessStatusCode)
                {
                    var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
                    if (auth is null) return (LoginResult.InvalidPassword, "Ungültige Antwort.");
                    StoreTokens(auth);
                    return (LoginResult.Success, string.Empty);
                }
                if ((int)resp.StatusCode == 429) return (LoginResult.AccountLocked, "Account gesperrt.");
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (LoginResult.InvalidPassword, "Falsches Passwort.");
                return (LoginResult.NoAccountFound, "Benutzer nicht gefunden.");
            }
            catch (Exception ex) { return (LoginResult.NoAccountFound, $"Verbindungsfehler: {ex.Message}"); }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (_refreshToken is null) return false;
            try
            {
                var resp = await _http.PostAsJsonAsync("api/auth/refresh",
                    new RefreshRequest(_refreshToken));
                if (!resp.IsSuccessStatusCode) { ClearTokens(); return false; }
                var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
                if (auth is null) { ClearTokens(); return false; }
                StoreTokens(auth);
                return true;
            }
            catch { ClearTokens(); return false; }
        }

        public async Task LogoutAsync()
        {
            if (_refreshToken != null)
            {
                try
                {
                    await EnsureValidTokenAsync();
                    await _http.PostAsJsonAsync("api/auth/logout", new RefreshRequest(_refreshToken));
                }
                catch { /* ignore */ }
            }
            ClearTokens();
        }

        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            if (!await EnsureValidTokenAsync()) return false;
            try
            {
                var resp = await _http.PostAsJsonAsync("api/auth/change-password",
                    new ChangePasswordRequest(oldPassword, newPassword));
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ── Zeiteinträge ──────────────────────────────────────────────────────

        public async Task<List<TimeEntryDto>?> GetEntriesAsync()
        {
            if (!await EnsureValidTokenAsync()) return null;
            try
            {
                return await _http.GetFromJsonAsync<List<TimeEntryDto>>("api/entries");
            }
            catch { return null; }
        }

        public async Task<bool> UpsertEntryAsync(UpsertTimeEntryRequest req)
        {
            if (!await EnsureValidTokenAsync()) return false;
            try
            {
                var resp = await _http.PostAsJsonAsync("api/entries", req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteEntryAsync(Guid id)
        {
            if (!await EnsureValidTokenAsync()) return false;
            try
            {
                var resp = await _http.DeleteAsync($"api/entries/{id}");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<TimeEntryDto>?> SyncEntriesAsync(List<TimeEntryDto> localEntries)
        {
            if (!await EnsureValidTokenAsync()) return null;
            try
            {
                var resp = await _http.PostAsJsonAsync("api/entries/sync", localEntries);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadFromJsonAsync<List<TimeEntryDto>>();
            }
            catch { return null; }
        }

        // ── Token-Verwaltung ──────────────────────────────────────────────────

        private async Task<bool> EnsureValidTokenAsync()
        {
            if (_accessToken != null && DateTime.UtcNow < _accessTokenExpiry.AddSeconds(-30))
            {
                SetAuthHeader();
                return true;
            }
            return await RefreshTokenAsync();
        }

        private void SetAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        private void StoreTokens(AuthResponse auth)
        {
            _accessToken       = auth.AccessToken;
            _refreshToken      = auth.RefreshToken;
            _accessTokenExpiry = auth.ExpiresAt;
            Preferences.Default.Set(PrefKeyRefresh,  auth.RefreshToken);
            Preferences.Default.Set(PrefKeyUsername, auth.Username);
            SetAuthHeader();
        }

        private void ClearTokens()
        {
            _accessToken  = null;
            _refreshToken = null;
            Preferences.Default.Remove(PrefKeyRefresh);
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }
}
