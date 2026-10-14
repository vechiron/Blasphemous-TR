// TurkishFontFix - Blasphemous Türkçe yaması için font düzeltmesi. Hazırlayan: Vechiron
// Eksik Türkçe harfleri (ğ Ğ ş Ş ı İ) her fontun KENDİ gliflerinden üretir
// (g + breve, s + ç'nin çengeli, i'nin noktası vb.) ve font atlasına ekler.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 618 // CharacterInfo.uv / vert / width / flipped (eski ama çalışan alanlar)

[assembly: AssemblyTitle("TurkishFontFix")]
[assembly: AssemblyDescription("Blasphemous Türkçe yaması için font düzeltmesi: ğ Ğ ş Ş ı İ harflerini her fontun kendi gliflerinden üretir.")]
[assembly: AssemblyProduct("TurkishFontFix")]
[assembly: AssemblyCompany("Vechiron")]
[assembly: AssemblyCopyright("© 2026 Vechiron")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace TurkishFontFix
{
    [BepInPlugin(GUID, "Turkish Font Fix", VERSION)]
    [BepInDependency(TurkishPatchGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class TurkishFontFixPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.vechiron.blasphemous.turkishfontfix";
        public const string VERSION = "1.0.0";
        private const string TurkishPatchGuid = "com.alituna.blasphemous.turkishpatch";
        private const string TurkishPatchType = "TurkishPatch.TurkishPatchPlugin";

        // Bu harfler fontta yoksa font Türkçe metin için zaten kullanılamaz; yamalamaya değmez.
        private static readonly int[] RequiredLatin = { 0xE7, 0xC7, 0xF6, 0xD6, 0xFC, 0xDC }; // ç Ç ö Ö ü Ü

        internal static ManualLogSource Log;

        private ConfigEntry<bool> disableOldFontPatch;
        private ConfigEntry<bool> preventDesktopDump;
        private ConfigEntry<bool> dumpPreview;
        private string pluginDir;
        private float scanTimer;
        private float materialTimer;
        private bool oldPatchCheckedInUpdate;

        // font instanceId -> fontun "tamam" sayıldığı atlas (yamalandı / zaten destekliyor / uygun değil).
        // Atlas değişirse (başka bir yama ya da fontun yeniden yüklenmesi) font yeniden değerlendirilir.
        private readonly Dictionary<int, int> settled = new Dictionary<int, int>();
        // font instanceId -> ilk görülen (orijinal) atlas
        private readonly Dictionary<int, Texture> firstSeen = new Dictionary<int, Texture>();
        private readonly HashSet<int> ourTextures = new HashSet<int>();
        // eski atlas instanceId -> yeni atlas
        private readonly Dictionary<int, Texture2D> textureSwaps = new Dictionary<int, Texture2D>();

        private void Awake()
        {
            Log = Logger;
            pluginDir = Path.GetDirectoryName(Info.Location);
            disableOldFontPatch = Config.Bind("Uyumluluk", "EskiFontYamasiniKapat", true,
                "Türkçe yamanın (TurkishPatch) kendi font yamasını kapatır. Kapatılmasa da bu eklenti harfleri yine kendi sürümüyle değiştirir; açık kalması önerilir.");
            preventDesktopDump = Config.Bind("Uyumluluk", "MasaustuDokumunuEngelle", true,
                "Türkçe yamanın her açılışta Masaüstüne 'MajesticExtended_FullLatin_Dump' klasörü yazmasını engeller.");
            dumpPreview = Config.Bind("HataAyiklama", "OnizlemeKaydet", false,
                "true ise yamalanan her font için eklenti klasörüne önizleme PNG'si kaydeder.");
            ConfigureOldPatch();
        }

        private void Update()
        {
            if (!oldPatchCheckedInUpdate)
            {
                // Tüm eklentilerin Awake'i bittikten sonra bir kez daha uygula (yükleme sırası ne olursa olsun).
                oldPatchCheckedInUpdate = true;
                ConfigureOldPatch();
            }

            scanTimer += Time.unscaledDeltaTime;
            if (scanTimer >= 0.5f)
            {
                scanTimer = 0f;
                if (ScanFonts()) RedirectMaterials();
            }

            materialTimer += Time.unscaledDeltaTime;
            if (materialTimer >= 3f)
            {
                materialTimer = 0f;
                RedirectMaterials();
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch
                {
                }
            }
            return null;
        }

        // Türkçe yama eklentisinin (tek bir fonttan alınmış PNG'leri tüm fontlara yapıştıran) font yamasını
        // ve Masaüstüne döküm yazmasını kapatır. Çeviri kısmı aynen çalışmaya devam eder.
        private void ConfigureOldPatch()
        {
            try
            {
                Type t = FindType(TurkishPatchType);
                if (t == null) return;
                const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                if (disableOldFontPatch.Value)
                {
                    FieldInfo f = t.GetField("imagesPath", SF);
                    string disabled = Path.Combine(pluginDir, "__eski_font_yamasi_kapali__");
                    if (f != null && f.FieldType == typeof(string))
                    {
                        if (!disabled.Equals(f.GetValue(null) as string))
                        {
                            f.SetValue(null, disabled);
                            Log.LogInfo("Türkçe yamanın eski font yaması kapatıldı (çeviri etkilenmez).");
                        }
                    }
                    else
                    {
                        Log.LogWarning("Türkçe yamanın bu sürümünde eski font yaması kapatılamadı; harfler yine de bu eklentiyle değiştirilecek.");
                    }
                }
                if (preventDesktopDump.Value)
                {
                    FieldInfo d = t.GetField("dumpedFonts", SF);
                    if (d != null && d.FieldType == typeof(bool) && !(bool)d.GetValue(null))
                    {
                        d.SetValue(null, true);
                        Log.LogInfo("Türkçe yamanın Masaüstüne font dökümü yazması engellendi.");
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError("Türkçe yama ayarlanamadı: " + e);
            }
        }

        private static bool HasAll(int[] codes, Func<int, bool> has)
        {
            for (int i = 0; i < codes.Length; i++)
            {
                if (!has(codes[i])) return false;
            }
            return true;
        }

        // Başka bir eklentinin çalışma anında ürettiği (isimsiz) atlas mı?
        private bool IsForeignRuntimeTexture(Texture tex)
        {
            return !ourTextures.Contains(tex.GetInstanceID()) && string.IsNullOrEmpty(tex.name);
        }

        // true: en az bir font yamalandı
        private bool ScanFonts()
        {
            bool any = false;

            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < fonts.Length; i++)
            {
                Font font = fonts[i];
                if (font == null || font.dynamic) continue;
                Material mat = font.material;
                if (mat == null) continue;
                Texture tex = mat.mainTexture;
                if (tex == null) continue;
                int id = font.GetInstanceID();
                int settledTex;
                if (settled.TryGetValue(id, out settledTex) && settledTex == tex.GetInstanceID()) continue;
                if (!firstSeen.ContainsKey(id)) firstSeen[id] = tex;

                try
                {
                    if (!ourTextures.Contains(tex.GetInstanceID()))
                    {
                        CharacterInfo[] infos = font.characterInfo;
                        HashSet<int> present = new HashSet<int>();
                        if (infos != null)
                        {
                            for (int k = 0; k < infos.Length; k++) present.Add(infos[k].index);
                        }
                        bool latin = HasAll(RequiredLatin, present.Contains);
                        bool foreign = IsForeignRuntimeTexture(tex);
                        bool complete = HasAll(GlyphComposer.Codes, present.Contains);
                        if (latin && (foreign || !complete))
                        {
                            if (PatchLegacyFont(font, infos, foreign)) any = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.LogError("Font yamalanamadı (" + font.name + "): " + e);
                }
                Texture now = font.material != null ? font.material.mainTexture : null;
                settled[id] = now != null ? now.GetInstanceID() : 0;
            }

            TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                TMP_FontAsset fa = assets[i];
                if (fa == null || fa.fontAssetType != TMP_FontAsset.FontAssetTypes.Bitmap || fa.material == null) continue;
                Texture tex = fa.atlas != null ? (Texture)fa.atlas : fa.material.mainTexture;
                if (tex == null) continue;
                int id = fa.GetInstanceID();
                int settledTex;
                if (settled.TryGetValue(id, out settledTex) && settledTex == tex.GetInstanceID()) continue;
                if (!firstSeen.ContainsKey(id)) firstSeen[id] = tex;

                try
                {
                    if (!ourTextures.Contains(tex.GetInstanceID()))
                    {
                        Dictionary<int, TMP_Glyph> dict = fa.characterDictionary;
                        if (dict != null && dict.Count > 0)
                        {
                            bool latin = HasAll(RequiredLatin, dict.ContainsKey);
                            bool foreign = IsForeignRuntimeTexture(tex);
                            bool complete = HasAll(GlyphComposer.Codes, dict.ContainsKey);
                            if (latin && (foreign || !complete))
                            {
                                if (PatchTMPFont(fa, dict, tex, foreign)) any = true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.LogError("TMP font yamalanamadı (" + fa.name + "): " + e);
                }
                Texture now = fa.atlas != null ? (Texture)fa.atlas : (fa.material != null ? fa.material.mainTexture : null);
                settled[id] = now != null ? now.GetInstanceID() : 0;
            }
            return any;
        }

        // ---------------------------------------------------------------- eski tip font (UnityEngine.Font)

        private bool PatchLegacyFont(Font font, CharacterInfo[] infos, bool replaceExisting)
        {
            Texture src = font.material.mainTexture;
            int W = src.width, H = src.height;
            Color32[] px = TextureUtil.ReadPixels(src);

            Dictionary<int, CharacterInfo> map = new Dictionary<int, CharacterInfo>();
            for (int i = 0; i < infos.Length; i++)
            {
                if (!map.ContainsKey(infos[i].index)) map[infos[i].index] = infos[i];
            }

            Dictionary<int, PixelMap> cache = new Dictionary<int, PixelMap>();
            Func<int, PixelMap> get = delegate (int code)
            {
                PixelMap pm;
                if (cache.TryGetValue(code, out pm)) return pm;
                CharacterInfo ci;
                pm = map.TryGetValue(code, out ci) ? ExtractLegacy(ci, px, W, H) : null;
                cache[code] = pm;
                return pm;
            };

            Dictionary<int, PixelMap> built = GlyphComposer.Build(get);

            AtlasPacker packer = new AtlasPacker(px, W, H);
            for (int i = 0; i < infos.Length; i++)
            {
                Rect r = UvPixelRect(infos[i], W, H);
                packer.Reserve(Mathf.FloorToInt(r.xMin), Mathf.FloorToInt(r.yMin), Mathf.CeilToInt(r.xMax), Mathf.CeilToInt(r.yMax), 1);
            }

            List<CharacterInfo> list = new List<CharacterInfo>(infos);
            List<string> added = new List<string>();
            const int pad = 1; // orijinal glifler gibi 1 piksel saydam kenar
            foreach (int code in GlyphComposer.Codes)
            {
                if (!replaceExisting && map.ContainsKey(code)) continue;
                PixelMap g;
                if (!built.TryGetValue(code, out g)) continue;
                int x0, x1, y0, y1;
                if (!g.BBox(false, out x0, out x1, out y0, out y1)) continue;
                int gw = x1 - x0 + 1 + pad * 2, gh = y1 - y0 + 1 + pad * 2;
                int ax, ay;
                if (!packer.Find(gw, gh, 1, out ax, out ay))
                {
                    Log.LogWarning(font.name + ": atlasta yer yok (U+" + code.ToString("X4") + ")");
                    continue;
                }
                packer.Mark(ax, ay, gw, gh);
                int left = x0 - pad, top = y1 + 1 + pad;
                WriteGlyph(px, W, g, left, top, gw, gh, ax, ay);

                CharacterInfo b = map[GlyphComposer.BaseOf(code)];
                CharacterInfo ci = new CharacterInfo();
                ci.index = code;
                ci.size = b.size;
                ci.style = b.style;
                ci.flipped = false;
                ci.uv = new Rect((float)ax / W, (float)ay / H, (float)gw / W, (float)gh / H);
                ci.vert = new Rect(left, top, gw, -gh);
                ci.width = g.Advance;
                int c2 = code;
                list.RemoveAll(delegate (CharacterInfo q) { return q.index == c2; });
                list.Add(ci);
                added.Add(((char)code).ToString());
            }
            if (added.Count == 0) return false;

            Texture2D nt = TextureUtil.MakeTexture(px, W, H, src);
            font.material.mainTexture = nt;
            font.characterInfo = list.ToArray();
            RegisterSwap(font.GetInstanceID(), src, nt);

            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].font == font) texts[i].SetAllDirty();
            }
            TextMesh[] meshes = Resources.FindObjectsOfTypeAll<TextMesh>();
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] != null && meshes[i].font == font && !string.IsNullOrEmpty(meshes[i].text))
                {
                    string s = meshes[i].text;
                    meshes[i].text = "";
                    meshes[i].text = s;
                }
            }
            Log.LogInfo("Font yamalandı: " + font.name + " -> " + string.Join(" ", added.ToArray()) +
                (replaceExisting ? " (önceki yamanın harfleri değiştirildi)" : ""));

            if (dumpPreview.Value)
            {
                CharacterInfo[] finalInfos = font.characterInfo;
                Dictionary<int, CharacterInfo> fmap = new Dictionary<int, CharacterInfo>();
                for (int i = 0; i < finalInfos.Length; i++)
                {
                    if (!fmap.ContainsKey(finalInfos[i].index)) fmap[finalInfos[i].index] = finalInfos[i];
                }
                Preview.Dump(Path.Combine(pluginDir, "onizleme_" + font.name + ".png"), delegate (int code)
                {
                    CharacterInfo ci;
                    return fmap.TryGetValue(code, out ci) ? ExtractLegacy(ci, px, W, H) : null;
                });
            }
            return true;
        }

        private static Rect UvPixelRect(CharacterInfo ci, int W, int H)
        {
            Vector2 a = ci.uvBottomLeft, b = ci.uvBottomRight, c = ci.uvTopLeft, d = ci.uvTopRight;
            float xmin = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)) * W;
            float xmax = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)) * W;
            float ymin = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y)) * H;
            float ymax = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y)) * H;
            return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
        }

        private static PixelMap ExtractLegacy(CharacterInfo ci, Color32[] px, int W, int H)
        {
            PixelMap pm = new PixelMap();
            pm.Advance = ci.width;
            int w = Mathf.RoundToInt(Mathf.Abs(ci.vert.width));
            int h = Mathf.RoundToInt(Mathf.Abs(ci.vert.height));
            if (w <= 0 || h <= 0) return pm;
            int left = Mathf.RoundToInt(ci.vert.x);
            int top = Mathf.RoundToInt(ci.vert.height < 0 ? ci.vert.y : ci.vert.y + ci.vert.height);
            Vector2 tl = ci.uvTopLeft, tr = ci.uvTopRight, bl = ci.uvBottomLeft;
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    float u = (col + 0.5f) / w, v = (row + 0.5f) / h;
                    Vector2 t = tl + (tr - tl) * u + (bl - tl) * v;
                    int tx = Mathf.Clamp(Mathf.FloorToInt(t.x * W), 0, W - 1);
                    int ty = Mathf.Clamp(Mathf.FloorToInt(t.y * H), 0, H - 1);
                    byte a = px[ty * W + tx].a;
                    if (a > 0) pm.Put(left + col, top - 1 - row, a);
                }
            }
            return pm;
        }

        // ---------------------------------------------------------------- TextMeshPro (bitmap)

        private bool PatchTMPFont(TMP_FontAsset fa, Dictionary<int, TMP_Glyph> dict, Texture src, bool replaceExisting)
        {
            int W = src.width, H = src.height;
            Color32[] px = TextureUtil.ReadPixels(src);

            Dictionary<int, PixelMap> cache = new Dictionary<int, PixelMap>();
            Func<int, PixelMap> get = delegate (int code)
            {
                PixelMap pm;
                if (cache.TryGetValue(code, out pm)) return pm;
                TMP_Glyph tg;
                pm = (dict.TryGetValue(code, out tg) && tg != null) ? ExtractTMP(tg, px, W, H) : null;
                cache[code] = pm;
                return pm;
            };

            Dictionary<int, PixelMap> built = GlyphComposer.Build(get);

            int margin = Mathf.CeilToInt(fa.fontInfo != null ? fa.fontInfo.Padding : 2f) + 1;
            AtlasPacker packer = new AtlasPacker(px, W, H);
            foreach (TMP_Glyph tg in dict.Values)
            {
                if (tg == null) continue;
                int gx = Mathf.FloorToInt(tg.x), gyTop = Mathf.FloorToInt(tg.y);
                int gw = Mathf.CeilToInt(tg.width), gh = Mathf.CeilToInt(tg.height);
                packer.Reserve(gx, H - gyTop - gh, gx + gw, H - gyTop, margin);
            }

            FieldInfo listField = typeof(TMP_FontAsset).GetField("m_glyphInfoList", BindingFlags.Instance | BindingFlags.NonPublic);
            List<TMP_Glyph> glyphList = listField != null ? listField.GetValue(fa) as List<TMP_Glyph> : null;

            List<string> added = new List<string>();
            foreach (int code in GlyphComposer.Codes)
            {
                if (!replaceExisting && dict.ContainsKey(code)) continue;
                PixelMap g;
                if (!built.TryGetValue(code, out g)) continue;
                int x0, x1, y0, y1;
                if (!g.BBox(false, out x0, out x1, out y0, out y1)) continue;
                int gw = x1 - x0 + 1, gh = y1 - y0 + 1;
                int ax, ay;
                if (!packer.Find(gw, gh, margin, out ax, out ay))
                {
                    Log.LogWarning(fa.name + ": atlasta yer yok (U+" + code.ToString("X4") + ")");
                    continue;
                }
                packer.Mark(ax, ay, gw, gh);
                WriteGlyph(px, W, g, x0, y1 + 1, gw, gh, ax, ay);

                TMP_Glyph b = dict[GlyphComposer.BaseOf(code)];
                TMP_Glyph ng = new TMP_Glyph();
                ng.id = code;
                ng.x = ax;
                ng.y = H - (ay + gh);
                ng.width = gw;
                ng.height = gh;
                ng.xOffset = x0;
                ng.yOffset = y1 + 1;
                ng.xAdvance = g.Advance;
                ng.scale = b.scale;
                dict[code] = ng;
                if (glyphList != null)
                {
                    int c2 = code;
                    glyphList.RemoveAll(delegate (TMP_Glyph q) { return q != null && q.id == c2; });
                    glyphList.Add(ng);
                }
                added.Add(((char)code).ToString());
            }
            if (added.Count == 0) return false;

            Texture2D nt = TextureUtil.MakeTexture(px, W, H, src);
            fa.atlas = nt;
            fa.material.mainTexture = nt;
            RegisterSwap(fa.GetInstanceID(), src, nt);

            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].font == fa) texts[i].SetAllDirty();
            }
            Log.LogInfo("TMP font yamalandı: " + fa.name + " -> " + string.Join(" ", added.ToArray()) +
                (replaceExisting ? " (önceki yamanın harfleri değiştirildi)" : ""));

            if (dumpPreview.Value)
            {
                Preview.Dump(Path.Combine(pluginDir, "onizleme_TMP_" + fa.name + ".png"), delegate (int code)
                {
                    TMP_Glyph tg;
                    return (dict.TryGetValue(code, out tg) && tg != null) ? ExtractTMP(tg, px, W, H) : null;
                });
            }
            return true;
        }

        private static PixelMap ExtractTMP(TMP_Glyph g, Color32[] px, int W, int H)
        {
            PixelMap pm = new PixelMap();
            pm.Advance = g.xAdvance;
            int x = Mathf.RoundToInt(g.x), y = Mathf.RoundToInt(g.y);
            int w = Mathf.RoundToInt(g.width), h = Mathf.RoundToInt(g.height);
            int left = Mathf.RoundToInt(g.xOffset), top = Mathf.RoundToInt(g.yOffset);
            for (int row = 0; row < h; row++)
            {
                int ty = H - 1 - (y + row);
                if (ty < 0 || ty >= H) continue;
                for (int col = 0; col < w; col++)
                {
                    int tx = x + col;
                    if (tx < 0 || tx >= W) continue;
                    byte a = px[ty * W + tx].a;
                    if (a > 0) pm.Put(left + col, top - 1 - row, a);
                }
            }
            return pm;
        }

        // ---------------------------------------------------------------- ortak

        // Glifi atlasa dik yazar: glif satırı 0 (üst) -> doku satırı ay+gh-1.
        private static void WriteGlyph(Color32[] px, int W, PixelMap g, int left, int top, int gw, int gh, int ax, int ay)
        {
            for (int r = 0; r < gh; r++)
            {
                int ty = ay + gh - 1 - r;
                for (int c = 0; c < gw; c++)
                {
                    byte a = g.Get(left + c, top - 1 - r);
                    px[ty * W + ax + c] = new Color32(255, 255, 255, a);
                }
            }
        }

        private void RegisterSwap(int fontId, Texture src, Texture2D nt)
        {
            ourTextures.Add(nt.GetInstanceID());
            textureSwaps[src.GetInstanceID()] = nt;
            Texture orig;
            if (firstSeen.TryGetValue(fontId, out orig) && orig != null && orig != src && !ourTextures.Contains(orig.GetInstanceID()))
            {
                textureSwaps[orig.GetInstanceID()] = nt;
            }
        }

        // Eski atlasa referans veren diğer materyalleri (TMP preset'leri, TextMesh materyalleri vb.) yeni atlasa yönlendirir.
        private void RedirectMaterials()
        {
            if (textureSwaps.Count == 0) return;
            Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null || !m.HasProperty("_MainTex")) continue;
                Texture t = m.mainTexture;
                if (t == null) continue;
                Texture2D nt;
                if (textureSwaps.TryGetValue(t.GetInstanceID(), out nt) && nt != null && t != nt) m.mainTexture = nt;
            }
        }
    }
}
