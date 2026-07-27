using Microsoft.EntityFrameworkCore;
using Portfolio.Entities.Content;

namespace Portfolio.Repositories.Sql;

/// <summary>
/// İçeriği MSSQL'den okur/yazar (Faz 4). <see cref="JsonContentStore"/> ile
/// AYNI arayüz → view/servis/admin katmanı hiç değişmez, DI'da tek satır farkeder.
///
/// <para>
/// <b>Yazma stratejisi: tam değiştirme, tek transaction.</b> Admin katmanı
/// (<c>AdminContentService</c>) her düzenlemede kök <see cref="SiteContent"/>'i
/// yükleyip tamamını geri kaydediyor — "şu alanı güncelle" diye bir çağrı yok.
/// Bu yüzden satır bazlı diff tutmanın karşılığı yok: tabloları boşaltıp yeniden
/// yazmak hem daha basit hem de sıralamayı (<c>Order</c>) doğal olarak koruyor.
/// Veri kümesi onlarca satır — maliyet önemsiz. Transaction sayesinde yarı
/// yazılmış içerik oluşamaz (JSON deposundaki atomik rename'in DB karşılığı).
/// </para>
/// <para>
/// Satır Id'leri her kayıtta yeniden üretilir; DIŞARIDAN HİÇBİR ŞEY bu Id'lere
/// referans vermez (oyun kapağı bile dosya yolu üzerinden bağlı).
/// Eşleme mantığı burada değil <see cref="IcerikSatirlari"/>'nda — DB'siz test edilsin diye.
/// </para>
/// </summary>
public sealed class SqlContentStore : IContentStore
{
    private readonly IDbContextFactory<PortfolioDbContext> _fabrika;
    private readonly SemaphoreSlim _kilit = new(1, 1);
    private SiteContent? _onbellek;

    public SqlContentStore(IDbContextFactory<PortfolioDbContext> fabrika) => _fabrika = fabrika;

    public async Task<SiteContent> LoadAsync(CancellationToken ct = default)
    {
        if (_onbellek is not null) return _onbellek;

        await _kilit.WaitAsync(ct);
        try
        {
            if (_onbellek is not null) return _onbellek;   // kilidi beklerken başkası doldurmuş olabilir

            await using var db = await _fabrika.CreateDbContextAsync(ct);

            var satirlar = new IcerikSatirlari
            {
                Meta = await db.SiteMeta.AsNoTracking().FirstOrDefaultAsync(ct),
                Copy = await db.Copy.AsNoTracking().ToListAsync(ct),
                HeroRoles = await db.HeroRoles.AsNoTracking().ToListAsync(ct),
                Logs = await db.TerminalLogs.AsNoTracking().ToListAsync(ct),
                Facts = await db.Facts.AsNoTracking().ToListAsync(ct),
                Techs = await db.Techs.AsNoTracking().ToListAsync(ct),
                Games = await db.Games.AsNoTracking().ToListAsync(ct),
                Demos = await db.Demos.AsNoTracking().ToListAsync(ct),
                Repos = await db.Repos.AsNoTracking().ToListAsync(ct),
                Covers = await db.Covers.AsNoTracking().ToListAsync(ct)
            };

            _onbellek = satirlar.Birlestir();
            return _onbellek;
        }
        finally
        {
            _kilit.Release();
        }
    }

    public async Task SaveAsync(SiteContent icerik, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(icerik);

        var satirlar = IcerikSatirlari.Ayristir(icerik);

        await _kilit.WaitAsync(ct);
        try
        {
            await using var db = await _fabrika.CreateDbContextAsync(ct);
            await using var islem = await db.Database.BeginTransactionAsync(ct);

            // 1) Boşalt — ExecuteDelete sunucu tarafında tek DELETE, satırları belleğe çekmez.
            await db.Copy.ExecuteDeleteAsync(ct);
            await db.HeroRoles.ExecuteDeleteAsync(ct);
            await db.TerminalLogs.ExecuteDeleteAsync(ct);
            await db.Facts.ExecuteDeleteAsync(ct);
            await db.Techs.ExecuteDeleteAsync(ct);
            await db.Games.ExecuteDeleteAsync(ct);
            await db.Demos.ExecuteDeleteAsync(ct);
            await db.Repos.ExecuteDeleteAsync(ct);
            await db.Covers.ExecuteDeleteAsync(ct);
            await db.SiteMeta.ExecuteDeleteAsync(ct);

            // 2) Yeniden yaz
            if (satirlar.Meta is not null) db.SiteMeta.Add(satirlar.Meta);
            db.Copy.AddRange(satirlar.Copy);
            db.HeroRoles.AddRange(satirlar.HeroRoles);
            db.TerminalLogs.AddRange(satirlar.Logs);
            db.Facts.AddRange(satirlar.Facts);
            db.Techs.AddRange(satirlar.Techs);
            db.Games.AddRange(satirlar.Games);
            db.Demos.AddRange(satirlar.Demos);
            db.Repos.AddRange(satirlar.Repos);
            db.Covers.AddRange(satirlar.Covers);

            await db.SaveChangesAsync(ct);
            await islem.CommitAsync(ct);

            _onbellek = icerik;
        }
        finally
        {
            _kilit.Release();
        }
    }

    /// <summary>
    /// Tohumlama gerekiyor mu? "Boş" ölçütü künye satırı DEĞİL, gerçek içerik:
    /// kopya tablosu. (Yarım kalmış bir aktarımdan sonra künye var ama içerik
    /// yoksa yine dolduruyoruz.)
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu, tohumun KENDİSİNİ almaktan ayrı bir çağrı — bilerek. Tek bir
    /// <c>Tohumla(tohum)</c> metodu olsaydı çağıran, DB dolu olsa bile JSON
    /// tohumunu okumak zorunda kalırdı; o dosya silindiğinde DB'de her şey
    /// yerinde olmasına rağmen uygulama açılmazdı.
    /// </remarks>
    public async Task<bool> TohumlamaGerekliMiAsync(CancellationToken ct = default)
    {
        await using var db = await _fabrika.CreateDbContextAsync(ct);
        return !await db.Copy.AnyAsync(ct);
    }
}
