using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public class NGO
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Display(Name = "NGO Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Registration Number")]
        public string? RegistrationNumber { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Logo URL / Path")]
        public string? LogoUrl { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
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

        public int EstablishedYear { get; set; } = 2020;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();
    }
}
