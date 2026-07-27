# Bu klasörü Claude Code ile aç

Önce `README.md` — tasarımın tam spesifikasyonu (tokenlar, bölümler, animasyonlar,
içerik şeması, güvenlik notları, önerilen sıra) orada.

Önemli kurallar:

0. Tasarımı görmek için `live-html/index.html` ve `live-html/admin.html` dosyalarını tarayıcıda aç — sohbette gösterilen çalışan sayfaların birebir kopyası. Her ikisi de tek dosyadır, ek dosya gerekmez.
1. `live-html/*` ve `source-dc/*` dosyaları **tasarım referansıdır**, production kodu değil.
   Hedef kod tabanının kendi framework'ünde yeniden üret; bu HTML'i olduğu gibi taşımayın.
2. "Burası nasıl görünüyordu?" sorusunu dosyadan yanıtla — tahmin etme:
   ```bash
   npx playwright install chromium
   npx playwright screenshot --full-page --viewport-size=1440,900 "live-html/index.html" ref-home.png
   npx playwright screenshot --viewport-size=1440,900 "live-html/admin.html" ref-admin.png
   ```
   Bölüm çapaları: `#top #about #stack #games #demos #github`.
   Kendi çıktını aynı viewport'ta çekip yan yana karşılaştır.
3. `source-dc/site-data.js` içindeki `SEED` **tek doğruluk kaynağıdır** — içerik alan adlarını
   ve gerçek metinleri oradan al, yeniden yazma.
4. `source-dc/support.js` prototip çalışma zamanıdır; okuma/kopyalama gerekmez.
5. Değiştirilmemesi gereken bilinçli kararlar: kare/ızgara arka plan yok; arka plan hareketi
   düşük kontrast ve dikkat çekmeyecek; palet tek yön (mor + camgöbeği, indigo taban);
   demolar iframe içinde çalışır (ekran görüntüsü değil).
