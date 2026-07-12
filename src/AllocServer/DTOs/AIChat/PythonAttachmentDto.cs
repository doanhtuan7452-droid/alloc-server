using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonAttachmentDto
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty;

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("storage_url")]
        public string StorageUrl { get; set; } = string.Empty;

        [JsonPropertyName("extracted_text")]
        public string? ExtractedText { get; set; }
    }
}
