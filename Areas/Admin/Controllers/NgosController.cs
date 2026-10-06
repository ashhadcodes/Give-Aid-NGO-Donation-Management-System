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
    public class NgosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IAuditService _auditService;

        public NgosController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            IAuditService auditService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _auditService = auditService;
        }

        // GET: /Admin/Ngos
        public async Task<IActionResult> Index()
        {
            var ngos = await _context.NGOs
                .Include(n => n.Programmes)
                .OrderBy(n => n.DisplayOrder)
                .ThenBy(n => n.Name)
                .ToListAsync();
            return View(ngos);
        }

        // GET: /Admin/Ngos/Create
        public IActionResult Create()
        {
            return View(new NgoFormViewModel());
        }

        // POST: /Admin/Ngos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NgoFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string? logoUrl = model.LogoUrl;
            if (model.LogoFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.LogoFile, "ngos");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                logoUrl = uploadResult.FilePath;
            }

            var ngo = new NGO
            {
                Name = model.Name.Trim(),
                RegistrationNumber = model.RegistrationNumber?.Trim(),
                Description = model.Description.Trim(),
                LogoUrl = logoUrl,
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                Website = model.Website?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                Country = model.Country?.Trim() ?? "Pakistan",
                EstablishedYear = model.EstablishedYear,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _context.NGOs.Add(ngo);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreateNGO", nameof(NGO), ngo.Id.ToString(), $"Registered new NGO '{ngo.Name}'");

            TempData["SuccessMessage"] = $"NGO '{ngo.Name}' registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Ngos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var ngo = await _context.NGOs.FindAsync(id);
            if (ngo == null) return NotFound();

            var model = new NgoFormViewModel
            {
                Id = ngo.Id,
                Name = ngo.Name,
                RegistrationNumber = ngo.RegistrationNumber,
                Description = ngo.Description,
                LogoUrl = ngo.LogoUrl,
                Email = ngo.Email,
                Phone = ngo.Phone,
                Website = ngo.Website,
                Address = ngo.Address,
                City = ngo.City,
                Country = ngo.Country,
                EstablishedYear = ngo.EstablishedYear,
                IsActive = ngo.IsActive,
                DisplayOrder = ngo.DisplayOrder
            };

            return View(model);
        }

        // POST: /Admin/Ngos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NgoFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var ngo = await _context.NGOs.FindAsync(id);
            if (ngo == null) return NotFound();

            if (model.LogoFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.LogoFile, "ngos");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                ngo.LogoUrl = uploadResult.FilePath;
            }

            ngo.Name = model.Name.Trim();
            ngo.RegistrationNumber = model.RegistrationNumber?.Trim();
            ngo.Description = model.Description.Trim();
            ngo.Email = model.Email.Trim();
            ngo.Phone = model.Phone.Trim();
            ngo.Website = model.Website?.Trim();
            ngo.Address = model.Address?.Trim();
            ngo.City = model.City?.Trim();
            ngo.Country = model.Country?.Trim() ?? "Pakistan";
            ngo.EstablishedYear = model.EstablishedYear;
            ngo.IsActive = model.IsActive;
            ngo.DisplayOrder = model.DisplayOrder;
            ngo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UpdateNGO", nameof(NGO), ngo.Id.ToString(), $"Updated NGO '{ngo.Name}' details");

            TempData["SuccessMessage"] = $"NGO '{ngo.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Ngos/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var ngo = await _context.NGOs.FindAsync(id);
            if (ngo == null) return NotFound();

            ngo.IsActive = !ngo.IsActive;
            ngo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"NGO '{ngo.Name}' status updated to {(ngo.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
