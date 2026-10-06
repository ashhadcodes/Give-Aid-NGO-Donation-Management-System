using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public SettingsController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /Admin/Settings
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SiteSettings
                .OrderBy(s => s.Group)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();

            return View(settings);
        }

        // POST: /Admin/Settings/UpdateSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(Dictionary<string, string> settings)
        {
            if (settings != null)
            {
                foreach (var (key, val) in settings)
                {
                    var existing = await _context.SiteSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
                    if (existing != null)
                    {
                        existing.SettingValue = val ?? "";
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("UpdateSettings", nameof(SiteSetting), null, "Updated system site settings configuration.");
                TempData["SuccessMessage"] = "System settings updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
