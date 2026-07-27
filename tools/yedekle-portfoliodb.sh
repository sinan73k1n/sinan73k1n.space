#!/usr/bin/env bash
# tools/yedekle-portfoliodb.sh — portfoliodb'yi yedekler ve YEDEĞİ DOĞRULAR.
#
# SUNUCUDA çalışır (Mac'te değil). Kurulum: ~/bin/yedekle-portfoliodb.sh + cron.
#
# ── Neden bu üç karar ─────────────────────────────────────────────────────────
# 1) `BACKUP DATABASE` kullanılır, `.mdf` KOPYALANMAZ. Çalışan bir veri dosyasını
#    kopyalamak tutarsız yedek verir (sunucuda daha önce yaşandı).
# 2) Parola HİÇBİR YERDE durmaz: sqlcmd konteynerin İÇİNDE çalışır ve parolayı
#    konteynerin kendi $SA_PASSWORD değişkeninden okur. Script'te, cron'da,
#    ortam dosyasında parola yok. (`portfolio_app` yedek alamaz — datareader/
#    writer/ddladmin yetmez, `db_backupoperator` gerekirdi. Ona yeni yetki vermek
#    yerine sa kullanıldı: servis kullanıcısı zaten docker grubunda, yani o yetkiye fiilen
#    sahip; ek rol görünürde daha dar ama gerçekte hiçbir şey kazandırmıyordu.)
# 3) Yedek alındıktan sonra `RESTORE VERIFYONLY` ile OKUNABİLİRLİĞİ SINANIR.
#    Sunucudaki bilinen en büyük yedek riski "hiç geri yüklenmemiş yedek"ti;
#    doğrulama, bozuk bir yedeği aylar sonra değil aynı gün yakalar.
#
# ⛔ COMPRESSION YOK: mssql 2022 **Express** sürümü yedek sıkıştırmayı desteklemez.
set -euo pipefail

DB="portfoliodb"
KONTEYNER="mssql"
KLASOR_KONTEYNER="/var/opt/mssql/backups"   # host'ta /mnt/backups/mssql olarak bağlı
KLASOR_HOST="/mnt/backups/mssql"
SAKLANACAK=14                                # kaç günlük yedek tutulsun

zaman="$(date +%Y%m%d_%H%M%S)"
dosya="${DB}_${zaman}.bak"                   # mevcut adlandırma deseniyle aynı

sql() {
    # -b: SQL hatasında sqlcmd sıfırdan farklı kod döndürür → set -e yakalar.
    docker exec -i "$KONTEYNER" bash -c \
        "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$SA_PASSWORD\" -C -b -Q \"$1\""
}

echo "▶ 1/4  Yedek alınıyor: $dosya"
# STATS verilmez: geçerli aralık 1-100, ve ilerleme çıktısına zaten ihtiyaç yok.
sql "BACKUP DATABASE [$DB] TO DISK='$KLASOR_KONTEYNER/$dosya' WITH INIT, CHECKSUM;"

echo "▶ 2/4  Yedek doğrulanıyor (RESTORE VERIFYONLY)"
# Doğrulama BAŞARISIZ olursa script burada durur (set -e) ve bozuk yedek
# `_latest` olarak işaretlenmez — yani en son "iyi bilinen" yedek korunur.
sql "RESTORE VERIFYONLY FROM DISK='$KLASOR_KONTEYNER/$dosya' WITH CHECKSUM;"

echo "▶ 3/4  _latest güncelleniyor"
# Yalnız doğrulanmış yedek `_latest` olur. Geri yükleme her zaman bunu arar.
cp "$KLASOR_HOST/$dosya" "$KLASOR_HOST/${DB}_latest.bak"

echo "▶ 4/4  Eskiler temizleniyor (son $SAKLANACAK yedek kalır)"
# `_latest.bak` desene uymadığı için bu listeye GİRMEZ, silinmez.
mapfile -t eskiler < <(ls -1t "$KLASOR_HOST/${DB}"_2*.bak 2>/dev/null | tail -n +$((SAKLANACAK + 1)))
if [ ${#eskiler[@]} -gt 0 ]; then
    printf '  siliniyor: %s\n' "${eskiler[@]##*/}"
    rm -f "${eskiler[@]}"
else
    echo "  silinecek yok"
fi

echo "✔ Bitti: $KLASOR_HOST/$dosya ($(du -h "$KLASOR_HOST/$dosya" | cut -f1)), doğrulandı."
