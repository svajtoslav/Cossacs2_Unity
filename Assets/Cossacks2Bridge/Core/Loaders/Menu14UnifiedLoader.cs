using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// V388: one parser/loader for every menu DialogsSystem file.
    ///
    /// Cossacks II menu files are XML-like, but not valid System.Xml documents
    /// (for example &lt;Position&amp;Width&gt; and the empty &lt;&gt; wrapper).  This loader
    /// therefore builds a tiny balanced-tag tree and reads ONLY direct scalar
    /// children of each control.  ChildDialogs is traversed recursively and is
    /// never flattened into its parent.
    ///
    /// All menu screens routed by Dialogs/MainMenu.xml pass through this class.
    /// MainMenuLoader and OptionsLoader are now compatibility wrappers only.
    /// </summary>
    public sealed class Menu14UnifiedLoader
    {
        private const string RouterPath = @"Dialogs\MainMenu.xml";
        private readonly CoreFileSystem _fs;
        private readonly string _canonicalMenuDataRoot;

        private static readonly HashSet<string> SectionTitleKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "INTF_OPT_VO", "INTF_OPT_AO", "INTF_OPT_GO"
        };

        private static readonly HashSet<string> OptionLabelKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "#MM_Options_VideoResolution_Hint",
            "#MM_Options_AnimationQuality",
            "#MM_Options_SoundVolume_Hint",
            "#MM_Options_MusicVolume_Hint",
            "#MM_Options_ScrollingSpeed_Hint",
            "#MM_Options_EnableMusic",
            "#MM_Options_ShowHint_Hint",
            "#MM_Options_ShowVideo_Hint",
            "#MO_ArcadeMode",
        };

        private static readonly HashSet<string> WindowTitleKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "#Options_Window"
        };

        public Menu14UnifiedLoader(CoreFileSystem fs, string canonicalMenuDataRoot = null)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _canonicalMenuDataRoot = canonicalMenuDataRoot ?? string.Empty;
        }

        public UiDesk LoadScreen(string screenId)
        {
            string routerText = ReadMenuText(RouterPath, out string routerSource);
            if (string.IsNullOrWhiteSpace(routerText))
            {
                UnityEngine.Debug.LogWarning($"[C2:MENU14 XML V388] router missing '{RouterPath}'");
                return new UiDesk { SourcePath = $"[missing:{RouterPath}]" };
            }

            LiteNode routerDoc = LiteDocument.Parse(routerText);
            LiteNode menu = routerDoc.FirstDescendant("MainMenu") ?? routerDoc;
            LiteNode route = menu.Children.FirstOrDefault(n =>
                n.Tag.Equals(screenId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            if (route == null && !string.Equals(screenId, "Main", StringComparison.OrdinalIgnoreCase))
                route = menu.Children.FirstOrDefault(n => n.Tag.Equals("Main", StringComparison.OrdinalIgnoreCase));

            string target = NormalizePath(route?.DirectText() ?? @"Dialogs\v\M_Main.DialogsSystem.xml");
            return LoadPath(target, screenId ?? string.Empty, routerSource);
        }

        public UiDesk LoadPath(string dialogsSystemPath, string screenId = "", string routerSource = "")
        {
            dialogsSystemPath = NormalizePath(dialogsSystemPath);
            string text = ReadMenuText(dialogsSystemPath, out string sourceKind);
            var desk = new UiDesk
            {
                SourcePath = dialogsSystemPath,
                XmlSource = sourceKind,
                SourceBundleId = string.Equals(sourceKind, "StreamingAssets-clean14", StringComparison.OrdinalIgnoreCase)
                    ? "clean14"
                    : "DataRoot",
                SourceDataRoot = string.Equals(sourceKind, "StreamingAssets-clean14", StringComparison.OrdinalIgnoreCase)
                    ? _canonicalMenuDataRoot
                    : (_fs.DataRoot ?? string.Empty),
                ScreenId = screenId ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                UnityEngine.Debug.LogWarning($"[C2:MENU14 XML V388] missing screen='{screenId}' path='{dialogsSystemPath}'");
                return desk;
            }

            LiteNode doc = LiteDocument.Parse(text);
            LiteNode rootChildren = doc.Children.FirstOrDefault(n => n.Tag.Equals("ChildDialogs", StringComparison.OrdinalIgnoreCase));
            if (rootChildren == null)
            {
                // Some dialog fragments are a single control/container rather than a full DialogsSystem.
                rootChildren = doc.FirstDescendant("ChildDialogs");
            }

            int sourceId = 0;
            if (rootChildren != null)
                ParseChildDialogs(rootChildren, 0, 0, true, true, -1, 0, desk, ref sourceId);
            else
                ParseLooseTopLevel(doc, desk, ref sourceId);

            desk.ParsedNodeCount = sourceId;
            desk.GenericNodeCount = desk.Children.Count(n => n is UiGenericNode);

            UnityEngine.Debug.Log(
                $"[C2:MENU14 XML V388] screen='{screenId}' path='{dialogsSystemPath}' " +
                $"xml='{sourceKind}' controls={desk.Children.Count} parsed={desk.ParsedNodeCount} " +
                $"generic={desk.GenericNodeCount}");
            return desk;
        }

        /// <summary>
        /// Parses every screen route once.  This is intentionally a parser self-test only;
        /// it creates no Unity UI and changes no game state.
        /// </summary>
        public void ValidateAllRoutes()
        {
            try
            {
                string routerText = ReadMenuText(RouterPath, out _);
                if (string.IsNullOrWhiteSpace(routerText)) return;

                LiteNode doc = LiteDocument.Parse(routerText);
                LiteNode menu = doc.FirstDescendant("MainMenu") ?? doc;
                int ok = 0;
                int failed = 0;
                int controls = 0;
                foreach (LiteNode route in menu.Children)
                {
                    string path = NormalizePath(route.DirectText());
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    try
                    {
                        UiDesk d = LoadPath(path, route.Tag);
                        controls += d.Children.Count;
                        if (d.Children.Count > 0) ok++; else failed++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        UnityEngine.Debug.LogWarning(
                            $"[C2:MENU14 XML V388] route parse failed screen='{route.Tag}' path='{path}': {ex.GetType().Name}: {ex.Message}");
                    }
                }

                UnityEngine.Debug.Log($"[C2:MENU14 XML V388] router-selftest ok={ok} failed={failed} controls={controls}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[C2:MENU14 XML V388] router-selftest failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void ParseLooseTopLevel(LiteNode doc, UiDesk desk, ref int sourceId)
        {
            foreach (LiteNode child in doc.Children)
            {
                if (!LooksLikeControl(child.Tag)) continue;
                ParseControl(child, 0, 0, true, true, -1, 0, desk, ref sourceId);
            }
        }

        private void ParseChildDialogs(
            LiteNode childDialogs,
            int baseX,
            int baseY,
            bool inheritedVisible,
            bool inheritedEnabled,
            int parentSourceId,
            int depth,
            UiDesk desk,
            ref int sourceId)
        {
            foreach (LiteNode control in childDialogs.Children)
            {
                if (!LooksLikeControl(control.Tag))
                    continue;

                ParseControl(control, baseX, baseY, inheritedVisible, inheritedEnabled,
                    parentSourceId, depth, desk, ref sourceId);
            }
        }

        private void ParseControl(
            LiteNode x,
            int baseX,
            int baseY,
            bool inheritedVisible,
            bool inheritedEnabled,
            int parentSourceId,
            int depth,
            UiDesk desk,
            ref int sourceId)
        {
            int id = sourceId++;
            int localX = x.Int("x");
            int localY = x.Int("y");
            int absX = baseX + localX;
            int absY = baseY + localY;
            bool localVisible = x.Bool("Visible", true);
            bool localEnabled = x.Bool("Enabled", true);
            bool visible = inheritedVisible && localVisible;
            bool enabled = inheritedEnabled && localEnabled;

            UiNode node = CreateNode(x, desk.SourcePath, absX, absY, visible, enabled);
            if (node == null)
                node = new UiGenericNode { Kind = x.Tag };

            FillCommon(node, x, absX, absY, localX, localY, localVisible, localEnabled, visible, enabled, id, parentSourceId, depth);
            FillActions(node, x);
            desk.Children.Add(node);

            // Every control/container may own ChildDialogs in the original engine.
            // We recurse for all of them, not only DialogsDesk. This is what keeps
            // cva_MU_NickInput (InputBox inside VitButton) and similar nested controls.
            LiteNode nested = x.Child("ChildDialogs");
            if (nested != null)
            {
                int dx = x.Int("dx");
                int dy = x.Int("dy");
                ParseChildDialogs(
                    nested,
                    absX + dx,
                    absY + dy,
                    visible,
                    enabled,
                    id,
                    depth + 1,
                    desk,
                    ref sourceId);
            }
        }

        private UiNode CreateNode(LiteNode x, string sourcePath, int absX, int absY, bool visible, bool enabled)
        {
            string tag = x.Tag;

            if (tag.Equals("DialogsDesk", StringComparison.OrdinalIgnoreCase))
            {
                return new UiDialogsDesk
                {
                    Border = x.Value("Border"),
                    EnableHorizontalScroller = x.Bool("EnableHorizontalScroller", false),
                    EnableVerticalScroller = x.Bool("EnableVerticalScroller", false),
                    HideVScroller = x.Bool("HideVScroller", false),
                    EnableMouseShift = x.Bool("EnableMouseShift", false),
                    XShift = x.Int("XShift"),
                    YShift = x.Int("YShift")
                };
            }

            if (tag.Equals("BitPicture", StringComparison.OrdinalIgnoreCase))
            {
                return new UiBitPicture { FileName = x.Value("FileName") };
            }

            if (tag.Equals("GPPicture", StringComparison.OrdinalIgnoreCase))
            {
                return new UiGPPicture
                {
                    FileID = x.Value("FileID"),
                    SpriteID = x.Int("SpriteID")
                };
            }

            if (tag.Equals("TextButton", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                string message = x.Value("Message");
                string passive = x.Value("PassiveFont");
                return new UiTextButton
                {
                    MessageKey = message,
                    HintKey = x.Value("Hint"),
                    PassiveFont = passive,
                    ActiveFont = x.Value("ActiveFont"),
                    DisabledFont = x.Value("DisabledFont"),
                    Align = EmptyTo(x.Value("Align"), "Left"),
                    MaxWidth = x.Int("MaxWidth", 10000),
                    Vertical = x.Bool("Vertical", false),
                    Style = DetermineTextStyle(sourcePath, message, passive, absX, absY)
                };
            }

            if (tag.Equals("GP_TextButton", StringComparison.OrdinalIgnoreCase))
            {
                return new UiGPTextButton
                {
                    MessageKey = x.Value("Message"),
                    FileID = x.Value("FileID"),
                    Sprite = x.Int("Sprite"),
                    Sprite1 = x.Int("Sprite1"),
                    PassiveFont = x.Value("PassiveFont"),
                    ActiveFont = x.Value("ActiveFont"),
                    DisabledFont = x.Value("DisabledFont"),
                    Center = x.Bool("Center", false),
                    FontDx = x.Int("FontDx"),
                    FontDy = x.Int("FontDy"),
                    Style = UiTextStyle.Button
                };
            }

            if (tag.Equals("VitButton", StringComparison.OrdinalIgnoreCase))
            {
                int state = x.Int("State");
                return new UiVitButton
                {
                    MessageKey = x.Value("Message"),
                    HintKey = x.Value("Hint"),
                    GP_File = x.Value("GP_File"),
                    State = state,
                    SpritePassive = x.Int("SpritePassive" + state, -1),
                    SpriteActive = x.Int("SpriteOver" + state, -1),
                    SpriteDx = x.Int("SpriteDx" + state),
                    OneSprited = x.Bool("OneSprited", false),
                    DisableCycling = x.Bool("DisableCycling", false),
                    FontPassive = x.Value("FontPassive"),
                    FontOver = x.Value("FontOver"),
                    FontDx = x.Int("FontDx"),
                    FontDy = x.Int("FontDy"),
                    Align = EmptyTo(x.Value("Align"), "Center")
                };
            }

            if (tag.Equals("InputBox", StringComparison.OrdinalIgnoreCase))
            {
                return new UiInputBox
                {
                    MaxLen = x.Int("StrMaxLen", x.Int("MaxLen", 30)),
                    Action = x.Value("Action"),
                    Font = EmptyTo(x.Value("Font"), "BlackFont")
                };
            }

            if (tag.Equals("ComboBox", StringComparison.OrdinalIgnoreCase))
            {
                return new UiComboBox
                {
                    GP_File = x.Value("GP_File"),
                    ActiveFont = x.Value("ActiveFont"),
                    PassiveFont = x.Value("PassiveFont"),
                    FontDx = x.Int("FontDx"),
                    FontDy = x.Int("FontDy"),
                    OneDx = x.Int("OneDx"),
                    OneDy = x.Int("OneDy"),
                    Center = x.Int("Center"),
                    MaxLY = x.Int("MaxLY")
                };
            }

            if (tag.Equals("CheckBox", StringComparison.OrdinalIgnoreCase))
            {
                return new UiCheckBox
                {
                    GP_File = x.Value("GP_File"),
                    State = x.Bool("State", false),
                    GroupIndex = x.Int("GroupIndex")
                };
            }

            if (tag.Equals("VScrollBar", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("HorisontalSlider", StringComparison.OrdinalIgnoreCase))
            {
                return new UiSlider
                {
                    Position = x.Int("SPos"),
                    MaxPosition = Math.Max(1, x.Int("SMaxPos", 100)),
                    SliderPos = x.Int("SliderPos"),
                    GroupIndex = x.Int("GroupIndex"),
                    LineLx = x.Int("LineLx"),
                    LineLy = x.Int("LineLy"),
                    ScrDx = x.Int("ScrDx"),
                    ScrDy = x.Int("ScrDy"),
                    GP_File = x.Value("GP_File")
                };
            }

            if (tag.Equals("ListDesk", StringComparison.OrdinalIgnoreCase))
            {
                var ld = new UiListDesk
                {
                    Border = x.Value("Border"),
                    MarginX = x.Int("marginX", 3),
                    MarginY = x.Int("marginY", 3),
                    Action = x.Value("Action")
                };

                LiteNode element = x.Child("Element");
                LiteNode vit = element?.FirstDescendant("VitButton");
                if (vit != null)
                {
                    int state = vit.Int("State");
                    ld.ElementTemplate = new UiListDeskElement
                    {
                        GP_File = vit.Value("GP_File"),
                        SpritePassive = vit.Int("SpritePassive" + state, -1),
                        SpriteOver = vit.Int("SpriteOver" + state),
                        SpriteSelected = vit.Int("SpritePassive1", 5),
                        Width = vit.Int("Width", 460),
                        Height = vit.Int("Height", 20),
                        FontPassive = EmptyTo(vit.Value("FontPassive"), "BlackFont"),
                        FontOver = EmptyTo(vit.Value("FontOver"), "RedFont"),
                        FontDx = vit.Int("FontDx", 10),
                        FontDy = vit.Int("FontDy"),
                        Align = EmptyTo(vit.Value("Align"), "Left")
                    };
                }
                return ld;
            }

            // These are real menu controls/containers found in the 1.4 XML set.
            // They are preserved instead of silently dropped even where the current
            // Unity renderer does not yet implement a visual adapter for them.
            if (tag.Equals("Window", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("TabDesk", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("ChatDesk", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("Canvas", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("TabButton", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("ColoredBar", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("GP_Button", StringComparison.OrdinalIgnoreCase))
            {
                return new UiGenericNode { Kind = tag };
            }

            return new UiGenericNode { Kind = tag };
        }

        private static void FillCommon(
            UiNode node,
            LiteNode x,
            int absX,
            int absY,
            int localX,
            int localY,
            bool localVisible,
            bool localEnabled,
            bool visible,
            bool enabled,
            int sourceId,
            int parentSourceId,
            int depth)
        {
            node.SourceTag = x.Tag;
            node.SourceId = sourceId;
            node.ParentSourceId = parentSourceId;
            node.Depth = depth;
            node.LocalX = localX;
            node.LocalY = localY;
            node.Name = x.Value("Name");
            node.Hint = x.Value("Hint");
            node.X = absX;
            node.Y = absY;
            node.Width = x.Int("Width");
            node.Height = x.Int("Height");
            node.LocalVisible = localVisible;
            node.LocalEnabled = localEnabled;
            node.Visible = visible;
            node.Enabled = enabled;
            node.DeepColor = x.Bool("DeepColor", false);
            node.HotKey = EmptyTo(x.Value("HotKey"), "NONE");
            node.ColorArgb = ParseArgb(x.Value("Color"), 0xFFFFFFFFu);

            // ParentFrame transform is serialized after the empty <Transform> marker.
            // GPPicture also has an older ScaleX/ScaleY pair before this block, so
            // the LAST ScaleX/ScaleY values are the ParentFrame values used by
            // ParentFrame::GetMatrix in the original engine.
            node.EnableTransform = x.Bool("EnableTransform", false);
            node.PivotPosition = EmptyTo(x.Value("PivotPosition"), "Left");
            node.PivotDx = ParseFloatInvariant(x.Value("PivotDx"), 0f);
            node.PivotDy = ParseFloatInvariant(x.Value("PivotDy"), 0f);
            node.TransformScaleX = ParseFloatInvariant(x.ValueLast("ScaleX"), 1f);
            node.TransformScaleY = ParseFloatInvariant(x.ValueLast("ScaleY"), 1f);
            node.TransformAngle = ParseFloatInvariant(x.Value("Angle"), 0f);
            node.FlipX = x.Bool("FlipX", false);
            node.FlipY = x.Bool("FlipY", false);
        }

        private static float ParseFloatInvariant(string raw, float fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            return float.TryParse(raw.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }

        private static uint ParseArgb(string raw, uint fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            string s = raw.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (uint.TryParse(s, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out uint value))
                return value;
            return fallback;
        }

        private static void FillActions(UiNode node, LiteNode x)
        {
            LiteNode actions = x.Child("v_Actions");
            if (actions == null) return;

            foreach (LiteNode a in actions.Children)
            {
                if (string.IsNullOrWhiteSpace(a.Tag)) continue;
                node.Actions.Add(new UiAction
                {
                    Name = a.Tag,
                    Payload = a.InnerRaw().Trim()
                });
            }

            if (node is UiInputBox ib && string.IsNullOrWhiteSpace(ib.Action) && ib.Actions.Count > 0)
                ib.Action = ib.Actions[0].Name;
        }

        private static bool LooksLikeControl(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            return tag.Equals("DialogsDesk", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("TabDesk", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("ChatDesk", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("Window", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("Canvas", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("BitPicture", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("GPPicture", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("TextButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("GP_TextButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("GP_Button", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("VitButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("InputBox", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("ComboBox", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("CheckBox", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("VScrollBar", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("HorisontalSlider", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("ListDesk", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("TabButton", StringComparison.OrdinalIgnoreCase) ||
                   tag.Equals("ColoredBar", StringComparison.OrdinalIgnoreCase);
        }

        private UiTextStyle DetermineTextStyle(string sourcePath, string messageKey, string passiveFont, int x, int y)
        {
            if (string.Equals(messageKey, "#MAIN_MENU_Window", StringComparison.OrdinalIgnoreCase))
                return UiTextStyle.MainMenuTitle;

            bool options = !string.IsNullOrEmpty(sourcePath) &&
                           sourcePath.IndexOf("M_Options", StringComparison.OrdinalIgnoreCase) >= 0;

            // V390: XML font names are global menu data, not an Options-only hint.
            // M_PROF_ADD commander description explicitly says BlackFont in clean 1.4 XML.
            // V388 ignored that outside M_Options and fell back to the generic decorative font.
            if (options)
            {
                if (SectionTitleKeys.Contains(messageKey ?? string.Empty)) return UiTextStyle.SectionTitle;
                if (OptionLabelKeys.Contains(messageKey ?? string.Empty)) return UiTextStyle.OptionLabel;
                if (WindowTitleKeys.Contains(messageKey ?? string.Empty)) return x > 750 ? UiTextStyle.GoldenTitle : UiTextStyle.WindowTitle;
            }

            string f = passiveFont ?? string.Empty;
            if (f.IndexOf("menutitle2red", StringComparison.OrdinalIgnoreCase) >= 0) return UiTextStyle.SectionTitle;
            if (f.IndexOf("blackfont", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("grayfont", StringComparison.OrdinalIgnoreCase) >= 0) return UiTextStyle.OptionLabel;
            if (f.IndexOf("menutitlewhite", StringComparison.OrdinalIgnoreCase) >= 0) return UiTextStyle.WindowTitle;
            if (f.IndexOf("menugold", StringComparison.OrdinalIgnoreCase) >= 0) return UiTextStyle.GoldenTitle;
            return UiTextStyle.Default;
        }

        private string ReadMenuText(string relativePath, out string sourceKind)
        {
            relativePath = NormalizePath(relativePath);

            // V388 menu XML source policy: when the project contains the bundled
            // clean 1.4 Dialogs set, use it for ALL menu XML.  Resources/images are
            // still loaded through CoreFileSystem/DataRoot.  This prevents mixing a
            // 1.1/modified installed menu XML with the 1.4 port while keeping the
            // original resource installation untouched.
            if (!string.IsNullOrWhiteSpace(_canonicalMenuDataRoot))
            {
                string canonical = ResolveCaseInsensitive(_canonicalMenuDataRoot, relativePath);
                if (!string.IsNullOrEmpty(canonical) && File.Exists(canonical))
                {
                    sourceKind = "StreamingAssets-clean14";
                    return ReadTextEncodingAware(canonical);
                }
            }

            if (_fs.Exists(relativePath))
            {
                sourceKind = "DataRoot";
                return _fs.ReadAllText(relativePath);
            }

            sourceKind = "missing";
            return string.Empty;
        }

        private static string ResolveCaseInsensitive(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
            string[] parts = relativePath.Replace('/', '\\').Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string cur = root;
            foreach (string part in parts)
            {
                string direct = Path.Combine(cur, part);
                if (File.Exists(direct) || Directory.Exists(direct))
                {
                    cur = direct;
                    continue;
                }

                if (!Directory.Exists(cur)) return string.Empty;
                string match = Directory.EnumerateFileSystemEntries(cur)
                    .FirstOrDefault(p => string.Equals(Path.GetFileName(p), part, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(match)) return string.Empty;
                cur = match;
            }
            return cur;
        }

        private static string ReadTextEncodingAware(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch
            {
                try { return Encoding.GetEncoding(1251).GetString(bytes); }
                catch { return Encoding.ASCII.GetString(bytes); }
            }
        }

        private static string NormalizePath(string target)
        {
            target = (target ?? string.Empty).Trim().Replace('/', '\\');
            if (target.StartsWith("dialogs\\", StringComparison.OrdinalIgnoreCase))
                target = "Dialogs\\" + target.Substring("dialogs\\".Length);
            return target;
        }

        private static string EmptyTo(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

        // ------------------------------------------------------------------
        // Tiny balanced-tag tree for the original XML-like syntax.
        // ------------------------------------------------------------------
        private sealed class LiteNode
        {
            public string Tag = string.Empty;
            public string Source = string.Empty;
            public int ContentStart;
            public int ContentEnd;
            public readonly List<LiteNode> Children = new();

            public LiteNode Child(string tag)
            {
                return Children.FirstOrDefault(n => n.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
            }

            public LiteNode FirstDescendant(string tag)
            {
                foreach (LiteNode c in Children)
                {
                    if (c.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)) return c;
                    LiteNode d = c.FirstDescendant(tag);
                    if (d != null) return d;
                }
                return null;
            }

            public string DirectText()
            {
                if (ContentEnd <= ContentStart || string.IsNullOrEmpty(Source)) return string.Empty;
                string raw = Source.Substring(ContentStart, ContentEnd - ContentStart);
                int lt = raw.IndexOf('<');
                if (lt >= 0) raw = raw.Substring(0, lt);
                return raw.Trim();
            }

            public string InnerRaw()
            {
                if (ContentEnd <= ContentStart || string.IsNullOrEmpty(Source)) return string.Empty;
                return Source.Substring(ContentStart, ContentEnd - ContentStart);
            }

            public string Value(string tag)
            {
                LiteNode n = Child(tag);
                return n?.DirectText() ?? string.Empty;
            }

            public string ValueLast(string tag)
            {
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    LiteNode n = Children[i];
                    if (n != null && n.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                        return n.DirectText();
                }
                return string.Empty;
            }

            public int Int(string tag, int fallback = 0)
            {
                string s = Value(tag);
                return int.TryParse((s ?? string.Empty).Trim(), out int v) ? v : fallback;
            }

            public bool Bool(string tag, bool fallback)
            {
                string s = Value(tag);
                if (string.IsNullOrWhiteSpace(s)) return fallback;
                s = s.Trim();
                return s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s == "1";
            }
        }

        private static class LiteDocument
        {
            public static LiteNode Parse(string source)
            {
                source ??= string.Empty;
                var root = new LiteNode { Tag = "#document", Source = source, ContentStart = 0, ContentEnd = source.Length };
                var stack = new Stack<LiteNode>();
                stack.Push(root);

                int pos = 0;
                while (pos < source.Length)
                {
                    int lt = source.IndexOf('<', pos);
                    if (lt < 0) break;
                    int gt = source.IndexOf('>', lt + 1);
                    if (gt < 0) break;

                    string token = source.Substring(lt + 1, gt - lt - 1).Trim();
                    pos = gt + 1;
                    if (token.Length == 0) continue;       // original empty <> wrapper
                    if (token.StartsWith("?", StringComparison.Ordinal) || token.StartsWith("!", StringComparison.Ordinal)) continue;

                    // Cossacks II DialogsSystem files use a non-standard empty
                    // wrapper pair: <> ... </>.  The closing wrapper token is
                    // exactly "/".  V388 evaluated selfClosing before removing
                    // the leading slash, then attempted Substring(0, -1).
                    // Treat empty wrappers as structural no-ops and never apply
                    // self-closing handling to a closing tag.
                    bool closing = token.StartsWith("/", StringComparison.Ordinal);
                    if (closing)
                    {
                        token = token.Substring(1).Trim();
                        if (token.Length == 0) continue; // original </> wrapper
                    }

                    bool selfClosing = !closing && token.EndsWith("/", StringComparison.Ordinal);
                    if (selfClosing)
                    {
                        token = token.Substring(0, token.Length - 1).Trim();
                        if (token.Length == 0) continue;
                    }

                    // The original files have no XML attributes on control tags.
                    // Keep odd tag names such as "Position&Width" intact.
                    string tag = token;

                    if (closing)
                    {
                        LiteNode match = null;
                        while (stack.Count > 1)
                        {
                            LiteNode top = stack.Pop();
                            top.ContentEnd = lt;
                            if (top.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                            {
                                match = top;
                                break;
                            }
                        }
                        if (match == null)
                        {
                            // Tolerate malformed/unknown closing tags; the source game
                            // reader is permissive as well.
                        }
                        continue;
                    }

                    var node = new LiteNode
                    {
                        Tag = tag,
                        Source = source,
                        ContentStart = gt + 1,
                        ContentEnd = gt + 1
                    };
                    stack.Peek().Children.Add(node);
                    if (!selfClosing)
                        stack.Push(node);
                }

                while (stack.Count > 1)
                {
                    LiteNode n = stack.Pop();
                    n.ContentEnd = source.Length;
                }
                return root;
            }
        }
    }
}
