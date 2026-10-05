
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cossacks2Bridge.Core;

namespace Cossacks2Bridge.UnityAdapters.Battles
{
    /// <summary>Simple battle/skirmish entry used by MbattlesXmlRenderer.</summary>
    internal struct MbPreviewPoint
    {
        public int X;
        public int Y;

        public MbPreviewPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    internal sealed class BattleEntrySimple
    {
        public string Id = "";
        public string DisplayName = "";
        public string Description = "";
        public string PreviewPath = "";
        public int PreviewCenterX;
        public int PreviewCenterY;
        public readonly List<MbPreviewPoint> PreviewScreenSaver = new List<MbPreviewPoint>();
    }

    internal sealed class MbScene
    {
        public readonly List<MbNode> Nodes = new List<MbNode>();
        public readonly List<string> MissionIds = new List<string>();
        public string SelectedMissionId = "";
        public string DescriptionText = "";
        public string PreviewPath = "";
        public bool ShowBattles;
        public bool ShowLoad;
    }

    internal abstract class MbNode
    {
        public string Name = "";
        public string Role = "";
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int HostX;
        public int HostY;
        public int HostWidth;
        public int HostHeight;
        public bool Visible = true;
        public readonly List<UiAction> Actions = new List<UiAction>();
    }

    internal sealed class MbDeskNode : MbNode
    {
        public string Border = "";
        public bool EnableVerticalScroller;
        public bool EnableHorizontalScroller;
    }

    internal sealed class MbBitPictureNode : MbNode
    {
        public string FileName = "";
        public bool ActualSize;
    }

    internal sealed class MbGpPictureNode : MbNode
    {
        public string FileID = "";
        public int SpriteID;
    }

    internal sealed class MbTextNode : MbNode
    {
        public string Message = "";
        public string Align = "Left";
        public string Font = "";
        public int MaxWidth;
        public bool IsFormatted;
    }

    internal sealed class MbGpTextButtonNode : MbNode
    {
        public string Message = "";
        public string FileID = "";
        public int Sprite;
        public int Sprite1 = -1;
        public bool Center;
        public int FontDx;
        public int FontDy;
        public string PassiveFont = "";
        public string ActiveFont = "";
        public string DisabledFont = "";
    }

    internal sealed class MbComboBoxNode : MbNode
    {
        public string GP_File = "";
        public string DisplayText = "";
        public string ActiveFont = "";
        public string PassiveFont = "";
        public int FontDx;
        public int FontDy;
        public int OneDx;
        public int OneDy;
        public int Center;
        public int MaxLY;
        public int ID;
    }

    internal sealed class MbListNode : MbNode
    {
        public string Border = "";
        public readonly List<string> Items = new List<string>();
    }

    internal sealed class MbParseContext
    {
        public string XmlDir = "";
        public string Role = "";
        public int HostX;
        public int HostY;
        public int HostWidth;
        public int HostHeight;
    }

    internal sealed class MbattlesXmlLoader
    {
        private readonly CoreFileSystem _fs;
        private readonly LocDb _loc;
        private readonly HashSet<string> _loadedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MbattlesXmlLoader(CoreFileSystem fs, LocDb loc)
        {
            _fs = fs;
            _loc = loc;
        }

        public MbScene Load()
        {
            var scene = new MbScene();
            scene.ShowBattles = MenuActionSink.SingleBattlesShowBattles && !MenuActionSink.SingleBattlesShowLoad;
            scene.ShowLoad = MenuActionSink.SingleBattlesShowLoad;

            string rootPath = @"Dialogs\v\M_Battles.DialogsSystem.xml";
            ParseIntoScene(scene, rootPath, 0, 0, new MbParseContext
            {
                XmlDir = @"Dialogs\v",
                Role = "",
                HostX = 0,
                HostY = 0,
                HostWidth = 1024,
                HostHeight = 768
            }, allowTabFiltering: true);

            PopulateMissions(scene);
            ApplyDynamicMissionData(scene);
            return scene;
        }

