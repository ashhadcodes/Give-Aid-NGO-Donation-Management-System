using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class ProgrammesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProgrammesController> _logger;

        public ProgrammesController(
            ApplicationDbContext context,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProgrammesController> logger)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Programmes
        public async Task<IActionResult> Index(string? category, string? search)
        {
            var query = _context.Programmes
                .Include(p => p.NGO)
                .Where(p => p.Status != ProgrammeStatus.Cancelled)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(term) ||
                                         p.Description.ToLower().Contains(term) ||
                                         p.Location.ToLower().Contains(term));
            }

            var programmes = await query.OrderBy(p => p.StartDate).ToListAsync();
            var categories = await _context.Programmes
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            var model = new ProgrammeListViewModel
            {
                Programmes = programmes,
                SelectedCategory = category ?? "All",
                SearchTerm = search,
                AvailableCategories = categories
            };

            return View(model);
        }

        // GET: /Programmes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var programme = await _context.Programmes
                .Include(p => p.NGO)
                .Include(p => p.GalleryItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (programme == null)
            {
                return NotFound();
            }

            var related = await _context.Programmes
                .Where(p => p.Id != id && p.Category == programme.Category)
                .Take(3)
                .ToListAsync();

            var form = new ExpressInterestViewModel
            {
                ProgrammeId = programme.Id
            };

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    form.FullName = user.FullName;
                    form.Email = user.Email ?? string.Empty;
                    form.Phone = user.PhoneNumber;
                }
            }

            var model = new ProgrammeDetailsViewModel
            {
                Programme = programme,
                InterestForm = form,
                RelatedProgrammes = related
            };

            return View(model);
        }

        // POST: /Programmes/ExpressInterest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpressInterest(ExpressInterestViewModel model)
        {
            var programme = await _context.Programmes.FindAsync(model.ProgrammeId);
            if (programme == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Details), new { id = model.ProgrammeId });
            }

            string? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            var interest = new ProgrammeInterest
            {
                ProgrammeId = model.ProgrammeId,
                UserId = userId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone?.Trim(),
                Notes = model.Notes?.Trim(),
                Status = InterestStatus.Pending,
                ExpressedAt = DateTime.UtcNow
            };

            _context.ProgrammeInterests.Add(interest);
            await _context.SaveChangesAsync();

            _ = _emailService.SendProgrammeInterestConfirmationAsync(
                interest.Email,
                interest.FullName,
                programme.Title,
                programme.Location,
                programme.StartDate.ToString("dd MMM yyyy")
            );

            TempData["SuccessMessage"] = $"Thank you {model.FullName}! Your interest in '{programme.Title}' has been registered. We've sent a confirmation email.";
            return RedirectToAction(nameof(Details), new { id = model.ProgrammeId });
        }
    }
}
