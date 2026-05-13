using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Accounts
{
    public class UpdateAccountProfileRequest : IValidatableObject
    {
        [Required(ErrorMessage = "FullName is required.")]
        [StringLength(100, ErrorMessage = "FullName must not exceed 100 characters.")]
        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "PhoneNumber must not exceed 20 characters.")]
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "AvatarURL must not exceed 500 characters.")]
        [JsonPropertyName("avatarUrl")]
        public string? AvatarURL { get; set; }

        [StringLength(50, ErrorMessage = "Timezone must not exceed 50 characters.")]
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                yield return new ValidationResult(
                    "FullName must not be empty.",
                    new[] { nameof(FullName) });
            }
        }
    }
}
