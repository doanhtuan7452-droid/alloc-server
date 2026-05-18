using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class AttachTaskAssetRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "Danh sách AssetIds không được rỗng")]
        [JsonPropertyName("assetIds")]
        public List<int> AssetIds { get; set; } = new();
    }
}
