using Microsoft.EntityFrameworkCore;
using Portfolio.Entities.Metrik;

namespace Portfolio.Repositories.Sql;

public sealed partial class MetrikDeposu
{
    /// <summary>
    /// Bir günün ham verisini <see cref="GunlukOzetRow"/>'a çevirir.
    ///
    /// <para>
    /// <b>Idempotent:</b> önce o güne ait özetler silinir, sonra yeniden yazılır.
    /// İş yarıda kalıp tekrar çalışsa bile sayılar ikiye katlanmaz — "artır"
    /// yerine "yeniden hesapla" seçilmesinin sebebi bu.
    /// </para>
    /// <para>
    /// Gruplamalar SQL tarafında yapılır (<c>GroupBy</c> → <c>GROUP BY</c>);
    /// ham satırlar uygulamaya çekilmez.
    /// </para>
    /// </summary>
    public async Task OzetleAsync(DateOnly gun, CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);

        var satirlar = new List<GunlukOzetRow>();
        void Ekle(string tip, string anahtar, int deger)
        {
            if (deger > 0) satirlar.Add(new GunlukOzetRow { Gun = gun, Tip = tip, Anahtar = anahtar, Deger = deger });
        }

        var ziyaretler = db.Ziyaretler.Where(x => x.Gun == gun);

        Ekle(OzetTipi.Toplam, "", await ziyaretler.CountAsync(ct));
        Ekle(OzetTipi.Tekil, "", await ziyaretler.Select(x => x.ZiyaretciHash).Distinct().CountAsync(ct));

        foreach (var g in await ziyaretler.GroupBy(x => x.KaynakTipi)
                     .Select(g => new { g.Key, Adet = g.Count() }).ToListAsync(ct))
            Ekle(OzetTipi.Kaynak, g.Key, g.Adet);

        foreach (var g in await ziyaretler.GroupBy(x => x.Dil)
                     .Select(g => new { g.Key, Adet = g.Count() }).ToListAsync(ct))
            Ekle(OzetTipi.Dil, g.Key, g.Adet);

        foreach (var g in await ziyaretler.GroupBy(x => x.Cihaz)
                     .Select(g => new { g.Key, Adet = g.Count() }).ToListAsync(ct))
            Ekle(OzetTipi.Cihaz, g.Key, g.Adet);

        // Yönlendiren siteler de özete girer: ham veri 90 gün sonra silinince
        // "hangi siteden geldiler" bilgisi tamamen kaybolmasın.
        foreach (var g in await ziyaretler.Where(x => x.KaynakHost != "").GroupBy(x => x.KaynakHost)
                     .Select(g => new { g.Key, Adet = g.Count() }).ToListAsync(ct))
            Ekle(OzetTipi.KaynakSite, g.Key, g.Adet);

        var olaylar = db.Olaylar.Where(x => x.Gun == gun);

        // Bölüm: KAÇ KİŞİ gördü (kaç kez değil). "Ziyaretçilerin %40'ı GitHub'a kadar
        // indi" cümlesini kurabilmek için tekil sayım gerekiyor; olay sayısı aynı
        // kişinin aşağı-yukarı kaydırmasıyla şişerdi.
        foreach (var g in await olaylar.Where(x => x.Tip == OlayTipi.Bolum)
                     .GroupBy(x => x.Deger)
                     .Select(g => new { g.Key, Adet = g.Select(x => x.ZiyaretciHash).Distinct().Count() })
                     .ToListAsync(ct))
            Ekle(OzetTipi.Bolum, g.Key, g.Adet);

        // Demo ve bağlantı: toplam eylem sayısı (aynı kişi iki demo açtıysa ikisi de sayılır).
        foreach (var g in await olaylar.Where(x => x.Tip == OlayTipi.Demo)
                     .GroupBy(x => x.Deger).Select(g => new { g.Key, Adet = g.Count() }).ToListAsync(ct))
            Ekle(OzetTipi.Demo, g.Key, g.Adet);

        foreach (var g in await olaylar.Where(x => x.Tip == OlayTipi.Baglanti)
                     .GroupBy(x => x.Deger).Select(g => new { g.Key, Adet = g.Count() }).ToListAsync(ct))
            Ekle(OzetTipi.Baglanti, g.Key, g.Adet);

        // Süre: bölüm başına TOPLAM saniye. Ortalama, okuma anında
        // (toplam süre ÷ o bölümü gören kişi) hesaplanır — özet ham kalsın.
        foreach (var g in await olaylar.Where(x => x.Tip == OlayTipi.Sure)
                     .GroupBy(x => x.Deger)
                     .Select(g => new { g.Key, Toplam = g.Sum(x => x.SaniyeSure) }).ToListAsync(ct))
            Ekle(OzetTipi.Sure, g.Key, g.Toplam);

        await using var islem = await db.Database.BeginTransactionAsync(ct);
        await db.GunlukOzetler.Where(x => x.Gun == gun).ExecuteDeleteAsync(ct);
        db.GunlukOzetler.AddRange(satirlar);
        await db.SaveChangesAsync(ct);
        await islem.CommitAsync(ct);
    }

    /// <summary>
    /// Özetlenmesi gereken günler: ham verisi olup henüz özeti olmayan,
    /// <b>bugün hariç</b> (bugün daha bitmedi, sayıları değişmeye devam ediyor).
    /// </summary>
    public async Task<IReadOnlyList<DateOnly>> OzetlenecekGunlerAsync(DateOnly bugun, CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);

        var hamGunler = await db.Ziyaretler.Where(x => x.Gun < bugun)
            .Select(x => x.Gun).Distinct().ToListAsync(ct);

        var ozetliGunler = await db.GunlukOzetler.Where(x => x.Gun < bugun)
            .Select(x => x.Gun).Distinct().ToListAsync(ct);

        return hamGunler.Except(ozetliGunler).OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Ham veriyi ve o günlere ait tuzları siler (varsayılan 90 gün).
    /// <para>
    /// ⚠️ Tuzun da silinmesi kasıtlı: tuz gidince o günün hash'leri artık hiçbir
    /// IP'ye geri bağlanamaz. Veri "eski" olmakla kalmaz, <b>çözülemez</b> olur.
    /// </para>
    /// </summary>
    /// <returns>Silinen ziyaret + olay satırı sayısı.</returns>
    public async Task<int> TemizleAsync(DateOnly sinir, CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);

        var z = await db.Ziyaretler.Where(x => x.Gun < sinir).ExecuteDeleteAsync(ct);
        var o = await db.Olaylar.Where(x => x.Gun < sinir).ExecuteDeleteAsync(ct);
        await db.GunlukTuzlar.Where(x => x.Gun < sinir).ExecuteDeleteAsync(ct);

        return z + o;
    }
}
