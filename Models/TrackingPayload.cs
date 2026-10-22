using System.Text.Json.Serialization;

namespace ZamZamApp.Models
{
    public class TrackingPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; } = 0;

        [JsonPropertyName("employeeId")]
        public long EmployeeId { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public string DateOfBirth { get; set; } = string.Empty;

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("batteryPercentage")]
        public int BatteryPercentage { get; set; }

        [JsonPropertyName("recordedAt")]
        public string RecordedAt { get; set; } = string.Empty;
    }
}
