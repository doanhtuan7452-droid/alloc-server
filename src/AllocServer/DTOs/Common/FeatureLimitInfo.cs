using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Common
{
    public class FeatureLimitInfo
    {
        [JsonPropertyName("isIncluded")]
        public bool IsIncluded { get; set; }

        [JsonPropertyName("limitValue")]
        public int LimitValue { get; set; }
    }
}
