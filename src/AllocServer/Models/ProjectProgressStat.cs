namespace AllocServer.Models
{
    public class ProjectProgressStat
    {
        public int ProjectID { get; set; }
        public string ProjectStatus { get; set; } = string.Empty;
        public int TodoCount { get; set; }
        public int InProgressCount { get; set; }
        public int ReviewCount { get; set; }
        public int DoneCount { get; set; }
        public int TotalTasks { get; set; }
        public decimal TotalValue { get; set; }
        public double SimpleProgress { get; set; }
        public double WeightedProgress { get; set; }
    }
}
