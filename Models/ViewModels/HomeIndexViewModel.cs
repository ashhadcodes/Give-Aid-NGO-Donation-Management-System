using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;

namespace Give_Aid_NGO_Donation_Management_System.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public decimal TotalDonationsRaised { get; set; }
        public int TotalDonationsCount { get; set; }
        public int TotalPeopleHelped { get; set; }
        public int TotalActiveProgrammes { get; set; }
        public int TotalPartnerNgos { get; set; }

        public IEnumerable<DonationCause> FeaturedCauses { get; set; } = new List<DonationCause>();
        public IEnumerable<Programme> LatestProgrammes { get; set; } = new List<Programme>();
        public IEnumerable<NGO> FeaturedNgos { get; set; } = new List<NGO>();
        public IEnumerable<Partner> Partners { get; set; } = new List<Partner>();
        public IEnumerable<GalleryItem> RecentGallery { get; set; } = new List<GalleryItem>();
        public IEnumerable<Faq> TopFaqs { get; set; } = new List<Faq>();
    }
}
