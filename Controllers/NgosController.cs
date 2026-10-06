using Give_Aid_NGO_Donation_Management_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Controllers
{
    public class NgosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NgosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Ngos
        public async Task<IActionResult> Index()
        {
            var ngos = await _context.NGOs
                .Where(n => n.IsActive)
                .Include(n => n.Programmes)
                .OrderBy(n => n.DisplayOrder)
                .ToListAsync();

            return View(ngos);
        }

        // GET: /Ngos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ngo = await _context.NGOs
                .Include(n => n.Programmes)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (ngo == null)
            {
                return NotFound();
            }

            return View(ngo);
        }
    }
}
