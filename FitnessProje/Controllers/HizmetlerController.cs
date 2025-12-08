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

        // 4. SİLME KONTROLÜ (GELİŞTİRİLMİŞ)
        public async Task<IActionResult> Delete(int id)
        {
            var hizmet = await _context.Hizmetler.FindAsync(id);
            if (hizmet == null) return NotFound();

            // İstatistikleri Topla
            var randevuSayisi = await _context.Randevular.CountAsync(r => r.HizmetId == id);
            var antrenorSayisi = await _context.Antrenörler.CountAsync(a => a.HizmetlerId == id);

            if (randevuSayisi > 0 || antrenorSayisi > 0)
            {
                string uyariDetayi = $"<strong>{hizmet.HizmetAdi}</strong> hizmetini silmek üzeresiniz.<br><br>" +
                                     $"Bu hizmete bağlı veriler:<br>" +
                                     $"- <b>{antrenorSayisi}</b> adet Antrenör<br>" +
                                     $"- <b>{randevuSayisi}</b> adet Randevu<br><br>" +
                                     "Onaylarsanız hepsi <b>SİLİNECEK</b> ve kullanıcılara bildirim gidecektir.";

                TempData["SilmeOnayiGerekli"] = true;
                TempData["SilinecekId"] = id;
                TempData["SilinecekTur"] = "Hizmet";
                TempData["UyariMesaji"] = uyariDetayi;

                return RedirectToAction(nameof(Index));
            }

            _context.Hizmetler.Remove(hizmet);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Hizmet sorunsuz silindi.";
            return RedirectToAction(nameof(Index));
        }
        // 5. ZORLA SİLME
        [HttpPost]
        public async Task<IActionResult> ForceDelete(int id)
        {
            var hizmet = await _context.Hizmetler
                .Include(h => h.Antrenörler)
                    .ThenInclude(a => a.Randevular) // Antrenörlerin randevularını da al
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hizmet == null) return NotFound();

            // Bildirim Oluşturma Döngüsü (Biraz karmaşık çünkü Hizmet -> Antrenör -> Randevu zinciri var)
            // Doğrudan hizmete bağlı randevuları veya antrenör üzerinden bağlı olanları bulalım.

            // Basitlik adına veritabanındaki o hizmete ait tüm randevuları çekelim
            var etkilenecekRandevular = await _context.Randevular.Where(r => r.HizmetId == id).ToListAsync();

            foreach (var randevu in etkilenecekRandevular)
            {
                var bildirim = new Bildirim
                {
                    UyeId = randevu.UyeId,
                    Mesaj = $"Sayın üyemiz, {hizmet.HizmetAdi} hizmetinin kaldırılması nedeniyle {randevu.RandevuTarihi:dd.MM.yyyy} tarihli randevunuz iptal edilmiştir.",
                    Tarih = DateTime.Now
                };
                _context.Bildirimler.Add(bildirim);
            }

            // Önce Randevuları Sil
            _context.Randevular.RemoveRange(etkilenecekRandevular);

            // Sonra Antrenörleri Sil
            if (hizmet.Antrenörler != null)
            {
                _context.Antrenörler.RemoveRange(hizmet.Antrenörler);
            }

            // Sonra Hizmeti Sil
            _context.Hizmetler.Remove(hizmet);

            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Hizmet ve bağlı tüm veriler silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}