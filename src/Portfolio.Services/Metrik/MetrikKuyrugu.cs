using System.Threading.Channels;

namespace Portfolio.Services.Metrik;

/// <summary>Kuyruğa atılan tek bir metrik yazma işi.</summary>
public abstract record MetrikIsi;

public sealed record ZiyaretIsi(
    DateTime ZamanUtc,
    string? Ip,
    string? Tarayici,
    string Yol,
    string Dil,
    string? Referrer,
    string? KendiHost) : MetrikIsi;

public sealed record OlayIsi(
    DateTime ZamanUtc,
    string? Ip,
    string? Tarayici,
    IReadOnlyList<(string Tip, string Deger, int SaniyeSure)> Olaylar) : MetrikIsi;

/// <summary>
/// Metrik yazımının önündeki tampon.
///
/// <para>
/// <b>Neden kuyruk:</b> ziyaret kaydı sayfa yanıtının önünde durmamalı. Doğrudan
/// yazsaydık her sayfa görüntüleme bir DB gidiş-dönüşü kadar YAVAŞLAR, ve mssql
/// bir an takılırsa (bu sunucuda yaşanmış bir olay) <b>site çökerdi</b>.
/// Metrik, içeriğin doğruluğu kadar kritik değil — kaybı tolere edilebilir,
/// gecikmesi tolere edilemez.
/// </para>
/// <para>
/// <b>Sınırlı kapasite + dolunca DÜŞÜR:</b> ani bir trafikte kuyruk sonsuz büyüseydi
/// bellek şişer ve 4 GB'lık sunucuda asıl siteyi tehdit ederdi. Dolu kuyrukta yeni
/// kayıt sessizce düşer — birkaç ziyaret eksik sayılır, site ayakta kalır.
/// </para>
/// </summary>
public interface IMetrikKuyrugu
{
    /// <summary>Kuyruğa ekler. Kuyruk doluysa <c>false</c> döner ve iş DÜŞER (bloklamaz).</summary>
    bool Ekle(MetrikIsi is_);

    IAsyncEnumerable<MetrikIsi> OkuAsync(CancellationToken ct);

    /// <summary>Kapasite dolduğu için düşen iş sayısı (izleme için).</summary>
    long DusenSayisi { get; }
}

public sealed class MetrikKuyrugu : IMetrikKuyrugu
{
    private readonly Channel<MetrikIsi> _kanal;
    private long _dusen;

    public MetrikKuyrugu(int kapasite = 2000)
    {
        _kanal = Channel.CreateBounded<MetrikIsi>(new BoundedChannelOptions(kapasite)
        {
            // ⚠️ `Wait` seçili ama HİÇBİR ZAMAN beklenmiyor: yalnız TryWrite
            // kullanıyoruz ve o, dolu kuyrukta anında `false` döner.
            //
            // İlk hâli `DropWrite`'tı ve YANLIŞTI: o modda TryWrite, öğeyi
            // düşürdüğü hâlde `true` döner → düşen sayacı hiç artmaz, kuyruk
            // sessizce veri kaybederken bunu göremezdik. (Test yakaladı.)
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public long DusenSayisi => Interlocked.Read(ref _dusen);

    public bool Ekle(MetrikIsi is_)
    {
        if (_kanal.Writer.TryWrite(is_)) return true;

        Interlocked.Increment(ref _dusen);
        return false;
    }

    public IAsyncEnumerable<MetrikIsi> OkuAsync(CancellationToken ct) =>
        _kanal.Reader.ReadAllAsync(ct);
}
