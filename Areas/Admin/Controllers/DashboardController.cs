using Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync();
            var model = new AdminDashboardViewModel
            {
                TotalDonationAmount = summary.TotalDonationAmount,
                TotalDonationCount = summary.TotalDonationsCount,
                PendingDonationsCount = summary.PendingPaymentsCount,
                ApprovedDonationsCount = summary.SuccessfulPaymentsCount,
                TotalDonorsCount = summary.TotalUsersCount,
                ActiveProgrammesCount = summary.ActiveProgrammesCount,
                ActiveCausesCount = summary.ActiveCausesCount,
                TotalPartnerNgos = summary.ActiveNgosCount,
                TotalPartnersCount = summary.ActivePartnersCount,
                OpenQueriesCount = summary.PendingQueriesCount,
                UnreadMessagesCount = summary.NewContactMessagesCount,
                RecentDonations = summary.RecentDonations,
                RecentAuditLogs = summary.RecentAuditLogs.Select(a => new DashboardAuditLogItemViewModel
                {
                    Action = a.Action,
                    Timestamp = a.CreatedAt,
                    Details = a.Description,
                    UserEmail = a.UserName ?? (a.User != null ? a.User.Email : "Admin")
                }).ToList(),
                MonthlyDonations = summary.MonthlyDonations.ToDictionary(m => m.MonthName, m => m.TotalAmount),
                CausesDistribution = summary.DonationsByCause.ToDictionary(c => c.Label, c => c.Amount)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync();
            return Json(new
            {
                monthly = summary.MonthlyDonations,
                causes = summary.DonationsByCause,
                methods = summary.PaymentMethodDistribution
            });
        }
    }
}
