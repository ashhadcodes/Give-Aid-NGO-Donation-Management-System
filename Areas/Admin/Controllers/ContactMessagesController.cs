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
    public class ContactMessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ContactMessagesController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Admin/ContactMessages
        public async Task<IActionResult> Index(MessageStatus? status)
        {
            var query = _context.ContactMessages.AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            var messages = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
            ViewBag.SelectedStatus = status;
            return View(messages);
        }

        // GET: /Admin/ContactMessages/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (!message.IsRead)
            {
                message.IsRead = true;
                if (message.Status == MessageStatus.New)
                {
                    message.Status = MessageStatus.InReview;
                }
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // POST: /Admin/ContactMessages/Reply/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyText)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(replyText))
            {
                message.AdminNotes = replyText.Trim();
                message.Status = MessageStatus.Replied;
                message.RepliedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var body = $"<p>Dear {message.Name},</p><p>Thank you for contacting Give-AID regarding: <strong>{message.Subject}</strong></p><p>{replyText}</p><p>Best regards,<br/>Give-AID Team</p>";
                _ = _emailService.SendEmailAsync(message.Email, $"Re: {message.Subject}", body);

                TempData["SuccessMessage"] = "Reply sent successfully.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/ContactMessages/Archive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                message.Status = MessageStatus.Archived;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message archived.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
