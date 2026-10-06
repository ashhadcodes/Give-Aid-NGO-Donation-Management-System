using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class DonationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDonationService _donationService;
        private readonly IAuditService _auditService;

        public DonationsController(
            ApplicationDbContext context,
            IDonationService donationService,
            IAuditService auditService)
        {
            _context = context;
            _donationService = donationService;
            _auditService = auditService;
        }

        // GET: /Admin/Donations
        public async Task<IActionResult> Index(string? searchTerm, int? causeId, DonationStatus? status, string? paymentMethod, int page = 1)
        {
            PaymentProvider? parsedPayment = null;
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                if (paymentMethod.Equals("CreditCard", StringComparison.OrdinalIgnoreCase))
                {
                    parsedPayment = PaymentProvider.Card;
                }
                else if (Enum.TryParse<PaymentProvider>(paymentMethod, true, out var prov))
                {
                    parsedPayment = prov;
                }
            }

            var filter = new DonationFilterParams
            {
                Search = searchTerm,
                CauseId = causeId,
                Status = status,
                PaymentMethod = parsedPayment,
                PageNumber = page < 1 ? 1 : page,
                PageSize = 15
            };

            var pagedResult = await _donationService.GetPagedDonationsAsync(filter);
            var causes = await _context.DonationCauses.OrderBy(c => c.Title).ToListAsync();

            var vm = new Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels.DonationListViewModel
            {
                SearchTerm = searchTerm,
                SelectedCauseId = causeId,
                SelectedStatus = status,
                SelectedPaymentMethod = paymentMethod,
                Causes = causes,
                Donations = pagedResult.Items,
                CurrentPage = pagedResult.PageNumber,
                TotalPages = pagedResult.TotalPages < 1 ? 1 : pagedResult.TotalPages,
                TotalCount = pagedResult.TotalItems
            };

            return View(vm);
        }

        // GET: /Admin/Donations/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var donation = await _donationService.GetDonationByIdAsync(id);
            if (donation == null) return NotFound();
            return View(donation);
        }

        // POST: /Admin/Donations/Refund/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id, string? reason)
        {
            var success = await _donationService.RefundDonationAsync(id, reason);
            if (success)
            {
                TempData["SuccessMessage"] = $"Donation #{id} has been refunded successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Donation #{id} could not be refunded.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/Donations/ApproveManual/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveManual(int id, string? notes)
        {
            var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Admin";
            var adminEmail = User.Identity?.Name ?? "Admin";
            var success = await _donationService.ApproveManualDonationAsync(id, adminUserId, adminEmail, notes);

            if (success)
            {
                TempData["SuccessMessage"] = $"Manual donation #{id} has been verified and approved. Official digital receipt generated and emailed to donor.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Donation #{id} could not be approved or is already paid.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/Donations/RejectManual/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectManual(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "A rejection reason is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Admin";
            var adminEmail = User.Identity?.Name ?? "Admin";
            var success = await _donationService.RejectManualDonationAsync(id, adminUserId, adminEmail, reason);

            if (success)
            {
                TempData["SuccessMessage"] = $"Manual donation #{id} has been rejected.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Donation #{id} could not be rejected or is already processed.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Admin/Donations/ExportCsv
        public async Task<IActionResult> ExportCsv([FromQuery] DonationFilterParams filter)
        {
            filter.PageSize = 10000;
            filter.PageNumber = 1;
            var result = await _donationService.GetPagedDonationsAsync(filter);

            var sb = new StringBuilder();
            sb.AppendLine("DonationID,ReceiptNumber,DonorName,DonorEmail,DonorPhone,Cause,Amount,Currency,PaymentMethod,Status,CreatedAt");

            foreach (var d in result.Items)
            {
                var receiptNum = d.Receipt?.ReceiptNumber ?? "";
                var method = d.PaymentTransactions.LastOrDefault()?.PaymentMethod.ToString() ?? "";
                sb.AppendLine($"\"{d.Id}\",\"{receiptNum}\",\"{d.DonorName}\",\"{d.DonorEmail}\",\"{d.DonorPhone}\",\"{d.Cause?.Title}\",\"{d.Amount}\",\"{d.Currency}\",\"{method}\",\"{d.Status}\",\"{d.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"GiveAid_Donations_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
    }
}
