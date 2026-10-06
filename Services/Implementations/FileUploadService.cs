using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Implementations
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileUploadSettings _settings;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IWebHostEnvironment environment,
            IOptions<FileUploadSettings> settings,
            ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _settings = settings.Value;
            _logger = logger;
        }

        public bool IsValidImageFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "Please select a valid non-empty file.";
                return false;
            }

            if (file.Length > _settings.MaxFileSizeBytes)
            {
                errorMessage = $"File size exceeds the maximum allowed limit of {_settings.MaxFileSizeBytes / (1024 * 1024)} MB.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(extension))
            {
                errorMessage = $"File extension '{extension}' is not permitted. Allowed: {string.Join(", ", _settings.AllowedExtensions)}";
                return false;
            }

            var contentType = file.ContentType.ToLowerInvariant();
            if (!_settings.AllowedMimeTypes.Contains(contentType))
            {
                errorMessage = $"Invalid file MIME type '{contentType}'.";
                return false;
            }

            return true;
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadFileAsync(IFormFile file, string subFolder)
        {
            if (!IsValidImageFile(file, out string validationError))
            {
                return (false, null, validationError);
            }

            try
            {
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var targetDir = Path.Combine(webRoot, _settings.UploadFolder, subFolder);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{extension}";
                var fullPhysicalPath = Path.Combine(targetDir, uniqueFileName);

                using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativeUrl = $"/{_settings.UploadFolder}/{subFolder}/{uniqueFileName}".Replace("\\", "/");
                _logger.LogInformation("File uploaded successfully to {RelativeUrl}", relativeUrl);

                return (true, relativeUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while uploading file: {FileName}", file.FileName);
                return (false, null, "An error occurred while saving the file to disk.");
            }
        }

        public bool DeleteFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;

            try
            {
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var cleanRelative = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physicalPath = Path.Combine(webRoot, cleanRelative);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    _logger.LogInformation("Deleted file at {PhysicalPath}", physicalPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file at path {RelativePath}", relativePath);
            }

            return false;
        }
    }
}
