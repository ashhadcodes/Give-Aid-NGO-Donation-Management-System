using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.ViewModels
{
    public class CauseFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(150)]
        [Display(Name = "Cause Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(50)]
        [Display(Name = "Code / Identifier")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(100)]
        public string Category { get; set; } = "General Welfare";

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Target Amount (PKR)")]
        [Range(100, 1000000000, ErrorMessage = "Target amount must be at least 100 PKR.")]
        public decimal TargetAmount { get; set; } = 100000m;

        [Display(Name = "Raised Amount (PKR)")]
        public decimal RaisedAmount { get; set; } = 0m;

        public string? ImageUrl { get; set; }

        [Display(Name = "Upload New Image")]
        public IFormFile? ImageFile { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    public class NgoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "NGO Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Registration Number")]
        public string? RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        [Display(Name = "Upload NGO Logo")]
        public IFormFile? LogoFile { get; set; }

        [Required(ErrorMessage = "Official Email is required.")]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; } = "Pakistan";

        [Display(Name = "Established Year")]
        public int EstablishedYear { get; set; } = DateTime.UtcNow.Year;

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class PartnerFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Partner Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        [Display(Name = "Upload Partner Logo")]
        public IFormFile? LogoFile { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class ProgrammeFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Programme title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "Education";

        [Required(ErrorMessage = "Location is required.")]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);

        public string? ImageUrl { get; set; }

        [Display(Name = "Upload Cover Image")]
        public IFormFile? ImageFile { get; set; }

        public int Capacity { get; set; } = 100;
        public ProgrammeStatus Status { get; set; } = ProgrammeStatus.Upcoming;
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Affiliated NGO (Optional)")]
        public int? NGOId { get; set; }

        public IEnumerable<NGO>? AvailableNgos { get; set; }
    }

    public class GalleryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; } = "General";

        public string? ImageUrl { get; set; }

        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Associated Programme (Optional)")]
        public int? ProgrammeId { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public IEnumerable<Programme>? AvailableProgrammes { get; set; }
    }

    public class FaqFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "FAQ Category")]
        public int FaqCategoryId { get; set; }

        [Required(ErrorMessage = "Question is required.")]
        [MaxLength(300)]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Answer is required.")]
        public string Answer { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public IEnumerable<FaqCategory>? AvailableCategories { get; set; }
    }

    public class UserManagementItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public string? Profession { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public int TotalDonations { get; set; }
        public decimal TotalDonatedAmount { get; set; }
    }

    public class EditUserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> AssignedRoles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
    }

    public class QueryReplyViewModel
    {
        public int QueryId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public QueryPriority Priority { get; set; }
        public QueryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required(ErrorMessage = "Please write a response before sending.")]
        [MaxLength(4000)]
        [Display(Name = "Admin Response")]
        public string AdminResponse { get; set; } = string.Empty;

        [Display(Name = "Send Email Notification to User")]
        public bool SendEmailNotification { get; set; } = true;
    }

    public class DonationReportViewModel
    {
        public DonationFilterParams Filter { get; set; } = new();
        public PagedResult<Donation> PagedDonations { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal SuccessfulAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal FailedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public IEnumerable<DonationCause> AvailableCauses { get; set; } = new List<DonationCause>();
    }

    public class AuditLogItemViewModel
    {
        public DateTime Timestamp { get; set; }
        public string? UserEmail { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class AuditLogsViewModel
    {
        public string? SearchTerm { get; set; }
        public string? ActionFilter { get; set; }
        public string? EntityFilter { get; set; }
        public List<AuditLogItemViewModel> Logs { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }

    public class DashboardAuditLogItemViewModel
    {
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TotalDonationAmount { get; set; }
        public int TotalDonationCount { get; set; }
        public int PendingDonationsCount { get; set; }
        public int ApprovedDonationsCount { get; set; }
        public int TotalDonorsCount { get; set; }
        public int ActiveProgrammesCount { get; set; }
        public int ActiveCausesCount { get; set; }
        public int TotalPartnerNgos { get; set; }
        public int TotalPartnersCount { get; set; }
        public int OpenQueriesCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public IEnumerable<Donation> RecentDonations { get; set; } = new List<Donation>();
        public IEnumerable<DashboardAuditLogItemViewModel> RecentAuditLogs { get; set; } = new List<DashboardAuditLogItemViewModel>();
        public Dictionary<string, decimal> MonthlyDonations { get; set; } = new();
        public Dictionary<string, decimal> CausesDistribution { get; set; } = new();
    }

    public class DonationListViewModel
    {
        public string? SearchTerm { get; set; }
        public IEnumerable<DonationCause> Causes { get; set; } = new List<DonationCause>();
        public int? SelectedCauseId { get; set; }
        public DonationStatus? SelectedStatus { get; set; }
        public string? SelectedPaymentMethod { get; set; }
        public IEnumerable<Donation> Donations { get; set; } = new List<Donation>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }

    public class ReportCauseSummaryViewModel
    {
        public string CauseTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReportsViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SelectedCauseId { get; set; }
        public string? SelectedPaymentMethod { get; set; }
        public IEnumerable<DonationCause> Causes { get; set; } = new List<DonationCause>();
        public decimal TotalAmount { get; set; }
        public int TotalDonations { get; set; }
        public List<ReportCauseSummaryViewModel> CauseSummaries { get; set; } = new();
        public IEnumerable<Donation> Donations { get; set; } = new List<Donation>();
    }
}
