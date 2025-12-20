using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FitnessProje.Controllers
{
    [Authorize]
    public class YapayZekaController : Controller
    {
        // SENİN GOOGLE API KEY'İN (Sadece Metin İçin Kullanacağız)
        private const string ApiKey = "AIzaSyAacZhJ7eb76NSf15zIbr5gHs5wFBDm5ms";
        private const string TextApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> AnalizYap(int yas, int kilo, int boy, string cinsiyet, string hedef)
        {
            // 1. METİN PLANI (GOOGLE GEMINI)
            string textPrompt = $"Ben {yas} yaşında, {kilo} kg, {boy} cm boyunda bir {cinsiyet} bireyim. " +
                                $"Hedefim: {hedef}. " +
                                $"Bana maddeler halinde kısa, motive edici ve etkili bir beslenme ve egzersiz planı hazırla. " +
                                $"Cevabı düz metin olarak ver.";

            // 2. RESİM OLUŞTURMA (DOĞAL VE GERÇEKÇİ AYARLAR)
            string genderTerm = cinsiyet == "Erkek" ? "man" : "woman";
            string bodyType = "";

            // ABARTIYI ENGELLEYEN YENİ PROMPT'LAR:
            if (hedef.Contains("Kas"))
                // "Bodybuilder" yerine "Athletic" ve "Toned" (Sıkı/Kaslı) kullandık.
                bodyType = "fit athletic body, toned muscles, strong physique, wearing gym t-shirt, healthy look";
            else if (hedef.Contains("Kilo"))
                // "Very slim" yerine "Slim and fit" (İnce ve fit) kullandık.
                bodyType = "slim and fit body, flat stomach, healthy weight, wearing jogging outfit, energetic";
            else
                // Genel sağlık için
                bodyType = "average healthy body, happy expression, active lifestyle, casual sportswear";

            // GENEL FOTOĞRAF AYARLARI:
            // "Cinematic neon" yerine "Natural lighting" (Doğal ışık)
            // "8k masterpiece" yerine "Photorealistic, photo taken with iphone" (Gerçekçi, doğal çekim)
            string imagePrompt = $"medium shot photo of a {genderTerm}, {bodyType}, standing in a bright modern gym with natural window light, looking at camera, smiling, photorealistic, realistic skin texture, 4k, authentic style, no filters";

            string encodedPrompt = Uri.EscapeDataString(imagePrompt);

            // Pollinations URL
            string resimUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width=1024&height=1024&nologo=true&seed={new Random().Next(1, 9999)}";

            // --- 3. GOOGLE'DAN METNİ İSTE ---
            string planSonuc = await CallGeminiText(textPrompt);

            // --- 4. SONUÇLARI GÖNDER ---
            ViewBag.Sonuc = planSonuc;
            ViewBag.UretilenResim = resimUrl;

            ViewBag.Yas = yas; ViewBag.Kilo = kilo; ViewBag.Boy = boy;

            return View("Index");
        }
        // YARDIMCI METOT: SADECE METİN İÇİN GOOGLE'A GİDER
        private async Task<string> CallGeminiText(string prompt)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-goog-api-key", ApiKey);
                var json = new StringContent(JsonSerializer.Serialize(new { contents = new[] { new { parts = new[] { new { text = prompt } } } } }), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(TextApiUrl, json);

                if (response.IsSuccessStatusCode)
                {
                    var respStr = await response.Content.ReadAsStringAsync();
                    var node = JsonNode.Parse(respStr);
                    string text = node?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";
                    return text.Replace("**", "").Replace("*", "-");
                }
                return "Yapay zeka planı hazırlarken bir yoğunluk yaşandı. Lütfen tekrar deneyin.";
            }
            catch
            {
                return "Bağlantı hatası.";
            }
        }
    }
}