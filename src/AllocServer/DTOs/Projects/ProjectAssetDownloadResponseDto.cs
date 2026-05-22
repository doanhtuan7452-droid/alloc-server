using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Projects
{
    public class ProjectAssetDownloadResponseDto
    {
        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }
}
