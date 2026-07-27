using Microsoft.EntityFrameworkCore;
using Portfolio.Entities.Metrik;

namespace Portfolio.Repositories.Sql;

/// <summary>Admin metrik ekranının tek veri kaynağı.</summary>
public interface IMetrikOkuyucu
{
    Task<MetrikOzeti> OzetAsync(DateOnly baslangic, DateOnly bitis, CancellationToken ct = default);
}

/// <summary>Bir dönemin metrik özeti. Tüm sayılar o döneme aittir.</summary>
public sealed class MetrikOzeti
{
    public DateOnly Baslangic { get; init; }
    public DateOnly Bitis { get; init; }

    public int ToplamZiyaret { get; set; }

    /// <summary>
    /// ⚠️ Tekil ziyaretçi GÜNLERİN TOPLAMIDIR, dönemin gerçek tekili değil.
    /// Kimlik her gece değiştiği için (çerezsiz tasarım) dönem geneli tekil
    /// sayısı hesaplanamaz. Bu bilinçli bir bedel; ekranda da böyle yazıyor.
    /// </summary>
    public int TekilZiyaretciGunToplami { get; set; }

    /// <summary>Gün → ziyaret sayısı (grafik için, boş günler 0 ile dolu).</summary>
    public List<(DateOnly Gun, int Ziyaret, int Tekil)> Gunluk { get; set; } = new();

    public Dictionary<string, int> Kaynak { get; set; } = new();
    public Dictionary<string, int> Dil { get; set; } = new();
    public Dictionary<string, int> Cihaz { get; set; } = new();

    /// <summary>Bölüm → o bölümü gören ziyaretçi (gün toplamı).</summary>
    public Dictionary<string, int> Bolum { get; set; } = new();

    /// <summary>Bölüm → o bölümde geçirilen toplam saniye.</summary>
    public Dictionary<string, int> BolumSure { get; set; } = new();

    public List<(string Ad, int Adet)> Demolar { get; set; } = new();
    public List<(string Hedef, int Adet)> Baglantilar { get; set; } = new();
    public List<(string Host, int Adet)> KaynakSiteler { get; set; } = new();

    public bool VeriVarMi => ToplamZiyaret > 0;
}

/// <summary>
/// Okuma HER ZAMAN iki kaynağı birleştirir: kalıcı <c>GunlukOzet</c> +
/// henüz özetlenmemiş günlerin (pratikte bugünün) HAM verisi.
/// <para>
/// Neden: özetleme gece çalışıyor. Yalnız özete baksaydık admin "bugün" seçtiğinde
/// her zaman boş görürdü — en çok merak edilen gün.
/// </para>
/// </summary>
public sealed class MetrikOkuyucu : IMetrikOkuyucu
{
    private readonly IDbContextFactory<PortfolioDbContext> _fabrika;

    public MetrikOkuyucu(IDbContextFactory<PortfolioDbContext> fabrika) => _fabrika = fabrika;

    public async Task<MetrikOzeti> OzetAsync(DateOnly baslangic, DateOnly bitis, CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);

        var ozet = new MetrikOzeti { Baslangic = baslangic, Bitis = bitis };

        // ⚠️ YEREL olmak zorundalar. Örnek alanı yapılsaydı bu servis singleton
        // olduğu için sayılar çağrılar arasında birikir ve eşzamanlı iki istekte
        // birbirine karışırdı.
        var demolar = new Dictionary<string, int>();
        var baglantilar = new Dictionary<string, int>();
        var kaynakSiteler = new Dictionary<string, int>();

        // 1) Özetlenmiş günler
        var ozetSatirlar = await db.GunlukOzetler
            .Where(x => x.Gun >= baslangic && x.Gun <= bitis)
            .AsNoTracking().ToListAsync(ct);

        var ozetliGunler = ozetSatirlar.Select(x => x.Gun).ToHashSet();

        // 2) Henüz özetlenmemiş günlerin ham verisi
        var hamZiyaret = await db.Ziyaretler
            .Where(x => x.Gun >= baslangic && x.Gun <= bitis && !ozetliGunler.Contains(x.Gun))
            .AsNoTracking().ToListAsync(ct);

        var hamOlay = await db.Olaylar
            .Where(x => x.Gun >= baslangic && x.Gun <= bitis && !ozetliGunler.Contains(x.Gun))
            .AsNoTracking().ToListAsync(ct);

        // --- Birleştirme ---------------------------------------------------

        var gunlukZiyaret = new Dictionary<DateOnly, int>();
        var gunlukTekil = new Dictionary<DateOnly, int>();

