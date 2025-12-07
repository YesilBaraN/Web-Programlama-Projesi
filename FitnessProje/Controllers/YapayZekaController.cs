using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.GenAI;

namespace FitnessProje.Controllers
{
    [Authorize]
    public class YapayZekaController : Controller
    {
        private const string ApiKey = "";

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OneriAl(int yas, int kilo, int boy, string cinsiyet, string hedef)
        {
            string oneriMetni = "";
            string prompt = $"Ben {yas} yaşında, {kilo} kg, {boy} cm boyunda bir {cinsiyet} bireyim. " +
                            $"Hedefim: {hedef}. " +
                            $"Bana maddeler halinde kısa ve etkili bir beslenme ve egzersiz planı önerir misin? " +
                            $"Lütfen cevabı HTML formatı kullanmadan, sadece düz metin olarak, başlıkları büyük harfle yazarak ver.";

            try
            {
                using var client = new Google.GenAI.Client(apiKey: ApiKey);

                // DÜZELTME BURADA:
                // Kısa isim yerine tam sürüm adını kullanıyoruz: "gemini-1.5-flash-001"
                // Eğer bu da hata verirse lütfen buraya "gemini-pro" yaz.
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: prompt
                );

                if (response != null && response.Candidates.Count > 0)
                {
                    oneriMetni = response.Candidates[0].Content.Parts[0].Text.Trim();
                }
                else
                {
                    oneriMetni = "Yapay zeka boş bir cevap döndürdü.";
                }
            }
            catch (Exception ex)
            {
                oneriMetni = $"HATA: Bir sorun oluştu.\nDetay: {ex.Message}";
            }

            if (!string.IsNullOrEmpty(oneriMetni))
            {
                oneriMetni = oneriMetni.Replace("**", "").Replace("*", "-");
            }

            ViewBag.Sonuc = oneriMetni;
            ViewBag.Yas = yas;
            ViewBag.Kilo = kilo;
            ViewBag.Boy = boy;

            return View("Index");
        }
    }
}