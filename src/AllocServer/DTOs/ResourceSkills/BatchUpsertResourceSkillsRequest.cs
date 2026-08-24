using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.ResourceSkills
{
    public class SkillLevelItemRequest
    {
        [Required(ErrorMessage = "SkillId is required")]
        [JsonPropertyName("skillId")]
        public int SkillID { get; set; }

        [Required(ErrorMessage = "Level is required")]
        [Range(1, 5, ErrorMessage = "Skill level must be between 1 and 5")]
        [JsonPropertyName("level")]
        public int Level { get; set; }
    }

    public class BatchUpsertResourceSkillsRequest
    {
        [Required(ErrorMessage = "Skills list is required")]
        [JsonPropertyName("skills")]
        public List<SkillLevelItemRequest> Skills { get; set; } = new();
    }
}