        private void ParseIntoScene(MbScene scene, string relPath, int baseX, int baseY, MbParseContext ctx, bool allowTabFiltering)
        {
            relPath = NormalizePath(relPath);
            if (string.IsNullOrWhiteSpace(relPath) || !_fs.Exists(relPath))
                return;
            string dedupeKey = relPath + "@" + baseX + "," + baseY + "|" + (ctx != null ? ctx.Role : "");
            if (!_loadedSources.Add(dedupeKey))
                return;

            string raw = _fs.ReadAllText(relPath);
            string sanitized = SanitizeXml(raw);
            XElement root;
            try
            {
                root = XElement.Parse(sanitized, LoadOptions.PreserveWhitespace);
            }
            catch
            {
                return;
            }

            var nextCtx = ctx ?? new MbParseContext();
            nextCtx.XmlDir = Path.GetDirectoryName(relPath) ?? "";
            ParseChildDialogs(scene, root, baseX, baseY, nextCtx, allowTabFiltering);
        }

        private void ParseChildDialogs(MbScene scene, XElement owner, int baseX, int baseY, MbParseContext ctx, bool allowTabFiltering)
        {
            var childDialogs = owner.Element("ChildDialogs");
            if (childDialogs == null) return;

            foreach (var el in childDialogs.Elements())
                ParseElement(scene, el, baseX, baseY, ctx, allowTabFiltering);
        }

        private void ParseElement(MbScene scene, XElement el, int baseX, int baseY, MbParseContext ctx, bool allowTabFiltering)
        {
            string tag = el.Name.LocalName;
            bool visible = ToBool(Get(el, "Visible"), true);
            if (!visible) return;

            int x = baseX + ToInt(Get(el, "x"));
            int y = baseY + ToInt(Get(el, "y"));
            int w = ToInt(Get(el, "Width"));
            int h = ToInt(Get(el, "Height"));

            if (tag.Equals("DialogsDesk", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("Window", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("Canvas", StringComparison.OrdinalIgnoreCase))
            {
                string border = Get(el, "Border");
                bool enableV = ToBool(Get(el, "EnableVerticalScroller"), false);
                bool enableH = ToBool(Get(el, "EnableHorizontalScroller"), false);
                string name = Get(el, "Name") ?? "";
                string source = ResolveSource(ctx != null ? ctx.XmlDir : "", Get(el, "Source"));
                var actions = ReadActions(el);

                string role = DetermineDeskRole(name, source, actions, enableV);
                var node = new MbDeskNode
                {
                    Name = name,
                    Role = role,
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    Border = border,
                    EnableVerticalScroller = enableV,
                    EnableHorizontalScroller = enableH,
                    Visible = true
                };
                node.Actions.AddRange(actions);

                if (ShouldRenderDesk(node))
                    scene.Nodes.Add(node);

                var childCtx = new MbParseContext
                {
                    XmlDir = ctx != null ? ctx.XmlDir : "",
                    Role = string.IsNullOrWhiteSpace(role) ? (ctx != null ? ctx.Role : "") : role,
                    HostX = x,
                    HostY = y,
                    HostWidth = w,
                    HostHeight = h
                };

                if (!string.IsNullOrWhiteSpace(source))
                {
                    var sourceCtx = new MbParseContext
                    {
                        XmlDir = Path.GetDirectoryName(source) ?? "",
                        Role = childCtx.Role,
                        HostX = x,
                        HostY = y,
                        HostWidth = w,
                        HostHeight = h
                    };
                    ParseIntoScene(scene, source, x, y, sourceCtx, allowTabFiltering);
                }

                ParseChildDialogs(scene, el, x, y, childCtx, allowTabFiltering);
                return;
            }

            if (tag.Equals("TabDesk", StringComparison.OrdinalIgnoreCase))
            {
                string parentDialogId = Get(el, "ParentDialogID");
                if (allowTabFiltering && !ShouldRenderTabDesk(parentDialogId, scene.ShowBattles, scene.ShowLoad))
                    return;

                string border = Get(el, "Border");
                var node = new MbDeskNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = "TabDesk",
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    Border = border,
                    EnableVerticalScroller = ToBool(Get(el, "EnableVerticalScroller"), false),
                    EnableHorizontalScroller = ToBool(Get(el, "EnableHorizontalScroller"), false),
                    Visible = true
                };
                node.Actions.AddRange(ReadActions(el));
                if (ShouldRenderDesk(node))
                    scene.Nodes.Add(node);

                var childCtx = new MbParseContext
                {
                    XmlDir = ctx != null ? ctx.XmlDir : "",
                    Role = "TabDesk",
                    HostX = x,
                    HostY = y,
                    HostWidth = w,
                    HostHeight = h
                };

                string source = ResolveSource(ctx != null ? ctx.XmlDir : "", Get(el, "Source"));
                if (!string.IsNullOrWhiteSpace(source))
                {
                    var sourceCtx = new MbParseContext
                    {
                        XmlDir = Path.GetDirectoryName(source) ?? "",
                        Role = childCtx.Role,
                        HostX = x,
                        HostY = y,
                        HostWidth = w,
                        HostHeight = h
                    };
                    ParseIntoScene(scene, source, x, y, sourceCtx, false);
                }

                ParseChildDialogs(scene, el, x, y, childCtx, false);
                return;
            }

            if (tag.Equals("GPPicture", StringComparison.OrdinalIgnoreCase))
            {
                var node = new MbGpPictureNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = DetermineNodeRole(tag, Get(el, "Name"), ctx != null ? ctx.Role : "", ReadActions(el)),
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    FileID = Get(el, "FileID") ?? "",
                    SpriteID = ToInt(Get(el, "SpriteID")),
                    Visible = true
                };
                node.Actions.AddRange(ReadActions(el));
                scene.Nodes.Add(node);
                return;
            }

            if (tag.Equals("BitPicture", StringComparison.OrdinalIgnoreCase))
            {
                var node = new MbBitPictureNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = DetermineNodeRole(tag, Get(el, "Name"), ctx != null ? ctx.Role : "", ReadActions(el)),
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    FileName = Get(el, "FileName") ?? "",
                    ActualSize = ToBool(Get(el, "ActualSize"), false),
                    Visible = true
                };
                node.Actions.AddRange(ReadActions(el));
                scene.Nodes.Add(node);
                return;
            }

