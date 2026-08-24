using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Skills
{
    public class SkillResponse
    {
        [JsonPropertyName("skillId")]
        public int SkillID { get; set; }

        [JsonPropertyName("skillName")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("usageCount")]
        public int UsageCount { get; set; }
    }
}
