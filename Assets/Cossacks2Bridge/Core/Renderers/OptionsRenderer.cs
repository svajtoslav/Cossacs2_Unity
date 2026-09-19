using Cossacks2Bridge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cossacks2Bridge.UnityAdapters.AddProfile;
using Cossacks2Bridge.UnityAdapters;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    public sealed class OptionsRenderer : BaseUiRenderer
    {
        private const bool EnableVitLogs = false; // set true only when debugging VitButtonTiled
        private const bool VerboseLayoutLogs = false; // old GPT-era layout spam; keep errors/warnings visible
        private static int s_lastBuildFrame = -1;
        private static string s_lastBuildSource = string.Empty;
        private static object parent;

        // V395O: EW2 campaign statistics must use the final 1.4 shkala.jpg
        // from the effective game DataRoot, not a stale StreamingAssets copy.
        private static Texture2D s_campaignStatsBackgroundTextureV395O;
        private static Sprite s_campaignStatsBackgroundSpriteV395O;
        private static string s_campaignStatsBackgroundPathV395O = string.Empty;

        // V387B3: original 1.1/1.4 runtime state behind
        // cva_Multi_ManualServer / cva_Multi_ManualIPServer.
        private static bool s_multiManualServerV387B3 = false;
        private static string s_multiManualIpV387B3 = "";

        public override void Render(UiDesk desk, CoreFileSystem fs, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            // ═══════════════════════════════════════════════════════════
            // ЗАЩИТА ОТ ПОВТОРНЫХ ВЫЗОВОВ В ТОТ ЖЕ FRAME
            // ═══════════════════════════════════════════════════════════
            int frame = Time.frameCount;
            string buildSource = desk?.SourcePath ?? string.Empty;
            if (s_lastBuildFrame == frame && string.Equals(s_lastBuildSource, buildSource, StringComparison.OrdinalIgnoreCase)) return;
            s_lastBuildFrame = frame;
            s_lastBuildSource = buildSource;

            RenderCounter++;

            // 1) Найти/создать Canvas
            RectTransform root = EnsureOptionsCanvas(opt);
            bool isMulti = !string.IsNullOrEmpty(desk?.SourcePath) &&
                           desk.SourcePath.IndexOf("M_Multi", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isEW2CampaignStatsV395M = !string.IsNullOrEmpty(desk?.SourcePath) &&
                           desk.SourcePath.IndexOf("EW2_CampaignStats.DialogsSystem.xml", StringComparison.OrdinalIgnoreCase) >= 0;
            long campaignStatsRenderStartV395Q = isEW2CampaignStatsV395M
                ? System.Diagnostics.Stopwatch.GetTimestamp()
                : 0L;


            // 2) Очистить Canvas
            DestroyAllChildrenImmediate(root);

            // V395P: the EW2 statistics screen is expensive to rebuild and all of
            // its Resources sprites are immutable during the session.  Clearing the
            // renderer cache here forced the same GP/UI frames to be resolved again
            // every time Statistics was opened.  Keep the cache for this one screen;
            // preserve the previous behavior everywhere else.
            if (!isEW2CampaignStatsV395M)
                ResFrames.ClearCache();
            else
                Debug.Log("[C2:CAMPSTAT PERF V395P] ResFrames cache preserved=1");

            // V391: begin one common action/state runtime for the XML screen.
            // This keeps SetFrameState-style actions bound to the exact UiNode
            // instances produced by Menu14UnifiedLoader instead of duplicating
            // the state logic inside individual screen controllers.
            Menu14ActionStateRuntime.BeginScreen(desk, fs, loc);

            // V395O: final 1.4 EW2 statistics background is an external bitmap:
            //   Data\Interf3\background\shkala.jpg
            // The project can contain an older 1.1 bitmap under StreamingAssets,
            // so this screen explicitly resolves the XML asset from the effective
            // game DataRoot first.  Existing generic BitPicture behavior is kept as
            // fallback if the real 1.4 file is unavailable.
            bool campaignStatsBackgroundFromDataRootV395O = false;
            if (isEW2CampaignStatsV395M)
                campaignStatsBackgroundFromDataRootV395O = TryCreateCampaignStatsBackgroundFromDataRootV395O(root, fs);

            // 4) Фон
            bool hasBackground = desk?.Children != null && desk.Children.Any(n => n is UiBitPicture);
            if (isMulti)
            {
                // Original M_Multi background is INTERF3\ELEMENTS\BACKGROUND sprite 6.
                // Draw it explicitly before all controls so this screen never falls back
                // to the Unity camera/sky gradient if generic GP resource routing changes.
                CreateMultiBackgroundV387A3(root);
            }
            else if (!hasBackground)
            {
                var bgPic = new UiBitPicture
                {
                    FileName = "Interf3/background/options.jpg",
                    X = 0,
                    Y = 0,
                    Width = 1024,
                    Height = 768,
                    Visible = true
                };
                CreateBitPicture(root, bgPic, fs, opt);
            }

            if (desk?.Children == null) return;

            // V389: XML-driven visual ancestry.  The unified V388 parser preserves
            // SourceId/ParentSourceId, so child editors can use the exact original
            // parent VitButton GP/sprite surface instead of Unity-picked colors.
            var nodesBySourceId = desk.Children
                .Where(n => n != null && n.SourceId >= 0)
                .GroupBy(n => n.SourceId)
                .ToDictionary(g => g.Key, g => g.First());

            // V396A7R2: the profile-delete confirmation is a hidden source subtree
            // whose root visibility is controlled by cva_ProfDel_Desk.  Render it
            // in XML/source order after the normal profile screen, rather than
            // flattening it into the type-grouped passes below.
            UiNode profileDeleteRootV396A7R2 = FindNodeWithActionV396A7R2(desk, "cva_ProfDel_Desk");
            HashSet<int> profileDeleteSubtreeV396A7R2 = BuildSubtreeIdsV396A7R2(desk, profileDeleteRootV396A7R2);

            // V395Q: Menu14's current model is flattened, but original DialogsSystem
            // visibility is hierarchical. A child with Visible=true must still NOT be
            // rendered when any ancestor DialogsDesk is Visible=false. EW2 statistics
            // contains a large hidden post-game/template subtree: 109 TextButtons,
            // 25 DialogsDesks and 3 GPPictures are individually visible but live
            // below hidden parents. V395P was instantiating those controls anyway,
            // which dominated the opening delay although the actual stats pass was
            // only a few milliseconds.
            int campaignStatsHierarchySkippedV395Q = 0;
            if (isEW2CampaignStatsV395M)
            {
                for (int i = 0; i < desk.Children.Count; i++)
                {
                    UiNode n = desk.Children[i];
                    if (n != null && n.Visible && !IsEffectivelyVisibleV395Q(n, nodesBySourceId))
                        campaignStatsHierarchySkippedV395Q++;
                }
            }

            UiVitButton multiInputPrototypeV389 = null;
            if (isMulti)
            {
                var nickNode = desk.Children.OfType<UiInputBox>()
                    .FirstOrDefault(n => HasAction(n, "cva_MU_NickInput"));
                if (nickNode != null &&
                    nodesBySourceId.TryGetValue(nickNode.ParentSourceId, out UiNode nickParent))
                {
                    multiInputPrototypeV389 = nickParent as UiVitButton;
                }
            }

            bool isAddProfile = desk.Children.Any(n =>
    n?.Actions != null && n.Actions.Any(a =>
        a != null && !string.IsNullOrEmpty(a.Name) &&
        a.Name.StartsWith("cva_ProfAdd_", StringComparison.OrdinalIgnoreCase)));

            // 1) BitPicture (фоны)
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;
                if (node is UiBitPicture pic)
                {
                    string bitFileV395O = (pic.FileName ?? string.Empty).Replace('\\', '/');
                    bool isCampaignStatsShkalaV395O =
                        isEW2CampaignStatsV395M &&
                        bitFileV395O.Equals("Interf3/background/shkala.JPG", StringComparison.OrdinalIgnoreCase);

                    // When the final 1.4 bitmap has already been loaded directly
                    // from DataRoot, do not draw the stale project copy on top of it.
                    if (isCampaignStatsShkalaV395O && campaignStatsBackgroundFromDataRootV395O)
                        continue;

                    CreateBitPicture(root, pic, fs, opt);
                }
            }

            // 2) GPPicture (декор)
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;
                if (node is UiGPPicture gp)
                {
                    string gpFidV395M = (gp.FileID ?? string.Empty).Replace('\\', '/');
                    bool isMultiRootBg = isMulti &&
                        gpFidV395M.Equals("INTERF3/ELEMENTS/BACKGROUND", StringComparison.OrdinalIgnoreCase) &&
                        gp.SpriteID == 6 && gp.X == 0 && gp.Y == 0;

                    // V395M: EW2_CampaignStats.xml contains an old nested
                    // Interf3\mainmenu sprite-0 placeholder under a DialogsDesk.
                    // The original 1.4 statistics screen does NOT show that battle
                    // picture: the graph grid remains visible through this area.
                    // Our flattened Unity renderer was incorrectly promoting this
                    // nested GPPicture to a root-level 375x235 Image, so it covered
                    // the player tables/graph and visibly escaped its source desk.
                    // Suppress only this exact campaign-statistics placeholder;
                    // no other GPPicture or screen is affected.
                    bool suppressCampaignStatsMainMenuPlaceholderV395M =
                        isEW2CampaignStatsV395M &&
                        gpFidV395M.Equals("Interf3/mainmenu", StringComparison.OrdinalIgnoreCase) &&
                        gp.SpriteID == 0;

                    if (suppressCampaignStatsMainMenuPlaceholderV395M)
                    {
                        Debug.Log($"[C2:CAMPSTAT V395M] suppressed nested placeholder gp='{gp.FileID}' sprite={gp.SpriteID} sourceId={gp.SourceId} parentSourceId={gp.ParentSourceId}");
                        continue;
                    }

                    if (!isMultiRootBg)
                        CreateGPPicture(root, gp, fs, opt);
                }
            }

            // 3) VitButton / InputBox.  Multi is special: the normal screen has
            // exactly one live nickname editor.  Manual-IP controls belong to a
            // different state and must not overlap/capture clicks here.
            int multiNickInputs = 0;
            int multiHiddenInputs = 0;
            int multiManualSuppressed = 0;
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;

                if (isMulti && IsMultiManualIpNodeV387B2(node))
                {
                    multiManualSuppressed++;
                    continue;
                }

                if (node is UiVitButton vb)
                {
                    // V395H: a VitButton nested under a ListDesk <Element> is the
                    // XML prototype used by ListDesk::AddElement().  It is not an
                    // independent root-level control and must never be drawn here.
                    if (nodesBySourceId.TryGetValue(vb.ParentSourceId, out UiNode vbParentV395H) &&
                        vbParentV395H is UiListDesk)
                        continue;

                    bool isDecorative = (vb.Actions == null || vb.Actions.Count == 0);
                    if (isDecorative)
                        CreateVitButtonTiled(vb, root);
                    else
                        CreateVitButton(vb, root, opt, sink);
                }
                else if (node is UiInputBox ib)
                {
                    bool isNickInput = HasAction(ib, "cva_MU_NickInput");

                    if (isMulti)
                    {
                        // The source screen contains one nickname InputBox nested in
                        // its decorative VitButton. Any other InputBox visible in the
                        // flattened runtime is a duplicate/inactive state. Never let
                        // it receive focus or cover the real nickname editor.
                        if (!isNickInput || multiNickInputs > 0)
                        {
                            multiHiddenInputs++;
                            continue;
                        }
                    }

                    UiVitButton inputParentV389 = null;
                    if (nodesBySourceId.TryGetValue(ib.ParentSourceId, out UiNode parentNodeV389))
                        inputParentV389 = parentNodeV389 as UiVitButton;

                    CreateInputBox(ib, root, opt, inputParentV389);
                    if (isMulti && isNickInput) multiNickInputs++;
                }
            }

            // 4) TextButton
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;
                if (isMulti && IsMultiManualIpNodeV387B2(node)) { multiManualSuppressed++; continue; }
                if (node is UiTextButton btn)
                {
                    int before = root.childCount;
                    CreateTextButton(root, btn, opt, sink, loc, MenuOverrideDb.Resolve);
                    if (root.childCount > before)
                    {
                        GameObject created = root.GetChild(root.childCount - 1).gameObject;
                        Menu14ActionStateRuntime.RegisterControl(btn, created, fs);

                        // V395O: the source XML title is #EW2_Stat with
                        // MenuTextWhite for all states.  The generic TextButton
                        // path was leaving this label in the dark generic menu
                        // color, making it nearly invisible on the title plaque.
                        if (isEW2CampaignStatsV395M &&
                            string.Equals(btn.MessageKey, "#EW2_Stat", StringComparison.OrdinalIgnoreCase))
                            FixCampaignStatsTitleV395O(created);
                    }
                }
            }


            // 5) GP_TextButton
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;
                if (isMulti && IsMultiManualIpNodeV387B2(node)) { multiManualSuppressed++; continue; }
                if (node is UiGPTextButton gpBtn)
                {
                    int before = root.childCount;
                    CreateGPTextButton(root, gpBtn, opt, sink, loc);
                    if (root.childCount > before)
                    {
                        GameObject created = root.GetChild(root.childCount - 1).gameObject;
                        Menu14ActionStateRuntime.RegisterControl(gpBtn, created, fs);
                    }
                }
            }
            // 5) ListDesk (список подключений)
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;
                if (isMulti && IsMultiManualIpNodeV387B2(node)) { multiManualSuppressed++; continue; }
                if (node is UiListDesk ld)
                {
                    CreateListDeskVisual(ld, root, opt);
                }
            }
             
            // 6) CheckBox, Slider, ComboBox
            int cbIndex = 0;
            foreach (var node in desk.Children)
            {
                if (!node.Visible || (isEW2CampaignStatsV395M && !IsEffectivelyVisibleV395Q(node, nodesBySourceId))) continue;
                if (profileDeleteSubtreeV396A7R2 != null && profileDeleteSubtreeV396A7R2.Contains(node.SourceId)) continue;
                if (isMulti && IsMultiManualIpNodeV387B2(node)) { multiManualSuppressed++; continue; }

                if (node is UiCheckBox cb)
                {
                    cbIndex++;
                    CreateCheckBox(cb, desk, root, opt, sink, loc, cbIndex);
                }
                else if (node is UiSlider sl)
                {
                    // AddProfile: allow ONLY the profile-related scrollers (portrait selector etc.)
                    if (!isAddProfile)
                    {
                        CreateSlider(sl, desk, root, opt, sink, loc);
                    }
                    else
                    {
                        if (HasActionPrefix(sl, "cva_ProfAdd_"))
                            CreateSlider(sl, desk, root, opt, sink, loc);
                    }
                }
                else if (node is UiComboBox combo)
                {
                    CreateComboBox(combo, desk, fs, root, opt, sink, loc);
                }
            }

            // V396A7R2: render the original M_PROF_SEL/Delete subtree last, in
            // parser/source order. This preserves overlay -> Main border -> header
            // -> buttons/portrait/text z-order while using the existing XML model.
            if (profileDeleteRootV396A7R2 != null && profileDeleteRootV396A7R2.Visible)
                RenderProfileDeleteModalV396A7R2(desk, root, fs, opt, sink, loc,
                    profileDeleteRootV396A7R2, profileDeleteSubtreeV396A7R2);

            // V391: one post-build SetFrameState pass. This mirrors the
            // original per-frame action state update for controls already bound
            // to the runtime, including cva_ProfAdd_RaceFlg.
            Menu14ActionStateRuntime.ApplyAllFrameStates();

            if (isEW2CampaignStatsV395M)
            {
                double renderMsV395Q = (System.Diagnostics.Stopwatch.GetTimestamp() - campaignStatsRenderStartV395Q)
                    * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                Debug.Log(
                    $"[C2:CAMPSTAT PERF V395Q] renderMs={renderMsV395Q:F2} parsedNodes={desk.Children.Count} " +
                    $"hierarchySkipped={campaignStatsHierarchySkippedV395Q} unityChildren={root.childCount}");
            }

            if (isMulti)
            {
                // The clean M_Multi XML does not contain the manual-server row;
                // the original engine exposes it through runtime SetFrameState
                // (cva_Multi_ManualServer / cva_Multi_ManualIPServer).  V387B2
                // suppressed leaked fake ManualIP actions and accidentally removed
                // the legitimate visible row.  Recreate that original runtime row
                // explicitly, while keeping the leaked nodes suppressed.
                CreateMultiManualServerRowV389(root, multiInputPrototypeV389);
                HideMultiServerIpTokenV387A3(root);
                Debug.Log(
                    $"[C2:MENU14 V389] screen=Multi originalBackgroundSprite=6 " +
                    $"nickInputs={multiNickInputs} hiddenInactiveInputs={multiHiddenInputs} " +
                    $"manualIpSuppressed={multiManualSuppressed} manualIpRuntimeRow=1 " +
                    $"manualInputSurfaceXml={(multiInputPrototypeV389 != null ? 1 : 0)} " +
                    $"manualEnabled={(s_multiManualServerV387B3 ? 1 : 0)} rawServerIpHidden=1");
            }
        }

        private static UiNode FindNodeWithActionV396A7R2(UiDesk desk, string actionName)
        {
            if (desk?.Children == null || string.IsNullOrWhiteSpace(actionName)) return null;
            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (HasAction(n, actionName)) return n;
            }
            return null;
        }

        private static HashSet<int> BuildSubtreeIdsV396A7R2(UiDesk desk, UiNode root)
        {
            if (desk?.Children == null || root == null || root.SourceId < 0) return null;
            var result = new HashSet<int> { root.SourceId };
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < desk.Children.Count; i++)
                {
                    UiNode n = desk.Children[i];
                    if (n == null || result.Contains(n.SourceId)) continue;
                    if (result.Contains(n.ParentSourceId))
                    {
                        result.Add(n.SourceId);
                        changed = true;
                    }
                }
            } while (changed);
            return result;
        }

        private static void RenderProfileDeleteModalV396A7R2(
            UiDesk desk,
            RectTransform root,
            CoreFileSystem fs,
            RenderOptions opt,
            IUiActionSink sink,
            LocDb loc,
            UiNode modalRoot,
            HashSet<int> subtree)
        {
            if (desk?.Children == null || root == null || modalRoot == null || subtree == null) return;

            int rendered = 0;
            int borders = 0;
            int hotkeys = 0;

            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (n == null || !subtree.Contains(n.SourceId) || !n.Visible) continue;

                if (n is UiDialogsDesk dd)
                {
                    // The modal root is a 1024x768 input-owning DialogsDesk. Keep a
                    // transparent raycast surface so the visible modal blocks the
                    // underlying profile screen exactly while vCurProfDel is true.
                    if (n.SourceId == modalRoot.SourceId)
                    {
                        var blocker = new GameObject("DialogsDesk_Delete_SourceRoot", typeof(RectTransform), typeof(Image));
                        blocker.transform.SetParent(root, false);
                        RectTransform brt = (RectTransform)blocker.transform;
                        brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
                        brt.pivot = new Vector2(0f, 1f);
                        brt.anchoredPosition = new Vector2(dd.X, -dd.Y);
                        brt.sizeDelta = new Vector2(Mathf.Max(1f, dd.Width), Mathf.Max(1f, dd.Height));
                        Image bi = blocker.GetComponent<Image>();
                        bi.color = new Color(1f, 1f, 1f, 0f);
                        bi.raycastTarget = true;
                        Menu14ActionStateRuntime.RegisterControl(dd, blocker, fs);
                        rendered++;
                    }

                    if (!string.IsNullOrWhiteSpace(dd.Border) &&
                        !dd.Border.Equals("NullBorder", StringComparison.OrdinalIgnoreCase) &&
                        ListDeskSourceRuntime14.TryGetBorderTemplate(dd.Border, out ListDeskSourceRuntime14.TemplateSpec borderSpec) &&
                        borderSpec != null)
                    {
                        var frame = new GameObject("DialogsDesk_Border_" + SafeName(dd.Border), typeof(RectTransform));
                        frame.transform.SetParent(root, false);
                        RectTransform frt = (RectTransform)frame.transform;
                        frt.anchorMin = frt.anchorMax = new Vector2(0f, 1f);
                        frt.pivot = new Vector2(0f, 1f);
                        frt.anchoredPosition = new Vector2(dd.X, -dd.Y);
                        frt.sizeDelta = new Vector2(Mathf.Max(1f, dd.Width), Mathf.Max(1f, dd.Height));
                        DrawFilledRect3V395K(frame.transform, dd.Width, dd.Height, borderSpec);
                        borders++;
                        rendered++;
                    }
                    continue;
                }

                if (n is UiGPPicture gp)
                {
                    CreateGPPicture(root, gp, fs, opt);
                    rendered++;
                    continue;
                }

                if (n is UiVitButton vb)
                {
                    bool interactive = vb.Actions != null && vb.Actions.Count > 0;
                    if (!interactive)
                    {
                        // Use the same DrawHeaderEx2-style source path as runtime
                        // ListDesk buttons, but without inventing a hit target/text.
                        var holder = new GameObject("VitButton_SourceDecor", typeof(RectTransform));
                        holder.transform.SetParent(root, false);
                        RectTransform hrt = (RectTransform)holder.transform;
                        hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 1f);
                        hrt.pivot = new Vector2(0f, 1f);
                        hrt.anchoredPosition = new Vector2(vb.X, -vb.Y);
                        hrt.sizeDelta = new Vector2(Mathf.Max(1f, vb.Width), Mathf.Max(1f, vb.Height));
                        CreateListDeskVitButtonVisualV395J(holder.transform, vb.GP_File, vb.SpritePassive,
                            vb.SpriteDx, vb.Width, vb.Height, vb.OneSprited, vb.DisableCycling, "Passive");
                        rendered++;
                        continue;
                    }

                    string message = loc?.Resolve(vb.MessageKey) ?? vb.MessageKey ?? string.Empty;
                    int state = Mathf.Max(0, vb.State);
                    GameObject buttonGo = CreateListDeskElementFromSourceTemplateV395J(
                        root,
                        message,
                        state,
                        vb.Enabled,
                        () =>
                        {
                            for (int ai = 0; ai < vb.Actions.Count; ai++)
                            {
                                UiAction a = vb.Actions[ai];
                                if (a == null) continue;
                                try { sink?.OnAction(vb.MessageKey, a); }
                                catch (Exception ex) { Debug.LogError($"[C2:PROFILE DELETE V396A7R2] action error {a.Name}: {ex}"); }
                            }
                        },
                        vb.X, vb.Y, vb.Width, vb.Height,
                        vb.GP_File, vb.SpritePassive, vb.SpriteActive, vb.SpriteDx,
                        vb.FontPassive, vb.FontOver, vb.FontDx, vb.FontDy, vb.Align,
                        vb.OneSprited, vb.DisableCycling);

                    if (buttonGo != null)
                    {
                        Menu14ActionStateRuntime.RegisterControl(vb, buttonGo, fs);
                        Button b = buttonGo.GetComponent<Button>();
                        if (b != null && !string.IsNullOrWhiteSpace(vb.HotKey) &&
                            !vb.HotKey.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                        {
                            var hk = buttonGo.AddComponent<SourceHotKeyInvokesButtonV396A7R2>();
                            hk.Target = b;
                            hk.HotKey = vb.HotKey;
                            hotkeys++;
                        }
                    }
                    rendered++;
                    continue;
                }

                if (n is UiTextButton tb)
                {
                    int before = root.childCount;
                    CreateTextButton(root, tb, opt, sink, loc, MenuOverrideDb.Resolve);
                    if (root.childCount > before)
                    {
                        GameObject created = root.GetChild(root.childCount - 1).gameObject;
                        Menu14ActionStateRuntime.RegisterControl(tb, created, fs);
                    }
                    rendered++;
                    continue;
                }

                if (n is UiGPTextButton gpt)
                {
                    int before = root.childCount;
                    CreateGPTextButton(root, gpt, opt, sink, loc);
                    if (root.childCount > before)
                        Menu14ActionStateRuntime.RegisterControl(gpt, root.GetChild(root.childCount - 1).gameObject, fs);
                    rendered++;
                }
            }

            Debug.Log($"[C2:PROFILE DELETE V396A7R2] rendered source='{desk.SourcePath}' desk='{modalRoot.Name}' " +
                      $"nodes={rendered} borders={borders} hotkeys={hotkeys} order=XML_PREORDER parser=Menu14UnifiedLoader");
            Debug.Log("[C2:UI SEAMFIX V396A7R7] DrawRect4=source_exact DrawHeaderEx2=inclusive clipGuard=1px sourcePhase=i_mod_3 genericPath=source_start_x0");
            Debug.Log("[C2:UI SOURCE TRANSFORM V396A7R4] ParentFrame_GetMatrix=enabled pointSampling=enabled internet_menu=rotate_90_270_not_overlay");
        }

        private sealed class SourceHotKeyInvokesButtonV396A7R2 : MonoBehaviour
        {
            public Button Target;
            public string HotKey;

            private void Update()
            {
                if (Target == null || !Target.interactable || !Target.gameObject.activeInHierarchy) return;
                bool pressed = false;
                string key = HotKey ?? string.Empty;
#if ENABLE_INPUT_SYSTEM
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null)
                {
                    if (key.Equals("ENTER", StringComparison.OrdinalIgnoreCase))
                        pressed = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                    else if (key.Equals("ESC", StringComparison.OrdinalIgnoreCase) || key.Equals("ESCAPE", StringComparison.OrdinalIgnoreCase))
                        pressed = keyboard.escapeKey.wasPressedThisFrame;
                }
#else
                if (key.Equals("ENTER", StringComparison.OrdinalIgnoreCase))
                    pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                else if (key.Equals("ESC", StringComparison.OrdinalIgnoreCase) || key.Equals("ESCAPE", StringComparison.OrdinalIgnoreCase))
                    pressed = Input.GetKeyDown(KeyCode.Escape);
#endif
                if (pressed) Target.onClick.Invoke();
            }
        }

        private static bool IsEffectivelyVisibleV395Q(UiNode node, Dictionary<int, UiNode> nodesBySourceId)
        {
            if (node == null || !node.Visible) return false;
            if (nodesBySourceId == null || nodesBySourceId.Count == 0) return true;

            int parentId = node.ParentSourceId;
            int guard = 0;
            while (parentId >= 0 && guard++ < 512)
            {
                if (!nodesBySourceId.TryGetValue(parentId, out UiNode parentNode) || parentNode == null)
                    break;
                if (!parentNode.Visible) return false;

                int next = parentNode.ParentSourceId;
                if (next == parentId) break;
                parentId = next;
            }
            return true;
        }

        private static bool TryCreateCampaignStatsBackgroundFromDataRootV395O(RectTransform root, CoreFileSystem fs)
        {
            if (root == null) return false;

            string dataRoot = Menu14ActionStateRuntime.CurrentLogicalDataRoot;
            if (string.IsNullOrWhiteSpace(dataRoot))
                dataRoot = fs?.DataRoot ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                Debug.LogWarning("[C2:CAMPSTAT BG V395O] DataRoot unavailable; using generic BitPicture fallback");
                return false;
            }

            string[] candidates =
            {
                Path.Combine(dataRoot, "Interf3", "background", "shkala.jpg"),
                Path.Combine(dataRoot, "Interf3", "background", "shkala.JPG"),
                Path.Combine(dataRoot, "INTERF3", "BACKGROUND", "SHKALA.JPG")
            };

            string path = string.Empty;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    path = candidates[i];
                    break;
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[C2:CAMPSTAT BG V395O] final14 shkala.jpg not found under DataRoot='{dataRoot}'; using generic BitPicture fallback");
                return false;
            }

            try
            {
                if (s_campaignStatsBackgroundTextureV395O == null ||
                    s_campaignStatsBackgroundSpriteV395O == null ||
                    !string.Equals(s_campaignStatsBackgroundPathV395O, path, StringComparison.OrdinalIgnoreCase))
                {
                    if (s_campaignStatsBackgroundSpriteV395O != null)
                        UnityEngine.Object.Destroy(s_campaignStatsBackgroundSpriteV395O);
                    if (s_campaignStatsBackgroundTextureV395O != null)
                        UnityEngine.Object.Destroy(s_campaignStatsBackgroundTextureV395O);

                    byte[] bytes = File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.name = "C2_Final14_shkala_V395O";
                    if (!tex.LoadImage(bytes, false))
                    {
                        UnityEngine.Object.Destroy(tex);
                        Debug.LogWarning($"[C2:CAMPSTAT BG V395O] LoadImage failed path='{path}'; using generic BitPicture fallback");
                        return false;
                    }
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;

                    s_campaignStatsBackgroundTextureV395O = tex;
                    s_campaignStatsBackgroundSpriteV395O = Sprite.Create(
                        tex,
                        new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        1f);
                    s_campaignStatsBackgroundSpriteV395O.name = "C2_Final14_shkala_sprite_V395O";
                    s_campaignStatsBackgroundPathV395O = path;
                }

                var go = new GameObject("C2_CampaignStats_Background_V395O", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                RectTransform rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(1024f, 768f);

                Image img = go.GetComponent<Image>();
                img.sprite = s_campaignStatsBackgroundSpriteV395O;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
                img.raycastTarget = false;
                go.transform.SetAsFirstSibling();

                Debug.Log($"[C2:CAMPSTAT BG V395O] source=DataRoot path='{path}' bitmap={s_campaignStatsBackgroundTextureV395O.width}x{s_campaignStatsBackgroundTextureV395O.height} draw=1024x768");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[C2:CAMPSTAT BG V395O] failed path='{path}' error='{ex.Message}'; using generic BitPicture fallback");
                return false;
            }
        }

        private static void FixCampaignStatsTitleV395O(GameObject created)
        {
            if (created == null) return;

            TextMeshProUGUI[] labels = created.GetComponentsInChildren<TextMeshProUGUI>(true);
            Color32 sourceWhite = ResolveC2FontColor("MenuTextWhite", new Color32(255, 247, 239, 255));
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI tmp = labels[i];
                if (tmp == null) continue;
                tmp.enableAutoSizing = false;
                tmp.fontSize = ResolveC2FontSize("MenuTextWhite", 14f);
                tmp.color = sourceWhite;
                tmp.alignment = TextAlignmentOptions.Midline;
                tmp.fontStyle = FontStyles.Normal;
                tmp.raycastTarget = false;
            }

            // XML source: x=468 y=68 Width=85 Height=12, Align=Center,
            // Active/Passive/DisabledFont=MenuTextWhite. Geometry remains XML-owned.
            Debug.Log($"[C2:CAMPSTAT TITLE V395O] message=#EW2_Stat font=MenuTextWhite color={sourceWhite} labels={labels.Length} geometry=xml");
        }

        private static void CreateMultiManualServerRowV389(RectTransform root, UiVitButton inputSurfacePrototype)
        {
            if (root == null) return;

            // V387B3A: exact 1024x768 client-space placement.
            // Clean M_Multi.DialogsSystem.xml does NOT serialize the manual-IP row;
            // it is driven by cva_Multi_ManualServer / cva_Multi_ManualIPServer.
            // We therefore anchor it to the exact XML bottom-button desk position:
            //   buttons desk: x=420, y=602, w=503, h=44
            // and place the manual-IP row 28 px above it, matching the original 1.4 UI.
            const float labelX = 435f;
            const float buttonsDeskY = 602f;
            const float rowY = buttonsDeskY - 28f; // 574
            const float checkX = 580f;
            const float checkY = rowY - 1f;        // 573
            const float inputX = 600f;
            const float inputY = rowY - 1f;        // 573
            const float inputW = 310f;
            const float inputH = 20f;

            var labelGo = new GameObject("MultiManualIP_Label_V387B3", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(root, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 1);
            lrt.anchoredPosition = new Vector2(labelX, -rowY);
            lrt.sizeDelta = new Vector2(125, 20);
            var ltmp = labelGo.GetComponent<TextMeshProUGUI>();
            ltmp.text = "IP сервера";
            ltmp.fontSize = 14f;
            ltmp.color = new Color32(45, 35, 30, 255);
            ltmp.alignment = TextAlignmentOptions.Left;
            ltmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            ltmp.raycastTarget = false;

            const string cbFolder = "interf3_elements_checkbox_frames";
            var spOff = ResFrames.GetByName(cbFolder, "frame_0000");
            var spOn = ResFrames.GetByName(cbFolder, "frame_0001");

            var checkGo = new GameObject("MultiManualServer_CheckBox_V387B3", typeof(RectTransform), typeof(Image), typeof(Button));
            checkGo.transform.SetParent(root, false);
            var crt = (RectTransform)checkGo.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
            crt.pivot = new Vector2(0, 1);
            crt.anchoredPosition = new Vector2(checkX, -checkY);
            // Original CheckBox XML uses the same Interf3\elements\checkbox GP, 19x19.
            crt.sizeDelta = new Vector2(19, 19);
            var cimg = checkGo.GetComponent<Image>();
            cimg.sprite = s_multiManualServerV387B3 ? (spOn ?? spOff) : (spOff ?? spOn);
            cimg.preserveAspect = false;
            cimg.raycastTarget = true;
            var cbtn = checkGo.GetComponent<Button>();
            cbtn.targetGraphic = cimg;

            // V389: render the manual-IP editor with the SAME original GP/sprite
            // surface that V388 parsed for the network nickname InputBox parent.
            // No hand-picked white/gray/cherry Unity colors are used here.
            if (inputSurfacePrototype != null)
            {
                var surface = new UiVitButton
                {
                    Name = "MultiManualIP_Surface_V389",
                    GP_File = inputSurfacePrototype.GP_File,
                    State = inputSurfacePrototype.State,
                    SpritePassive = inputSurfacePrototype.SpritePassive,
                    SpriteActive = inputSurfacePrototype.SpriteActive,
                    SpriteDx = inputSurfacePrototype.SpriteDx,
                    OneSprited = inputSurfacePrototype.OneSprited,
                    X = (int)inputX,
                    Y = (int)inputY,
                    Width = (int)inputW,
                    Height = (int)inputH,
                    Visible = true,
                    Enabled = true
                };
                CreateVitButtonTiled(surface, root);
            }
            else
            {
                Debug.LogWarning("[C2:MULTI V389] XML input surface prototype not found; manual IP editor will stay transparent rather than invent a color");
            }

            var inputGo = new GameObject("InputBox_MultiManualIP_V389", typeof(RectTransform), typeof(Image));
            inputGo.transform.SetParent(root, false);
            var irt = (RectTransform)inputGo.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 1);
            irt.pivot = new Vector2(0, 1);
            irt.anchoredPosition = new Vector2(inputX, -inputY);
            irt.sizeDelta = new Vector2(inputW, inputH);
            var bg = inputGo.GetComponent<Image>();
            bg.color = Color.clear;
            bg.raycastTarget = true;

            var textAreaGo = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textAreaGo.transform.SetParent(inputGo.transform, false);
            var art = (RectTransform)textAreaGo.transform;
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(5, 1);
            art.offsetMax = new Vector2(-5, -1);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textAreaGo.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 14f;
            tmp.color = new Color32(45, 35, 30, 255);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.raycastTarget = false;
            tmp.richText = false;

            var input = inputGo.AddComponent<TMP_InputField>();
            input.textComponent = tmp;
            input.textViewport = art;
            input.targetGraphic = bg;
            input.characterLimit = 32; // original cva_Multi_ManualIPServer
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.interactable = s_multiManualServerV387B3;
            input.readOnly = !s_multiManualServerV387B3;
            input.SetTextWithoutNotify(s_multiManualIpV387B3 ?? string.Empty);

            var cb = input.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.disabledColor = new Color32(255, 255, 255, 180);
            cb.colorMultiplier = 1f;
            input.colors = cb;

            input.onValueChanged.AddListener(v =>
            {
                s_multiManualIpV387B3 = (v ?? string.Empty).Trim();
            });

            cbtn.onClick.AddListener(() =>
            {
                s_multiManualServerV387B3 = !s_multiManualServerV387B3;
                cimg.sprite = s_multiManualServerV387B3 ? (spOn ?? spOff) : (spOff ?? spOn);
                input.interactable = s_multiManualServerV387B3;
                input.readOnly = !s_multiManualServerV387B3;
                // Visual surface stays the original XML/GP skin in both states;
                // state only controls editability, like the source runtime action.
                if (s_multiManualServerV387B3)
                    input.ActivateInputField();
                else
                    input.DeactivateInputField();
                Debug.Log($"[C2:MULTI V389] manual server={(s_multiManualServerV387B3 ? 1 : 0)} ip='{s_multiManualIpV387B3}'");
            });

            inputGo.transform.SetAsLastSibling();
            checkGo.transform.SetAsLastSibling();
            labelGo.transform.SetAsLastSibling();
        }

        private static bool IsMultiManualIpNodeV387B2(UiNode node)
        {
            if (node == null) return false;

            if (HasAction(node, "cva_Multi_ManualIPServer") ||
                HasAction(node, "cva_Multi_ManualServer"))
                return true;

            if (node is UiInputBox ib)
            {
                string a = ib.Action ?? "";
                if (a.IndexOf("ManualIP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.IndexOf("ManualServer", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            if (node is UiListDesk ld)
            {
                string a = ld.Action ?? "";
                if (a.IndexOf("ManualIP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.IndexOf("ManualServer", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static void CreateMultiBackgroundV387A3(RectTransform root)
        {
            Sprite sp = LoadSpriteFromResources("INTERF3_ELEMENTS_BACKGROUND_frames", "frame_0006");
            if (sp == null)
            {
                Debug.LogWarning("[C2:MENU14 V387A3] Multi background frame_0006 not found");
                return;
            }

            var go = new GameObject("GPPicture_Multi_Background_V387A3", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1024, 768);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            go.transform.SetAsFirstSibling();
        }

        private static void HideMultiServerIpTokenV387A3(RectTransform root)
        {
            if (root == null) return;
            var labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels)
            {
                if (label == null) continue;
                string t = (label.text ?? string.Empty).Trim();
                if (t.Equals("#SERVERIP", StringComparison.OrdinalIgnoreCase) ||
                    t.Equals("SERVERIP", StringComparison.OrdinalIgnoreCase))
                {
                    label.text = string.Empty;
                }
            }
        }

        // ===================== CANVAS / CLEANUP =====================

        private static RectTransform EnsureOptionsCanvas(RenderOptions opt)
        {
            var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            RectTransform reusable = null;

            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;

                if (canvas.gameObject.name == "C2_OptionsCanvas")
                {
                    if (reusable == null)
                    {
                        reusable = canvas.GetComponent<RectTransform>();
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                    }
                }
                else if (canvas.gameObject.name == "C2_MainMenuCanvas")
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }
                else if (canvas.gameObject.name == "C2_MenuCanvas")
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }
            }

            if (reusable != null) return reusable;

            return CreateCanvasClean("C2_OptionsCanvas", opt);
        }

        private static RectTransform CreateCanvasClean(string canvasName, RenderOptions opt)
        {
            var canvasGO = new GameObject(canvasName);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 1000;

            var scaler = canvasGO.AddComponent<CanvasScaler>();

            // ═══════════════════════════════════════════════════════════
            // ВАРИАНТ A: Целочисленное масштабирование
            // ═══════════════════════════════════════════════════════════
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // Вычисляем целочисленный множитель
            int scaleFactorX = Mathf.Max(1, Screen.width / 1024);
            int scaleFactorY = Mathf.Max(1, Screen.height / 768);
            int scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY); // Берём меньший

            scaler.scaleFactor = scaleFactor; // 1x, 2x, 3x - ЦЕЛОЕ число!

            // ═══════════════════════════════════════════════════════════
            // ИЛИ ВАРИАНТ B: ScaleWithScreenSize но с Expand
            // ═══════════════════════════════════════════════════════════
            // scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // scaler.referenceResolution = new Vector2(1024, 768);
            // scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // scaler.matchWidthOrHeight = 0f; // Игнорируется при Expand

            var gr = canvasGO.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = true;
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            var root = canvasGO.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            EnsureEventSystem();

            return root;
        }


        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                var go = new GameObject("EventSystem");
                existing = go.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                var legacy = existing.GetComponent<StandaloneInputModule>();
                if (legacy != null) legacy.enabled = false;
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[C2:UNITY66 V387B1] EventSystem uses InputSystemUIInputModule");
            }
#else
            if (existing.GetComponent<StandaloneInputModule>() == null)
            {
                existing.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[C2:UNITY66 V387B1] EventSystem uses StandaloneInputModule");
            }
#endif
        }
        /// <summary>
        /// ListDesk по логике DrawFilledRect + DrawRect4 из оригинала
        /// Маппинг BD фреймов:
        /// 0 - левый нижний угол, 1 - правый нижний угол
        /// 2 - левый верхний угол, 3 - правый верхний угол
        /// 4 - верхняя горизонтальная линия (текстура горизонтальная)
        /// 5 - нижняя горизонтальная линия (текстура горизонтальная)
        /// 7 - левая вертикальная линия (текстура вертикальная)
        /// 8 - правая вертикальная линия (текстура вертикальная)
        /// 6, 9, 10, 11 - наполнитель
        /// </summary>
        /// <summary>
        /// ListDesk по логике DrawFilledRect + DrawRect4 из оригинала
        /// ИСПРАВЛЕНО: позиции углов используют реальные размеры спрайтов
        /// </summary>
        private static void CreateListDeskVisual(UiListDesk ld, RectTransform parent, RenderOptions opt)
        {
            if (ld == null || parent == null) return;

            // V395K: when the source model was recovered from the same DialogsSystem
            // XML + Dialogs/borders.xml, render the desk through a direct port of
            // StdBorder::Draw -> DrawFilledRect3 -> DrawRect4 and the DialogsDesk
            // VScroller creation formula. No corner-size shrink, no hand-swapped
            // frames, no extra filler frame.
            if (ListDeskSourceRuntime14.TryGet(ld.SourceId, out ListDeskSourceRuntime14.TemplateSpec spec) &&
                spec != null && !string.IsNullOrEmpty(spec.BorderGPFile))
            {
                CreateListDeskVisualSourceExactV395K(ld, parent, spec);
                return;
            }

            CreateListDeskVisualLegacyV395J(ld, parent, opt);
        }

        private static void CreateListDeskVisualLegacyV395J(UiListDesk ld, RectTransform parent, RenderOptions opt)
        {
            if (ld == null || parent == null) return;

            // Диагностика Canvas
            var canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (VerboseLayoutLogs) Debug.Log($"[CANVAS DIAG] scaleFactor={canvas.scaleFactor}, " +
                    $"referencePixelsPerUnit={canvas.referencePixelsPerUnit}, " +
                    $"pixelPerfect={canvas.pixelPerfect}");
            }

            const string folder = "interf3_elements_border_BD_frames";

            float w = ld.Width;
            float h = ld.Height;

            // ═══════════════════════════════════════════════════════════
            // 1. ЗАГРУЗКА СПРАЙТОВ
            // ═══════════════════════════════════════════════════════════

            // Углы (по документации: 0=LB, 1=RB, 2=LT, 3=RT)
            Sprite spCornerLB = LoadSpriteFromResources(folder, "frame_0000");
            Sprite spCornerRB = LoadSpriteFromResources(folder, "frame_0001");
            Sprite spCornerLT = LoadSpriteFromResources(folder, "frame_0002");
            Sprite spCornerRT = LoadSpriteFromResources(folder, "frame_0003");

            // Линии (4=Top, 5=Bottom, 7=Left, 8=Right)
            Sprite spLineTop = LoadSpriteFromResources(folder, "frame_0004");
            Sprite spLineBottom = LoadSpriteFromResources(folder, "frame_0005");
            Sprite spLineLeft = LoadSpriteFromResources(folder, "frame_0007");
            Sprite spLineRight = LoadSpriteFromResources(folder, "frame_0008");

            // Наполнитель
            Sprite[] fill = {
        LoadSpriteFromResources(folder, "frame_0006"),
        LoadSpriteFromResources(folder, "frame_0009"),
        LoadSpriteFromResources(folder, "frame_0010"),
        LoadSpriteFromResources(folder, "frame_0011")
    };

            // ═══════════════════════════════════════════════════════════
            // 2. РЕАЛЬНЫЕ РАЗМЕРЫ из спрайтов (не константа S!)
            // ═══════════════════════════════════════════════════════════
            float cornerW = spCornerLT != null ? spCornerLT.rect.width : 32f;
            float cornerH = spCornerLT != null ? spCornerLT.rect.height : 32f;

            // Для безопасности берём максимум из всех углов
            if (spCornerLB != null) { cornerW = Mathf.Max(cornerW, spCornerLB.rect.width); cornerH = Mathf.Max(cornerH, spCornerLB.rect.height); }
            if (spCornerRB != null) { cornerW = Mathf.Max(cornerW, spCornerRB.rect.width); cornerH = Mathf.Max(cornerH, spCornerRB.rect.height); }
            if (spCornerRT != null) { cornerW = Mathf.Max(cornerW, spCornerRT.rect.width); cornerH = Mathf.Max(cornerH, spCornerRT.rect.height); }

            float lineThickness = spLineTop != null ? spLineTop.rect.height : cornerH;

            if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Real sizes: cornerW={cornerW}, cornerH={cornerH}, lineThickness={lineThickness}");

            // ═══════════════════════════════════════════════════════════
            // 3. КОНТЕЙНЕР
            // ═══════════════════════════════════════════════════════════
            var container = new GameObject($"C2Xml_ListDesk_{ld.SourceId}", typeof(RectTransform));
            container.transform.SetParent(parent, false);

            var rt = (RectTransform)container.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(ld.X, -ld.Y);
            rt.sizeDelta = new Vector2(w, h);

            // ═══════════════════════════════════════════════════════════
            // 4. НАПОЛНИТЕЛЬ (первый слой - самый нижний)
            // ═══════════════════════════════════════════════════════════
            float fillStartX = cornerW / 2f;
            float fillStartY = cornerH / 2f;
            float fillW = w - cornerW;
            float fillH = h - cornerH;

            var fillContainer = new GameObject("Fill", typeof(RectTransform), typeof(RectMask2D));
            fillContainer.transform.SetParent(container.transform, false);
            fillContainer.transform.SetAsFirstSibling(); // ← В САМЫЙ НИЗ!

            var fillRt = (RectTransform)fillContainer.transform;
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 1);
            fillRt.anchoredPosition = new Vector2(fillStartX, -fillStartY);
            fillRt.sizeDelta = new Vector2(fillW, fillH);

            float tileSize = fill[0] != null ? fill[0].rect.width : 32f;
            int nx = Mathf.CeilToInt(fillW / tileSize);
            int ny = Mathf.CeilToInt(fillH / tileSize);

            for (int iy = 0; iy < ny; iy++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    int idx = (ix + iy) % fill.Length;
                    Sprite sp = fill[idx] ?? fill[0];
                    if (sp == null) continue;

                    var tile = new GameObject($"F_{ix}_{iy}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(fillContainer.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(ix * tileSize, -iy * tileSize);
                    tileRt.sizeDelta = new Vector2(tileSize, tileSize);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                }
            }
            // ═══════════════════════════════════════════════════════════
            // 5. ЛИНИИ (второй слой - над наполнителем)
            // ═══════════════════════════════════════════════════════════
            float innerW = w - cornerW * 2;
            float innerH = h - cornerH * 2;

            if (VerboseLayoutLogs) Debug.Log($"[ListDesk LINES] innerW={innerW}, innerH={innerH}, cornerW={cornerW}, cornerH={cornerH}");
            if (VerboseLayoutLogs) Debug.Log($"[ListDesk LINES] spLineTop={spLineTop != null}, spLineBottom={spLineBottom != null}");
            if (VerboseLayoutLogs) Debug.Log($"[ListDesk LINES] spLineLeft={spLineLeft != null}, spLineRight={spLineRight != null}");

            // Верхняя горизонтальная линия — ПОДНЯТА на 1px
            if (spLineTop != null && innerW > 0)
            {
                float topY = 1f;
                if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Creating Line_Top at X={cornerW}, Y={topY}, size={innerW}x{spLineTop.rect.height}");
                CreateTiledLineWithLog(container.transform, "Line_Top", spLineTop,
                    cornerW, topY, innerW, spLineTop.rect.height, isHorizontal: true);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Top: sprite={spLineTop != null}, innerW={innerW}");
            }

            // Нижняя горизонтальная линия — ОПУЩЕНА на 1px
            if (spLineBottom != null && innerW > 0)
            {
                float bottomY = -(h - spLineBottom.rect.height) - 1f;
                if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Creating Line_Bottom at X={cornerW}, Y={bottomY}, size={innerW}x{spLineBottom.rect.height}");
                CreateTiledLineWithLog(container.transform, "Line_Bottom", spLineBottom,
                    cornerW, bottomY, innerW, spLineBottom.rect.height, isHorizontal: true);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Bottom: sprite={spLineBottom != null}, innerW={innerW}");
            }

            // Левая вертикальная линия (без изменений)
            if (spLineLeft != null && innerH > 0)
            {
                if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Creating Line_Left at X=0, Y={-cornerH}, size={spLineLeft.rect.width}x{innerH}");
                CreateTiledLineWithLog(container.transform, "Line_Left", spLineLeft,
                    0, -cornerH, spLineLeft.rect.width, innerH, isHorizontal: false);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Left: sprite={spLineLeft != null}, innerH={innerH}");
            }

            // Правая вертикальная линия (без изменений)
            if (spLineRight != null && innerH > 0)
            {
                float rightX = w - spLineRight.rect.width;
                if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Creating Line_Right at X={rightX}, Y={-cornerH}, size={spLineRight.rect.width}x{innerH}");
                CreateTiledLineWithLog(container.transform, "Line_Right", spLineRight,
                    rightX, -cornerH, spLineRight.rect.width, innerH, isHorizontal: false);
            }
            else
            {
                Debug.LogWarning($"[ListDesk] SKIPPED Line_Right: sprite={spLineRight != null}, innerH={innerH}");
            }

            // ═══════════════════════════════════════════════════════════
            // 6. УГЛЫ (третий слой - ПОВЕРХ ВСЕГО!)
            // ═══════════════════════════════════════════════════════════

            // Левый верхний (LT) - позиция (0, 0)
            if (spCornerLT != null)
            {
                CreateCornerSprite(container.transform, "Corner_LT", spCornerLT, 0, 0);
            }

            // Правый верхний (RT) - позиция (w - spriteWidth, 0)
            if (spCornerRT != null)
            {
                float rtX = w - spCornerRT.rect.width;
                CreateCornerSprite(container.transform, "Corner_RT", spCornerRT, rtX, 0);
            }

            // Левый нижний (LB) - позиция (0, -(h - spriteHeight))
            if (spCornerLB != null)
            {
                float lbY = -(h - spCornerLB.rect.height);
                if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Corner_LB: h={h}, spriteH={spCornerLB.rect.height}, Y={lbY}");
                CreateCornerSprite(container.transform, "Corner_LB", spCornerLB, 0, lbY);
            }

            // Правый нижний (RB) - позиция (w - spriteWidth, -(h - spriteHeight))
            if (spCornerRB != null)
            {
                float rbX = w - spCornerRB.rect.width;
                float rbY = -(h - spCornerRB.rect.height);
                if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Corner_RB: X={rbX}, Y={rbY}");
                CreateCornerSprite(container.transform, "Corner_RB", spCornerRB, rbX, rbY);
            }

            Menu14ActionStateRuntime.RegisterControl(ld, container);
            if (VerboseLayoutLogs) Debug.Log($"[ListDesk] Created at ({ld.X},{ld.Y}) size {w}x{h}, fill area {fillW}x{fillH}");
        }


        private static void CreateListDeskVisualSourceExactV395K(
            UiListDesk ld,
            RectTransform parent,
            ListDeskSourceRuntime14.TemplateSpec spec)
        {
            float w = Mathf.Max(1f, ld.Width);
            float h = Mathf.Max(1f, ld.Height);

            var container = new GameObject($"C2Xml_ListDesk_{ld.SourceId}", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var rt = (RectTransform)container.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(ld.X, -ld.Y);
            rt.sizeDelta = new Vector2(w, h);

            // StdBorder::Draw: if(NFillers) DrawFilledRect3(...); else DrawRect4(...)
            if (spec.BorderNFillers > 0 && spec.BorderStartFiller >= 0)
                DrawFilledRect3V395K(container.transform, w, h, spec);
            else
                DrawRect4V395K(container.transform, w, h, spec);

            // DialogsDesk::Process creates VScroller from StdBorder even when the
            // content does not need scrolling. HideVScroller=false keeps it visible.
            if (spec.EnableVerticalScroller && !string.IsNullOrEmpty(spec.VScrollerGPFile))
                CreateListDeskVScrollerV395K(container.transform, w, h, spec);

            Menu14ActionStateRuntime.RegisterControl(ld, container);

            // Log the actual logical geometry used by the renderer. These values are
            // intentionally independent from sprite transparent bounds.
            Debug.Log(
                $"[C2:LISTDESK RENDER V395K] sourceId={ld.SourceId} logical=({ld.X:0.#},{ld.Y:0.#},{w:0.#},{h:0.#}) " +
                $"border='{spec.BorderName}' gp='{spec.BorderGPFile}' edges={spec.BorderLeftTop},{spec.BorderRightTop},{spec.BorderLeftBottom},{spec.BorderRightBottom}/" +
                $"{spec.BorderTopLine},{spec.BorderBottomLine},{spec.BorderLeftLine},{spec.BorderRightLine} fill={spec.BorderStartFiller}+{spec.BorderNFillers} " +
                $"vscroll={(spec.EnableVerticalScroller ? 1 : 0)} hide={(spec.HideVScroller ? 1 : 0)} vgp='{spec.VScrollerGPFile}'");
        }

        private static Sprite LoadGpFrameV395K(string gpFile, int frame)
        {
            if (string.IsNullOrEmpty(gpFile) || frame < 0) return null;
            string folder = gpFile.Replace("\\", "_").Replace("/", "_").ToLowerInvariant() + "_frames";
            Sprite sp = LoadSpriteFromResources(folder, $"frame_{frame:0000}");
            if (sp == null)
                sp = Menu14ActionStateRuntime.TryLoadGpSpriteForRenderer(gpFile, frame, true);
            return sp;
        }

        // V396A7R5: expose the already-audited source GP/border path to the
        // profile DialogsDesk runtime.  This avoids a second border implementation.
        internal static Sprite LoadSourceGpFrameV396A7R5(string gpFile, int frame)
            => LoadGpFrameV395K(gpFile, frame);

        internal static bool DrawSourceDialogsDeskBorderV396A7R5(
            Transform parent, float width, float height, string borderName,
            out ListDeskSourceRuntime14.TemplateSpec spec)
        {
            spec = null;
            if (parent == null || string.IsNullOrWhiteSpace(borderName)) return false;
            if (!ListDeskSourceRuntime14.TryGetBorderTemplate(borderName, out spec) || spec == null)
                return false;
            if (spec.BorderNFillers > 0 && spec.BorderStartFiller >= 0)
                DrawFilledRect3V395K(parent, width, height, spec);
            else
                DrawRect4V395K(parent, width, height, spec);
            return true;
        }

        private static void DrawFilledRect3V395K(
            Transform parent,
            float width,
            float height,
            ListDeskSourceRuntime14.TemplateSpec spec)
        {
            // DrawForms.cpp::DrawFilledRect3:
            // IntersectWindows(x0,y0,x1,y1); tiles start at x0/y0 and are clipped
            // to the FULL logical rectangle. The old Unity code incorrectly shrank
            // this by one full corner width/height.
            Sprite first = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderStartFiller);
            if (first != null)
            {
                float tileW = Mathf.Max(1f, first.rect.width);
                float tileH = Mathf.Max(1f, first.rect.height);
                var clip = CreateClipRectV395K(parent, "Fill_FullRect", 0f, 0f, width, height);

                int nx = Mathf.FloorToInt((width - 1f) / tileW);
                int ny = Mathf.FloorToInt((height - 1f) / tileH);
                for (int ix = 0; ix <= nx; ix++)
                {
                    for (int iy = 0; iy <= ny; iy++)
                    {
                        int pattern = (ix * ix + iy * iy * iy) % Mathf.Max(1, spec.BorderNFillers);
                        int frame = spec.BorderStartFiller + pattern;
                        Sprite sp = LoadGpFrameV395K(spec.BorderGPFile, frame);
                        if (sp == null) continue;
                        CreateSpriteAtV395K(clip, $"Fill_{ix}_{iy}_F{frame}", sp,
                            ix * tileW, iy * tileH, 0f, 0f);
                    }
                }
            }

            DrawRect4V395K(parent, width, height, spec);
        }

        private static void DrawRect4V395K(
            Transform parent,
            float width,
            float height,
            ListDeskSourceRuntime14.TemplateSpec spec)
        {
            Sprite clu = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderLeftTop);
            Sprite cru = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderRightTop);
            Sprite cld = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderLeftBottom);
            Sprite crd = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderRightBottom);
            Sprite lu = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderTopLine);
            Sprite ld = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderBottomLine);
            Sprite ll = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderLeftLine);
            Sprite lr = LoadGpFrameV395K(spec.BorderGPFile, spec.BorderRightLine);

            float ullx = clu != null ? clu.rect.width : 32f;
            if (ullx <= 0f) ullx = 32f;
            float lx2 = Mathf.Floor(ullx / 2f);

            float dllx = cld != null ? cld.rect.width : (clu != null ? clu.rect.width : 32f);
            if (dllx <= 0f) dllx = 32f;
            float lx3 = Mathf.Floor(dllx / 2f);

            float uplx = lu != null ? lu.rect.width : 32f;
            if (uplx <= 0f) uplx = 32f;
            float dnlx = ld != null ? ld.rect.width : 32f;
            if (dnlx <= 0f) dnlx = 32f;

            float lsly = clu != null ? clu.rect.height : 0f;
            float lly2 = Mathf.Floor(lsly / 2f);

            // Original DrawRect4 contains this exact quirk: LDLY is GP WIDTH of CLD.
            float ldly = cld != null ? cld.rect.width : 0f;
            if (ldly > 1000f) ldly = 0f;
            float ldy2 = Mathf.Floor(ldly / 2f);

            float leftly = ll != null ? ll.rect.height : 32f;
            if (leftly <= 0f) leftly = 32f;
            float rightly = lr != null ? lr.rect.height : 32f;
            if (rightly <= 0f) rightly = 32f;

            float x1 = width - 1f;
            float y1 = height - 1f;
            float midY = Mathf.Floor((y1 + 0f) / 2f);

            // Horizontal top line.
            if (lu != null)
            {
                float cx0 = lx2;
                float cy0 = -lly2;
                float cx1 = x1 - lx2 - 1f;
                float cy1 = midY;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Border_Top_Clip", cx0, cy0, cx1, cy1);
                int n = Mathf.FloorToInt((width - ullx) / uplx);
                for (int i = 0; i <= n + 2; i++)
                    CreateSpriteAtV395K(clip, $"Top_{i}", lu, i * uplx + lx2, -lly2, cx0, cy0);
            }

            // Horizontal bottom line.
            if (ld != null)
            {
                float cx0 = lx3;
                float cy0 = midY + 1f;
                float cx1 = x1 - lx3 - 1f;
                float cy1 = y1 + lly2;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Border_Bottom_Clip", cx0, cy0, cx1, cy1);
                int n = Mathf.FloorToInt((width - dllx) / dnlx);
                for (int i = 0; i <= n + 2; i++)
                    CreateSpriteAtV395K(clip, $"Bottom_{i}", ld, i * dnlx + lx3, y1 - ldy2, cx0, cy0);
            }

            // Vertical lines share one clipping window in original DrawRect4.
            {
                float cx0 = -lx3;
                float cy0 = lly2;
                float cx1 = x1 + lx3;
                float cy1 = y1 - ldy2;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Border_Vertical_Clip", cx0, cy0, cx1, cy1);
                if (ll != null)
                {
                    int n = Mathf.FloorToInt((height - lly2 - ldy2) / leftly);
                    for (int i = 0; i <= n + 1; i++)
                        CreateSpriteAtV395K(clip, $"Left_{i}", ll, -lx3, i * leftly + lly2, cx0, cy0);
                }
                if (lr != null)
                {
                    int n = Mathf.FloorToInt((height - lly2 - ldy2) / rightly);
                    for (int i = 0; i <= n + 1; i++)
                        CreateSpriteAtV395K(clip, $"Right_{i}", lr, x1 - lx3, i * rightly + lly2, cx0, cy0);
                }
            }

            float cornerMid = Mathf.Floor((y1 - ldy2 + lly2) / 2f);
            float xMid = Mathf.Floor(x1 / 2f);

            if (clu != null)
            {
                float cx0 = -lx2, cy0 = -lly2, cx1 = xMid - 1f, cy1 = cornerMid - 1f;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Corner_LT_Clip", cx0, cy0, cx1, cy1);
                CreateSpriteAtV395K(clip, "Corner_LT", clu, -lx2, -lly2, cx0, cy0);
            }
            if (cru != null)
            {
                // V396A7R3: original DrawRect4 starts the right half at
                // (x0+x1)/2, not +1.  The previous +1 skipped exactly one
                // framebuffer column between the left/right corner clip regions.
                float cx0 = xMid, cy0 = -lly2, cx1 = x1 + lx2, cy1 = cornerMid - 1f;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Corner_RT_Clip", cx0, cy0, cx1, cy1);
                CreateSpriteAtV395K(clip, "Corner_RT", cru, x1 - lx2, -lly2, cx0, cy0);
            }
            if (cld != null)
            {
                float cx0 = -lx3, cy0 = cornerMid, cx1 = xMid - 1f, cy1 = y1 + lly2;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Corner_LB_Clip", cx0, cy0, cx1, cy1);
                CreateSpriteAtV395K(clip, "Corner_LB", cld, -lx3, y1 - ldy2, cx0, cy0);
            }
            if (crd != null)
            {
                // Same inclusive split as DrawForms.cpp::DrawRect4.
                float cx0 = xMid, cy0 = cornerMid, cx1 = x1 + lx3, cy1 = y1 + lly2;
                Transform clip = CreateClipRectInclusiveV395K(parent, "Corner_RB_Clip", cx0, cy0, cx1, cy1);
                CreateSpriteAtV395K(clip, "Corner_RB", crd, x1 - lx3, y1 - ldy2, cx0, cy0);
            }
        }

        private static Transform CreateClipRectInclusiveV395K(
            Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            return CreateClipRectV395K(parent, name, x0, y0,
                Mathf.Max(1f, x1 - x0 + 1f), Mathf.Max(1f, y1 - y0 + 1f));
        }

        private static Transform CreateClipRectV395K(
            Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, w), Mathf.Max(1f, h));
            return go.transform;
        }

        private static void CreateSpriteAtV395K(
            Transform clip,
            string name,
            Sprite sprite,
            float sourceX,
            float sourceY,
            float clipX,
            float clipY)
        {
            if (clip == null || sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(clip, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(sourceX - clipX, -(sourceY - clipY));
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }

        private static void CreateListDeskVScrollerV395K(
            Transform parent,
            float listWidth,
            float listHeight,
            ListDeskSourceRuntime14.TemplateSpec spec)
        {
            // Dialogs.cpp::DialogsDesk::Process:
            // x = x1 - VScroller_DX_right; y = y + VScroller_DY_top;
            // Ly = height - VScroller_DY_top - VScrolled_DY_bottom.
            float x = (listWidth - 1f) - spec.VScrollerDXRight;
            float y = spec.VScrollerDYTop;
            float ly = listHeight - spec.VScrollerDYTop - spec.VScrolledDYBottom;
            if (ly <= 0f) return;

            Sprite up = LoadGpFrameV395K(spec.VScrollerGPFile, 0);
            Sprite down = LoadGpFrameV395K(spec.VScrollerGPFile, 2);
            Sprite c0 = LoadGpFrameV395K(spec.VScrollerGPFile, 5);
            Sprite c1 = LoadGpFrameV395K(spec.VScrollerGPFile, 6);
            Sprite c2 = LoadGpFrameV395K(spec.VScrollerGPFile, 7);
            if (up == null && down == null && c0 == null) return;

            float width = up != null ? up.rect.width : (c0 != null ? c0.rect.width : 16f);
            var root = new GameObject("VScroller_SourceExact_V395K", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, ly);

            float upH = up != null ? up.rect.height : 0f;
            float dnH = down != null ? down.rect.height : 0f;
            float centerY0 = Mathf.Floor(upH / 2f);
            float centerY1 = ly - 1f - centerY0;
            Transform centerClip = CreateClipRectInclusiveV395K(root.transform, "Track_Clip", -64f, centerY0, width + 64f, centerY1);
            Sprite[] centers = { c0, c1, c2 };
            float centerH = c0 != null ? Mathf.Max(1f, c0.rect.height) : 1f;
            int n = Mathf.FloorToInt(ly / centerH);
            for (int i = 0; i <= n; i++)
            {
                Sprite sp = centers[i % 3] ?? c0;
                if (sp == null) continue;
                CreateSpriteAtV395K(centerClip, $"Track_{i}", sp, 0f, i * centerH + centerY0, -64f, centerY0);
            }

            if (up != null)
            {
                var upClip = CreateClipRectV395K(root.transform, "Up_Clip", 0f, 0f, Mathf.Max(width, up.rect.width), up.rect.height);
                CreateSpriteAtV395K(upClip, "Up", up, 0f, 0f, 0f, 0f);
            }
            if (down != null)
            {
                float dy = ly - down.rect.height;
                var dnClip = CreateClipRectV395K(root.transform, "Down_Clip", 0f, dy, Mathf.Max(width, down.rect.width), down.rect.height);
                CreateSpriteAtV395K(dnClip, "Down", down, 0f, dy, 0f, dy);
            }

            // When SMaxPos==0 original NewGP_VScrollBar_OnDraw deliberately hides
            // the thumb but keeps the scroller visible if HideVScroller==false.
            // The profile ListDesk starts in exactly that state for one/few rows.
            root.SetActive(!spec.HideVScroller);
        }

        /// <summary>
        /// Создаёт угловой спрайт с правильным размером и позиционированием
        /// </summary>
        private static void CreateCornerSprite(Transform parent, string name, Sprite sp, float x, float y)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);

            // ИСПОЛЬЗУЕМ РЕАЛЬНЫЙ РАЗМЕР СПРАЙТА!
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.preserveAspect = false;

            // УГЛЫ ПОВЕРХ ВСЕГО!
            go.transform.SetAsLastSibling();

            if (VerboseLayoutLogs) Debug.Log($"[ListDesk] {name} placed at ({x},{y}), size={sp.rect.width}x{sp.rect.height}");
        }

        /// <summary>
        /// Тайлируемая линия с логированием и перекрытием
        /// </summary>
        private static void CreateTiledLineWithLog(Transform parent, string name, Sprite sp,
            float x, float y, float areaWidth, float areaHeight, bool isHorizontal)
        {
            if (sp == null)
            {
                Debug.LogWarning($"[TiledLine] {name}: sprite is NULL!");
                return;
            }

            if (VerboseLayoutLogs) Debug.Log($"[TiledLine] {name}: creating at ({x},{y}), area={areaWidth}x{areaHeight}");

            var container = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            container.transform.SetParent(parent, false);

            var containerRt = (RectTransform)container.transform;
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(x, y);
            containerRt.sizeDelta = new Vector2(areaWidth, areaHeight);

            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Перекрытие для устранения субпиксельных щелей
            const float OVERLAP = 0.5f;

            int tilesCreated = 0;

            if (isHorizontal)
            {
                float stepX = Mathf.Max(1f, tileW - OVERLAP);
                int tilesNeeded = Mathf.CeilToInt(areaWidth / stepX) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(i * stepX, 0);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;

                    tilesCreated++;
                }
            }
            else
            {
                float stepY = Mathf.Max(1f, tileH - OVERLAP);
                int tilesNeeded = Mathf.CeilToInt(areaHeight / stepY) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(0, -i * stepY);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;

                    tilesCreated++;
                }
            }

            if (VerboseLayoutLogs) Debug.Log($"[TiledLine] {name}: created {tilesCreated} tiles");
        }

        /// <summary>
        /// Создаёт тайлируемую линию С ПЕРЕКРЫТИЕМ для устранения щелей
        /// </summary>
        private static void CreateTiledLineManual(Transform parent, string name, Sprite sp,
            float x, float y, float areaWidth, float areaHeight, bool isHorizontal)
        {
            if (sp == null)
            {
                Debug.LogWarning($"[TiledLine] {name}: sprite is NULL!");
                return;
            }

            var container = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            container.transform.SetParent(parent, false);

            var containerRt = (RectTransform)container.transform;
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(x, y);
            containerRt.sizeDelta = new Vector2(areaWidth, areaHeight);

            // Размер тайла
            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // ═══════════════════════════════════════════════════════════
            // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Добавляем перекрытие 1-2 пикселя
            // чтобы компенсировать субпиксельные щели при масштабировании
            // ═══════════════════════════════════════════════════════════
            const float OVERLAP = 1f;

            if (isHorizontal)
            {
                // Шаг меньше чем размер тайла = перекрытие
                float stepX = tileW - OVERLAP;
                int tilesNeeded = Mathf.CeilToInt(areaWidth / stepX) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(i * stepX, 0);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;
                }
            }
            else
            {
                // Вертикальная линия
                float stepY = tileH - OVERLAP;
                int tilesNeeded = Mathf.CeilToInt(areaHeight / stepY) + 1;

                for (int i = 0; i < tilesNeeded; i++)
                {
                    var tile = new GameObject($"T{i}", typeof(RectTransform), typeof(Image));
                    tile.transform.SetParent(container.transform, false);

                    var tileRt = (RectTransform)tile.transform;
                    tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                    tileRt.pivot = new Vector2(0, 1);
                    tileRt.anchoredPosition = new Vector2(0, -i * stepY);
                    tileRt.sizeDelta = new Vector2(tileW, tileH);

                    var img = tile.GetComponent<Image>();
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.raycastTarget = false;
                    img.preserveAspect = false;
                }
            }
        }
        /// <summary>
        /// Использует ВСТРОЕННЫЙ тайлинг Unity — надёжнее при масштабировании Canvas
        /// </summary>
        private static void CreateTiledLineNative(Transform parent, string name, Sprite sp,
            float x, float y, float areaWidth, float areaHeight)
        {
            if (sp == null) return;

            // Контейнер с маской
            var container = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            container.transform.SetParent(parent, false);

            var containerRt = (RectTransform)container.transform;
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(x, y);
            containerRt.sizeDelta = new Vector2(areaWidth, areaHeight);

            // Один Image с типом Tiled
            var imageGO = new GameObject("TiledImage", typeof(RectTransform), typeof(Image));
            imageGO.transform.SetParent(container.transform, false);

            var imageRt = (RectTransform)imageGO.transform;
            imageRt.anchorMin = Vector2.zero;
            imageRt.anchorMax = Vector2.one;
            imageRt.offsetMin = Vector2.zero;
            imageRt.offsetMax = Vector2.zero;

            var img = imageGO.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Tiled;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = 1f;  // ← ВАЖНО!
            img.raycastTarget = false;
        }
        /// <summary>
        /// Создаёт тайлируемую линию используя встроенный Image.Type.Tiled
        /// </summary>
        private static void CreateTiledEdgeNative(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            img.type = Image.Type.Tiled;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = 1f;
        }
         
         

        /// <summary>
        /// Создаёт ГОРИЗОНТАЛЬНУЮ тайлируемую линию
        /// ВАЖНО: используем ТОЧНЫЙ размер спрайта, без растяжения
        /// </summary>
        private static void CreateTiledEdgeHorizontal(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            // ═══════════════════════════════════════════════════════════
            // ТОЧНЫЙ размер спрайта (не параметр height!)
            // ═══════════════════════════════════════════════════════════
            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Сколько тайлов нужно чтобы заполнить ширину + запас
            int tilesNeeded = Mathf.CeilToInt(width / tileW) + 2;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(go.transform, false);

                var trt = (RectTransform)tile.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                // Позиция: каждый тайл вплотную к предыдущему
                trt.anchoredPosition = new Vector2(i * tileW, 0);

                // ТОЧНЫЙ размер спрайта — без растяжения!
                trt.sizeDelta = new Vector2(tileW, tileH);

                var img = tile.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;  // Размер уже точный
                img.raycastTarget = false;
            }
        }

        /// <summary>
        /// Создаёт ВЕРТИКАЛЬНУЮ тайлируемую линию
        /// ВАЖНО: используем ТОЧНЫЙ размер спрайта, без растяжения
        /// </summary>
        private static void CreateTiledEdgeVertical(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            if (sp == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            // ═══════════════════════════════════════════════════════════
            // ТОЧНЫЙ размер спрайта
            // ═══════════════════════════════════════════════════════════
            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Сколько тайлов нужно чтобы заполнить высоту + запас
            int tilesNeeded = Mathf.CeilToInt(height / tileH) + 2;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(go.transform, false);

                var trt = (RectTransform)tile.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                // Позиция: каждый тайл вплотную к предыдущему (вниз)
                trt.anchoredPosition = new Vector2(0, -i * tileH);

                // ТОЧНЫЙ размер спрайта
                trt.sizeDelta = new Vector2(tileW, tileH);

                var img = tile.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
            }
        }


        /// <summary>
        /// Диагностика размеров спрайтов границы
        /// </summary>
        private static void DebugBorderSprites(string folder)
        {
            if (VerboseLayoutLogs) Debug.Log("═══════════════════════════════════════════════════════════");
            if (VerboseLayoutLogs) Debug.Log("[BORDER DEBUG] Checking sprite sizes:");

            for (int i = 0; i <= 11; i++)
            {
                var sp = LoadSpriteFromResources(folder, $"frame_{i:D4}");
                if (sp != null)
                {
                    var tex = sp.texture;
                    if (VerboseLayoutLogs) Debug.Log($"  frame_{i:D4}: " +
                        $"sprite.rect = {sp.rect.width}x{sp.rect.height}, " +
                        $"texture = {tex.width}x{tex.height}, " +
                        $"PPU = {sp.pixelsPerUnit}");
                }
                else
                {
                    if (VerboseLayoutLogs) Debug.Log($"  frame_{i:D4}: NOT FOUND");
                }
            }

            if (VerboseLayoutLogs) Debug.Log("═══════════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Размещает один спрайт (угол)
        /// </summary>
        private static void PlaceSpriteInternal(Transform parent, string name, Sprite sp, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            // Углы должны быть поверх всего
            go.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Создаёт тайлируемую линию (горизонтальную или вертикальную)
        /// </summary>
        private static void CreateTiledEdgeInternal(Transform parent, string name, Sprite sp,
            float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            float tileW = sp.rect.width;
            float tileH = sp.rect.height;

            // Определяем направление тайлинга
            bool horizontal = width > height;

            int tilesNeeded;
            if (horizontal)
                tilesNeeded = Mathf.CeilToInt(width / tileW) + 1;
            else
                tilesNeeded = Mathf.CeilToInt(height / tileH) + 1;

            for (int i = 0; i < tilesNeeded; i++)
            {
                var tile = new GameObject($"Tile_{i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(go.transform, false);

                var trt = (RectTransform)tile.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);

                if (horizontal)
                    trt.anchoredPosition = new Vector2(i * tileW, 0);
                else
                    trt.anchoredPosition = new Vector2(0, -i * tileH);

                trt.sizeDelta = new Vector2(tileW, tileH);

                var img = tile.GetComponent<Image>();
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
            }
        }

        private static void PlaceSprite(Transform parent, string name, Sprite sp, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            go.transform.SetAsLastSibling();
        }

        private static void CreateTiledEdge(Transform parent, string name, Sprite sp, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
        }

        private static void CreateSprite(Transform parent, string name, Sprite sp, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            go.transform.SetAsLastSibling();
        }

        private static void CreateTiledLine(Transform parent, string name, Sprite sp, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт бордер BD из спрайтов (12 фреймов)
        /// Структура BD: 
        /// 0-3: углы (TL, TR, BL, BR)
        /// 4-7: стороны для тайлинга (Top, Right, Bottom, Left)
        /// 8-11: дополнительные элементы
        /// </summary>
        private static void CreateBorderBD(Transform parent, float width, float height)
        {
            const string folder = "interf3_elements_border_BD_frames";

            // Загружаем спрайты углов
            Sprite spTL = LoadSpriteFromResources(folder, "frame_0000"); // Top-Left
            Sprite spTR = LoadSpriteFromResources(folder, "frame_0001"); // Top-Right
            Sprite spBL = LoadSpriteFromResources(folder, "frame_0002"); // Bottom-Left
            Sprite spBR = LoadSpriteFromResources(folder, "frame_0003"); // Bottom-Right

            // Загружаем спрайты сторон для тайлинга
            Sprite spTop = LoadSpriteFromResources(folder, "frame_0004");    // Top edge
            Sprite spRight = LoadSpriteFromResources(folder, "frame_0005");  // Right edge
            Sprite spBottom = LoadSpriteFromResources(folder, "frame_0006"); // Bottom edge
            Sprite spLeft = LoadSpriteFromResources(folder, "frame_0007");   // Left edge

            // Проверяем загрузку
            bool hasSprites = spTL != null || spTop != null;

            if (!hasSprites)
            {
                Debug.LogWarning($"[BorderBD] No sprites found in {folder}, using fallback");
                CreateBorderFallback(parent, width, height);
                return;
            }

            // Размеры углов
            float cornerW = spTL != null ? spTL.rect.width : 8f;
            float cornerH = spTL != null ? spTL.rect.height : 8f;

            // ═══════════════════════════════════════════════════════════
            // Верхняя сторона (тайлится между углами)
            // ═══════════════════════════════════════════════════════════
            if (spTop != null)
            {
                var topGO = new GameObject("Border_Top", typeof(RectTransform), typeof(Image));
                topGO.transform.SetParent(parent, false);

                var topRt = (RectTransform)topGO.transform;
                topRt.anchorMin = topRt.anchorMax = new Vector2(0, 1);
                topRt.pivot = new Vector2(0, 1);
                topRt.anchoredPosition = new Vector2(cornerW, 0);
                topRt.sizeDelta = new Vector2(width - cornerW * 2, spTop.rect.height);

                var topImg = topGO.GetComponent<Image>();
                topImg.sprite = spTop;
                topImg.type = Image.Type.Tiled;
                topImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Нижняя сторона
            // ═══════════════════════════════════════════════════════════
            if (spBottom != null)
            {
                var bottomGO = new GameObject("Border_Bottom", typeof(RectTransform), typeof(Image));
                bottomGO.transform.SetParent(parent, false);

                var bottomRt = (RectTransform)bottomGO.transform;
                bottomRt.anchorMin = bottomRt.anchorMax = new Vector2(0, 1);
                bottomRt.pivot = new Vector2(0, 1);
                bottomRt.anchoredPosition = new Vector2(cornerW, -height + spBottom.rect.height);
                bottomRt.sizeDelta = new Vector2(width - cornerW * 2, spBottom.rect.height);

                var bottomImg = bottomGO.GetComponent<Image>();
                bottomImg.sprite = spBottom;
                bottomImg.type = Image.Type.Tiled;
                bottomImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Левая сторона
            // ═══════════════════════════════════════════════════════════
            if (spLeft != null)
            {
                var leftGO = new GameObject("Border_Left", typeof(RectTransform), typeof(Image));
                leftGO.transform.SetParent(parent, false);

                var leftRt = (RectTransform)leftGO.transform;
                leftRt.anchorMin = leftRt.anchorMax = new Vector2(0, 1);
                leftRt.pivot = new Vector2(0, 1);
                leftRt.anchoredPosition = new Vector2(0, -cornerH);
                leftRt.sizeDelta = new Vector2(spLeft.rect.width, height - cornerH * 2);

                var leftImg = leftGO.GetComponent<Image>();
                leftImg.sprite = spLeft;
                leftImg.type = Image.Type.Tiled;
                leftImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Правая сторона
            // ═══════════════════════════════════════════════════════════
            if (spRight != null)
            {
                var rightGO = new GameObject("Border_Right", typeof(RectTransform), typeof(Image));
                rightGO.transform.SetParent(parent, false);

                var rightRt = (RectTransform)rightGO.transform;
                rightRt.anchorMin = rightRt.anchorMax = new Vector2(0, 1);
                rightRt.pivot = new Vector2(0, 1);
                rightRt.anchoredPosition = new Vector2(width - spRight.rect.width, -cornerH);
                rightRt.sizeDelta = new Vector2(spRight.rect.width, height - cornerH * 2);

                var rightImg = rightGO.GetComponent<Image>();
                rightImg.sprite = spRight;
                rightImg.type = Image.Type.Tiled;
                rightImg.raycastTarget = false;
            }

            // ═══════════════════════════════════════════════════════════
            // Углы (поверх сторон)
            // ═══════════════════════════════════════════════════════════

            // Top-Left
            if (spTL != null)
            {
                CreateCorner(parent, "Corner_TL", spTL, 0, 0);
            }

            // Top-Right
            if (spTR != null)
            {
                CreateCorner(parent, "Corner_TR", spTR, width - spTR.rect.width, 0);
            }

            // Bottom-Left
            if (spBL != null)
            {
                CreateCorner(parent, "Corner_BL", spBL, 0, -height + spBL.rect.height);
            }

            // Bottom-Right
            if (spBR != null)
            {
                CreateCorner(parent, "Corner_BR", spBR, width - spBR.rect.width, -height + spBR.rect.height);
            }
        }

        private static void CreateCorner(Transform parent, string name, Sprite sprite, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.preserveAspect = false;

            // Углы должны быть поверх сторон
            go.transform.SetAsLastSibling();
        }

        private static void CreateBorderFallback(Transform parent, float width, float height)
        {
            Color borderColor = new Color(0.45f, 0.38f, 0.28f, 0.95f);
            float thickness = 3f;

            // Top
            CreateSimpleLine(parent, "Border_Top", 0, 0, width, thickness, borderColor);
            // Bottom
            CreateSimpleLine(parent, "Border_Bottom", 0, height - thickness, width, thickness, borderColor);
            // Left
            CreateSimpleLine(parent, "Border_Left", 0, 0, thickness, height, borderColor);
            // Right
            CreateSimpleLine(parent, "Border_Right", width - thickness, 0, thickness, height, borderColor);
        }

        private static void CreateSimpleLine(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт вертикальный скроллер из спрайтов Scroll3
        /// Структура: 0=track, 1=thumb, 2=arrowUp, 3=arrowDown, 4-7=hover states
        /// </summary>
        private static Scrollbar CreateScroll3(Transform parent, float containerWidth, float containerHeight, float scrollerWidth, float padding)
        {
            const string folder = "Interf3_elements_scroll3_frames";

            // Загружаем спрайты
            Sprite spTrack = LoadSpriteFromResources(folder, "frame_0000");
            Sprite spThumb = LoadSpriteFromResources(folder, "frame_0001");
            Sprite spArrowUp = LoadSpriteFromResources(folder, "frame_0002");
            Sprite spArrowDown = LoadSpriteFromResources(folder, "frame_0003");
            Sprite spArrowUpHover = LoadSpriteFromResources(folder, "frame_0004");
            Sprite spArrowDownHover = LoadSpriteFromResources(folder, "frame_0005");
            Sprite spThumbHover = LoadSpriteFromResources(folder, "frame_0006");

            bool hasSprites = spTrack != null || spThumb != null;

            // ═══════════════════════════════════════════════════════════
            // Контейнер скроллера
            // ═══════════════════════════════════════════════════════════
            var scrollbarGO = new GameObject("VScrollbar", typeof(RectTransform), typeof(Image));
            scrollbarGO.transform.SetParent(parent, false);

            var scrollbarRt = (RectTransform)scrollbarGO.transform;
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 0.5f);
            scrollbarRt.anchoredPosition = new Vector2(-padding, 0);
            scrollbarRt.sizeDelta = new Vector2(scrollerWidth, -(padding * 2));

            var scrollbarImg = scrollbarGO.GetComponent<Image>();
            if (spTrack != null)
            {
                scrollbarImg.sprite = spTrack;
                scrollbarImg.type = Image.Type.Sliced;
            }
            else
            {
                scrollbarImg.color = new Color(0.12f, 0.10f, 0.08f, 0.95f);
            }
            scrollbarImg.raycastTarget = true;

            // ═══════════════════════════════════════════════════════════
            // Размер стрелок
            // ═══════════════════════════════════════════════════════════
            float arrowHeight = spArrowUp != null ? spArrowUp.rect.height : scrollerWidth;
            float arrowWidth = spArrowUp != null ? spArrowUp.rect.width : scrollerWidth;

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вверх"
            // ═══════════════════════════════════════════════════════════
            var arrowUpGO = new GameObject("ArrowUp", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowUpGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowUpRt = (RectTransform)arrowUpGO.transform;
            arrowUpRt.anchorMin = new Vector2(0.5f, 1);
            arrowUpRt.anchorMax = new Vector2(0.5f, 1);
            arrowUpRt.pivot = new Vector2(0.5f, 1);
            arrowUpRt.anchoredPosition = new Vector2(0, 0);
            arrowUpRt.sizeDelta = new Vector2(arrowWidth, arrowHeight);

            var arrowUpImg = arrowUpGO.GetComponent<Image>();
            if (spArrowUp != null)
            {
                arrowUpImg.sprite = spArrowUp;
                arrowUpImg.type = Image.Type.Simple;
                arrowUpImg.preserveAspect = false;

                // Добавляем hover эффект
                if (spArrowUpHover != null)
                {
                    var hoverUp = arrowUpGO.AddComponent<ScrollArrowHover>();
                    hoverUp.Normal = spArrowUp;
                    hoverUp.Hover = spArrowUpHover;
                    hoverUp.Img = arrowUpImg;
                }
            }
            else
            {
                arrowUpImg.color = new Color(0.35f, 0.30f, 0.25f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вниз"
            // ═══════════════════════════════════════════════════════════
            var arrowDownGO = new GameObject("ArrowDown", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowDownGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowDownRt = (RectTransform)arrowDownGO.transform;
            arrowDownRt.anchorMin = new Vector2(0.5f, 0);
            arrowDownRt.anchorMax = new Vector2(0.5f, 0);
            arrowDownRt.pivot = new Vector2(0.5f, 0);
            arrowDownRt.anchoredPosition = new Vector2(0, 0);
            arrowDownRt.sizeDelta = new Vector2(arrowWidth, arrowHeight);

            var arrowDownImg = arrowDownGO.GetComponent<Image>();
            if (spArrowDown != null)
            {
                arrowDownImg.sprite = spArrowDown;
                arrowDownImg.type = Image.Type.Simple;
                arrowDownImg.preserveAspect = false;

                if (spArrowDownHover != null)
                {
                    var hoverDown = arrowDownGO.AddComponent<ScrollArrowHover>();
                    hoverDown.Normal = spArrowDown;
                    hoverDown.Hover = spArrowDownHover;
                    hoverDown.Img = arrowDownImg;
                }
            }
            else
            {
                arrowDownImg.color = new Color(0.35f, 0.30f, 0.25f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Sliding Area (между стрелками)
            // ═══════════════════════════════════════════════════════════
            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarGO.transform, false);

            var slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(1, arrowHeight + 2);
            slidingRt.offsetMax = new Vector2(-1, -arrowHeight - 2);

            // ═══════════════════════════════════════════════════════════
            // Handle (ползунок)
            // ═══════════════════════════════════════════════════════════
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = new Vector2(0, 0);
            handleRt.anchorMax = new Vector2(1, 1);
            handleRt.offsetMin = new Vector2(1, 0);
            handleRt.offsetMax = new Vector2(-1, 0);

            var handleImg = handle.GetComponent<Image>();
            if (spThumb != null)
            {
                handleImg.sprite = spThumb;
                handleImg.type = Image.Type.Sliced;

                if (spThumbHover != null)
                {
                    var hoverThumb = handle.AddComponent<ScrollArrowHover>();
                    hoverThumb.Normal = spThumb;
                    hoverThumb.Hover = spThumbHover;
                    hoverThumb.Img = handleImg;
                }
            }
            else
            {
                handleImg.color = new Color(0.55f, 0.48f, 0.38f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Scrollbar компонент
            // ═══════════════════════════════════════════════════════════
            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            scrollbar.value = 1f;

            // ═══════════════════════════════════════════════════════════
            // Подключаем кнопки стрелок к скроллу
            // ═══════════════════════════════════════════════════════════
            var arrowController = scrollbarGO.AddComponent<ScrollArrowController>();
            arrowController.Scrollbar = scrollbar;
            arrowController.ArrowUp = arrowUpGO.GetComponent<Button>();
            arrowController.ArrowDown = arrowDownGO.GetComponent<Button>();
            arrowController.ScrollStep = 0.1f;

            return scrollbar;
        }

        /// <summary>
        /// Hover эффект для элементов скроллера
        /// </summary>
        private sealed class ScrollArrowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Sprite Normal;
            public Sprite Hover;
            public Image Img;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Img != null && Hover != null) Img.sprite = Hover;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (Img != null && Normal != null) Img.sprite = Normal;
            }
        }

        /// <summary>
        /// Контроллер кнопок-стрелок скроллера
        /// </summary>
        private sealed class ScrollArrowController : MonoBehaviour
        {
            public Scrollbar Scrollbar;
            public Button ArrowUp;
            public Button ArrowDown;
            public float ScrollStep = 0.1f;

            private void Awake()
            {
                if (ArrowUp != null)
                {
                    ArrowUp.onClick.AddListener(() =>
                    {
                        if (Scrollbar != null)
                            Scrollbar.value = Mathf.Clamp01(Scrollbar.value + ScrollStep);
                    });
                }

                if (ArrowDown != null)
                {
                    ArrowDown.onClick.AddListener(() =>
                    {
                        if (Scrollbar != null)
                            Scrollbar.value = Mathf.Clamp01(Scrollbar.value - ScrollStep);
                    });
                }
            }
        }

        /// <summary>
        /// Создаёт бордер (рамку) вокруг ListDesk
        /// </summary>
        private static void CreateListDeskBorder(Transform parent, float width, float height)
        {
            // Пробуем загрузить спрайты бордера
            const string borderFolder = "interf3_elements_borders_frames";

            // Попробуем разные варианты
            Sprite spTop = LoadSpriteFromResources(borderFolder, "frame_0000");
            Sprite spBottom = LoadSpriteFromResources(borderFolder, "frame_0001");
            Sprite spLeft = LoadSpriteFromResources(borderFolder, "frame_0002");
            Sprite spRight = LoadSpriteFromResources(borderFolder, "frame_0003");

            float borderThickness = 2f;

            // Если спрайтов нет - рисуем простую рамку цветом
            if (spTop == null)
            {
                // Верхняя линия
                CreateBorderLine(parent, "BorderTop", 0, 0, width, borderThickness);
                // Нижняя линия
                CreateBorderLine(parent, "BorderBottom", 0, height - borderThickness, width, borderThickness);
                // Левая линия
                CreateBorderLine(parent, "BorderLeft", 0, 0, borderThickness, height);
                // Правая линия
                CreateBorderLine(parent, "BorderRight", width - borderThickness, 0, borderThickness, height);
            }
            else
            {
                // Если есть спрайты - используем их (TODO: реализовать когда будут спрайты)
            }
        }

        private static void CreateBorderLine(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.4f, 0.35f, 0.25f, 0.9f); // Коричневатый цвет рамки
            img.raycastTarget = false;
        }

        /// <summary>
        /// Создаёт вертикальный скроллер для ListDesk
        /// </summary>
        private static Scrollbar CreateListDeskScrollbar(Transform parent, float containerWidth, float containerHeight, float scrollerWidth, float padding)
        {
            const string scrollFolder = "interf3_elements_scroll3_frames";

            // Загружаем спрайты скроллера
            Sprite spTrack = LoadSpriteFromResources(scrollFolder, "frame_0000");     // Фон трека
            Sprite spThumb = LoadSpriteFromResources(scrollFolder, "frame_0001");     // Ползунок
            Sprite spArrowUp = LoadSpriteFromResources(scrollFolder, "frame_0002");   // Стрелка вверх
            Sprite spArrowDown = LoadSpriteFromResources(scrollFolder, "frame_0003"); // Стрелка вниз

            // ═══════════════════════════════════════════════════════════
            // Основной контейнер скроллера
            // ═══════════════════════════════════════════════════════════
            var scrollbarGO = new GameObject("VScrollbar", typeof(RectTransform), typeof(Image));
            scrollbarGO.transform.SetParent(parent, false);

            var scrollbarRt = (RectTransform)scrollbarGO.transform;
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 1);
            scrollbarRt.anchoredPosition = new Vector2(-padding, -padding);
            scrollbarRt.sizeDelta = new Vector2(scrollerWidth, -(padding * 2));

            var scrollbarImg = scrollbarGO.GetComponent<Image>();
            if (spTrack != null)
            {
                scrollbarImg.sprite = spTrack;
                scrollbarImg.type = Image.Type.Sliced;
            }
            else
            {
                scrollbarImg.color = new Color(0.15f, 0.12f, 0.1f, 0.9f); // Тёмный фон
            }
            scrollbarImg.raycastTarget = true;

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вверх"
            // ═══════════════════════════════════════════════════════════
            float arrowHeight = scrollerWidth; // Квадратные кнопки

            var arrowUpGO = new GameObject("ArrowUp", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowUpGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowUpRt = (RectTransform)arrowUpGO.transform;
            arrowUpRt.anchorMin = new Vector2(0, 1);
            arrowUpRt.anchorMax = new Vector2(1, 1);
            arrowUpRt.pivot = new Vector2(0.5f, 1);
            arrowUpRt.anchoredPosition = new Vector2(0, 0);
            arrowUpRt.sizeDelta = new Vector2(0, arrowHeight);

            var arrowUpImg = arrowUpGO.GetComponent<Image>();
            if (spArrowUp != null)
            {
                arrowUpImg.sprite = spArrowUp;
                arrowUpImg.type = Image.Type.Simple;
            }
            else
            {
                arrowUpImg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Кнопка "Вниз"
            // ═══════════════════════════════════════════════════════════
            var arrowDownGO = new GameObject("ArrowDown", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowDownGO.transform.SetParent(scrollbarGO.transform, false);

            var arrowDownRt = (RectTransform)arrowDownGO.transform;
            arrowDownRt.anchorMin = new Vector2(0, 0);
            arrowDownRt.anchorMax = new Vector2(1, 0);
            arrowDownRt.pivot = new Vector2(0.5f, 0);
            arrowDownRt.anchoredPosition = new Vector2(0, 0);
            arrowDownRt.sizeDelta = new Vector2(0, arrowHeight);

            var arrowDownImg = arrowDownGO.GetComponent<Image>();
            if (spArrowDown != null)
            {
                arrowDownImg.sprite = spArrowDown;
                arrowDownImg.type = Image.Type.Simple;
            }
            else
            {
                arrowDownImg.color = new Color(0.3f, 0.25f, 0.2f, 1f);
            }

            // ═══════════════════════════════════════════════════════════
            // Sliding Area (между стрелками)
            // ═══════════════════════════════════════════════════════════
            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarGO.transform, false);

            var slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(0, arrowHeight + 2);
            slidingRt.offsetMax = new Vector2(0, -arrowHeight - 2);

            // ═══════════════════════════════════════════════════════════
            // Handle (ползунок)
            // ═══════════════════════════════════════════════════════════
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = new Vector2(0, 0);
            handleRt.anchorMax = new Vector2(1, 1);
            handleRt.offsetMin = new Vector2(2, 0);
            handleRt.offsetMax = new Vector2(-2, 0);

            var handleImg = handle.GetComponent<Image>();
            if (spThumb != null)
            {
                handleImg.sprite = spThumb;
                handleImg.type = Image.Type.Sliced;
            }
            else
            {
                handleImg.color = new Color(0.5f, 0.45f, 0.35f, 1f); // Светлее фона
            }

            // ═══════════════════════════════════════════════════════════
            // Scrollbar компонент
            // ═══════════════════════════════════════════════════════════
            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            scrollbar.value = 1f; // Начинаем сверху

            return scrollbar;
        }

        /// <summary>
        /// Создаёт вертикальный скроллер
        /// </summary>
        private static Scrollbar CreateVerticalScrollbar(Transform parent, float containerWidth, float containerHeight, float scrollerWidth)
        {
            const string folder = "interf3_elements_scroll_frames";

            var scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGO.transform.SetParent(parent, false);

            var scrollbarRt = (RectTransform)scrollbarGO.transform;
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 1);
            scrollbarRt.anchoredPosition = Vector2.zero;
            scrollbarRt.sizeDelta = new Vector2(scrollerWidth, 0);

            var scrollbarImg = scrollbarGO.GetComponent<Image>();
            scrollbarImg.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);

            // Sliding Area
            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarGO.transform, false);

            var slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(0, 2);
            slidingRt.offsetMax = new Vector2(0, -2);

            // Handle
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            var handleImg = handle.GetComponent<Image>();

            // Пробуем загрузить спрайт скроллера
            var scrollSprite = LoadSpriteFromResources(folder, "frame_0004");
            if (scrollSprite != null)
            {
                handleImg.sprite = scrollSprite;
                handleImg.type = Image.Type.Sliced;
            }
            else
            {
                handleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;

            return scrollbar;
        }
        private static void DestroyAllChildrenImmediate(RectTransform root)
        {
            if (root == null) return;

            root.gameObject.SetActive(false);

            var toDestroy = new List<GameObject>();
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child != null) toDestroy.Add(child.gameObject);
            }

            foreach (var go in toDestroy)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            root.gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
        }

        // ===================== RES FRAMES =====================

        private static class ResFrames
        {
            private static readonly Dictionary<string, Sprite> _cache = new();
            private static readonly Dictionary<string, Texture2D> _texCache = new();

            public static void ClearCache()
            {
                _cache.Clear();
                _texCache.Clear();
            }

            public static Texture2D GetTexture(string folder, string frameName)
            {
                string key = folder + "/" + frameName;

                if (_texCache.TryGetValue(key, out var cached) && cached != null)
                    return cached;

                var tex = Resources.Load<Texture2D>(key);
                if (tex != null)
                {
                    _texCache[key] = tex;
                    return tex;
                }

                return null;
            }

            public static Sprite GetByName(string folder, string frameName)
            {
                string key = folder + "/" + frameName;

                if (_cache.TryGetValue(key, out var sp) && sp != null)
                    return sp;

                var tex = Resources.Load<Texture2D>(key);
                if (tex == null) return null;

                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 1f);
                sp.name = frameName;
                _cache[key] = sp;

                return sp;
            }
        }

        private static class SpriteCropper
        {
            private static readonly Dictionary<string, Sprite> _cache = new();

            public static Sprite CropLeft(Sprite src, int leftPx)
            {
                if (src == null || leftPx <= 0) return src;

                string key = $"{src.texture.GetEntityId()}:{src.rect.x}:{src.rect.y}:{src.rect.width}:{src.rect.height}:L{leftPx}";
                if (_cache.TryGetValue(key, out var s) && s != null) return s;

                var r = src.rect;
                if (r.width <= leftPx + 1) return src;

                var newRect = new Rect(r.x + leftPx, r.y, r.width - leftPx, r.height);

                Vector2 pivotPx = new Vector2(src.pivot.x * r.width, src.pivot.y * r.height);
                pivotPx.x = Mathf.Max(0f, pivotPx.x - leftPx);
                var newPivot = new Vector2(pivotPx.x / newRect.width, pivotPx.y / newRect.height);

                var outSp = Sprite.Create(src.texture, newRect, newPivot, 1f);
                outSp.name = src.name + $"_cropL{leftPx}";
                _cache[key] = outSp;
                return outSp;
            }
        }

        private static Sprite LoadSpriteFromResources(string folder, string frameName)
        {
            var sp = Resources.Load<Sprite>($"{folder}/{frameName}");
            if (sp != null)
            {
                PrepareUiSpriteSamplingV396A7R4(sp);
                return sp;
            }

            var tex = Resources.Load<Texture2D>($"{folder}/{frameName}");
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            var created = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 1f);
            PrepareUiSpriteSamplingV396A7R4(created);
            return created;
        }

        private static void PrepareUiSpriteSamplingV396A7R4(Sprite sp)
        {
            if (sp == null || sp.texture == null) return;
            // Original GPS.ShowGP is a framebuffer blit; it does not bilinear-filter
            // neighboring transparent texels.  Point+Clamp prevents the remaining
            // one-pixel hairlines between independently rendered GP pieces.
            sp.texture.filterMode = FilterMode.Point;
            sp.texture.wrapMode = TextureWrapMode.Clamp;
        }

        // ===================== OPTIONS CONTROLS =====================
        private static bool _isAddProfileCtx;
        private static void CreateCheckBox(
            UiCheckBox cb,
            UiDesk desk,
            RectTransform parent,
            RenderOptions opt,
            IUiActionSink sink,
            LocDb loc,
            int index)
        {

            // CUT мусорные чекбоксы
            if (index == 1 || index == 5) return;

            const string folder = "interf3_elements_checkbox_frames";

            var spOff = ResFrames.GetByName(folder, "frame_0000");
            var spOn = ResFrames.GetByName(folder, "frame_0001");

            var go = new GameObject($"CheckBox_{index:00}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(cb.X, -cb.Y);

            var sizeTex = (spOff != null ? spOff.texture : (spOn != null ? spOn.texture : null));
            rt.sizeDelta = sizeTex != null ? new Vector2(sizeTex.width, sizeTex.height) : new Vector2(16, 16);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            img.sprite = cb.State ? (spOn ?? spOff) : (spOff ?? spOn);
            img.preserveAspect = false;
            img.type = Image.Type.Simple;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;

            var dbg = go.AddComponent<CheckBoxDebugToggle>();
            dbg.Index = index;
            dbg.Image = img;
            dbg.SpriteOff = spOff;
            dbg.SpriteOn = spOn;
            dbg.State = cb.State;
        }

        // V392: AddProfile nation/difficulty data is owned by the shared
        // Menu14ActionStateRuntime source context.  OptionsRenderer no longer
        // reads AI\\ai.dat independently from DataRoot.

        private static bool HasActionPrefix(UiNode node, string prefix)
        {
            if (node?.Actions == null || node.Actions.Count == 0 || string.IsNullOrEmpty(prefix)) return false;
            for (int i = 0; i < node.Actions.Count; i++)
            {
                var a = node.Actions[i];
                if (a == null || string.IsNullOrEmpty(a.Name)) continue;
                if (a.Name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string[] SplitWs(string s)
        {
            return System.Text.RegularExpressions.Regex.Split(s.Trim(), "\\s+");
        }

        private static bool HasAction(UiComboBox box, string actionName)
        {
            if (box == null) return false;
            for (int i = 0; i < box.Actions.Count; i++)
                if (box.Actions[i] != null && box.Actions[i].Name != null &&
                    box.Actions[i].Name.Equals(actionName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static System.Collections.Generic.List<string> ResolveComboItems(UiComboBox box, CoreFileSystem fs, RenderOptions opt, LocDb loc, out string initialText)
        {
            initialText = "";

            // AddProfile: Nation.  V392 uses the same source-bound GlobalAI
            // table as cva_ProfAdd_RaceFlg and the portrait controller.
            if (HasAction(box, "cva_ProfAdd_Race"))
            {
                var items = Menu14ActionStateRuntime.GetNationDisplayNames(loc);
                initialText = items.Count > 0 ? items[0] : "";
                return items;
            }

            // AddProfile: Difficulty from the same source-bound AI\\ai.dat.
            if (HasAction(box, "cva_ProfAdd_Diff"))
            {
                var items = Menu14ActionStateRuntime.GetDifficultyDisplayNames(loc);
                initialText = items.Count > 0 ? items[0] : "";
                return items;
            }

            // EW2 campaign statistics: cva_CampStat_Mode::Init adds these exact
            // nine localized lines and sets CurLine=0.  Keep the list sourced by
            // the original action runtime instead of leaving the XML ComboBox empty.
            if (HasAction(box, "cva_CampStat_Mode"))
            {
                var items = Menu14ActionStateRuntime.GetCampaignStatsModeDisplayNames(loc);
                initialText = items.Count > 0 ? items[0] : "";
                return items;
            }

            // Options: video mode (resolution)
            if (opt != null && opt.FillResolutionCombos && HasAction(box, "cva_Opt_VMode"))
            {
                var list = BuildResolutionList();
                initialText = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
                return list;
            }

            // Fallback: empty
            return new System.Collections.Generic.List<string>();
        }

        private static void CreateComboBox(UiComboBox box, UiDesk desk, CoreFileSystem fs, RectTransform parent, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            const string folder = "Interf3_elements_combo_frames";

            var spClosed = ResFrames.GetByName(folder, "frame_0000");
            var spOpen = ResFrames.GetByName(folder, "frame_0001");
            var spRow = ResFrames.GetByName(folder, "frame_0005");
            var spRowHover = ResFrames.GetByName(folder, "frame_0006");

            const int CROP_L = 3;
            spRow = SpriteCropper.CropLeft(spRow, CROP_L);
            spRowHover = SpriteCropper.CropLeft(spRowHover, CROP_L);

            string comboName = $"ComboBox_{SafeName(box.Name)}";
            if (HasAction(box, "cva_ProfAdd_Race")) comboName = "ComboBox_ProfAdd_Race";
            else if (HasAction(box, "cva_ProfAdd_Diff")) comboName = "ComboBox_ProfAdd_Diff";
            else if (HasAction(box, "cva_CampStat_Mode")) comboName = "ComboBox_CampStat_Mode";
            var go = new GameObject(comboName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(box.X, -box.Y);
            rt.sizeDelta = new Vector2(box.Width, box.Height);

            var boxImgGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxImgGO.transform.SetParent(go.transform, false);

            var boxRt = (RectTransform)boxImgGO.transform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0, 1);
            boxRt.pivot = new Vector2(0, 1);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(box.Width, box.Height);

            var boxImg = boxImgGO.GetComponent<Image>();
            boxImg.raycastTarget = true;
            boxImg.sprite = spClosed;
            boxImg.preserveAspect = false;
            boxImg.type = Image.Type.Simple;

            var textGO = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(boxImgGO.transform, false);

            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = new Vector2(24, 2);
            trt.offsetMax = new Vector2(-35, -2);

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            ApplyTextStyle(tmp, UiTextStyle.OptionLabel, opt);

            var items = ResolveComboItems(box, fs, opt, loc, out string initialText);
            tmp.text = initialText;

            // V391: bind the exact XML ComboBox node to the common state runtime.
            Menu14ActionStateRuntime.RegisterControl(box, go, fs);

            // Original Init sets CurLine=0 for both AddProfile combos and
            // cva_CampStat_Mode.  V395P separates initialization from a user
            // selection: campaign statistics used to call the full statistics
            // refresh here and then immediately refresh a second time in the
            // single post-build ApplyAllFrameStates pass.
            if (items.Count > 0)
                Menu14ActionStateRuntime.InitializeComboSelection(box, 0);

            if (HasAction(box, "cva_CampStat_Mode") && items.Count > 0)
            {
                var keys = go.AddComponent<CampaignStatsModeKeys>();
                keys.Box = box;
                keys.Label = tmp;
                keys.Items = items.ToArray();
            }

            // V390: nation flag is NOT synthesized by ComboBox anymore.
            // The real M_PROF_ADD GPPicture (INTERF3\\FLAG + cva_ProfAdd_RaceFlg)
            // is rendered from XML and updated by Menu14ActionStateRuntime.

            float rowH = 20f;
            int maxVisible = 14;
            int visibleCount = Mathf.Min(maxVisible, items.Count);

            float topPad = 2f;
            float botPad = 4f;
            float panelH = topPad + botPad + visibleCount * rowH;
            float panelW = box.Width;

            var panelGO = new GameObject("ComboDropPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);

            var panelRt = (RectTransform)panelGO.transform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0, 1);
            panelRt.pivot = new Vector2(0, 1);
            panelRt.anchoredPosition = new Vector2(box.X + 10f, -box.Y - box.Height);
            panelRt.sizeDelta = new Vector2(panelW, panelH);

            var panelImg = panelGO.GetComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0);
            panelImg.raycastTarget = true;

            var blockerGO = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
            blockerGO.transform.SetParent(parent, false);

            var blockerRt = (RectTransform)blockerGO.transform;
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = Vector2.zero;
            blockerRt.offsetMax = Vector2.zero;

            var blockerImg = blockerGO.GetComponent<Image>();
            blockerImg.color = new Color(0, 0, 0, 0);
            blockerImg.raycastTarget = true;

            var rowsContainer = new GameObject("Rows", typeof(RectTransform));
            rowsContainer.transform.SetParent(panelGO.transform, false);

            var rowsRt = (RectTransform)rowsContainer.transform;
            rowsRt.anchorMin = new Vector2(0, 1);
            rowsRt.anchorMax = new Vector2(1, 1);
            rowsRt.pivot = new Vector2(0, 1);
            rowsRt.anchoredPosition = new Vector2(0, -topPad);
            rowsRt.sizeDelta = new Vector2(0, visibleCount * rowH);

            var controller = boxImgGO.AddComponent<ComboBoxController>();
            controller.Panel = panelGO;
            controller.Blocker = blockerGO;
            controller.BoxImage = boxImg;
            controller.SpriteClosed = spClosed;
            controller.SpriteOpen = spOpen ?? spClosed;
            // external listeners (AddProfile)
            controller.OnSelected = null;

            for (int i = 0; i < visibleCount; i++)
            {
                string itemText = items[i];

                var row = new GameObject($"Row_{i:00}", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(rowsContainer.transform, false);

                var rrt = (RectTransform)row.transform;
                rrt.anchorMin = new Vector2(0, 1);
                rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0, 1);
                rrt.anchoredPosition = new Vector2(0, -i * rowH);
                rrt.sizeDelta = new Vector2(0, rowH);

                var rowImg = row.GetComponent<Image>();
                rowImg.raycastTarget = true;
                rowImg.sprite = spRow;
                rowImg.type = Image.Type.Sliced;
                rowImg.color = Color.white;

                var lab = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                lab.transform.SetParent(row.transform, false);

                var lrt = (RectTransform)lab.transform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(35, 0);
                lrt.offsetMax = new Vector2(-6, 0);

                var ltmp = lab.GetComponent<TextMeshProUGUI>();
                ltmp.raycastTarget = false;
                ltmp.richText = false;
                ltmp.textWrappingMode = TextWrappingModes.NoWrap;
                ltmp.alignment = TextAlignmentOptions.Left;
                ltmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                ltmp.text = itemText;
                ApplyTextStyle(ltmp, UiTextStyle.OptionLabel, opt);

                var hover = row.AddComponent<RowHoverSwap>();
                hover.Bg = rowImg;
                hover.NormalSprite = spRow;
                hover.HoverSprite = spRowHover ?? spRow;

                string capturedText = itemText;
                int capturedIndex = i;
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    tmp.text = capturedText;

                    // V391: ComboBox SetFrameState actions are interpreted by the
                    // shared runtime. They are not synthetic button-click actions.
                    bool handledAsState = Menu14ActionStateRuntime.OnComboSelectionChanged(box, capturedIndex);
                    if (!handledAsState && sink != null && box.Actions.Count > 0)
                        sink.OnAction(box.Hint, box.Actions[0]);

                    controller.OnSelected?.Invoke(capturedIndex, capturedText);
                    controller.ClosePopup();
                });
            }

            var blockerClick = blockerGO.AddComponent<Button>();
            blockerClick.transition = Selectable.Transition.None;
            blockerClick.onClick.AddListener(controller.ClosePopup);

            blockerGO.SetActive(false);
            panelGO.SetActive(false);
        }


        private sealed class CampaignStatsModeKeys : MonoBehaviour
        {
            public UiComboBox Box;
            public TextMeshProUGUI Label;
            public string[] Items;
            private int _index;

            private void Update()
            {
                if (Box == null || Items == null || Items.Length == 0) return;
                if (Label != null)
                {
                    int shown = Array.IndexOf(Items, Label.text);
                    if (shown >= 0) _index = shown;
                }
                int next = _index;
#if ENABLE_INPUT_SYSTEM
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame) next--;
                    else if (keyboard.downArrowKey.wasPressedThisFrame) next++;
                }
#else
                if (Input.GetKeyDown(KeyCode.UpArrow)) next--;
                else if (Input.GetKeyDown(KeyCode.DownArrow)) next++;
#endif
                next = Mathf.Clamp(next, 0, Items.Length - 1);
                if (next == _index) return;
                _index = next;
                if (Label != null) Label.text = Items[_index];
                Menu14ActionStateRuntime.OnComboSelectionChanged(Box, _index);
            }
        }

        private static List<string> BuildResolutionList()
        {
            var hs = new HashSet<string>();
            var res = Screen.resolutions;
            for (int i = 0; i < res.Length; i++)
                hs.Add($"{res[i].width}x{res[i].height}");

            var list = new List<string>(hs);
            list.Sort((a, b) =>
            {
                Parse(a, out int aw, out int ah);
                Parse(b, out int bw, out int bh);
                int c = aw.CompareTo(bw);
                return c != 0 ? c : ah.CompareTo(bh);
            });
            return list;

            static void Parse(string s, out int w, out int h)
            {
                w = 0; h = 0;
                int x = s.IndexOf('x');
                if (x <= 0) return;
                int.TryParse(s.Substring(0, x), out w);
                int.TryParse(s.Substring(x + 1), out h);
            }
        }

        private static bool HasAction(UiNode node, string actionName)
        {
            if (node == null || node.Actions == null) return false;
            for (int i = 0; i < node.Actions.Count; i++)
                if (node.Actions[i] != null && node.Actions[i].Name != null &&
                    node.Actions[i].Name.Equals(actionName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static void CreateSlider(UiSlider sl, UiDesk desk, RectTransform parent, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            // AddProfile portrait scroller: render as vertical scrollbar with scroll3 frames
            if (HasAction(sl, "cva_ProfAdd_PortScr"))
            {
                CreateAddProfilePortraitHScroll(sl, parent);
                return;
            }

            const string folder = "interf3_elements_slider_frames";
            const int removeLeftLamellas = 1;
            const int gapPx = 1;
            const int REAL_LAM_W = 10, REAL_LAM_H = 18;
            const int REAL_THUMB_W = 15, REAL_THUMB_H = 20;

            Sprite spThumb = ResFrames.GetByName(folder, "frame_0000");
            Sprite spLam0 = ResFrames.GetByName(folder, "frame_0003");
            if (spThumb == null) return;

            int lineW = (sl.LineLx > 0) ? sl.LineLx : Mathf.Max(1, sl.Width);
            int lineH = (sl.LineLy > 0) ? sl.LineLy : Mathf.Max(1, sl.Height);
            int max = Mathf.Max(1, sl.MaxPosition);
            int pos = Mathf.Clamp(sl.Position, 0, max);

            float lamY = -Mathf.Max(0f, (lineH - REAL_LAM_H) * 0.5f);
            float thumbY = -Mathf.Max(0f, (lineH - REAL_THUMB_H) * 0.5f);

            int rawCount = Mathf.Max(1, (lineW + gapPx) / (REAL_LAM_W + gapPx));
            int count = Mathf.Max(1, rawCount - removeLeftLamellas);
            int startX = removeLeftLamellas * (REAL_LAM_W + gapPx);

            var sliderRoot = new GameObject($"Slider_{SafeName(sl.Name)}", typeof(RectTransform));
            sliderRoot.transform.SetParent(parent, false);

            var rrt = (RectTransform)sliderRoot.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(0, 1);
            rrt.pivot = new Vector2(0, 1);
            rrt.anchoredPosition = new Vector2(sl.X, -sl.Y);
            rrt.sizeDelta = new Vector2(sl.Width > 0 ? sl.Width : lineW, sl.Height > 0 ? sl.Height : lineH);

            var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGO.transform.SetParent(sliderRoot.transform, false);

            var trackRT = (RectTransform)trackGO.transform;
            trackRT.anchorMin = trackRT.anchorMax = new Vector2(0, 1);
            trackRT.pivot = new Vector2(0, 1);
            trackRT.anchoredPosition = new Vector2(sl.ScrDx, sl.ScrDy);
            trackRT.sizeDelta = new Vector2(lineW, lineH);

            var trackImg = trackGO.GetComponent<Image>();
            trackImg.color = new Color(0, 0, 0, 0);
            trackImg.raycastTarget = true;

            var lamContainer = new GameObject("Lamellas", typeof(RectTransform));
            lamContainer.transform.SetParent(trackGO.transform, false);

            var lamContainerRT = (RectTransform)lamContainer.transform;
            lamContainerRT.anchorMin = lamContainerRT.anchorMax = new Vector2(0, 1);
            lamContainerRT.pivot = new Vector2(0, 1);
            lamContainerRT.anchoredPosition = Vector2.zero;
            lamContainerRT.sizeDelta = new Vector2(lineW, lineH);

            for (int i = 0; i < count; i++)
            {
                int x = startX + i * (REAL_LAM_W + gapPx);

                var seg = new GameObject($"Lam_{i:00}", typeof(RectTransform), typeof(Image));
                seg.transform.SetParent(lamContainer.transform, false);

                var srt = (RectTransform)seg.transform;
                srt.anchorMin = srt.anchorMax = new Vector2(0, 1);
                srt.pivot = new Vector2(0, 1);
                srt.anchoredPosition = new Vector2(x, lamY);
                srt.sizeDelta = new Vector2(REAL_LAM_W, REAL_LAM_H);

                var img = seg.GetComponent<Image>();
                img.sprite = spLam0;
                img.raycastTarget = false;
                img.type = Image.Type.Simple;
            }

            int thumbW = REAL_THUMB_W;
            float initialT = (max > 0) ? (pos / (float)max) : 0f;

            float minPx = startX;
            float maxPx = Mathf.Max(minPx, lineW - thumbW);
            float initialPx = Mathf.Lerp(minPx, maxPx, initialT);

            var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(sliderRoot.transform, false);

            var thRT = (RectTransform)thumbGO.transform;
            thRT.anchorMin = thRT.anchorMax = new Vector2(0, 1);
            thRT.pivot = new Vector2(0, 1);
            thRT.sizeDelta = new Vector2(REAL_THUMB_W, REAL_THUMB_H);

            float thumbBaseX = sl.ScrDx + initialPx;
            float thumbBaseY = sl.ScrDy + thumbY;
            thRT.anchoredPosition = new Vector2(thumbBaseX, thumbBaseY);

            var thImg = thumbGO.GetComponent<Image>();
            thImg.sprite = spThumb;
            thImg.raycastTarget = false;
            thImg.type = Image.Type.Simple;
            thImg.preserveAspect = false;

            thumbGO.transform.SetAsLastSibling();

            var handler = sliderRoot.AddComponent<SliderController>();
            handler.Initialize(
                trackRT: trackRT,
                thumbRT: thRT,
                thumbImage: thImg,
                max: max,
                initialPos: pos,
                thumbY: thumbBaseY,
                trackOffsetX: sl.ScrDx,
                lineWidth: lineW,
                thumbWidth: thumbW,
                lamMinX: sl.ScrDx + startX
            );
        }

        private static void CreateAddProfilePortraitHScroll(UiSlider sl, RectTransform parent)
        {
            const string folder = "Interf3_elements_scroll3_frames";
            // frame_0000/0001 are arrow/button-like frames in this asset set;
            // track/thumb for AddProfile portrait strip are 0004/0005.
            var spTrack = ResFrames.GetByName(folder, "frame_0004") ?? ResFrames.GetByName(folder, "frame_0000");
            var spThumb = ResFrames.GetByName(folder, "frame_0005") ?? ResFrames.GetByName(folder, "frame_0001");
            if (spTrack == null || spThumb == null) return;

            var root = new GameObject("HScroll_ProfAdd_PortScr", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rrt = (RectTransform)root.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(0, 1);
            rrt.pivot = new Vector2(0, 1);
            rrt.anchoredPosition = new Vector2(sl.X, -sl.Y);
            rrt.sizeDelta = new Vector2(sl.Width, sl.Height);

            var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGO.transform.SetParent(root.transform, false);
            var trackRT = (RectTransform)trackGO.transform;
            trackRT.anchorMin = trackRT.anchorMax = new Vector2(0, 1);
            trackRT.pivot = new Vector2(0, 1);
            trackRT.anchoredPosition = Vector2.zero;
            trackRT.sizeDelta = new Vector2(sl.Width, sl.Height);

            var trackImg = trackGO.GetComponent<Image>();
            trackImg.sprite = spTrack;
            // Track must not "stretch" a single pixel column; tile it.
            trackImg.type = Image.Type.Tiled;
            trackImg.raycastTarget = true;

            var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(trackGO.transform, false);
            var thumbRT = (RectTransform)thumbGO.transform;
            thumbRT.anchorMin = thumbRT.anchorMax = new Vector2(0, 1);
            thumbRT.pivot = new Vector2(0, 1);
            // Keep native size so it is not distorted.
            thumbRT.sizeDelta = new Vector2(spThumb.rect.width, spThumb.rect.height);

            var thumbImg = thumbGO.GetComponent<Image>();
            thumbImg.sprite = spThumb;
            thumbImg.type = Image.Type.Simple;
            thumbImg.preserveAspect = true;
            thumbImg.raycastTarget = true;

            int max = Mathf.Max(1, sl.MaxPosition);
            int pos = Mathf.Clamp(sl.Position, 0, max);

            var ctrl = root.AddComponent<Cossacks2Bridge.UnityAdapters.AddProfile.HorizontalScrollbarController>();
            ctrl.Initialize(trackRT, thumbRT, max, pos);
        }

        private static Color32 ResolveNodeColorV396A7R2(UiNode node)
        {
            uint c = node != null ? node.ColorArgb : 0xFFFFFFFFu;
            return new Color32(
                (byte)((c >> 16) & 0xFF),
                (byte)((c >> 8) & 0xFF),
                (byte)(c & 0xFF),
                (byte)((c >> 24) & 0xFF));
        }

        private static void CreateGPPicture(RectTransform parent, UiGPPicture gp, CoreFileSystem fs, RenderOptions opt)
        {
            var fid = (gp?.FileID ?? "").Trim().Replace('\\', '/');

            // ===== Profile portraits (lva_XXs): keep the real XML GPPicture =====
            // Original cva_ProfCur_Port/cva_ProfAdd_Port only changes FileID and
            // SpriteID of this existing control.  V395C therefore creates exactly
            // this Image and lets the common SetFrameState runtime fill it.
            if (!string.IsNullOrEmpty(fid) &&
                fid.StartsWith("Interf3/TotalWarGraph/lva_", System.StringComparison.OrdinalIgnoreCase))
            {
                bool isProfAddPort = gp?.Actions != null &&
                    gp.Actions.Exists(a => a != null &&
                                          !string.IsNullOrEmpty(a.Name) &&
                                          a.Name.Equals("cva_ProfAdd_Port", System.StringComparison.OrdinalIgnoreCase));
                bool isProfCurPort = gp?.Actions != null &&
                    gp.Actions.Exists(a => a != null &&
                                          !string.IsNullOrEmpty(a.Name) &&
                                          a.Name.Equals("cva_ProfCur_Port", System.StringComparison.OrdinalIgnoreCase));

                string portraitGoName = isProfAddPort ? "GPPicture_ProfAdd_Port" :
                                        (isProfCurPort ? "GPPicture_ProfCur_Port" : "GPPicture");

                var portraitGo = new GameObject(portraitGoName, typeof(RectTransform), typeof(Image));
                portraitGo.transform.SetParent(parent, false);

                var portraitRt = (RectTransform)portraitGo.transform;
                portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0, 1);
                portraitRt.pivot = new Vector2(0, 1);
                portraitRt.anchoredPosition = new Vector2(gp.X, -gp.Y);
                portraitRt.sizeDelta = new Vector2(gp.Width, gp.Height);

                var portraitImg = portraitGo.GetComponent<Image>();
                portraitImg.type = Image.Type.Simple;
                portraitImg.preserveAspect = false;
                portraitImg.raycastTarget = false;
                portraitImg.color = ResolveNodeColorV396A7R2(gp);
                portraitImg.sprite = Menu14ActionStateRuntime.TryLoadPortraitSpriteForRenderer(gp.FileID, gp.SpriteID);
                portraitImg.enabled = portraitImg.sprite != null;

                Menu14ActionStateRuntime.RegisterControl(gp, portraitGo, fs);
                return;
            }

            // V390: render the original XML flag GPPicture.
            // Original cva_ProfAdd_RaceFlg::SetFrameState changes only SpriteID
            // to GlobalAI.Ai[vNewProf.m_iNation].NWaterAI.
            if (fid.Equals("INTERF3/FLAG", System.StringComparison.OrdinalIgnoreCase))
            {
                bool isProfAddRaceFlag = gp?.Actions != null &&
                    gp.Actions.Exists(a => a != null &&
                                          !string.IsNullOrEmpty(a.Name) &&
                                          a.Name.Equals("cva_ProfAdd_RaceFlg", System.StringComparison.OrdinalIgnoreCase));

                var flagGo = new GameObject(
                    isProfAddRaceFlag ? "GPPicture_ProfAdd_RaceFlg" : "GPPicture",
                    typeof(RectTransform), typeof(Image));
                flagGo.transform.SetParent(parent, false);

                var flagRt = (RectTransform)flagGo.transform;
                flagRt.anchorMin = flagRt.anchorMax = new Vector2(0, 1);
                flagRt.pivot = new Vector2(0, 1);
                flagRt.anchoredPosition = new Vector2(gp.X, -gp.Y);
                flagRt.sizeDelta = new Vector2(gp.Width, gp.Height);

                var flagImg = flagGo.GetComponent<Image>();
                flagImg.sprite = LoadSpriteFromResources("INTERF3_FLAG_frames", $"frame_{gp.SpriteID:0000}");
                flagImg.type = Image.Type.Simple;
                flagImg.preserveAspect = true;
                flagImg.raycastTarget = false;
                flagImg.color = ResolveNodeColorV396A7R2(gp);

                // V391: bind the real XML GPPicture and its
                // cva_ProfAdd_RaceFlg action to the common state runtime.
                Menu14ActionStateRuntime.RegisterControl(gp, flagGo, fs);

                return;
            }


            if (VerboseLayoutLogs) Debug.Log($"[CreateGPPicture HIT] FileID={gp?.FileID} SpriteID={gp?.SpriteID}");

            string resPath = (gp.FileID ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToUpperInvariant() + "_frames";

            var loadedSprite = LoadSpriteFromResources(resPath, $"frame_{gp.SpriteID:0000}");
            if (loadedSprite == null)
            {
                // V395C: not every clean 1.4 GP bank has been pre-exported to
                // Unity Resources. Resolve the exact XML FileID/SpriteID directly
                // from the active C2 Data/Cash bank instead of producing a white
                // placeholder or dropping the control.
                loadedSprite = Menu14ActionStateRuntime.TryLoadGpSpriteForRenderer(gp.FileID, gp.SpriteID, true);
            }
            if (loadedSprite == null)
            {
                Debug.LogWarning($"[GPPicture] sprite not found {resPath}/frame_{gp.SpriteID:0000} runtimeFallback=missing");
                return;
            }

            string normalGoName = "GPPicture";
            bool isPortraitBorder = fid.Equals("INTERF3/ELEMENTS/PORTRAITS_BORDER", System.StringComparison.OrdinalIgnoreCase);
            if (gp?.Actions != null)
            {
                if (gp.Actions.Exists(a => a != null && !string.IsNullOrEmpty(a.Name) &&
                                          a.Name.Equals("cva_ProfAdd_Port", System.StringComparison.OrdinalIgnoreCase)))
                    normalGoName = "GPPicture_ProfAdd_Port";
                else if (gp.Actions.Exists(a => a != null && !string.IsNullOrEmpty(a.Name) &&
                                               a.Name.Equals("cva_ProfAdd_RaceFlg", System.StringComparison.OrdinalIgnoreCase)))
                    normalGoName = "GPPicture_ProfAdd_RaceFlg";
                else if (isPortraitBorder)
                    normalGoName = "GPPicture_ProfAdd_PortFrame";
            }
            else if (isPortraitBorder)
            {
                normalGoName = "GPPicture_ProfAdd_PortFrame";
            }

            Transform visualParent = parent;
            float visualX = gp.X;
            float visualY = gp.Y;

            // V396A7R4: ParentFrame::PushMatrix/GetMatrix parity for source
            // GPPictures.  The delete-profile XML contains two internet_menu
            // ornaments with Angle=90 and Angle=270.  R2/R3 parsed them as ordinary
            // unrotated Images, which put the first ornament directly over the
            // portrait.  The original engine rotates each around its XML pivot.
            if (gp.EnableTransform)
            {
                ResolveSourcePivotV396A7R4(gp, out float pivotX, out float pivotY);

                var transformHost = new GameObject("GPPicture_SourceTransform_V396A7R4", typeof(RectTransform));
                transformHost.transform.SetParent(parent, false);
                var hostRt = (RectTransform)transformHost.transform;
                hostRt.anchorMin = hostRt.anchorMax = new Vector2(0f, 1f);
                hostRt.pivot = new Vector2(0f, 1f);
                hostRt.anchoredPosition = new Vector2(pivotX, -pivotY);
                hostRt.sizeDelta = Vector2.zero;

                float sx = gp.FlipX ? -gp.TransformScaleX : gp.TransformScaleX;
                float sy = gp.FlipY ? -gp.TransformScaleY : gp.TransformScaleY;
                hostRt.localScale = new Vector3(sx, sy, 1f);

                // Cossacks screen Y grows downward.  Unity UI local Y grows upward,
                // so original +Angle is Unity -Angle.
                hostRt.localRotation = Quaternion.Euler(0f, 0f, -gp.TransformAngle);

                visualParent = hostRt;
                visualX = gp.X - pivotX;
                visualY = gp.Y - pivotY;

                if (fid.Equals("Interf3/elements/internet_menu", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[C2:PROFILE ORNAMENT V396A7R4] file='{gp.FileID}' sprite={gp.SpriteID} " +
                              $"source=({gp.X},{gp.Y},{gp.Width},{gp.Height}) pivot=({pivotX:0.###},{pivotY:0.###}) " +
                              $"angle={gp.TransformAngle:0.###} scale=({sx:0.###},{sy:0.###}) mode=ParentFrame_GetMatrix");
                }
            }

            var normalGo = new GameObject(normalGoName, typeof(RectTransform), typeof(Image));
            normalGo.transform.SetParent(visualParent, false);

            var normalRt = (RectTransform)normalGo.transform;
            normalRt.anchorMin = normalRt.anchorMax = new Vector2(0, 1);
            normalRt.pivot = new Vector2(0, 1);
            normalRt.anchoredPosition = new Vector2(visualX, -visualY);
            normalRt.sizeDelta = new Vector2(gp.Width, gp.Height);

            var normalImg = normalGo.GetComponent<Image>();
            normalImg.sprite = loadedSprite;
            normalImg.type = Image.Type.Simple;
            normalImg.preserveAspect = false;
            normalImg.raycastTarget = false;
            normalImg.color = ResolveNodeColorV396A7R2(gp);

            // V391: every XML GPPicture can participate in the common
            // SetFrameState runtime. Unknown actions remain inert.
            Menu14ActionStateRuntime.RegisterControl(gp, normalGo, fs);

            // V395E: DO NOT synthesize a second AddProfile portrait slot here.
            // M_PROF_ADD already contains the real nested cva_ProfAdd_Port GPPicture.
            // The old compatibility child had the same GameObject name; GameObject.Find
            // could bind the controller to that child while the real XML lva_EGs portrait
            // stayed visible underneath, producing the persistent Wellington overlay.

        }

        private static void ResolveSourcePivotV396A7R4(UiNode node, out float pivotX, out float pivotY)
        {
            // Dialogs.cpp::ParentFrame::GetMatrix:
            // Left   -> (x,y)
            // Center -> ((x+x1)/2,(y+y1)/2) using integer coordinates
            // Right  -> (x1,y1)
            int x1 = node.X + Mathf.Max(0, node.Width - 1);
            int y1 = node.Y + Mathf.Max(0, node.Height - 1);
            string pivot = node.PivotPosition ?? "Left";

            if (pivot.Equals("Center", StringComparison.OrdinalIgnoreCase))
            {
                pivotX = (node.X + x1) / 2;
                pivotY = (node.Y + y1) / 2;
            }
            else if (pivot.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                pivotX = x1;
                pivotY = y1;
            }
            else
            {
                pivotX = node.X;
                pivotY = node.Y;
            }

            pivotX += node.PivotDx;
            pivotY += node.PivotDy;
        }


        private static void CreateInputBox(UiInputBox ib, RectTransform root, RenderOptions opt, UiVitButton parentSurfaceV389)
        {
            var go = new GameObject($"InputBox_{SafeName(ib.Name)}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(ib.X, -ib.Y);

            float w = ib.Width > 0 ? ib.Width : 320;
            float h = ib.Height > 0 ? ib.Height : 20;
            rt.sizeDelta = new Vector2(w, h);

            var bgImg = go.GetComponent<Image>();
            // V389: when V388 says this InputBox is a child of a VitButton, the
            // parent already rendered the exact GP_File/Sprite surface from XML.
            // This editor layer is therefore transparent and exists only for
            // caret/text/raycast.  No Unity-selected background color is allowed.
            bool hasXmlVitSurfaceV389 = parentSurfaceV389 != null &&
                                        !string.IsNullOrWhiteSpace(parentSurfaceV389.GP_File) &&
                                        parentSurfaceV389.SpritePassive >= 0;
            bgImg.color = Color.clear;
            bgImg.raycastTarget = true;
            if (!hasXmlVitSurfaceV389)
            {
                Debug.Log($"[C2:INPUT V389] no VitButton XML surface for action='{ib.Action}' sourceId={ib.SourceId}; keeping editor transparent (no invented Unity skin)");
            }

            var textAreaGO = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textAreaGO.transform.SetParent(go.transform, false);

            var textAreaRt = (RectTransform)textAreaGO.transform;
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(5, 2);
            textAreaRt.offsetMax = new Vector2(-5, -2);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(textAreaGO.transform, false);

            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.text = "";

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(textAreaGO.transform, false);

            var prt = (RectTransform)placeholderGO.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var ptmp = placeholderGO.GetComponent<TextMeshProUGUI>();
            ptmp.fontSize = 14;
            ptmp.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            ptmp.alignment = TextAlignmentOptions.Left;
            ptmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            ptmp.raycastTarget = false;
            ptmp.fontStyle = FontStyles.Italic;
            ptmp.text = "Введите имя...";

            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textComponent = tmp;
            inputField.placeholder = ptmp;
            inputField.textViewport = textAreaRt;
            inputField.targetGraphic = bgImg;

            inputField.characterLimit = ib.MaxLen > 0 ? ib.MaxLen : 30;
            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.inputType = TMP_InputField.InputType.Standard;
            inputField.keyboardType = TouchScreenKeyboardType.Default;

            inputField.caretColor = Color.black;
            inputField.caretWidth = 2;
            inputField.caretBlinkRate = 0.85f;
            inputField.selectionColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);

            inputField.interactable = ib.Enabled;
            inputField.readOnly = !ib.Enabled;

            bool isNetworkNick = HasAction(ib, "cva_MU_NickInput");
            if (isNetworkNick)
            {
                go.name = "InputBox_MultiNick_V387B1"; // kept for MenuActionSink compatibility
                inputField.characterLimit = 17; // original cva_MU_NickInput
                // Parent VitButton from V388 owns the original
                // interf3\elements\vbuttons GP skin. Keep only this transparent
                // editor above it so clicks/caret work without repainting the XML.
                bgImg.raycastTarget = true;
                go.transform.SetAsLastSibling();
                ptmp.text = string.Empty;

                string initial = global::MenuActionSink.NetworkPlayerNick;
                if (string.IsNullOrWhiteSpace(initial))
                    initial = global::MenuActionSink.CurrentProfileName;

                if (!string.IsNullOrWhiteSpace(initial))
                    inputField.SetTextWithoutNotify(initial);

                inputField.onValueChanged.AddListener(value =>
                {
                    string sanitized = global::MenuActionSink.SetNetworkPlayerNickV387B1(value);
                    if (!string.Equals(sanitized, value, StringComparison.Ordinal))
                    {
                        int caret = Mathf.Min(inputField.caretPosition, sanitized.Length);
                        inputField.SetTextWithoutNotify(sanitized);
                        inputField.caretPosition = caret;
                    }
                });

                inputField.onEndEdit.AddListener(value =>
                {
                    string sanitized = global::MenuActionSink.SetNetworkPlayerNickV387B1(value);
                    inputField.SetTextWithoutNotify(sanitized);
                    Debug.Log($"[C2:MULTI V389] nick commit='{sanitized}'");
                });

                Debug.Log(
                    $"[C2:MULTI V389] cva_MU_NickInput bound pos=({ib.X},{ib.Y}) " +
                    $"size={w}x{h} value='{inputField.text}' xmlSurface={(hasXmlVitSurfaceV389 ? 1 : 0)} " +
                    $"gp='{parentSurfaceV389?.GP_File}' sprite={parentSurfaceV389?.SpritePassive ?? -1}");
            }
        }

        private static void CreateListDesk(UiListDesk ld, RectTransform root)
        {
            var go = new GameObject("ListDesk", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(ld.X, -ld.Y);
            rt.sizeDelta = new Vector2(ld.Width, ld.Height);

            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
        }

        private static void CreateVitButton(UiVitButton vb, RectTransform parent, RenderOptions opt, IUiActionSink sink)
        {
            if (vb == null || parent == null) return;

            string resPath = (vb.GP_File ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToUpperInvariant() + "_frames";

            var spNormal = LoadSpriteFromResources(resPath, $"frame_{vb.SpritePassive:0000}");
            var spHover = LoadSpriteFromResources(resPath, $"frame_{vb.SpriteActive:0000}") ?? spNormal;

            var go = new GameObject($"VitButton_{SafeName(vb.Name)}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(vb.X, -vb.Y);

            float w = vb.Width > 0 ? vb.Width : (spNormal != null ? spNormal.rect.width : 0f);
            float h = vb.Height > 0 ? vb.Height : (spNormal != null ? spNormal.rect.height : 0f);
            if (w <= 0) w = 16;
            if (h <= 0) h = 16;
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            img.sprite = spNormal;
            img.preserveAspect = false;
            img.type = Image.Type.Simple;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = vb.Enabled;

            var hover = go.AddComponent<VitButtonHoverSwap>();
            hover.Bg = img;
            hover.Normal = spNormal;
            hover.Hover = spHover;

            if (vb.Actions != null && vb.Actions.Count > 0)
            {
                btn.onClick.AddListener(() =>
                {
                    foreach (var a in vb.Actions)
                    {
                        try { sink?.OnAction(vb.Name, a); }
                        catch (Exception e) { Debug.LogError($"VitButton action error: {e}"); }
                    }
                });
            }
        }

        /// <summary>
        /// V395J: renders one runtime ListDesk element from the exact
        /// ListDesk/Element/VitButton source-template fields.  The current menu
        /// model keeps Element as a ListDesk prototype rather than a normal
        /// UiDesk child, so this renderer intentionally does not require a
        /// UiVitButton instance.
        ///
        /// VitButton source behavior reproduced here:
        ///  - State 0/1 selects SpritePassive/Over[state]
        ///  - State==1 uses FontOver even when the mouse is not over the item
        ///  - DisableCycling=false uses DrawHeaderEx2 (five-frame tiled header)
        /// </summary>
        public static GameObject CreateListDeskElementFromSourceTemplateV395J(
            RectTransform parent,
            string message,
            int state,
            bool enabled,
            Action onClick,
            float x,
            float y,
            float width,
            float height,
            string gpFile,
            int passiveSprite,
            int overSprite,
            int spriteDx,
            string passiveFontName,
            string overFontName,
            int fontDx,
            int fontDy,
            string align,
            bool oneSprited,
            bool disableCycling)
        {
            if (parent == null) return null;

            var go = new GameObject($"ListDeskVitButton_{SafeName(message)}_S{state}",
                typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));

            // Transparent hit target. Original visual is drawn by VitButton::_Draw.
            Image hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            GameObject normalVisual = CreateListDeskVitButtonVisualV395J(
                go.transform, gpFile, passiveSprite, spriteDx, width, height, oneSprited, disableCycling, "Passive");
            GameObject hoverVisual = CreateListDeskVitButtonVisualV395J(
                go.transform, gpFile, overSprite, spriteDx, width, height, oneSprited, disableCycling, "Over");
            if (hoverVisual != null) hoverVisual.SetActive(false);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = hit;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            tr.anchoredPosition += new Vector2(fontDx, -fontDy);

            // Original VitButton::_Draw: State==1 selects FontOver even without hover.
            string normalFontName = enabled && state == 1 && !string.IsNullOrWhiteSpace(overFontName)
                ? overFontName
                : passiveFontName;
            string hoverFontResolved = enabled && !string.IsNullOrWhiteSpace(overFontName)
                ? overFontName
                : normalFontName;

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.richText = false;
            tmp.raycastTarget = false;
            tmp.text = message ?? string.Empty;
            tmp.fontSize = ResolveC2FontSize(normalFontName, 14f);
            tmp.color = ResolveC2FontColor(normalFontName, OptionsTextStyleConfig.Button.NormalColor);

            string a = (align ?? string.Empty).Trim();
            if (a.Equals("Right", StringComparison.OrdinalIgnoreCase))
                tmp.alignment = TextAlignmentOptions.MidlineRight;
            else if (a.Equals("Center", StringComparison.OrdinalIgnoreCase))
                tmp.alignment = TextAlignmentOptions.Midline;
            else
                tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var hover = go.AddComponent<ListDeskVitButtonHoverV395J>();
            hover.NormalVisual = normalVisual;
            hover.HoverVisual = hoverVisual;
            hover.Label = tmp;
            hover.NormalText = ResolveC2FontColor(normalFontName, tmp.color);
            hover.HoverText = ResolveC2FontColor(hoverFontResolved, hover.NormalText);
            hover.Enabled = enabled;

            return go;
        }

        private static GameObject CreateListDeskVitButtonVisualV395J(
            Transform parent,
            string gpFile,
            int baseSprite,
            int spriteDx,
            float width,
            float height,
            bool oneSprited,
            bool disableCycling,
            string suffix)
        {
            if (parent == null || baseSprite < 0) return null;

            var visual = new GameObject("Visual_" + suffix, typeof(RectTransform));
            visual.transform.SetParent(parent, false);
            RectTransform vrt = (RectTransform)visual.transform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(0f, 1f);
            vrt.pivot = new Vector2(0f, 1f);
            vrt.anchoredPosition = new Vector2(spriteDx, 0f);
            vrt.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));

            if (disableCycling)
            {
                Sprite sp = LoadGpButtonFrame(gpFile, baseSprite);
                if (sp == null)
                {
                    UnityEngine.Object.DestroyImmediate(visual);
                    return null;
                }
                CreateListDeskSpritePieceV395J(visual.transform, "Sprite", sp, 0f, 0f, sp.rect.width, sp.rect.height);
                return visual;
            }

            // Dialogs.cpp::VitButton::_Draw -> DrawHeaderEx2:
            // OneSprited=false: L=S, R=S+1, C1=S+2, C2=S+3, C3=S+4.
            Sprite spL = oneSprited ? null : LoadGpButtonFrame(gpFile, baseSprite);
            Sprite spR = oneSprited ? null : LoadGpButtonFrame(gpFile, baseSprite + 1);
            Sprite spC1 = LoadGpButtonFrame(gpFile, oneSprited ? baseSprite : baseSprite + 2);
            Sprite spC2 = LoadGpButtonFrame(gpFile, oneSprited ? baseSprite : baseSprite + 3);
            Sprite spC3 = LoadGpButtonFrame(gpFile, oneSprited ? baseSprite : baseSprite + 4);

            if (spC1 == null) spC1 = spL;
            if (spC2 == null) spC2 = spC1;
            if (spC3 == null) spC3 = spC1;
            if (spC1 == null)
            {
                UnityEngine.Object.DestroyImmediate(visual);
                return null;
            }

            float leftW = spL != null ? spL.rect.width : 0f;
            float rightW = spR != null ? spR.rect.width : 0f;
            // DrawHeaderEx2 clamps only the clipping window; the center tiles
            // themselves start at x0 and are clipped by [x0+L, x0+Lx-R].
            // DrawForms.cpp::DrawHeaderEx2 clips center tiles with inclusive
            // coordinates [x0+frWidthL, x0+Lx-frWidthR].  In pixel terms that
            // region is Lx-left-right+1 wide.  The old Unity mask omitted the
            // final column and exposed a vertical seam at the center/right join.
            float centerW = Mathf.Max(0f, width - leftW - rightW + 1f);

            // V396A7R7: RectMask2D rasterizes the left edge as a half-open UI
            // rectangle, while the original IntersectWindows works on inclusive
            // framebuffer pixel coordinates.  At an exact GP join (L -> center)
            // that produced a one-pixel uncovered column even though all source
            // x/width values were correct.  Keep the ORIGINAL logical clip, but
            // give the Unity mask a one-pixel guard band under the edge sprites.
            // EdgeL/EdgeR are drawn later and remain authoritative, so this does
            // not change the visible geometry; it only prevents the mask boundary
            // from exposing the background between adjacent GP pieces.
            const float clipGuard = 1f;
            float clipStartX = Mathf.Max(0f, leftW - clipGuard);
            float logicalEndInclusive = width - rightW;
            float clipEndExclusive = Mathf.Min(width, logicalEndInclusive + 1f + clipGuard);
            float guardedCenterW = Mathf.Max(0f, clipEndExclusive - clipStartX);

            var center = new GameObject("CenterMask", typeof(RectTransform), typeof(RectMask2D));
            center.transform.SetParent(visual.transform, false);
            RectTransform crt = (RectTransform)center.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = new Vector2(clipStartX, 0f);
            crt.sizeDelta = new Vector2(guardedCenterW, Mathf.Max(1f, height));

            Sprite[] centers = { spC1, spC2, spC3 };
            float sourceX = 0f;
            int tile = 0;
            while (sourceX < width && tile < 300)
            {
                // Original DrawHeaderEx2 uses switch(i % 3), where i is the
                // current PIXEL x offset, not the tile ordinal.
                int phase = ((int)sourceX) % 3;
                if (phase < 0) phase += 3;
                Sprite sp = centers[phase] ?? spC1;
                if (sp == null) break;
                float tw = Mathf.Max(1f, sp.rect.width);
                CreateListDeskSpritePieceV395J(center.transform, "Tile_" + tile, sp, sourceX - clipStartX, 0f, tw, sp.rect.height);
                sourceX += tw;
                tile++;
            }

            if (spL != null)
                CreateListDeskSpritePieceV395J(visual.transform, "EdgeL", spL, 0f, 0f, spL.rect.width, spL.rect.height);
            if (spR != null)
                CreateListDeskSpritePieceV395J(visual.transform, "EdgeR", spR,
                    Mathf.Max(0f, width - spR.rect.width), 0f, spR.rect.width, spR.rect.height);

            return visual;
        }

        private static void CreateListDeskSpritePieceV395J(
            Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
        {
            if (parent == null || sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }

        private sealed class ListDeskVitButtonHoverV395J : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public GameObject NormalVisual;
            public GameObject HoverVisual;
            public TextMeshProUGUI Label;
            public Color32 NormalText;
            public Color32 HoverText;
            public bool Enabled = true;

            public void OnPointerEnter(PointerEventData e)
            {
                if (!Enabled) return;
                if (NormalVisual != null && HoverVisual != null) NormalVisual.SetActive(false);
                if (HoverVisual != null) HoverVisual.SetActive(true);
                if (Label != null) Label.color = HoverText;
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (HoverVisual != null) HoverVisual.SetActive(false);
                if (NormalVisual != null) NormalVisual.SetActive(true);
                if (Label != null) Label.color = NormalText;
            }
        }

        /// <summary>
        /// V395H: renders one runtime ListDesk element from the UiVitButton that
        /// the unified XML parser already produced for ListDesk/Element.
        /// All layout/font/GP/state values are supplied by that parsed template
        /// (with raw-XML state-slot fallbacks prepared by Menu14ActionStateRuntime).
        /// No profile-specific visual constants live here.
        /// </summary>
        public static GameObject CreateListDeskElementFromParsedVitButtonV395H(
            UiVitButton template,
            RectTransform parent,
            string message,
            int state,
            bool enabled,
            Action onClick,
            float x,
            float y,
            float width,
            float height,
            int passiveSprite,
            int overSprite,
            int spriteDx,
            string passiveFontName,
            string overFontName,
            int fontDx,
            int fontDy,
            string align)
        {
            if (template == null || parent == null) return null;

            string gpFile = template.GP_File ?? string.Empty;
            Sprite normalSp = LoadGpButtonFrame(gpFile, passiveSprite);
            Sprite hoverSp = LoadGpButtonFrame(gpFile, overSprite) ?? normalSp;

            var go = new GameObject($"ListDeskVitButton_{SafeName(message)}_S{state}", typeof(RectTransform), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));

            var bgGo = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(go.transform, false);
            RectTransform brt = (RectTransform)bgGo.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(spriteDx, 0f);
            brt.offsetMax = new Vector2(spriteDx, 0f);

            Image bg = bgGo.GetComponent<Image>();
            bg.sprite = normalSp;
            bg.color = normalSp != null ? Color.white : Color.clear;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = enabled;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            tr.anchoredPosition += new Vector2(fontDx, -fontDy);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.richText = false;
            tmp.raycastTarget = false;
            tmp.text = message ?? string.Empty;
            tmp.fontSize = ResolveC2FontSize(passiveFontName, 14f);
            tmp.color = ResolveC2FontColor(passiveFontName, OptionsTextStyleConfig.Button.NormalColor);

            string a = (align ?? string.Empty).Trim();
            if (a.Equals("Right", StringComparison.OrdinalIgnoreCase))
                tmp.alignment = TextAlignmentOptions.MidlineRight;
            else if (a.Equals("Center", StringComparison.OrdinalIgnoreCase))
                tmp.alignment = TextAlignmentOptions.Midline;
            else
                tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var hover = go.AddComponent<ListDeskVitButtonHoverV395H>();
            hover.Background = bg;
            hover.Label = tmp;
            hover.NormalSprite = normalSp;
            hover.HoverSprite = hoverSp;
            hover.NormalText = ResolveC2FontColor(passiveFontName, tmp.color);
            hover.HoverText = ResolveC2FontColor(overFontName, hover.NormalText);
            hover.Enabled = enabled;

            return go;
        }

        private sealed class ListDeskVitButtonHoverV395H : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Background;
            public TextMeshProUGUI Label;
            public Sprite NormalSprite;
            public Sprite HoverSprite;
            public Color32 NormalText;
            public Color32 HoverText;
            public bool Enabled = true;

            public void OnPointerEnter(PointerEventData e)
            {
                if (!Enabled) return;
                if (Background != null)
                {
                    Background.sprite = HoverSprite;
                    Background.color = HoverSprite != null ? Color.white : Color.clear;
                }
                if (Label != null) Label.color = HoverText;
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (Background != null)
                {
                    Background.sprite = NormalSprite;
                    Background.color = NormalSprite != null ? Color.white : Color.clear;
                }
                if (Label != null) Label.color = NormalText;
            }
        }

        /// <summary>
        /// Рисует VitButton как тайловую линию по логике DrawHeaderEx2 из оригинала.
        /// Спрайты: CSpr (L), CSpr+1 (R), CSpr+2/+3/+4 (Center 1/2/3)
        /// </summary>
        private static void CreateVitButtonTiled(UiVitButton vb, RectTransform parent)
        {
            if (vb == null || parent == null) return;

            // ═══════════════════════════════════════════════════════════
            // 1. Путь к ресурсам (lowercase для совместимости)
            // ═══════════════════════════════════════════════════════════
            string resPath = (vb.GP_File ?? "")
                .Replace("\\", "_")
                .Replace("/", "_")
                .ToLowerInvariant() + "_frames";  // ← LOWERCASE!

            int baseSpr = vb.SpritePassive;

            if (EnableVitLogs) Debug.Log($"[VitButtonTiled] GP={vb.GP_File}, resPath={resPath}, baseSpr={baseSpr}, " +
                      $"pos=({vb.X},{vb.Y}), size=({vb.Width}x{vb.Height}), OneSprited={vb.OneSprited}");

            // ═══════════════════════════════════════════════════════════
            // 2. Загружаем спрайты согласно логике DrawHeaderEx2
            // ═══════════════════════════════════════════════════════════
            Sprite spL, spR, spC1, spC2, spC3;

            if (vb.OneSprited)
            {
                // OneSprited: все центры = baseSpr, краёв нет
                spL = null;
                spR = null;
                spC1 = spC2 = spC3 = LoadSpriteFromResources(resPath, $"frame_{baseSpr:0000}");
            }
            else
            {
                // Стандартная логика: L=baseSpr, R=baseSpr+1, C1/C2/C3=baseSpr+2/3/4
                spL = LoadSpriteFromResources(resPath, $"frame_{baseSpr:0000}");
                spR = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 1:0000}");
                spC1 = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 2:0000}");
                spC2 = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 3:0000}");
                spC3 = LoadSpriteFromResources(resPath, $"frame_{baseSpr + 4:0000}");
            }

            if (EnableVitLogs) Debug.Log($"[VitButtonTiled] Sprites loaded: L={spL != null}, R={spR != null}, " +
                      $"C1={spC1 != null}, C2={spC2 != null}, C3={spC3 != null}");

            // Fallback
            if (spC1 == null) spC1 = spL;
            if (spC2 == null) spC2 = spC1;
            if (spC3 == null) spC3 = spC1;

            if (spC1 == null)
            {
                Debug.LogError($"[VitButtonTiled] No sprites found for {resPath}!");
                return;
            }

            Sprite[] centerSprites = { spC1, spC2, spC3 };

            // Размеры
            float widthL = spL != null ? spL.rect.width : 0f;
            float widthR = spR != null ? spR.rect.width : 0f;
            float centerTileW = spC1.rect.width;
            float height = vb.Height > 0 ? vb.Height : spC1.rect.height;
            float totalWidth = vb.Width > 0 ? vb.Width : 200f;

            // ═══════════════════════════════════════════════════════════
            // 3. Корневой контейнер
            // ═══════════════════════════════════════════════════════════
            var root = new GameObject($"VitLine_{SafeName(vb.Name)}", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0, 1);
            rootRt.pivot = new Vector2(0, 1);
            rootRt.anchoredPosition = new Vector2(vb.X, -vb.Y);
            rootRt.sizeDelta = new Vector2(totalWidth, height);

            // ═══════════════════════════════════════════════════════════
            // 4. Центр с МАСКОЙ (аналог IntersectWindows)
            // ═══════════════════════════════════════════════════════════
            float centerStartX = widthL;
            float centerEndX = totalWidth - widthR;
            // V396A7R5 full-audit correction: DrawHeaderEx2 uses an inclusive
            // clip [x0+frWidthL, x0+Lx-frWidthR]. Unity RectTransform widths are
            // counts, therefore the corresponding width is end-start+1.
            float centerWidth = Mathf.Max(0f, centerEndX - centerStartX + 1f);

            // V396A7R7: one-pixel UI-mask guard band.  The GP edge sprites are
            // rendered after the center and cover this guard, matching the
            // inclusive framebuffer join without altering source coordinates.
            const float genericClipGuard = 1f;
            float genericClipStart = Mathf.Max(0f, centerStartX - genericClipGuard);
            float genericClipEndExclusive = Mathf.Min(totalWidth, centerEndX + 1f + genericClipGuard);
            float genericGuardedWidth = Mathf.Max(0f, genericClipEndExclusive - genericClipStart);

            var centerContainer = new GameObject("CenterMask", typeof(RectTransform), typeof(RectMask2D));
            centerContainer.transform.SetParent(root.transform, false);

            var centerRt = (RectTransform)centerContainer.transform;
            centerRt.anchorMin = centerRt.anchorMax = new Vector2(0, 1);
            centerRt.pivot = new Vector2(0, 1);
            centerRt.anchoredPosition = new Vector2(genericClipStart, 0);
            centerRt.sizeDelta = new Vector2(genericGuardedWidth, height);

            // ═══════════════════════════════════════════════════════════
            // 5. Тайлим центр ТОЧНО как DrawHeaderEx2: switch(i % 3),
            //    где i — исходная пиксельная X-координата, а не номер тайла.
            //    Рисование начинается от x0 и затем обрезается clip-окном.
            // ═══════════════════════════════════════════════════════════
            float sourceX = 0f;
            int tileIndex = 0;
            int maxTiles = 300;

            while (sourceX < totalWidth && tileIndex < maxTiles)
            {
                int phase = ((int)sourceX) % 3;
                if (phase < 0) phase += 3;
                Sprite tileSp = centerSprites[phase] ?? spC1;
                if (tileSp == null) break;
                float tileW = Mathf.Max(1f, tileSp.rect.width);

                var tileGO = new GameObject($"Tile_{tileIndex}", typeof(RectTransform), typeof(Image));
                tileGO.transform.SetParent(centerContainer.transform, false);

                var tileRt = (RectTransform)tileGO.transform;
                tileRt.anchorMin = tileRt.anchorMax = new Vector2(0, 1);
                tileRt.pivot = new Vector2(0, 1);
                tileRt.anchoredPosition = new Vector2(sourceX - genericClipStart, 0);
                tileRt.sizeDelta = new Vector2(tileW, tileSp.rect.height);

                var tileImg = tileGO.GetComponent<Image>();
                tileImg.sprite = tileSp;
                tileImg.type = Image.Type.Simple;
                tileImg.raycastTarget = false;
                tileImg.preserveAspect = false;

                sourceX += tileW;
                tileIndex++;
            }

            if (EnableVitLogs) Debug.Log($"[VitButtonTiled] Created {tileIndex} center tiles");

            // ═══════════════════════════════════════════════════════════
            // 6. Левый край ПОВЕРХ
            // ═══════════════════════════════════════════════════════════
            if (spL != null && widthL > 0)
            {
                var leftGO = new GameObject("EdgeL", typeof(RectTransform), typeof(Image));
                leftGO.transform.SetParent(root.transform, false);

                var leftRt = (RectTransform)leftGO.transform;
                leftRt.anchorMin = leftRt.anchorMax = new Vector2(0, 1);
                leftRt.pivot = new Vector2(0, 1);
                leftRt.anchoredPosition = new Vector2(0, 0);
                leftRt.sizeDelta = new Vector2(widthL, spL.rect.height);

                var leftImg = leftGO.GetComponent<Image>();
                leftImg.sprite = spL;
                leftImg.type = Image.Type.Simple;
                leftImg.raycastTarget = false;

                leftGO.transform.SetAsLastSibling();
            }

            // ═══════════════════════════════════════════════════════════
            // 7. Правый край ПОВЕРХ
            // ═══════════════════════════════════════════════════════════
            if (spR != null && widthR > 0)
            {
                var rightGO = new GameObject("EdgeR", typeof(RectTransform), typeof(Image));
                rightGO.transform.SetParent(root.transform, false);

                var rightRt = (RectTransform)rightGO.transform;
                rightRt.anchorMin = rightRt.anchorMax = new Vector2(0, 1);
                rightRt.pivot = new Vector2(0, 1);
                rightRt.anchoredPosition = new Vector2(totalWidth - widthR, 0);
                rightRt.sizeDelta = new Vector2(widthR, spR.rect.height);

                var rightImg = rightGO.GetComponent<Image>();
                rightImg.sprite = spR;
                rightImg.type = Image.Type.Simple;
                rightImg.raycastTarget = false;

                rightGO.transform.SetAsLastSibling();
            }

            if (EnableVitLogs) Debug.Log($"[VitButtonTiled] Complete: L={widthL > 0}, R={widthR > 0}, tiles={tileIndex}");
        }


        private sealed class VitButtonHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Bg;
            public Sprite Normal;
            public Sprite Hover;

            public void OnPointerEnter(PointerEventData e)
            {
                if (Bg != null && Hover != null) Bg.sprite = Hover;
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (Bg != null && Normal != null) Bg.sprite = Normal;
            }
        }

        private static void CreateGPTextButton(RectTransform parent, UiGPTextButton btn, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            var go = new GameObject($"GPTextButton_{SafeName(btn.MessageKey)}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(btn.X, -btn.Y);
            rt.sizeDelta = new Vector2(btn.Width, btn.Height);

            var bg = go.AddComponent<Image>();
            bg.raycastTarget = true;

            Sprite normalSp = LoadGpButtonFrame(btn.FileID, btn.Sprite1);
            Sprite hoverSp = LoadGpButtonFrame(btn.FileID, btn.Sprite);
            // Cossacks II Dialogs.cpp::GP_TextButton_OnDraw does NOT use Sprite1+1
            // for disabled. It draws the same passive Sprite1 with diffuse * 230/255.
            Sprite disabledSp = normalSp;

            // Original GP_TextButton uses Sprite1=-1 to mean "no passive
            // overlay": the parent GPPicture remains visible and Sprite is only
            // the hover state. A null Unity Image must therefore be transparent,
            // never the default solid white rectangle.
            bg.sprite = normalSp;
            bg.color = normalSp != null ? Color.white : Color.clear;
            bg.type = Image.Type.Simple;

            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = btn.Enabled;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);

            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            string textResolved = loc?.Resolve(btn.MessageKey) ?? btn.MessageKey;
            tmp.text = textResolved;
            ApplyTextStyle(tmp, UiTextStyle.Button, opt);

            // Exact original GP_TextButton text state comes from this XML node's
            // ActiveFont / PassiveFont / DisabledFont, not a global Unity button style.
            string activeFontName = GetStringMember(btn, "ActiveFont");
            string passiveFontName = GetStringMember(btn, "PassiveFont");
            string disabledFontName = GetStringMember(btn, "DisabledFont");
            Color32 passiveText = ResolveC2FontColor(passiveFontName, OptionsTextStyleConfig.Button.NormalColor);
            Color32 activeText = ResolveC2FontColor(activeFontName, OptionsTextStyleConfig.Button.HoverColor);
            Color32 disabledText = ResolveC2FontColor(disabledFontName, OptionsTextStyleConfig.Button.DisabledColor);
            tmp.color = passiveText;
            tmp.fontSize = ResolveC2FontSize(passiveFontName, tmp.fontSize);

            if (textResolved?.Trim() is "ПРИНЯТЬ" or "ОТМЕНА")
            {
                tmp.fontStyle = FontStyles.Normal;
                tmp.fontWeight = FontWeight.Regular;
                var m = new Material(tmp.fontMaterial);
                m.SetFloat(ShaderUtilities.ID_FaceDilate, -0.15f);
                m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                tmp.fontMaterial = m;
            }

            tmp.alignment = btn.Center ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
            tmp.rectTransform.anchoredPosition += new Vector2(btn.FontDx, -btn.FontDy);

            var swap = go.AddComponent<GpButtonHoverSwap>();
            swap.Bg = bg;
            swap.Label = tmp;
            swap.Button = button;
            swap.NormalBg = normalSp;
            swap.HoverBg = hoverSp;
            swap.DisabledBg = disabledSp;
            swap.NormalText = passiveText;
            swap.HoverText = activeText;
            swap.DisabledText = disabledText;
            swap.NormalBgTint = new Color32(255, 255, 255, 255);
            swap.HoverBgTint = new Color32(255, 255, 255, 255);
            swap.DisabledBgTint = new Color32(230, 230, 230, 255);

            button.onClick.AddListener(() =>
            {
                bool hasNavBack =
                    btn.Actions != null && btn.Actions.Exists(x =>
                        x != null && (x.Name == "cva_MM_MultiBack" || x.Name == "cva_MM_Back"));

                foreach (var a in btn.Actions)
                {
                    if (a == null) continue;

                    // ВАЖНО: на кнопке Back в Multi есть и MultiBack и Close — Close надо скипнуть
                    if (hasNavBack && a.Name == "cva_MM_Close")
                        continue;

                    try { sink?.OnAction(btn.MessageKey, a); }
                    catch (Exception e) { Debug.LogError($"[BaseRenderer] Action error: {e}"); }
                }
            });

            // EW2_CampaignStats.XML assigns VK_ESCAPE to its cva_MM_Cancel
            // GP_TextButton.  OptionsRenderer previously rendered the button but
            // had no keyboard-hotkey pass, so preserve the original escape route
            // for any XML cancel button without inventing a stats-only navigation path.
            bool cancelAction = btn.Actions != null && btn.Actions.Exists(x =>
                x != null && string.Equals(x.Name, "cva_MM_Cancel", StringComparison.OrdinalIgnoreCase));
            if (cancelAction)
            {
                var esc = go.AddComponent<EscapeInvokesButton>();
                esc.Target = button;
            }
        }

        private sealed class EscapeInvokesButton : MonoBehaviour
        {
            public Button Target;

            private void Update()
            {
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                pressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
                pressed = Input.GetKeyDown(KeyCode.Escape);
#endif
                if (pressed && Target != null && Target.interactable && Target.gameObject.activeInHierarchy)
                    Target.onClick.Invoke();
            }
        }

        private static string GetStringMember(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return string.Empty;
            try
            {
                var t = obj.GetType();
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (p != null)
                {
                    object v = p.GetValue(obj, null);
                    return v != null ? v.ToString() : string.Empty;
                }
                var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (f != null)
                {
                    object v = f.GetValue(obj);
                    return v != null ? v.ToString() : string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }

        // InitFonts.h, Cossacks II 1.1/final 1.4 palette.
        private static Color32 ResolveC2FontColor(string fontName, Color32 fallback)
        {
            string n = (fontName ?? string.Empty).Trim();
            if (n.IndexOf("Black", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0x2E, 0x23, 0x17, 0xFF);
            if (n.IndexOf("Red", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0x8A, 0x10, 0x00, 0xFF);
            if (n.IndexOf("Yellow", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0xD4, 0xC1, 0x9C, 0xFF);
            if (n.IndexOf("White", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0xFF, 0xF7, 0xEF, 0xFF);
            if (n.IndexOf("Gray", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0x6D, 0x68, 0x62, 0xFF);
            if (n.IndexOf("Disable", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0x66, 0x5F, 0x57, 0xC0);
            if (n.IndexOf("Orange", StringComparison.OrdinalIgnoreCase) >= 0) return new Color32(0x6A, 0x30, 0x00, 0xFF);
            return fallback;
        }

        private static float ResolveC2FontSize(string fontName, float fallback)
        {
            string n = (fontName ?? string.Empty).Trim();
            // BlackFont/RedFont/GrayFont and MenuText* are 14-pixel menu fonts in this UI.
            if (n.StartsWith("Small", StringComparison.OrdinalIgnoreCase)) return 10f;
            if (n.IndexOf("Font", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.StartsWith("MenuText", StringComparison.OrdinalIgnoreCase)) return 14f;
            return fallback;
        }

        private static Sprite LoadButtonFrame(int id)
        {
            if (id < 0) return null;
            var tex = Resources.Load<Texture2D>($"Buttons/frame_{id:0000}");
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
            PrepareUiSpriteSamplingV396A7R4(sp);
            return sp;
        }

        private static Sprite LoadGpButtonFrame(string fileId, int id)
        {
            if (id < 0) return null;

            // Source first: GP_TextButton explicitly names the bank in FileID.
            // Do not silently substitute the legacy generic Buttons bank when
            // the original XML points at another GP file.
            if (!string.IsNullOrWhiteSpace(fileId))
            {
                string resPath = fileId.Replace("\\", "_").Replace("/", "_").ToUpperInvariant() + "_frames";
                Sprite exact = LoadSpriteFromResources(resPath, $"frame_{id:0000}");
                if (exact != null) return exact;

                exact = Menu14ActionStateRuntime.TryLoadGpSpriteForRenderer(fileId, id, true);
                if (exact != null) return exact;
            }

            // Compatibility only for old menu assets that never carried FileID.
            return LoadButtonFrame(id);
        }

        // ===================== HELPER CLASSES =====================

        private sealed class ComboBoxController : MonoBehaviour, IPointerClickHandler
        {
            public GameObject Panel;
            public GameObject Blocker;
            public Image BoxImage;
            public Sprite SpriteClosed;
            public Sprite SpriteOpen;

            public System.Action<int, string> OnSelected;

            private bool _isOpen;

            public void OnPointerClick(PointerEventData e)
            {
                if (Panel == null) return;
                _isOpen = !_isOpen;
                if (_isOpen) OpenPopup();
                else ClosePopup();
            }

            private void OpenPopup()
            {
                if (Blocker != null)
                {
                    Blocker.SetActive(true);
                    Blocker.transform.SetAsLastSibling();
                }

                if (Panel != null)
                {
                    Panel.SetActive(true);
                    Panel.transform.SetAsLastSibling();
                }

                if (BoxImage != null) BoxImage.sprite = SpriteOpen;
                _isOpen = true;
            }

            public void ClosePopup()
            {
                _isOpen = false;
                if (Panel != null) Panel.SetActive(false);
                if (Blocker != null) Blocker.SetActive(false);
                if (BoxImage != null) BoxImage.sprite = SpriteClosed;
            }

            private void OnDisable() => ClosePopup();
        }

        private sealed class RowHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Bg;
            public Sprite NormalSprite;
            public Sprite HoverSprite;

            public void OnPointerEnter(PointerEventData e)
            {
                if (Bg == null) return;
                Bg.sprite = HoverSprite != null ? HoverSprite : NormalSprite;
                Bg.SetVerticesDirty();
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (Bg == null) return;
                Bg.sprite = NormalSprite;
                Bg.SetVerticesDirty();
            }
        }

        private sealed class GpButtonHoverSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image Bg;
            public TextMeshProUGUI Label;
            public Button Button;
            public Sprite NormalBg, HoverBg, DisabledBg;
            public Color32 NormalText, HoverText, DisabledText;
            public Color32 NormalBgTint, HoverBgTint, DisabledBgTint;

            void OnEnable() => ApplyCurrent();

            void ApplyCurrent()
            {
                bool ok = Button == null || Button.interactable;
                if (Bg)
                {
                    Bg.sprite = ok ? NormalBg : DisabledBg;
                    Bg.color = Bg.sprite != null ? (Color)(ok ? NormalBgTint : DisabledBgTint) : Color.clear;
                }
                if (Label) Label.color = ok ? NormalText : DisabledText;
            }

            public void OnPointerEnter(PointerEventData e)
            {
                if (Button != null && !Button.interactable) return;
                if (Bg)
                {
                    Bg.sprite = HoverBg;
                    Bg.color = HoverBg != null ? (Color)HoverBgTint : Color.clear;
                }
                if (Label) Label.color = HoverText;
            }

            public void OnPointerExit(PointerEventData e) => ApplyCurrent();
        }

        private sealed class SliderController : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
        {
            private RectTransform _trackRT;
            private RectTransform _thumbRT;
            private Image _thumbImage;
            private int _max;
            private int _currentPos;
            private float _thumbY;
            private float _trackOffsetX;
            private float _lineWidth;
            private float _thumbWidth;
            private bool _isDragging;
            private Canvas _canvas;
            private CanvasRenderer _thumbCanvasRenderer;
            private float _lamMinX;
            private float _grabOffsetPx;

            public void Initialize(
                RectTransform trackRT,
                RectTransform thumbRT,
                Image thumbImage,
                int max,
                int initialPos,
                float thumbY,
                float trackOffsetX,
                float lineWidth,
                float thumbWidth,
                float lamMinX)
            {
                _trackRT = trackRT;
                _thumbRT = thumbRT;
                _thumbImage = thumbImage;
                _max = max;
                _lamMinX = lamMinX;
                _currentPos = initialPos;
                _thumbY = thumbY;
                _trackOffsetX = trackOffsetX;
                _lineWidth = lineWidth;
                _thumbWidth = thumbWidth;
                _isDragging = false;

                _canvas = GetComponentInParent<Canvas>();
                _thumbCanvasRenderer = thumbRT != null ? thumbRT.GetComponent<CanvasRenderer>() : null;
            }

            private void Start()
            {
                StartCoroutine(ForceRefreshNextFrame());
            }

            private IEnumerator ForceRefreshNextFrame()
            {
                yield return null;
                ForceRefresh();
            }

            public void OnPointerDown(PointerEventData e)
            {
                _isDragging = true;

                if (_trackRT != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _trackRT, e.position, e.pressEventCamera, out var lp))
                {
                    float curLeft = _thumbRT.anchoredPosition.x - _trackOffsetX;

                    if (lp.x >= curLeft && lp.x <= curLeft + _thumbWidth)
                        _grabOffsetPx = lp.x - curLeft;
                    else
                        _grabOffsetPx = _thumbWidth * 0.5f;

                    _grabOffsetPx = Mathf.Clamp(_grabOffsetPx, 0f, _thumbWidth);
                }
                else
                {
                    _grabOffsetPx = _thumbWidth * 0.5f;
                }

                HandleInput(e);
            }

            public void OnBeginDrag(PointerEventData e) => _isDragging = true;

            public void OnDrag(PointerEventData e)
            {
                if (_isDragging) HandleInput(e);
            }

            public void OnEndDrag(PointerEventData e) => _isDragging = false;

            private void HandleInput(PointerEventData e)
            {
                if (_trackRT == null || _max <= 0) return;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _trackRT, e.position, e.pressEventCamera, out var localPoint))
                    return;

                float minPx = _lamMinX - _trackOffsetX;
                float maxPx = Mathf.Max(minPx, _lineWidth - _thumbWidth);

                float leftPx = localPoint.x - _grabOffsetPx;
                leftPx = Mathf.Clamp(leftPx, minPx, maxPx);

                float newX = _trackOffsetX + leftPx;
                _thumbRT.anchoredPosition = new Vector2(newX, _thumbY);

                float denom = Mathf.Max(1e-4f, maxPx - minPx);
                float t = (leftPx - minPx) / denom;
                _currentPos = Mathf.Clamp(Mathf.RoundToInt(t * _max), 0, _max);

                ForceRefresh();
            }

            private void ForceRefresh()
            {
                if (_thumbImage != null)
                {
                    _thumbImage.SetVerticesDirty();
                    _thumbImage.SetMaterialDirty();
                }

                if (_thumbCanvasRenderer != null)
                    _thumbCanvasRenderer.SetAlpha(1f);

                if (_thumbRT != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_thumbRT);

                if (_canvas != null)
                    Canvas.ForceUpdateCanvases();
            }
        }

        private sealed class CheckBoxDebugToggle : MonoBehaviour
        {
            public int Index;
            public Image Image;
            public Sprite SpriteOff;
            public Sprite SpriteOn;
            public bool State;

            public void Toggle()
            {
                State = !State;
                if (Image)
                    Image.sprite = State ? (SpriteOn ?? SpriteOff) : (SpriteOff ?? SpriteOn);
            }

            private void Awake()
            {
                var b = GetComponent<Button>();
                if (b) b.onClick.AddListener(Toggle);
            }
        }
    }
}