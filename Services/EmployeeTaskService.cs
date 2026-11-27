using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
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

                using var request = CreateAuthorizedRequest(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.", null);
                }

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

                if (taskResponse.StatusCode == 401)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.", null);
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

        public async Task<(bool Succeeded, string Message, long CreatedId)> CreateAsync(EmployeeTaskCreateRequest payload)
        {
            try
            {
                using var request = CreateAuthorizedRequest(HttpMethod.Post, AppSettings.EmployeeTaskCreateEndpoint);
                string json = JsonSerializer.Serialize(payload, JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.", 0);
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return (false, $"Server returned empty response (HTTP {(int)response.StatusCode}).", 0);
                }

                EmployeeTaskCreateResponse? createResponse = null;
                try
                {
                    createResponse = JsonSerializer.Deserialize<EmployeeTaskCreateResponse>(responseBody, JsonOptions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EmployeeTaskService] Create JSON parse error: {ex.Message}");
                }

                if (createResponse == null)
                {
                    return (false, $"Unexpected server response (HTTP {(int)response.StatusCode}).", 0);
                }

                if (createResponse.StatusCode == 401)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.", 0);
                }

                if (!createResponse.Succeeded)
                {
                    string errorMessage = !string.IsNullOrWhiteSpace(createResponse.Message)
                        ? createResponse.Message
                        : $"Failed to create task (Status {createResponse.StatusCode}).";
                    return (false, errorMessage, 0);
                }

                string successMessage = !string.IsNullOrWhiteSpace(createResponse.Message)
                    ? createResponse.Message
                    : "Employee task successfully created.";
                return (true, successMessage, createResponse.Data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmployeeTaskService] Create error: {ex.Message}");
                return (false, ex.Message, 0);
            }
        }

        public async Task<(bool Succeeded, string Message)> DeleteAsync(long id)
        {
            try
            {
                string url = AppSettings.EmployeeTaskDeleteEndpoint(id);
                using var request = CreateAuthorizedRequest(HttpMethod.Delete, url);

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.");
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return response.IsSuccessStatusCode
                        ? (true, "Employee task successfully deleted.")
                        : (false, $"Server returned empty response (HTTP {(int)response.StatusCode}).");
                }

                EmployeeTaskDeleteResponse? deleteResponse = null;
                try
                {
                    deleteResponse = JsonSerializer.Deserialize<EmployeeTaskDeleteResponse>(responseBody, JsonOptions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EmployeeTaskService] Delete JSON parse error: {ex.Message}");
                }

                if (deleteResponse == null)
                {
                    return (false, $"Unexpected server response (HTTP {(int)response.StatusCode}).");
                }

                if (deleteResponse.StatusCode == 401)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.");
                }

                if (!deleteResponse.Succeeded || !deleteResponse.Data)
                {
                    string errorMessage = !string.IsNullOrWhiteSpace(deleteResponse.Message)
                        ? deleteResponse.Message
                        : $"Failed to delete task (Status {deleteResponse.StatusCode}).";
                    return (false, errorMessage);
                }

                string successMessage = !string.IsNullOrWhiteSpace(deleteResponse.Message)
                    ? deleteResponse.Message
                    : "Employee task successfully deleted.";
                return (true, successMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmployeeTaskService] Delete error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            string token = AuthService.Instance.GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return request;
        }
    }
}
