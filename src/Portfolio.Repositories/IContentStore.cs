using Portfolio.Entities.Content;

namespace Portfolio.Repositories;

/// <summary>
/// İçerik deposu soyutlaması.
/// İki implementasyon planı (AdminPanel'in "Sql + InMemory" deseni):
///   · <see cref="JsonContentStore"/> — seed dosyası, DB'siz geliştirme (Faz 3-5)
///   · SqlContentStore              — MSSQL + EF Core (Faz 4, Tailscale gerekiyor)
/// Böylece DB'yi beklemek işi BLOKE ETMİYOR; ortam bazlı DI ile seçilir.
/// </summary>
public interface IContentStore
{
    Task<SiteContent> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// İçeriği kalıcı hâle getirir (admin "Kaydet"). Uygulama **atomik** olmalı:
    /// yarım yazılmış dosya/kayıt bırakma. Kaydettikten sonra önbellek tazelenir.
    /// </summary>
    Task SaveAsync(SiteContent icerik, CancellationToken ct = default);
}
