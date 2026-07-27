using System.Text.Json;
using Portfolio.Entities.Content;

namespace Portfolio.Repositories;

/// <summary>
/// İçeriği JSON dosyasından okur (Faz 3 — DB'siz geliştirme).
/// Kaynak dosya `App_Data/seed-content.json`; içeriği design_handoff'taki
/// `site-data.js → SEED`'den ÜRETİLDİ (elle kopyalanmadı, metinler birebir).
///
/// Bir kez okur, bellekte tutar (içerik salt okunur ve nadiren değişir).
/// Faz 5'te admin yazma geldiğinde bu sınıf yazma da öğrenecek ya da
/// SqlContentStore devralacak.
/// </summary>
public sealed class JsonContentStore : IContentStore
{
    private static readonly JsonSerializerOptions Secenekler = new()
    {
        PropertyNameCaseInsensitive = true   // JSON camelCase ↔ C# PascalCase
    };

    private readonly string _dosyaYolu;
    private readonly SemaphoreSlim _kilit = new(1, 1);
    private SiteContent? _onbellek;

    public JsonContentStore(string dosyaYolu) => _dosyaYolu = dosyaYolu;

    public async Task<SiteContent> LoadAsync(CancellationToken ct = default)
    {
        if (_onbellek is not null) return _onbellek;

        await _kilit.WaitAsync(ct);
        try
        {
            if (_onbellek is not null) return _onbellek;   // kilidi beklerken başkası doldurmuş olabilir

            if (!File.Exists(_dosyaYolu))
                throw new FileNotFoundException($"İçerik dosyası bulunamadı: {_dosyaYolu}", _dosyaYolu);

            await using var akis = File.OpenRead(_dosyaYolu);
            _onbellek = await JsonSerializer.DeserializeAsync<SiteContent>(akis, Secenekler, ct)
                        ?? throw new InvalidOperationException($"İçerik dosyası boş/geçersiz: {_dosyaYolu}");

            return _onbellek;
        }
        finally
        {
            _kilit.Release();
        }
    }
}
