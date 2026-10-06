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
    public class PartnersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IAuditService _auditService;

        public PartnersController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            IAuditService auditService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _auditService = auditService;
        }

        // GET: /Admin/Partners
        public async Task<IActionResult> Index()
        {
            var partners = await _context.Partners
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();
            return View(partners);
        }

        // GET: /Admin/Partners/Create
        public IActionResult Create()
        {
            return View(new PartnerFormViewModel());
        }

        // POST: /Admin/Partners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PartnerFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string? logoUrl = model.LogoUrl;
            if (model.LogoFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.LogoFile, "partners");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                logoUrl = uploadResult.FilePath;
            }

            var partner = new Partner
            {
                Name = model.Name.Trim(),
                LogoUrl = logoUrl,
                Description = model.Description?.Trim(),
                Website = model.Website?.Trim(),
                ContactEmail = model.ContactEmail?.Trim(),
                ContactPhone = model.ContactPhone?.Trim(),
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreatePartner", nameof(Partner), partner.Id.ToString(), $"Added partner organization '{partner.Name}'");

            TempData["SuccessMessage"] = $"Partner '{partner.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Partners/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner == null) return NotFound();

            var model = new PartnerFormViewModel
            {
                Id = partner.Id,
                Name = partner.Name,
                LogoUrl = partner.LogoUrl,
                Description = partner.Description,
                Website = partner.Website,
                ContactEmail = partner.ContactEmail,
                ContactPhone = partner.ContactPhone,
                DisplayOrder = partner.DisplayOrder,
                IsActive = partner.IsActive
            };

            return View(model);
        }

        // POST: /Admin/Partners/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PartnerFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var partner = await _context.Partners.FindAsync(id);
            if (partner == null) return NotFound();

            if (model.LogoFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.LogoFile, "partners");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                partner.LogoUrl = uploadResult.FilePath;
            }

            partner.Name = model.Name.Trim();
            partner.Description = model.Description?.Trim();
            partner.Website = model.Website?.Trim();
            partner.ContactEmail = model.ContactEmail?.Trim();
            partner.ContactPhone = model.ContactPhone?.Trim();
            partner.DisplayOrder = model.DisplayOrder;
            partner.IsActive = model.IsActive;
            partner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UpdatePartner", nameof(Partner), partner.Id.ToString(), $"Updated partner '{partner.Name}'");

            TempData["SuccessMessage"] = $"Partner '{partner.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Partners/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner == null) return NotFound();

            partner.IsActive = !partner.IsActive;
            partner.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Partner '{partner.Name}' status updated to {(partner.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
