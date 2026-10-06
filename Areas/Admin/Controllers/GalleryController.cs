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
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IAuditService _auditService;

        public GalleryController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            IAuditService auditService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _auditService = auditService;
        }

        // GET: /Admin/Gallery
        public async Task<IActionResult> Index()
        {
            var gallery = await _context.GalleryItems
                .Include(g => g.Programme)
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.CreatedAt)
                .ToListAsync();
            return View(gallery);
        }

        // GET: /Admin/Gallery/Create
        public async Task<IActionResult> Create()
        {
            var programmes = await _context.Programmes.OrderBy(p => p.Title).ToListAsync();
            return View(new GalleryFormViewModel { AvailableProgrammes = programmes });
        }

        // POST: /Admin/Gallery/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GalleryFormViewModel model)
        {
            if (model.ImageFile == null && string.IsNullOrEmpty(model.ImageUrl))
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Please upload an image or provide an image URL.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableProgrammes = await _context.Programmes.OrderBy(p => p.Title).ToListAsync();
                return View(model);
            }

            string imageUrl = model.ImageUrl ?? "";
            if (model.ImageFile != null)
            {
                var uploadResult = await _fileUploadService.UploadFileAsync(model.ImageFile, "gallery");
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), uploadResult.ErrorMessage!);
                    model.AvailableProgrammes = await _context.Programmes.OrderBy(p => p.Title).ToListAsync();
                    return View(model);
                }
                imageUrl = uploadResult.FilePath!;
            }

            var item = new GalleryItem
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Category = model.Category?.Trim() ?? "General",
                ImageUrl = imageUrl,
                ProgrammeId = model.ProgrammeId,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.GalleryItems.Add(item);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreateGalleryItem", nameof(GalleryItem), item.Id.ToString(), $"Added gallery photo '{item.Title}'");

            TempData["SuccessMessage"] = "Gallery image added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Gallery/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.GalleryItems.FindAsync(id);
            if (item != null)
            {
                _fileUploadService.DeleteFile(item.ImageUrl);
                _context.GalleryItems.Remove(item);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("DeleteGalleryItem", nameof(GalleryItem), id.ToString(), $"Deleted gallery item '{item.Title}'");
                TempData["SuccessMessage"] = "Gallery item deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