            if (tag.Equals("TextButton", StringComparison.OrdinalIgnoreCase) || tag.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                string msg = Get(el, "Message");
                if (string.IsNullOrWhiteSpace(msg)) msg = Get(el, "Text");
                var actions = ReadActions(el);
                var node = new MbTextNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = DetermineTextRole(Get(el, "Name"), ctx != null ? ctx.Role : "", actions),
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    Message = msg ?? "",
                    Align = Get(el, "Align") ?? "Left",
                    Font = Get(el, "PassiveFont") ?? Get(el, "Font") ?? "",
                    MaxWidth = ToInt(Get(el, "MaxWidth"), w),
                    IsFormatted = !string.IsNullOrWhiteSpace(msg) && msg.IndexOf('{') >= 0,
                    Visible = true
                };
                node.Actions.AddRange(actions);
                scene.Nodes.Add(node);
                return;
            }

            if (tag.Equals("GP_TextButton", StringComparison.OrdinalIgnoreCase))
            {
                var node = new MbGpTextButtonNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = DetermineNodeRole(tag, Get(el, "Name"), ctx != null ? ctx.Role : "", ReadActions(el)),
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    Message = Get(el, "Message") ?? "",
                    FileID = Get(el, "FileID") ?? "",
                    Sprite = ToInt(Get(el, "Sprite")),
                    Sprite1 = ToInt(Get(el, "Sprite1"), -1),
                    Center = ToBool(Get(el, "Center"), false),
                    FontDx = ToInt(Get(el, "FontDx")),
                    FontDy = ToInt(Get(el, "FontDy")),
                    PassiveFont = Get(el, "PassiveFont") ?? "",
                    ActiveFont = Get(el, "ActiveFont") ?? "",
                    DisabledFont = Get(el, "DisabledFont") ?? "",
                    Visible = true
                };
                FillActions(node.Actions, el);
                RemapSpecialButtonActions(node);
                scene.Nodes.Add(node);
                return;
            }

