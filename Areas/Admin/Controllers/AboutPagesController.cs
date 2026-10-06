using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AboutPagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AboutPagesController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /Admin/AboutPages
        public async Task<IActionResult> Index()
        {
            var pages = await _context.AboutPages.ToListAsync();
            return View(pages);
        }

        // GET: /Admin/AboutPages/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var page = await _context.AboutPages.FindAsync(id);
            if (page == null) return NotFound();
            return View(page);
        }

        // POST: /Admin/AboutPages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AboutPage model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var page = await _context.AboutPages.FindAsync(id);
            if (page == null) return NotFound();

            page.Title = model.Title.Trim();
            page.Subtitle = model.Subtitle?.Trim();
            page.Content = model.Content;
            page.BannerImageUrl = model.BannerImageUrl?.Trim();
            page.LastModifiedAt = DateTime.UtcNow;
            page.ModifiedBy = User.Identity?.Name ?? "Admin";

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UpdateAboutPage", nameof(AboutPage), page.Id.ToString(), $"Updated CMS content for section '{page.SectionKey}'");

            TempData["SuccessMessage"] = $"CMS Page '{page.Title}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
