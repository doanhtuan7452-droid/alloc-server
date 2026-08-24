namespace AllocServer.Converters
{
    public static class SkillLevelConverter
    {
        public static double ToNumericLevel(string? requiredSkillLevel) => requiredSkillLevel?.Trim().ToLowerInvariant() switch
        {
            "low" => 1.0,
            "medium" => 3.0,
            "high" => 4.0,
            "expert" => 5.0,
            _ => 3.0 // Fallback an toan
        };
    }
}
