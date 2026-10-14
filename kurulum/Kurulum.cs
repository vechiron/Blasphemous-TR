// Blasphemous TR Kurulum - Blasphemous Türkçe çevirisinin kurulum aracı. Hazırlayan: Vechiron
// Kalıbı font eklentisinin eski kurulum aracından (TurkishFontFix-Kurulum).
// Kullanım: BlasphemousTR-Kurulum.exe [oyun klasörü]
//           BlasphemousTR-Kurulum.exe /sessiz /kur|/kaldir [oyun klasörü]
//           (sessiz çıkış kodları: 0 tamam, 1 oyun yok, 3 oyun açık, 4 hata)
// Gömülü kaynakları derleme betiği (derle.ps1) ekler; ad = önek + oyun klasörüne göre yol ('/' ile):
//   "B/" BepInEx 5.4.23.4 (yalnız BepInEx yoksa yazılır) · "E/" eklentiler (her kurulumda yazılır).
// Kurulum ne yaptığını BepInEx\plugins\BlasphemousTR\kurulum-kaydi.txt'ye yazar; Kaldır yalnız kendi
// yaptığını geri alır: önceden var olan BepInEx'e ve font eklentisine dokunmaz, kapattığı yamayı açar.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Blasphemous TR Kurulum")]
[assembly: AssemblyDescription("Blasphemous Türkçe çeviri kurulum aracı")]
[assembly: AssemblyCompany("Vechiron")]
[assembly: AssemblyProduct("Blasphemous TR")]
[assembly: AssemblyCopyright("© 2026 Vechiron")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace BlasphemousTRKurulum
{
    internal enum KurulumDurumu { Yok, Guncel, Farkli }

    internal class KurulumKaydi
    {
        public bool BepInExKurdu;
        public bool FontKurdu;
        public List<string> KapatilanYamalar = new List<string>();
    }

    internal static class Kurulum
    {
        public const string Surum = "1.0.0";
        public const string Yazar = "Vechiron";
        private const string SteamAppId = "774361";
        private const string EklentiKlasoru = @"BepInEx\plugins\BlasphemousTR";
        private const string FontKlasoru = @"BepInEx\plugins\TurkishFontFix";
        private const string KayitAdi = "kurulum-kaydi.txt";
        private const string KapaliUzanti = ".kapali";
        private static readonly string[] FontAyarDosyalari = { "com.vechiron.blasphemous.turkishfontfix.cfg", "com.blasphemous.turkishfontfix.cfg" };

        private static Dictionary<string, byte[]> kaynaklar;

        public static Dictionary<string, byte[]> Kaynaklar()
        {
            if (kaynaklar != null) return kaynaklar;
            kaynaklar = new Dictionary<string, byte[]>();
            Assembly a = Assembly.GetExecutingAssembly();
            foreach (string ad in a.GetManifestResourceNames())
            {
                if (!ad.StartsWith("B/") && !ad.StartsWith("E/")) continue;
                using (Stream s = a.GetManifestResourceStream(ad))
                using (MemoryStream m = new MemoryStream())
                {
                    s.CopyTo(m);
                    kaynaklar[ad] = m.ToArray();
                }
            }
            return kaynaklar;
        }

        private static string Yol(string d, string kaynakAdi)
        {
            return Path.Combine(d, kaynakAdi.Substring(2).Replace('/', '\\'));
        }

        private static bool Esit(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public static bool OyunKlasoruMu(string d)
        {
            try
            {
                return !string.IsNullOrEmpty(d) && File.Exists(Path.Combine(d, "Blasphemous.exe")) && Directory.Exists(Path.Combine(d, "Blasphemous_Data"));
            }
            catch
            {
                return false;
            }
        }

        public static bool BepInExVar(string d)
        {
            return File.Exists(Path.Combine(d, @"BepInEx\core\BepInEx.dll"));
        }

        // Oyuncunun önceden kurmuş olabileceği başka bir Türkçe yama (TurkishPatch.dll); uzantısı ".kapali" olan kapalı sayılır.
        private static List<string> Yamalar(string d, string uzanti)
        {
            List<string> l = new List<string>();
            string p = Path.Combine(d, @"BepInEx\plugins");
            try
            {
                if (!Directory.Exists(p)) return l;
                foreach (string f in Directory.GetFiles(p, "TurkishPatch*" + uzanti, SearchOption.AllDirectories))
                {
                    // Windows'un 3 harfli uzantı eşleşmesi gevşek: "*.dll" başka uzantıları da getirebilir
                    if (f.EndsWith(uzanti, StringComparison.OrdinalIgnoreCase)) l.Add(f);
                }
            }
            catch
            {
            }
            return l;
        }

        public static List<string> AcikYamalar(string d)
        {
            return Yamalar(d, ".dll");
        }

        public static KurulumDurumu Durum(string d)
        {
            bool hepsi = true, hicbiri = true;
            foreach (KeyValuePair<string, byte[]> k in Kaynaklar())
            {
                if (!k.Key.StartsWith("E/BepInEx/plugins/BlasphemousTR/")) continue;
                string f = Yol(d, k.Key);
                if (!File.Exists(f))
                {
                    hepsi = false;
                    continue;
                }
                hicbiri = false;
                if (!Esit(File.ReadAllBytes(f), k.Value)) hepsi = false;
            }
            if (hicbiri) return KurulumDurumu.Yok;
            return hepsi ? KurulumDurumu.Guncel : KurulumDurumu.Farkli;
        }

        public static bool OyunAcik()
        {
            return Process.GetProcessesByName("Blasphemous").Length > 0;
        }

        // ------------------------------------------------------------ kurulum kaydı

        private static string KayitYolu(string d)
        {
            return Path.Combine(Path.Combine(d, EklentiKlasoru), KayitAdi);
        }

        public static KurulumKaydi KayitOku(string d)
        {
            string p = KayitYolu(d);
            if (!File.Exists(p)) return null;
            KurulumKaydi k = new KurulumKaydi();
            foreach (string satir in File.ReadAllLines(p, Encoding.UTF8))
            {
                int i = satir.IndexOf('=');
                if (satir.StartsWith("#") || i < 0) continue;
                string ad = satir.Substring(0, i).Trim(), deger = satir.Substring(i + 1).Trim();
                if (ad == "bepinex") k.BepInExKurdu = deger == "kurdu";
                else if (ad == "font") k.FontKurdu = deger == "kurdu";
                else if (ad == "kapatilan" && deger.Length > 0) k.KapatilanYamalar.Add(deger);
            }
            return k;
        }

        private static void KayitYaz(string d, KurulumKaydi k)
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("# Blasphemous TR kurulum kaydı - kurulum aracının Kaldır düğmesi bu dosyaya göre geri alır. Elle değiştirmeyin.");
            s.AppendLine("bepinex=" + (k.BepInExKurdu ? "kurdu" : "vardi"));
            s.AppendLine("font=" + (k.FontKurdu ? "kurdu" : "vardi"));
            foreach (string y in k.KapatilanYamalar) s.AppendLine("kapatilan=" + y);
            Yaz(KayitYolu(d), new UTF8Encoding(false).GetBytes(s.ToString()));
        }

        // ------------------------------------------------------------ kur / kaldır

        private static void Yaz(string hedef, byte[] veri)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(hedef));
            string gecici = hedef + ".tmp";
            File.WriteAllBytes(gecici, veri);
            if (File.Exists(hedef)) File.Delete(hedef);
            File.Move(gecici, hedef);
        }

        private static void Sil(string f)
        {
            if (File.Exists(f)) File.Delete(f);
        }

        public static void Kur(string d)
        {
            // Yeniden kur/Güncelle: ilk kurulumun kaydı korunur (o zaman BepInEx'i biz kurduysak hâlâ bizimdir)
            KurulumKaydi k = KayitOku(d);
            bool bepVardi = BepInExVar(d);
            if (k == null)
            {
                k = new KurulumKaydi();
                k.BepInExKurdu = !bepVardi;
                k.FontKurdu = !File.Exists(Path.Combine(Path.Combine(d, FontKlasoru), "TurkishFontFix.dll"));
            }
            // kayıt önce yazılır: yarıda kalan bir kurulum da Kaldır ile temizlenebilsin
            KayitYaz(d, k);

            foreach (KeyValuePair<string, byte[]> r in Kaynaklar())
            {
                if (r.Key.StartsWith("B/") && bepVardi) continue;
                Yaz(Yol(d, r.Key), r.Value);
            }

            // iki çeviri aynı English yuvasına yazıyor: öteki yama silinmez, kapatılır
            string kok = d.TrimEnd('\\') + "\\";
            foreach (string y in AcikYamalar(d))
            {
                string kapali = y + KapaliUzanti;
                Sil(kapali);
                File.Move(y, kapali);
                string goreli = y.StartsWith(kok, StringComparison.OrdinalIgnoreCase) ? y.Substring(kok.Length) : y;
                if (!k.KapatilanYamalar.Contains(goreli)) k.KapatilanYamalar.Add(goreli);
            }
            KayitYaz(d, k);
        }

        public static void Kaldir(string d)
        {
            // kayıt yoksa (elle kurulum) yalnız kendi klasörümüz silinir
            KurulumKaydi k = KayitOku(d) ?? new KurulumKaydi();

            string ek = Path.Combine(d, EklentiKlasoru);
            if (Directory.Exists(ek)) Directory.Delete(ek, true);

            if (k.FontKurdu)
            {
                string f = Path.Combine(d, FontKlasoru);
                if (Directory.Exists(f)) Directory.Delete(f, true);
                foreach (string c in FontAyarDosyalari) Sil(Path.Combine(d, @"BepInEx\config\" + c));
            }

            foreach (string g in k.KapatilanYamalar)
            {
                string y = Path.Combine(d, g);
                if (File.Exists(y + KapaliUzanti) && !File.Exists(y)) File.Move(y + KapaliUzanti, y);
            }

            // BepInEx'i biz kurduysak ve başka eklenti kalmadıysa onu da kaldır
            if (k.BepInExKurdu && BepInExBos(d))
            {
                foreach (string r in Kaynaklar().Keys)
                {
                    if (r.StartsWith("B/") && r.IndexOf('/', 2) < 0) Sil(Yol(d, r)); // oyun kökündeki dosyalar
                }
                string b = Path.Combine(d, "BepInEx");
                if (Directory.Exists(b)) Directory.Delete(b, true);
            }
        }

        private static bool BepInExBos(string d)
        {
            foreach (string alt in new[] { "plugins", "patchers" })
            {
                string p = Path.Combine(Path.Combine(d, "BepInEx"), alt);
                if (Directory.Exists(p) && Directory.GetFiles(p, "*", SearchOption.AllDirectories).Length > 0) return false;
            }
            return true;
        }

        // ------------------------------------------------------------ oyun klasörünü bulma

        public static string Bul()
        {
            // 1) Kurulum aracı oyun klasöründen (ya da alt klasöründen) çalıştırıldıysa
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath));
                for (int i = 0; i < 4 && di != null; i++, di = di.Parent)
                {
                    if (OyunKlasoruMu(di.FullName)) return di.FullName;
                }
            }
            catch
            {
            }

            // 2) Steam kaldırma kaydı
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                string p = KayitDegeri(RegistryHive.LocalMachine, view, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + SteamAppId, "InstallLocation");
                if (OyunKlasoruMu(p)) return p;
            }

            // 3) Steam kütüphaneleri
            string steam = KayitDegeri(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Valve\Steam", "SteamPath");
            if (string.IsNullOrEmpty(steam)) steam = KayitDegeri(RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Valve\Steam", "InstallPath");
            if (!string.IsNullOrEmpty(steam))
            {
                steam = steam.Replace('/', '\\');
                string aday = Path.Combine(steam, @"steamapps\common\Blasphemous");
                if (OyunKlasoruMu(aday)) return aday;
                try
                {
                    string vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"(?:path|\\d+)\"\\s+\"([^\"]+)\""))
                        {
                            string lib = m.Groups[1].Value.Replace("\\\\", "\\");
                            aday = Path.Combine(lib, @"steamapps\common\Blasphemous");
                            if (OyunKlasoruMu(aday)) return aday;
                        }
                    }
                }
                catch
                {
                }
            }

            // 4) GOG
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey games = root.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                    {
                        if (games == null) continue;
                        foreach (string id in games.GetSubKeyNames())
                        {
                            using (RegistryKey g = games.OpenSubKey(id))
                            {
                                string p = g == null ? null : g.GetValue("path") as string;
                                if (OyunKlasoruMu(p)) return p;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            // 5) Varsayılan yollar
            foreach (string p in new[] { @"C:\Program Files (x86)\Steam\steamapps\common\Blasphemous", @"C:\GOG Games\Blasphemous", @"C:\Program Files (x86)\GOG Galaxy\Games\Blasphemous" })
            {
                if (OyunKlasoruMu(p)) return p;
            }
            return "";
        }

        private static string KayitDegeri(RegistryHive hive, RegistryView view, string key, string name)
        {
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey k = root.OpenSubKey(key))
                {
                    return k == null ? null : k.GetValue(name) as string;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool Yonetici()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        // ------------------------------------------------------------ giriş noktası

        [STAThread]
        private static int Main(string[] args)
        {
            string yol = null, islem = null;
            bool sessiz = false;
            foreach (string a in args)
            {
                string l = a.ToLowerInvariant();
                if (l == "/sessiz") sessiz = true;
                else if (l == "/kur") islem = "kur";
                else if (l == "/kaldir") islem = "kaldir";
                else yol = a.Trim().Trim('"').TrimEnd('\\');
            }

            if (sessiz && islem != null)
            {
                if (string.IsNullOrEmpty(yol)) yol = Bul();
                if (!OyunKlasoruMu(yol)) return 1;
                if (OyunAcik()) return 3;
                try
                {
                    if (islem == "kur") Kur(yol);
                    else Kaldir(yol);
                    return 0;
                }
                catch
                {
                    return 4;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new KurulumFormu(yol));
            return 0;
        }
    }

    internal class KurulumFormu : Form
    {
        private static readonly Color Bordo = Color.FromArgb(74, 16, 20);
        private static readonly Color Krem = Color.FromArgb(235, 222, 196);
        private static readonly Color Yesil = Color.FromArgb(46, 125, 50);
        private static readonly Color Kirmizi = Color.FromArgb(183, 28, 28);
        private static readonly Color Turuncu = Color.FromArgb(200, 110, 0);
        private static readonly Color Gri = Color.FromArgb(105, 105, 105);

        private const string DilNotu = "Yazılar Türkçe görünmüyorsa oyunda Seçenekler > Oyun bölümündeki metin dili satırını \"Türkçe\" yapın.";

        private readonly TextBox yol = new TextBox();
        private readonly Label lOyun = new Label(), lBepInEx = new Label(), lYama = new Label(), lCeviri = new Label();
        private readonly Button bKur = new Button(), bKaldir = new Button(), bKapat = new Button(), bGozat = new Button();

        public KurulumFormu(string ilkYol)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9.75F);
            Text = "Blasphemous TR Kurulum";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 392);
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Panel ust = new Panel();
            ust.BackColor = Bordo;
            ust.SetBounds(0, 0, 560, 80);
            Label baslik = YeniEtiket("Blasphemous Türkçe Çeviri", 20, 12, new Font("Segoe UI Semibold", 15F), Color.White);
            Label alt = YeniEtiket("Blasphemous TR v" + Kurulum.Surum + "   ·   Hazırlayan: " + Kurulum.Yazar, 22, 48, Font, Krem);
            baslik.BackColor = alt.BackColor = Bordo;
            ust.Controls.Add(baslik);
            ust.Controls.Add(alt);
            Controls.Add(ust);

            Label aciklama = new Label();
            aciklama.Text = "Oyunun bütün metnini Türkçeye çevirir; ğ, ş, ı, İ harflerini oyunun yazı stiline uydurur. Oyun dosyalarını değiştirmez, BepInEx eklentisi olarak çalışır.";
            aciklama.SetBounds(20, 94, 520, 42);
            Controls.Add(aciklama);

            Controls.Add(YeniEtiket("Oyun klasörü:", 20, 144, Font, Color.Black));
            yol.SetBounds(20, 166, 424, 26);
            bGozat.Text = "Gözat...";
            bGozat.SetBounds(450, 164, 90, 29);
            Controls.Add(yol);
            Controls.Add(bGozat);

            // lYama yalnız başka bir Türkçe yama açıkken görünür; en altta durur ki gizlenince boşluk kalmasın
            int y = 208;
            foreach (Label l in new[] { lOyun, lBepInEx, lCeviri, lYama })
            {
                l.AutoSize = true;
                l.Location = new Point(22, y);
                Controls.Add(l);
                y += 25;
            }

            Label imza = YeniEtiket("© 2026 " + Kurulum.Yazar, 20, 352, Font, Gri);
            Controls.Add(imza);
            bKaldir.Text = "Kaldır";
            bKur.Text = "Kur";
            bKapat.Text = "Kapat";
            bKaldir.SetBounds(254, 344, 90, 32);
            bKur.SetBounds(352, 344, 90, 32);
            bKapat.SetBounds(450, 344, 90, 32);
            Controls.Add(bKaldir);
            Controls.Add(bKur);
            Controls.Add(bKapat);
            AcceptButton = bKur;
            CancelButton = bKapat;

            bGozat.Click += delegate { Gozat(); };
            bKur.Click += delegate { KurTikla(); };
            bKaldir.Click += delegate { KaldirTikla(); };
            bKapat.Click += delegate { Close(); };
            yol.TextChanged += delegate { DurumuYenile(); };

            ResumeLayout(false);
            PerformLayout();

            yol.Text = string.IsNullOrEmpty(ilkYol) ? Kurulum.Bul() : ilkYol;
            DurumuYenile();
            ActiveControl = bKur.Enabled ? (Control)bKur : bGozat;
        }

        private static Label YeniEtiket(string text, int x, int y, Font font, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Location = new Point(x, y);
            l.Font = font;
            l.ForeColor = color;
            return l;
        }

        private static void Yaz(Label l, string isaret, string text, Color renk)
        {
            l.Text = isaret + "  " + text;
            l.ForeColor = renk;
        }

        private string Klasor()
        {
            return yol.Text.Trim().Trim('"').TrimEnd('\\');
        }

        private void DurumuYenile()
        {
            string d = Klasor();
            bool oyun = Kurulum.OyunKlasoruMu(d);
            if (oyun) Yaz(lOyun, "✓", "Blasphemous bulundu", Yesil);
            else if (d.Length == 0) Yaz(lOyun, "✗", "Oyun klasörü bulunamadı: Gözat ile seçin", Kirmizi);
            else Yaz(lOyun, "✗", "Bu klasörde Blasphemous bulunamadı", Kirmizi);

            if (!oyun) Yaz(lBepInEx, "•", "BepInEx", Gri);
            else if (Kurulum.BepInExVar(d)) Yaz(lBepInEx, "✓", "BepInEx kurulu", Yesil);
            else Yaz(lBepInEx, "•", "BepInEx kurulu değil: çeviriyle birlikte kurulacak", Gri);

            lYama.Visible = oyun && Kurulum.AcikYamalar(d).Count > 0;
            Yaz(lYama, "!", "Başka bir Türkçe yama kurulu: kurulumda kapatılacak (silinmez)", Turuncu);

            KurulumDurumu durum = oyun ? Kurulum.Durum(d) : KurulumDurumu.Yok;
            if (durum == KurulumDurumu.Yok)
            {
                Yaz(lCeviri, "•", "Türkçe çeviri kurulu değil", Gri);
                bKur.Text = "Kur";
            }
            else if (durum == KurulumDurumu.Guncel)
            {
                Yaz(lCeviri, "✓", "Türkçe çeviri v" + Kurulum.Surum + " kurulu", Yesil);
                bKur.Text = "Yeniden kur";
            }
            else
            {
                Yaz(lCeviri, "!", "Türkçe çevirinin farklı bir sürümü kurulu", Turuncu);
                bKur.Text = "Güncelle";
            }
            bKur.Enabled = oyun;
            bKaldir.Enabled = oyun && durum != KurulumDurumu.Yok;
        }

        private void Gozat()
        {
            using (FolderBrowserDialog f = new FolderBrowserDialog())
            {
                f.Description = "Blasphemous'un kurulu olduğu klasörü seçin (içinde Blasphemous.exe bulunan klasör).";
                f.ShowNewFolderButton = false;
                if (Directory.Exists(Klasor())) f.SelectedPath = Klasor();
                if (f.ShowDialog(this) == DialogResult.OK) yol.Text = f.SelectedPath;
            }
        }

        private bool OyunKapaliMi()
        {
            if (!Kurulum.OyunAcik()) return true;
            MessageBox.Show(this, "Blasphemous şu anda açık. Lütfen önce oyunu kapatın.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void KurTikla()
        {
            string d = Klasor();
            if (!OyunKapaliMi()) return;
            if (Kurulum.AcikYamalar(d).Count > 0 &&
                MessageBox.Show(this, "Oyunda başka bir Türkçe yama kurulu. İki çeviri aynı anda çalışamaz.\n\n" +
                    "Kurulum o yamayı silmeden kapatır; bu aracın Kaldır düğmesi onu yeniden açar. Devam edilsin mi?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                Kurulum.Kur(d);
                MessageBox.Show(this, "Kurulum tamamlandı. Oyunu başlatabilirsiniz.\n\n" + DilNotu, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                YoneticiOlarakDene(d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Kurulum başarısız oldu:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            DurumuYenile();
        }

        private void KaldirTikla()
        {
            string d = Klasor();
            if (!OyunKapaliMi()) return;
            KurulumKaydi k = Kurulum.KayitOku(d);
            string ek = k != null && k.KapatilanYamalar.Count > 0 ? "\n\nKurulumda kapatılan öteki Türkçe yama yeniden açılacak." : "";
            if (MessageBox.Show(this, "Türkçe çeviri kaldırılsın mı?" + ek, Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                Kurulum.Kaldir(d);
                MessageBox.Show(this, "Türkçe çeviri kaldırıldı.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                YoneticiOlarakDene(d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Kaldırma başarısız oldu:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            DurumuYenile();
        }

        private void YoneticiOlarakDene(string d)
        {
            if (Kurulum.Yonetici())
            {
                MessageBox.Show(this, "Bu klasöre yazılamadı (erişim engellendi).", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show(this, "Bu klasöre yazmak için yönetici izni gerekiyor. Kurulum aracı yönetici olarak yeniden başlatılsın mı?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath, "\"" + d + "\"");
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                Process.Start(psi);
                Close();
            }
            catch (Win32Exception)
            {
                // kullanıcı UAC penceresini iptal etti
            }
        }
    }
}
