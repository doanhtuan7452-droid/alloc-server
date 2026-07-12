namespace AllocServer.Models
{
    public class ProjectRiskFeature
    {
        public int ProjectID { get; set; }
        public string Project_Name { get; set; } = null!;
        public int Project_Duration_Days { get; set; }
        public decimal Expected_Budget { get; set; }
        public string Methodology_Used { get; set; } = null!;
        public int Total_Tasks { get; set; }
        public int Team_Size { get; set; }
        public double Avg_Team_Skill_Level { get; set; }
        public double Raw_Complexity_Score { get; set; }
        public decimal Budget_Utilization_Rate { get; set; }
        public double Overall_Risk_Score { get; set; }
    }
}
