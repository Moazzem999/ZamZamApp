using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ZamZamApp.Models;

namespace ZamZamApp.Services
{
    public class AuthService
    {
        private static readonly Lazy<AuthService> _instance = new(() => new AuthService());
        public static AuthService Instance => _instance.Value;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;

        private const string PrefToken = "ZamZam_AuthToken";
        private const string PrefExpiresAt = "ZamZam_AuthExpiresAt";
        private const string PrefUserJson = "ZamZam_AuthUser";

        public event Action? AuthStateChanged;

        private AuthService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(GetToken());

        public string GetToken()
        {
            return Preferences.Default.Get(PrefToken, string.Empty);
        }

        public AuthUser? GetCurrentUser()
        {
            string json = Preferences.Default.Get(PrefUserJson, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<AuthUser>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Succeeded, string Message, LoginData? Data)> LoginAsync(string userName, string password)
        {
            try
            {
                var requestPayload = new LoginRequest
                {
                    UserName = userName.Trim(),
                    Password = password
                };

                string json = JsonSerializer.Serialize(requestPayload, JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(AppSettings.LoginEndpoint, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return (false, $"Server returned empty response (HTTP {(int)response.StatusCode}).", null);
                }

                LoginResponse? loginResponse = null;
                try
                {
                    loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody, JsonOptions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AuthService] JSON parse error: {ex.Message}");
                }

                if (loginResponse == null)
                {
                    return (false, $"Unexpected server response (HTTP {(int)response.StatusCode}).", null);
                }

                if (!loginResponse.Succeeded || loginResponse.Data == null || string.IsNullOrWhiteSpace(loginResponse.Data.Token))
                {
                    string errorMessage = !string.IsNullOrWhiteSpace(loginResponse.Message)
                        ? loginResponse.Message
                        : $"Login failed (Status {loginResponse.StatusCode}).";
                    return (false, errorMessage, null);
                }

                SaveSession(loginResponse.Data);
                return (true, loginResponse.Message, loginResponse.Data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] Login error: {ex.Message}");
                return (false, ex.Message, null);
            }
        }

        public void Logout()
        {
            Preferences.Default.Remove(PrefToken);
            Preferences.Default.Remove(PrefExpiresAt);
            Preferences.Default.Remove(PrefUserJson);
            NotifyAuthStateChanged();
        }

        private void SaveSession(LoginData data)
        {
            Preferences.Default.Set(PrefToken, data.Token);

            if (data.ExpiresIn > 0)
            {
                var expiresAt = DateTime.UtcNow.AddDays(data.ExpiresIn);
                Preferences.Default.Set(PrefExpiresAt, expiresAt.ToString("O"));
            }

            if (data.User != null)
            {
                string userJson = JsonSerializer.Serialize(data.User, JsonOptions);
                Preferences.Default.Set(PrefUserJson, userJson);
            }

            NotifyAuthStateChanged();
        }

        private void NotifyAuthStateChanged()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() => AuthStateChanged?.Invoke());
            }
            catch
            {
            }
        }
    }
}
