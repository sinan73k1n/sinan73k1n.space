using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Portfolio.Entities.Content;

namespace Portfolio.Repositories;

/// <summary>
/// İçeriği JSON dosyasından okur/yazar (Faz 3-5 — DB'siz çalışma).
/// Kaynak dosya `App_Data/seed-content.json`; ilk hâli design_handoff'taki
/// `site-data.js → SEED`'den ÜRETİLDİ (elle kopyalanmadı).
///
/// Okuma: bir kez okur, bellekte tutar. Yazma: **atomik** (geçici dosya + rename)
/// → süreç yazarken ölse bile yarım/bozuk dosya kalmaz. Yazımdan önce **yedek**
/// alınır (`.bak`), çünkü bu dosya sitenin TEK içerik kaynağı.
/// </summary>
public sealed class JsonContentStore : IContentStore
{
    private static readonly JsonSerializerOptions OkumaSecenekleri = new()
    {
        PropertyNameCaseInsensitive = true   // JSON camelCase ↔ C# PascalCase
    };

    private static readonly JsonSerializerOptions YazmaSecenekleri = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Türkçe/Rusça karakterler \uXXXX'e kaçmasın — dosya elle de okunabilir kalsın
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
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
            _onbellek = await JsonSerializer.DeserializeAsync<SiteContent>(akis, OkumaSecenekleri, ct)
                        ?? throw new InvalidOperationException($"İçerik dosyası boş/geçersiz: {_dosyaYolu}");

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

        await _kilit.WaitAsync(ct);
        try
        {
            var klasor = Path.GetDirectoryName(_dosyaYolu)!;
            Directory.CreateDirectory(klasor);

            // 1) Geçici dosyaya yaz (aynı dizinde — rename'in atomik olması için aynı birimde olmalı)
            var gecici = Path.Combine(klasor, $".{Path.GetFileName(_dosyaYolu)}.tmp");
            await using (var akis = File.Create(gecici))
                await JsonSerializer.SerializeAsync(akis, icerik, YazmaSecenekleri, ct);

            // 2) Mevcut dosyayı yedekle (tek içerik kaynağı — geri dönüş şansı kalsın)
            if (File.Exists(_dosyaYolu))
                File.Copy(_dosyaYolu, _dosyaYolu + ".bak", overwrite: true);

            // 3) Atomik yerine koyma
            File.Move(gecici, _dosyaYolu, overwrite: true);

            _onbellek = icerik;
        }
        finally
        {
            _kilit.Release();
        }
    }
}
