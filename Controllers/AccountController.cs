using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService,
            IAuditService auditService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                Country = model.Country?.Trim() ?? "Pakistan",
                Profession = model.Profession?.Trim(),
                DateOfBirth = model.DateOfBirth,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                _logger.LogInformation("New user registered: {Email}", user.Email);

                _ = _emailService.SendRegistrationWelcomeAsync(user.Email, user.FullName);
                await _auditService.LogAsync("UserRegister", nameof(ApplicationUser), user.Id, $"User {user.Email} registered successfully.", user.Id, user.FullName);

                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = $"Welcome to Give-AID, {user.FullName}! Your registration was successful.";
                return RedirectToAction(nameof(Dashboard));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(currentUser);
                    if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                }
                return RedirectToAction(nameof(Dashboard));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailOrUser = model.Email?.Trim() ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(emailOrUser) 
                    ?? await _userManager.FindByNameAsync(emailOrUser);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials. No account found with this email.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account is currently deactivated. Please contact administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in: {Email}", model.Email);

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && 
                        (returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) || 
                         returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Dashboard));
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your password (default for testing is 'admin123').");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);
                _ = _emailService.SendPasswordResetAsync(model.Email, resetLink ?? "");
            }

            // Always display same message to avoid account enumeration
            TempData["SuccessMessage"] = "If your email is registered with us, a password reset link has been dispatched.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid password reset token.";
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["SuccessMessage"] = "Your password has been successfully reset.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password has been successfully updated. You can now login.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/Dashboard
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var donations = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.Receipt)
                .Include(d => d.PaymentTransactions)
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var interests = await _context.ProgrammeInterests
                .Include(p => p.Programme)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.ExpressedAt)
                .ToListAsync();

            var queries = await _context.SupportQueries
                .Where(q => q.UserId == user.Id || q.Email == user.Email)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var invitations = await _context.Invitations
                .Where(i => i.SenderUserId == user.Id)
                .OrderByDescending(i => i.SentAt)
                .ToListAsync();

            var totalDonated = donations.Where(d => d.Status == DonationStatus.Paid).Sum(d => d.Amount);

            var model = new UserDashboardViewModel
            {
                User = user,
                TotalDonationsCount = donations.Count,
                TotalDonatedAmount = totalDonated,
                RegisteredProgrammesCount = interests.Count,
                TotalQueriesCount = queries.Count,
                RecentDonations = donations.Take(5),
                ProgrammeInterests = interests,
                Queries = queries,
                InvitationsSent = invitations
            };

            return View(model);
        }

        // GET: /Account/Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                Country = user.Country,
                Profession = user.Profession,
                DateOfBirth = user.DateOfBirth
            };

            return View(model);
        }

        // POST: /Account/Profile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Address = model.Address?.Trim();
            user.City = model.City?.Trim();
            user.Country = model.Country?.Trim() ?? "Pakistan";
            user.Profession = model.Profession?.Trim();
            user.DateOfBirth = model.DateOfBirth;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your profile information has been updated.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/ChangePassword
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Password has been successfully changed.";
                return RedirectToAction(nameof(Dashboard));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/MyDonations
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyDonations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var donations = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.Receipt)
                .Include(d => d.PaymentTransactions)
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(donations);
        }

        // GET: /Account/MyInterests
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyInterests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var interests = await _context.ProgrammeInterests
                .Include(p => p.Programme)
                    .ThenInclude(prog => prog!.NGO)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.ExpressedAt)
                .ToListAsync();

            return View(interests);
        }

        // GET: /Account/MyQueries
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyQueries()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var queries = await _context.SupportQueries
                .Where(q => q.UserId == user.Id || q.Email == user.Email)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(queries);
        }

        // GET: /Account/InviteFriend
        [Authorize]
        [HttpGet]
        public IActionResult InviteFriend()
        {
            return View();
        }

        // POST: /Account/InviteFriend
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteFriend(InviteFriendViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var invitation = new Invitation
            {
                SenderUserId = user.Id,
                SenderName = user.FullName,
                SenderEmail = user.Email ?? "",
                FriendName = model.FriendName.Trim(),
                FriendEmail = model.FriendEmail.Trim(),
                Message = model.Message?.Trim(),
                Status = InvitationStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            _context.Invitations.Add(invitation);
            await _context.SaveChangesAsync();

            var siteUrl = Url.Action("Index", "Home", null, Request.Scheme) ?? "https://giveaid.org";
            _ = _emailService.SendFriendInvitationEmailAsync(
                invitation.FriendEmail,
                invitation.FriendName,
                invitation.SenderName,
                invitation.Message,
                siteUrl
            );

            TempData["SuccessMessage"] = $"Your invitation has been sent to {model.FriendName} ({model.FriendEmail})!";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
