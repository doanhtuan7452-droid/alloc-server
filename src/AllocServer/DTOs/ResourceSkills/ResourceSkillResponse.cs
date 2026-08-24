using System.Text.Json.Serialization;

namespace AllocServer.DTOs.ResourceSkills
{
    public class ResourceSkillResponse
    {
        [JsonPropertyName("resourceId")]
        public int ResourceID { get; set; }

        [JsonPropertyName("skillId")]
        public int SkillID { get; set; }

        [JsonPropertyName("skillName")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }
    }
}
