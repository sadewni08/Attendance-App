using System.Text.Json.Serialization;

namespace ChronoTrack_ViewLayer.Models
{
    public class AttendanceStatsDto
    {
        [JsonPropertyName("totalEmployees")]
        public int TotalEmployees { get; set; }
        
        [JsonPropertyName("totalArrived")]
        public int TotalArrived { get; set; }
        
        [JsonPropertyName("onTime")]
        public int OnTime { get; set; }
        
        [JsonPropertyName("lateArrivals")]
        public int LateArrivals { get; set; }
        
        [JsonPropertyName("earlyDeparture")]
        public int EarlyDeparture { get; set; }
        
        [JsonPropertyName("absent")]
        public int Absent { get; set; }
        
        [JsonPropertyName("asOf")]
        public DateTime AsOf { get; set; }
    }
} 