using Portfolio.Entities.Content;

namespace Portfolio.Entities;

/// <summary>
/// Sitenin İLK açılış içeriği — boş bir veritabanını/dosyayı doldurmak için.
/// <para>
/// ⚠️ Bu metinlerin hepsi <b>bilinçli yer tutucudur</b> (karar: Sinan, 2026-07-26).
/// Gerçek içerik admin panelinden girilir; burası yeniden yazılmaz.
/// </para>
/// <para>
/// Kaynağı <c>design_handoff_portfolio/source-dc/site-data.js → SEED</c>.
/// Elle kopyalanmadı: önce JSON'a, sonra bu dosyaya <b>programla</b> üretildi —
/// üç dilde 31 kopya anahtarı elle taşınırken tek karakter sapması bile sessiz
/// bir içerik hatası olurdu.
/// </para>
/// <para>
/// <b>Neden JSON dosyası değil de kod</b> (karar: 2026-07-27): tohum ömründe
/// <b>bir kez</b> çalışır — boş DB'yi doldurur, sonra hiç okunmaz. Dosya olarak
/// durduğunda publish çıktısında taşınması ve "dosya yok" diye bir başlatma hata
/// sınıfı gerekiyordu. Kodda derleme zamanında denetleniyor ve o hata sınıfı yok.
/// ⛔ Admin'deki JSON dışa/içe aktarma bundan AYRI ve duruyor — o yedekleme özelliği.
/// </para>
/// </summary>
public static class SeedIcerik
{
    /// <summary>Her çağrıda YENİ nesne döndürür — çağıran üzerinde değişiklik yapabilsin.</summary>
    public static SiteContent Olustur() => new()
    {
        Meta = new SiteMeta
        {
            Name = "Sinan Tekin",
            Handle = "sinan.tekin",
            Mail = "hello@sinantekin.dev",
            Github = "https://github.com/sinan73k1n",
            GithubUser = "sinan73k1n",
            Play = "https://play.google.com/store/apps/dev?id=5968286876093646143",
        },

        I18n = new()
        {
            ["tr"] = new()
            {
                ["navAbout"] = "Hakkımda",
                ["navStack"] = "Teknolojiler",
                ["navGames"] = "Oyunlar",
                ["navDemos"] = "Demolar",
                ["navGithub"] = "GitHub",
                ["heroTag"] = "yeni projelere açık",
                ["heroLine2"] = "yazılım geliştirici",
                ["heroLead"] = "Oyun geliştiriyorum, yönetim panelleri kuruyorum, arayüzleri kendim yazıyorum. Aşağıdaki her şey gerçekten çalışan işler.",
                ["heroBtn1"] = "Demoları çalıştır",
                ["heroBtn2"] = "Kodlara bak",
                ["scroll"] = "aşağı kaydır",
                ["marquee"] = "unity  ·  c#  ·  javascript  ·  php  ·  mysql  ·  google play  ·  admin panel  ·  forum  ·  api  ·  demo  ·",
                ["aboutTitle"] = "Fikirden yayına, tek başına.",
                ["aboutBig"] = "Ekranda görüneni de arkada çalışanı da ben yazıyorum — tasarım, kod, yayın döngüsü aynı elde.",
                ["aboutSmall"] = "Oyun tarafında Unity ile geliştirip Google Play'de yayınlıyorum; mağaza, sürüm ve oyuncu geri bildirimi süreçlerini kendim yönetiyorum. Web tarafında yönetim panelleri, forum ve etkileşim yoğun arayüzler kuruyorum. Bu sitedeki demolar ekran görüntüsü değil, tarayıcıda gerçekten açılan sürümler.",
                ["stackTitle"] = "Çalıştığım teknolojiler",
                ["stackLead"] = "Liste admin panelinden güncellenir.",
                ["stackSlot"] = "yeni teknoloji",
                ["gamesTitle"] = "Google Play oyunları",
                ["demosTitle"] = "Çalışan demolar",
                ["demosLead"] = "Satırların üstüne gel, önizleme değişir. Tıkla, demo tam ekran açılır.",
                ["demosHint"] = "önizleme yer tutucudur — gerçek demo tam ekranda çalışır",
                ["demosSlot"] = "boş demo alanı — admin panelinden eklenecek",
                ["openDemo"] = "Tam ekran aç",
                ["close"] = "Kapat",
                ["githubTitle"] = "Kaynak kodlar ve depolar",
                ["githubLead"] = "Depoların ön izlemesi. Takip etmek isteyen doğrudan profilden ilerleyebilir.",
                ["contactTitle"] = "İletişim",
                ["contactHead"] = "Bir işi konuşalım mı?",
                ["footerNote"] = "tasarım ve kod bana ait",
                ["imgSlot"] = "oyun görseli buraya",
            },
            ["en"] = new()
            {
                ["navAbout"] = "About",
                ["navStack"] = "Stack",
                ["navGames"] = "Games",
                ["navDemos"] = "Demos",
                ["navGithub"] = "GitHub",
                ["heroTag"] = "open to new projects",
                ["heroLine2"] = "software developer",
                ["heroLead"] = "I build games, ship admin panels and write the interfaces myself. Everything below is work that actually runs.",
                ["heroBtn1"] = "Run the demos",
                ["heroBtn2"] = "Read the code",
                ["scroll"] = "scroll down",
                ["marquee"] = "unity  ·  c#  ·  javascript  ·  php  ·  mysql  ·  google play  ·  admin panel  ·  forum  ·  api  ·  demo  ·",
                ["aboutTitle"] = "From idea to release, solo.",
                ["aboutBig"] = "I write what you see on screen and what runs behind it — design, code and shipping in one pair of hands.",
                ["aboutSmall"] = "On the game side I build with Unity and publish on Google Play, handling store presence, releases and player feedback myself. On the web side I build admin panels, forums and interaction-heavy interfaces. The demos here aren't screenshots — they open and run in your browser.",
                ["stackTitle"] = "Technologies I work with",
                ["stackLead"] = "This list is maintained from the admin panel.",
                ["stackSlot"] = "new technology",
                ["gamesTitle"] = "Google Play games",
                ["demosTitle"] = "Running demos",
                ["demosLead"] = "Hover a row and the preview follows. Click to open the demo full screen.",
                ["demosHint"] = "the preview is a placeholder — the real demo runs full screen",
                ["demosSlot"] = "empty demo slot — added from the admin panel",
                ["openDemo"] = "Open full screen",
                ["close"] = "Close",
                ["githubTitle"] = "Source code and repos",
                ["githubLead"] = "A preview of the repositories. Anyone who wants to follow along can continue from the profile.",
                ["contactTitle"] = "Contact",
                ["contactHead"] = "Got something to build?",
                ["footerNote"] = "design and code by me",
                ["imgSlot"] = "game artwork here",
            },
            ["ru"] = new()
            {
                ["navAbout"] = "Обо мне",
                ["navStack"] = "Технологии",
                ["navGames"] = "Игры",
                ["navDemos"] = "Демо",
                ["navGithub"] = "GitHub",
                ["heroTag"] = "открыт к новым проектам",
                ["heroLine2"] = "разработчик ПО",
                ["heroLead"] = "Делаю игры, собираю админ-панели и сам пишу интерфейсы. Всё ниже — проекты, которые реально работают.",
                ["heroBtn1"] = "Запустить демо",
                ["heroBtn2"] = "Смотреть код",
                ["scroll"] = "прокрутите вниз",
                ["marquee"] = "unity  ·  c#  ·  javascript  ·  php  ·  mysql  ·  google play  ·  админ-панель  ·  форум  ·  api  ·  демо  ·",
                ["aboutTitle"] = "От идеи до релиза — один.",
                ["aboutBig"] = "Пишу и то, что видно на экране, и то, что работает за ним: дизайн, код и релиз в одних руках.",
                ["aboutSmall"] = "В играх работаю на Unity и публикую в Google Play, сам занимаюсь магазином, релизами и обратной связью игроков. В вебе строю админ-панели, форумы и интерфейсы с плотным взаимодействием. Демо здесь — не скриншоты, они действительно запускаются в браузере.",
                ["stackTitle"] = "Технологии, с которыми работаю",
                ["stackLead"] = "Список обновляется из админ-панели.",
                ["stackSlot"] = "новая технология",
                ["gamesTitle"] = "Игры в Google Play",
                ["demosTitle"] = "Рабочие демо",
                ["demosLead"] = "Наведите на строку — предпросмотр меняется. Нажмите, чтобы открыть демо на весь экран.",
                ["demosHint"] = "предпросмотр — заглушка, само демо работает на весь экран",
                ["demosSlot"] = "пустой слот демо — добавится из админ-панели",
                ["openDemo"] = "Открыть на весь экран",
                ["close"] = "Закрыть",
                ["githubTitle"] = "Исходный код и репозитории",
                ["githubLead"] = "Предпросмотр репозиториев. Кто хочет следить — переходите в профиль.",
                ["contactTitle"] = "Контакты",
                ["contactHead"] = "Обсудим проект?",
                ["footerNote"] = "дизайн и код мои",
                ["imgSlot"] = "изображение игры",
            },
        },

        Roles = new()
        {
            ["tr"] = new() { "Unity ile mobil oyunlar", "yönetim panelleri & backend", "çalışan web demoları" },
            ["en"] = new() { "mobile games with Unity", "admin panels & backend", "web demos that actually run" },
            ["ru"] = new() { "мобильные игры на Unity", "админ-панели и бэкенд", "рабочие веб-демо" },
        },

        Logs = new()
        {
            "$ sinan --whoami",
            "→ unity · c# · javascript · php · mysql",
            "$ ship --target play-store",
            "✓ build passed   ✓ release live",
            "$ serve ./demo/*",
            "→ ServerAdminPanel  ready",
            "→ ForumEtkilesim    ready",
            "→ GameLauncher      ready",
            "$ open portfolio ▸",
        },

        Facts = new()
        {
            new() { Value = 3, Label = new() { Tr = "Google Play'de yayında oyun", En = "games live on Google Play", Ru = "игр в Google Play" } },
            new() { Value = 3, Label = new() { Tr = "yayınlanmış çalışan demo", En = "published running demos", Ru = "опубликованных рабочих демо" } },
            new() { Value = 4, Label = new() { Tr = "aktif GitHub deposu", En = "active GitHub repositories", Ru = "активных репозиториев GitHub" } },
        },

        Techs = new()
        {
            new() { Name = "Unity", Note = "oyun motoru" },
            new() { Name = "C#", Note = "gameplay · backend" },
            new() { Name = "JavaScript", Note = "arayüz · demo" },
            new() { Name = "PHP", Note = "panel · api" },
            new() { Name = "MySQL", Note = "veri" },
            new() { Name = "HTML / CSS", Note = "arayüz" },
            new() { Name = "Git", Note = "sürüm" },
        },

        Games = new()
        {
            new()
            {
                Name = "Oyun 01",
                Url = "",
                Image = "",
                Cover = "violet",
                Desc = new() { Tr = "Başlık, açıklama ve görsel admin panelinden gelecek.", En = "Title, description and artwork come from the admin panel.", Ru = "Название, описание и обложка задаются в админ-панели." },
            },
            new()
            {
                Name = "Oyun 02",
                Url = "",
                Image = "",
                Cover = "cyan",
                Desc = new() { Tr = "Google Play'de yayında olan projelerden biri.", En = "One of the projects live on Google Play.", Ru = "Один из проектов, опубликованных в Google Play." },
            },
            new()
            {
                Name = "Oyun 03",
                Url = "",
                Image = "",
                Cover = "magenta",
                Desc = new() { Tr = "Yeni oyunlar bu alana eklenecek.", En = "New games get added here.", Ru = "Новые игры добавляются здесь." },
            },
        },

        Demos = new()
        {
            new()
            {
                Path = "demo/ServerAdminPanel",
                Name = "Server Admin Panel",
                Tags = new() { "HTML", "JS", "Dashboard" },
                Html = "",
                Desc = new() { Tr = "Sunucu yönetimi, kullanıcı listesi ve log ekranlarını içeren panel demosu.", En = "Panel demo with server management, user list and log screens.", Ru = "Демо панели: управление сервером, список пользователей и логи." },
            },
            new()
            {
                Path = "demo/ForumEtkilesim",
                Name = "Forum Etkileşim",
                Tags = new() { "HTML", "JS", "Community" },
                Html = "",
                Desc = new() { Tr = "Konu akışı, oylama ve bildirim etkileşimleri olan forum arayüzü.", En = "Forum interface with topic feed, voting and notifications.", Ru = "Интерфейс форума: лента тем, голосование и уведомления." },
            },
            new()
            {
                Path = "demo/GameLauncher",
                Name = "Game Launcher",
                Tags = new() { "HTML", "JS", "UI" },
                Html = "",
                Desc = new() { Tr = "Oyun kütüphanesi ve güncelleme akışı olan masaüstü tarzı arayüz.", En = "Desktop-style interface with a game library and update flow.", Ru = "Интерфейс в стиле десктопа: библиотека игр и обновления." },
            },
        },

        Repos = new()
        {
            new()
            {
                Name = "server-admin-panel",
                Lang = "PHP",
                Updated = "2026",
                Url = "",
                Desc = new() { Tr = "PHP + MySQL tabanlı sunucu yönetim paneli.", En = "Server admin panel built on PHP + MySQL.", Ru = "Панель управления сервером на PHP + MySQL." },
            },
            new()
            {
                Name = "unity-game-tools",
                Lang = "C#",
                Updated = "2026",
                Url = "",
                Desc = new() { Tr = "Unity projelerinde tekrar kullandığım editör scriptleri.", En = "Editor scripts I reuse across Unity projects.", Ru = "Скрипты редактора, которые я переиспользую в Unity." },
            },
            new()
            {
                Name = "forum-etkilesim",
                Lang = "JavaScript",
                Updated = "2026",
                Url = "",
                Desc = new() { Tr = "Forum etkileşim arayüzü prototipi ve demoları.", En = "Forum interaction interface prototype and demos.", Ru = "Прототип интерфейса форума и демо." },
            },
            new()
            {
                Name = "portfolio",
                Lang = "HTML",
                Updated = "2026",
                Url = "",
                Desc = new() { Tr = "Bu sitenin kaynak kodu ve demo dosyaları.", En = "Source code of this site and its demo files.", Ru = "Исходный код сайта и файлы демо." },
            },
        },

        Covers = new()
        {
            ["violet"] = new() { "oklch(0.42 0.15 300)", "oklch(0.26 0.09 280)" },
            ["cyan"] = new() { "oklch(0.4 0.13 210)", "oklch(0.25 0.08 250)" },
            ["magenta"] = new() { "oklch(0.44 0.14 340)", "oklch(0.26 0.08 300)" },
            ["mint"] = new() { "oklch(0.42 0.13 165)", "oklch(0.25 0.07 200)" },
            ["amber"] = new() { "oklch(0.46 0.13 72)", "oklch(0.27 0.07 50)" },
        },
    };
}
