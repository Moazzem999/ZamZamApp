using System.Text.Json.Serialization;

namespace ZamZamApp.Models
{
    public class EmployeeTaskResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("data")]
        public EmployeeTaskPagedData? Data { get; set; }
    }

    public class EmployeeTaskDeleteResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("data")]
        public bool Data { get; set; }
    }

    public class EmployeeTaskCreateRequest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("scheduledOn")]
        public DateTime ScheduledOn { get; set; }
    }

    public class EmployeeTaskCreateResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("data")]
        public long Data { get; set; }
    }

    public class EmployeeTaskUpdateResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("data")]
        public System.Text.Json.JsonElement? Data { get; set; }
    }

    public class EmployeeTaskPagedData
    {
        [JsonPropertyName("items")]
        public List<EmployeeTaskItem> Items { get; set; } = new();

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }

    public class EmployeeTaskItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("employeeId")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("employeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("imagePath")]
        public string? ImagePath { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("scheduledOn")]
        public DateTime ScheduledOn { get; set; }

        [JsonPropertyName("createdOn")]
        public DateTime CreatedOn { get; set; }

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; }

        [JsonPropertyName("createdBy")]
        public long CreatedBy { get; set; }

        [JsonPropertyName("updatedBy")]
        public long? UpdatedBy { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonIgnore]
        public string ScheduledOnDisplay => ScheduledOn == default ? "-" : ScheduledOn.ToString("dd MMM yyyy, hh:mm tt");

        [JsonIgnore]
        public string StatusDisplay => Status switch
        {
            1 => "Active",
            0 => "Inactive",
            _ => $"Status {Status}"
        };
    }
}
