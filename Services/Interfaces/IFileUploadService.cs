using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Interfaces
{
    public interface IFileUploadService
    {
        Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadFileAsync(IFormFile file, string subFolder);
        bool DeleteFile(string? relativePath);
        bool IsValidImageFile(IFormFile file, out string errorMessage);
    }
}
