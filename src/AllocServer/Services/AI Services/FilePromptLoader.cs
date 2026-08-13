using System;
using System.IO;
using AllocServer.Interfaces.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllocServer.Services.AI_Services
{
    public class FilePromptLoader : IPromptLoader
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<FilePromptLoader> _logger;

        public FilePromptLoader(IHostEnvironment hostEnvironment, ILogger<FilePromptLoader> logger)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public string LoadPrompt(string key, string defaultPrompt)
        {
            try
            {
                var promptsDir = Path.Combine(_hostEnvironment.ContentRootPath, "Prompts");
                var filePath = Path.Combine(promptsDir, $"{key}.txt");

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("[Prompt Loader] File prompt '{FilePath}' khong ton tai. Su dung fallback mac dinh.", filePath);
                    return defaultPrompt;
                }

                var content = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("[Prompt Loader] File prompt '{FilePath}' rong. Su dung fallback mac dinh.", filePath);
                    return defaultPrompt;
                }

                return content.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Prompt Loader] Loi khi doc file prompt cho key '{Key}'. Su dung fallback mac dinh.", key);
                return defaultPrompt;
            }
        }
    }
}
