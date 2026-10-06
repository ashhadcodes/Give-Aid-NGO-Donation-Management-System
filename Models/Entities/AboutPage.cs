using System;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public class AboutPage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SectionKey { get; set; } = string.Empty; // what-we-do, our-mission, our-team, career, achievements, supporters, read-about-us

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Subtitle { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        [MaxLength(500)]
        public string? FeaturedImageUrl { get; set; }

        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }
    }
}
