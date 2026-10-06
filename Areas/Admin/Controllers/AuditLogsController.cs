using Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/AuditLogs
        public async Task<IActionResult> Index(string? searchTerm, string? actionFilter, string? entityFilter, int page = 1)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a => a.Description.ToLower().Contains(term) ||
                                         a.EntityName.ToLower().Contains(term) ||
                                         (a.UserName != null && a.UserName.ToLower().Contains(term)) ||
                                         (a.User != null && a.User.Email != null && a.User.Email.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                var act = actionFilter.Trim().ToLower();
                query = query.Where(a => a.Action.ToLower().Contains(act));
            }

            if (!string.IsNullOrWhiteSpace(entityFilter))
            {
                var ent = entityFilter.Trim().ToLower();
                query = query.Where(a => a.EntityName.ToLower().Contains(ent));
            }

            var totalCount = await query.CountAsync();
            int pageSize = 20;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogItemViewModel
                {
                    Timestamp = a.CreatedAt,
                    UserEmail = a.User != null ? a.User.Email : (a.UserName ?? "System"),
                    UserId = a.UserId,
                    Action = a.Action,
                    EntityType = a.EntityName,
                    EntityId = a.EntityId,
                    IpAddress = a.IpAddress,
                    Details = a.Description
                })
                .ToListAsync();

            var model = new AuditLogsViewModel
            {
                SearchTerm = searchTerm,
                ActionFilter = actionFilter,
                EntityFilter = entityFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                Logs = items
            };

            return View(model);
        }
    }
}
