using FitnessProje.Data;
using FitnessProje.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessProje.Controllers
{
    [Authorize] // Sadece giriş yapmış üyeler randevu alabilir
    public class RandevuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<UygulamaKullanıcı> _userManager;

        public RandevuController(ApplicationDbContext context, UserManager<UygulamaKullanıcı> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. RANDEVU ALMA SAYFASI (Formu Göster)
        // Ana sayfadan "Randevu Al" butonuna basınca buraya gelecek.
        [HttpGet]
        public async Task<IActionResult> Al(int hizmetId)
        {
            // Seçilen hizmeti bul
            var hizmet = await _context.Hizmetler.FindAsync(hizmetId);
            if (hizmet == null) return NotFound();

            // Sadece bu hizmeti verebilen antrenörleri getir
            var uygunAntrenorler = _context.Antrenörler
                                           .Where(a => a.HizmetlerId == hizmetId)
                                           .ToList();

            // View'a veri taşıma
            ViewBag.HizmetAdi = hizmet.HizmetAdi;
            ViewBag.Sure = hizmet.SureDakika;
            ViewBag.Ucret = hizmet.Ucret;
            ViewBag.HizmetId = hizmetId;

            // Antrenörleri Dropdown için hazırla
            ViewBag.Antrenorler = new SelectList(uygunAntrenorler, "Id", "AdSoyad");

            return View();
        }

        // 2. RANDEVU KAYDETME İŞLEMİ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Al(int hizmetId, int antrenorId, DateTime tarih)
        {
            // 1. Temel Kontroller
            if (tarih < DateTime.Now)
            {
                TempData["Hata"] = "Geçmiş bir tarihe randevu alamazsınız.";
                return RedirectToAction("Al", new { hizmetId = hizmetId });
            }

            // Hizmet ve Antrenör bilgilerini çek
            var hizmet = await _context.Hizmetler.FindAsync(hizmetId);
            var antrenor = await _context.Antrenörler.FindAsync(antrenorId);

            if (hizmet == null || antrenor == null) return NotFound();

            // 2. Çalışma Saati Kontrolü (PDF Madde 2)
            // Sadece saat kısmını karşılaştırıyoruz (TimeOfDay)
            if (tarih.TimeOfDay < antrenor.BaslangicSaati || tarih.TimeOfDay > antrenor.BitisSaati)
            {
                TempData["Hata"] = $"Antrenör sadece {antrenor.BaslangicSaati} - {antrenor.BitisSaati} saatleri arasında çalışmaktadır.";
                return RedirectToAction("Al", new { hizmetId = hizmetId });
            }

            // 3. ÇAKIŞMA KONTROLÜ (PDF Madde 3 - En Önemli Kısım)
            // Yeni randevunun bitiş saati
            var randevuBitis = tarih.AddMinutes(hizmet.SureDakika);

            // Veritabanında, seçilen antrenörün o tarih aralığında çakışan randevusu var mı?
            var cakismaVarMi = await _context.Randevular
                .AnyAsync(r => r.AntrenörId == antrenorId &&
                               r.Durum != "Iptal" && // İptal edilenler çakışma yaratmaz
                               (
                                   // Mevcut randevunun başlangıcı, yeni randevunun aralığına denk geliyorsa
                                   (r.RandevuTarihi < randevuBitis && r.RandevuTarihi >= tarih)
                                   ||
                                   // Mevcut randevunun bitişi, yeni randevunun aralığına denk geliyorsa
                                   (r.RandevuTarihi.AddMinutes(r.Hizmet.SureDakika) > tarih && r.RandevuTarihi <= tarih)
                               ));

            if (cakismaVarMi)
            {
                TempData["Hata"] = "Seçtiğiniz saatte antrenör dolu. Lütfen başka bir saat seçiniz.";
                return RedirectToAction("Al", new { hizmetId = hizmetId });
            }

            // 4. KAYIT İŞLEMİ
            var user = await _userManager.GetUserAsync(User); // Giriş yapan kullanıcı

            var yeniRandevu = new RandevuSistemi
            {
                HizmetId = hizmetId,
                AntrenörId = antrenorId,
                UyeId = user.Id,
                RandevuTarihi = tarih,
                Durum = "OnayBekliyor", // PDF: Onay mekanizması olmalı
                OlusturulmaTarihi = DateTime.Now
            };

            _context.Randevular.Add(yeniRandevu);
            await _context.SaveChangesAsync();

            // Başarılı sayfasına veya randevularım sayfasına yönlendir
            return RedirectToAction("Index", "Home"); // Şimdilik ana sayfaya atalım, sonra "Randevularım" yapacağız
        }

        // --- 3. ÜYE İÇİN: RANDEVULARIM SAYFASI ---
        public async Task<IActionResult> Index()
        {
            // Giriş yapan kullanıcıyı bul
            var user = await _userManager.GetUserAsync(User);

            // Sadece o kullanıcının randevularını getir
            var randevular = await _context.Randevular
                                           .Include(r => r.Hizmet)
                                           .Include(r => r.Antrenör)
                                           .Where(r => r.UyeId == user.Id)
                                           .OrderByDescending(r => r.RandevuTarihi) // En yeni en üstte
                                           .ToListAsync();

            return View(randevular);
        }

        // --- 4. ADMIN İÇİN: TÜM RANDEVULARI YÖNETME ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Yonetim()
        {
            // Tüm randevuları getir (Üye bilgisiyle beraber)
            var randevular = await _context.Randevular
                                           .Include(r => r.Hizmet)
                                           .Include(r => r.Antrenör)
                                           .Include(r => r.Uye) // Kim almış?
                                           .OrderByDescending(r => r.OlusturulmaTarihi)
                                           .ToListAsync();
            return View(randevular);
        }

        // --- 5. ADMIN İÇİN: ONAY / İPTAL İŞLEMİ ---
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DurumDegistir(int id, string durum)
        {
            var randevu = await _context.Randevular.FindAsync(id);
            if (randevu != null)
            {
                randevu.Durum = durum; // "Onaylandi" veya "Iptal" gelecek
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Yonetim));
        }

        // --- 6. ADMIN İÇİN: RANDEVU SİLME (DELETE) ---
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            // 1. Randevuyu detaylarıyla bul (Bildirim için gerekli)
            var randevu = await _context.Randevular
                                        .Include(r => r.Hizmet)
                                        .Include(r => r.Antrenör)
                                        .Include(r => r.Uye)
                                        .FirstOrDefaultAsync(r => r.Id == id);

            if (randevu == null)
            {
                TempData["Hata"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Yonetim));
            }

            // 2. Üyeye Bildirim Gönder (Nezaketen)
            // Eğer üye hala sistemdeyse ona haber verelim.
            if (randevu.Uye != null)
            {
                var bildirim = new Bildirim
                {
                    UyeId = randevu.UyeId,
                    Mesaj = $"Sayın {randevu.Uye.Ad} {randevu.Uye.Soyad}, <br>" +
                            $"{randevu.RandevuTarihi:dd.MM.yyyy HH:mm} tarihindeki <strong>{randevu.Hizmet?.HizmetAdi}</strong> randevunuz yönetim tarafından silinmiştir.",
                    Tarih = DateTime.Now,
                    OkunduMu = false
                };
                _context.Bildirimler.Add(bildirim);
            }

            // 3. Randevuyu Sil
            _context.Randevular.Remove(randevu);
            await _context.SaveChangesAsync();

            TempData["Basarili"] = "Randevu kalıcı olarak silindi ve üyeye bildirim gönderildi.";
            return RedirectToAction(nameof(Yonetim));
        }


        // --- 7. RANDEVU BAŞLANGIÇ (Hizmet Seçimi veya Login Kontrolü) ---
        [HttpGet]
        [AllowAnonymous] // Herkes tıklayabilir, içeride kontrol edeceğiz
        public async Task<IActionResult> Basla()
        {
            // 1. KONTROL: Kullanıcı giriş yapmış mı?
            if (!User.Identity.IsAuthenticated)
            {
                // Giriş yapmamışsa uyarı ver ve Kayıt Ol sayfasına at
                TempData["Hata"] = "Randevu almak için üye olmanız gerekmektedir. Lütfen önce kayıt olun.";
                return RedirectToAction("Register", "Account");
            }

            // 2. KONTROL: Eğer giriş yapmışsa Hizmetleri getir
            var hizmetler = await _context.Hizmetler.ToListAsync();
            return View(hizmetler);
        }




    }
}