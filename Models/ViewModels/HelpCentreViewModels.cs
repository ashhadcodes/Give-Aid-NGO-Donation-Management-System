using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.ViewModels
{
    public class HelpCentreViewModel
    {
        public IEnumerable<FaqCategory> Categories { get; set; } = new List<FaqCategory>();
        public List<Faq> Faqs { get; set; } = new();
        public IEnumerable<Faq> FilteredFaqs { get => Faqs; set => Faqs = value?.ToList() ?? new(); }
        public string? SearchQuery { get; set; }
        public int? SelectedCategoryId { get; set; }
        public RaiseQueryViewModel NewQuery { get; set; } = new();
    }

    public class RaiseQueryViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(30)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe your query in detail.")]
        [MaxLength(4000)]
        [Display(Name = "Query Details")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Priority")]
        public QueryPriority Priority { get; set; } = QueryPriority.Medium;
    }
}
