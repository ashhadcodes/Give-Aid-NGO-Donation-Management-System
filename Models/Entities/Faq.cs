using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public class Faq
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FaqCategoryId { get; set; }

        [ForeignKey("FaqCategoryId")]
        public virtual FaqCategory? Category { get; set; }

        [Required]
        [MaxLength(300)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
