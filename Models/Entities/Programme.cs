using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public enum ProgrammeStatus
    {
        Upcoming = 0,
        Ongoing = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class Programme
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Programme Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "Education"; // Education, Healthcare, Children, Disabled, Women, Youth, Elderly, General

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        [Display(Name = "Cover Image")]
        public string? ImageUrl { get; set; }

        public int Capacity { get; set; } = 100;

        public ProgrammeStatus Status { get; set; } = ProgrammeStatus.Upcoming;

        public bool IsFeatured { get; set; } = false;

        public int? NGOId { get; set; }

        [ForeignKey("NGOId")]
        public virtual NGO? NGO { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<ProgrammeInterest> Interests { get; set; } = new List<ProgrammeInterest>();
        public virtual ICollection<GalleryItem> GalleryItems { get; set; } = new List<GalleryItem>();
    }
}
