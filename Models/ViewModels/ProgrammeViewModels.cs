using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.ViewModels
{
    public class ProgrammeListViewModel
    {
        public IEnumerable<Programme> Programmes { get; set; } = new List<Programme>();
        public string? SelectedCategory { get; set; }
        public string? SearchTerm { get; set; }
        public IEnumerable<string> AvailableCategories { get; set; } = new List<string>();
    }

    public class ProgrammeDetailsViewModel
    {
        public Programme Programme { get; set; } = null!;
        public ExpressInterestViewModel InterestForm { get; set; } = new();
        public IEnumerable<Programme> RelatedProgrammes { get; set; } = new List<Programme>();
    }

    public class ExpressInterestViewModel
    {
        [Required]
        public int ProgrammeId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(30)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [MaxLength(500)]
        [Display(Name = "Why do you want to participate / Volunteer notes")]
        public string? Notes { get; set; }
    }
}
