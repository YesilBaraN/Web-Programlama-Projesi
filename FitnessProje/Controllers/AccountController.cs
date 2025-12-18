using FitnessProje.Models;
using FitnessProje.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessProje.Controllers
{
    /// <summary>
    /// Kullanıcı Kayıt, Giriş ve Çıkış işlemlerini yönetir.
    /// Identity kütüphanesini kullanır.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<UygulamaKullanıcı> _userManager;
        private readonly SignInManager<UygulamaKullanıcı> _signInManager;

        public AccountController(UserManager<UygulamaKullanıcı> userManager, SignInManager<UygulamaKullanıcı> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        #region Giriş İşlemleri (Login)

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Sifre, model.BeniHatirla, false);
                if (result.Succeeded)
                {
                    TempData["Basarili"] = "Başarıyla giriş yaptınız. Hoş geldiniz!";
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
        }

        #endregion

        #region Kayıt İşlemleri (Register)

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                ModelState.AddModelError("", "Bu e-posta adresi zaten kayıtlı.");
                return View(model);
            }

            var newUser = new UygulamaKullanıcı
            {
                UserName = model.Email,
                Email = model.Email,
                Ad = model.Ad,
                Soyad = model.Soyad,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, model.Sifre);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Uye"); // Varsayılan rol
                await _signInManager.SignInAsync(newUser, isPersistent: false);

                TempData["Basarili"] = "Kaydınız başarıyla oluşturuldu.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}