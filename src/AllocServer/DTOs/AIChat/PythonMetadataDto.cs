using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonMetadataDto
    {
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
    }
}
