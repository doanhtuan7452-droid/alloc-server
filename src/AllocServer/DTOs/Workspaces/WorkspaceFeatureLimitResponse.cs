using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceFeatureLimitResponse
    {
        [JsonPropertyName("featureCode")]
        public string FeatureCode { get; set; } = string.Empty;

        [JsonPropertyName("isIncluded")]
        public bool IsIncluded { get; set; }

        [JsonPropertyName("limitValue")]
        public int LimitValue { get; set; }
    }
}
