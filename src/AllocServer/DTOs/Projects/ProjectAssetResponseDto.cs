using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Projects
{
    public class ProjectAssetResponseDto
    {
        [JsonPropertyName("assetId")]
        public int AssetID { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectID { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceID { get; set; }

        [JsonPropertyName("assetName")]
        public string AssetName { get; set; } = string.Empty;

        [JsonPropertyName("assetType")]
        public string AssetType { get; set; } = string.Empty;

        [JsonPropertyName("fileSizeKB")]
        public int FileSizeKB { get; set; }

        [JsonPropertyName("uploadedBy")]
        public int UploadedBy { get; set; }

        [JsonPropertyName("uploadedByName")]
        public string? UploadedByName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
