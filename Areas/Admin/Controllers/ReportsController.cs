using Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDonationService _donationService;

        public ReportsController(ApplicationDbContext context, IDonationService donationService)
        {
            _context = context;
            _donationService = donationService;
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? causeId, string? paymentMethod)
        {
            var query = _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .Include(d => d.Receipt)
                .Where(d => d.Status == DonationStatus.Paid)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(d => d.CreatedAt <= endOfDay);
            }

            if (causeId.HasValue && causeId.Value > 0)
            {
                query = query.Where(d => d.CauseId == causeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod) && Enum.TryParse<PaymentProvider>(paymentMethod, true, out var prov))
            {
                query = query.Where(d => d.PaymentTransactions.Any(p => p.PaymentMethod == prov));
            }

            var donations = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            var totalAmount = donations.Sum(d => d.Amount);
            var totalDonations = donations.Count;

            var causeSummaries = donations
                .GroupBy(d => d.Cause != null ? d.Cause.Title : "General Relief")
                .Select(g => new ReportCauseSummaryViewModel
                {
                    CauseTitle = g.Key,
                    Category = g.FirstOrDefault()?.Cause?.Category ?? "General",
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(cs => cs.TotalAmount)
                .ToList();

            var causes = await _context.DonationCauses.OrderBy(c => c.Title).ToListAsync();

            var model = new ReportsViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedCauseId = causeId,
                SelectedPaymentMethod = paymentMethod,
                Causes = causes,
                TotalAmount = totalAmount,
                TotalDonations = totalDonations,
                CauseSummaries = causeSummaries,
                Donations = donations
            };

            return View(model);
        }

        // GET: /Admin/Reports/ExportCsv
        public async Task<IActionResult> ExportCsv(DateTime? startDate, DateTime? endDate, int? causeId, string? paymentMethod)
        {
            var query = _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .Include(d => d.Receipt)
                .Where(d => d.Status == DonationStatus.Paid)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(d => d.CreatedAt <= endOfDay);
            }

            if (causeId.HasValue && causeId.Value > 0)
            {
                query = query.Where(d => d.CauseId == causeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod) && Enum.TryParse<PaymentProvider>(paymentMethod, true, out var prov))
            {
                query = query.Where(d => d.PaymentTransactions.Any(p => p.PaymentMethod == prov));
            }

            var donations = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("DonationID,ReceiptNumber,DonorName,DonorEmail,DonorPhone,Cause,Amount,Currency,PaymentMethod,Status,Date");

            foreach (var d in donations)
            {
                var rNum = d.Receipt?.ReceiptNumber ?? "N/A";
                var method = d.PaymentTransactions.LastOrDefault()?.PaymentMethod.ToString() ?? "N/A";
                sb.AppendLine($"\"{d.Id}\",\"{rNum}\",\"{d.DonorName}\",\"{d.DonorEmail}\",\"{d.DonorPhone}\",\"{d.Cause?.Title}\",\"{d.Amount}\",\"{d.Currency}\",\"{method}\",\"{d.Status}\",\"{d.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"GiveAid_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
    }
}
