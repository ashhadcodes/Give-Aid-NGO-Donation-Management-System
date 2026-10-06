using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Give_Aid_NGO_Donation_Management_System.Services.Payments;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDonationService _donationService;
        private readonly IReceiptService _receiptService;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DonationController> _logger;

        public DonationController(
            ApplicationDbContext context,
            IDonationService donationService,
            IReceiptService receiptService,
            IFileUploadService fileUploadService,
            UserManager<ApplicationUser> userManager,
            ILogger<DonationController> logger)
        {
            _context = context;
            _donationService = donationService;
            _receiptService = receiptService;
            _fileUploadService = fileUploadService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Donation
        public async Task<IActionResult> Index(string? searchString, string? category)
        {
            var query = _context.DonationCauses
                .Where(c => c.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                query = query.Where(c => c.Title.Contains(term) ||
                                         c.Description.Contains(term) ||
                                         c.Category.Contains(term) ||
                                         c.Code.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Category == category);
            }

            var causes = await query.OrderBy(c => c.DisplayOrder).ToListAsync();

            var categories = await _context.DonationCauses
                .Where(c => c.IsActive && !string.IsNullOrEmpty(c.Category))
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.SearchString = searchString;

            return View(causes);
        }

        // GET: /Donation/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var cause = await _context.DonationCauses
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (cause == null)
            {
                TempData["ErrorMessage"] = "The requested welfare cause could not be found or is inactive.";
                return RedirectToAction(nameof(Index));
            }

            return View(cause);
        }

        // GET: /Donation/Donate?causeId=1
        public async Task<IActionResult> Donate(int? causeId)
        {
            var causes = await _context.DonationCauses
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            if (!causes.Any())
            {
                TempData["ErrorMessage"] = "No active donation causes are currently available.";
                return RedirectToAction("Index", "Home");
            }

            var selectedCause = causeId.HasValue
                ? causes.FirstOrDefault(c => c.Id == causeId.Value) ?? causes.First()
                : causes.First();

            var model = new DonateFormViewModel
            {
                CauseId = selectedCause.Id,
                SelectedCause = selectedCause,
                AvailableCauses = causes,
                Amount = 1000m,
                PaymentMethod = PaymentProvider.JazzCash
            };

            // If user is authenticated, pre-fill personal info
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.DonorName = user.FullName;
                    model.DonorEmail = user.Email ?? string.Empty;
                    model.DonorPhone = user.PhoneNumber;
                }
            }

            return View(model);
        }

        // POST: /Donation/Donate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Donate(DonateFormViewModel model)
        {
            var causes = await _context.DonationCauses
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            model.AvailableCauses = causes;
            model.SelectedCause = causes.FirstOrDefault(c => c.Id == model.CauseId);

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Donation amount must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string? currentUserId = null;
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                var donation = new Donation
                {
                    UserId = currentUserId,
                    CauseId = model.CauseId,
                    Amount = model.Amount,
                    Currency = "PKR",
                    DonorName = model.DonorName.Trim(),
                    DonorEmail = model.DonorEmail.Trim(),
                    DonorPhone = model.DonorPhone?.Trim(),
                    Anonymous = model.Anonymous,
                    Message = model.Message?.Trim(),
                    Status = DonationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                var createdDonation = await _donationService.CreateDonationAsync(donation);

                string? proofFilePath = null;
                if (model.ProofFile != null && model.ProofFile.Length > 0)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(model.ProofFile, "payment_proofs");
                    if (uploadResult.Success)
                    {
                        proofFilePath = uploadResult.FilePath;
                    }
                }

                var paymentRequest = new PaymentRequestDto
                {
                    DonationId = createdDonation.Id,
                    InternalTransactionId = Guid.NewGuid().ToString("N"),
                    Amount = model.Amount,
                    Currency = "PKR",
                    DonorName = model.DonorName,
                    DonorEmail = model.DonorEmail,
                    DonorPhone = model.DonorPhone,
                    PaymentMethod = model.PaymentMethod,
                    CardNumber = model.CardNumber,
                    CardHolderName = model.CardHolderName,
                    ExpiryMonth = model.ExpiryMonth,
                    ExpiryYear = model.ExpiryYear,
                    Cvv = model.Cvv,
                    MobileAccountNumber = model.MobileAccountNumber,
                    CnicLast6Digits = model.CnicLast6Digits,
                    DepositReference = model.DepositReference,
                    DepositorBank = model.DepositorBank,
                    DepositDate = model.DepositDate,
                    ProofFilePath = proofFilePath,
                    TransferNotes = model.TransferNotes,
                    ReturnUrl = Url.Action("PaymentCallback", "Donation", new { provider = model.PaymentMethod.ToString() }, Request.Scheme)
                };

                var paymentResult = await _donationService.InitiateAndProcessPaymentAsync(createdDonation.Id, paymentRequest);

                if (paymentResult.RequiresRedirect && !string.IsNullOrEmpty(paymentResult.RedirectUrl))
                {
                    return Redirect(paymentResult.RedirectUrl);
                }

                if (paymentResult.IsSuccessful && paymentResult.Status == DonationStatus.Paid)
                {
                    TempData["SuccessMessage"] = "Thank you! Your donation was processed successfully.";
                    return RedirectToAction(nameof(PaymentSuccess), new { id = createdDonation.Id });
                }

                if (paymentResult.IsSuccessful && paymentResult.Status == DonationStatus.Pending)
                {
                    TempData["SuccessMessage"] = "Thank you! Your manual bank transfer donation details have been received and are pending administrative verification.";
                    return RedirectToAction(nameof(PaymentSuccess), new { id = createdDonation.Id });
                }

                TempData["ErrorMessage"] = paymentResult.FailureReason ?? paymentResult.Message;
                return RedirectToAction(nameof(PaymentFailed), new { id = createdDonation.Id, reason = paymentResult.FailureReason });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing donation.");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while processing your donation. Please try again.");
                return View(model);
            }
        }

        // GET: /Donation/PaymentSuccess/5
        public async Task<IActionResult> PaymentSuccess(int id)
        {
            var donation = await _donationService.GetDonationByIdAsync(id);
            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // GET: /Donation/PaymentFailed/5
        public async Task<IActionResult> PaymentFailed(int id, string? reason)
        {
            var donation = await _donationService.GetDonationByIdAsync(id);
            ViewBag.FailureReason = reason ?? "Payment could not be authorized.";
            return View(donation);
        }

        // GET: /Donation/Receipt/5
        public async Task<IActionResult> Receipt(int id)
        {
            var receipt = await _receiptService.GetReceiptByDonationIdAsync(id);
            if (receipt == null)
            {
                var donation = await _donationService.GetDonationByIdAsync(id);
                if (donation != null && donation.Status == DonationStatus.Paid)
                {
                    receipt = await _receiptService.GenerateReceiptAsync(id);
                }
                else
                {
                    TempData["ErrorMessage"] = "Receipt is only available for paid donations.";
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(receipt);
        }

        // GET: /Donation/PrintReceipt/5
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var receipt = await _receiptService.GetReceiptByDonationIdAsync(id);
            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        // GET/POST: /Donation/PaymentCallback
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> PaymentCallback([FromQuery] string? provider)
        {
            _logger.LogInformation("Received gateway payment callback for provider: {Provider}", provider);

            var formFields = Request.HasFormContentType
                ? Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString())
                : Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            if (!Enum.TryParse<PaymentProvider>(provider, true, out var paymentProvider))
            {
                paymentProvider = PaymentProvider.JazzCash;
            }

            formFields.TryGetValue("pp_TxnRefNo", out var jcRef);
            formFields.TryGetValue("orderId", out var epRef);
            formFields.TryGetValue("pp_ResponseCode", out var jcCode);
            formFields.TryGetValue("responseCode", out var epCode);
            formFields.TryGetValue("pp_ResponseMessage", out var jcMsg);
            formFields.TryGetValue("desc", out var epMsg);
            formFields.TryGetValue("pp_SecureHash", out var jcHash);

            var callbackDto = new PaymentCallbackDto
            {
                Provider = paymentProvider,
                TransactionId = jcRef ?? epRef ?? formFields.GetValueOrDefault("TransactionId"),
                GatewayTransactionId = formFields.GetValueOrDefault("pp_TxnRefNo") ?? formFields.GetValueOrDefault("orderId"),
                ResponseCode = jcCode ?? epCode,
                ResponseMessage = jcMsg ?? epMsg,
                SecureHash = jcHash,
                FormFields = formFields
            };

            var result = await _donationService.HandlePaymentCallbackAsync(callbackDto);

            var txn = await _context.PaymentTransactions.FirstOrDefaultAsync(p => p.TransactionId == callbackDto.TransactionId);
            if (txn != null)
            {
                if (result.IsSuccessful)
                {
                    return RedirectToAction(nameof(PaymentSuccess), new { id = txn.DonationId });
                }
                return RedirectToAction(nameof(PaymentFailed), new { id = txn.DonationId, reason = result.FailureReason });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
