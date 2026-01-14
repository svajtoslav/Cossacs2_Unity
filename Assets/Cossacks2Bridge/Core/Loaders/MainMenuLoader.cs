using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// Загрузчик главного меню (Main, Single, Network и т.д.)
    /// </summary>
    public sealed class MainMenuLoader
    {
        private readonly CoreFileSystem _fs;

        public MainMenuLoader(CoreFileSystem fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public bool CanHandle(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId)) return false;

            if (screenId.Equals("Options", StringComparison.OrdinalIgnoreCase)) return false;
            if (screenId.StartsWith("Options_", StringComparison.OrdinalIgnoreCase)) return false;
            if (screenId.StartsWith("Options/", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }
        private static string RemoveSimpleTagBlocks(string text, string tag)
        {
            if (string.IsNullOrEmpty(text)) return text;

            char[] buf = text.ToCharArray();
            int i = 0;

            while (i < text.Length)
            {
                int open = text.IndexOf("<" + tag + ">", i, StringComparison.OrdinalIgnoreCase);
                if (open < 0) break;

                int close = text.IndexOf("</" + tag + ">", open, StringComparison.OrdinalIgnoreCase);
                if (close < 0) break;

                int end = close + tag.Length + 3; // </Tag>
                for (int k = open; k < end; k++) buf[k] = ' ';

                i = end;
            }

            return new string(buf);
        }
        public UiDesk LoadScreen(string screenId)
        {
            string routerPath = @"Dialogs\MainMenu.xml";
            if (!_fs.Exists(routerPath))
            {
                UnityEngine.Debug.LogWarning($"[MainMenuLoader] Router not found: {routerPath}");
                return new UiDesk { SourcePath = $"[missing:{routerPath}]" };
            }

            string router = _fs.ReadAllText(routerPath);
            string target = ExtractTagValue(router, screenId);

            if (string.IsNullOrWhiteSpace(target))
            {
                target = @"dialogs\v\M_Main.DialogsSystem.xml";

            }

            target = NormalizePath(target);
            return LoadDesk(target);
        }

        private UiDesk LoadDesk(string dialogsSystemPath)
        {
            if (!_fs.Exists(dialogsSystemPath))
            {
                UnityEngine.Debug.LogWarning($"[MainMenuLoader] File not found: {dialogsSystemPath}");
                return new UiDesk { SourcePath = dialogsSystemPath };
            }

            string text = _fs.ReadAllText(dialogsSystemPath);
            var desk = new UiDesk { SourcePath = dialogsSystemPath };

            var windows = ExtractTopLevelWindowSpans(text);
            string textWithoutWindows = RemoveRanges(text, windows);

            ParseElements(textWithoutWindows, 0, 0, desk);

            foreach (var w in windows)
            {
                int wx = ToInt(GetLast(w.Content, "x"));
                int wy = ToInt(GetLast(w.Content, "y"));
                ParseElements(w.Content, wx, wy, desk);
            }

            return desk;
        }

        private void ParseElements(string region, int baseX, int baseY, UiDesk desk)
        {
            if (string.IsNullOrEmpty(region)) return;

            string region2 = region;

            // ─────────────────────────────────────────────────────────────
            // 1) DialogsDesk рекурсивно (с учётом tail x/y!)
            foreach (var cb in FindCompositeBlocks(region2, "DialogsDesk"))
            {
                // coords (x/y/Width/Height) у DialogsDesk лежат в хвосте после закрывающего тега
                int tx = ToInt(Get(cb.Tail, "x"));
                int ty = ToInt(Get(cb.Tail, "y"));

                // внутренний dx/dy (если вдруг используется в каких-то диалогах)
                int dx = ToInt(Get(cb.Inner, "dx"));
                int dy = ToInt(Get(cb.Inner, "dy"));

                // если у Desk есть Border != NullBorder — создаём отдельный узел, чтобы рендерить рамку/зону
                string border = Get(cb.Inner, "Border") ?? "";
                bool hasBorder = !string.IsNullOrEmpty(border) &&
                                 border.IndexOf("NullBorder", StringComparison.OrdinalIgnoreCase) < 0;

                if (hasBorder)
                {
                    var dd = new UiDialogsDesk();
                    FillCommon(dd, cb.Tail, baseX, baseY);
                    dd.Border = border;
                    desk.Children.Add(dd);

                    ParseElements(cb.Inner, baseX + tx + dx, baseY + ty + dy, desk);
                }
                else
                {
                    ParseElements(cb.Inner, baseX + tx + dx, baseY + ty + dy, desk);
                }
            }

            // вырезаем DialogsDesk чтобы не распарсить второй раз
            region2 = RemoveSimpleTagBlocks(region2, "DialogsDesk");

 

            // ─────────────────────────────────────────────────────────────
            // 2) ListDesk как composite (и сразу вырезаем, чтобы шаблонный VitButton не попал в общий парсинг)
            // ─────────────────────────────────────────────────────────────
            foreach (var cb in FindCompositeBlocks(region2, "ListDesk"))
            {
                var ld = new UiListDesk();

                // coords/size/flags идут в хвосте (после закрывающего тега)
                FillCommon(ld, cb.Tail, baseX, baseY);

                // Border + Element template живут внутри тега
                ld.Border = Get(cb.Inner, "Border") ?? "";

                string element = GetRawInner(cb.Inner, "Element") ?? "";
                if (!string.IsNullOrWhiteSpace(element))
                {
                    string vb = GetRawInner(element, "VitButton") ?? "";
                    if (!string.IsNullOrWhiteSpace(vb))
                    {
                        var e = new UiListDeskElement();
                        e.GP_File = Get(vb, "GP_File") ?? "";

                        e.SpritePassive = ToInt(Get(vb, "SpritePassive"));
                        e.SpriteOver = ToInt(Get(vb, "SpriteOver"));
                        e.SpriteSelected = ToInt(Get(vb, "SpriteSelected"));

                        e.Width = ToInt(Get(vb, "Width"));
                        e.Height = ToInt(Get(vb, "Height"));

                        e.FontPassive = Get(vb, "FontPassive") ?? "";
                        e.FontOver = Get(vb, "FontOver") ?? "";

                        e.FontDx = ToInt(Get(vb, "FontDx"));
                        e.FontDy = ToInt(Get(vb, "FontDy"));
                        e.Align = Get(vb, "Align") ?? "";

                        ld.ElementTemplate = e;
                    }
                }

                desk.Children.Add(ld);
            }
            // Удаляем ListDesk из текста
            region2 = RemoveCompositeTagBlocks(region2, "ListDesk");

            // ─────────────────────────────────────────────────────────────
            // Дальше ПАРСИМ ТОЛЬКО region2 (очищенный от контейнеров)
            // ─────────────────────────────────────────────────────────────

            // BitPicture
            foreach (string block in FindBlocks(region2, "BitPicture"))
            {
                var pic = new UiBitPicture();
                FillCommon(pic, block, baseX, baseY);
                pic.FileName = Get(block, "FileName") ?? "";
                desk.Children.Add(pic);
            }

            // GPPicture
            foreach (var b in FindBlocks(region2, "GPPicture"))
            {
                var gp = new UiGPPicture();
                FillCommon(gp, b, baseX, baseY);
                gp.FileID = Get(b, "FileID") ?? "";
                gp.SpriteID = ToInt(Get(b, "SpriteID"));

                // FIX(AddProfile): portrait frame + portrait image exact positions
                if (gp.FileID.IndexOf(@"INTERF3\ELEMENTS\PORTRAITS_BORDER", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    gp.X = 426;
                    gp.Y = 418;
                    gp.Width = 119;
                    gp.Height = 132;
                }
                if (gp.FileID.IndexOf(@"Interf3\TotalWarGraph\lva_EGs", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    gp.X = 430;
                    gp.Y = 421;
                    gp.Width = 111;
                    gp.Height = 124;
                }

                desk.Children.Add(gp);
            }

            // TextButton
            foreach (string block in FindBlocks(region2, "TextButton"))
            {
                var btn = new UiTextButton();
                FillCommon(btn, block, baseX, baseY);
                btn.MessageKey = Get(block, "Message") ?? "";
                btn.HintKey = Get(block, "Hint") ?? "";
                btn.PassiveFont = Get(block, "PassiveFont") ?? "";
                btn.ActiveFont = Get(block, "ActiveFont") ?? "";
                btn.DisabledFont = Get(block, "DisabledFont") ?? "";

                // Для главного меню НЕ назначаем стиль - используем Default
                btn.Style = DetermineMainMenuStyle(btn.MessageKey, btn.PassiveFont);

                FillActions(btn, block);
                desk.Children.Add(btn);
            }

            // InputBox
            foreach (var b in FindBlocks(region2, "InputBox"))
            {
                var ib = new UiInputBox();
                FillCommon(ib, b, baseX, baseY);
                ib.MaxLen = ToInt(Get(b, "MaxLen"));
                ib.Action = Get(b, "Action") ?? "";
                ib.Font = Get(b, "Font") ?? "";

                // FIX(AddProfile): nickname input exact position
                if (!string.IsNullOrEmpty(ib.Action) &&
                    ib.Action.IndexOf("cva_ProfAdd_Name", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ib.X = 573;
                    ib.Y = 286;
                    ib.Width = 280;
                    ib.Height = 20;
                }

                desk.Children.Add(ib);
            }

            // ComboBox
            foreach (var b in FindBlocks(region2, "ComboBox"))
            {
                var cbx = new UiComboBox();
                FillCommon(cbx, b, baseX, baseY);
                cbx.GP_File = Get(b, "GP_File") ?? "";
                cbx.ActiveFont = Get(b, "ActiveFont") ?? "";
                cbx.PassiveFont = Get(b, "PassiveFont") ?? "";
                cbx.FontDx = ToInt(Get(b, "FontDx"));
                cbx.FontDy = ToInt(Get(b, "FontDy"));
                cbx.OneDx = ToInt(Get(b, "OneDx"));
                cbx.OneDy = ToInt(Get(b, "OneDy"));
                cbx.Center = ToInt(Get(b, "Center"));
                cbx.MaxLY = ToInt(Get(b, "MaxLY"));
                FillActions(cbx, b);
                desk.Children.Add(cbx);
            }

            // VitButton (только реальный, не из ListDesk, т.к. ListDesk уже вырезан)
            // VitButton (реальный)
            foreach (var b in FindBlocks(region2, "VitButton"))
            {
                var vb = new UiVitButton();
                FillCommon(vb, b, baseX, baseY);

                vb.GP_File = Get(b, "GP_File") ?? "";
                vb.SpritePassive = ToInt(Get(b, "SpritePassive"));
                vb.SpriteActive = ToInt(Get(b, "SpriteActive"));
                vb.OneSprited = ToBool(Get(b, "OneSprited"), false);

                FillActions(vb, b);
                desk.Children.Add(vb);
            }

            // Text (плоский)
            foreach (string flat in FindFlatBlocks(region2, "Text"))
            {
                var btn = new UiTextButton();
                FillCommon(btn, flat, baseX, baseY);
                btn.MessageKey = Get(flat, "Message") ?? "";
                btn.HintKey = Get(flat, "Hint") ?? "";
                btn.PassiveFont = Get(flat, "PassiveFont") ?? "";
                btn.ActiveFont = Get(flat, "ActiveFont") ?? "";
                btn.DisabledFont = Get(flat, "DisabledFont") ?? "";

                if (btn.Width <= 0) btn.Width = 600;
                if (btn.Height <= 0) btn.Height = 40;
                btn.Visible = true;

                btn.Style = DetermineMainMenuStyle(btn.MessageKey, btn.PassiveFont);

                FillActions(btn, flat);
                desk.Children.Add(btn);
            }
        }


        /// <summary>
        /// Определяет стиль для элементов главного меню.
        /// Большинство элементов используют Default (оригинальные цвета).
        /// </summary>
        private static UiTextStyle DetermineMainMenuStyle(string messageKey, string passiveFont)
        {
            if (string.IsNullOrEmpty(messageKey)) return UiTextStyle.Default;

            // ✅ Заголовок главного меню — MainMenuTitle (не жирный)
            if (messageKey.Equals("#MAIN_MENU_Window", StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Debug.Log($"[MainMenuLoader] '{messageKey}' -> MainMenuTitle style");
                return UiTextStyle.MainMenuTitle;
            }

            return UiTextStyle.Default;
        }

        #region XML Parsing Helpers

        private sealed class Span
        {
            public int Start;
            public int End;
            public string Content;
        }


        private sealed class CompositeBlock
        {
            public string Inner; // содержимое между <Tag>...</Tag>
            public string Tail;  // параметры после </Tag> до следующего блока
        }

        /// <summary>
        /// Удаляет полностью блок тега вместе с его "хвостом" (параметрами до следующего маркера),
        /// заменяя всё пробелами, чтобы не нарушать индексы (хотя здесь мы передаем строку целиком, 
        /// но замена пробелами безопаснее).
        /// </summary>
        private static string RemoveCompositeTagBlocks(string text, string tag)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string[] markers = {
        "BitPicture","TextButton","Text","CheckBox","Window","ListDesk",
        "DialogsDesk","GPPicture","InputBox","ComboBox","VitButton"
    };

            char[] buf = text.ToCharArray();
            int i = 0;

            while (i < text.Length)
            {
                int open = text.IndexOf("<" + tag + ">", i, StringComparison.OrdinalIgnoreCase);
                if (open < 0) break;

                int close = text.IndexOf("</" + tag + ">", open, StringComparison.OrdinalIgnoreCase);
                if (close < 0) break;

                int tailStart = close + tag.Length + 3; // </Tag>
                int next = text.Length;

                foreach (var mk in markers)
                {
                    var rx = new Regex($@"<{mk}(\s*>|\s+/?>)", RegexOptions.IgnoreCase);
                    var m = rx.Match(text, tailStart);
                    if (m.Success && m.Index < next) next = m.Index;
                }

                // replace [open, next) with spaces
                for (int k = open; k < next; k++)
                    buf[k] = ' ';

                i = next;
            }

            return new string(buf);
        }

        private static IEnumerable<CompositeBlock> FindCompositeBlocks(string text, string tag)
        {
            // <Tag> ... </Tag>  +  "хвост" (x/y/Width/Height/Visible/...) до следующего маркера
            string[] markers = {
                "BitPicture", "TextButton", "Text", "CheckBox", "Window", "ListDesk",
                "DialogsDesk", "GPPicture", "InputBox", "ComboBox", "VitButton"
            };

            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf("<" + tag + ">", i, StringComparison.OrdinalIgnoreCase);
                if (open < 0) yield break;

                int close = text.IndexOf("</" + tag + ">", open, StringComparison.OrdinalIgnoreCase);
                if (close < 0) yield break;

                int innerStart = open + tag.Length + 2;
                int innerEnd = close;
                string inner = text.Substring(innerStart, innerEnd - innerStart);

                int tailStart = close + tag.Length + 3; // </Tag>
                int next = text.Length;

                foreach (var mk in markers)
                {
                    var rx = new Regex($@"<{mk}(\s*>|\s+/?>)", RegexOptions.IgnoreCase);
                    var m = rx.Match(text, tailStart);
                    if (m.Success && m.Index < next) next = m.Index;
                }

                string tail = text.Substring(tailStart, Math.Max(0, next - tailStart));
                yield return new CompositeBlock { Inner = inner, Tail = tail };

                i = next;
            }
        }


        private static List<Span> ExtractTopLevelWindowSpans(string text)
        {
            var spans = new List<Span>();
            if (string.IsNullOrEmpty(text)) return spans;

            int depth = 0;
            int start = -1;
            int i = 0;

            while (i < text.Length)
            {
                int open = text.IndexOf("<Window>", i, StringComparison.OrdinalIgnoreCase);
                int close = text.IndexOf("</Window>", i, StringComparison.OrdinalIgnoreCase);

                if (open < 0 && close < 0) break;

                if (open >= 0 && (close < 0 || open < close))
                {
                    if (depth == 0) start = open;
                    depth++;
                    i = open + 8;
                    continue;
                }

                if (close >= 0)
                {
                    depth = Math.Max(0, depth - 1);
                    int end = close + 9;
                    if (depth == 0 && start >= 0)
                    {
                        spans.Add(new Span { Start = start, End = end, Content = text.Substring(start, end - start) });
                        start = -1;
                    }
                    i = end;
                }
            }

            return spans;
        }

        private static string RemoveRanges(string text, List<Span> spans)
        {
            if (spans == null || spans.Count == 0) return text;

            char[] buf = text.ToCharArray();
            foreach (var span in spans)
            {
                int a = Math.Max(0, span.Start);
                int b = Math.Min(buf.Length, span.End);
                for (int j = a; j < b; j++) buf[j] = ' ';
            }
            return new string(buf);
        }

        private static void FillCommon(UiNode n, string block, int addX, int addY)
        {
            n.Name = Get(block, "Name") ?? "";
            n.X = ToInt(Get(block, "x")) + addX;
            n.Y = ToInt(Get(block, "y")) + addY;
            n.Width = ToInt(Get(block, "Width"));
            n.Height = ToInt(Get(block, "Height"));
            n.Visible = ToBool(Get(block, "Visible"), true);
            n.Enabled = ToBool(Get(block, "Enabled"), true);
        }

        private static void FillActions(UiNode node, string block)
        {
            string actions = GetRawInner(block, "v_Actions") ?? "";
            if (string.IsNullOrWhiteSpace(actions)) return;

            foreach (Match m in Regex.Matches(actions, @"<([A-Za-z0-9_]+)>\s*(.*?)\s*</\1>", RegexOptions.Singleline))
            {
                node.Actions.Add(new UiAction
                {
                    Name = m.Groups[1].Value.Trim(),
                    Payload = m.Groups[2].Value.Trim()
                });
            }
        }

        private static IEnumerable<string> FindBlocks(string text, string tag)
        {
            var rx = new Regex($@"<{tag}>\s*(.*?)</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            foreach (Match m in rx.Matches(text))
                yield return m.Groups[1].Value;
        }

        private static IEnumerable<string> FindFlatBlocks(string text, string tag)
        {
            string[] markers = {
                "BitPicture", "TextButton", "Text", "CheckBox", "Window",
                "DialogsDesk", "GPPicture", "InputBox", "ComboBox", "VitButton"
            };
            var markerPositions = new List<(int start, int end)>();

            var rxThis = new Regex($@"<{tag}>\s*</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            foreach (Match m in rxThis.Matches(text))
                markerPositions.Add((m.Index, m.Index + m.Length));

            markerPositions.Sort((a, b) => a.start.CompareTo(b.start));

            for (int i = 0; i < markerPositions.Count; i++)
            {
                int blockStart = markerPositions[i].end;
                int next = text.Length;

                foreach (var mk in markers)
                {
                    var rx = new Regex($@"<{mk}(\s*>|\s+/?>)", RegexOptions.IgnoreCase);
                    var m = rx.Match(text, blockStart);
                    if (m.Success && m.Index < next) next = m.Index;
                }

                yield return text.Substring(blockStart, next - blockStart);
            }
        }

        private static string Get(string block, string tag)
        {
            var m = Regex.Match(block, $@"<{tag}>\s*(.*?)\s*</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string GetLast(string block, string tag)
        {
            var ms = Regex.Matches(block, $@"<{tag}>\s*(.*?)\s*</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return ms.Count > 0 ? ms[ms.Count - 1].Groups[1].Value : "";
        }

        private static string GetRawInner(string block, string tag)
        {
            var m = Regex.Match(block, $@"<{tag}>\s*(.*?)\s*</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string ExtractTagValue(string xml, string tag)
        {
            var m = Regex.Match(xml, $@"<{tag}>\s*(.*?)\s*</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string NormalizePath(string target)
        {
            target = target.Trim().Replace('/', '\\');
            if (target.StartsWith("dialogs\\", StringComparison.OrdinalIgnoreCase))
                target = "Dialogs\\" + target.Substring("dialogs\\".Length);
            return target;
        }

        private static int ToInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            int.TryParse(s.Trim(), out int v);
            return v;
        }

        private static bool ToBool(string s, bool def)
        {
            if (string.IsNullOrWhiteSpace(s)) return def;
            s = s.Trim().ToLowerInvariant();
            return s == "true" || s == "1" || s == "yes";
        }

        #endregion
    }
}