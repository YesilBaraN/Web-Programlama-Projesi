using FitnessProje.Data;
using FitnessProje.Models;
using FitnessProje.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FitnessProje.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Veritabanýndan verileri çek
            var hizmetler = await _context.Hizmetler.ToListAsync();

            // Antrenörleri çekerken hizmet bilgisini de dahil et (Include)
            var antrenorler = await _context.Antrenörler.Include(a => a.Hizmet).ToListAsync();

            // ViewModel paketini hazýrla
            var model = new HomeViewModel
            {
                Hizmetler = hizmetler,
                Antrenörler = antrenorler
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}