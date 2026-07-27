using Portfolio.Services.Auth;

namespace Portfolio.SITE_UI;

/// <summary>
/// `--setup-auth` modu: admin şifresi + TOTP kimliği üretir.
/// Sinan bunu KENDİ terminalinde interaktif çalıştırır → şifre yalnız stdin'den girilir;
/// argümana, log'a, transcript'e DÜŞMEZ. Diskte yalnız PBKDF2 hash'i saklanır.
/// Çıktı: 0600 izinli env dosyası + Authenticator'a girilecek otpauth URI.
/// </summary>
public static class AuthSetup
{
    public static int Run(string[] args)
    {
        Console.WriteLine("=== Portfolyo admin kimlik kurulumu (şifre + TOTP 2FA) ===");
        Console.WriteLine("⚠️  Admin paneli İNTERNETE AÇIK olacak — güçlü ve BU SİTEYE ÖZEL bir şifre seç.");
        Console.WriteLine();

        Console.Write("Kullanıcı adı [admin]: ");
        var user = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(user)) user = "admin";

        var pw = SirOku("Şifre: ");
        var pw2 = SirOku("Şifre (tekrar): ");
        if (pw != pw2) { Console.Error.WriteLine("✗ Şifreler uyuşmuyor. İptal."); return 1; }
        if (pw.Length < 12)
        {
            Console.Error.WriteLine("✗ Şifre en az 12 karakter olmalı (panel public, sınır AdminPanel'den yüksek). İptal.");
            return 1;
        }

        var hash = PasswordHasher.Hash(pw);
        var secret = Totp.GenerateSecret();
        var uri = Totp.OtpAuthUri("sinan73k1n.space", user!, secret);

        var cikti = ArgDegeri(args, "--out")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "deploy-staging", "portfolio-auth.env");
        Directory.CreateDirectory(Path.GetDirectoryName(cikti)!);
        File.WriteAllText(cikti,
            $"Auth__Username={user}\nAuth__PasswordHash={hash}\nAuth__TotpSecret={secret}\n");
        try { File.SetUnixFileMode(cikti, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { /* Windows */ }

        Console.WriteLine();
        Console.WriteLine($"✅ Kimlik env dosyası yazıldı (0600): {cikti}");
        Console.WriteLine();
        Console.WriteLine("── Authenticator'a EKLE (Google Authenticator / Authy) ──");
        Console.WriteLine(uri);
        Console.WriteLine();
        Console.WriteLine($"Elle girmek istersen secret: {secret}");
        Console.WriteLine();
        Console.WriteLine("── SONRAKİ ADIMLAR (sunucu) ──");
        Console.WriteLine("1) Dosyayı servis kullanıcısının okuyabileceği yere taşı:");
        Console.WriteLine("   sudo install -m 0600 -o $USER -g $USER <dosya> /etc/portfolio/auth.env");
        Console.WriteLine("2) systemd birimine ekle:  EnvironmentFile=/etc/portfolio/auth.env");
        Console.WriteLine("3) Servisi yeniden başlat, /admin adresinden giriş yap.");
        Console.WriteLine();
        Console.WriteLine("⛔ Bu env dosyası ASLA git'e girmez (.gitignore: *.env).");
        Console.WriteLine();
        Console.WriteLine("🐛 TUZAK: bu dosyayı shell'de `source` / `. dosya` ile YÜKLEME —");
        Console.WriteLine("   hash içindeki $210000 ve $salt parçaları değişken sanılıp genişletilir,");
        Console.WriteLine("   şifre sessizce yanlış olur. systemd `EnvironmentFile=` genişletme YAPMAZ,");
        Console.WriteLine("   doğru kullanım odur. Elle test edeceksen:");
        Console.WriteLine("   export Auth__PasswordHash=\"$(grep '^Auth__PasswordHash=' dosya | cut -d= -f2-)\"");
        return 0;
    }

    /// <summary>Şifreyi ekrana yazmadan okur (yankı kapalı).</summary>
    private static string SirOku(string istem)
    {
        Console.Write(istem);
        var sb = new System.Text.StringBuilder();
        try
        {
            ConsoleKeyInfo tus;
            while ((tus = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (tus.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
                }
                else if (!char.IsControl(tus.KeyChar))
                {
                    sb.Append(tus.KeyChar); Console.Write('*');
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Yönlendirilmiş girdi (boru/dosya) → ReadKey çalışmaz, satır olarak oku
            return Console.ReadLine() ?? "";
        }
        Console.WriteLine();
        return sb.ToString();
    }

    private static string? ArgDegeri(string[] args, string ad)
    {
        var i = Array.IndexOf(args, ad);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
