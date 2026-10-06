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
    public class CausesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IAuditService _auditService;

        public CausesController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            IAuditService auditService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _auditService = auditService;
        }

        // GET: /Admin/Causes
        public async Task<IActionResult> Index()
        {
            var causes = await _context.DonationCauses
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Title)
                .ToListAsync();
            return View(causes);
        }

        // GET: /Admin/Causes/Create
        public IActionResult Create()
        {
            return View(new CauseFormViewModel());
        }

        // POST: /Admin/Causes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CauseFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Verify unique code
            if (await _context.DonationCauses.AnyAsync(c => c.Code == model.Code.Trim()))
            {
                ModelState.AddModelError(nameof(model.Code), "A cause with this code already exists.");
                return View(model);
            }

            string? imageUrl = model.ImageUrl;
            if (model.ImageFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.ImageFile, "causes");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                imageUrl = uploadResult.FilePath;
            }

            var cause = new DonationCause
            {
                Title = model.Title.Trim(),
                Code = model.Code.Trim().ToUpperInvariant(),
                Category = model.Category.Trim(),
                Description = model.Description.Trim(),
                TargetAmount = model.TargetAmount,
                RaisedAmount = model.RaisedAmount,
                ImageUrl = imageUrl,
                IsActive = model.IsActive,
                IsFeatured = model.IsFeatured,
                DisplayOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _context.DonationCauses.Add(cause);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreateCause", nameof(DonationCause), cause.Id.ToString(), $"Created donation cause '{cause.Title}' ({cause.Code})");

            TempData["SuccessMessage"] = $"Donation cause '{cause.Title}' was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Causes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var cause = await _context.DonationCauses.FindAsync(id);
            if (cause == null) return NotFound();

            var model = new CauseFormViewModel
            {
                Id = cause.Id,
                Title = cause.Title,
                Code = cause.Code,
                Category = cause.Category,
                Description = cause.Description,
                TargetAmount = cause.TargetAmount,
                RaisedAmount = cause.RaisedAmount,
                ImageUrl = cause.ImageUrl,
                IsActive = cause.IsActive,
                IsFeatured = cause.IsFeatured,
                DisplayOrder = cause.DisplayOrder
            };

            return View(model);
        }

        // POST: /Admin/Causes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CauseFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var cause = await _context.DonationCauses.FindAsync(id);
            if (cause == null) return NotFound();

            if (await _context.DonationCauses.AnyAsync(c => c.Code == model.Code.Trim() && c.Id != id))
            {
                ModelState.AddModelError(nameof(model.Code), "Another cause already uses this code.");
                return View(model);
            }

            if (model.ImageFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.ImageFile, "causes");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                cause.ImageUrl = uploadResult.FilePath;
            }

            cause.Title = model.Title.Trim();
            cause.Code = model.Code.Trim().ToUpperInvariant();
            cause.Category = model.Category.Trim();
            cause.Description = model.Description.Trim();
            cause.TargetAmount = model.TargetAmount;
            cause.IsActive = model.IsActive;
            cause.IsFeatured = model.IsFeatured;
            cause.DisplayOrder = model.DisplayOrder;
            cause.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UpdateCause", nameof(DonationCause), cause.Id.ToString(), $"Updated donation cause '{cause.Title}'");

            TempData["SuccessMessage"] = $"Donation cause '{cause.Title}' has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Causes/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var cause = await _context.DonationCauses.FindAsync(id);
            if (cause == null) return NotFound();

            cause.IsActive = !cause.IsActive;
            cause.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("ToggleCauseStatus", nameof(DonationCause), cause.Id.ToString(), $"Toggled cause '{cause.Title}' active status to {cause.IsActive}");

            TempData["SuccessMessage"] = $"Cause '{cause.Title}' is now {(cause.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
