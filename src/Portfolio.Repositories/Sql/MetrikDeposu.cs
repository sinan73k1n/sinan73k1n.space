using Microsoft.EntityFrameworkCore;

namespace Portfolio.Repositories.Sql;

/// <summary>Metrik yazma/okuma. Yalnız SQL deposu varken kayıtlıdır.</summary>
public interface IMetrikDeposu
{
    /// <summary>O güne ait tuzu getirir; yoksa üretip kaydeder.</summary>
    Task<string> GunlukTuzAsync(DateOnly gun, Func<string> uret, CancellationToken ct = default);

    Task ZiyaretYazAsync(ZiyaretRow satir, CancellationToken ct = default);
    Task OlaylarYazAsync(IReadOnlyList<OlayRow> satirlar, CancellationToken ct = default);

    // --- Özetleme / temizlik (bkz. MetrikOzetleyiciDeposu.cs) ---
    Task OzetleAsync(DateOnly gun, CancellationToken ct = default);
    Task<IReadOnlyList<DateOnly>> OzetlenecekGunlerAsync(DateOnly bugun, CancellationToken ct = default);
    Task<int> TemizleAsync(DateOnly sinir, CancellationToken ct = default);
}

public sealed partial class MetrikDeposu : IMetrikDeposu
{
    private readonly IDbContextFactory<PortfolioDbContext> _fabrika;

    public MetrikDeposu(IDbContextFactory<PortfolioDbContext> fabrika) => _fabrika = fabrika;

    public async Task<string> GunlukTuzAsync(DateOnly gun, Func<string> uret, CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);

        var mevcut = await db.GunlukTuzlar.AsNoTracking().FirstOrDefaultAsync(x => x.Gun == gun, ct);
        if (mevcut is not null) return mevcut.Tuz;

        var yeni = new GunlukTuzRow { Gun = gun, Tuz = uret() };
        db.GunlukTuzlar.Add(yeni);

        try
        {
            await db.SaveChangesAsync(ct);
            return yeni.Tuz;
        }
        catch (DbUpdateException)
        {
            // Yarış: gün dönümünde iki istek aynı anda tuz üretmeye kalkabilir.
            // Birincil anahtar çakışması → kaybeden, kazananın tuzunu okur.
            // (Kendi tuzunda ısrar etseydi aynı ziyaretçi o gün İKİ kimlik alırdı.)
            await using var tekrar = await _fabrika.CreateDbContextAsync(ct);
            var kazanan = await tekrar.GunlukTuzlar.AsNoTracking().FirstOrDefaultAsync(x => x.Gun == gun, ct);
            return kazanan?.Tuz ?? yeni.Tuz;
        }
    }

    public async Task ZiyaretYazAsync(ZiyaretRow satir, CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);
        db.Ziyaretler.Add(satir);
        await db.SaveChangesAsync(ct);
    }

    public async Task OlaylarYazAsync(IReadOnlyList<OlayRow> satirlar, CancellationToken ct = default)
    {
        if (satirlar.Count == 0) return;

        await using var db = await _fabrika.CreateDbContextAsync(ct);
        db.Olaylar.AddRange(satirlar);
        await db.SaveChangesAsync(ct);
    }
}
