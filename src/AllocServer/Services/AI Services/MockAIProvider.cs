using AllocServer.Interfaces.AI;

namespace AllocServer.Services.AI_Services
{
    public class MockAIProvider : IAIProvider
    {
        public async Task<string> GenerateAnalysisAsync(AIAnalysisContext context)
        {
            await Task.Delay(200);

            return context.AnalysisType switch
            {
                "Risk Warning" => BuildRiskWarning(context),
                "Resource Suggestion" => BuildResourceSuggestion(context),
                "Budget Forecast" => BuildBudgetForecast(context),
                _ => "Mock AI khong ho tro loai phan tich nay."
            };
        }

        private static string BuildRiskWarning(AIAnalysisContext context)
        {
            return string.Join(
                Environment.NewLine,
                "Mock AI Risk Warning:",
                "- Du an co kha nang phat sinh rui ro ve tien do neu cac task dang tre khong duoc xu ly som.",
                "- Nen uu tien review cac task co effort cao va cap nhat mitigation plan cho risk quan trong.",
                $"Context: {context.ProjectSummary}",
                FormatTarget(context),
                FormatPrompt(context));
        }

        private static string BuildResourceSuggestion(AIAnalysisContext context)
        {
            return string.Join(
                Environment.NewLine,
                "Mock AI Resource Suggestion:",
                "- Nen can bang lai workload cho cac thanh vien dang nam o task co trang thai In Progress.",
                "- Co the bo sung reviewer cho cac task sap den han de giam bottleneck.",
                $"Context: {context.ProjectSummary}",
                FormatTarget(context),
                FormatPrompt(context));
        }

        private static string BuildBudgetForecast(AIAnalysisContext context)
        {
            return string.Join(
                Environment.NewLine,
                "Mock AI Budget Forecast:",
                "- Chi phi du kien can duoc theo doi sat hon neu tien do cham so voi moc ke hoach.",
                "- Nen so sanh expected budget, revenue va effort con lai hang tuan.",
                $"Context: {context.ProjectSummary}",
                FormatTarget(context),
                FormatPrompt(context));
        }

        private static string FormatTarget(AIAnalysisContext context)
        {
            return string.IsNullOrWhiteSpace(context.TargetEntitySummary)
                ? "Target: none"
                : $"Target: {context.TargetEntitySummary}";
        }

        private static string FormatPrompt(AIAnalysisContext context)
        {
            return string.IsNullOrWhiteSpace(context.Prompt)
                ? "Prompt: none"
                : $"Prompt: {context.Prompt}";
        }
    }
}
