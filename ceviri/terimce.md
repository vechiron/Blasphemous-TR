# Terimce — Blasphemous-TR

Tekrar eden her terim burada **bir kez** kararlaştırılır; çeviri buradan beslenir. Yeni terim
partide çıkarsa önce buraya yazılır, sonra kullanılır. Değiştirmek gerekirse: buradaki satır +
`grep -rn "eski" ceviri/` ile bütün kopyalar aynı commit'te.

## Üslup

- **Hitap «sen».** Oyunun dili arkaik ve dinsel: İngilizcedeki *thy, thou, cometh* Türkçede
  zamir değil **söz seçimiyle** verilir (*şafak söküyor* değil *şafak sökmekte*; *mahşer, kıyam,
  lütuf, vebal, ıstırap*). Arayüz sade ve kısa: düğme emir kipi (*Kuşan, Çıkar*), başlık ad.
- **Özel ad çevrilmez** (kişi, Albero, Jondo, Cvstodia, Mea Culpa, Amanecida); **unvan ve yer
  adının anlamlı kısmı çevrilir** (*Wasteland of the Buried Churches* -> *Gömülü Kiliseler
  Çoraklığı*). «X of the Y» kalıbı -> «Y'nin X'i» ya da «Y X».
- **Kesme:** kişi, varlık, yer adına gelen ek kesmeyle (*Tövbekâr'ın, Mucize'nin, Düş'ün,
  Cvstodia'da*). Oyun terimi (eşya, kaynak türü) kesmesiz (*Kefaret Gözyaşlarını, Dualarını*).
- **Büyük harf:** İngilizcesi büyük harfle yazılmışsa (*ROSARY BEADS*) Türkçesi de büyük, Türkçe
  kuralla (*TESPİH TANELERİ*). İngilizcesi normal harfse normal yazılır — başlık kutularında oyun
  kendisi büyütür (yükleyici i -> İ yapar; `proje/dokuman/metin-yolu.md` §4).
- **Font:** yalnız ASCII + Latin-1 (*â î û* dahil) + *ğĞşŞıİ*. `« » “ ” ’ … — –` YOK ->
  düz tırnak `"` `'`, üç nokta `...`, tire `-`. Sınama (B5) denetler.
- **Biçim öğeleri birebir:** `{[yer_tutucu]}`, `<color=#..>..</color>`, `[ACT:..]`, `[ICON:..]`,
  satır sonu sayısı, baştaki/sondaki boşluk (B6-B8; dosya yazma aracı sondaki boşluğu kırpar ->
  B8 yakalar, `sed` ile eklenir). Parça birleştiren metinlerde (sonuna oyun ad
  ya da sayı ekliyor: *Do you want to give*) Türkçe cümle **sona eklenen parçayla** okunur kurulur:
  *Şunu vermek ister misin:*.
- **Selam kalıbı** *<Sıfat> be the heart, Penitent One* (es. *<Sıfat> corazón sea*; 8 yer,
  `parti.py --ara "be the heart"`) -> *Kalbin <sıfat> olsun, Tövbekâr.* Sıfat **İspanyolcadan**
  (İngilizce aynı sıfata iki karşılık vermiş): Compungido -> pişman · Pesaroso -> mahzun ·
  Misericordioso -> merhametli · Contrito y humillado -> kırık ve pişman (Mezmur 51) · Agraciado
  -> lütuflu. *May your heart be free of sin* -> *Kalbin günahsız olsun.*
- **Hitap** *Penitent One in X* (16 yer, `--ara "Penitent One in"`) -> *(ey) <sıfat> Tövbekâr*:
  in silence -> suskun · in guilt -> suçlu · in piety -> dindar · in good fortune -> kutlu ·
  full of clemency (es. en la clemencia, ST03) -> bağışlayıcı · (oh) virtuous ~ (es. en la Virtud;
  ST08, ST201) -> (ey) erdemli Tövbekâr · in charity -> hayırsever · in the righteous path ->
  doğru yoldaki · merciful (es. misericorde, ST203) -> merhametli · Go fearlessly (es. en el
  arrojo, ST202) -> ey gözü pek Tövbekâr · in sleeplessness (es. en el desvelo, ST12) -> uykusuz.
- **İngilizce çeviri hatası** İspanyolcayla düzeltilir, commit'e yazılır (`ispanyolca.py <Kat>`):
  ST07 *promised land* (es. *comprometido lugar*) -> çetin bir diyar.
- **Sayıdan sonra tekil:** *{[beads]} bead / beads* -> ikisi de *{[beads]} tespih tanesi*.
- **Tuş simgeli cümle:** *Press [ACT:X] to Y* -> *Y için [ACT:X] tuşuna bas*; birden çok simge ->
  *tuşlarına*; *Hold* -> *tuşunu basılı tut*. Simgeye doğrudan ek gelmez (simge klavye ya da kol olabilir).
- **Ürün adı çevrilmez:** oyunun ve DLC'lerin adı (*Blasphemous, The Stir of Dawn, Strife and Ruin,
  Wounds of Eventide*) Steam'deki adla eşleşsin diye İngilizce kalır; aynı sözler hikâye metninde
  gündelik anlamda geçerse çevrilir (*the wounds of eventide*, QI301).
