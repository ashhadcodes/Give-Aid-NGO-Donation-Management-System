using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return await RenderSection("what-we-do");
        }

        public async Task<IActionResult> WhatWeDo()
        {
            return await RenderSection("what-we-do");
        }

        public async Task<IActionResult> OurMission()
        {
            return await RenderSection("our-mission");
        }

        public async Task<IActionResult> OurTeam()
        {
            return await RenderSection("our-team");
        }

        public async Task<IActionResult> Career()
        {
            return await RenderSection("career");
        }

        public async Task<IActionResult> Achievements()
        {
            return await RenderSection("achievements");
        }

        public async Task<IActionResult> Supporters()
        {
            return await RenderSection("supporters");
        }

        public async Task<IActionResult> ReadAboutUs()
        {
            return await RenderSection("read-about-us");
        }

        private async Task<IActionResult> RenderSection(string sectionKey)
        {
            var page = await _context.AboutPages.FirstOrDefaultAsync(p => p.SectionKey == sectionKey)
                       ?? await _context.AboutPages.FirstOrDefaultAsync()
                       ?? new AboutPage { Title = "About Give-AID", SectionKey = sectionKey, Content = "<p>Give-AID is a certified welfare ecosystem.</p>" };

            return View("AboutSection", page);
        }
    }
}
