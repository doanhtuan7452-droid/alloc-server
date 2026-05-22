using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Projects
{
    public class UploadAssetRequestDto
    {
        [Required]
        [JsonPropertyName("file")]
        public IFormFile? File { get; set; }
    }
}
