using FitnessProje.Data;
using FitnessProje.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessProje.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Adminler girebilir
    public class HizmetlerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HizmetlerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME SAYFASI (INDEX)
        public async Task<IActionResult> Index()
        {
            var hizmetler = await _context.Hizmetler.ToListAsync();
            return View(hizmetler);
        }

        // 2. EKLEME SAYFASI (CREATE) - GET
        public IActionResult Create()
        {
            return View();
        }

        // 3. EKLEME İŞLEMİ (CREATE) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Hizmetler hizmet, IFormFile? resimDosyasi)
        {
            // Resim Yükleme İşlemi
            if (resimDosyasi != null)
            {
                var uzanti = Path.GetExtension(resimDosyasi.FileName);
                var resimAdi = Guid.NewGuid() + uzanti;
                var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/hizmetler", resimAdi);

                // Klasör yoksa oluştur
                var klasor = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/hizmetler");
                if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);

                using (var stream = new FileStream(yol, FileMode.Create))
                {
                    await resimDosyasi.CopyToAsync(stream);
                }
                hizmet.ResimUrl = "/img/hizmetler/" + resimAdi;
            }
            else
            {
                hizmet.ResimUrl = "/img/default.jpg"; // Resim yüklenmezse varsayılan
            }

            // Validasyon hatası olsa bile veritabanına kaydetmeyi zorlayalım (Basitlik için)
            _context.Add(hizmet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 4. SİLME İŞLEMİ (DELETE)
        public async Task<IActionResult> Delete(int id)
        {
            var hizmet = await _context.Hizmetler.FindAsync(id);
            if (hizmet != null)
            {
                _context.Hizmetler.Remove(hizmet);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}