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
    public class FaqsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FaqsController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: /Admin/Faqs
        public async Task<IActionResult> Index()
        {
            var faqs = await _context.Faqs
                .Include(f => f.Category)
                .OrderBy(f => f.Category!.DisplayOrder)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();
            return View(faqs);
        }

        // GET: /Admin/Faqs/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _context.FaqCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            return View(new FaqFormViewModel { AvailableCategories = categories });
        }

        // POST: /Admin/Faqs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FaqFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCategories = await _context.FaqCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
                return View(model);
            }

            var faq = new Faq
            {
                FaqCategoryId = model.FaqCategoryId,
                Question = model.Question.Trim(),
                Answer = model.Answer.Trim(),
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Faqs.Add(faq);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CreateFAQ", nameof(Faq), faq.Id.ToString(), $"Created FAQ '{faq.Question}'");

            TempData["SuccessMessage"] = "FAQ created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Faqs/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return NotFound();

            var categories = await _context.FaqCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            var model = new FaqFormViewModel
            {
                Id = faq.Id,
                FaqCategoryId = faq.FaqCategoryId,
                Question = faq.Question,
                Answer = faq.Answer,
                DisplayOrder = faq.DisplayOrder,
                IsActive = faq.IsActive,
                AvailableCategories = categories
            };

            return View(model);
        }

        // POST: /Admin/Faqs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FaqFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                model.AvailableCategories = await _context.FaqCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
                return View(model);
            }

            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return NotFound();

            faq.FaqCategoryId = model.FaqCategoryId;
            faq.Question = model.Question.Trim();
            faq.Answer = model.Answer.Trim();
            faq.DisplayOrder = model.DisplayOrder;
            faq.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("UpdateFAQ", nameof(Faq), faq.Id.ToString(), $"Updated FAQ '{faq.Question}'");

            TempData["SuccessMessage"] = "FAQ updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Faqs/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq != null)
            {
                _context.Faqs.Remove(faq);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("DeleteFAQ", nameof(Faq), id.ToString(), $"Deleted FAQ '{faq.Question}'");
                TempData["SuccessMessage"] = "FAQ deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Faqs/Categories
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.FaqCategories.Include(c => c.Faqs).OrderBy(c => c.DisplayOrder).ToListAsync();
            return View(categories);
        }

        // POST: /Admin/Faqs/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, string? description, int displayOrder)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var cat = new FaqCategory
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    DisplayOrder = displayOrder,
                    IsActive = true
                };
                _context.FaqCategories.Add(cat);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "FAQ Category added.";
            }
            return RedirectToAction(nameof(Categories));
        }
    }
}
