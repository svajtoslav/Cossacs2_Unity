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

            foreach (string block in FindBlocks(region, "BitPicture"))
            {
                var pic = new UiBitPicture();
                FillCommon(pic, block, baseX, baseY);
                pic.FileName = Get(block, "FileName") ?? "";
                desk.Children.Add(pic);
            }

            foreach (string block in FindBlocks(region, "TextButton"))
            {
                var btn = new UiTextButton();
                FillCommon(btn, block, baseX, baseY);
                btn.MessageKey = Get(block, "Message") ?? "";
                btn.HintKey = Get(block, "Hint") ?? "";
                btn.PassiveFont = Get(block, "PassiveFont") ?? "";
                btn.ActiveFont = Get(block, "ActiveFont") ?? "";
                btn.DisabledFont = Get(block, "DisabledFont") ?? "";
                
                // ✅ ИСПРАВЛЕНО: Для главного меню НЕ назначаем стиль - используем Default
                // Это сохранит оригинальные цвета из RenderOptions
                btn.Style = DetermineMainMenuStyle(btn.MessageKey, btn.PassiveFont);
                
                FillActions(btn, block);
                desk.Children.Add(btn);
            }

            foreach (string flat in FindFlatBlocks(region, "Text"))
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
                
                // ✅ ИСПРАВЛЕНО: Для главного меню используем Default стиль
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
            string[] markers = { "BitPicture", "TextButton", "Text", "CheckBox", "Window" };
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