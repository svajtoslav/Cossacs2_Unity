using System;
using System.Collections.Generic;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters.Profiles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    /// <summary>
    /// V395C: M_PROF_SEL stays XML-owned.
    ///
    /// The original engine does not paint replacement labels/portraits over the
    /// profile dialog. cva_Prof* changes the state of the controls that already
    /// exist in M_PROF_SEL.DIALOGSSYSTEM.XML.  V395C follows that contract:
    /// OptionsRenderer renders the XML controls and Menu14ActionStateRuntime applies
    /// cva_ProfList/cva_ProfCur_* directly to those exact controls.  This renderer
    /// now owns no replacement profile-list rows.
    /// </summary>
    public sealed class ProfileSelectionRenderer
    {
        private readonly OptionsRenderer _inner = new OptionsRenderer();
        public static int SelectedIndex { get; private set; }
        public static bool DeletePending { get; private set; }

        public void Render(UiDesk desk, CoreFileSystem fs, BaseUiRenderer.RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            C2ProfileRuntime14.EnsureLoaded();
            SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Mathf.Max(0, C2ProfileRuntime14.Count - 1));

            ApplyDeleteDeskVisibility(desk, DeletePending);

            var local = new BaseUiRenderer.RenderOptions
            {
                FontResourcePath = opt.FontResourcePath,
                FontSize = opt.FontSize,
                NormalColor = opt.NormalColor,
                HoverColor = opt.HoverColor,
                DisabledColor = opt.DisabledColor,
                CanvasScaleMode = opt.CanvasScaleMode,
                ReferenceResolution = opt.ReferenceResolution,
                VerboseLogs = opt.VerboseLogs,
                DrawDebugOutline = opt.DrawDebugOutline,
                FillResolutionCombos = false
            };

            // Important: no V395_* replacement Text/Image controls are created.
            // Menu14ActionStateRuntime.SetFrameState pass in OptionsRenderer updates
            // the actual XML TextButton/GPPicture instances.
            _inner.Render(desk, fs, local, sink, loc);

            GameObject canvas = GameObject.Find("C2_OptionsCanvas");
            if (canvas == null) return;

            // V395G: static labels remain XML-owned.  The final 1.4 data puts
            // several profile-menu strings (including #PM_Difficulty) in
            // Text\add\text1.txt, while the legacy LocDb default probes
            // Text\text1.txt.  Resolve only still-literal #keys from that
            // source file; do not replace labels that LocDb already resolved.
            ResolveAddTextFallbacks(canvas);

            // V396A7R5 full audit: the original cva_ProfDel_Desk changes ONLY the
            // Delete desk visibility. It never removes/disables the profile description
            // DialogsDesk. Therefore its clipping + VScroller must exist both before
            // and while the delete modal is visible. The runtime desk is then placed
            // immediately below the source Delete subtree so the modal remains on top.
            var desc = canvas.GetComponent<ProfileDescriptionViewportV395F>();
            if (desc == null) desc = canvas.AddComponent<ProfileDescriptionViewportV395F>();
            desc.Initialize(desk);

            Debug.Log(
                $"[C2:PROFILE V396A7R5] SelProfile xml-owned controls={desk?.Children?.Count ?? 0} " +
                $"profiles={C2ProfileRuntime14.Count} selected={SelectedIndex} deletePending={(DeletePending ? 1 : 0)} " +
                $"replacements=0 listRuntime=cva_ProfList descDesk=1 modalDoesNotDisableDesc=1");
        }

        private static Dictionary<string, string> s_addTextFallbacks;

        private static void ResolveAddTextFallbacks(GameObject canvas)
        {
            if (canvas == null) return;
            if (s_addTextFallbacks == null)
            {
                s_addTextFallbacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string raw = Menu14ActionStateRuntime.ReadSourceText(@"Text\add\text1.txt", System.Text.Encoding.GetEncoding(1251));
                if (!string.IsNullOrEmpty(raw))
                {
                    string[] lines = raw.Replace("\r", string.Empty).Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i]?.Trim();
                        if (string.IsNullOrEmpty(line) || line[0] != '#') continue;
                        int p = 1;
                        while (p < line.Length && !char.IsWhiteSpace(line[p])) p++;
                        if (p <= 1 || p >= line.Length) continue;
                        string key = line.Substring(0, p);
                        string value = line.Substring(p).Trim();
                        if (!string.IsNullOrEmpty(value)) s_addTextFallbacks[key] = value;
                    }
                }
            }

            if (s_addTextFallbacks.Count == 0) return;
            foreach (TextMeshProUGUI t in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t == null || string.IsNullOrEmpty(t.text) || t.text[0] != '#') continue;
                if (s_addTextFallbacks.TryGetValue(t.text.Trim(), out string value))
                    t.text = value;
            }
        }

        public static void SetSelectedIndex(int index)
        {
            SelectedIndex = Mathf.Max(0, index);
        }

        public static void SetDeletePending(bool value)
        {
            DeletePending = value;
        }

        private static void ApplyDeleteDeskVisibility(UiDesk desk, bool visible)
        {
            if (desk?.Children == null) return;

            UiNode modalRoot = null;
            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (HasAction(n, "cva_ProfDel_Desk"))
                {
                    modalRoot = n;
                    break;
                }
            }
            if (modalRoot == null) return;

            // Original cva_ProfDel_Desk::SetFrameState overwrites the root desk's
            // Visible flag with vCurProfDel. The root's serialized <Visible>false>
            // is therefore NOT a permanent visibility mask. Descendants keep their
            // own XML Visible flags and inherit the runtime parent state.
            var byId = new Dictionary<int, UiNode>();
            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (n != null && n.SourceId >= 0) byId[n.SourceId] = n;
            }

            var subtree = new HashSet<int> { modalRoot.SourceId };
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < desk.Children.Count; i++)
                {
                    UiNode n = desk.Children[i];
                    if (n == null || subtree.Contains(n.SourceId)) continue;
                    if (subtree.Contains(n.ParentSourceId))
                    {
                        subtree.Add(n.SourceId);
                        changed = true;
                    }
                }
            } while (changed);

            modalRoot.Visible = visible;
            modalRoot.Enabled = modalRoot.LocalEnabled;

            // Parser order is parent-before-child, so parent effective visibility
            // has already been recomputed when a child is reached.
            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (n == null || n.SourceId == modalRoot.SourceId || !subtree.Contains(n.SourceId)) continue;
                bool parentVisible = byId.TryGetValue(n.ParentSourceId, out UiNode parent) && parent != null && parent.Visible;
                bool parentEnabled = byId.TryGetValue(n.ParentSourceId, out parent) && parent != null && parent.Enabled;
                n.Visible = parentVisible && n.LocalVisible;
                n.Enabled = parentEnabled && n.LocalEnabled;
            }

            Debug.Log($"[C2:PROFILE DELETE V396A7R2] XML desk='Delete' runtimeVisible={(visible ? 1 : 0)} subtree={subtree.Count} sourceLocalVisibility=preserved");
        }

        private static bool HasAction(UiNode node, string actionName)
        {
            if (node?.Actions == null) return false;
            foreach (UiAction a in node.Actions)
                if (a != null && string.Equals(a.Name, actionName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// V395F: source-parent runtime for cva_ProfCur_Desc.
    ///
    /// M_PROF_SEL contains the description TextButton inside a real DialogsDesk:
    ///   desk  x=226 y=163 w=254 h=157 (relative to the 510x370 profile desk)
    ///   text  x=9   y=0   w=222 h=114, MaxWidth=235
    /// The unified parser already resolves X/Y to screen coordinates while preserving
    /// ParentSourceId.  V395E incorrectly inferred the desk origin from a rendered TMP
    /// RectTransform.  V395F takes the parent UiNode directly from the parsed XML, so
    /// viewport, clipping and scrollbar all use the source hierarchy rather than a
    /// guessed screen position.
    /// </summary>
    public sealed class ProfileDescriptionViewportV395F : MonoBehaviour, IScrollHandler
    {
        private RectTransform _deskRoot;
        private RectTransform _viewport;
        private RectTransform _textRt;
        private TextMeshProUGUI _text;
        private RectTransform _thumb;
        private RectTransform _track;
        private float _max;
        private float _pos;
        private float _localX;
        private float _localY;
        private float _lineLy;
        private float _scrLy;
        private float _btnLy;

        public void Initialize(UiDesk desk)
        {
            if (!Menu14ActionStateRuntime.TryGetBoundControlForAction("cva_ProfCur_Desc", out UiNode rawNode, out GameObject go)) return;
            UiTextButton node = rawNode as UiTextButton;
            if (node == null || go == null || desk?.Children == null) return;

            _text = go.GetComponent<TextMeshProUGUI>() ?? go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_text == null) return;
            _textRt = _text.rectTransform;

            UiDialogsDesk parentNode = null;
            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (n != null && n.SourceId == node.ParentSourceId)
                {
                    parentNode = n as UiDialogsDesk;
                    break;
                }
            }
            if (parentNode == null)
            {
                Debug.LogError($"[C2:PROFILE V396A7R5] cva_ProfCur_Desc parent SourceId={node.ParentSourceId} is not DialogsDesk");
                return;
            }

            RectTransform root = transform as RectTransform;
            if (root == null) return;

            DestroyOldRuntimeRoot(root);

            float deskX = parentNode.X;
            float deskY = parentNode.Y;
            float deskW = parentNode.Width > 0 ? parentNode.Width : 254f;
            float deskH = parentNode.Height > 0 ? parentNode.Height : 157f;

            var deskGo = new GameObject("V396A7R5_ProfCurDescDesk", typeof(RectTransform));
            deskGo.transform.SetParent(root, false);
            _deskRoot = (RectTransform)deskGo.transform;
            _deskRoot.anchorMin = _deskRoot.anchorMax = new Vector2(0f, 1f);
            _deskRoot.pivot = new Vector2(0f, 1f);
            _deskRoot.anchoredPosition = new Vector2(deskX, -deskY);
            _deskRoot.sizeDelta = new Vector2(deskW, deskH);

            ListDeskSourceRuntime14.TemplateSpec borderSpec;
            bool hasBorder = OptionsRenderer.DrawSourceDialogsDeskBorderV396A7R5(
                _deskRoot, deskW, deskH, parentNode.Border, out borderSpec);

            float left = hasBorder ? borderSpec.BorderLeftMargin : 0f;
            float top = hasBorder ? borderSpec.BorderTopMargin : 0f;
            float right = hasBorder ? borderSpec.BorderRightMargin : 0f;
            float bottom = hasBorder ? borderSpec.BorderBottomMargin : 0f;

            // Original SimpleDialog::Process:
            // IntersectWindows(x+l,y+u,x1-r,y1-d);
            // child ShiftDialog(x+l-XShift, y+u-YShift).
            float clipW = Mathf.Max(1f, deskW - left - right);
            float clipH = Mathf.Max(1f, deskH - top - bottom);
            var vpGo = new GameObject("ContentClip", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            vpGo.transform.SetParent(_deskRoot, false);
            _viewport = (RectTransform)vpGo.transform;
            _viewport.anchorMin = _viewport.anchorMax = new Vector2(0f, 1f);
            _viewport.pivot = new Vector2(0f, 1f);
            _viewport.anchoredPosition = new Vector2(left, -top);
            _viewport.sizeDelta = new Vector2(clipW, clipH);
            Image vpHit = vpGo.GetComponent<Image>();
            vpHit.color = new Color(1f, 1f, 1f, 0.001f);
            vpHit.raycastTarget = true;

            _textRt.SetParent(_viewport, false);
            _textRt.anchorMin = _textRt.anchorMax = new Vector2(0f, 1f);
            _textRt.pivot = new Vector2(0f, 1f);

            int maxWidth = node.MaxWidth > 0 && node.MaxWidth < 10000 ? node.MaxWidth : Mathf.Max(1, (int)node.Width);
            _text.enableAutoSizing = false;
            _text.fontSize = 14f;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.verticalAlignment = VerticalAlignmentOptions.Top;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.raycastTarget = false;
            _text.ForceMeshUpdate();

            float prefH = Mathf.Max(node.Height > 0 ? node.Height : 1f,
                _text.GetPreferredValues(_text.text, maxWidth, 0f).y);
            _textRt.sizeDelta = new Vector2(maxWidth, prefH);

            // DialogsDesk::Process computes maxy from child y1 BEFORE parent shift:
            // maxy -= (height-1) - top - bottom - 12.
            float visibleThreshold = (deskH - 1f) - top - bottom - 12f;
            _max = Mathf.Max(0f, node.LocalY + prefH - visibleThreshold);
            _pos = Mathf.Clamp(parentNode.YShift, 0f, _max);
            _localX = node.LocalX - parentNode.XShift;
            _localY = node.LocalY;

            if (parentNode.EnableVerticalScroller && hasBorder && !string.IsNullOrEmpty(borderSpec.VScrollerGPFile))
                BuildSourceVScroller(_deskRoot, deskW, deskH, borderSpec);

            ApplyPosition();
            PlaceBelowDeleteModal(root);

            Debug.Log(
                $"[C2:PROFILE DESC V396A7R5] source=DialogsDesk::Process parent={parentNode.SourceId} " +
                $"desk=({deskX:0},{deskY:0},{deskW:0},{deskH:0}) border='{parentNode.Border}' " +
                $"margins=({left:0},{top:0},{right:0},{bottom:0}) clip=({left:0},{top:0},{clipW:0},{clipH:0}) " +
                $"textLocal=({node.LocalX},{node.LocalY}) maxWidth={maxWidth} prefH={prefH:0.0} " +
                $"scrollMax={_max:0.0} vscroll={(parentNode.EnableVerticalScroller ? 1 : 0)} " +
                $"deletePending={(ProfileSelectionRenderer.DeletePending ? 1 : 0)}");
        }

        private static void DestroyOldRuntimeRoot(RectTransform root)
        {
            string[] names =
            {
                "V396A7R5_ProfCurDescDesk",
                "V395F_ProfCurDescDesk",
                "V395E_ProfCurDescDesk",
                "V395E_ProfCurDescScroll"
            };
            for (int i = 0; i < names.Length; i++)
            {
                Transform old = root.Find(names[i]);
                if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            }
        }

        private void PlaceBelowDeleteModal(RectTransform root)
        {
            if (_deskRoot == null || root == null) return;
            if (ProfileSelectionRenderer.DeletePending &&
                Menu14ActionStateRuntime.TryGetBoundControlForAction("cva_ProfDel_Desk", out UiNode modalNode, out GameObject modalRoot) &&
                modalRoot != null && modalRoot.transform.parent == root)
            {
                int modalIndex = modalRoot.transform.GetSiblingIndex();
                _deskRoot.SetSiblingIndex(Mathf.Max(0, modalIndex));
                Debug.Log($"[C2:PROFILE DESC V396A7R5] zOrder=below_Delete modalIndex={modalIndex}");
            }
            else
            {
                _deskRoot.SetAsLastSibling();
            }
        }

        private void BuildSourceVScroller(
            RectTransform parent,
            float deskW,
            float deskH,
            ListDeskSourceRuntime14.TemplateSpec spec)
        {
            // DialogsDesk::Process/addNewGP_VScrollBar source geometry.
            float x = (deskW - 1f) - spec.VScrollerDXRight;
            float y = spec.VScrollerDYTop;
            float ly = deskH - spec.VScrollerDYTop - spec.VScrolledDYBottom;
            if (ly <= 0f) return;

            Sprite upNormal = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 0);
            Sprite upHover = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 1);
            Sprite downNormal = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 2);
            Sprite downHover = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 3);
            Sprite thumbSp = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 4);
            Sprite c0 = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 5);
            Sprite c1 = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 6);
            Sprite c2 = OptionsRenderer.LoadSourceGpFrameV396A7R5(spec.VScrollerGPFile, 7);
            if (upNormal == null || downNormal == null || c0 == null) return;

            float width = upNormal.rect.width;
            _btnLy = upNormal.rect.height;
            _lineLy = Mathf.Max(1f, ly - 2f * _btnLy);
            _scrLy = thumbSp != null ? thumbSp.rect.height : 0f;

            var barGo = new GameObject("VScroller_Source_V396A7R5", typeof(RectTransform));
            barGo.transform.SetParent(parent, false);
            var bar = (RectTransform)barGo.transform;
            bar.anchorMin = bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = new Vector2(x, -y);
            bar.sizeDelta = new Vector2(width, ly);

            float centerY0 = Mathf.Floor(_btnLy / 2f);
            float centerY1 = ly - 1f - centerY0;
            var trackGo = new GameObject("TrackClip", typeof(RectTransform), typeof(RectMask2D));
            trackGo.transform.SetParent(bar, false);
            _track = (RectTransform)trackGo.transform;
            _track.anchorMin = _track.anchorMax = new Vector2(0f, 1f);
            _track.pivot = new Vector2(0f, 1f);
            _track.anchoredPosition = new Vector2(-64f, -centerY0);
            _track.sizeDelta = new Vector2(width + 128f, Mathf.Max(1f, centerY1 - centerY0 + 1f));

            Sprite[] centers = { c0, c1, c2 };
            float centerH = Mathf.Max(1f, c0.rect.height);
            int n = Mathf.FloorToInt(ly / centerH);
            for (int i = 0; i <= n; i++)
            {
                Sprite sp = centers[i % 3] ?? c0;
                if (sp == null) continue;
                MakeImage(_track, "Track_" + i, sp,
                    new Vector2(64f, -(i * centerH)),
                    new Vector2(sp.rect.width, sp.rect.height), false);
            }

            Button up = MakeButton(bar, "Up", upNormal, upHover,
                new Vector2(0f, 0f), new Vector2(upNormal.rect.width, upNormal.rect.height));
            Button down = MakeButton(bar, "Down", downNormal, downHover,
                new Vector2(0f, -(ly - downNormal.rect.height)), new Vector2(downNormal.rect.width, downNormal.rect.height));
            if (up != null) up.onClick.AddListener(() => ScrollBy(-32f));
            if (down != null) down.onClick.AddListener(() => ScrollBy(32f));

            if (thumbSp != null)
            {
                var th = MakeImage(bar, "Thumb", thumbSp, Vector2.zero,
                    new Vector2(thumbSp.rect.width, thumbSp.rect.height), false);
                _thumb = th != null ? th.transform as RectTransform : null;
            }

            UpdateThumb();
            Debug.Log(
                $"[C2:PROFILE DESC V396A7R5] vscrollGP='{spec.VScrollerGPFile}' " +
                $"xy=({x:0},{y:0}) Ly={ly:0} btnLy={_btnLy:0} lineLy={_lineLy:0} scrLy={_scrLy:0} " +
                $"OnesDy=32 ScrDy={ly - 32f:0}");
        }

        private static GameObject MakeImage(RectTransform parent, string name, Sprite sp, Vector2 pos, Vector2 size, bool raycast)
        {
            if (parent == null || sp == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var im = go.GetComponent<Image>();
            im.sprite = sp;
            im.type = Image.Type.Simple;
            im.preserveAspect = false;
            im.raycastTarget = raycast;
            return go;
        }

        private static Button MakeButton(RectTransform parent, string name, Sprite normal, Sprite hover, Vector2 pos, Vector2 size)
        {
            GameObject go = MakeImage(parent, name, normal, pos, size, true);
            if (go == null) return null;
            var b = go.AddComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();
            var colors = b.colors;
            colors.fadeDuration = 0f;
            b.colors = colors;
            var swap = go.AddComponent<SourceScrollHoverV396A7R5>();
            swap.Image = go.GetComponent<Image>();
            swap.Normal = normal;
            swap.Hover = hover ?? normal;
            return b;
        }

        private sealed class SourceScrollHoverV396A7R5 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Image;
            public Sprite Normal;
            public Sprite Hover;
            public void OnPointerEnter(PointerEventData eventData) { if (Image != null && Hover != null) Image.sprite = Hover; }
            public void OnPointerExit(PointerEventData eventData) { if (Image != null && Normal != null) Image.sprite = Normal; }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData == null) return;
            // Original DialogsDesk wheel updates VScroller::SPos directly; use the
            // source OnesDy=32 quantum rather than the former arbitrary 18 pixels.
            float dir = Mathf.Sign(-eventData.scrollDelta.y);
            if (Mathf.Abs(dir) > 0.01f) ScrollBy(dir * 32f);
        }

        private void ScrollBy(float d)
        {
            _pos = Mathf.Clamp(_pos + d, 0f, _max);
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (_textRt != null)
                _textRt.anchoredPosition = new Vector2(_localX, -_localY + _pos);
            UpdateThumb();
        }

        private void UpdateThumb()
        {
            if (_thumb == null) return;
            if (_max <= 0f || _scrLy <= 0f)
            {
                _thumb.gameObject.SetActive(false);
                return;
            }
            _thumb.gameObject.SetActive(true);
            float travel = Mathf.Max(0f, _lineLy - _scrLy);
            float y = _btnLy + (_pos * travel) / _max;
            _thumb.anchoredPosition = new Vector2(0f, -y);
        }
    }

}
