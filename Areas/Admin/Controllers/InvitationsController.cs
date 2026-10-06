using Give_Aid_NGO_Donation_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class InvitationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvitationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Invitations
        public async Task<IActionResult> Index()
        {
            var invitations = await _context.Invitations
                .Include(i => i.SenderUser)
                .OrderByDescending(i => i.SentAt)
                .ToListAsync();
            return View(invitations);
        }
    }
}
