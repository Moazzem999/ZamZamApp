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
            var (succeeded, message, data) = await SendTaskMutationAsync(
                HttpMethod.Post,
                AppSettings.EmployeeTaskCreateEndpoint,
                payload,
                "Employee task successfully created.",
                "Failed to create task");

            long createdId = 0;
            if (succeeded && data.HasValue && data.Value.ValueKind == JsonValueKind.Number)
            {
                data.Value.TryGetInt64(out createdId);
            }

            return (succeeded, message, createdId);
        }

        public async Task<(bool Succeeded, string Message)> UpdateAsync(EmployeeTaskCreateRequest payload)
        {
            var (succeeded, message, _) = await SendTaskMutationAsync(
                HttpMethod.Put,
                AppSettings.EmployeeTaskUpdateEndpoint,
                payload,
                "Employee task successfully updated.",
                "Failed to update task");

            return (succeeded, message);
        }

        private async Task<(bool Succeeded, string Message, JsonElement? Data)> SendTaskMutationAsync(
            HttpMethod method,
            string url,
            EmployeeTaskCreateRequest payload,
            string defaultSuccessMessage,
            string defaultErrorPrefix)
        {
            try
            {
                using var request = CreateAuthorizedRequest(method, url);
                string json = JsonSerializer.Serialize(payload, JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.", null);
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return response.IsSuccessStatusCode
                        ? (true, defaultSuccessMessage, null)
                        : (false, $"Server returned empty response (HTTP {(int)response.StatusCode}).", null);
                }

                EmployeeTaskUpdateResponse? apiResponse = null;
                try
                {
                    apiResponse = JsonSerializer.Deserialize<EmployeeTaskUpdateResponse>(responseBody, JsonOptions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EmployeeTaskService] {method} JSON parse error: {ex.Message}");
                }

                if (apiResponse == null)
                {
                    return (false, $"Unexpected server response (HTTP {(int)response.StatusCode}).", null);
                }

                if (apiResponse.StatusCode == 401)
                {
                    AuthService.Instance.HandleUnauthorized();
                    return (false, "Session expired. Please sign in again.", null);
                }

                if (!apiResponse.Succeeded)
                {
                    string errorMessage = !string.IsNullOrWhiteSpace(apiResponse.Message)
                        ? apiResponse.Message
                        : $"{defaultErrorPrefix} (Status {apiResponse.StatusCode}).";
                    return (false, errorMessage, null);
                }

                string successMessage = !string.IsNullOrWhiteSpace(apiResponse.Message)
                    ? apiResponse.Message
                    : defaultSuccessMessage;
                return (true, successMessage, apiResponse.Data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EmployeeTaskService] {method} error: {ex.Message}");
                return (false, ex.Message, null);
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