        void Topla(Dictionary<string, int> hedef, string anahtar, int deger)
        {
            if (string.IsNullOrEmpty(anahtar)) return;
            hedef[anahtar] = hedef.GetValueOrDefault(anahtar) + deger;
        }

        foreach (var s in ozetSatirlar)
        {
            switch (s.Tip)
            {
                case OzetTipi.Toplam: gunlukZiyaret[s.Gun] = gunlukZiyaret.GetValueOrDefault(s.Gun) + s.Deger; break;
                case OzetTipi.Tekil: gunlukTekil[s.Gun] = gunlukTekil.GetValueOrDefault(s.Gun) + s.Deger; break;
                case OzetTipi.Kaynak: Topla(ozet.Kaynak, s.Anahtar, s.Deger); break;
                case OzetTipi.Dil: Topla(ozet.Dil, s.Anahtar, s.Deger); break;
                case OzetTipi.Cihaz: Topla(ozet.Cihaz, s.Anahtar, s.Deger); break;
                case OzetTipi.Bolum: Topla(ozet.Bolum, s.Anahtar, s.Deger); break;
                case OzetTipi.Sure: Topla(ozet.BolumSure, s.Anahtar, s.Deger); break;
                case OzetTipi.Demo: Topla(demolar, s.Anahtar, s.Deger); break;
                case OzetTipi.Baglanti: Topla(baglantilar, s.Anahtar, s.Deger); break;
                case OzetTipi.KaynakSite: Topla(kaynakSiteler, s.Anahtar, s.Deger); break;
            }
        }

        foreach (var g in hamZiyaret.GroupBy(x => x.Gun))
        {
            gunlukZiyaret[g.Key] = gunlukZiyaret.GetValueOrDefault(g.Key) + g.Count();
            gunlukTekil[g.Key] = gunlukTekil.GetValueOrDefault(g.Key) + g.Select(x => x.ZiyaretciHash).Distinct().Count();
        }

        foreach (var g in hamZiyaret.GroupBy(x => x.KaynakTipi)) Topla(ozet.Kaynak, g.Key, g.Count());
        foreach (var g in hamZiyaret.GroupBy(x => x.Dil)) Topla(ozet.Dil, g.Key, g.Count());
        foreach (var g in hamZiyaret.GroupBy(x => x.Cihaz)) Topla(ozet.Cihaz, g.Key, g.Count());
        foreach (var g in hamZiyaret.Where(x => x.KaynakHost.Length > 0).GroupBy(x => x.KaynakHost))
            Topla(kaynakSiteler, g.Key, g.Count());

        foreach (var g in hamOlay.Where(x => x.Tip == OlayTipi.Bolum).GroupBy(x => x.Deger))
            Topla(ozet.Bolum, g.Key, g.Select(x => x.ZiyaretciHash).Distinct().Count());
        foreach (var g in hamOlay.Where(x => x.Tip == OlayTipi.Sure).GroupBy(x => x.Deger))
            Topla(ozet.BolumSure, g.Key, g.Sum(x => x.SaniyeSure));
        foreach (var g in hamOlay.Where(x => x.Tip == OlayTipi.Demo).GroupBy(x => x.Deger))
            Topla(demolar, g.Key, g.Count());
        foreach (var g in hamOlay.Where(x => x.Tip == OlayTipi.Baglanti).GroupBy(x => x.Deger))
            Topla(baglantilar, g.Key, g.Count());

        // --- Çıktı ----------------------------------------------------------

        // Grafik için BOŞ GÜNLER DE olmalı: yalnız veri olan günleri çizersek
        // ziyaretsiz günler grafikten düşer ve trend olduğundan iyi görünür.
        for (var g = baslangic; g <= bitis; g = g.AddDays(1))
            ozet.Gunluk.Add((g, gunlukZiyaret.GetValueOrDefault(g), gunlukTekil.GetValueOrDefault(g)));

        ozet.ToplamZiyaret = gunlukZiyaret.Values.Sum();
        ozet.TekilZiyaretciGunToplami = gunlukTekil.Values.Sum();

        ozet.Demolar = demolar.OrderByDescending(x => x.Value).Take(10).Select(x => (x.Key, x.Value)).ToList();
        ozet.Baglantilar = baglantilar.OrderByDescending(x => x.Value).Take(10).Select(x => (x.Key, x.Value)).ToList();
        ozet.KaynakSiteler = kaynakSiteler.OrderByDescending(x => x.Value).Take(10).Select(x => (x.Key, x.Value)).ToList();

        return ozet;
    }
}
