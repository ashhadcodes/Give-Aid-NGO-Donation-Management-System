using Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
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
    public class QueriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public QueriesController(
            ApplicationDbContext context,
            IEmailService emailService,
            IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        // GET: /Admin/Queries
        public async Task<IActionResult> Index(QueryStatus? status)
        {
            var query = _context.SupportQueries.AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(q => q.Status == status.Value);
            }

            var list = await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
            ViewBag.SelectedStatus = status;
            return View(list);
        }

        // GET: /Admin/Queries/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var query = await _context.SupportQueries
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (query == null) return NotFound();

            var model = new QueryReplyViewModel
            {
                QueryId = query.Id,
                SenderName = query.Name,
                SenderEmail = query.Email,
                Subject = query.Subject,
                Message = query.Message,
                Priority = query.Priority,
                Status = query.Status,
                CreatedAt = query.CreatedAt,
                AdminResponse = query.AdminResponse ?? string.Empty
            };

            return View(model);
        }

        // POST: /Admin/Queries/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(QueryReplyViewModel model)
        {
            if (!ModelState.IsValid) return View("Details", model);

            var query = await _context.SupportQueries.FindAsync(model.QueryId);
            if (query == null) return NotFound();

            query.AdminResponse = model.AdminResponse.Trim();
            query.Status = model.Status == QueryStatus.Open ? QueryStatus.Resolved : model.Status;
            query.RespondedAt = DateTime.UtcNow;
            query.RespondedBy = User.Identity?.Name ?? "Administrator";

            await _context.SaveChangesAsync();

            if (model.SendEmailNotification)
            {
                _ = _emailService.SendQueryResponseEmailAsync(query.Email, query.Name, query.Subject, query.AdminResponse);
            }

            await _auditService.LogAsync("ReplyQuery", nameof(SupportQuery), query.Id.ToString(), $"Responded to query #{query.Id} from {query.Email}");

            TempData["SuccessMessage"] = $"Response sent for Query #{query.Id}. Status updated to {query.Status}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
