using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Contact
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SiteSettings
                .Where(s => s.Group == "Contact" || s.Group == "General")
                .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

            var model = new ContactPageViewModel
            {
                SiteSettings = settings,
                Form = new ContactFormViewModel()
            };

            return View(model);
        }

        // POST: /Contact/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ContactPageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.SiteSettings = await _context.SiteSettings
                    .Where(s => s.Group == "Contact" || s.Group == "General")
                    .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

                return View("Index", model);
            }

            var contactMessage = new ContactMessage
            {
                Name = model.Form.Name.Trim(),
                Email = model.Form.Email.Trim(),
                Phone = model.Form.Phone?.Trim(),
                Subject = model.Form.Subject.Trim(),
                Message = model.Form.Message.Trim(),
                Status = MessageStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for reaching out to Give-AID! Your message has been received and our team will contact you shortly.";
            return RedirectToAction(nameof(Index));
        }
    }
}