            if (tag.Equals("TabButton", StringComparison.OrdinalIgnoreCase))
            {
                string group = Get(el, "Group");
                int state = ToInt(Get(el, "State"));
                bool active = group.Equals("Skirmish", StringComparison.OrdinalIgnoreCase) ? (!scene.ShowBattles && !scene.ShowLoad) :
                              group.Equals("Battles", StringComparison.OrdinalIgnoreCase) ? (scene.ShowBattles && !scene.ShowLoad) :
                              group.Equals("Load", StringComparison.OrdinalIgnoreCase) ? scene.ShowLoad :
                              state != 0;

                int sprite = active ? ToInt(Get(el, "SpritePassive1"), ToInt(Get(el, "SpriteOver1"), 5))
                                    : ToInt(Get(el, "SpritePassive0"), ToInt(Get(el, "SpriteOver0"), 10));

                var node = new MbGpTextButtonNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = "TabButton",
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    Message = Get(el, "Message") ?? "",
                    FileID = Get(el, "GP_File") ?? "",
                    Sprite = sprite,
                    Sprite1 = sprite,
                    Center = true,
                    FontDx = ToInt(Get(el, "FontDx")),
                    FontDy = ToInt(Get(el, "FontDy")),
                    PassiveFont = Get(el, "FontPassive") ?? "",
                    ActiveFont = Get(el, "FontOver") ?? "",
                    DisabledFont = Get(el, "FontPassive") ?? "",
                    Visible = true
                };

                if (group.Equals("Skirmish", StringComparison.OrdinalIgnoreCase))
                    node.Actions.Add(new UiAction { Name = "cva_Battles_Mode_Skirmish", Payload = "Skirmish" });
                else if (group.Equals("Battles", StringComparison.OrdinalIgnoreCase))
                    node.Actions.Add(new UiAction { Name = "cva_Battles_Mode_Battles", Payload = "Battles" });
                else if (group.Equals("Load", StringComparison.OrdinalIgnoreCase))
                    node.Actions.Add(new UiAction { Name = "cva_Battles_Mode_Load", Payload = "Load" });

