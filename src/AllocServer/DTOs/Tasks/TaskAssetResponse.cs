using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class TaskAssetResponse
    {
        [JsonPropertyName("assetId")]
        public int AssetId { get; set; }

        [JsonPropertyName("assetName")]
        public string AssetName { get; set; } = string.Empty;

        [JsonPropertyName("assetType")]
        public string? AssetType { get; set; }

        [JsonPropertyName("fileSizeKB")]
        public int FileSizeKB { get; set; }

        [JsonPropertyName("attachedBy")]
        public int AttachedBy { get; set; }

        [JsonPropertyName("attachedByName")]
        public string AttachedByName { get; set; } = string.Empty;

        [JsonPropertyName("attachedAt")]
        public DateTime AttachedAt { get; set; }
    }
}