- İmlâ: şapka (*hâlâ, kâfir, ebedî*), *ıstırap -> ıstırabı*, *kalp -> kalbi*.

## Terim tablosu

`ceviri.py` B9 bu tabloyu okur: İngilizce terim metinde (büyük harfiyle, kelime başından) geçiyorsa
Türkçede «B9 kökü» geçmeli (harf duyarsız); geçmezse **uyarı** (hata değil — terim kimi cümlede
gündelik anlamda geçebilir). Kök, ekli ve yumuşamış biçimleri de yakalayacak kadar kısa seçilir.

| İngilizce | Türkçe | B9 kökü | not |
|---|---|---|---|
| Penitent One | Tövbekâr | tövbekâr | unvan; «The» düşer; hitapta «Tövbekâr,» |
| Miracle | Mucize | mucize | kişileştirilmiş güç, büyük harf |
| Ancient Miracle | Kadim Mucize | kadim mucize | es. «Milagro Antiguo» (ST102, ST01); Ancient Processions emsali |
| Grievous Miracle | Acılı Mucize | acılı mucize | |
| Sorrowful Miracle | Kederli Mucize | kederli mucize | |
| Dream | Düş | düş | «the Dream» (öbür dünya) büyük harfle; the other side of the ~ -> Düş'ün öbür yanı (16 yer) |
| High Will | Yüce İrade | yüce irade | çoğul Yüce İradeler |
| Tears of Atonement | Kefaret Gözyaşları | gözyaş | para birimi; kısa «Tears» -> Gözyaşı |
| Guilt | Vebal | vebal | ölüm mekaniği; Guilt Fragment -> Vebal Parçası; gündelik guilt -> suçluluk |
| Fervour | Şevk | şevk | büyü gücü çubuğu |
| Bile Flask | Safra Şişesi | safra şişe | |
| Bile Vessel | Safra Kabı | safra ka | kap -> kabı, kaplar |
| Prayer | Dua | dua | |
| Relic | Emanet | emanet | kutsal emanet; Reliquary -> Emanet Sandığı |
| Rosary Bead | Tespih Tanesi | tespih tane | |
| Rosary Knot | Tespih Düğümü | tespih düğüm | tane yuvası |
| Rosary | Tespih | tespih | |
| Rosary Mystery | Tespih Sırrı | tespih sır | Katolik «Tespih Sırları»; Soledad (ST21) düğüm bağlayıp yuva açar; mystery (genel) -> gizem |
| Mea Culpa | Mea Culpa | mea culpa | kılıç; çevrilmez |
| Sword Heart | Kılıç Kalbi | kılıç kal | Mea Culpa Hearts -> Mea Culpa Kalpleri |
| Prie Dieu | Dua Rahlesi | dua rahle | kayıt noktası |
| Prie-Dieu | Dua Rahlesi | dua rahle | tireli yazımı da var |
| Confessor | Günah Çıkaran | günah çıkar | Vebal'i silen heykel |
| Tithe | Ondalık | ondalık | |
| Apodictic Heart | Tartışılmaz Kalp | tartışılmaz kal | ~ of Mea Culpa -> Mea Culpa'nın Tartışılmaz Kalbi |
| Pilgrimage | Hac Yolu | hac | ana menü «Continue»; Pilgrim -> Hacı |
| Teleport | Yolculuk | yolculuk | diz çökme menüsü; UI_Map TRAVEL ile aynı |
| True Torment | Gerçek Eziyet | gerçek eziyet | NG+ zorluk modu |
| Sacred Sorrows | Kutsal Kederler | kutsal keder | boss yarışı modu; course -> yol |
| Life Orb | Can Küresi | küre | |
| Unwavering Faith | Sarsılmaz İman | sarsılmaz iman | |
| Bleeding Heart | Kanayan Kalp | kanayan kal | |
| Altar of Penitence | Tövbe Sunağı | tövbe sunağ | |
| Penitence | Tövbe | tövbe | zorluk modu da (UI_Penitences) |
| Penance | Çile | çile | |
| Humiliation | Alçalış | alçalış | es. «Humillación» (ST01): ~ of Penance -> Çilenin ~ı; Three ~s -> Üç Alçalış |
| Atonement | Kefaret | kefaret | |
| Quest Item | Görev Eşyası | görev eşya | |
| Collectible | Koleksiyon Parçası | koleksiyon | kemikler |
| Ossuary | Kemiklik | kemikli | yumuşar: Kemikliğin; Lady of the ~ -> Kemikliğin Hatunu (Isidora, QI201) |
| Aspect | Kostüm | kostüm | ana menüdeki «Kostüm Seçimi» ile tutarlı |
| Cvstodia | Cvstodia | cvstodia | |
| Amanecida | Amanecida | amanecida | çoğul Amanecida'lar |
| His Holiness | Hazretleri | hazret | His Holiness Escribar -> Escribar Hazretleri; kesmesiz: Hazretlerinin. «His» olmadan «(the) Holiness» (es. «la Santidad»; ST17, ST09) -> Kutsallık: eşyayı kutsayan ST14 Lotl, «sahte kutsallık» verilmiş tuz azizi |
| Our Lady | Meryem Ana | meryem ana | Katolik «Our Lady»; İSTİSNA ST09: rahibe hanımı Socorro'yu kastediyor (es. «Nuestra Señora») -> Hatunumuz (B9 uyarısı geçerli) |
| Holy Mother Church | Kutsal Ana Kilise | ana kilise | |
| Mother of Mothers | Anaların Anası | anaların ana | yer adı da |
| Silent Sorrow | Sessiz Keder | sessiz keder | Brotherhood of the ~ -> Sessiz Keder Kardeşliği |
| Anointed Legion | Meshedilmiş Lejyon | lejyon | |
| Scribe | Kâtip | kâti | yumuşar: Kâtibin (QI70) |
| Twisted One | Burulmuş Olan | burulmuş | |
| Visage | Çehre | çehre | Holy ~ Kutsal Çehre · Golden ~ Altın Çehre |
| Charred Visage | Yanık Çehre | yanık çehre | Our Lady of the ~ -> Yanık Çehreli Meryem Ana |
| Inverted Verses | Ters Dizeler | ters dize | Chalice of the ~ -> Ters Dizeler Kadehi |
| Three Words | Üç Söz | üç söz | Word (dinsel) -> Söz |
| Negated Words | Yadsınmış Sözler | yadsınmış söz | |
| Denied Words | Yadsınmış Sözler | yadsınmış söz | es. «Palabras Negadas» = Negated Words; Library of the ~ -> Yadsınmış Sözler Kütüphanesi (ST11) |
| Holy Prohibitions | Kutsal Yasaklar | kutsal yasak | |
| Three Calvaries | Üç Çarmıh | üç çarmıh | |
| Kissers of Wounds | Yara Öpenler | yara öpen | |
| Wrapped Agony | Sarmalanmış Istırap | sarmalanmış ıstıra | |
| Perpetual Agony | Ebedî Istırap | ebedî ıstıra | |
| Eternal Processions | Ebedî Alaylar | ebedî alay | es. «Procesiones Eternas» |
| Ancient Processions | Kadim Alaylar | kadim alay | es. «Procesiones Antiguas» (Eternal'dan AYRI); Path of the ~ -> Kadim Alaylar Yolu (ST204) |
| Old Processions | Kadim Alaylar | kadim alay | = Ancient Processions (es. aynı; ST03, ST01 küçük harfle) |
| Suburbs | Varoşlar | varoş | bölge (Map/D01) |
| Holy Line | Kutsal Hat | kutsal hat | |
| Albero | Albero | albero | |
| Buried Churches | Gömülü Kiliseler | gömülü kilise | Wasteland of the ~ -> Gömülü Kiliseler Çoraklığı |
| Mercy Dreams | Merhamet Düşleri | merhamet düş | |
| Desecrated Cistern | Kirletilmiş Sarnıç | kirletilmiş sarnıç | |
| Petrous | Petrous | petrous | |
| Enclosed Convent | Kapalı Manastır | kapalı manastır | |
| Olive Trees Wither | Zeytinlerin Kuruduğu Yer | zeytinlerin kurudu | kısa: harita kutusu |
| Graveyard of the Peaks | Doruklar Mezarlığı | doruklar mezarlığ | |
| Bronze Frontier | Tunç Sınır | tunç sınır | |
| Endless Dusk | Bitmeyen Alacakaranlık | bitmeyen alacakaranlık | Mountains of the ~ -> ~ Dağları |
| Jondo | Jondo | jondo | |
| Grievance Ascends | Feryat Yükselir | feryat yüksel | |
| Silent Steps | Sessiz Adımlar | sessiz adım | Patio of the ~ -> ~ Avlusu |
| Tears of the Sea | Denizin Gözyaşları | denizin | All the ~ -> Denizin Bütün Gözyaşları |
| Low Creeds | Aşağı İnançlar | aşağı inanç | |
| Sleeping Canvases | Uyuyan Tuvaller | uyuyan tuval | |
| Archcathedral | Başkatedral | başkatedral | ~ Rooftops -> ~ Çatıları |
| Deambulatory | Revak | revak | ~ of His Holiness -> Hazretlerinin Revakı |
| Ferrous Tree | Demir Ağaç | demir ağaç | |
| Hall of the Dawning | Şafak Salonu | şafak salon | |
| Echoes of Salt | Tuzun Yankıları | tuzun yankı | |
| Mourning and Havoc | Matem ve Yıkım | matem ve yıkım | |
| Resting Place of the Sister | Kız Kardeşin Son Durağı | son dura | kız kardeş = Esdras'ın kardeşi Perpetva |
| Warden | Bekçi | bekçi | Warden of the Silent Sorrow -> Sessiz Kederin Bekçisi |
| Parry | Savuşturma | savuştur | teknik; UI_Controls PARRY -> Savuştur |
| Counter | Karşı Saldırı | karşı saldırı | Counterattack da |
| Retribution | Misilleme | misilleme | savuşturma sonrası zamanlı vuruş |
| Execution | İnfaz | infaz | Execute -> infaz et |
| Stun | Sersemletme | sersem | sersemlemiş düşman |
| Dodge | Kaçınma | kaçın | Dash da; UI_Controls DASH -> Kaçın |
| Air Impulse | Hava Atılımı | hava atılım | lunge -> atılım |
| Blood Penitence | Kan Tövbesi | kan tövbe | teknik; Blood Penance (zorluk) -> Kan Çilesi |
| Biliary Flask | Safra Şişesi | safra şişe | Bile Flask ile aynı |
| Mea Culpa Shrine | Mea Culpa Sunağı | suna | altar of Mea Culpa da |
| Children of Moonlight | Ay Işığının Çocukları | ay ışığı | kafesli kerubiler |
| Petrified Bell | Taşlaşmış Çan | taşlaşmış çan | görev eşyası QI106 |
| Righteous Riposte | Haklı Misilleme | misilleme | AC28, Sword/HE11; Retribution ile aynı hareket varsayıldı (ölçülmedi) |
| Offering | Adak | adak | GENERIC «Adak sunulsun mu?» |
| Boss | boss | boss | oyun jargonu; ek kesmeyle: boss'ları |
| Challenge | Sınama | sınama | Miriam's Challenges -> Miriam'ın Sınamaları; arena (AC15) |
| Key Master | Anahtar Ustası | anahtar usta | Kütüphane girişindeki NPC (PATCH_4_0) |
| Turned Throne | Ters Taht | ters taht | Escribar'ın tahtı (5 terimde) |
| Punished | Cezalandırılmış | cezalandırıl | the Punished = Mucize'nin Orduları |
| Fraternal Master | Kardeşlik Üstadı | kardeşlik üstad | master (tarikat) -> üstat; followers -> müritler |
| Mother Superior | Başrahibe | başrahibe | |
| Our Father | Babamız | baba | iyelikli, kesmesiz (Babamızın); the Father -> Baba'nın; hitap «Father,»: rahibe Peder (RB203, QI75), Tanrı'ya Baba (QI70) |
| Verse | Dize | dize | First Verse -> Birinci Dize (Inverted Verses emsali) |
| Miasma | Miazma | miazma | |
| Mother Church | Ana Kilise | ana kilise | The Mother (kurum) da -> Ana Kilise'den |
| Pontiff | Papa | papa | Escribar; Papa'nın (kişi, Tövbekâr emsali); pontifical -> papalık; Holy See -> Kutsal Makam (kurum, kesmesiz) |
| Inquisitor | Engizisyoncu | engizisyon | unvan ad önünde: Engizisyoncu Ashgan |
| Genuflectors | Diz Çökenler | diz çöken | Order of the ~ -> Diz Çökenler Tarikatı (Redento); genuflecting pilgrim -> diz çöken hacı; Order (kurum) -> Tarikat, kesmesiz (Tarikata) |
| Barefoot | Yalınayak | yalınayak | First Barefoot Pilgrim -> İlk Yalınayak Hacı; Barefoot Rule -> Yalınayak Kuralı; First Pilgrim -> İlk Hacı |
| Dolphos | Dolphos | dolphos | Confessions from/of X. Part N -> X'in İtirafları. Bölüm N (Dolphos RB/QI, Tirso QI) |
| Attrition | Nedamet | nedamet | Holy Wound of ~ -> Nedametin Kutsal Yarası; Visage of ~ -> Nedamet Çehresi (ST00/ST01) |
| Contrition | Pişmanlık | pişmanl | Holy Wound of ~ -> Pişmanlığın Kutsal Yarası; Visage of ~ -> Pişmanlık Çehresi |
| Compunction | Vicdan Azabı | vicdan azab | Holy Wound of ~ -> Vicdan Azabının Kutsal Yarası; Visage of ~ -> Vicdan Azabı Çehresi |
| Eldest Brother | Ulu Kardeş | ulu kardeş | Kardeşliğin başı (QI58, ST01 DLG_0101) |
| Most Illustrious | Zatıâlileri | zatıâli | Escribar'a hitap (QI60, QI62); his ~ -> Zatıâlilerinin |
| Refuge | Sığınak | sığına | Mark of the First ~ -> Birinci Sığınağın İşareti (Socorro, QI02-04) |
| True Shrine | Hakiki Defin | hakiki defin | es. «Genuino Sepulto» = True Burial: Order of the ~ (ST04), AC30_NAME; Winch (es. «Torno», manastır dolabı) -> döner dolap |
| True Burial | Hakiki Defin | hakiki defin | Order of the ~ -> Hakiki Defin Tarikatı; Lvdovico «second, true burial» -> ikinci, hakiki defin; RB15 Genuine Sepulchre -> Hakiki Kabir |
| Affliction | Musibet | musibet | kavram, büyük harf (13 terim); Cradle of ~ -> Musibetin Beşiği; gündelik affliction -> ıstırap (PR11) |
| Lady of the Lamp | Fenerli Hatun | fenerli hatun | unvan «Lady» -> Hatun (Our Lady hariç: Meryem Ana); Pious Lady of the ~ -> ~'nin Dindar Hatunu |
| Lady of the Bandaged Sword | Sargılı Kılıçlı Hatun | sargılı kılıçlı hatun | Crisanta varsayıldı (ST12 «my bandaged sword»; ölçülmedi) |
| Traitor | Hain | hain | the Traitor (Stir of Dawn, QI201-204); unvan, kesmeli: Hain'in |
| Quicksilver | Cıva | cıva | QI101-105; ST101 Nacimiento diyalogları |
| Abnegation | Feragat | feragat | Holy Wound of ~ -> Feragatin Kutsal Yarası (QI301, ST203) |
| Exiled Visage | Sürgün Çehre | sürgün çehre | Feragat yarasını veren (QI301, ST203) |
| Scapular | Skapular | skapular | QI203; ST202 Perpetva |
| Punishment | Ceza | ceza | es. «el Castigo», büyük harfli kavram, kesmesiz (Cezayı, Cezadan; Musibet emsali): ST03 Tirso, ST01 «pulled from Punishment»; the punished -> cezalandırılmışlar |
| Virtuous Pain | Erdemli Acı | erdemli acı | es. «Buen Dolor»; Heart of the ~ -> Erdemli Acının Kalbi (HE02) |
| Blue Rose | Mavi Gül | mavi gül | Miriam (ST205, PR201 «Mavi Gülün Cantiña'sı»); Portal of the ~ -> Mavi Gül Geçidi; Crystal -> Kristal (kesmesiz); Hall of Mirrors (es. sala del Espejo) -> Ayna Salonu; faceless -> çehresiz («yüzsüz» = utanmaz) |
| Holy Guardian Visages | Kutsal Muhafız Çehreler | muhafız çehre | Sürgün Çehre'nin üç kardeşi (ST203); kesmeli: Çehreler'den |
| Deogracias | Deogracias | deogracias | QI31-35 LORE başlığındaki «Deosgracias» oyunun yazım hatası -> Deogracias |
