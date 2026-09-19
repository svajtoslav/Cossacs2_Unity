using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cossacks2Bridge.Core;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// V395J: generic source model for ListDesk.Element.
    ///
    /// Cossacks II does not treat ListDesk/Element as a visible child.  Element is
    /// a prototype owned by ListDesk and ListDesk::AddElement clones that prototype
    /// at runtime (Dialogs.cpp).  The old Menu14 flattened model does not expose
    /// this prototype, so this class restores that missing model layer once per
    /// screen from the SAME source DialogsSystem XML.  It is deliberately generic:
    /// there is no profile-specific geometry, font, GP or sprite constant here.
    ///
    /// The original GSC XML dialect is not standards-compliant XML (for example
    /// tags such as Position&Width are legal in it), therefore System.Xml cannot
    /// parse the files verbatim.  The small stack parser below mirrors the game's
    /// tolerant xmlQuote tree semantics for tags/text and preserves such names.
    /// </summary>
    public static class ListDeskSourceRuntime14
    {
        public sealed class TemplateSpec
        {
            public int SourceId;
            public string SourcePath = string.Empty;
            public string BorderName = string.Empty;
            public float MarginX;
            public float MarginY;
            public float BorderLeftMargin;
            public float BorderTopMargin;
            public float BorderRightMargin;
            public float BorderBottomMargin;

            // StdBorder fields from Dialogs/borders.xml. V395K uses the exact
            // source border definition instead of renderer-side BD constants.
            public string BorderGPFile = string.Empty;
            public int BorderLeftTop = -1;
            public int BorderRightTop = -1;
            public int BorderLeftBottom = -1;
            public int BorderRightBottom = -1;
            public int BorderTopLine = -1;
            public int BorderBottomLine = -1;
            public int BorderLeftLine = -1;
            public int BorderRightLine = -1;
            public int BorderNFillers;
            public int BorderStartFiller = -1;
            public string VScrollerGPFile = string.Empty;
            public int VScrollerDXRight;
            public int VScrollerDYTop;
            public int VScrolledDYBottom;

            public bool EnableVerticalScroller;
            public bool HideVScroller;
            public bool EnableHorizontalScroller;
            public bool EnableMouseShift;
            public int XShift;
            public int YShift;
            public int CurrentElement;

            // ListDesk.Element/VitButton fields copied by original ListDesk::AddElement.
            public bool HasVitButtonPrototype;
            public float PrototypeX;
            public float PrototypeY;
            public float Width;
            public float Height;
            public string FontPassive = string.Empty;
            public string FontOver = string.Empty;
            public int FontDx;
            public int FontDy;
            public string Align = "Left";
            public bool OneSprited;
            public bool DisableCycling;
            public string GPFile = string.Empty;
            public readonly int[] SpritePassive = { -1, -1, -1 };
            public readonly int[] SpriteOver = { -1, -1, -1 };
            public readonly int[] SpriteDx = { 0, 0, 0 };
            public string Hint = string.Empty;
            public string HotKey = string.Empty;
            public readonly List<string> PrototypeActions = new List<string>();
            public string MatchAction = string.Empty;

            public float OriginalButtonWidth(float listDeskWidth)
            {
                // Dialogs.cpp::ListDesk::_Draw:
                // ButW = GetWidth() - (marginX << 1) - l - r;
                return Math.Max(1f, listDeskWidth - MarginX * 2f - BorderLeftMargin - BorderRightMargin);
            }

            public float OriginalRowY(int index)
            {
                // yy = marginY; yy += SD->GetHeight() + marginY;
                return MarginY + Math.Max(0, index) * (Height + MarginY);
            }
        }

        private sealed class BorderSpec
        {
            public string Name = string.Empty;
            public string GPFile = string.Empty;
            public int LeftTop = -1;
            public int RightTop = -1;
            public int LeftBottom = -1;
            public int RightBottom = -1;
            public int TopLine = -1;
            public int BottomLine = -1;
            public int LeftLine = -1;
            public int RightLine = -1;
            public float LeftMargin;
            public float TopMargin;
            public float RightMargin;
            public float BottomMargin;
            public int NFillers;
            public int StartFiller = -1;
            public string VScrollerGPFile = string.Empty;
            public int VScrollerDXRight;
            public int VScrollerDYTop;
            public int VScrolledDYBottom;
        }

        private sealed class RawNode
        {
            public string Name = string.Empty;
            public string Text = string.Empty;
            public RawNode Parent;
            public readonly List<RawNode> Children = new List<RawNode>();

            public RawNode Child(string name)
            {
                for (int i = 0; i < Children.Count; i++)
                    if (string.Equals(Children[i].Name, name, StringComparison.OrdinalIgnoreCase))
                        return Children[i];
                return null;
            }

            public string Value(string name, string fallback = "")
            {
                RawNode c = Child(name);
                if (c == null) return fallback;
                string s = (c.Text ?? string.Empty).Trim();
                return s.Length == 0 ? fallback : s;
            }
        }

        private static readonly Dictionary<int, TemplateSpec> s_bySourceId = new Dictionary<int, TemplateSpec>();
        private static string s_screenKey = string.Empty;

        public static void BeginScreen(UiDesk desk)
        {
            s_bySourceId.Clear();
            s_screenKey = desk?.SourcePath ?? string.Empty;
            if (desk?.Children == null || string.IsNullOrEmpty(desk.SourcePath)) return;

            // V395Q: do not re-read and loose-parse the entire DialogsSystem XML
            // on screens that contain no ListDesk at all. EW2_CampaignStats is a
            // large file and has zero UiListDesk nodes, so the old path duplicated
            // a full source parse for no result.
            bool hasListDeskV395Q = false;
            for (int i = 0; i < desk.Children.Count; i++)
            {
                if (desk.Children[i] is UiListDesk)
                {
                    hasListDeskV395Q = true;
                    break;
                }
            }
            if (!hasListDeskV395Q) return;

            string xml = Menu14ActionStateRuntime.ReadSourceText(desk.SourcePath, Cp1251());
            if (string.IsNullOrEmpty(xml)) return;

            RawNode root = ParseLooseXml(xml);
            if (root == null) return;

            var sourceLists = new List<RawNode>();
            Collect(root, "ListDesk", sourceLists);
            if (sourceLists.Count == 0) return;

            Dictionary<string, BorderSpec> borders = ParseBorders();
            var used = new HashSet<RawNode>();

            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiListDesk ld = desk.Children[i] as UiListDesk;
                if (ld == null) continue;

                RawNode best = null;
                int bestScore = int.MinValue;
                for (int j = 0; j < sourceLists.Count; j++)
                {
                    RawNode candidate = sourceLists[j];
                    if (used.Contains(candidate)) continue;
                    int score = Score(ld, candidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }

                if (best == null || bestScore < 20) continue;
                TemplateSpec spec = BuildSpec(ld, best, borders);
                if (spec == null) continue;

                used.Add(best);
                s_bySourceId[ld.SourceId] = spec;

                Debug.Log(
                    $"[C2:LISTDESK V395K] parsed sourceId={ld.SourceId} action='{spec.MatchAction}' " +
                    $"list=({ld.X:0.#},{ld.Y:0.#},{ld.Width:0.#},{ld.Height:0.#}) " +
                    $"border='{spec.BorderName}' margins=({spec.BorderLeftMargin:0.#},{spec.BorderTopMargin:0.#},{spec.BorderRightMargin:0.#},{spec.BorderBottomMargin:0.#}) " +
                    $"element=VitButton {spec.Width:0.#}x{spec.Height:0.#} gp='{spec.GPFile}' " +
                    $"font='{spec.FontPassive}/{spec.FontOver}' state0={spec.SpritePassive[0]}/{spec.SpriteOver[0]} state1={spec.SpritePassive[1]}/{spec.SpriteOver[1]}");
                Debug.Log(
                    $"[C2:LISTDESK BORDER V395K] sourceId={ld.SourceId} gp='{spec.BorderGPFile}' " +
                    $"corners={spec.BorderLeftTop}/{spec.BorderRightTop}/{spec.BorderLeftBottom}/{spec.BorderRightBottom} " +
                    $"lines={spec.BorderTopLine}/{spec.BorderBottomLine}/{spec.BorderLeftLine}/{spec.BorderRightLine} " +
                    $"fill={spec.BorderStartFiller}+{spec.BorderNFillers} " +
                    $"vscroll='{spec.VScrollerGPFile}' dxRight={spec.VScrollerDXRight} dyTop={spec.VScrollerDYTop} dyBottom={spec.VScrolledDYBottom}");
            }
        }

        public static bool TryGet(int sourceId, out TemplateSpec spec)
        {
            return s_bySourceId.TryGetValue(sourceId, out spec) && spec != null;
        }

        public static string CurrentScreenKey => s_screenKey;

        /// <summary>
        /// V396A7R2: expose an exact StdBorder definition from Dialogs/borders.xml
        /// for ordinary DialogsDesk rendering. No renderer-side frame constants.
        /// </summary>
        public static bool TryGetBorderTemplate(string borderName, out TemplateSpec spec)
        {
            spec = null;
            if (string.IsNullOrWhiteSpace(borderName) ||
                borderName.Equals("NullBorder", StringComparison.OrdinalIgnoreCase))
                return false;

            Dictionary<string, BorderSpec> borders = ParseBorders();
            if (!borders.TryGetValue(borderName, out BorderSpec b) || b == null)
                return false;

            spec = new TemplateSpec
            {
                BorderName = borderName,
                BorderLeftMargin = b.LeftMargin,
                BorderTopMargin = b.TopMargin,
                BorderRightMargin = b.RightMargin,
                BorderBottomMargin = b.BottomMargin,
                BorderGPFile = b.GPFile,
                BorderLeftTop = b.LeftTop,
                BorderRightTop = b.RightTop,
                BorderLeftBottom = b.LeftBottom,
                BorderRightBottom = b.RightBottom,
                BorderTopLine = b.TopLine,
                BorderBottomLine = b.BottomLine,
                BorderLeftLine = b.LeftLine,
                BorderRightLine = b.RightLine,
                BorderNFillers = b.NFillers,
                BorderStartFiller = b.StartFiller,
                VScrollerGPFile = b.VScrollerGPFile,
                VScrollerDXRight = b.VScrollerDXRight,
                VScrollerDYTop = b.VScrollerDYTop,
                VScrolledDYBottom = b.VScrolledDYBottom
            };
            return true;
        }

        private static TemplateSpec BuildSpec(UiListDesk ld, RawNode list, Dictionary<string, BorderSpec> borders)
        {
            RawNode element = list.Child("Element");
            RawNode vb = element != null ? element.Child("VitButton") : null;

            var spec = new TemplateSpec
            {
                SourceId = ld.SourceId,
                SourcePath = s_screenKey,
                BorderName = list.Value("Border", string.Empty),
                MarginX = Float(list.Value("marginX", "0"), 0f),
                MarginY = Float(list.Value("marginY", "0"), 0f),
                EnableVerticalScroller = Bool(list.Value("EnableVerticalScroller", "false"), false),
                HideVScroller = Bool(list.Value("HideVScroller", "false"), false),
                EnableHorizontalScroller = Bool(list.Value("EnableHorizontalScroller", "false"), false),
                EnableMouseShift = Bool(list.Value("EnableMouseShift", "false"), false),
                XShift = Int(list.Value("XShift", "0"), 0),
                YShift = Int(list.Value("YShift", "0"), 0),
                CurrentElement = Int(list.Value("CurrentElement", "-1"), -1),

                HasVitButtonPrototype = vb != null,
                PrototypeX = vb != null ? Float(vb.Value("x", "0"), 0f) : 0f,
                PrototypeY = vb != null ? Float(vb.Value("y", "0"), 0f) : 0f,
                Width = vb != null ? Float(vb.Value("Width", "0"), 0f) : 0f,
                Height = vb != null ? Float(vb.Value("Height", "0"), 0f) : 0f,
                FontPassive = vb != null ? vb.Value("FontPassive", string.Empty) : string.Empty,
                FontOver = vb != null ? vb.Value("FontOver", string.Empty) : string.Empty,
                FontDx = vb != null ? Int(vb.Value("FontDx", "0"), 0) : 0,
                FontDy = vb != null ? Int(vb.Value("FontDy", "0"), 0) : 0,
                Align = vb != null ? vb.Value("Align", "Left") : "Left",
                OneSprited = vb != null && Bool(vb.Value("OneSprited", "false"), false),
                DisableCycling = vb != null && Bool(vb.Value("DisableCycling", "false"), false),
                GPFile = vb != null ? vb.Value("GP_File", string.Empty) : string.Empty,
                Hint = vb != null ? vb.Value("Hint", string.Empty) : string.Empty,
                HotKey = vb != null ? vb.Value("HotKey", string.Empty) : string.Empty,
                MatchAction = FirstAction(list)
            };

            if (vb != null)
            {
                for (int st = 0; st < 3; st++)
                {
                    spec.SpritePassive[st] = Int(vb.Value("SpritePassive" + st, "-1"), -1);
                    spec.SpriteOver[st] = Int(vb.Value("SpriteOver" + st, "-1"), -1);
                    spec.SpriteDx[st] = Int(vb.Value("SpriteDx" + st, "0"), 0);
                }

                RawNode va = vb.Child("v_Actions");
                if (va != null)
                {
                    for (int i = 0; i < va.Children.Count; i++)
                        if (!string.IsNullOrEmpty(va.Children[i].Name)) spec.PrototypeActions.Add(va.Children[i].Name);
                }
            }

            if (borders != null && !string.IsNullOrEmpty(spec.BorderName) && borders.TryGetValue(spec.BorderName, out BorderSpec b) && b != null)
            {
                spec.BorderLeftMargin = b.LeftMargin;
                spec.BorderTopMargin = b.TopMargin;
                spec.BorderRightMargin = b.RightMargin;
                spec.BorderBottomMargin = b.BottomMargin;
                spec.BorderGPFile = b.GPFile;
                spec.BorderLeftTop = b.LeftTop;
                spec.BorderRightTop = b.RightTop;
                spec.BorderLeftBottom = b.LeftBottom;
                spec.BorderRightBottom = b.RightBottom;
                spec.BorderTopLine = b.TopLine;
                spec.BorderBottomLine = b.BottomLine;
                spec.BorderLeftLine = b.LeftLine;
                spec.BorderRightLine = b.RightLine;
                spec.BorderNFillers = b.NFillers;
                spec.BorderStartFiller = b.StartFiller;
                spec.VScrollerGPFile = b.VScrollerGPFile;
                spec.VScrollerDXRight = b.VScrollerDXRight;
                spec.VScrollerDYTop = b.VScrollerDYTop;
                spec.VScrolledDYBottom = b.VScrolledDYBottom;
            }

            // A ListDesk without an Element prototype still needs its source Border
            // and scroller model for generic rendering. Profile runtime separately
            // checks HasVitButtonPrototype before calling AddElement semantics.
            return spec;
        }

        private static int Score(UiListDesk ld, RawNode n)
        {
            int score = 0;
            float x = Float(n.Value("x", "-99999"), -99999f);
            float y = Float(n.Value("y", "-99999"), -99999f);
            float w = Float(n.Value("Width", "-99999"), -99999f);
            float h = Float(n.Value("Height", "-99999"), -99999f);
            if (Near(x, ld.X)) score += 12;
            if (Near(y, ld.Y)) score += 12;
            if (Near(w, ld.Width)) score += 12;
            if (Near(h, ld.Height)) score += 12;

            var srcActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            RawNode va = n.Child("v_Actions");
            if (va != null)
                for (int i = 0; i < va.Children.Count; i++) srcActions.Add(va.Children[i].Name);

            if (ld.Actions != null)
            {
                for (int i = 0; i < ld.Actions.Count; i++)
                {
                    string a = ld.Actions[i]?.Name ?? string.Empty;
                    if (!string.IsNullOrEmpty(a) && srcActions.Contains(a)) score += 100;
                }
            }
            return score;
        }

        private static string FirstAction(RawNode list)
        {
            RawNode va = list?.Child("v_Actions");
            if (va == null || va.Children.Count == 0) return string.Empty;
            return va.Children[0].Name ?? string.Empty;
        }

        private static Dictionary<string, BorderSpec> ParseBorders()
        {
            var result = new Dictionary<string, BorderSpec>(StringComparer.OrdinalIgnoreCase);
            string raw = Menu14ActionStateRuntime.ReadSourceText(@"Dialogs\borders.xml", Cp1251());
            if (string.IsNullOrEmpty(raw)) return result;
            RawNode root = ParseLooseXml(raw);
            if (root == null) return result;
            var nodes = new List<RawNode>();
            Collect(root, "StdBorder", nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                RawNode n = nodes[i];
                string name = n.Value("Name", string.Empty);
                if (string.IsNullOrEmpty(name)) continue;
                result[name] = new BorderSpec
                {
                    Name = name,
                    GPFile = n.Value("GP_File", string.Empty),
                    LeftTop = Int(n.Value("LeftTop", "-1"), -1),
                    RightTop = Int(n.Value("RightTop", "-1"), -1),
                    LeftBottom = Int(n.Value("LeftBottom", "-1"), -1),
                    RightBottom = Int(n.Value("RightBottom", "-1"), -1),
                    TopLine = Int(n.Value("TopLine", "-1"), -1),
                    BottomLine = Int(n.Value("BottomLine", "-1"), -1),
                    LeftLine = Int(n.Value("LeftLine", "-1"), -1),
                    RightLine = Int(n.Value("RightLine", "-1"), -1),
                    LeftMargin = Float(n.Value("LeftMargin", "0"), 0f),
                    TopMargin = Float(n.Value("TopMargin", "0"), 0f),
                    RightMargin = Float(n.Value("RightMargin", "0"), 0f),
                    BottomMargin = Float(n.Value("BottomMargin", "0"), 0f),
                    NFillers = Int(n.Value("NFillers", "0"), 0),
                    StartFiller = Int(n.Value("StartFiller", "-1"), -1),
                    VScrollerGPFile = n.Value("VScroller_GP_File", string.Empty),
                    VScrollerDXRight = Int(n.Value("VScroller_DX_right", "0"), 0),
                    VScrollerDYTop = Int(n.Value("VScroller_DY_top", "0"), 0),
                    VScrolledDYBottom = Int(n.Value("VScrolled_DY_bottom", "0"), 0)
                };
            }
            return result;
        }

        private static void Collect(RawNode n, string name, List<RawNode> output)
        {
            if (n == null || output == null) return;
            if (string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase)) output.Add(n);
            for (int i = 0; i < n.Children.Count; i++) Collect(n.Children[i], name, output);
        }

        private static RawNode ParseLooseXml(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            var root = new RawNode { Name = "__ROOT__" };
            var stack = new Stack<RawNode>();
            stack.Push(root);
            int p = 0;
            while (p < raw.Length)
            {
                int lt = raw.IndexOf('<', p);
                if (lt < 0)
                {
                    AppendText(stack.Peek(), raw.Substring(p));
                    break;
                }
                if (lt > p) AppendText(stack.Peek(), raw.Substring(p, lt - p));

                if (raw.IndexOf("<!--", lt, StringComparison.Ordinal) == lt)
                {
                    int ce = raw.IndexOf("-->", lt + 4, StringComparison.Ordinal);
                    p = ce >= 0 ? ce + 3 : raw.Length;
                    continue;
                }

                int gt = raw.IndexOf('>', lt + 1);
                if (gt < 0) break;
                string token = raw.Substring(lt + 1, gt - lt - 1).Trim();
                p = gt + 1;
                if (token.Length == 0 || token[0] == '?' || token[0] == '!') continue;

                bool closing = token[0] == '/';
                if (closing)
                {
                    string closeName = token.Substring(1).Trim();
                    while (stack.Count > 1)
                    {
                        RawNode top = stack.Pop();
                        if (string.Equals(top.Name, closeName, StringComparison.OrdinalIgnoreCase)) break;
                    }
                    continue;
                }

                bool selfClosing = token.EndsWith("/", StringComparison.Ordinal);
                if (selfClosing) token = token.Substring(0, token.Length - 1).TrimEnd();
                int ws = 0;
                while (ws < token.Length && !char.IsWhiteSpace(token[ws])) ws++;
                string name = token.Substring(0, ws);
                if (name.Length == 0) continue;

                var node = new RawNode { Name = name, Parent = stack.Peek() };
                stack.Peek().Children.Add(node);
                if (!selfClosing) stack.Push(node);
            }
            return root;
        }

        private static void AppendText(RawNode node, string text)
        {
            if (node == null || string.IsNullOrEmpty(text)) return;
            node.Text += text;
        }

        private static bool Near(float a, float b) => Math.Abs(a - b) < 0.01f;
        private static int Int(string s, int fallback) => int.TryParse((s ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
        private static float Float(string s, float fallback) => float.TryParse((s ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        private static bool Bool(string s, bool fallback) => bool.TryParse((s ?? string.Empty).Trim(), out bool v) ? v : fallback;
        private static Encoding Cp1251() { try { return Encoding.GetEncoding(1251); } catch { return Encoding.UTF8; } }
    }
}
