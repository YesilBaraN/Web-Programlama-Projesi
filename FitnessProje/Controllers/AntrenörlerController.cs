using FitnessProje.Data;
using FitnessProje.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Açılır liste için gerekli
using Microsoft.EntityFrameworkCore;

namespace FitnessProje.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AntrenörlerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AntrenörlerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME (INDEX)
        public async Task<IActionResult> Index()
        {
            // Antrenörleri çekerken bağlı olduğu Hizmet bilgisini de getir (Include)
            var antrenorler = await _context.Antrenörler
                                            .Include(a => a.Hizmet)
                                            .ToListAsync();
            return View(antrenorler);
        }

        // 2. EKLEME SAYFASI (CREATE) - GET
        public IActionResult Create()
        {
            // Veritabanındaki Hizmetleri çekip View'a "SelectList" olarak gönderiyoruz
            // Bu sayede HTML'de <select> içinde gösterebileceğiz.
            ViewData["HizmetlerId"] = new SelectList(_context.Hizmetler, "Id", "HizmetAdi");
            return View();
        }

        // 3. EKLEME İŞLEMİ (CREATE) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Antrenör antrenör, IFormFile? resimDosyasi)
        {
            // Resim Yükleme (Hizmetler'dekiyle aynı mantık)
            if (resimDosyasi != null)
            {
                var uzanti = Path.GetExtension(resimDosyasi.FileName);
                var yeniIsim = Guid.NewGuid() + uzanti;
                var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/antrenorler", yeniIsim);

                var klasor = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/antrenorler");
                if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);

                using (var stream = new FileStream(yol, FileMode.Create))
                {
                    await resimDosyasi.CopyToAsync(stream);
                }
                antrenör.FotoUrl = "/img/antrenorler/" + yeniIsim;
            }
            else
            {
                antrenör.FotoUrl = "/img/default-user.png";
            }

            // Kaydet
            _context.Add(antrenör);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 4. SİLME İŞLEMİ
        public async Task<IActionResult> Delete(int id)
        {
            var antrenor = await _context.Antrenörler.FindAsync(id);
            if (antrenor != null)
            {
                _context.Antrenörler.Remove(antrenor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}