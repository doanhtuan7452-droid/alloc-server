using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class CreateTaskDependencyRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "predecessorTaskId phai lon hon 0.")]
        [JsonPropertyName("predecessorTaskId")]
        public int PredecessorTaskId { get; set; }

        [Required(ErrorMessage = "DependencyType la bat buoc.")]
        [StringLength(10, ErrorMessage = "DependencyType khong duoc vuot qua 10 ky tu.")]
        [JsonPropertyName("dependencyType")]
        public string DependencyType { get; set; } = string.Empty;
    }
}
