using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class AIAskRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "projectId phai lon hon 0.")]
        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "analysisType la bat buoc.")]
        [StringLength(50, ErrorMessage = "analysisType toi da 50 ky tu.")]
        [JsonPropertyName("analysisType")]
        public string AnalysisType { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "targetEntityId phai lon hon 0.")]
        [JsonPropertyName("targetEntityId")]
        public int? TargetEntityId { get; set; }

        [StringLength(4000, ErrorMessage = "prompt toi da 4000 ky tu.")]
        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }

        [JsonPropertyName("workspaceMemberIds")]
        public List<int>? WorkspaceMemberIds { get; set; }
    }
}
