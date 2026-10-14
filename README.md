# Blasphemous TR: Blasphemous Türkçe çevirisi

Blasphemous'un bütün metni Türkçe: menüler, diyaloglar, eşya ve dua açıklamaları, öğreticiler, harita. Çeviri İngilizceden yapıldı; anlamı belirsiz yerlerde oyunun asıl dili olan İspanyolcaya bakıldı. **ğ, ş, ı, İ** harfleri oyunun kendi piksel yazı stilinde görünür (Türkçe harf düzeltmesi dahil).

Hazırlayan: **Vechiron**

### ⬇ [Kurulum dosyasını indir (BlasphemousTR-Kurulum.exe)](https://github.com/vechiron/Blasphemous-TR/releases/latest/download/BlasphemousTR-Kurulum.exe)

Tüm sürümler: [Releases](https://github.com/vechiron/Blasphemous-TR/releases)

## Gereksinimler

- Blasphemous, Windows (test edilen sürüm: 4.0.67, Steam)

Başka bir şey gerekmez. Eklentileri oyuna yükleyen BepInEx oyununuzda yoksa kurulum aracı onu da kurar.

## Kurulum (3 adım)

1. **[BlasphemousTR-Kurulum.exe](https://github.com/vechiron/Blasphemous-TR/releases/latest/download/BlasphemousTR-Kurulum.exe)** dosyasını indirin.
2. Çalıştırın. Windows "Bilinmeyen yayımcı" (SmartScreen) uyarısı verirse **Ek bilgi > Yine de çalıştır** deyin; uygulama dijital olarak imzalı olmadığı için bu uyarı normaldir.
3. **Kur**'a basın. Oyun klasörü otomatik bulunur (başka sürücüdeki Steam kütüphaneleri ve GOG dahil); bulunamazsa **Gözat** ile `Blasphemous.exe`'nin bulunduğu klasörü seçin. Oyunu başlatın, hepsi bu.

Aynı araçla güncelleyebilir ya da **Kaldır**'a basarak kaldırabilirsiniz. Kaldır yalnızca kurulumun yaptığını geri alır: BepInEx'i kendisi kurduysa onu da kaldırır, önceden varsa dokunmaz.

**Yazılar Türkçe görünmüyorsa:** Türkçe, oyunun İngilizce dilinin yerine geçer. Oyun başka bir dilde açılıyorsa **Seçenekler > Oyun** bölümündeki metin dili satırını **Türkçe** yapın.

**Daha önce başka bir Türkçe yama kurduysanız:** iki çeviri aynı anda çalışamaz. Kurulum aracı öteki yamayı silmeden kapatır; Kaldır'a basınca yeniden açılır.

**Elle kurulum:** [BlasphemousTR.zip](https://github.com/vechiron/Blasphemous-TR/releases/latest/download/BlasphemousTR.zip) içindekilerin hepsini oyun klasörüne kopyalayın (Steam'de: **Yönet > Yerel dosyalara göz at**), klasörleri birleştirin. Kaldırmak için `BepInEx\plugins\BlasphemousTR` klasörünü silin.

Kurulum aracı komut satırından da kullanılabilir: `BlasphemousTR-Kurulum.exe /sessiz /kur` veya `/sessiz /kaldir` (isteğe bağlı olarak sonuna oyun klasörü).

## İngilizce kalanlar

- Jenerik ve ana menüdeki telif satırı (özgün hâliyle bırakıldı).
- Resmin içine çizilmiş birkaç yazı.

## Nasıl çalışır?

- BepInEx eklentisi (`BlasphemousTR.dll`) oyun açılırken `Turkce.txt`'deki çeviriyi oyunun dil sistemine (I2 Localization) İngilizce dilinin yerine yükler.
- Oyunun büyük harfe çevirdiği başlıklarda Türkçe kuralı uygular: "ERİŞİLEBİLİRLİK" ("ERIŞILEBILIRLIK" değil).
- Türkçe harf düzeltmesi (`TurkishFontFix.dll`): oyunun fontlarında olmayan ğ, ş, ı, İ harflerini her fontun kendi harflerinden üretir (g ile kısa işareti, s ile ç'nin çengeli, i'nin noktası) ve fonta ekler.
- Oyun dosyalarına dokunmaz; her şey oyun çalışırken bellekte yapılır.
- Çalıştığını görmek için: oyunu bir kez açıp kapattıktan sonra `BepInEx\LogOutput.log` dosyasında `[Blasphemous TR]` satırları görünür.

## Depo düzeni

| yer | ne |
|---|---|
| `ceviri/` | çevirinin kaynağı, kategori başına bir dosya (`Kategori/ANAHTAR = metin`); `terimce.md`: üslup kararları ve terim tablosu |
| `Turkce.txt` | eklentinin okuduğu birleşik çeviri dosyası (`ceviri/`'den biçim denetimiyle üretilir) |
| `src/` | eklentilerin kodu: `BlasphemousTR.cs` (çeviri yükleyici), `TurkishFontFix/` (Türkçe harf düzeltmesi) |
| `kurulum/` | kurulum aracının kodu ve oyuncu için BENİOKU |
| `derle.ps1` | derleme betiği |

## Derleme

Windows ile gelen .NET Framework 4 derleyicisi (`csc.exe`) yeterlidir. Oyunun kurulu olması gerekir, çünkü eklenti oyunun kendi kütüphanelerine karşı derlenir. Betik BepInEx 5.4.23.4'ü resmî sürüm sayfasından indirir ve karmasını denetler.

```bat
powershell -ExecutionPolicy Bypass -File derle.ps1 "C:\Program Files (x86)\Steam\steamapps\common\Blasphemous"
```

Çıktı: `bin\BlasphemousTR-Kurulum.exe` (her şeyi içine gömülü taşıyan kurulum aracı), `bin\BlasphemousTR.zip` (elle kurulum), `bin\BlasphemousTR.dll` ve `bin\TurkishFontFix.dll` (eklentiler)

## Yeni sürüm yayınlama (geliştiriciler için)

1. Sürüm numarasını güncelleyin: `src/BlasphemousTR.cs` (`VERSION`), `kurulum/Kurulum.cs` (`Surum`, `AssemblyVersion`, `AssemblyFileVersion`) ve `kurulum/BENIOKU.txt` (ilk satır).
2. `derle.ps1` ile derleyin.
3. Değişiklikleri gönderip sürümü [GitHub CLI](https://cli.github.com/) ile yayınlayın:

   ```bat
   gh release create vX.Y.Z bin\BlasphemousTR-Kurulum.exe bin\BlasphemousTR.zip --title "Blasphemous TR vX.Y.Z" --notes "Değişiklikler..."
   ```

   Dosya adlarını değiştirmeyin; README'deki "en son sürümü indir" bağlantıları bu adlara göre çalışır.

## Lisans

- Kod (`src/`, `kurulum/`, `derle.ps1`): MIT lisanslı, © 2026 Vechiron (bkz. `LICENSE`).
- Türkçe çeviri metni (`ceviri/`, `Turkce.txt`): © 2026 Vechiron, her hakkı saklıdır. Bu yamanın parçası olarak, değiştirilmeden dağıtılabilir.
- Blasphemous, The Game Kitchen'ın eseridir; bu depo oyunun hiçbir dosyasını ve özgün metnini içermez.
- BepInEx (LGPL-2.1) ve beraberindeki HarmonyX, MonoMod, Mono.Cecil (MIT) kurulum aracına değiştirilmeden gömülür: https://github.com/BepInEx/BepInEx
