using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TurkishFontFix
{
    // Font uzayında seyrek piksel haritası: (fx, fy) -> alfa. fy = 0, taban çizgisinin hemen üstündeki satır.
    internal class PixelMap
    {
        public readonly Dictionary<int, byte> Px = new Dictionary<int, byte>();
        public float Advance;

        public static int Key(int x, int y) { return ((x + 2048) << 12) | (y + 2048); }
        public static int X(int k) { return (k >> 12) - 2048; }
        public static int Y(int k) { return (k & 0xFFF) - 2048; }

        public byte Get(int x, int y)
        {
            byte a;
            return Px.TryGetValue(Key(x, y), out a) ? a : (byte)0;
        }

        public void Put(int x, int y, byte a)
        {
            if (a == 0) return;
            int k = Key(x, y);
            byte o;
            if (!Px.TryGetValue(k, out o) || o < a) Px[k] = a;
        }

        public List<int> MaskKeys()
        {
            List<int> l = new List<int>();
            foreach (KeyValuePair<int, byte> kv in Px)
            {
                if (kv.Value >= GlyphComposer.T) l.Add(kv.Key);
            }
            return l;
        }

        public int MaskCount { get { return MaskKeys().Count; } }

        public PixelMap Clone()
        {
            PixelMap p = new PixelMap();
            p.Advance = Advance;
            foreach (KeyValuePair<int, byte> kv in Px) p.Px[kv.Key] = kv.Value;
            return p;
        }

        public void MergeFrom(PixelMap o, int dx, int dy)
        {
            foreach (KeyValuePair<int, byte> kv in o.Px) Put(X(kv.Key) + dx, Y(kv.Key) + dy, kv.Value);
        }

        public bool BBox(bool maskOnly, out int x0, out int x1, out int y0, out int y1)
        {
            x0 = y0 = int.MaxValue;
            x1 = y1 = int.MinValue;
            foreach (KeyValuePair<int, byte> kv in Px)
            {
                if (maskOnly && kv.Value < GlyphComposer.T) continue;
                int x = X(kv.Key), y = Y(kv.Key);
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
            }
            return x1 >= x0;
        }

        public float CenterX()
        {
            int x0, x1, y0, y1;
            if (!BBox(true, out x0, out x1, out y0, out y1)) return 0f;
            return (x0 + x1) / 2f;
        }

        public byte MaxAlpha()
        {
            byte m = 0;
            foreach (byte a in Px.Values) if (a > m) m = a;
            return m;
        }
    }

    // Türkçe harfleri fontun kendi gliflerinden üretir.
    internal static class GlyphComposer
    {
        public const byte T = 128;
        public static readonly int[] Codes = { 0x11F, 0x11E, 0x15F, 0x15E, 0x131, 0x130 };

        public static int BaseOf(int code)
        {
            switch (code)
            {
                case 0x11E: return 'G';
                case 0x11F: return 'g';
                case 0x130: return 'I';
                case 0x131: return 'i';
                case 0x15E: return 'S';
                default: return 's';
            }
        }

        private static PixelMap NonEmpty(Func<int, PixelMap> get, int c)
        {
            PixelMap p = get(c);
            return (p != null && p.MaskCount > 0) ? p : null;
        }

        public static Dictionary<int, PixelMap> Build(Func<int, PixelMap> get)
        {
            Dictionary<int, PixelMap> res = new Dictionary<int, PixelMap>();
            int xh = XHeight(get);

            // ı (i'den nokta silinir) ve İ (I + i'nin noktası)
            PixelMap i = NonEmpty(get, 'i');
            PixelMap dot = null;
            if (i != null)
            {
                HashSet<int> dotKeys = new HashSet<int>();
                foreach (HashSet<int> comp in Components(i.MaskKeys()))
                {
                    int minY = int.MaxValue;
                    foreach (int k in comp) minY = Math.Min(minY, PixelMap.Y(k));
                    if (minY > xh) dotKeys.UnionWith(comp);
                }
                if (dotKeys.Count == 0)
                {
                    foreach (int k in i.MaskKeys())
                    {
                        if (PixelMap.Y(k) > xh) dotKeys.Add(k);
                    }
                }
                if (dotKeys.Count > 0 && dotKeys.Count < i.MaskCount)
                {
                    dot = new PixelMap();
                    foreach (int k in dotKeys) dot.Px[k] = i.Px[k];
                    int dx0, dx1, dy0, dy1;
                    dot.BBox(true, out dx0, out dx1, out dy0, out dy1);
                    PixelMap dotless = new PixelMap();
                    dotless.Advance = i.Advance;
                    foreach (KeyValuePair<int, byte> kv in i.Px)
                    {
                        if (dotKeys.Contains(kv.Key)) continue;
                        int x = PixelMap.X(kv.Key), y = PixelMap.Y(kv.Key);
                        if (kv.Value < T && y > xh && x >= dx0 - 1 && x <= dx1 + 1 && y >= dy0 - 1 && y <= dy1 + 1) continue;
                        dotless.Px[kv.Key] = kv.Value;
                    }
                    res[0x131] = dotless;
                }
            }

            PixelMap I = NonEmpty(get, 'I');
            if (I != null && dot != null)
            {
                int dx0, dx1, dy0, dy1, Ix0, Ix1, Iy0, Iy1;
                dot.BBox(true, out dx0, out dx1, out dy0, out dy1);
                I.BBox(true, out Ix0, out Ix1, out Iy0, out Iy1);
                int ty = 0;
                float cx = 0f;
                bool have = false;
                PixelMap idia = NonEmpty(get, 0xCF); // Ï: noktaların yüksekliği referans alınır
                if (idia != null)
                {
                    PixelMap dia = Diff(idia, I, Iy1, int.MaxValue);
                    int ax0, ax1, ay0, ay1;
                    if (dia.BBox(true, out ax0, out ax1, out ay0, out ay1))
                    {
                        ty = ay0;
                        cx = (ax0 + ax1) / 2f;
                        have = true;
                    }
                }
                if (!have)
                {
                    int sx0, sx1, sy0, sy1;
                    res[0x131].BBox(true, out sx0, out sx1, out sy0, out sy1);
                    int gap = Math.Max(1, dy0 - sy1 - 1);
                    ty = Iy1 + 1 + gap;
                    cx = StemX(I);
                }
                PixelMap r = I.Clone();
                r.MergeFrom(dot, Mathf.RoundToInt(cx - (dx0 + dx1) / 2f), ty - dy0);
                res[0x130] = r;
            }

            BuildCedilla(res, get, 's', 'c', 0xE7, 0x15F);
            BuildCedilla(res, get, 'S', 'C', 0xC7, 0x15E);
            BuildBreve(res, get, 'g', 'a', 0xE2, 0x11F);
            BuildBreve(res, get, 'G', 'A', 0xC2, 0x11E);
            return res;
        }

        // s + (ç - c) farkı, yani fontun kendi çengeli
        private static void BuildCedilla(Dictionary<int, PixelMap> res, Func<int, PixelMap> get, int baseC, int srcC, int cedC, int code)
        {
            PixelMap b = NonEmpty(get, baseC);
            if (b == null) return;
            int bx0, bx1, by0, by1;
            b.BBox(true, out bx0, out bx1, out by0, out by1);
            PixelMap ced = null;
            float rel = 0f;
            int ddy = 0;
            PixelMap c = NonEmpty(get, srcC), cc = NonEmpty(get, cedC);
            if (c != null && cc != null)
            {
                int cx0, cx1, cy0, cy1;
                c.BBox(true, out cx0, out cx1, out cy0, out cy1);
                ced = Diff(cc, c, int.MinValue, cy0 + 1);
                if (ced.MaskCount == 0) ced = null;
                else
                {
                    rel = ced.CenterX() - c.CenterX();
                    ddy = by0 - cy0;
                }
            }
            if (ced == null)
            {
                ced = ProceduralCedilla(b);
                rel = 0f;
                ddy = 0;
            }
            PixelMap r = b.Clone();
            r.MergeFrom(ced, Mathf.RoundToInt(b.CenterX() + rel - ced.CenterX()), ddy);
            res[code] = r;
        }

        // g + breve (â - a farkının kutusuna çizilen U biçimli breve)
        private static void BuildBreve(Dictionary<int, PixelMap> res, Func<int, PixelMap> get, int baseC, int srcC, int accC, int code)
        {
            PixelMap b = NonEmpty(get, baseC);
            if (b == null) return;
            int bx0, bx1, by0, by1;
            b.BBox(true, out bx0, out bx1, out by0, out by1);
            PixelMap br = null;
            float rel = 0f;
            int ddy = 0;
            PixelMap a = NonEmpty(get, srcC), acc = NonEmpty(get, accC);
            if (a != null && acc != null)
            {
                int ax0, ax1, ay0, ay1;
                a.BBox(true, out ax0, out ax1, out ay0, out ay1);
                PixelMap circ = Diff(acc, a, ay1, int.MaxValue);
                if (circ.MaskCount > 0)
                {
                    br = MakeBreve(circ);
                    rel = circ.CenterX() - a.CenterX();
                    ddy = by1 - ay1;
                }
            }
            if (br == null)
            {
                br = ProceduralBreve(b);
                rel = 0f;
                ddy = 0;
            }
            PixelMap r = b.Clone();
            r.MergeFrom(br, Mathf.RoundToInt(b.CenterX() + rel - br.CenterX()), ddy);
            res[code] = r;
        }

        // src'de olup base'de olmayan pikseller; yMinExcl < y < yMaxExcl
        private static PixelMap Diff(PixelMap src, PixelMap bas, int yMinExcl, int yMaxExcl)
        {
            PixelMap d = new PixelMap();
            foreach (KeyValuePair<int, byte> kv in src.Px)
            {
                if (kv.Value < T) continue;
                int x = PixelMap.X(kv.Key), y = PixelMap.Y(kv.Key);
                if (y <= yMinExcl || y >= yMaxExcl) continue;
                if (bas.Get(x, y) >= T) continue;
                d.Px[kv.Key] = kv.Value;
            }
            return d;
        }

        private static PixelMap MakeBreve(PixelMap circ)
        {
            int x0, x1, y0, y1;
            circ.BBox(true, out x0, out x1, out y0, out y1);
            int w = x1 - x0 + 1, h = Math.Max(2, y1 - y0 + 1);
            byte a = circ.MaxAlpha();
            PixelMap o = new PixelMap();
            if (w <= 2)
            {
                for (int x = x0; x <= x1; x++) o.Put(x, y0, a);
                o.Put(x0, y0 + 1, a);
                return o;
            }
            for (int x = x0 + 1; x < x1; x++) o.Put(x, y0, a);
            for (int y = y0 + 1; y < y0 + h; y++)
            {
                o.Put(x0, y, a);
                o.Put(x1, y, a);
            }
            return o;
        }

        private static PixelMap ProceduralBreve(PixelMap b)
        {
            int x0, x1, y0, y1;
            b.BBox(true, out x0, out x1, out y0, out y1);
            int w = Mathf.Clamp(x1 - x0 - 1, 3, 5);
            int cx = Mathf.RoundToInt(b.CenterX());
            int left = cx - w / 2, right = left + w - 1;
            byte a = b.MaxAlpha();
            PixelMap o = new PixelMap();
            for (int x = left + 1; x < right; x++) o.Put(x, y1 + 2, a);
            o.Put(left, y1 + 3, a);
            o.Put(right, y1 + 3, a);
            return o;
        }

        private static PixelMap ProceduralCedilla(PixelMap b)
        {
            int x0, x1, y0, y1;
            b.BBox(true, out x0, out x1, out y0, out y1);
            int cx = Mathf.RoundToInt(b.CenterX());
            byte a = b.MaxAlpha();
            PixelMap o = new PixelMap();
            o.Put(cx, y0 - 1, a);
            o.Put(cx + 1, y0 - 2, a);
            o.Put(cx, y0 - 3, a);
            o.Put(cx - 1, y0 - 3, a);
            return o;
        }

        private static int XHeight(Func<int, PixelMap> get)
        {
            List<int> tops = new List<int>();
            foreach (string set in new string[] { "xuvzwnmr", "oace" })
            {
                foreach (char c in set)
                {
                    PixelMap p = get(c);
                    int x0, x1, y0, y1;
                    if (p != null && p.BBox(true, out x0, out x1, out y0, out y1)) tops.Add(y1);
                }
                if (tops.Count > 0) break;
            }
            if (tops.Count == 0) return int.MaxValue / 2;
            tops.Sort();
            return tops[tops.Count / 2];
        }

        private static float StemX(PixelMap p)
        {
            Dictionary<int, int> cols = new Dictionary<int, int>();
            foreach (int k in p.MaskKeys())
            {
                int x = PixelMap.X(k);
                int n;
                cols.TryGetValue(x, out n);
                cols[x] = n + 1;
            }
            int best = 0;
            foreach (int v in cols.Values) best = Math.Max(best, v);
            int mn = int.MaxValue, mx = int.MinValue;
            foreach (KeyValuePair<int, int> kv in cols)
            {
                if (kv.Value != best) continue;
                mn = Math.Min(mn, kv.Key);
                mx = Math.Max(mx, kv.Key);
            }
            return (mn + mx) / 2f;
        }

        private static List<HashSet<int>> Components(List<int> keys)
        {
            HashSet<int> left = new HashSet<int>(keys);
            List<HashSet<int>> outList = new List<HashSet<int>>();
            while (left.Count > 0)
            {
                int start = 0;
                foreach (int k in left) { start = k; break; }
                left.Remove(start);
                HashSet<int> comp = new HashSet<int>();
                comp.Add(start);
                Stack<int> st = new Stack<int>();
                st.Push(start);
                while (st.Count > 0)
                {
                    int k = st.Pop();
                    int x = PixelMap.X(k), y = PixelMap.Y(k);
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int q = PixelMap.Key(x + dx, y + dy);
                            if (left.Remove(q))
                            {
                                comp.Add(q);
                                st.Push(q);
                            }
                        }
                    }
                }
                outList.Add(comp);
            }
            return outList;
        }
    }

    // Atlasta boş dikdörtgen arar (y yukarı doku koordinatları).
    internal class AtlasPacker
    {
        private readonly int W, H;
        private readonly bool[] occ;
        private int[] sat;
        private bool dirty = true;

        public AtlasPacker(Color32[] px, int w, int h)
        {
            W = w;
            H = h;
            occ = new bool[w * h];
            for (int i = 0; i < occ.Length; i++) occ[i] = px[i].a != 0;
        }

        public void Reserve(int x0, int y0, int x1, int y1, int margin)
        {
            x0 = Math.Max(0, x0 - margin);
            y0 = Math.Max(0, y0 - margin);
            x1 = Math.Min(W, x1 + margin);
            y1 = Math.Min(H, y1 + margin);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++) occ[y * W + x] = true;
            }
            dirty = true;
        }

        public void Mark(int x, int y, int w, int h)
        {
            Reserve(x, y, x + w, y + h, 0);
        }

        private void BuildSat()
        {
            int sw = W + 1;
            sat = new int[sw * (H + 1)];
            for (int y = 0; y < H; y++)
            {
                int row = 0;
                for (int x = 0; x < W; x++)
                {
                    if (occ[y * W + x]) row++;
                    sat[(y + 1) * sw + x + 1] = sat[y * sw + x + 1] + row;
                }
            }
            dirty = false;
        }

        private int Sum(int x0, int y0, int x1, int y1)
        {
            int sw = W + 1;
            return sat[y1 * sw + x1] - sat[y0 * sw + x1] - sat[y1 * sw + x0] + sat[y0 * sw + x0];
        }

        public bool Find(int w, int h, int margin, out int fx, out int fy)
        {
            if (dirty) BuildSat();
            for (int y = H - margin - h; y >= margin; y--)
            {
                for (int x = margin; x + w + margin <= W; x++)
                {
                    if (Sum(x - margin, y - margin, x + w + margin, y + h + margin) == 0)
                    {
                        fx = x;
                        fy = y;
                        return true;
                    }
                }
            }
            fx = fy = -1;
            return false;
        }
    }

    internal static class TextureUtil
    {
        // Okunamaz (non-readable) dokuları da GPU üzerinden okur.
        public static Color32[] ReadPixels(Texture tex)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.filterMode = FilterMode.Point;
            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                Texture2D tmp = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, true);
                tmp.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
                tmp.Apply(false, false);
                Color32[] px = tmp.GetPixels32();
                UnityEngine.Object.Destroy(tmp);
                return px;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        public static Texture2D MakeTexture(Color32[] px, int w, int h, Texture like)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.name = like.name + " (TR)";
            t.filterMode = like.filterMode;
            t.wrapMode = like.wrapMode;
            t.anisoLevel = like.anisoLevel;
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }
    }

    // Hata ayıklama: yamalanmış fontla örnek metin çizip PNG kaydeder.
    internal static class Preview
    {
        public static void Dump(string path, Func<int, PixelMap> get)
        {
            try
            {
                string[] lines =
                {
                    "gğ GĞ sş SŞ iı Iİ cç CÇ",
                    "Herhangi bir TUŞA BAS",
                    "Işık Ağaç Değiş İçeri ĞÜŞİÖÇ"
                };
                const int lineH = 26, asc = 18, scale = 4;
                int cw = 0;
                foreach (string s in lines)
                {
                    float w = 4f;
                    foreach (char ch in s)
                    {
                        PixelMap p = get(ch);
                        w += p != null ? p.Advance : 4f;
                    }
                    cw = Math.Max(cw, Mathf.CeilToInt(w) + 4);
                }
                int ch2 = lineH * lines.Length;
                Color32 bg = new Color32(30, 25, 20, 255);
                Color32[] canvas = new Color32[cw * ch2];
                for (int i = 0; i < canvas.Length; i++) canvas[i] = bg;
                for (int li = 0; li < lines.Length; li++)
                {
                    float x = 2f;
                    int baseRow = li * lineH + asc;
                    foreach (char c in lines[li])
                    {
                        PixelMap p = get(c);
                        if (p == null) { x += 4f; continue; }
                        foreach (KeyValuePair<int, byte> kv in p.Px)
                        {
                            int X = Mathf.FloorToInt(x) + PixelMap.X(kv.Key);
                            int R = baseRow - 1 - PixelMap.Y(kv.Key);
                            if (X < 0 || X >= cw || R < 0 || R >= ch2) continue;
                            float t = kv.Value / 255f;
                            Color32 o = canvas[R * cw + X];
                            canvas[R * cw + X] = new Color32((byte)(o.r + (235 - o.r) * t), (byte)(o.g + (225 - o.g) * t), (byte)(o.b + (200 - o.b) * t), 255);
                        }
                        x += p.Advance;
                    }
                }
                int ow = cw * scale, oh = ch2 * scale;
                Color32[] outPx = new Color32[ow * oh];
                for (int y = 0; y < oh; y++)
                {
                    for (int x = 0; x < ow; x++) outPx[(oh - 1 - y) * ow + x] = canvas[(y / scale) * cw + x / scale];
                }
                Texture2D tex = new Texture2D(ow, oh, TextureFormat.RGBA32, false);
                tex.SetPixels32(outPx);
                tex.Apply(false, false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
            }
            catch (Exception e)
            {
                TurkishFontFixPlugin.Log.LogWarning("Önizleme kaydedilemedi: " + e.Message);
            }
        }
    }
}
