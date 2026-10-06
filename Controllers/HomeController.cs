using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalDonationsRaised = await _context.Donations
                .Where(d => d.Status == DonationStatus.Paid)
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

            var totalDonationsCount = await _context.Donations
                .CountAsync(d => d.Status == DonationStatus.Paid);

            var activeProgrammesCount = await _context.Programmes
                .CountAsync(p => p.Status == ProgrammeStatus.Upcoming || p.Status == ProgrammeStatus.Ongoing);

            var partnerNgosCount = await _context.NGOs.CountAsync(n => n.IsActive);

            var featuredCauses = await _context.DonationCauses
                .Where(c => c.IsActive && c.IsFeatured)
                .OrderBy(c => c.DisplayOrder)
                .Take(6)
                .ToListAsync();

            if (!featuredCauses.Any())
            {
                featuredCauses = await _context.DonationCauses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Take(6)
                    .ToListAsync();
            }

            var latestProgrammes = await _context.Programmes
                .Include(p => p.NGO)
                .Where(p => p.Status != ProgrammeStatus.Cancelled)
                .OrderBy(p => p.StartDate)
                .Take(3)
                .ToListAsync();

            var featuredNgos = await _context.NGOs
                .Where(n => n.IsActive)
                .OrderBy(n => n.DisplayOrder)
                .Take(4)
                .ToListAsync();

            var partners = await _context.Partners
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Take(8)
                .ToListAsync();

            var recentGallery = await _context.GalleryItems
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .Take(6)
                .ToListAsync();

            var topFaqs = await _context.Faqs
                .Include(f => f.Category)
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .Take(5)
                .ToListAsync();

            var model = new HomeIndexViewModel
            {
                TotalDonationsRaised = totalDonationsRaised > 0 ? totalDonationsRaised : 25400000m,
                TotalDonationsCount = totalDonationsCount > 0 ? totalDonationsCount : 4520,
                TotalPeopleHelped = 68500,
                TotalActiveProgrammes = activeProgrammesCount > 0 ? activeProgrammesCount : 18,
                TotalPartnerNgos = partnerNgosCount > 0 ? partnerNgosCount : 24,
                FeaturedCauses = featuredCauses,
                LatestProgrammes = latestProgrammes,
                FeaturedNgos = featuredNgos,
                Partners = partners,
                RecentGallery = recentGallery,
                TopFaqs = topFaqs
            };

            return View(model);
        }

        public async Task<IActionResult> Partners()
        {
            var partners = await _context.Partners
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            return View(partners);
        }

        public async Task<IActionResult> Gallery(string? category)
        {
            var query = _context.GalleryItems
                .Include(g => g.Programme)
                .Where(g => g.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(g => g.Category == category);
            }

            var items = await query.OrderBy(g => g.DisplayOrder).ToListAsync();
            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.Categories = await _context.GalleryItems
                .Where(g => g.IsActive && !string.IsNullOrEmpty(g.Category))
                .Select(g => g.Category!)
                .Distinct()
                .ToListAsync();

            return View(items);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.StatusCode = statusCode;
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
