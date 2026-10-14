// Blasphemous-TR yükleyicisi - Hazırlayan: Vechiron (MIT)
// Türkçe çeviriyi (Turkce.txt) oyunun I2 Localization kaynaklarında English
// yuvasının üstüne yazar. Oyuncu dili English seçili kalır.
//
// Yol (oyun kodu okundu, proje/dokuman/metin-yolu.md §1):
// - Her I2 kaynağı I2.Loc.LocalizationManager.AddSource'tan geçer (sahne
//   kaynakları, I2Languages, DialogManager.Start'taki "Dialog/Languages").
//   Postfix orada English sütununu ezer.
// - DialogManager metni kendi kopyasına AddSource'tan HEMEN SONRA alır
//   (Start: AddLanguageSource -> OnLocalizationChange). Yama ondan önce
//   kurulduysa kopya Türkçe olur; öz denetim (Update) bunu ölçer, kopya
//   eskiyse yeniler ve kayda yazar.
// - Büyük harf: oyun 17 başlık kutusunda (Localize ToUpper) ve haritadaki
//   bölge adında string.ToUpper() çağırıyor (İngilizce kural: i -> I). Aynı
//   terimler düğmelerde normal harfle gösterildiği için sözlükte BÜYÜK
//   yazmak çözüm değil (ölçüldü). Transpiler o iki yerdeki ToUpper'ı
//   TurkceBuyuk'la değiştirir: yalnız bizim Türkçe değerlerimize Türkçe
//   kural (i -> İ); oyunun kimlik/anahtar ToUpper'larına dokunulmaz.
// - Filtreleme ayarının değeri: oyun Unity enum adını (Disable/Enable/
//   ForceEnable) ToString ile ekrana yazıyor (Options/MapMenuWidget
//   ShowVideoValues; metotta tek ToString). Transpiler sonucu FiltreAdi'ndan
//   geçirir; ayar dosyası Enum.GetName ile ayrı yazıldığı için etkilenmez.
// Dosya biçimi: "ANAHTAR = metin"; # ile başlayan ve boş satır atlanır;
// metinde \n (LF), \r (CR), \t, \\ kaçışları. Dosyayı ceviri/ klasöründen
// biçim denetimiyle geliştirme ortamındaki ceviri.py yazar.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace BlasphemousTR
{
    [BepInPlugin(GUID, "Blasphemous TR", VERSION)]
    public class BlasphemousTRPlugin : BaseUnityPlugin
    {
        public const string GUID = "vechiron.blasphemous.tr";
        public const string VERSION = "1.0.0";
        private const string DigerYamaGuid = "com.alituna.blasphemous.turkishpatch";

        private static ManualLogSource log;
        private static readonly Dictionary<string, string> ceviri = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly HashSet<string> turkceDegerler = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> bulunan = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<LanguageSource, int> ezilen = new Dictionary<LanguageSource, int>();

        private bool denetimBitti;
        private float sonrakiDenetim;

        private void Awake()
        {
            log = Logger;
            string yol = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Turkce.txt");
            if (!Oku(yol))
            {
                denetimBitti = true;
                return;
            }
            try
            {
                Harmony h = new Harmony(GUID);
                h.Patch(AccessTools.Method(typeof(LocalizationManager), "AddSource"), null,
                    new HarmonyMethod(typeof(BlasphemousTRPlugin), "AddSourceSonrasi"));
                HarmonyMethod buyuk = new HarmonyMethod(typeof(BlasphemousTRPlugin), "BuyukHarfTranspiler");
                h.Patch(AccessTools.Method(typeof(Localize), "OnLocalize"), null, null, buyuk);
                MethodInfo harita = AccessTools.Method("Gameplay.UI.Others.MenuLogic.NewMapMenuWidget:UpdateCellData");
                if (harita != null)
                {
                    h.Patch(harita, null, null, buyuk);
                }
                log.LogInfo("Buyuk harf yamasi: " + degisenToUpper + " ToUpper cagrisi Turkce kurala baglandi (beklenen 3: Localize 2 + harita 1)");
            }
            catch (Exception e)
            {
                log.LogError("Harmony yamasi kurulamadi: " + e);
                denetimBitti = true;
                return;
            }
            // Ayrı: kurulamazsa yalnız filtreleme değeri İngilizce kalır, çeviri sürer
            try
            {
                Harmony h = new Harmony(GUID);
                HarmonyMethod filtre = new HarmonyMethod(typeof(BlasphemousTRPlugin), "FiltreTranspiler");
                foreach (string sinif in new string[] { "OptionsWidget", "MapMenuWidget" })
                {
                    MethodInfo m = AccessTools.Method("Gameplay.UI.Others.MenuLogic." + sinif + ":ShowVideoValues");
                    if (m != null)
                    {
                        h.Patch(m, null, null, filtre);
                    }
                }
                log.LogInfo("Filtreleme yamasi: " + degisenFiltre + " deger yazisi Turkceye baglandi (beklenen 2: secenekler + duraklatma menusu)");
            }
            catch (Exception e)
            {
                log.LogWarning("Filtreleme yamasi kurulamadi: " + e);
            }
            // Eklenti geç yüklendiyse zaten eklenmiş kaynaklar
            LanguageSource[] mevcut = LocalizationManager.Sources.ToArray();
            foreach (LanguageSource s in mevcut)
            {
                Ez(s);
            }
            if (mevcut.Length > 0)
            {
                log.LogWarning("Yama kurulmadan once eklenmis kaynak: " + mevcut.Length + " (ekran yenileniyor)");
                LocalizationManager.LocalizeAll(true);
            }
        }

        private void Start()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(DigerYamaGuid))
            {
                log.LogWarning("Baska bir Turkce yama (TurkishPatch) da acik: ikisi ayni English yuvasina yaziyor, ekranda karisik ceviri gorulebilir.");
            }
        }

        private static bool Oku(string yol)
        {
            if (!File.Exists(yol))
            {
                log.LogError("Ceviri dosyasi yok: " + yol);
                return false;
            }
            string[] satirlar = File.ReadAllLines(yol, Encoding.UTF8);
            int hatali = 0;
            for (int i = 0; i < satirlar.Length; i++)
            {
                string s = satirlar[i];
                if (s.Length == 0 || s[0] == '#')
                {
                    continue;
                }
                int k = s.IndexOf(" = ", StringComparison.Ordinal);
                if (k <= 0 || k + 3 >= s.Length)
                {
                    if (hatali++ < 10)
                    {
                        log.LogWarning("Turkce.txt:" + (i + 1) + " okunamadi (ayirici ya da metin yok)");
                    }
                    continue;
                }
                string metin = Coz(s.Substring(k + 3));
                ceviri[s.Substring(0, k)] = metin;
                turkceDegerler.Add(metin);
            }
            log.LogInfo("Ceviri okundu: " + ceviri.Count + " terim, okunamayan satir " + hatali);
            return ceviri.Count > 0;
        }

        private static string Coz(string s)
        {
            if (s.IndexOf('\\') < 0)
            {
                return s;
            }
            StringBuilder b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char d = s[++i];
                    if (d == 'n') b.Append('\n');
                    else if (d == 'r') b.Append('\r');
                    else if (d == 't') b.Append('\t');
                    else b.Append(d);
                }
                else
                {
                    b.Append(c);
                }
            }
            return b.ToString();
        }

        private static void AddSourceSonrasi(LanguageSource Source)
        {
            try
            {
                Ez(Source);
            }
            catch (Exception e)
            {
                log.LogError("Kaynak ezilemedi: " + e);
            }
        }

        // Kaynağın English sütununa Türkçeyi yazar; kaynakta olmayan ama
        // kategorisi bu kaynakta bulunan terimleri (oyunun eksiği, ör.
        // UI_Controls/ALREADY_USED) ekler.
        private static void Ez(LanguageSource s)
        {
            if (s == null || ezilen.ContainsKey(s))
            {
                return;
            }
            int en = -1;
            for (int i = 0; i < s.mLanguages.Count; i++)
            {
                if (s.mLanguages[i].Code == "en")
                {
                    en = i;
                    break;
                }
            }
            if (en < 0)
            {
                log.LogInfo("Kaynak '" + s.name + "': English yok, atlandi");
                return;
            }
            int n = 0;
            HashSet<string> kategoriler = new HashSet<string>(StringComparer.Ordinal);
            foreach (TermData t in s.mTerms)
            {
                if (t == null)
                {
                    continue;
                }
                kategoriler.Add(Kategori(t.Term));
                string tr;
                if (ceviri.TryGetValue(t.Term, out tr) && en < t.Languages.Length)
                {
                    t.Languages[en] = tr;
                    bulunan.Add(t.Term);
                    n++;
                }
            }
            int eklenen = 0;
            foreach (KeyValuePair<string, string> kv in ceviri)
            {
                if (bulunan.Contains(kv.Key) || !kategoriler.Contains(Kategori(kv.Key)) || s.GetTermData(kv.Key) != null)
                {
                    continue;
                }
                TermData t = s.AddTerm(kv.Key, eTermType.Text, false);
                t.Languages[en] = kv.Value;
                bulunan.Add(kv.Key);
                log.LogInfo("Eksik terim eklendi: " + kv.Key);
                eklenen++;
            }
            ezilen[s] = n + eklenen;
            log.LogInfo("Kaynak '" + s.name + "' (" + s.mTerms.Count + " terim, English=" + en + "): " + n + " ceviri yazildi, " + eklenen + " terim eklendi. Toplam bulunan " + bulunan.Count + "/" + ceviri.Count);
        }

        private static int degisenToUpper;

        private static IEnumerable<CodeInstruction> BuyukHarfTranspiler(IEnumerable<CodeInstruction> kodlar)
        {
            MethodInfo toUpper = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
            MethodInfo bizim = typeof(BlasphemousTRPlugin).GetMethod("TurkceBuyuk", BindingFlags.Public | BindingFlags.Static);
            foreach (CodeInstruction k in kodlar)
            {
                if ((k.opcode == OpCodes.Callvirt || k.opcode == OpCodes.Call) && (k.operand as MethodInfo) == toUpper)
                {
                    k.opcode = OpCodes.Call;
                    k.operand = bizim;
                    degisenToUpper++;
                }
                yield return k;
            }
        }

        // Yalnız bizim çevirimizden gelen metne Türkçe kural; gerisi oyunun eski davranışı
        public static string TurkceBuyuk(string s)
        {
            if (s == null || !turkceDegerler.Contains(s))
            {
                return s == null ? null : s.ToUpper();
            }
            return s.Replace('i', 'İ').ToUpperInvariant();
        }

        private static int degisenFiltre;

        // ShowVideoValues'taki tek ToString'in (filtreleme değeri) sonucunu FiltreAdi'ndan geçirir
        private static IEnumerable<CodeInstruction> FiltreTranspiler(IEnumerable<CodeInstruction> kodlar)
        {
            MethodInfo bizim = typeof(BlasphemousTRPlugin).GetMethod("FiltreAdi", BindingFlags.Public | BindingFlags.Static);
            foreach (CodeInstruction k in kodlar)
            {
                yield return k;
                MethodInfo m = k.operand as MethodInfo;
                if ((k.opcode == OpCodes.Callvirt || k.opcode == OpCodes.Call) && m != null && m.Name == "ToString" && m.GetParameters().Length == 0)
                {
                    degisenFiltre++;
                    yield return new CodeInstruction(OpCodes.Call, bizim);
                }
            }
        }

        // Kapalı/Açık sözlükteki UI/DISABLED_TEXT, UI/ENABLED_TEXT ile aynı; başka metin olduğu gibi kalır
        public static string FiltreAdi(string s)
        {
            switch (s)
            {
                case "Disable": return "Kapalı";
                case "Enable": return "Açık";
                case "ForceEnable": return "Zorla Açık";
                default: return s;
            }
        }

        private static string Kategori(string terim)
        {
            int k = terim.IndexOf('/');
            return k < 0 ? string.Empty : terim.Substring(0, k);
        }

        // Öz denetim: DialogManager'ın kopyasında Türkçe satır var mı?
        private void Update()
        {
            if (denetimBitti || Time.realtimeSinceStartup < sonrakiDenetim)
            {
                return;
            }
            sonrakiDenetim = Time.realtimeSinceStartup + 2f;
            try
            {
                Denetle();
            }
            catch (Exception e)
            {
                log.LogError("Oz denetim kosamadi: " + e);
                denetimBitti = true;
            }
        }

        private void Denetle()
        {
            Type core = Type.GetType("Framework.Managers.Core, Assembly-CSharp");
            PropertyInfo p = core == null ? null : core.GetProperty("Dialog", BindingFlags.Public | BindingFlags.Static);
            object dm = p == null ? null : p.GetValue(null, null);
            if (dm == null)
            {
                return;
            }
            Type t = dm.GetType();
            IDictionary diyaloglar = Alan(dm, "allDialogs") as IDictionary;
            IDictionary mesajlar = Alan(dm, "allMessges") as IDictionary;
            if (diyaloglar == null || diyaloglar.Count == 0)
            {
                return;
            }
            denetimBitti = true;
            MethodInfo kaynakAl = t.GetMethod("GetLanguageSource", BindingFlags.Public | BindingFlags.Static);
            LanguageSource kaynak = kaynakAl == null ? null : kaynakAl.Invoke(null, null) as LanguageSource;
            int kaynaktaki = 0;
            if (kaynak != null)
            {
                ezilen.TryGetValue(kaynak, out kaynaktaki);
            }
            int toplam, turkce;
            KopyaSay(diyaloglar, mesajlar, out toplam, out turkce);
            log.LogInfo("Oz denetim: diyalog kopyasi " + toplam + " satir, Turkce " + turkce + "; Dialog kaynaginda yazilan " + kaynaktaki);
            if (kaynaktaki > 0 && turkce == 0)
            {
                log.LogWarning("Diyalog kopyasi eski kalmis, yenileniyor");
                FieldInfo dil = t.GetField("currentLanguage", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo yenile = t.GetMethod("OnLocalizationChange", BindingFlags.NonPublic | BindingFlags.Instance);
                if (dil == null || yenile == null)
                {
                    log.LogError("DialogManager uyeleri bulunamadi; diyaloglar Ingilizce kalabilir");
                    return;
                }
                dil.SetValue(null, string.Empty);
                yenile.Invoke(dm, null);
                KopyaSay(diyaloglar, mesajlar, out toplam, out turkce);
                log.LogInfo("Oz denetim (yenileme sonrasi): diyalog kopyasi " + toplam + " satir, Turkce " + turkce);
            }
        }

        private static object Alan(object o, string ad)
        {
            FieldInfo f = o.GetType().GetField(ad, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return f == null ? null : f.GetValue(o);
        }

        private static void KopyaSay(IDictionary diyaloglar, IDictionary mesajlar, out int toplam, out int turkce)
        {
            toplam = 0;
            turkce = 0;
            foreach (object d in diyaloglar.Values)
            {
                Say(Alan(d, "dialogLines") as List<string>, ref toplam, ref turkce);
                Say(Alan(d, "answersLines") as List<string>, ref toplam, ref turkce);
            }
            if (mesajlar != null)
            {
                foreach (object m in mesajlar.Values)
                {
                    Say(Alan(m, "msgLines") as List<string>, ref toplam, ref turkce);
                }
            }
        }

        private static void Say(List<string> satirlar, ref int toplam, ref int turkce)
        {
            if (satirlar == null)
            {
                return;
            }
            foreach (string s in satirlar)
            {
                toplam++;
                if (s != null && turkceDegerler.Contains(s))
                {
                    turkce++;
                }
            }
        }
    }
}
