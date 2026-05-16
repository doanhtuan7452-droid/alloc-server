using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class RiskLifecycleResponse
    {
        [JsonPropertyName("historyId")]
        public int HistoryId { get; set; }

        [JsonPropertyName("riskId")]
        public int RiskId { get; set; }

        [JsonPropertyName("changedByMemberId")]
        public int ChangedByMemberId { get; set; }

        [JsonPropertyName("oldStatus")]
        public string? OldStatus { get; set; }

        [JsonPropertyName("newStatus")]
        public string? NewStatus { get; set; }

        [JsonPropertyName("oldScore")]
        public int? OldScore { get; set; }

        [JsonPropertyName("newScore")]
        public int? NewScore { get; set; }

        [JsonPropertyName("changeNote")]
        public string? ChangeNote { get; set; }

        [JsonPropertyName("changeDate")]
        public DateTime ChangeDate { get; set; }
    }
}
