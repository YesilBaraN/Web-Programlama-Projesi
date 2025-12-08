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

        // 4. SİLME KONTROLÜ (GELİŞTİRİLMİŞ DETAYLI UYARI)
        public async Task<IActionResult> Delete(int id)
        {
            var antrenor = await _context.Antrenörler.FindAsync(id);
            if (antrenor == null) return NotFound();

            // Sadece var mı diye değil, KAÇ TANE var diye soruyoruz
            var randevuSayisi = await _context.Randevular.CountAsync(r => r.AntrenörId == id);

            if (randevuSayisi > 0)
            {
                // SweetAlert için detaylı HTML mesaj hazırlıyoruz
                string uyariDetayi = $"<strong>{antrenor.AdSoyad}</strong> isimli eğitmenin sistemde <strong>{randevuSayisi}</strong> adet kayıtlı randevusu bulunmaktadır.<br><br>" +
                                     "Eğer silme işlemine devam ederseniz:<br>" +
                                     "1. Bu randevuların hepsi <b>İPTAL</b> edilecek.<br>" +
                                     "2. İlgili üyelere otomatik <b>BİLDİRİM</b> gönderilecek.<br><br>" +
                                     "Bu işlemi yapmak istediğinize emin misiniz?";

                TempData["SilmeOnayiGerekli"] = true;
                TempData["SilinecekId"] = id;
                TempData["SilinecekTur"] = "Antrenor";
                TempData["UyariMesaji"] = uyariDetayi; // Artık sayı içeren detaylı mesaj gidiyor

                return RedirectToAction(nameof(Index));
            }

            // Randevu yoksa temiz sil
            _context.Antrenörler.Remove(antrenor);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Antrenör başarıyla silindi (Bağlı randevusu yoktu).";

            return RedirectToAction(nameof(Index));
        }

        // 5. ZORLA SİLME (FORCE DELETE) - Admin Onaylarsa Burası Çalışır
        [HttpPost]
        public async Task<IActionResult> ForceDelete(int id)
        {
            var antrenor = await _context.Antrenörler.Include(a => a.Randevular).FirstOrDefaultAsync(a => a.Id == id);
            if (antrenor == null) return NotFound();

            // 1. Randevusu olan üyelere bildirim oluştur
            foreach (var randevu in antrenor.Randevular)
            {
                var bildirim = new Bildirim
                {
                    UyeId = randevu.UyeId,
                    Mesaj = $"Sayın üyemiz, {randevu.RandevuTarihi:dd.MM.yyyy HH:mm} tarihindeki {antrenor.AdSoyad} ile olan randevunuz, eğitmenin kurumumuzdan ayrılması nedeniyle iptal edilmiştir.",
                    Tarih = DateTime.Now,
                    OkunduMu = false
                };
                _context.Bildirimler.Add(bildirim);
            }

            // 2. Randevuları Sil
            _context.Randevular.RemoveRange(antrenor.Randevular);

            // 3. Antrenörü Sil
            _context.Antrenörler.Remove(antrenor);

            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Antrenör ve bağlı tüm randevular silindi, üyelere bildirim gönderildi.";

            return RedirectToAction(nameof(Index));
        }
    }
}