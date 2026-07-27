using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Portfolio.Entities;
using Portfolio.Entities.Content;

namespace Portfolio.Repositories;

/// <summary>
/// İçeriği JSON dosyasından okur/yazar — **Mac'te DB'siz geliştirme** içindir.
/// Production'da içerik MSSQL'den gelir (<c>SqlContentStore</c>).
///
/// Okuma: bir kez okur, bellekte tutar. Yazma: **atomik** (geçici dosya + rename)
/// → süreç yazarken ölse bile yarım/bozuk dosya kalmaz. Yazımdan önce **yedek**
/// alınır (`.bak`).
///
/// <para>
/// Dosya yoksa <see cref="SeedIcerik"/>'ten üretilir. Tohum artık bir DOSYA değil,
/// **kod** (karar: 2026-07-27) — ömründe bir kez çalışan bir şeyin publish çıktısında
/// taşınması ve "tohum dosyası yok" diye bir başlatma hata sınıfı doğurması gereksizdi.
/// </para>
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
    private readonly Func<SiteContent>? _tohum;
    private readonly SemaphoreSlim _kilit = new(1, 1);
    private SiteContent? _onbellek;

    /// <param name="dosyaYolu">CANLI içerik dosyası. Production'da publish klasörünün DIŞINDA olmalı.</param>
    /// <param name="tohum">
    /// Dosya yoksa içeriği üreten tohum (varsayılan <see cref="SeedIcerik.Olustur"/>).
    /// Bir kez çalışır; dosya VARSA çağrılmaz bile.
    /// <para>
    /// ⚠️ Canlı dosyanın publish DIŞINDA durması kritik: içeride dursaydı her deploy
    /// (rsync) admin'den yapılan düzenlemeleri tohumla EZERDİ.
    /// </para>
    /// </param>
    public JsonContentStore(string dosyaYolu, Func<SiteContent>? tohum = null)
    {
        _dosyaYolu = dosyaYolu;
        _tohum = tohum ?? SeedIcerik.Olustur;
    }

    public async Task<SiteContent> LoadAsync(CancellationToken ct = default)
    {
        if (_onbellek is not null) return _onbellek;

        await _kilit.WaitAsync(ct);
        try
        {
            if (_onbellek is not null) return _onbellek;   // kilidi beklerken başkası doldurmuş olabilir

            if (!File.Exists(_dosyaYolu))
            {
                // İlk çalıştırma: dosya yok → tohumu koddan üret ve YAZ.
                // Yazıyoruz ki sonraki düzenlemeler kalıcı olsun (tohum salt okunur bir
                // başlangıç; kullanıcının değiştirdiği içerik dosyada birikir).
                _onbellek = _tohum!();
                await YazAsync(_onbellek, ct);
                return _onbellek;
            }

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
            await YazAsync(icerik, ct);
            _onbellek = icerik;
        }
        finally
        {
            _kilit.Release();
        }
    }

    /// <summary>
    /// Atomik yazma. ⚠️ Kilidi ALMAZ — çağıran zaten tutuyor olmalı.
    /// (Tohumlama de bunu kullanıyor; kilit orada Load tarafından tutuluyor.)
    /// </summary>
    private async Task YazAsync(SiteContent icerik, CancellationToken ct)
    {
        var klasor = Path.GetDirectoryName(_dosyaYolu)!;
        if (!string.IsNullOrEmpty(klasor)) Directory.CreateDirectory(klasor);

        // 1) Geçici dosyaya yaz (aynı dizinde — rename'in atomik olması için aynı birimde olmalı)
        var gecici = Path.Combine(klasor, $".{Path.GetFileName(_dosyaYolu)}.tmp");
        await using (var akis = File.Create(gecici))
            await JsonSerializer.SerializeAsync(akis, icerik, YazmaSecenekleri, ct);

        // 2) Mevcut dosyayı yedekle (geri dönüş şansı kalsın)
        if (File.Exists(_dosyaYolu))
            File.Copy(_dosyaYolu, _dosyaYolu + ".bak", overwrite: true);

        // 3) Atomik yerine koyma
        File.Move(gecici, _dosyaYolu, overwrite: true);
    }
}
