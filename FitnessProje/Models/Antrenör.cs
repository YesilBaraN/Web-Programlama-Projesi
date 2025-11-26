namespace FitnessProje.Models
{
    public class Antrenör
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; }
        public string UzmanlikAlani { get; set; }
        public string? FotoUrl { get; set; }

        // Çalışma Saatleri
        public TimeSpan BaslangicSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }

        // İlişki: Bir antrenör bir ana hizmet dalına bağlıdır (Örn: Sadece Yoga hocası)
        public int HizmetlerId { get; set; }
        public Hizmetler Hizmet { get; set; }

        // Bir antrenörün birden fazla randevusu olabilir
        public ICollection<RandevuSistemi> Randevular { get; set; }
    }
}