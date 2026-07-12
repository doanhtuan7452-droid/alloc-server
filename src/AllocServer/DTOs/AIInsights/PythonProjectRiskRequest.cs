using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class PythonProjectRiskRequest
    {
        [JsonPropertyName("Project_Duration_Days")]
        public int ProjectDurationDays { get; set; }

        [JsonPropertyName("Expected_Budget")]
        public double ExpectedBudget { get; set; }

        [JsonPropertyName("Team_Size")]
        public int TeamSize { get; set; }

        [JsonPropertyName("Avg_Team_Skill_Level")]
        public double AvgTeamSkillLevel { get; set; }

        [JsonPropertyName("Complexity_Score")]
        public double ComplexityScore { get; set; }

        [JsonPropertyName("Budget_Utilization")]
        public double BudgetUtilization { get; set; }

        [JsonPropertyName("Methodology_Used_Kanban")]
        public int MethodologyUsedKanban { get; set; }

        [JsonPropertyName("Methodology_Used_Scrum")]
        public int MethodologyUsedScrum { get; set; }

        [JsonPropertyName("Methodology_Used_Waterfall")]
        public int MethodologyUsedWaterfall { get; set; }

        [JsonPropertyName("Methodology_Used_Hybrid")]
        public int MethodologyUsedHybrid { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "openai";

        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-4o";

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.5;
    }
}
