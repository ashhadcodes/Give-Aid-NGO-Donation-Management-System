using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityName, string? entityId, string description, string? userId = null, string? userName = null, string? ipAddress = null);
        Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
    }
}
