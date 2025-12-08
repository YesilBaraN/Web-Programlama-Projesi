using System.ComponentModel.DataAnnotations;

namespace FitnessProje.Models
{
    public class Bildirim
    {
        public int Id { get; set; }

        public string UyeId { get; set; } // Hangi üyeye gidecek?
        public string Mesaj { get; set; } // Mesaj ne?
        public bool OkunduMu { get; set; } = false; // Okudu mu?
        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}