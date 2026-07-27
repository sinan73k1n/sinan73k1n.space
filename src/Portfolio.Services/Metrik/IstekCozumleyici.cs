using Portfolio.Entities.Metrik;

namespace Portfolio.Services.Metrik;

/// <summary>
/// Ham HTTP başlıklarını metrik alanlarına çevirir: kaynak sınıflandırma,
/// cihaz tespiti, bot elemesi. Saf fonksiyonlar → HTTP olmadan test edilebilir.
/// </summary>
public static class IstekCozumleyici
{
    // Arama motorları. Alan adının SON iki etiketi eşleştirilir (google.com.tr → google).
    private static readonly string[] AramaMotorlari =
        { "google", "bing", "duckduckgo", "yandex", "yahoo", "baidu", "ecosia", "brave", "startpage", "qwant" };

    private static readonly string[] SosyalAglar =
        { "linkedin", "twitter", "x.com", "t.co", "facebook", "instagram", "reddit", "youtube",
          "medium", "mastodon", "bsky", "telegram", "whatsapp", "pinterest", "tiktok", "discord" };

    // Bot imzaları. Tam liste imkânsız; amaç trafiği "temiz sayılabilir" hâle getirmek.
    private static readonly string[] BotImzalari =
        { "bot", "crawl", "spider", "slurp", "curl", "wget", "python-requests", "httpclient",
          "headless", "phantom", "lighthouse", "pagespeed", "monitor", "uptime", "preview",
          "scraper", "fetcher", "archiver", "validator", "feed", "postman", "insomnia" };

    /// <summary>
    /// Bot mu? Doğruysa istek KAYDEDİLMEZ.
    /// <para>
    /// User-Agent boşsa da bot sayılır: gerçek tarayıcılar her zaman gönderir,
    /// göndermeyen genelde script'tir.
    /// </para>
    /// </summary>
    public static bool BotMu(string? tarayici)
    {
        if (string.IsNullOrWhiteSpace(tarayici)) return true;

        var t = tarayici.ToLowerInvariant();
        foreach (var imza in BotImzalari)
            if (t.Contains(imza, StringComparison.Ordinal)) return true;

        return false;
    }

    /// <summary>
    /// Referrer'ı sınıflandırır ve host'unu döndürür.
    /// <para>
    /// ⚠️ Kendi alan adımızdan gelen referrer <b>dogrudan</b> sayılır: site tek sayfa,
    /// kendi içinden gelen istek yeni bir ziyaret kaynağı değildir. Aksi hâlde her
    /// iç gezinme "yonlendirme" olarak birikir ve kaynak dağılımı anlamsızlaşırdı.
    /// </para>
    /// </summary>
    /// <param name="referrer">Referer başlığı (olmayabilir).</param>
    /// <param name="kendiHost">Sitenin kendi host'u (ör. sinan73k1n.space).</param>
    public static (string Tip, string Host) KaynagiCoz(string? referrer, string? kendiHost)
    {
        if (string.IsNullOrWhiteSpace(referrer)) return (KaynakTipi.Dogrudan, "");

        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
            return (KaynakTipi.Dogrudan, "");

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];

        // Kendi sitemiz (alt alan adları dahil: demo.sinan73k1n.space)
        if (!string.IsNullOrWhiteSpace(kendiHost))
        {
            var kendi = kendiHost.ToLowerInvariant();
            if (kendi.StartsWith("www.", StringComparison.Ordinal)) kendi = kendi[4..];
            if (host == kendi || host.EndsWith("." + kendi, StringComparison.Ordinal))
                return (KaynakTipi.Dogrudan, "");
        }

        foreach (var motor in AramaMotorlari)
            if (host.Contains(motor, StringComparison.Ordinal)) return (KaynakTipi.Organik, host);

        foreach (var sosyal in SosyalAglar)
            if (host.Contains(sosyal, StringComparison.Ordinal)) return (KaynakTipi.Sosyal, host);

        return (KaynakTipi.Yonlendirme, host);
    }

    /// <summary>
    /// Cihaz sınıfı. Kaba ama yeterli: amaç "mobilden mi bakılıyor" sorusu,
    /// cihaz parmak izi değil.
    /// </summary>
    public static string CihazCoz(string? tarayici)
    {
        if (string.IsNullOrWhiteSpace(tarayici)) return CihazTipi.Masaustu;

        var t = tarayici.ToLowerInvariant();

        // Sıra önemli: iPad'in UA'sı "mobile" de içerebilir → önce tablet bakılır.
        if (t.Contains("ipad") || (t.Contains("android") && !t.Contains("mobile")) || t.Contains("tablet"))
            return CihazTipi.Tablet;

        if (t.Contains("mobi") || t.Contains("iphone") || t.Contains("ipod") || t.Contains("android"))
            return CihazTipi.Mobil;

        return CihazTipi.Masaustu;
    }
}
