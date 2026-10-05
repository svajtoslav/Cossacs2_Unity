using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TemnyLessViewer;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        // C2 Dialogs/respanel.DialogsSystem.xml -> cva_ResPanel_Money (VUI_Actions.cpp).
        // V384B: HUD is read-only. ResourcePeasants/NGidot/NFarms come from the
        // authoritative V384A Nation/City runtime; the HUD never scans the scene.
        // FontC14 is decoded through C2DirectSpriteBank, using the same byte value as
        // GPS.ShowGP(..., strptr[0], ...) in Fastdraw.cpp. This avoids the logical-frame
        // remap used by the generic UI bridge that rendered digits as U/W/Q glyphs.
        private RectTransform _resourceRoot;
        private readonly List<ResourceNumber> _resourceNumbers = new List<ResourceNumber>();
        private bool _resourcePanelAttempted;

        private C2DirectSpriteBank _resourceFontBankV384B;
        private string _resourceFontPathV384B;
        private readonly Dictionary<int, Sprite> _resourceFontGlyphsV384B = new Dictionary<int, Sprite>();

        private sealed class ResourceNumber
        {
            public int Id;
            public bool Population;
            public string Value;
            public float X, Y, Width;
            public RectTransform Parent;
            public readonly List<Image> Glyphs = new List<Image>();
        }

        private void RefreshResourcePanelLikeOriginal(Camera battleCamera)
        {
            if (battleCamera == null)
            {
                if (_resourceRoot != null) _resourceRoot.gameObject.SetActive(false);
                return;
            }
            if (_resourceRoot == null && !_resourcePanelAttempted)
            {
                _resourcePanelAttempted = true;
                LoadResourcePanelLikeOriginal();
            }
            if (_resourceRoot == null) return;
            _resourceRoot.gameObject.SetActive(true);

            int nation = C2EditorRuntimeStateV333LikeOriginal.ControlledNation;
            foreach (ResourceNumber number in _resourceNumbers)
            {
                string value;
                if (number.Population)
                {
                    // GroupPurpose.cpp::GetCurrentUnits/GetMaxUnits -> NGidot/NFarms.
                    int current = C2NationResourceEconomyV348LikeOriginal.GetCurrentUnitsLikeOriginal(nation);
                    int maximum = C2NationResourceEconomyV348LikeOriginal.GetMaxUnitsLikeOriginal(nation);
                    value = current.ToString(CultureInfo.InvariantCulture);
                    if (maximum > 0)
                        value += "/" + maximum.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    // VUI_Actions.cpp::cva_ResPanel_Money clamps display to 999999.
                    // The resource ledger itself is NOT truncated.
                    int amount = C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(nation, number.Id);
                    if (amount > 999999) amount = 999999;
                    if (amount < 0) amount = 0;

                    int peasants = C2NationResourceEconomyV348LikeOriginal.GetResourcePeasantsLikeOriginal(nation, number.Id);
                    value = peasants > 0
                        ? amount.ToString(CultureInfo.InvariantCulture) + "(" + peasants.ToString(CultureInfo.InvariantCulture) + ")"
                        : amount.ToString(CultureInfo.InvariantCulture);
                }

                if (number.Value == value) continue;
                number.Value = value;
                SetResourceNumberLikeOriginal(number, value);
            }
        }

        private void LoadResourcePanelLikeOriginal()
        {
            string path = null;
            foreach (string dataRoot in C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal())
            {
                string candidate = Path.Combine(dataRoot, "Dialogs", "respanel.DialogsSystem.xml");
                if (File.Exists(candidate)) { path = candidate; break; }
            }
            if (path == null) path = Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "Dialogs", "respanel.DialogsSystem.xml");
            try
            {
                DialogNode money = FindResourceMoneyNodeLikeOriginal(DialogNode.Parse(File.ReadAllText(path, Encoding.GetEncoding(1251))));
                if (money == null) throw new InvalidDataException("cva_ResPanel_Money missing");

                string fontAudit;
                if (!EnsureResourceFontV384BLikeOriginal(out fontAudit))
                    throw new InvalidDataException("FontC14 direct load failed: " + fontAudit);
                PrewarmResourceFontV384BLikeOriginal();

                string file = money.TextOf("GP_File");
                Sprite panel = C2GameplayOriginalSpriteCacheV1.LoadSprite(file, money.Int("SpritePassive0", 0), "original_resource_panel");
                if (C2GameplayOriginalSpriteCacheV1.GetSourceAudit(file, 0).StartsWith("FALLBACK"))
                    throw new InvalidDataException("Original ResPanel sprite missing");

                var go = new GameObject("GameplayHud_OriginalResourcePanel", typeof(RectTransform), typeof(Image));
                go.layer = GameplayHudLayer;
                go.transform.SetParent(_canvas.transform, false);
                _resourceRoot = (RectTransform)go.transform;
                _resourceRoot.anchorMin = _resourceRoot.anchorMax = new Vector2(0.5f, 1);
                _resourceRoot.pivot = new Vector2(0.5f, 1);
                _resourceRoot.anchoredPosition = Vector2.zero;
                _resourceRoot.sizeDelta = new Vector2(money.Int("Width", 929), money.Int("Height", 54));
                var picture = go.GetComponent<Image>();
                picture.sprite = panel;
                picture.raycastTarget = true;

                foreach (DialogNode children in money.Children)
                {
                    if (children.Name != "ChildDialogs") continue;
                    foreach (DialogNode node in children.Children)
                    {
                        if (node.Name != "TextButton") continue;
                        int id = node.Int("ID", -1);
                        bool population = string.Equals(node.TextOf("Name"), "Population", StringComparison.OrdinalIgnoreCase);
                        if (!population && (id < 0 || id >= 6)) continue;

                        _resourceNumbers.Add(new ResourceNumber
                        {
                            Id = id,
                            Population = population,
                            Parent = _resourceRoot,
                            X = node.Int("x", 0),
                            Y = node.Int("y", 0),
                            Width = node.Int("Width", 49)
                        });
                    }
                }

                Debug.Log("[C2:ECONOMY PANEL V384B] original=" + path + " sprite=" + file +
                    " font=Interf3/Fonts/FontC14 renderer=direct_GN16_ASCII " + fontAudit +
                    " fields=" + _resourceNumbers.Count.ToString(CultureInfo.InvariantCulture) +
                    " resourcePeasants=V384A NGidotNFarms=V384A displayClamp=999999");
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:ECONOMY PANEL V384B] " + ex.Message);
            }
        }

        private static DialogNode FindResourceMoneyNodeLikeOriginal(DialogNode node)
        {
            if (node.Name == "VitButton" && NodeHasActionV125LikeOriginal(node, "cva_ResPanel_Money")) return node;
            foreach (DialogNode child in node.Children)
            {
                DialogNode found = FindResourceMoneyNodeLikeOriginal(child);
                if (found != null) return found;
            }
            return null;
        }

        private bool EnsureResourceFontV384BLikeOriginal(out string audit)
        {
            if (_resourceFontBankV384B != null && _resourceFontBankV384B.FrameCount >= 128)
            {
                audit = "path='" + (_resourceFontPathV384B ?? string.Empty) + "' frames=" +
                        _resourceFontBankV384B.FrameCount.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            List<string> candidates = new List<string>();
            Action<string> add = delegate (string p)
            {
                if (string.IsNullOrWhiteSpace(p)) return;
                for (int i = 0; i < candidates.Count; i++)
                    if (string.Equals(candidates[i], p, StringComparison.OrdinalIgnoreCase)) return;
                candidates.Add(p);
            };

            foreach (string root in C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal())
            {
                add(Path.Combine(root, "Interf3", "Fonts", "FontC14.g16"));
                add(Path.Combine(root, "Cash", "Interf3_Fonts_FontC14.g16"));
                add(Path.Combine(root, "Cash", "interf3_Fonts_FontC14.g16"));
            }
            add(Path.Combine(Application.dataPath, "Resources", "Interf3", "interf3_Fonts_FontC14.g16"));
            add(Path.Combine(Application.dataPath, "Resources", "Interf3", "Fonts", "FontC14.g16"));

            string lastError = "no FontC14.g16 candidate exists";
            for (int i = 0; i < candidates.Count; i++)
            {
                string candidate = candidates[i];
                if (!File.Exists(candidate)) continue;
                var bank = new C2DirectSpriteBank();
                string error;
                if (!bank.Load(candidate, out error))
                {
                    lastError = candidate + ": " + error;
                    continue;
                }
                if (bank.FrameCount < 128)
                {
                    lastError = candidate + ": frames=" + bank.FrameCount.ToString(CultureInfo.InvariantCulture);
                    continue;
                }

                _resourceFontBankV384B = bank;
                _resourceFontPathV384B = candidate;
                audit = "path='" + candidate + "' frames=" + bank.FrameCount.ToString(CultureInfo.InvariantCulture) +
                        " byteFrames=ASCII_0_48_9_57_paren_40_41_slash_47";
                return true;
            }

            audit = lastError;
            return false;
        }

        private void PrewarmResourceFontV384BLikeOriginal()
        {
            const string required = "0123456789()/";
            for (int i = 0; i < required.Length; i++)
                LoadResourceFontGlyphV384BLikeOriginal(required[i]);
        }

        private Sprite LoadResourceFontGlyphV384BLikeOriginal(char ch)
        {
            int frameId = ch <= 255 ? (int)(byte)ch : (int)'?';
            Sprite cached;
            if (_resourceFontGlyphsV384B.TryGetValue(frameId, out cached)) return cached;

            string audit;
            if (!EnsureResourceFontV384BLikeOriginal(out audit))
                throw new InvalidDataException("FontC14 unavailable: " + audit);

            C2RenderedFrame frame;
            string error;
            if (!_resourceFontBankV384B.RenderFrame(frameId, out frame, out error) || frame == null || frame.Rgba == null)
                throw new InvalidDataException("FontC14 frame " + frameId.ToString(CultureInfo.InvariantCulture) + " failed: " + error);
            if (frame.Width <= 0 || frame.Height <= 0 || frame.Rgba.Length < frame.Width * frame.Height * 4)
                throw new InvalidDataException("FontC14 frame " + frameId.ToString(CultureInfo.InvariantCulture) + " invalid dimensions");

            // C2RenderedFrame is top-left RGBA; Unity Texture2D raw rows are bottom-up.
            byte[] rgba = (byte[])frame.Rgba.Clone();
            FlipResourceFontRowsV384B(rgba, frame.Width, frame.Height);

            Texture2D tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false, false);
            tex.name = "C2_FontC14_" + frameId.ToString("000", CultureInfo.InvariantCulture);
            tex.LoadRawTextureData(rgba);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, frame.Width, frame.Height), new Vector2(0.5f, 0.5f), 100.0f);
            _resourceFontGlyphsV384B[frameId] = sprite;
            return sprite;
        }

        private static void FlipResourceFontRowsV384B(byte[] rgba, int width, int height)
        {
            if (rgba == null || width <= 0 || height <= 1) return;
            int stride = width * 4;
            byte[] tmp = new byte[stride];
            for (int y = 0; y < height / 2; y++)
            {
                int top = y * stride;
                int bottom = (height - 1 - y) * stride;
                Buffer.BlockCopy(rgba, top, tmp, 0, stride);
                Buffer.BlockCopy(rgba, bottom, rgba, top, stride);
                Buffer.BlockCopy(tmp, 0, rgba, bottom, stride);
            }
        }

        private void SetResourceNumberLikeOriginal(ResourceNumber number, string value)
        {
            float width = 0;
            for (int i = 0; i < value.Length; i++)
            {
                Sprite sprite = LoadResourceFontGlyphV384BLikeOriginal(value[i]);
                if (i == number.Glyphs.Count)
                {
                    var go = new GameObject(number.Population ? "PopulationGlyph" : "ResourceGlyph", typeof(RectTransform), typeof(Image));
                    go.layer = GameplayHudLayer;
                    go.transform.SetParent(number.Parent, false);
                    var image = go.GetComponent<Image>();
                    image.raycastTarget = false;
                    number.Glyphs.Add(image);
                }
                number.Glyphs[i].sprite = sprite;
                number.Glyphs[i].color = Color.white;
                width += sprite.rect.width;
            }

            // XML TextButton Align=Center. Keep the authored x/Width and allow long
            // resource(worker) strings to extend symmetrically like the original text.
            float x = number.X + (number.Width - width) * 0.5f;
            for (int i = 0; i < number.Glyphs.Count; i++)
            {
                Image glyph = number.Glyphs[i];
                glyph.gameObject.SetActive(i < value.Length);
                if (i >= value.Length) continue;
                Rect sprite = glyph.sprite.rect;
                // fonts.xml FontC14 Top=12: retain native glyph origin and dimensions.
                Place((RectTransform)glyph.transform, Mathf.RoundToInt(x), Mathf.RoundToInt(number.Y - 12),
                    Mathf.RoundToInt(sprite.width), Mathf.RoundToInt(sprite.height));
                x += sprite.width;
            }
        }
    }
}
