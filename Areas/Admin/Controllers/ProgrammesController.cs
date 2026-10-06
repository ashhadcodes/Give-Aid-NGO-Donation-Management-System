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
    public class ProgrammesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IAuditService _auditService;

        public ProgrammesController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            IAuditService auditService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _auditService = auditService;
        }

        // GET: /Admin/Programmes
        public async Task<IActionResult> Index()
        {
            var programmes = await _context.Programmes
                .Include(p => p.NGO)
                .Include(p => p.Interests)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            return View(programmes);
        }

        // GET: /Admin/Programmes/Create
        public async Task<IActionResult> Create()
        {
            var ngos = await _context.NGOs.Where(n => n.IsActive).OrderBy(n => n.Name).ToListAsync();
            return View(new ProgrammeFormViewModel { AvailableNgos = ngos });
        }

        // POST: /Admin/Programmes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProgrammeFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableNgos = await _context.NGOs.Where(n => n.IsActive).OrderBy(n => n.Name).ToListAsync();
                return View(model);
            }

            string? imageUrl = model.ImageUrl;
            if (model.ImageFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.ImageFile, "programmes");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), uploadResult.ErrorMessage!);
                    model.AvailableNgos = await _context.NGOs.Where(n => n.IsActive).OrderBy(n => n.Name).ToListAsync();
                    return View(model);
                }
                imageUrl = uploadResult.FilePath;
            }

            var programme = new Programme
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Category = model.Category.Trim(),
                Location = model.Location.Trim(),
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                ImageUrl = imageUrl,
                Capacity = model.Capacity,
                Status = model.Status,
                IsFeatured = model.IsFeatured,
                NGOId = model.NGOId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Programmes.Add(programme);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreateProgramme", nameof(Programme), programme.Id.ToString(), $"Created welfare programme '{programme.Title}'");

            TempData["SuccessMessage"] = $"Programme '{programme.Title}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Programmes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            var ngos = await _context.NGOs.Where(n => n.IsActive).OrderBy(n => n.Name).ToListAsync();

            var model = new ProgrammeFormViewModel
            {
                Id = programme.Id,
                Title = programme.Title,
                Description = programme.Description,
                Category = programme.Category,
                Location = programme.Location,
                StartDate = programme.StartDate,
                EndDate = programme.EndDate,
                ImageUrl = programme.ImageUrl,
                Capacity = programme.Capacity,
                Status = programme.Status,
                IsFeatured = programme.IsFeatured,
                NGOId = programme.NGOId,
                AvailableNgos = ngos
            };

            return View(model);
        }

        // POST: /Admin/Programmes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProgrammeFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                model.AvailableNgos = await _context.NGOs.Where(n => n.IsActive).OrderBy(n => n.Name).ToListAsync();
                return View(model);
            }

            var programme = await _context.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            if (model.ImageFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.ImageFile, "programmes");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), uploadResult.ErrorMessage!);
                    model.AvailableNgos = await _context.NGOs.Where(n => n.IsActive).OrderBy(n => n.Name).ToListAsync();
                    return View(model);
                }
                programme.ImageUrl = uploadResult.FilePath;
            }

            programme.Title = model.Title.Trim();
            programme.Description = model.Description.Trim();
            programme.Category = model.Category.Trim();
            programme.Location = model.Location.Trim();
            programme.StartDate = model.StartDate;
            programme.EndDate = model.EndDate;
            programme.Capacity = model.Capacity;
            programme.Status = model.Status;
            programme.IsFeatured = model.IsFeatured;
            programme.NGOId = model.NGOId;
            programme.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UpdateProgramme", nameof(Programme), programme.Id.ToString(), $"Updated programme '{programme.Title}'");

            TempData["SuccessMessage"] = $"Programme '{programme.Title}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Programmes/Interests/5
        public async Task<IActionResult> Interests(int id)
        {
            var programme = await _context.Programmes
                .Include(p => p.Interests)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (programme == null) return NotFound();
            return View(programme);
        }
    }
}
