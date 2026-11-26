using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using ZamZamApp.Models;

namespace ZamZamApp.Services
{
    public class EmployeeTaskService
    {
        private static readonly Lazy<EmployeeTaskService> _instance = new(() => new EmployeeTaskService());
        public static EmployeeTaskService Instance => _instance.Value;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;

        private EmployeeTaskService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<(bool Succeeded, string Message, EmployeeTaskPagedData? Data)> GetAllAsync(
            string? searchTerm = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                    queryParams.Add($"SearchTerm={Uri.EscapeDataString(searchTerm.Trim())}");

                if (pageNumber.HasValue && pageNumber.Value > 0)
                    queryParams.Add($"PageNumber={pageNumber.Value}");

                if (pageSize.HasValue && pageSize.Value > 0)
                    queryParams.Add($"PageSize={pageSize.Value}");

                string url = AppSettings.EmployeeTaskGetAllEndpoint;
                if (queryParams.Count > 0)
                    url = $"{url}?{string.Join("&", queryParams)}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                string token = AuthService.Instance.GetToken();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return (false, $"Server returned empty response (HTTP {(int)response.StatusCode}).", null);
                }

                EmployeeTaskResponse? taskResponse = null;
                try
                {
                    taskResponse = JsonSerializer.Deserialize<EmployeeTaskResponse>(responseBody, JsonOptions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EmployeeTaskService] JSON parse error: {ex.Message}");
                }

                if (taskResponse == null)
                {
                    return (false, $"Unexpected server response (HTTP {(int)response.StatusCode}).", null);
                }

                if (!taskResponse.Succeeded)
                {
                    string errorMessage = !string.IsNullOrWhiteSpace(taskResponse.Message)
                        ? taskResponse.Message
                        : $"Failed to load tasks (Status {taskResponse.StatusCode}).";
                    return (false, errorMessage, null);
                }

                return (true, taskResponse.Message, taskResponse.Data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmployeeTaskService] Error: {ex.Message}");
                return (false, ex.Message, null);
            }
        }
    }
}
