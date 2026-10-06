using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class HelpCentreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HelpCentreController(
            ApplicationDbContext context,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        // GET: /HelpCentre
        public async Task<IActionResult> Index(string? q, int? categoryId, string? search = null)
        {
            var categories = await _context.FaqCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var query = _context.Faqs
                .Include(f => f.Category)
                .Where(f => f.IsActive)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(f => f.FaqCategoryId == categoryId.Value);
            }

            var searchTerm = !string.IsNullOrWhiteSpace(q) ? q : search;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(f => f.Question.ToLower().Contains(term) || f.Answer.ToLower().Contains(term));
            }

            var faqs = await query.OrderBy(f => f.DisplayOrder).ToListAsync();

            var newQuery = new RaiseQueryViewModel();
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    newQuery.Name = user.FullName;
                    newQuery.Email = user.Email ?? string.Empty;
                    newQuery.Phone = user.PhoneNumber;
                }
            }

            var model = new HelpCentreViewModel
            {
                Categories = categories,
                FilteredFaqs = faqs,
                SearchQuery = q,
                SelectedCategoryId = categoryId,
                NewQuery = newQuery
            };

            return View(model);
        }

        // POST: /HelpCentre/SubmitQuery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuery(HelpCentreViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload categories and faqs
                model.Categories = await _context.FaqCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
                model.FilteredFaqs = await _context.Faqs.Include(f => f.Category).Where(f => f.IsActive).OrderBy(f => f.DisplayOrder).ToListAsync();
                return View("Index", model);
            }

            string? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            var query = new SupportQuery
            {
                UserId = userId,
                Name = model.NewQuery.Name.Trim(),
                Email = model.NewQuery.Email.Trim(),
                Phone = model.NewQuery.Phone?.Trim(),
                Subject = model.NewQuery.Subject.Trim(),
                Message = model.NewQuery.Message.Trim(),
                Priority = model.NewQuery.Priority,
                Status = QueryStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportQueries.Add(query);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Your query #{query.Id} has been registered. Our support team will review and respond promptly.";
            return RedirectToAction(nameof(Index));
        }
    }
}
