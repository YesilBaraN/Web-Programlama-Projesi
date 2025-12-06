using FitnessProje.Data;
using FitnessProje.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessProje.Controllers
{
    [Route("api/[controller]")] // Tarayıcıdan erişim adresi: /api/fitness
    [ApiController]
    public class FitnessApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FitnessApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TÜM ANTRENÖRLERİ LİSTELEME
        // İstek: GET /api/fitness/tum-antrenorler
        [HttpGet("tum-antrenorler")]
        public async Task<IActionResult> GetAntrenorler()
        {
            var antrenorler = await _context.Antrenörler
                                            .Include(a => a.Hizmet) // Hizmet adını da gör
                                            .ToListAsync();
            return Ok(antrenorler);
        }

        // 2. BELİRLİ BİR HİZMETİ VERENLERİ FİLTRELEME (LINQ Örneği)
        // İstek: GET /api/fitness/hizmet-ara?hizmetAdi=Pilates
        [HttpGet("hizmet-ara")]
        public async Task<IActionResult> GetAntrenorByHizmet(string hizmetAdi)
        {
            if (string.IsNullOrEmpty(hizmetAdi)) return BadRequest("Hizmet adı boş olamaz.");

            var sonuclar = await _context.Antrenörler
                                         .Include(a => a.Hizmet)
                                         .Where(a => a.Hizmet.HizmetAdi.Contains(hizmetAdi)) // LINQ Filtreleme
                                         .ToListAsync();

            if (sonuclar.Count == 0) return NotFound("Bu hizmeti veren antrenör bulunamadı.");

            return Ok(sonuclar);
        }

        // 3. TARİHE GÖRE MÜSAİT ANTRENÖRLERİ GETİRME (PDF'teki Karmaşık Sorgu)
        // İstek: GET /api/fitness/musait-antrenorler?tarih=2025-12-01T14:00:00
        [HttpGet("musait-antrenorler")]
        public async Task<IActionResult> GetMusaitAntrenorler(DateTime tarih)
        {
            // O tarih ve saatte randevusu OLAN antrenörlerin ID'lerini bul
            var doluAntrenorIdleri = await _context.Randevular
                .Where(r => r.RandevuTarihi == tarih && r.Durum != "Iptal")
                .Select(r => r.AntrenörId)
                .ToListAsync();

            // Dolu OLMAYAN antrenörleri ve çalışma saatleri uyanları getir
            var musaitAntrenorler = await _context.Antrenörler
                .Include(a => a.Hizmet)
                .Where(a => !doluAntrenorIdleri.Contains(a.Id) && // O saatte dolu değilse
                            tarih.TimeOfDay >= a.BaslangicSaati && // Çalışma saati içindeyse
                            tarih.TimeOfDay <= a.BitisSaati)
                .ToListAsync();

            return Ok(musaitAntrenorler);
        }
    }
}