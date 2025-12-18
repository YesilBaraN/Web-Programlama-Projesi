using FitnessProje.Data;
using FitnessProje.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessProje.Controllers
{
    /// <summary>
    /// Randevu işlemlerini (Alma, Listeleme, Silme, Onaylama) yöneten kontrolcü.
    /// </summary>
    [Authorize]
    public class RandevuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<UygulamaKullanıcı> _userManager;

        public RandevuController(ApplicationDbContext context, UserManager<UygulamaKullanıcı> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region Sayfa Yönlendirmeleri

        // GET: /Randevu/Basla
        // Kullanıcıyı karşılayan ve hizmet seçtiren ilk ekran.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Basla()
        {
            // Üyelik Kontrolü
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Hata"] = "Randevu almak için üye olmanız gerekmektedir. Lütfen önce kayıt olun.";
                return RedirectToAction("Register", "Account");
            }

            var hizmetler = await _context.Hizmetler.ToListAsync();
            return View(hizmetler);
        }

        // GET: /Randevu/Al?hizmetId=1
        // Tarih ve Eğitmen seçim ekranı.
        [HttpGet]
        public async Task<IActionResult> Al(int hizmetId)
        {
            var hizmet = await _context.Hizmetler.FindAsync(hizmetId);
            if (hizmet == null) return NotFound();

            // Sadece bu hizmeti verebilen antrenörleri getir
            var uygunAntrenorler = _context.Antrenörler
                                           .Where(a => a.HizmetlerId == hizmetId)
                                           .ToList();

            ViewBag.HizmetAdi = hizmet.HizmetAdi;
            ViewBag.Sure = hizmet.SureDakika;
            ViewBag.Ucret = hizmet.Ucret;
            ViewBag.HizmetId = hizmetId;
            ViewBag.Antrenorler = new SelectList(uygunAntrenorler, "Id", "AdSoyad");

            return View();
        }

        #endregion

        #region Randevu İş Mantığı (Business Logic)

        // POST: /Randevu/Al
        // Randevuyu kaydeden ve çakışma (conflict) kontrolü yapan ana metot.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Al(int hizmetId, int antrenorId, DateTime tarih)
        {
            // 1. Geçmiş Tarih Kontrolü
            if (tarih < DateTime.Now)
            {
                TempData["Hata"] = "Geçmiş bir tarihe randevu alamazsınız.";
                return RedirectToAction("Al", new { hizmetId = hizmetId });
            }

            var hizmet = await _context.Hizmetler.FindAsync(hizmetId);
            var antrenor = await _context.Antrenörler.FindAsync(antrenorId);

            if (hizmet == null || antrenor == null) return NotFound();

            // 2. Çalışma Saati Kontrolü
            if (tarih.TimeOfDay < antrenor.BaslangicSaati || tarih.TimeOfDay > antrenor.BitisSaati)
            {
                TempData["Hata"] = $"Antrenör sadece {antrenor.BaslangicSaati} - {antrenor.BitisSaati} saatleri arasında çalışmaktadır.";
                return RedirectToAction("Al", new { hizmetId = hizmetId });
            }

            // 3. ÇAKIŞMA KONTROLÜ (Conflict Check)
            var randevuBitis = tarih.AddMinutes(hizmet.SureDakika);

            var cakismaVarMi = await _context.Randevular
                .AnyAsync(r => r.AntrenörId == antrenorId &&
                               r.Durum != "Iptal" &&
                               (
                                   (r.RandevuTarihi < randevuBitis && r.RandevuTarihi >= tarih) ||
                                   (r.RandevuTarihi.AddMinutes(r.Hizmet.SureDakika) > tarih && r.RandevuTarihi <= tarih)
                               ));

            if (cakismaVarMi)
            {
                TempData["Hata"] = "Seçtiğiniz saatte antrenör dolu. Lütfen başka bir saat seçiniz.";
                return RedirectToAction("Al", new { hizmetId = hizmetId });
            }

            // 4. Kayıt
            var user = await _userManager.GetUserAsync(User);
            var yeniRandevu = new RandevuSistemi
            {
                HizmetId = hizmetId,
                AntrenörId = antrenorId,
                UyeId = user.Id,
                RandevuTarihi = tarih,
                Durum = "OnayBekliyor",
                OlusturulmaTarihi = DateTime.Now
            };

            _context.Randevular.Add(yeniRandevu);
            await _context.SaveChangesAsync();

            TempData["Basarili"] = "Randevunuz oluşturuldu! Onay bekleniyor.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Listeleme İşlemleri

        // Kullanıcının kendi randevularını gördüğü sayfa
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var randevular = await _context.Randevular
                                           .Include(r => r.Hizmet)
                                           .Include(r => r.Antrenör)
                                           .Where(r => r.UyeId == user.Id)
                                           .OrderByDescending(r => r.RandevuTarihi)
                                           .ToListAsync();
            return View(randevular);
        }

        #endregion

        #region Admin İşlemleri

        // Adminin tüm randevuları gördüğü panel
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Yonetim()
        {
            var randevular = await _context.Randevular
                                           .Include(r => r.Hizmet)
                                           .Include(r => r.Antrenör)
                                           .Include(r => r.Uye)
                                           .OrderByDescending(r => r.OlusturulmaTarihi)
                                           .ToListAsync();
            return View(randevular);
        }

        // Admin onay/ret işlemi
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DurumDegistir(int id, string durum)
        {
            var randevu = await _context.Randevular.FindAsync(id);
            if (randevu != null)
            {
                randevu.Durum = durum;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Yonetim));
        }

        // Admin kalıcı silme işlemi (Bildirim gönderir)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var randevu = await _context.Randevular
                                        .Include(r => r.Hizmet)
                                        .Include(r => r.Uye)
                                        .FirstOrDefaultAsync(r => r.Id == id);

            if (randevu == null)
            {
                TempData["Hata"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Yonetim));
            }

            // Silinen randevu için üyeye bildirim oluştur
            if (randevu.Uye != null)
            {
                var bildirim = new Bildirim
                {
                    UyeId = randevu.UyeId,
                    Mesaj = $"Sayın {randevu.Uye.Ad}, {randevu.RandevuTarihi:dd.MM.yyyy HH:mm} tarihindeki {randevu.Hizmet?.HizmetAdi} randevunuz silinmiştir.",
                    Tarih = DateTime.Now
                };
                _context.Bildirimler.Add(bildirim);
            }

            _context.Randevular.Remove(randevu);
            await _context.SaveChangesAsync();

            TempData["Basarili"] = "Randevu silindi ve üyeye bildirim gönderildi.";
            return RedirectToAction(nameof(Yonetim));
        }

        #endregion
    }
}