                scene.Nodes.Add(node);
                return;
            }

            if (tag.Equals("ComboBox", StringComparison.OrdinalIgnoreCase))
            {
                var actions = ReadActions(el);
                var node = new MbComboBoxNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = DetermineNodeRole(tag, Get(el, "Name"), ctx != null ? ctx.Role : "", actions),
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    GP_File = Get(el, "GP_File") ?? "",
                    ActiveFont = Get(el, "ActiveFont") ?? "",
                    PassiveFont = Get(el, "PassiveFont") ?? "",
                    FontDx = ToInt(Get(el, "FontDx"), 25),
                    FontDy = ToInt(Get(el, "FontDy"), 2),
                    OneDx = ToInt(Get(el, "OneDx"), 24),
                    OneDy = ToInt(Get(el, "OneDy"), 3),
                    Center = ToInt(Get(el, "Center"), 9),
                    MaxLY = ToInt(Get(el, "MaxLY"), 8),
                    ID = ToInt(Get(el, "ID")),
                    Visible = true
                };
                node.Actions.AddRange(actions);
                node.DisplayText = GetComboDisplayText(node);
                scene.Nodes.Add(node);
                return;
            }

            if (tag.Equals("ListDesk", StringComparison.OrdinalIgnoreCase))
            {
                var node = new MbListNode
                {
                    Name = Get(el, "Name") ?? "",
                    Role = DetermineNodeRole(tag, Get(el, "Name"), ctx != null ? ctx.Role : "", ReadActions(el)),
                    X = x, Y = y, Width = w, Height = h,
                    HostX = ctx != null ? ctx.HostX : 0,
                    HostY = ctx != null ? ctx.HostY : 0,
                    HostWidth = ctx != null ? ctx.HostWidth : 0,
                    HostHeight = ctx != null ? ctx.HostHeight : 0,
                    Border = Get(el, "Border") ?? "",
                    Visible = true
                };
                FillActions(node.Actions, el);
                if (node.Name.Equals("S", StringComparison.OrdinalIgnoreCase))
                    scene.Nodes.Add(node);
                return;
            }
        }

        private static bool ShouldRenderDesk(MbDeskNode node)
        {
            if (node == null) return false;
            if (!node.Visible) return false;
            if (!string.IsNullOrWhiteSpace(node.Role)) return true;
            if (!string.IsNullOrWhiteSpace(node.Border) &&
                !node.Border.Equals("NullBorder", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static string DetermineDeskRole(string name, string source, List<UiAction> actions, bool enableV)
        {
            if (!string.IsNullOrWhiteSpace(source) && source.IndexOf("MapPreviewJpg", StringComparison.OrdinalIgnoreCase) >= 0)
                return "PreviewHost";
            if (actions.Any(a => a.Name.Equals("cva_Map_PreviewJpg", StringComparison.OrdinalIgnoreCase)))
                return "PreviewHost";
            if (enableV)
                return "DescriptionHost";
            if (!string.IsNullOrWhiteSpace(name) && name.Equals("Arcada", StringComparison.OrdinalIgnoreCase))
                return "ArcadeHost";
            return "";
        }

        private static string DetermineTextRole(string name, string parentRole, List<UiAction> actions)
        {
            if (actions.Any(a => a.Name.Equals("va_MissDescription", StringComparison.OrdinalIgnoreCase)))
                return "MissionDescription";
            if (string.Equals(parentRole, "DescriptionHost", StringComparison.OrdinalIgnoreCase))
                return "MissionDescription";
            if (string.Equals(parentRole, "ArcadeHost", StringComparison.OrdinalIgnoreCase))
                return "ArcadeLabel";
            return "";
        }

        private static string DetermineNodeRole(string tag, string name, string parentRole, List<UiAction> actions)
        {
            if (tag.Equals("BitPicture", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parentRole, "PreviewHost", StringComparison.OrdinalIgnoreCase))
                return "MissionPreview";
            if (tag.Equals("GPPicture", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parentRole, "PreviewHost", StringComparison.OrdinalIgnoreCase))
                return "PreviewFrame";
            if (tag.Equals("ComboBox", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parentRole, "ArcadeHost", StringComparison.OrdinalIgnoreCase))
                return "ArcadeCombo";
            return "";
        }

        private static List<UiAction> ReadActions(XElement el)
        {
            var list = new List<UiAction>();
            FillActions(list, el);
            return list;
        }

        private void PopulateMissions(MbScene scene)
        {
            string relDir = scene.ShowBattles ? @"Missions\Battles" : @"Missions\Skirmish";
            string absDir = _fs.ResolvePath(relDir);
            if (Directory.Exists(absDir))
            {
                foreach (string f in Directory.GetFiles(absDir, "*.txt"))
                {
                    string id = Path.GetFileNameWithoutExtension(f);
                    if (!string.IsNullOrWhiteSpace(id))
                        scene.MissionIds.Add(id);
                }
                scene.MissionIds.Sort(NaturalStringComparer.Instance);
            }

            if (scene.MissionIds.Count == 0)
            {
                for (int i = 1; i <= 8; i++)
                    scene.MissionIds.Add((scene.ShowBattles ? "Battle" : "Skirmish") + i.ToString());
            }

            string selected = MenuActionSink.SingleBattlesSelectedId ?? "";
            if (string.IsNullOrWhiteSpace(selected) || !scene.MissionIds.Contains(selected, StringComparer.OrdinalIgnoreCase))
                selected = scene.MissionIds[0];

            scene.SelectedMissionId = selected;

            foreach (MbListNode list in scene.Nodes.OfType<MbListNode>())
            {
                if (list.Name.Equals("S", StringComparison.OrdinalIgnoreCase))
                {
                    list.Items.Clear();
                    list.Items.AddRange(scene.MissionIds);
                }
            }
        }

        private void ApplyDynamicMissionData(MbScene scene)
        {
            if (string.IsNullOrWhiteSpace(scene.SelectedMissionId))
                return;

            string relDir = scene.ShowBattles ? @"Missions\Battles" : @"Missions\Skirmish";
            string relTxt = relDir + @"\" + scene.SelectedMissionId + ".txt";
            scene.DescriptionText = ReadTextSmart(relTxt);

            foreach (string relImg in FindPreviewCandidates(relDir, scene.SelectedMissionId))
            {
                scene.PreviewPath = relImg;
                break;
            }

            foreach (MbTextNode t in scene.Nodes.OfType<MbTextNode>())
            {
                bool isDesc = t.Role.Equals("MissionDescription", StringComparison.OrdinalIgnoreCase)
                           || t.Actions.Any(a => a.Name.Equals("va_MissDescription", StringComparison.OrdinalIgnoreCase))
                           || (t.Name ?? "").IndexOf(@"Missions\", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isDesc)
                {
                    if (!string.IsNullOrWhiteSpace(scene.DescriptionText))
                    {
                        t.Message = scene.DescriptionText;
                        t.IsFormatted = t.Message.IndexOf('{') >= 0;
                    }
                    t.Role = "MissionDescription";
                }
            }

            foreach (MbBitPictureNode bp in scene.Nodes.OfType<MbBitPictureNode>())
            {
                if (bp.Role.Equals("MissionPreview", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(bp.FileName))
                    bp.FileName = scene.PreviewPath ?? "";
            }
        }

        private string ReadTextSmart(string relPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relPath) || !_fs.Exists(relPath))
                    return "";
                string abs = _fs.ResolvePath(relPath);
                if (!File.Exists(abs))
                    return "";

                byte[] bytes = File.ReadAllBytes(abs);
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

                try
                {
                    Encoding cp1251 = Encoding.GetEncoding(1251);
                    string s1251 = cp1251.GetString(bytes);
                    if (LooksReadable(s1251))
                        return s1251;
                }
                catch { }

                string utf8 = Encoding.UTF8.GetString(bytes);
                return utf8 ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static bool LooksReadable(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            int readable = 0;
            int weird = 0;
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                    readable++;
                if (c == '\uFFFD')
                    weird++;
            }
            return readable > 0 && weird < Math.Max(4, s.Length / 20);
        }

        private IEnumerable<string> FindPreviewCandidates(string relDir, string missionId)
        {
            string[] exts = { ".jpg", ".jpeg", ".bmp", ".png", ".tga" };
            foreach (string ext in exts)
            {
                string relImg = relDir + @"\" + missionId + ext;
                if (_fs.Exists(relImg))
                    yield return relImg;
            }

            string absDir = null;
            try
            {
                absDir = _fs.ResolvePath(relDir);
            }
            catch
            {
                absDir = null;
            }

            if (string.IsNullOrEmpty(absDir) || !Directory.Exists(absDir))
                yield break;

            foreach (string f in Directory.GetFiles(absDir))
            {
                string ext = Path.GetExtension(f);
                bool ok = false;
                foreach (string e in exts)
                {
                    if (ext.Equals(e, StringComparison.OrdinalIgnoreCase)) { ok = true; break; }
                }
                if (!ok) continue;
                string stem = Path.GetFileNameWithoutExtension(f);
                if (stem.Equals(missionId, StringComparison.OrdinalIgnoreCase))
                {
                    yield return relDir + @"\" + Path.GetFileName(f);
                    yield break;
                }
            }
        }

        private void RemapSpecialButtonActions(MbGpTextButtonNode node)
        {
            if (node == null) return;
            bool hasBack = node.Actions.Any(a => a.Name.Equals("cva_MM_Cancel", StringComparison.OrdinalIgnoreCase) ||
                                                 a.Name.Equals("cva_MM_CancelInDesk", StringComparison.OrdinalIgnoreCase));
            if (hasBack || (node.Message ?? "").IndexOf("BACK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                node.Actions.Clear();
                node.Actions.Add(new UiAction { Name = "cva_Battles_Back", Payload = "Single" });
            }
        }

        private static bool ShouldRenderTabDesk(string parentDialogId, bool showBattles, bool showLoad)
        {
            if (string.IsNullOrWhiteSpace(parentDialogId)) return true;
            if (parentDialogId.Equals("Skirmish", StringComparison.OrdinalIgnoreCase)) return !showBattles && !showLoad;
            if (parentDialogId.Equals("Battles", StringComparison.OrdinalIgnoreCase)) return showBattles && !showLoad;
            if (parentDialogId.Equals("Load", StringComparison.OrdinalIgnoreCase)) return showLoad;
            return true;
        }

        private string GetComboDisplayText(MbComboBoxNode node)
        {
            if (node.Actions.Any(a => a.Name.Equals("cva_BR_PlDiff", StringComparison.OrdinalIgnoreCase)))
                return "Легко";
            if (node.Actions.Any(a => a.Name.Equals("cva_BR_PlRace", StringComparison.OrdinalIgnoreCase)))
                return "Случайно";
            if (node.Actions.Any(a => a.Name.Equals("cva_BR_PlColor", StringComparison.OrdinalIgnoreCase)))
                return "Случайно";
            if (node.Actions.Any(a => a.Name.Equals("cva_BR_PlTeam", StringComparison.OrdinalIgnoreCase)))
                return "1";
            if (node.Actions.Any(a => a.Name.IndexOf("Arc", StringComparison.OrdinalIgnoreCase) >= 0))
                return MenuActionSink.SingleBattlesArcadeModeEnabled ? "Включен" : "Выключен";
            return "Случайно";
        }

        private string ResolveSource(string xmlDir, string src)
        {
            src = NormalizePath(src);
            if (string.IsNullOrWhiteSpace(src)) return "";

            if (_fs.Exists(src))
                return src;

            string fromDir = NormalizePath(Path.Combine(xmlDir ?? "", src));
            if (_fs.Exists(fromDir))
                return fromDir;

            if (src.StartsWith(@"#work#\", StringComparison.OrdinalIgnoreCase))
            {
                string tail = src.Substring(7);
                string probe1 = NormalizePath(Path.Combine(xmlDir ?? "", tail));
                if (_fs.Exists(probe1))
                    return probe1;
                string probe2 = NormalizePath(Path.Combine(@"Dialogs\v", tail));
                if (_fs.Exists(probe2))
                    return probe2;
                string probe3 = NormalizePath(Path.Combine(@"Dialogs\Interface", tail));
                if (_fs.Exists(probe3))
                    return probe3;
            }

            string probeV = NormalizePath(Path.Combine(@"Dialogs\v", src));
            if (_fs.Exists(probeV))
                return probeV;

            string probeI = NormalizePath(Path.Combine(@"Dialogs\Interface", src));
            if (_fs.Exists(probeI))
                return probeI;

            return "";
        }

        private static string SanitizeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "<Root />";
            s = s.Replace("<>", "<Root>").Replace("</>", "</Root>");
            s = s.Replace("Hint section", "Hint_section");
            s = s.Replace("Position&Width", "Position_Width");
            return s;
        }

        private static string NormalizePath(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "";
            return p.Trim().Replace('/', '\\');
        }

        private static string Get(XElement el, string name)
        {
            XElement ch = el.Element(name);
            return ch != null ? (ch.Value ?? "") : "";
        }

        private static int ToInt(string s, int def = 0)
        {
            int v;
            return int.TryParse((s ?? "").Trim(), out v) ? v : def;
        }

        private static bool ToBool(string s, bool def)
        {
            if (string.IsNullOrWhiteSpace(s)) return def;
            if (s.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return def;
        }

        private static void FillActions(List<UiAction> dst, XElement el)
        {
            XElement va = el.Element("v_Actions");
            if (va == null) return;
            foreach (XElement act in va.Elements())
            {
                dst.Add(new UiAction
                {
                    Name = act.Name.LocalName,
                    Payload = (act.Value ?? "").Trim()
                });
            }
        }

        private sealed class NaturalStringComparer : IComparer<string>
        {
            public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

            public int Compare(string a, string b)
            {
                a = a ?? "";
                b = b ?? "";
                return CompareImpl(a, b);
            }

            private static int CompareImpl(string a, string b)
            {
                int ia = 0, ib = 0;
                while (ia < a.Length && ib < b.Length)
                {
                    if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
                    {
                        long va = 0;
                        while (ia < a.Length && char.IsDigit(a[ia])) { va = va * 10 + (a[ia] - '0'); ia++; }
                        long vb = 0;
                        while (ib < b.Length && char.IsDigit(b[ib])) { vb = vb * 10 + (b[ib] - '0'); ib++; }
                        int cmpNum = va.CompareTo(vb);
                        if (cmpNum != 0) return cmpNum;
                    }
                    else
                    {
                        char ca = char.ToUpperInvariant(a[ia]);
                        char cb = char.ToUpperInvariant(b[ib]);
                        int cmp = ca.CompareTo(cb);
                        if (cmp != 0) return cmp;
                        ia++; ib++;
                    }
                }
                return a.Length.CompareTo(b.Length);
            }
        }
    }
}
