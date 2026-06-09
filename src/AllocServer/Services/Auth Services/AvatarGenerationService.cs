using AllocServer.Configurations;
using AllocServer.Interfaces.Auth;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Text;

namespace AllocServer.Services.Auth_Services
{
    public class AvatarGenerationService : IAvatarGenerationService
    {
        private readonly string _baseUrl;

        public AvatarGenerationService(IOptions<AvatarSettings> options)
        {
            _baseUrl = options.Value.BaseUrl;
        }

        public string GenerateAvatarUrl(string? name, string email)
        {
            var displayName = !string.IsNullOrWhiteSpace(name) 
                ? name.Trim() 
                : email.Split('@')[0];

            // Normalize Vietnamese characters
            var normalizedName = RemoveVietnameseSign(displayName);
            var encodedName = Uri.EscapeDataString(normalizedName);

            // Add background=random for a nice, dynamic appearance
            return $"{_baseUrl}?name={encodedName}&background=random";
        }

        public bool IsGeneratedAvatar(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase);
        }

        private static string RemoveVietnameseSign(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            // Replace 'Đ' and 'đ' manually since Unicode FormD decomposition doesn't handle them
            str = str.Replace("đ", "d").Replace("Đ", "D");

            var normalizedString = str.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
