using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Skills
{
    public class UpdateSkillRequest
    {
        [Required(ErrorMessage = "SkillName is required")]
        [StringLength(100, ErrorMessage = "SkillName cannot exceed 100 characters")]
        [JsonPropertyName("skillName")]
        public string SkillName { get; set; } = string.Empty;
    }
}
