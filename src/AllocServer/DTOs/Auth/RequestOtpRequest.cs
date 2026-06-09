using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Auth
{
    public class RequestOtpRequest
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;
    }
}
