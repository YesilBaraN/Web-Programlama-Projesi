using FitnessProje.Models;
using FitnessProje.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessProje.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<UygulamaKullanıcı> _userManager;
        private readonly SignInManager<UygulamaKullanıcı> _signInManager;

        // Constructor'da Identity servislerini çağırıyoruz
        public AccountController(UserManager<UygulamaKullanıcı> userManager, SignInManager<UygulamaKullanıcı> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // --- GİRİŞ YAP (LOGIN) ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kullanıcıyı bul
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Şifreyi kontrol et ve giriş yap
                var result = await _signInManager.PasswordSignInAsync(user, model.Sifre, model.BeniHatirla, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
        }

        // --- KAYIT OL (REGISTER) ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // E-posta daha önce alınmış mı?
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                ModelState.AddModelError("", "Bu e-posta adresi zaten kayıtlı.");
                return View(model);
            }

            // Yeni kullanıcı oluştur
            var newUser = new UygulamaKullanıcı
            {
                UserName = model.Email,
                Email = model.Email,
                Ad = model.Ad,
                Soyad = model.Soyad,
                EmailConfirmed = true // Şimdilik onaya gerek duymadan aktif et
            };

            var result = await _userManager.CreateAsync(newUser, model.Sifre);

            if (result.Succeeded)
            {
                // Varsayılan olarak "Uye" rolünü ata
                await _userManager.AddToRoleAsync(newUser, "Uye");

                // Otomatik giriş yap ve ana sayfaya yönlendir
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            // Hata varsa ekrana bas
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // --- ÇIKIŞ YAP (LOGOUT) ---
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}