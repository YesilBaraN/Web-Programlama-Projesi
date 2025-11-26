namespace FitnessProje.Models
{
    public class RandevuSistemi
    {
        public int Id { get; set; }
        public DateTime RandevuTarihi { get; set; }
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        // Durum: OnayBekliyor, Onaylandi, Iptal
        public string Durum { get; set; } = "OnayBekliyor";

        // İlişkiler: Kim aldı?
        public string UyeId { get; set; }
        public UygulamaKullanıcı Uye { get; set; }

        // İlişkiler: Hangi hoca?
        public int AntrenörId { get; set; }
        public Antrenör Antrenör { get; set; }

        // İlişkiler: Hangi ders?
        public int HizmetId { get; set; }
        public Hizmetler Hizmet { get; set; }
    }
}