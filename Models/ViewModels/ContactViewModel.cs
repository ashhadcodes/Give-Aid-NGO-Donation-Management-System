using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.ViewModels
{
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
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
        [Display(Name = "Phone Number (Optional)")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [MaxLength(3000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;
    }

    public class ContactPageViewModel
    {
        public ContactFormViewModel Form { get; set; } = new();
        public Dictionary<string, string> SiteSettings { get; set; } = new();
    }

    public class AboutUsPageViewModel
    {
        public string ActiveSection { get; set; } = "what-we-do";
        public AboutPage? PageContent { get; set; }
        public IEnumerable<AboutPage> AllSections { get; set; } = new List<AboutPage>();
        public IEnumerable<Partner> Partners { get; set; } = new List<Partner>();
    }
}
