using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// Загрузчик экрана настроек (Options).
    /// Обрабатывает сложную вложенную структуру с DialogsDesk/TabDesk.
    /// </summary>
    public sealed class OptionsLoader
    {
        private readonly CoreFileSystem _fs;
        private HashSet<string> _processedContent;
        private HashSet<string> _processedInputBoxKeys;  // ✅ Добавить!
        public OptionsLoader(CoreFileSystem fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        /// <summary>
        /// Проверяет, должен ли этот загрузчик обрабатывать данный screenId
        /// </summary>
        public bool CanHandle(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId)) return false;

            return screenId.Equals("Options", StringComparison.OrdinalIgnoreCase)
                || screenId.StartsWith("Options_", StringComparison.OrdinalIgnoreCase)
                || screenId.StartsWith("Options/", StringComparison.OrdinalIgnoreCase)
                || screenId.Equals("Multi", StringComparison.OrdinalIgnoreCase);
        }

        public UiDesk LoadScreen(string screenId)
        {
            string routerPath = @"Dialogs\MainMenu.xml";
            if (!_fs.Exists(routerPath))
            {
                Debug.LogWarning($"[OptionsLoader] Router not found: {routerPath}");
                return new UiDesk();
            }

            string router = _fs.ReadAllText(routerPath);
            string target = GetTagValue(router, screenId);

            if (string.IsNullOrWhiteSpace(target))
            {
                Debug.LogWarning($"[OptionsLoader] Screen '{screenId}' not found in MainMenu.xml");
                return new UiDesk();
            }

            target = NormalizePath(target);

            var desk = LoadDesk(target);

            // ✅ ДЕДУПЛИКАЦИЯ ВСЕХ ЭЛЕМЕНТОВ (глобально по desk.Children)
            DeduplicateChildren(desk);

            return desk;
        }

        private UiDesk LoadDesk(string relativePath)
        {
            if (!_fs.Exists(relativePath))
            {
                Debug.LogWarning($"[OptionsLoader] File not found: {relativePath}");
                return new UiDesk { SourcePath = relativePath };
            }

            string xml = _fs.ReadAllText(relativePath);
            var desk = new UiDesk { SourcePath = relativePath };
            _processedContent = new HashSet<string>();
            _processedInputBoxKeys = new HashSet<string>();  // ✅ Сброс
            ParseContainer(xml, 0, 0, desk, 0);

            Debug.Log($"[OptionsLoader] Loaded {desk.Children.Count} elements from {relativePath}");
            return desk;
        }

        private void ParseContainer(string xml, int baseX, int baseY, UiDesk desk, int depth)
        {
            if (depth > 15) return;

            string hash = xml.Length + "_" + xml.GetHashCode();
            if (_processedContent.Contains(hash)) return;
            _processedContent.Add(hash);

            // DialogsDesk
            foreach (var containerBlock in FindAllBlocks(xml, "DialogsDesk"))
            {
                int containerX = GetContainerCoord(containerBlock, "x");
                int containerY = GetContainerCoord(containerBlock, "y");
                int absoluteX = baseX + containerX;
                int absoluteY = baseY + containerY;

                // ✅ ОТЛАДКА
                Debug.Log($"[ParseContainer] DialogsDesk: container=({containerX},{containerY}), absolute=({absoluteX},{absoluteY})");

                string childDialogs = GetTagValue(containerBlock, "ChildDialogs");
                if (!string.IsNullOrEmpty(childDialogs))
                {
                    ParseElements(childDialogs, absoluteX, absoluteY, desk);
                    ParseContainer(childDialogs, absoluteX, absoluteY, desk, depth + 1);
                }
            }

            // TabDesk
            foreach (var containerBlock in FindAllBlocks(xml, "TabDesk"))
            {
                int containerX = GetContainerCoord(containerBlock, "x");
                int containerY = GetContainerCoord(containerBlock, "y");
                int absoluteX = baseX + containerX;
                int absoluteY = baseY + containerY;

                string childDialogs = GetTagValue(containerBlock, "ChildDialogs");
                if (!string.IsNullOrEmpty(childDialogs))
                {
                    ParseElements(childDialogs, absoluteX, absoluteY, desk);
                    ParseContainer(childDialogs, absoluteX, absoluteY, desk, depth + 1);
                }
            }

            // Direct ChildDialogs
            string directChildren = GetTagValue(xml, "ChildDialogs");
            if (!string.IsNullOrEmpty(directChildren))
            {
                ParseElements(directChildren, baseX, baseY, desk);
                ParseContainer(directChildren, baseX, baseY, desk, depth + 1);
            }
        }

        private void ParseElements(string xml, int baseX, int baseY, UiDesk desk)
        {
            var parsedInputBoxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // TextButton
            foreach (var block in FindAllBlocks(xml, "TextButton"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiTextButton();
                FillCommon(node, block, baseX, baseY);
                node.MessageKey = GetTagValue(block, "Message") ?? "";
                node.HintKey = GetTagValue(block, "Hint") ?? "";
                node.PassiveFont = GetTagValue(block, "PassiveFont") ?? "";
                node.ActiveFont = GetTagValue(block, "ActiveFont") ?? "";
                node.DisabledFont = GetTagValue(block, "DisabledFont") ?? "";

                node.Align = GetTagValue(block, "Align") ?? "Left";
                node.Style = DetermineOptionsStyle(node.MessageKey, node.PassiveFont, node.X, node.Y);
                FillActions(node, block);

                if (!string.IsNullOrEmpty(node.MessageKey) && node.Visible && !IsDuplicate(desk, node))
                    desk.Children.Add(node);
            }

            // GP_TextButton
            foreach (var block in FindAllBlocks(xml, "GP_TextButton"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiGPTextButton();
                FillCommon(node, block, baseX, baseY);
                node.MessageKey = GetTagValue(block, "Message") ?? "";
                node.FileID = GetTagValue(block, "FileID") ?? "";
                node.Sprite = GetInt(block, "Sprite");
                node.PassiveFont = GetTagValue(block, "PassiveFont") ?? "";
                node.ActiveFont = GetTagValue(block, "ActiveFont") ?? "";
                node.DisabledFont = GetTagValue(block, "DisabledFont") ?? "";
                node.Center = GetBool(block, "Center");
                node.FontDx = GetInt(block, "FontDx");
                node.FontDy = GetInt(block, "FontDy");
                node.Style = UiTextStyle.Button;
                FillActions(node, block);

                if (!string.IsNullOrEmpty(node.MessageKey) && !IsDuplicate(desk, node))
                    desk.Children.Add(node);
            }

            // GPPicture
            foreach (var block in FindAllBlocks(xml, "GPPicture"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiGPPicture();
                FillCommon(node, block, baseX, baseY);
                node.FileID = GetTagValue(block, "FileID") ?? "";
                node.SpriteID = GetInt(block, "SpriteID");
                if (node.Visible && !IsDuplicate(desk, node))
                    desk.Children.Add(node);
            }


            // VitButton (Multi)
            // VitButton (Multi)
            // VitButton (Multi) — БЕЗ проверки IsInsideContainer!
            // VitButton (Multi)
            foreach (var block in FindAllBlocks(xml, "VitButton"))
            {
                var node = new UiVitButton();

                int localX = GetContainerCoord(block, "x");
                int localY = GetContainerCoord(block, "y");

                node.X = baseX + localX;
                node.Y = baseY + localY;
                node.Width = GetContainerCoord(block, "Width");
                node.Height = GetContainerCoord(block, "Height");

                // Пропускаем корневой VitButton (x=0, y=0, часто шаблон внутри ListDesk)
                if (node.X == 0 && node.Y == 0)
                {
                    Debug.Log($"[OptionsLoader] SKIP root VitButton at (0,0)");
                    continue;
                }

                node.Name = GetTagValue(block, "Name") ?? "";
                node.Hint = GetTagValue(block, "Hint") ?? "";
                node.Visible = GetBool(block, "Visible", true);
                node.Enabled = GetBool(block, "Enabled", true);

                node.GP_File = GetTagValue(block, "GP_File") ?? "";

                // ═══════════════════════════════════════════════════════════
                // ИСПРАВЛЕНИЕ: правильно читаем State и спрайты
                // ═══════════════════════════════════════════════════════════
                int state = GetInt(block, "State");

                // Читаем OneSprited
                bool oneSprited = GetBool(block, "OneSprited", false);
                node.OneSprited = oneSprited;  // ← добавить поле в UiVitButton!

                node.SpritePassive = GetInt(block, $"SpritePassive{state}");
                node.SpriteActive = GetInt(block, $"SpriteOver{state}");

                // Fallback если -1 или 0
                if (node.SpritePassive <= 0 && node.SpriteActive > 0)
                    node.SpritePassive = node.SpriteActive;

                string vitKey = $"VitButton_{node.X}_{node.Y}";
                if (_processedInputBoxKeys.Contains(vitKey))
                    continue;
                _processedInputBoxKeys.Add(vitKey);

                Debug.Log($"[OptionsLoader] VitButton: ({node.X},{node.Y}), W={node.Width}, " +
                          $"GP={node.GP_File}, State={state}, SprPassive={node.SpritePassive}, OneSprited={oneSprited}");

                // ═══════════════════════════════════════════════════════════
                // ИСПРАВЛЕНИЕ: ВСЕГДА добавляем VitButton (это фон!)
                // ═══════════════════════════════════════════════════════════
                if (node.Visible)
                {
                    desk.Children.Add(node);
                }

                // Также обрабатываем InputBox внутри
                string vitChildDialogs = GetTagValue(block, "ChildDialogs");
                if (!string.IsNullOrEmpty(vitChildDialogs))
                {
                    foreach (var inner in FindAllBlocks(vitChildDialogs, "InputBox"))
                    {
                        int innerX = GetInt(inner, "x");
                        int innerY = GetInt(inner, "y");

                        int ibX = node.X + innerX;
                        int ibY = node.Y + innerY;

                        string ibKey = $"InputBox_{ibX}_{ibY}";
                        if (_processedInputBoxKeys.Contains(ibKey))
                            continue;
                        _processedInputBoxKeys.Add(ibKey);

                        var ib = new UiInputBox
                        {
                            X = ibX,
                            Y = ibY,
                            Width = GetInt(inner, "Width", 320),
                            Height = GetInt(inner, "Height", 18),
                            Name = GetTagValue(inner, "Name") ?? "",
                            Hint = GetTagValue(inner, "Hint") ?? "",
                            Visible = GetBool(inner, "Visible", true),
                            Enabled = GetBool(inner, "Enabled", true),
                            Action = GetTagValue(inner, "Action") ?? "",
                            Font = GetTagValue(inner, "Font") ?? "BlackFont",
                            MaxLen = GetInt(inner, "StrMaxLen", 30)
                        };

                        Debug.Log($"[OptionsLoader] InputBox inside VitButton: ({ib.X},{ib.Y})");

                        if (ib.Visible)
                            desk.Children.Add(ib);
                    }
                }
            }





            // ListDesk
            foreach (var block in FindAllBlocks(xml, "ListDesk"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiListDesk();
                FillCommon(node, block, baseX, baseY);

                node.Border = GetTagValue(block, "Border") ?? "";
                node.MarginX = GetInt(block, "marginX", 3);
                node.MarginY = GetInt(block, "marginY", 3);
                node.Action = GetTagValue(block, "Action") ?? "";

                // ═══════════════════════════════════════════════════════════
                // Парсим <Element><VitButton>...</VitButton></Element>
                // ═══════════════════════════════════════════════════════════
                string elementXml = GetTagValue(block, "Element");
                if (!string.IsNullOrEmpty(elementXml))
                {
                    var vitBlocks = FindAllBlocks(elementXml, "VitButton");
                    if (vitBlocks.Count > 0)
                    {
                        string vb = vitBlocks[0];

                        int state = GetInt(vb, "State");

                        node.ElementTemplate = new UiListDeskElement
                        {
                            GP_File = GetTagValue(vb, "GP_File") ?? "",
                            Width = GetInt(vb, "Width", 460),
                            Height = GetInt(vb, "Height", 20),
                            FontPassive = GetTagValue(vb, "FontPassive") ?? "BlackFont",
                            FontOver = GetTagValue(vb, "FontOver") ?? "RedFont",
                            FontDx = GetInt(vb, "FontDx", 10),
                            FontDy = GetInt(vb, "FontDy", 0),
                            Align = GetTagValue(vb, "Align") ?? "Left",

                            // Спрайты зависят от State
                            SpritePassive = GetInt(vb, $"SpritePassive{state}", -1),
                            SpriteOver = GetInt(vb, $"SpriteOver{state}", 0),
                            SpriteSelected = GetInt(vb, "SpritePassive1", 5),
                        };

                        // Если Width не указан в элементе, берём из ListDesk
                        if (node.ElementTemplate.Width <= 0)
                            node.ElementTemplate.Width = node.Width - node.MarginX * 2 - 20; // -20 на скроллер
                    }
                }

                Debug.Log($"[OptionsLoader] ListDesk at ({node.X},{node.Y}), size={node.Width}x{node.Height}, " +
                          $"element={node.ElementTemplate?.Width}x{node.ElementTemplate?.Height}");

                if (node.Visible && !IsDuplicate(desk, node))
                    desk.Children.Add(node);
            }


            // CheckBox
            foreach (var block in FindAllBlocks(xml, "CheckBox"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiCheckBox();
                FillCommon(node, block, baseX, baseY);
                node.GP_File = GetTagValue(block, "GP_File") ?? "";
                node.State = GetBool(block, "State", false);
                node.GroupIndex = GetInt(block, "GroupIndex");
                FillActions(node, block);
                if (!IsDuplicate(desk, node))
                    desk.Children.Add(node);

                 
            }

            // ComboBox
            foreach (var block in FindAllBlocks(xml, "ComboBox"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiComboBox();
                FillCommon(node, block, baseX, baseY);
                node.GP_File = GetTagValue(block, "GP_File") ?? "";
                node.ActiveFont = GetTagValue(block, "ActiveFont") ?? "";
                node.PassiveFont = GetTagValue(block, "PassiveFont") ?? "";

                // original (Options)
                node.FontDx = GetInt(block, "FontDx");
                node.FontDy = GetInt(block, "FontDy");
                node.OneDx = GetInt(block, "OneDx");
                node.OneDy = GetInt(block, "OneDy");
                node.Center = GetInt(block, "Center");
                node.MaxLY = GetInt(block, "MaxLY");
                FillActions(node, block);
                if (!IsDuplicate(desk, node))
                    desk.Children.Add(node);
            }

            // VScrollBar (Slider)
            foreach (var block in FindAllBlocks(xml, "VScrollBar"))
            {
                if (IsInsideContainer(xml, block)) continue;

                var node = new UiSlider();
                FillCommon(node, block, baseX, baseY);
                node.Position = GetInt(block, "SPos");
                node.MaxPosition = GetInt(block, "SMaxPos");
                node.SliderPos = GetInt(block, "SliderPos");
                node.GroupIndex = GetInt(block, "GroupIndex");

                // original (Options)
                node.LineLx = GetInt(block, "LineLx");
                node.LineLy = GetInt(block, "LineLy");
                node.ScrDx = GetInt(block, "ScrDx");
                node.ScrDy = GetInt(block, "ScrDy");
                if (node.MaxPosition <= 0) node.MaxPosition = 100;
                FillActions(node, block);

                if (!IsDuplicate(desk, node))
                    desk.Children.Add(node);
            }
        }

        // ✅ Универсальная дедупликация после полной загрузки desk
        private static void DeduplicateChildren(UiDesk desk)
        {
            if (desk?.Children == null) return;

            var seen = new HashSet<string>();
            var unique = new List<UiNode>(desk.Children.Count);

            foreach (var node in desk.Children)
            {
                if (node == null) continue;

                // Ключ: тип + координаты + имя.
                // Если надо жестче: добавь Width/Height/MessageKey и т.п.
                string key = $"{node.GetType().Name}_{node.X}_{node.Y}_{node.Name}";

                if (seen.Add(key))
                {
                    unique.Add(node);
                }
                 
            }

            int removed = desk.Children.Count - unique.Count;
            if (removed > 0)
            {
                 
                desk.Children.Clear();
                desk.Children.AddRange(unique);
            }
        }

        #region Style Detection

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

        private static UiTextStyle DetermineOptionsStyle(string messageKey, string passiveFont, int x, int y)
        {
            if (string.IsNullOrEmpty(messageKey)) return UiTextStyle.Default;

            if (SectionTitleKeys.Contains(messageKey))
                return UiTextStyle.SectionTitle;

            if (OptionLabelKeys.Contains(messageKey))
                return UiTextStyle.OptionLabel;

            if (WindowTitleKeys.Contains(messageKey))
                return x > 750 ? UiTextStyle.GoldenTitle : UiTextStyle.WindowTitle;

            if (!string.IsNullOrEmpty(passiveFont))
            {
                string fontLower = passiveFont.ToLowerInvariant();
                if (fontLower.Contains("menutitle2red")) return UiTextStyle.SectionTitle;
                if (fontLower.Contains("blackfont") || fontLower.Contains("grayfont")) return UiTextStyle.OptionLabel;
                if (fontLower.Contains("menutitlewhite")) return UiTextStyle.WindowTitle;
                if (fontLower.Contains("menugold")) return UiTextStyle.GoldenTitle;
            }

            return UiTextStyle.Default;
        }

        #endregion

        #region XML Parsing Helpers
        private static bool IsInsideTag(string parentXml, string elementBlock, string tag)
        {
            int elementPos = parentXml.IndexOf(elementBlock, StringComparison.Ordinal);
            if (elementPos < 0) return false;

            string openTag = $"<{tag}>";
            string closeTag = $"</{tag}>";

            int lastOpen = parentXml.LastIndexOf(openTag, elementPos, StringComparison.OrdinalIgnoreCase);
            if (lastOpen < 0) return false;

            int closePos = parentXml.IndexOf(closeTag, lastOpen, StringComparison.OrdinalIgnoreCase);
            if (closePos < 0) return false;

            return closePos > elementPos;
        }
        private static bool IsInsideContainer(string parentXml, string elementBlock)
        {
            int elementPos = parentXml.IndexOf(elementBlock, StringComparison.Ordinal);
            if (elementPos < 0) return false;

            int lastDialogsDesk = parentXml.LastIndexOf("<DialogsDesk>", elementPos, StringComparison.OrdinalIgnoreCase);
            int lastTabDesk = parentXml.LastIndexOf("<TabDesk>", elementPos, StringComparison.OrdinalIgnoreCase);
            int lastContainerStart = Math.Max(lastDialogsDesk, lastTabDesk);

            if (lastContainerStart < 0) return false;

            string closeTag = lastDialogsDesk > lastTabDesk ? "</DialogsDesk>" : "</TabDesk>";
            int closePos = parentXml.IndexOf(closeTag, lastContainerStart, StringComparison.OrdinalIgnoreCase);

            return closePos > elementPos;
        }

        private static bool IsDuplicate(UiDesk desk, UiNode node)
        {
            foreach (var existing in desk.Children)
            {
                if (existing == null) continue;

                // ✅ Базовая проверка: тип + координаты
                if (existing.X == node.X && existing.Y == node.Y && existing.GetType() == node.GetType())
                {
                    // Для текстовых кнопок — дополнительно проверяем MessageKey
                    if (existing is UiTextButton tb1 && node is UiTextButton tb2)
                        return tb1.MessageKey == tb2.MessageKey;

                    if (existing is UiGPTextButton gp1 && node is UiGPTextButton gp2)
                        return gp1.MessageKey == gp2.MessageKey;

                    // ✅ Для InputBox, VitButton и остальных — достаточно X/Y/Type
                    return true;
                }
            }
            return false;
        }

        private static void FillCommon(UiNode node, string block, int addX, int addY)
        {
            node.Name = GetTagValue(block, "Name") ?? "";
            node.Hint = GetTagValue(block, "Hint") ?? "";

            // ✅ ИСПРАВЛЕНИЕ: берём ПОСЛЕДНИЕ координаты (они после ChildDialogs)
            node.X = GetLastInt(block, "x") + addX;
            node.Y = GetLastInt(block, "y") + addY;
            node.Width = GetLastInt(block, "Width");
            node.Height = GetLastInt(block, "Height");

            node.Visible = GetBool(block, "Visible", true);
            node.Enabled = GetBool(block, "Enabled", true);
        }

        private static void FillActions(UiNode node, string block)
        {
            string actionsXml = GetTagValue(block, "v_Actions") ?? "";
            if (string.IsNullOrWhiteSpace(actionsXml)) return;

            var rx = new Regex(@"<([A-Za-z0-9_]+)>\s*(.*?)\s*</\1>", RegexOptions.Singleline);
            foreach (Match m in rx.Matches(actionsXml))
            {
                node.Actions.Add(new UiAction
                {
                    Name = m.Groups[1].Value.Trim(),
                    Payload = m.Groups[2].Value.Trim()
                });
            }
        }

        private static int GetContainerCoord(string containerBlock, string coord)
        {
            // 1) Пытаемся взять координаты контейнера ПОСЛЕ ChildDialogs (это правильное место в этих XML)
            int childDialogsEnd = containerBlock.LastIndexOf("</ChildDialogs>", StringComparison.OrdinalIgnoreCase);
            if (childDialogsEnd > 0)
            {
                string afterChildren = containerBlock.Substring(childDialogsEnd);
                int val = GetInt(afterChildren, coord);
                if (val != 0) return val;

                // даже если 0 — это валидно, но тогда НЕ надо падать в GetLastInt по всему блоку,
                // иначе можно схватить x/y из дочерних элементов.
                // Проверим наличие тега явно:
                if (Regex.IsMatch(afterChildren, $@"<{coord}>\s*\d+\s*</{coord}>", RegexOptions.IgnoreCase))
                    return 0;
            }

            // 2) Если координаты контейнера лежат до ChildDialogs — вырежем ChildDialogs и возьмём координаты из остатка
            string stripped = Regex.Replace(
                containerBlock,
                @"<ChildDialogs>.*?</ChildDialogs>",
                "",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return GetLastInt(stripped, coord);
        }

        private static int GetLastInt(string xml, string tag)
        {
            var matches = Regex.Matches(xml, $@"<{tag}>\s*(\d+)\s*</{tag}>", RegexOptions.IgnoreCase);
            if (matches.Count == 0) return 0;
            if (int.TryParse(matches[matches.Count - 1].Groups[1].Value.Trim(), out int result))
                return result;
            return 0;
        }

        private static List<string> FindAllBlocks(string xml, string tag)
        {
            var results = new List<string>();
            string openTag = $"<{tag}>";
            string closeTag = $"</{tag}>";

            int searchStart = 0;
            while (true)
            {
                int start = xml.IndexOf(openTag, searchStart, StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;

                int contentStart = start + openTag.Length;
                int depth = 1;
                int pos = contentStart;

                while (depth > 0 && pos < xml.Length)
                {
                    int nextOpen = xml.IndexOf(openTag, pos, StringComparison.OrdinalIgnoreCase);
                    int nextClose = xml.IndexOf(closeTag, pos, StringComparison.OrdinalIgnoreCase);

                    if (nextClose < 0) break;

                    if (nextOpen >= 0 && nextOpen < nextClose)
                    {
                        depth++;
                        pos = nextOpen + openTag.Length;
                    }
                    else
                    {
                        depth--;
                        if (depth == 0)
                            results.Add(xml.Substring(contentStart, nextClose - contentStart));
                        pos = nextClose + closeTag.Length;
                    }
                }

                searchStart = pos > searchStart ? pos : searchStart + 1;
            }

            return results;
        }

        private static string GetTagValue(string xml, string tag)
        {
            var blocks = FindAllBlocks(xml, tag);
            return blocks.Count > 0 ? blocks[0] : "";
        }

        private static int GetInt(string xml, string tag)
        {
            string v = GetTagValue(xml, tag);
            if (string.IsNullOrWhiteSpace(v)) return 0;
            int.TryParse(v.Trim(), out int result);
            return result;
        }
private static int GetInt(string xml, string tag, int defaultValue)
{
    try
    {
        if (string.IsNullOrEmpty(xml)) return defaultValue;
        int ix = xml.IndexOf("<" + tag + ">", StringComparison.OrdinalIgnoreCase);
        if (ix < 0) return defaultValue;
        int iy = xml.IndexOf("</" + tag + ">", ix, StringComparison.OrdinalIgnoreCase);
        if (iy < 0) return defaultValue;
        string s = xml.Substring(ix + tag.Length + 2, iy - (ix + tag.Length + 2)).Trim();
        return int.TryParse(s, out int v) ? v : defaultValue;
    }
    catch
    {
        return defaultValue;
    }
}


        private static bool GetBool(string xml, string tag, bool defaultValue)
        {
            string v = GetTagValue(xml, tag);
            if (string.IsNullOrWhiteSpace(v)) return defaultValue;
            v = v.Trim().ToLowerInvariant();
            return v == "true" || v == "1" || v == "yes";
        }

        private static bool GetBool(string xml, string tag)
        {
            return GetBool(xml, tag, false);
        }

        private static string NormalizePath(string target)
        {
            target = target.Trim().Replace('/', '\\');
            if (target.StartsWith("dialogs\\", StringComparison.OrdinalIgnoreCase))
                target = "Dialogs\\" + target.Substring("dialogs\\".Length);
            return target;
        }

        #endregion
    }
}