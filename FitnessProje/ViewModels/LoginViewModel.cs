using System.ComponentModel.DataAnnotations;

namespace FitnessProje.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; }

        public bool BeniHatirla { get; set; }
    }
}