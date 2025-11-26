namespace FitnessProje.Models
{
    public class Hizmetler
    {
        public int Id { get; set; }
        public string HizmetAdi { get; set; } // Örn: Pilates
        public string Aciklama { get; set; }
        public int SureDakika { get; set; } // Örn: 60 dk
        public decimal Ucret { get; set; }
        public string? ResimUrl { get; set; } // ? işareti boş geçilebilir demek

        // Bir hizmeti birden fazla antrenör verebilir
        public ICollection<Antrenör> Antrenörler { get; set; }
    }
}