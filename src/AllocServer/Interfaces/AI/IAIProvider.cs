namespace AllocServer.Interfaces.AI
{
    public interface IAIProvider
    {
        Task<string> GenerateAnalysisAsync(AIAnalysisContext context);
    }

    public class AIAnalysisContext
    {
        public string AnalysisType { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public string ProjectSummary { get; set; } = string.Empty;
        public string? TargetEntitySummary { get; set; }
    }
}
