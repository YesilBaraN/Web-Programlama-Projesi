using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.GenAI;

namespace FitnessProje.Controllers
{
    /// <summary>
    /// Google Gemini AI kullanarak kullanıcılara beslenme ve egzersiz tavsiyesi veren kontrolcü.
    /// </summary>
    [Authorize]
    public class YapayZekaController : Controller
    {
        // NOT: Güvenlik için API Key appsettings.json'dan da çekilebilir.
        // Şimdilik öğrenci projesi olduğu için burada tutuyoruz.
        private const string ApiKey = "";

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Kullanıcıdan alınan verileri Google Gemini API'ye gönderir ve sonucu döner.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OneriAl(int yas, int kilo, int boy, string cinsiyet, string hedef)
        {
            string oneriMetni = "";

            // AI'ya gidecek "Prompt" (İstek Mesajı) hazırlanıyor
            string prompt = $"Ben {yas} yaşında, {kilo} kg, {boy} cm boyunda bir {cinsiyet} bireyim. " +
                            $"Hedefim: {hedef}. " +
                            $"Bana maddeler halinde kısa ve etkili bir beslenme ve egzersiz planı önerir misin? " +
                            $"Lütfen cevabı HTML formatı kullanmadan, sadece düz metin olarak, başlıkları büyük harfle yazarak ver.";

            try
            {
                // Google GenAI İstemcisi oluşturuluyor
                using var client = new Google.GenAI.Client(apiKey: ApiKey);

                // İstek gönderiliyor (Model: Gemini 1.5 Flash - Hızlı ve Ücretsiz)
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-1.5-flash-001",
                    contents: prompt
                );

                if (response != null && response.Candidates.Count > 0)
                {
                    oneriMetni = response.Candidates[0].Content.Parts[0].Text.Trim();
                }
                else
                {
                    oneriMetni = "Yapay zeka şu an cevap veremiyor. Lütfen daha sonra deneyin.";
                }
            }
            catch (Exception ex)
            {
                oneriMetni = $"Bağlantı Hatası: {ex.Message}";
            }

            // Gelen metindeki markdown karakterlerini temizle
            if (!string.IsNullOrEmpty(oneriMetni))
            {
                oneriMetni = oneriMetni.Replace("**", "").Replace("*", "-");
            }

            // Sonuçları View'a taşı
            ViewBag.Sonuc = oneriMetni;
            ViewBag.Yas = yas;
            ViewBag.Kilo = kilo;
            ViewBag.Boy = boy;

            return View("Index");
        }
    }
}