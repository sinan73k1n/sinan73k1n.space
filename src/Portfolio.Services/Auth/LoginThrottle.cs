using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Portfolio.Services.Auth;

/// <summary>
/// Kaba kuvvet koruması — <b>bu projeye özel, AdminPanel'de YOKTU.</b>
/// Sebep: AdminPanel Tailscale arkasındaydı, login'e internetten ulaşılamıyordu.
/// Burada <c>/admin</c> İNTERNETE AÇIK → deneme sınırı olmadan şifre + 6 haneli TOTP
/// kaba kuvvetle denenebilir (TOTP alanı yalnız 10^6 olasılık).
///
/// Tasarım: IP başına başarısız sayacı + üstel kilit (60s → 120s → 240s … tavan 1sa).
/// Bellekte tutulur (tek örnek uygulama, DB'siz). Faz 4'ten sonra kalıcı hâle
/// getirilebilir ama bellek zaten yeterli: süreç yeniden başlarsa saldırgan da
/// baştan başlar, meşru kullanıcı da kilitten kurtulur.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>Kilitliyse kalan süre, değilse null.</summary>
    TimeSpan? KilitliMi(string anahtar);
    void BasarisizDeneme(string anahtar);
    void Sifirla(string anahtar);
}

public sealed class LoginThrottle : ILoginThrottle
{
    private sealed class Durum
    {
        public int Basarisiz;
        public int KilitTuru;              // kaçıncı kez kilitlendi (üstel artış için)
        public DateTimeOffset? KilitBitis;
        public DateTimeOffset SonHareket = DateTimeOffset.UtcNow;
    }

    private readonly ConcurrentDictionary<string, Durum> _kayit = new();
    private readonly AuthOptions _opt;

    public LoginThrottle(IOptions<AuthOptions> options) => _opt = options.Value;

    public TimeSpan? KilitliMi(string anahtar)
    {
        Temizle();
        if (!_kayit.TryGetValue(anahtar, out var d) || d.KilitBitis is null) return null;

        var kalan = d.KilitBitis.Value - DateTimeOffset.UtcNow;
        if (kalan <= TimeSpan.Zero)
        {
            d.KilitBitis = null;
            d.Basarisiz = 0;               // kilit doldu, sayaç sıfırlanır (kilit turu KORUNUR)
            return null;
        }
        return kalan;
    }

    public void BasarisizDeneme(string anahtar)
    {
        var d = _kayit.GetOrAdd(anahtar, _ => new Durum());
        lock (d)
        {
            d.SonHareket = DateTimeOffset.UtcNow;
            d.Basarisiz++;
            if (d.Basarisiz < _opt.MaxAttempts) return;

            // Üstel: 60s, 120s, 240s … tavanla sınırlı
            var saniye = Math.Min(
                _opt.LockoutSeconds * Math.Pow(2, d.KilitTuru),
                _opt.MaxLockoutSeconds);
            d.KilitTuru++;
            d.Basarisiz = 0;
            d.KilitBitis = DateTimeOffset.UtcNow.AddSeconds(saniye);
        }
    }

    /// <summary>Başarılı girişte sayaç ve kilit turu tamamen temizlenir.</summary>
    public void Sifirla(string anahtar) => _kayit.TryRemove(anahtar, out _);

    /// <summary>24 saattir hareketsiz kayıtları at (bellek sızdırmasın).</summary>
    private void Temizle()
    {
        if (_kayit.Count < 512) return;
        var esik = DateTimeOffset.UtcNow.AddHours(-24);
        foreach (var (k, v) in _kayit)
            if (v.SonHareket < esik && v.KilitBitis is null)
                _kayit.TryRemove(k, out _);
    }
}
