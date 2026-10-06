using Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var list = new List<UserManagementItemViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var donations = await _context.Donations.Where(d => d.UserId == u.Id && d.Status == DonationStatus.Paid).ToListAsync();

                list.Add(new UserManagementItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber,
                    City = u.City,
                    Profession = u.Profession,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    Roles = roles,
                    TotalDonations = donations.Count,
                    TotalDonatedAmount = donations.Sum(d => d.Amount)
                });
            }

            return View(list);
        }

        // GET: /Admin/Users/Details/guid
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var donations = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.Receipt)
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var interests = await _context.ProgrammeInterests
                .Include(p => p.Programme)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.ExpressedAt)
                .ToListAsync();

            ViewBag.Roles = roles;
            ViewBag.Donations = donations;
            ViewBag.Interests = interests;

            return View(user);
        }

        // POST: /Admin/Users/ToggleActive/guid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync("ToggleUserActive", nameof(ApplicationUser), user.Id, $"Toggled active status of user '{user.Email}' to {user.IsActive}");

            TempData["SuccessMessage"] = $"User {user.Email} is now {(user.IsActive ? "Active" : "Deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Users/EditRoles/guid
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                AssignedRoles = userRoles.ToList(),
                AllRoles = allRoles
            };

            return View(model);
        }

        // POST: /Admin/Users/EditRoles/guid
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> EditRoles(EditUserRolesViewModel model, List<string> selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            selectedRoles ??= new List<string>();

            // Remove unselected roles
            var toRemove = currentRoles.Except(selectedRoles);
            await _userManager.RemoveFromRolesAsync(user, toRemove);

            // Add newly selected roles
            var toAdd = selectedRoles.Except(currentRoles);
            await _userManager.AddToRolesAsync(user, toAdd);

            await _auditService.LogAsync("UpdateUserRoles", nameof(ApplicationUser), user.Id, $"Updated roles for {user.Email} to [{string.Join(", ", selectedRoles)}]");

            TempData["SuccessMessage"] = $"Roles updated for user {user.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
