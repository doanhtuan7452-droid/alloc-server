using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class PythonPersonnelAssessmentResponse
    {
        [JsonPropertyName("prediction_label")]
        public string PredictionLabel { get; set; } = string.Empty;

        [JsonPropertyName("prediction_code")]
        public int PredictionCode { get; set; }

        [JsonPropertyName("class_probabilities")]
        public Dictionary<string, double>? ClassProbabilities { get; set; }

        [JsonPropertyName("confidence_score")]
        public double ConfidenceScore { get; set; }

        [JsonPropertyName("business_status_code")]
        public string BusinessStatusCode { get; set; } = string.Empty;

        [JsonPropertyName("business_status_text")]
        public string BusinessStatusText { get; set; } = string.Empty;

        [JsonPropertyName("success_factors")]
        public List<string>? SuccessFactors { get; set; }

        [JsonPropertyName("potential_challenges")]
        public List<string>? PotentialChallenges { get; set; }

        [JsonPropertyName("llm_insight")]
        public string LlmInsight { get; set; } = string.Empty;

        [JsonPropertyName("explanation_source")]
        public string ExplanationSource { get; set; } = string.Empty;

        [JsonPropertyName("assumptions")]
        public Dictionary<string, object>? Assumptions { get; set; }

        [JsonPropertyName("missing_fields")]
        public List<string>? MissingFields { get; set; }

        [JsonPropertyName("confidence_penalty")]
        public double ConfidencePenalty { get; set; }

        [JsonPropertyName("fit_percentage")]
        public double FitPercentage { get; set; }

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string? EmployeeName { get; set; }
    }
}
