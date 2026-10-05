using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;
using Cossacks2Bridge.UnityAdapters.Profiles;
using Cossacks2Bridge.UnityAdapters.Renderers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TemnyLessCodec;
using TemnyLessViewer;

namespace Cossacks2Bridge.UnityAdapters.BigMap
{
    /// <summary>
    /// Port of the non-battle layer of ProcessBigMap(0).
    /// The layout constants, map rectangle, 3x3 Europe pieces, pages, resource
    /// economy, sector data and final 1.4 nine-country contract follow the audited
    /// 1.0/1.1 implementation while data/resources are taken from the 1.4 set.
    /// Battles and campaign AI are intentionally not executed yet.
    /// </summary>
    public sealed class C2BigMapRenderer14
    {
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        // VISUAL1: use the same direct GN16/CP1251 bitmap-font bank path that is already
        // proven in Campaign/Help. Do NOT request font glyphs through the ordinary BigMap
        // Melinoja per-frame session cache (that was the CHROME2A regression).
        private readonly Dictionary<string, C2DirectSpriteBank> _fontBanks = new Dictionary<string, C2DirectSpriteBank>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sprite> _glyphCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private CoreFileSystem _fs;
        private LocDb _loc;
        private BaseUiRenderer.RenderOptions _opt;
        private RectTransform _root;
        private C2BigMapData14.Data _data;
        private C2ProfileRuntime14.ProfileRecord _profile;
        private RectTransform _mapContent;
        private GameObject _mapViewportRoot;
        private Image _mapBorder;
        private readonly Dictionary<int, Image> _sectorOverlays = new Dictionary<int, Image>();
        private readonly Dictionary<int, Image> _sectorCityMarkers = new Dictionary<int, Image>();
        private readonly Dictionary<int, Image> _sectorFortMarkers = new Dictionary<int, Image>();
        private const string Final14TurnMapSha256 = "5820b536f1804cf424a2aefcebae57f7fa92a0745b4ab847b159f61178a03aba";
        private const string Final14BmElements5G16Sha256 = "687ba8a9faf8af301ab0f65bf1111cf458d1b8cbde5abf9b15b1ce9678421303";
        // R2: final-1.4 diplomacy lva_Flags.g17 expands the old 7-sprite cache to
        // 10 sprites (frame 0 + country frames 1..9).  Never let the 1.1-era
        // 7-frame cache service the 9-country final-1.4 diplomacy table.
        private const string Final14DiplFlagsG16Sha256 = "164a8d1a93ebaa280bb52ba9c3b26f7425eec369e56a50269e45965ee36b6437";
        private GameObject _sectorMenuRoot;
        // V396A8.6 Diplomacy visual/data shell.  Final 1.4 expands the original
        // six-country 1.1 relation panel to a source-proven 9x9 layout.
        private GameObject _diplomacyRoot;
        private GpTextHandle _diplCountryNameGp;
        private Image _diplCountryFlag;
        private Image _diplCountryPicture;
        private Image _diplStateBack;
        private RectTransform _diplHorBarDesk;
        private RectTransform _diplVerBarDesk;
        private readonly Image[] _diplRowFlags = new Image[C2Bfe14ContractV396A.CountryCount];
        private readonly Image[] _diplColFlags = new Image[C2Bfe14ContractV396A.CountryCount];
        private readonly GpTextHandle[,] _diplStateText = new GpTextHandle[C2Bfe14ContractV396A.CountryCount,C2Bfe14ContractV396A.CountryCount];
        private readonly Button[] _diplActionButtons = new Button[6];
        private Image _diplMapFilterFrame;
        private readonly Button[] _diplMapFilterButtons = new Button[5];
        private int _diplMapPressed2 = 2; // ROOT_SCENARIO.m_Scenario.m_inMapButtonPressed default in 1.1/final data
        private string _diplFlagsCanonicalPath = string.Empty;
        private bool _diplFlagsResolved;
        private bool _diplFlagsAuditLogged;
        private GpTextHandle _countryNameGp;
        private GpTextHandle _sectorTitleGp;
        private GpTextHandle _populationGp;
        private GpTextHandle _recruitsGp;
        private GpTextHandle _defenceGp;
        private Image _sectorPreview;
        private Image _sectorMiniBorder;
        private Image _sectorIncomePicture;
        private Image _sectorDelimiter;
        private readonly GpTextHandle[] _sectorIncomeGpText = new GpTextHandle[7];
        private Image _nationFlag;
        private readonly TextMeshProUGUI[] _resourceText = new TextMeshProUGUI[7];
        private readonly GpTextHandle[] _resourceGpText = new GpTextHandle[7];
        private Image _headerNationFlag;
        private GpTextHandle _headerGpText;
        private readonly Image[] _pageButtonImages = new Image[5];
        private readonly TextMeshProUGUI[] _pageButtonLabels = new TextMeshProUGUI[5];
        private readonly GpTextHandle[] _pageGpText = new GpTextHandle[5];
        private Button _defenceButton;
        private Button _diversionButton;
        private int _selectedSector;
        private int _activePage;
        private C2BigMapFinal14TextResolverV396A8_4 _final14Text;
        private readonly C2CampaignModalRenderer14 _helpRenderer = new C2CampaignModalRenderer14();

        public void Render(CoreFileSystem fs, BaseUiRenderer.RenderOptions opt, LocDb loc)
        {
            _fs = fs;
            _opt = opt;
            _loc = loc;
            C2ProfileRuntime14.EnsureLoaded();
            _profile = C2ProfileRuntime14.Current;
            if (_profile == null)
            {
                Debug.LogWarning("[C2:BIGMAP V395] blocked: no CurPlayer");
                return;
            }

            _data = C2BigMapData14.Load(fs);
            _final14Text = new C2BigMapFinal14TextResolverV396A8_4(_data != null ? _data.SourceDataRoot : string.Empty, _loc, _data != null ? _data.Sectors : null);
            C2BigMapData14.EnsureCampaignInitialized(_profile, _data);
            _selectedSector = Mathf.Clamp(_profile.m_iCurSecId, 0, Math.Max(0, _data.Sectors.Count - 1));
            _activePage = Mathf.Clamp(_profile.m_iCurMenuId, 0, C2Bfe14ContractV396A.MainPageCount - 1);

            _root = CreateCanvas("C2_BigMapCanvas", opt);
            BuildBackground();
            BuildCampaignHeader();
            BuildResourceHeader();
            BuildMap();
            BuildRightInfo();
            BuildDiplomacyVisualV396A8_6();
            BuildPages();
            BuildBottomButtons();
            BigMapHelpHotkeyV396A5 hotkey = _root.gameObject.AddComponent<BigMapHelpHotkeyV396A5>();
            hotkey.Initialize(this);
            RefreshAll();
            AuditFinal14NameProjectionV396A8_4();

            Debug.Log($"[C2:BIGMAP V396A] ProcessBigMap shell ready profile='{_profile.m_chName}' campaignNation={_profile.campaignNation} sectors={_data.Sectors.Count} map={_data.MapWidth}x{_data.MapHeight} battleRuntime=disabled aiRuntime=disabled dataStatus={_data.Audit?.Status ?? "UNKNOWN"}");
            Debug.Log($"[C2:BIGMAP V396A6] g16Orientation=flipY flagFrame=countryId+1 flagCountries=9 secondMiniAtlasStart=24 pixelStage=1024x768 pointFilter=YES screen={Screen.width}x{Screen.height}");
            Debug.Log($"[C2:BIGMAP VISUAL3 V396A8_3R1] projection=1024x768_constant_pixel_integer_origin background=FINAL14_lva_Turn_Map_SHA256 sectors=lva_Sectors:{_sectorOverlays.Count}:ownerTint=NATCOLOR+ALPHA_SECT selected=SELECTED_SECT_COLOR+NATCOLOR markers=CSectStatData_SetSityType_dynamic resourceOrder=0,3,2,1,4,5,6 sectorMenu=FINAL14_SetMenuData population=0_2_reset_then_plus1 defence=DefLvl_ENUM income=SECT_INCOME+GetGoldForRess sabotageDisplay=m_iSabotageID_digits input=UNCHANGED_CHROME1 gameplayAI=UNTOUCHED");
            Debug.Log($"[C2:BIGMAP MAINMAP V396A8_4] names=GetTextByID_final14_noHardcode textRoot='{_final14Text?.DataRoot ?? string.Empty}' filesScanned={_final14Text?.FilesScanned ?? 0} supplementalMatches={_final14Text?.SupplementMatches ?? 0} ambiguous={_final14Text?.AmbiguousCount ?? 0} input=UNCHANGED_CHROME1 gameplayAI=UNTOUCHED");
            Debug.Log("[C2:BIGMAP MAINMAP V396A8_5] sectorIncomeGeometry=SOURCE_FINAL14 TextButtonAlign=1(CENTER) anchorsTop=picX+35+56*i anchorsBottom=picX+50+62*(i-4) yTop=picY+45 yBottom=picY+106 coordinateDeltas=UNCHANGED input=UNCHANGED_CHROME1 gameplayAI=UNTOUCHED");
            Debug.Log("[C2:BIGMAP DIPLOMACY V396A8_6R2] visualShell=CDiplMenuInfo final14Countries=9 relationGrid=9x9 source11Logic=YES final14Expansion=YES back=bmElements5:1 table=bmElements5:0 bmElements5Sha256=687ba8a9faf8af301ab0f65bf1111cf458d1b8cbde5abf9b15b1ce9678421303 flags=lva_Flags:1..9 picture=dCounPic owner+1 stateInit=PEACE_PAIRWISE_36_DISPLAY_BLANK actions=6_SOURCE11_GEOMETRY_FINAL14_KEYS barProjection=ORIGINAL14_RUNTIME_CAPTURE_STRAIGHT_QUARTER_TURN sourceStoredAngle=64 sourceComment90=YES rebelPicture=dCounPic:0 gameplayContracts=PENDING input=UNCHANGED_CHROME1 mainMap=FROZEN_V396A8_5");
        }

        private RectTransform CreateCanvas(string name, BaseUiRenderer.RenderOptions opt)
        {
            foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null || c.gameObject == null) continue;
                if (c.gameObject.name.StartsWith("C2_", StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(c.gameObject);
            }
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true; canvas.sortingOrder = 1000;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            // CHROME1: ProcessBigMap is a fixed 1024x768 framebuffer.  Keep one
            // logical unit == one screen pixel and snap the stage origin to an
            // integer, the same proven projection rule as UIA3.1.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            RectTransform screen = go.GetComponent<RectTransform>();
            screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one; screen.offsetMin = Vector2.zero; screen.offsetMax = Vector2.zero;

            var stageGo = new GameObject("C2_BigMapStage_1024x768", typeof(RectTransform));
            stageGo.transform.SetParent(screen, false);
            RectTransform rt = (RectTransform)stageGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            float sx = Mathf.Round((Screen.width  - 1024f) * 0.5f);
            float sy = Mathf.Round((Screen.height -  768f) * 0.5f);
            rt.anchoredPosition = new Vector2(sx, -sy);
            rt.sizeDelta = new Vector2(1024, 768);
            EnsureEventSystem();
            return rt;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var e = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                e.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                e.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        private void BuildBackground()
        {
            var go = new GameObject("BigMap_Background", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(_root, false);
            RectTransform rt = (RectTransform)go.transform; Place(rt, 0,0,1024,768);
            RawImage ri = go.GetComponent<RawImage>(); ri.raycastTarget = false;

            // VISUAL3: lva_Turn_Map is part of the final-1.4 source contract, not a
            // Unity-authored backdrop.  The project previously contained a JPEG/JFIF
            // renamed to .bmp; that image already had the black silhouette baked in.
            // Accept the external DataRoot copy only when it is byte-identical to the
            // audited clean final-1.4 asset, otherwise fall back to our bundled clean copy.
            string p = ResolveFinal14TurnMap();
            if (File.Exists(p))
            {
                // V396A8.3R1: the audited final-1.4 file is a real uncompressed 24-bit BMP.
                // Texture2D.LoadImage is the PNG/JPEG path and rejected this source asset in runtime.
                // Reuse the project's existing original-image BMP decoder instead of converting
                // the game asset or inventing a replacement texture.
                byte[] bytes = File.ReadAllBytes(p);
                var sourceImage = Cossacks2Bridge.UnityAdapters.Maps.C2OriginalImageIO.CreateImageFromBytes(bytes, p);
                if (sourceImage != null && sourceImage.Width == 1024 && sourceImage.Height == 768)
                {
                    var tex = Cossacks2Bridge.UnityAdapters.Maps.C2OriginalTextureService.CreateTexture(
                        sourceImage,
                        "C2_Final14_lva_Turn_Map_V396A8_3R1",
                        Cossacks2Bridge.UnityAdapters.Maps.C2OriginalTexturePolicy.UnfilteredPictureLikeOriginal);
                    if (tex != null)
                    {
                        // C2OriginalImageIO normalizes BMP rows to top-down logical order.
                        // Unity RawImage UV has a bottom-left origin, so invert V once here.
                        ri.texture = tex;
                        ri.uvRect = new Rect(0f, 1f, 1f, -1f);
                        Debug.Log($"[C2:BIGMAP VISUAL3 V396A8_3R1] background source='{p}' canonicalFinal14=YES decoder=C2OriginalImageIO_BMP size={tex.width}x{tex.height} uv=TOPDOWN_TO_UNITY");
                    }
                    else
                    {
                        Debug.LogError($"[C2:BIGMAP VISUAL3 V396A8_3R1] background texture creation failed path='{p}'");
                    }
                }
                else
                {
                    Debug.LogError($"[C2:BIGMAP VISUAL3 V396A8_3R1] background BMP decode failed path='{p}'");
                }
            }
            else
            {
                Debug.LogError("[C2:BIGMAP VISUAL3 V396A8_3R1] canonical final-1.4 lva_Turn_Map.bmp not found");
            }
        }

        private void BuildCampaignHeader()
        {
            // ProcessBigMap 1.1 / final 1.4 visual contract:
            // fonMenuTitle[3] = FontG16 White, source GetRLen width, source shortening, flag desk 28x18.
            string nationName = ResolveFinal14CountryNameV396A8_4(_profile.campaignNation, out _, out _);
            string profileName = _profile.m_chName ?? string.Empty;
            Sprite flagSprite = LoadGp(@"Interf3\TotalWarGraph\lva_Flags", Mathf.Clamp(_profile.campaignNation + 1, 0, C2Bfe14ContractV396A.CountryCount));
            float flagW = flagSprite != null ? flagSprite.rect.width : 28f;
            const float headLimit = 535f;

            GpFontSpec font = FontG16White();
            string head = FormatBigMapHeader(profileName, nationName);
            float textW = MeasureGpText(head, font);
            while (profileName.Length > 4 && textW + flagW > headLimit)
            {
                // Keep the CHROME1 shortening policy in this VISUAL1 patch.
                // The exact source cutL algorithm is audited separately and will be changed only
                // in a dedicated header-geometry patch; this patch changes ONLY the renderer.
                profileName = profileName.Substring(0, Math.Max(1, profileName.Length - 4)) + "...";
                head = FormatBigMapHeader(profileName, nationName);
                textW = MeasureGpText(head, font);
            }

            float textX = Mathf.Round(512f - flagW * 0.5f - 5f - textW * 0.5f);
            _headerGpText = CreateGpText(_root, "BigMapCampaignHead_GP", textX, 67, C2LegacyText14.StripFormatting(head), font, GpTextAlign.Left);

            float flagDeskX = Mathf.Round(textX + textW + 10f);
            var desk = new GameObject("BigMapFlagDesk_28x18", typeof(RectTransform));
            desk.transform.SetParent(_root, false);
            RectTransform drt = (RectTransform)desk.transform; Place(drt, flagDeskX, 64, 28, 18);
            _headerNationFlag = MakeImage(drt, "BigMapCampaignFlag", -2, -3,
                flagSprite != null ? flagSprite.rect.width : 28f, flagSprite != null ? flagSprite.rect.height : 18f);
            _headerNationFlag.sprite = flagSprite;
            _headerNationFlag.preserveAspect = false;
            _headerNationFlag.enabled = flagSprite != null;
        }

        private string FormatBigMapHeader(string profileName, string nationName)
        {
            string fmt = Resolve("#CWT_BigMapHead");
            if (string.IsNullOrWhiteSpace(fmt)) return profileName;
            string s = fmt;
            int p = s.IndexOf("%s", StringComparison.Ordinal);
            if (p >= 0) s = s.Substring(0,p) + (profileName ?? string.Empty) + s.Substring(p+2);
            p = s.IndexOf("%s", StringComparison.Ordinal);
            if (p >= 0) s = s.Substring(0,p) + (nationName ?? string.Empty) + s.Substring(p+2);
            return s;
        }

        private float MeasureTmp(string text, float size)
        {
            var go = new GameObject("__MeasureTmp", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_root, false);
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            t.font = Resources.Load<TMP_FontAsset>(_opt.FontResourcePath);
            t.fontSize = size; t.text = C2LegacyText14.StripFormatting(text ?? string.Empty);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Vector2 v = t.GetPreferredValues(t.text);
            UnityEngine.Object.Destroy(go);
            return Mathf.Max(1f, Mathf.Ceil(v.x));
        }

        private void BuildResourceHeader()
        {
            // Final engine 1.4 CResPanel_BM display order is not numeric.
            // Slot -> logical resource: WOOD,GOLD,STONE,FOOD,IRON,COAL,RECRUITS.
            int[] order = { 0, 3, 2, 1, 4, 5, 6 };
            for (int slot = 0; slot < order.Length; slot++)
            {
                int res = order[slot];
                float x0 = 75 + 130 * slot;
                Sprite icon = LoadGp(@"Interf3\res_pic", res + 7);
                float tx = x0 + 54;
                if (icon != null)
                {
                    Image im = MakeImage(_root, "BigMapResIcon" + res, x0, -4, icon.rect.width, icon.rect.height);
                    im.sprite = icon; im.enabled = true; im.preserveAspect = false;
                    tx = x0 + icon.rect.width + 4;
                }
                _resourceGpText[res] = CreateGpText(_root, "BigMapRes" + res + "_GP", tx, 7, "0", FontG14White(), GpTextAlign.Left);
            }
        }

        private void BuildMap()
        {
            // Exact ProcessBigMap map viewport: x=75, y=113, w=600, h=530.
            var vpGo = new GameObject("BigMapViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(BigMapPanController));
            vpGo.transform.SetParent(_root,false);
            _mapViewportRoot = vpGo;
            RectTransform vp = (RectTransform)vpGo.transform; Place(vp,75,113,600,530);
            Image vi = vpGo.GetComponent<Image>(); vi.color = new Color(1,1,1,0.001f); vi.raycastTarget = true;

            var contentGo = new GameObject("BigMapContent", typeof(RectTransform));
            contentGo.transform.SetParent(vp,false);
            _mapContent = (RectTransform)contentGo.transform;
            _mapContent.anchorMin = _mapContent.anchorMax = new Vector2(0,1); _mapContent.pivot = new Vector2(0,1);
            _mapContent.anchoredPosition = new Vector2(_profile.m_iCurMX0, -_profile.m_iCurMY0);
            _mapContent.sizeDelta = new Vector2(_data.MapWidth, _data.MapHeight);

            // Original CPicesPict(a_dsMenu,3,3,0x50): each tile is positioned
            // from the previous tile in its own row/column.  Do not normalize
            // columns/rows to a maximum size: the source code does not do that.
            Sprite[] pieces = new Sprite[9];
            float[] px = new float[9];
            float[] py = new float[9];
            for (int i=0;i<9;i++) pieces[i] = LoadGp(@"Interf3\TotalWarGraph\lva_Europe00", i);
            for (int row=0; row<3; row++)
            for (int col=0; col<3; col++)
            {
                int i=row*3+col;
                if(col>0)
                {
                    int left=i-1;
                    px[i]=px[left]+(pieces[left]!=null?pieces[left].rect.width:0f);
                }
                if(row>0)
                {
                    int up=i-3;
                    py[i]=py[up]+(pieces[up]!=null?pieces[up].rect.height:0f);
                }
                if (pieces[i]==null) continue;
                var g=new GameObject("EuropePiece_"+i,typeof(RectTransform),typeof(Image)); g.transform.SetParent(_mapContent,false);
                RectTransform rt=(RectTransform)g.transform; Place(rt,px[i],py[i],pieces[i].rect.width,pieces[i].rect.height);
                Image im=g.GetComponent<Image>(); im.sprite=pieces[i]; im.preserveAspect=false; im.raycastTarget=false; im.color=Color.white;
            }

            // Original order: lva_Sectors overlays are the true visual/hit layer,
            // followed by bmPopulat / bmDefence markers.
            BuildSectorOverlays();
            BuildSectorMarkers();

            BigMapPanController pan = vpGo.GetComponent<BigMapPanController>();
            pan.Initialize(_mapContent, 600,530,_data.MapWidth,_data.MapHeight, OnMapPanChanged);

            // CPicesPict::CreateMapBorder + SetScreen: bmElements frame 0 at x0-2,y0-2.
            Sprite border = LoadGp(@"Interf3\TotalWarGraph\bmElements", 0);
            if (border != null)
            {
                _mapBorder = MakeImage(_root, "BigMapBorder_bmElements_0", 73, 111, border.rect.width, border.rect.height);
                _mapBorder.sprite = border; _mapBorder.preserveAspect = false; _mapBorder.raycastTarget = false;
            }
        }

        private void BuildSectorOverlays()
        {
            _sectorOverlays.Clear();
            foreach (var s in _data.Sectors)
            {
                Sprite sp = LoadGp(@"Interf3\TotalWarGraph\lva_Sectors", s.Id);
                if (sp == null) continue;
                int id = s.Id;
                var go = new GameObject("SectorOverlay_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_mapContent, false);
                RectTransform rt = (RectTransform)go.transform;
                // CSectStatData::DeposeTo: sector picture x/y are Center directly, not centered.
                Place(rt, s.CenterX, s.CenterY, sp.rect.width, sp.rect.height);
                Image img = go.GetComponent<Image>(); img.sprite = sp; img.preserveAspect = false; img.raycastTarget = true;
                Button b = go.GetComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = img;
                b.onClick.AddListener(() => SelectSector(id));
                _sectorOverlays[id] = img;
            }
            RefreshSectorOverlayColors();
        }

        private void RefreshSectorOverlayColors()
        {
            // CSectStatData::SetSectColor + CSectData::SetSelectedColor.
            // Normal map page uses the owner NATCOLOR with the source ALPHA_SECT
            // arithmetic; the current sector is then replaced by the source
            // SELECTED_SECT_COLOR + owner color. Diplomacy-specific red/green
            // relation colors are intentionally NOT introduced in VISUAL2.
            foreach (var kv in _sectorOverlays)
            {
                if (kv.Value == null) continue;
                C2ProfileRuntime14.SectorState ps = C2BigMapData14.StateFor(_profile, kv.Key);
                C2BigMapData14.SectorDefinition sd = C2BigMapData14.SectorById(_data, kv.Key);
                int owner = ps != null ? ps.owner : (sd != null ? sd.Owner : -1);
                bool selected = _activePage == 0 && kv.Key == _selectedSector;
                kv.Value.color = SourceSectorColor(owner, selected);
            }
        }

        private Color32 SourceSectorColor(int owner, bool selected)
        {
            // 1.1 source:
            //   SetSectColor(owner): ALPHA_SECT + GetBigMapPlayerColor(owner)
            //   SetSelectedColor:    SELECTED_SECT_COLOR + GetBigMapPlayerColor(owner/fallback6)
            // BigMapConst.dat supplies NATCOLORn and SELECTED_SECT_COLOR and final
            // 1.4 keeps them data-driven. Preserve DWORD wrap/add semantics.
            if (!selected && (owner < 0 || owner >= C2Bfe14ContractV396A.CountryCount))
                return new Color32(255, 255, 255, 0);

            int cid = owner;
            if (cid < 0 || cid >= C2Bfe14ContractV396A.CountryCount) cid = 6;
            uint ownerRaw = unchecked((uint)C2BigMapData14.Get(_data, "NATCOLOR" + cid, DefaultNatColorRaw(cid)));
            uint raw;
            if (selected)
            {
                uint selectedRaw = unchecked((uint)C2BigMapData14.Get(_data, "SELECTED_SECT_COLOR", unchecked((int)0x80000000)));
                raw = unchecked(selectedRaw + ownerRaw);
            }
            else
            {
                const uint ALPHA_SECT = 0x20000000u;
                raw = unchecked(ALPHA_SECT + ownerRaw);
            }
            return ArgbToColor32(raw);
        }

        private static int DefaultNatColorRaw(int nation)
        {
            switch (nation)
            {
                case 0: return unchecked((int)0x10EF4123);
                case 1: return unchecked((int)0x104E45C0);
                case 2: return unchecked((int)0x10808040);
                case 3: return unchecked((int)0x10A84DBB);
                case 4: return unchecked((int)0x1077D564);
                case 5: return unchecked((int)0x10FFCF00);
                case 6: return unchecked((int)0x00D0D0D0);
                default: return unchecked((int)0xFF6A4300);
            }
        }

        private static Color32 ArgbToColor32(uint argb)
        {
            return new Color32((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF), (byte)((argb >> 24) & 0xFF));
        }

        private void BuildSectorMarkers()
        {
            _sectorCityMarkers.Clear();
            _sectorFortMarkers.Clear();
            foreach (var s in _data.Sectors)
            {
                C2ProfileRuntime14.SectorState ps = C2BigMapData14.StateFor(_profile, s.Id);
                int pop = ps != null ? ps.population : s.Population;
                int def = ps != null ? ps.defence : s.Defence;

                Sprite city=LoadGp(@"Interf3\TotalWarGraph\bmPopulat", Mathf.Clamp(pop,0,2));
                float cw=city!=null?city.rect.width:18f, ch=city!=null?city.rect.height:18f;
                var go = new GameObject("SectorCity_"+s.Id, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_mapContent,false);
                RectTransform rt=(RectTransform)go.transform; Place(rt,s.CityX-cw/2f,s.CityY-ch/2f,cw,ch);
                Image img=go.GetComponent<Image>();
                if(city!=null){img.sprite=city;img.color=Color.white;} else img.color=new Color(0.95f,0.85f,0.55f,0.9f);
                img.preserveAspect=false; img.raycastTarget=false;
                _sectorCityMarkers[s.Id]=img;

                Sprite fort=LoadGp(@"Interf3\TotalWarGraph\bmDefence", Mathf.Clamp(def,0,3));
                float fw=fort!=null?fort.rect.width:18f, fh=fort!=null?fort.rect.height:18f;
                var fg=new GameObject("SectorFort_"+s.Id,typeof(RectTransform),typeof(Image));
                fg.transform.SetParent(_mapContent,false);
                RectTransform fr=(RectTransform)fg.transform; Place(fr,s.FortX-fw/2f,s.FortY-fh/2f,fw,fh);
                Image fim=fg.GetComponent<Image>(); fim.sprite=fort; fim.color=Color.white; fim.preserveAspect=false; fim.raycastTarget=false; fim.enabled=fort!=null;
                _sectorFortMarkers[s.Id]=fim;
            }
        }

        private void RefreshSectorMarkers()
        {
            // CSectStatData::SetSityType uses the mutable campaign population and
            // defence fields.  Do not keep the initial Sectors.dat frames after the
            // campaign state changes.
            foreach (var s in _data.Sectors)
            {
                C2ProfileRuntime14.SectorState ps = C2BigMapData14.StateFor(_profile, s.Id);
                int pop = ps != null ? ps.population : s.Population;
                int def = ps != null ? ps.defence : s.Defence;

                if (_sectorCityMarkers.TryGetValue(s.Id, out Image cityImage) && cityImage != null)
                {
                    Sprite city = LoadGp(@"Interf3\TotalWarGraph\bmPopulat", Mathf.Clamp(pop,0,2));
                    cityImage.sprite = city;
                    cityImage.enabled = city != null;
                    if (city != null)
                    {
                        cityImage.rectTransform.sizeDelta = new Vector2(city.rect.width, city.rect.height);
                        cityImage.rectTransform.anchoredPosition = new Vector2(Mathf.Round(s.CityX-city.rect.width/2f), -Mathf.Round(s.CityY-city.rect.height/2f));
                    }
                }

                if (_sectorFortMarkers.TryGetValue(s.Id, out Image fortImage) && fortImage != null)
                {
                    Sprite fort = LoadGp(@"Interf3\TotalWarGraph\bmDefence", Mathf.Clamp(def,0,3));
                    fortImage.sprite = fort;
                    fortImage.enabled = fort != null;
                    if (fort != null)
                    {
                        fortImage.rectTransform.sizeDelta = new Vector2(fort.rect.width, fort.rect.height);
                        fortImage.rectTransform.anchoredPosition = new Vector2(Mathf.Round(s.FortX-fort.rect.width/2f), -Mathf.Round(s.FortY-fort.rect.height/2f));
                    }
                }
            }
        }
        private void BuildRightInfo()
        {
            // CSectorMenu::Init/CreatePictureAndButton/SetMenuPosition visual path.
            // VISUAL3 changes only proven main-page/source-parity points: no market/diplomacy/AI runtime.
            var menu = new GameObject("CSectorMenu_SourceChrome", typeof(RectTransform));
            menu.transform.SetParent(_root, false);
            _sectorMenuRoot = menu;
            RectTransform mr = (RectTransform)menu.transform; Place(mr,0,0,1024,768);

            const float X0 = 701f; // Init X0=690; SetMenuPosition mutates X0 += 11.
            const float dX = 4f, dY = 3f, delta = 11f;
            float countryY = 119f; // YYY=114, then +5.

            GpFontSpec countryFont = FontG18(C2Red);        // fonMenuTitle2[1]
            GpFontSpec grayFont    = FontC14(C2Gray);       // GrayFont
            GpFontSpec redFont     = FontC14(C2Red);        // RedFont

            // Use the same direct GN16 bank as VISUAL1. Geometry follows the
            // source TextButton heights rather than Unity TMP metrics.
            float titleLineH = GpGlyphPixelHeight(countryFont, 'W', 23f);
            float grayLineH  = GpGlyphPixelHeight(grayFont, 'W', 23f);

            _countryNameGp = CreateGpText(mr,"SectorCountry_GP",705,countryY,"",countryFont,GpTextAlign.Left);
            _nationFlag = MakeImage(mr,"SectorFlag",904,114,32,24);

            float sectorY = countryY + titleLineH + 3f*dY;
            _sectorTitleGp = CreateGpText(mr,"SectorName_GP",705,sectorY,"",grayFont,GpTextAlign.Left,true);

            float miniY = sectorY + grayLineH + delta;
            Sprite miniBorderSp = LoadGp(@"Interf3\TotalWarGraph\bmElements",1);
            float miniW = miniBorderSp != null ? miniBorderSp.rect.width : 230f;
            float miniH = miniBorderSp != null ? miniBorderSp.rect.height : 115f;
            _sectorPreview = MakeImage(mr,"SectorMiniMap",X0+dX,miniY,miniW,miniH); _sectorPreview.preserveAspect=false;
            _sectorMiniBorder = MakeImage(mr,"SectorMiniBorder_bmElements_1",X0+dX,miniY,miniW,miniH);
            _sectorMiniBorder.sprite = miniBorderSp; _sectorMiniBorder.preserveAspect=false; _sectorMiniBorder.raycastTarget=false;

            float popY = miniY + miniH - 1f + delta + dY;
            _populationGp = CreateGpText(mr,"SectorPopulation_GP",X0+2*dX-20,popY,"",grayFont,GpTextAlign.Left,true);
            _recruitsGp   = CreateGpText(mr,"SectorRecruits_GP",X0+127,popY,"",grayFont,GpTextAlign.Left,true);

            float resY = popY + grayLineH + delta;
            Sprite resSp = LoadGp(@"Interf3\TotalWarGraph\bmElements",4);
            float resW = resSp != null ? resSp.rect.width : 235f;
            float resH = resSp != null ? resSp.rect.height : 120f;
            _sectorIncomePicture = MakeImage(mr,"SectorIncome_bmElements_4",X0+dX,resY,resW,resH);
            _sectorIncomePicture.sprite=resSp; _sectorIncomePicture.preserveAspect=false; _sectorIncomePicture.raycastTarget=false;
            for(int i=0;i<7;i++)
            {
                float tx = i<4 ? (X0+dX+35+56*i) : (X0+dX+50+62*(i-4));
                float ty = i<4 ? (resY+45) : (resY+106);
                // Original 1.1 and final 1.4 create these seven TextButton objects with Align=1.
                // Their X values are CENTER anchors, not left edges.
                _sectorIncomeGpText[i]=CreateGpText(mr,"SectorIncomeText_"+i+"_GP",tx,ty,"+0",redFont,GpTextAlign.Center);
            }

            float defY = resY + resH - 1f + delta + dY;
            _defenceGp = CreateGpText(mr,"SectorDefence_GP",X0+2*dX,defY,"",grayFont,GpTextAlign.Left,true);

            float actionY = defY + grayLineH + delta;
            // addGP_TextButton(..., dActions, 0, RedFont, OrangeFont), then source
            // sets Sprite=1(active), Sprite1=0(passive), FontDy=-2, Disabled=Gray.
            _defenceButton = MakeSourceGpButton(mr,"UpgradeDefence",X0+dX,actionY,@"Interf3\TotalWarGraph\dActions",0,1,Resolve("#UpgradeDef#"),14,-2,
                C2Orange,C2Red,OnUpgradeDefence);
            _diversionButton = MakeSourceGpButton(mr,"Diversion",X0+dX,actionY,@"Interf3\TotalWarGraph\dActions",0,1,Resolve("#CWB_Diversion"),14,-2,
                C2Orange,C2Red,OnDiversionChromeOnly);

            float actionH = 24f;
            Image actionImg = _defenceButton != null ? _defenceButton.targetGraphic as Image : null;
            if(actionImg!=null && actionImg.sprite!=null) actionH=actionImg.sprite.rect.height;
            Sprite delimSp = LoadGp(@"Interf3\TotalWarGraph\bmElements",30);
            float delimW = delimSp != null ? delimSp.rect.width : 100f;
            float delimH = delimSp != null ? delimSp.rect.height : 8f;
            float delimX = (X0+dX) + (resW-delimW)*0.5f + 2f;
            float delimY = actionY + actionH + delta + 1f;
            _sectorDelimiter = MakeImage(mr,"SectorDelimiter_bmElements_30",delimX,delimY,delimW,delimH);
            _sectorDelimiter.sprite=delimSp; _sectorDelimiter.preserveAspect=false; _sectorDelimiter.raycastTarget=false;
        }

        private void BuildDiplomacyVisualV396A8_6()
        {
            // Inventory/source proof:
            // 1.1 CDiplMenuInfo::CreateElements defines the panel semantics;
            // final 1.4 FUN_0111d2c0 keeps X0=690/Y0=112 but expands flags/cells
            // to 9x9 and replaces the old bmElements 5/6 backings with bmElements5 1/0.
            const float X0 = 690f;
            const float Y0 = 112f;
            const float stDX = 22f; // final 1.4 0x16
            const float stDY = 20f; // final 1.4 0x14

            var rootGo = new GameObject("CDiplMenuInfo_FINAL14_9x9", typeof(RectTransform));
            rootGo.transform.SetParent(_root, false);
            _diplomacyRoot = rootGo;
            RectTransform dr = (RectTransform)rootGo.transform;
            Place(dr,0,0,1024,768);

            _diplCountryNameGp = CreateGpText(dr,"DiplCountryName_GP",X0+15f,Y0+7f,"-",FontG18(C2Red),GpTextAlign.Left);

            Sprite firstFlag = LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\lva_Flags",1);
            _diplCountryFlag = MakeImage(dr,"DiplCountryFlag_lva_Flags",X0+214f,Y0+2f,
                firstFlag!=null?firstFlag.rect.width:32f, firstFlag!=null?firstFlag.rect.height:24f);
            _diplCountryFlag.sprite=firstFlag; _diplCountryFlag.enabled=firstFlag!=null; _diplCountryFlag.preserveAspect=false;

            Sprite firstPict = LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\dCounPic",0);
            _diplCountryPicture = MakeImage(dr,"DiplCountryPicture_dCounPic",X0+55f,Y0+34f,
                firstPict!=null?firstPict.rect.width:128f, firstPict!=null?firstPict.rect.height:64f);
            _diplCountryPicture.sprite=firstPict; _diplCountryPicture.enabled=firstPict!=null; _diplCountryPicture.preserveAspect=false;

            Sprite dipBackSp = LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\bmElements5",1);
            Image dipBack = MakeImage(dr,"DiplBack_bmElements5_1",X0+16f,Y0+29f,
                dipBackSp!=null?dipBackSp.rect.width:0f, dipBackSp!=null?dipBackSp.rect.height:0f);
            dipBack.sprite=dipBackSp; dipBack.enabled=dipBackSp!=null; dipBack.preserveAspect=false;

            // final 1.4: stX = dipBack.x; stY = dipBack.y1 + 4.
            float stX=X0+16f;
            float stY=(Y0+29f)+(dipBackSp!=null?dipBackSp.rect.height:0f)+4f;

            // The highlight desks are source clipping desks, not arbitrary colored Unity panels.
            // final 1.4 uses dActions frame 7, a 195x20 horizontal clip and a 33x(tabH-20) vertical clip.
            Sprite oldTableMetrics = LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\bmElements",6);
            float oldTabH=oldTableMetrics!=null?oldTableMetrics.rect.height:0f;
            Sprite barSp=LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\dActions",7);

            _diplHorBarDesk=CreateClipDeskV396A8_6(dr,"DiplRelationHorBarDesk",stX+33f,stY+20f,195f,20f);
            if(barSp!=null)
            {
                Image h=MakeImage(_diplHorBarDesk,"DiplRelationHorBar_dActions_7",-10f,-40f,barSp.rect.width,barSp.rect.height);
                h.sprite=barSp; h.preserveAspect=false;
            }
            _diplVerBarDesk=CreateClipDeskV396A8_6(dr,"DiplRelationVerBarDesk",stX+33f,stY+20f,33f,Mathf.Max(0f,oldTabH-20f));
            if(barSp!=null)
            {
                Image v=MakeImage(_diplVerBarDesk,"DiplRelationVerBar_dActions_7",-5f,-5f,barSp.rect.width,barSp.rect.height);
                v.sprite=barSp; v.preserveAspect=false;
                // R2 runtime-capture parity: the original 1.4 raster shows a straight
                // vertical selection strip, not the 66.93-degree wedge produced by treating
                // the stored legacy UI value Angle=64 as a literal Unity/radian angle.
                // The readable 1.1 source itself annotates that assignment as "//90".
                // Keep the original source desk/clipping geometry, but project its visual
                // quarter-turn result explicitly in Unity.
                v.rectTransform.localEulerAngles=new Vector3(0f,0f,-90f);
            }

            // R3: the final-1.4 relation table background (bmElements5:0) already contains
            // the visible 9+9 flag artwork.  The engine also creates lva_Flags underlays
            // before that background (for the original dialog/hint objects).  In the native
            // renderer the underlays do not leak outside the 229x213 table raster.
            // Unity decodes each GU16 frame as a full 32x24 RGBA sprite; with the final-1.4
            // compact spacing (22x20), the last REIN underlays otherwise protrude ~10 px
            // to the right and 4 px below the table, which looks like two extra flags.
            // Preserve the underlays and their source ScaleX=0.9, but clip them to the exact
            // bmElements5:0 bounds.  No REIN-specific suppression or hardcoded deletion.
            Sprite stateBackSp=LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\bmElements5",0);
            float stateBackW=stateBackSp!=null?stateBackSp.rect.width:229f;
            float stateBackH=stateBackSp!=null?stateBackSp.rect.height:213f;
            RectTransform flagUnderlayClip=CreateClipDeskV396A8_6(dr,"DiplFlagUnderlayClip_FINAL14",stX,stY,stateBackW,stateBackH);

            // final 1.4: row origin stX/stY+33, column origin stX+34/stY;
            // 9 frames, compact 22x20 spacing, ScaleX=0.9.
            for(int i=0;i<C2Bfe14ContractV396A.CountryCount;i++)
            {
                Sprite fl=LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\lva_Flags",i+1);
                float fw=fl!=null?fl.rect.width:32f, fh=fl!=null?fl.rect.height:24f;
                _diplRowFlags[i]=MakeImage(flagUnderlayClip,"DiplRowFlag_"+i,0f,33f+i*stDY,fw,fh);
                _diplRowFlags[i].sprite=fl; _diplRowFlags[i].enabled=fl!=null; _diplRowFlags[i].preserveAspect=false;
                _diplRowFlags[i].rectTransform.localScale=new Vector3(0.9f,1f,1f);

                _diplColFlags[i]=MakeImage(flagUnderlayClip,"DiplColFlag_"+i,34f+i*stDX,0f,fw,fh);
                _diplColFlags[i].sprite=fl; _diplColFlags[i].enabled=fl!=null; _diplColFlags[i].preserveAspect=false;
                _diplColFlags[i].rectTransform.localScale=new Vector3(0.9f,1f,1f);
            }

            _diplStateBack=MakeImage(dr,"DiplRelationBack_bmElements5_0",stX,stY,stateBackW,stateBackH);
            _diplStateBack.sprite=stateBackSp; _diplStateBack.enabled=stateBackSp!=null; _diplStateBack.preserveAspect=false;
            Debug.Log($"[C2:BIGMAP DIPLOMACY V396A8_6R3] flagUnderlay=FINAL14_LVA_FLAGS_BELOW_BMELEMENTS5 clip={stateBackW:0}x{stateBackH:0} gu16Frame=32x24 spacing=22x20 scaleX=0.9 reinLeakRightAndBottom=CLIPPED noNationSpecificSuppression=YES");

            // Final 1.4 creates all 81 relation cells with Align=1 (center).
            // CDiplomacyData::OnFirstInit initializes all 36 pairs to PEACE, while SetSectorsColor
            // deliberately does not assign the peace contract to the displayed matrix; therefore
            // the correct initial visual is blank off-diagonal (and blank diagonal), not invented symbols.
            for(int row=0;row<C2Bfe14ContractV396A.CountryCount;row++)
            for(int col=0;col<C2Bfe14ContractV396A.CountryCount;col++)
                _diplStateText[row,col]=CreateGpText(dr,"DiplState_"+row+"_"+col,
                    stX+43f+col*stDX,stY+38f+row*stDY,string.Empty,FontC14(C2Black),GpTextAlign.Center);

            // R2: CDiplMenuButOnMap / final-1.4 dOverMap controls over the Europe map.
            // Source/final coordinates: frame at 86,124; five 39x26 buttons at
            // x=86+i*41+6, y=124+6 (last button +1 px).  The visible normal
            // frames are i+6 except the source's Pressed1=WAR slot, which uses i+1
            // and the original RED_SECT_COLOR diffuse.  Hover/active frame is i+1.
            Sprite mapFilterBack=LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\dOverMap",0);
            _diplMapFilterFrame=MakeImage(dr,"DiplMapFilter_dOverMap_0",86f,124f,
                mapFilterBack!=null?mapFilterBack.rect.width:216f,mapFilterBack!=null?mapFilterBack.rect.height:38f);
            _diplMapFilterFrame.sprite=mapFilterBack; _diplMapFilterFrame.enabled=mapFilterBack!=null; _diplMapFilterFrame.preserveAspect=false;
            for(int i=0;i<5;i++)
            {
                int filter=i;
                int normalFrame=(i==0)?(i+1):(i+6);
                int hoverFrame=i+1;
                float bx=86f+i*41f+6f+(i==4?1f:0f);
                Button b=MakeSourceGpButton(dr,"DiplMapFilter_"+i,bx,130f,@"Interf3\TotalWarGraph\dOverMap",normalFrame,hoverFrame,string.Empty,13f,0f,
                    C2Black,C2Black,()=>OnDiplomacyMapFilterVisualOnlyV396A8_6R2(filter));
                _diplMapFilterButtons[i]=b;
                if(b!=null)
                {
                    BigMapGpHoverVisual hv=b.GetComponent<BigMapGpHoverVisual>();
                    Color32 tint=i==0 ? new Color32(0xFF,0x00,0x00,0xFF) :
                                 (i==_diplMapPressed2 ? new Color32(0x00,0xAA,0x00,0xFF) : new Color32(0xFF,0xFF,0xFF,0xFF));
                    if(hv!=null)
                    {
                        hv.NormalBackgroundTint=tint;
                        hv.HoverBackgroundTint=tint;
                        hv.DisabledBackgroundTint=tint;
                        hv.ApplyCurrentState(false);
                    }
                    else if(b.targetGraphic is Image bi) bi.color=tint;
                }
            }

            Sprite delimSp=LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\bmElements",30);
            float stateH=stateBackSp!=null?stateBackSp.rect.height:0f;
            Image delim=MakeImage(dr,"DiplDelimiter_bmElements_30",X0+20f,stY+stateH+3f,
                delimSp!=null?delimSp.rect.width:0f,delimSp!=null?delimSp.rect.height:0f);
            delim.sprite=delimSp; delim.enabled=delimSp!=null; delim.preserveAspect=false;

            // The six diplomacy actions are source-stable in 1.1 and final 1.4 still exposes
            // six #CHINT_Button1..6 controls.  Nation expansion does not alter this vertical strip.
            // No contract mutation is wired in this VISUAL/DATA shell.
            string[] actionKeys={"#CDB_Button1","#CDB_Button2","#CDB_Button3","#CDB_Button4","#CDB_Button5","#CDB_Button6"};
            for(int i=0;i<actionKeys.Length;i++)
            {
                int action=i;
                _diplActionButtons[i]=MakeSourceGpButton(dr,"DiplAction_"+i,705f,491f+(i+1)*27f,
                    @"Interf3\TotalWarGraph\dActions",0,1,ResolveDiplomacyActionLabelV396A8_6R2(i,actionKeys[i]),13f,-2f,
                    C2Red,C2Orange,()=>OnDiplomacyActionVisualOnlyV396A8_6(action));
            }

            _diplomacyRoot.SetActive(false);
            RefreshDiplomacyVisualV396A8_6();
        }

        private RectTransform CreateClipDeskV396A8_6(RectTransform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(RectMask2D));
            go.transform.SetParent(parent,false);
            RectTransform rt=(RectTransform)go.transform;
            Place(rt,x,y,w,h);
            return rt;
        }

        private void RefreshDiplomacyVisualV396A8_6()
        {
            if(_diplomacyRoot==null || _data==null || _profile==null) return;
            C2BigMapData14.SectorDefinition sd=C2BigMapData14.SectorById(_data,_selectedSector);
            C2ProfileRuntime14.SectorState ps=C2BigMapData14.StateFor(_profile,_selectedSector);
            int owner=ps!=null?ps.owner:(sd!=null?sd.Owner:-1);
            bool valid=owner>=0 && owner<C2Bfe14ContractV396A.CountryCount;

            if(_diplCountryNameGp!=null)
                SetGpText(_diplCountryNameGp,valid?ResolveFinal14CountryNameV396A8_4(owner,out _,out _):Resolve("#REBEL"));

            int frame=valid?owner+1:0;
            if(_diplCountryFlag!=null)
            {
                Sprite sp=valid?LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\lva_Flags",frame):null;
                _diplCountryFlag.sprite=sp; _diplCountryFlag.enabled=sp!=null;
                if(sp!=null) _diplCountryFlag.rectTransform.sizeDelta=new Vector2(sp.rect.width,sp.rect.height);
            }
            if(_diplCountryPicture!=null)
            {
                // Original/final SetCountryPicture always projects owner+1.
                // Rebel owner=-1 therefore uses dCounPic frame 0; only the flag is hidden.
                // Do not hide the country picture for a rebel sector.
                int pictFrame=valid?frame:0;
                Sprite sp=LoadDiplomacyGpV396A8_6(@"Interf3\TotalWarGraph\dCounPic",pictFrame);
                _diplCountryPicture.sprite=sp; _diplCountryPicture.enabled=sp!=null;
                if(sp!=null) _diplCountryPicture.rectTransform.sizeDelta=new Vector2(sp.rect.width,sp.rect.height);
            }

            // Exact final-1.4 ShiftTableBars projection, not the old 6-nation spacing:
            // horizontal Y = stateBack.y + (owner+1)*20 + 12;
            // vertical X   = stateBack.x + (owner+1)*30.
            if(_diplStateBack!=null && valid)
            {
                Vector2 sb=_diplStateBack.rectTransform.anchoredPosition;
                float stateX=sb.x;
                float stateY=-sb.y;
                if(_diplHorBarDesk!=null)
                {
                    Vector2 hp=_diplHorBarDesk.anchoredPosition;
                    _diplHorBarDesk.anchoredPosition=new Vector2(hp.x,-(stateY+(owner+1)*20f+12f));
                    _diplHorBarDesk.gameObject.SetActive(true);
                }
                if(_diplVerBarDesk!=null)
                {
                    Vector2 vp=_diplVerBarDesk.anchoredPosition;
                    _diplVerBarDesk.anchoredPosition=new Vector2(stateX+(owner+1)*30f,vp.y);
                    _diplVerBarDesk.gameObject.SetActive(true);
                }
            }
            else
            {
                if(_diplHorBarDesk!=null) _diplHorBarDesk.gameObject.SetActive(false);
                if(_diplVerBarDesk!=null) _diplVerBarDesk.gameObject.SetActive(false);
            }

            // Source CheckEnabledDiplButtons hides actions for own or rebel sectors.
            // With the only source-proven current diplomacy state (OnFirstInit: all pairs at PEACE),
            // the Peace action itself is disabled; no later contract state is invented here.
            bool foreign=valid && owner!=_profile.campaignNation;
            for(int i=0;i<_diplActionButtons.Length;i++)
            {
                Button b=_diplActionButtons[i]; if(b==null) continue;
                b.gameObject.SetActive(foreign);
                SetSourceGpButtonEnabled(b,foreign && i!=1);
            }
        }

        private string ResolveDiplomacyActionLabelV396A8_6R2(int index,string fallbackKey)
        {
            // The supplied original-1.4 runtime capture proves two shorter labels than
            // the clean BigMapData.txt #CDB_Button3/#CDB_Button4 strings.  Do not
            // hard-code translated labels: compose them from final-1.4 localization
            // entries that are present in the same text data set.
            if(index==2)
            {
                string v=ResolveFinal14BigMapTextV396A8_4("#CDB_MessLtr2",out _,out _);
                if(!string.IsNullOrWhiteSpace(v) && !v.StartsWith("#",StringComparison.Ordinal)) return v;
            }
            if(index==3)
            {
                string hint=C2LegacyText14.StripFormatting(ResolveFinal14BigMapTextV396A8_4("#CHINT_Button4",out _,out _));
                string relation=C2LegacyText14.StripFormatting(ResolveFinal14BigMapTextV396A8_4("#CDB_MessLtr3",out _,out _));
                if(!string.IsNullOrWhiteSpace(hint) && !hint.StartsWith("#",StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(relation) && !relation.StartsWith("#",StringComparison.Ordinal))
                {
                    string verb=hint.Trim();
                    int sp=verb.IndexOf(' '); if(sp>0) verb=verb.Substring(0,sp);
                    string noun=relation.Trim();
                    int through=noun.IndexOf(" через ",StringComparison.OrdinalIgnoreCase);
                    if(through>0) noun=noun.Substring(0,through);
                    if(noun.Length>0) noun=char.ToLowerInvariant(noun[0])+noun.Substring(1);
                    string composed=(verb+" "+noun).Trim();
                    if(!string.IsNullOrWhiteSpace(composed)) return composed;
                }
            }
            return ResolveFinal14BigMapTextV396A8_4(fallbackKey,out _,out _);
        }

        private void OnDiplomacyMapFilterVisualOnlyV396A8_6R2(int filter)
        {
            // Visual-only projection of CDiplMenuButOnMap::ShowPressedButton.  No sector
            // recoloring or diplomacy contract mutation is enabled in R2.
            if(filter>=0 && filter<_diplMapFilterButtons.Length)
            {
                _diplMapPressed2=filter;
                for(int i=0;i<_diplMapFilterButtons.Length;i++)
                {
                    Button b=_diplMapFilterButtons[i];
                    if(b==null) continue;
                    BigMapGpHoverVisual hv=b.GetComponent<BigMapGpHoverVisual>();
                    Color32 tint=i==0 ? new Color32(0xFF,0x00,0x00,0xFF) :
                                 (i==_diplMapPressed2 ? new Color32(0x00,0xAA,0x00,0xFF) : new Color32(0xFF,0xFF,0xFF,0xFF));
                    if(hv!=null)
                    {
                        hv.NormalBackgroundTint=tint; hv.HoverBackgroundTint=tint; hv.DisabledBackgroundTint=tint;
                        hv.ApplyCurrentState(false);
                    }
                }
            }
            Debug.Log($"[C2:BIGMAP DIPLOMACY V396A8_6R2] dOverMap click index={filter} visualPressed2={_diplMapPressed2} runtime=PENDING_ORIGINAL_DIPLOMACY_MAP_FILTER");
        }

        private void OnDiplomacyActionVisualOnlyV396A8_6(int action)
        {
            Debug.Log($"[C2:BIGMAP DIPLOMACY V396A8_6R2] action click index={action} sector={_selectedSector} runtime=PENDING_ORIGINAL_DIPLOMACY_CONTRACTS");
        }

        private Sprite LoadDiplomacyGpV396A8_6(string rel,int frame)
        {
            Sprite sp;
            if(string.Equals(rel,@"Interf3\TotalWarGraph\bmElements5",StringComparison.OrdinalIgnoreCase))
                sp=LoadCanonicalFinal14BmElements5V396A8_6(frame);
            else if(string.Equals(rel,@"Interf3\TotalWarGraph\lva_Flags",StringComparison.OrdinalIgnoreCase))
                sp=LoadCanonicalFinal14DiplFlagsV396A8_6R2(frame);
            else
                sp=LoadGp(rel,frame);
            if(sp==null) Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] missing original GP rel='{rel}' frame={frame}; no synthetic fallback");
            return sp;
        }

        private Sprite LoadCanonicalFinal14BmElements5V396A8_6(int frame)
        {
            string key="FINAL14_BMELEMENTS5|"+frame;
            if(_spriteCache.TryGetValue(key,out Sprite cached) && cached!=null) return cached;

            string normalized="Interf3_TotalWarGraph_bmElements5.g16";
            string external=Path.Combine(_fs.DataRoot,"Cash",normalized);
            string bundled=Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data","Cash",normalized);
            string path=string.Empty;
            if(IsSha256V396A8_6(external,Final14BmElements5G16Sha256)) path=external;
            else
            {
                if(File.Exists(external))
                    Debug.LogWarning($"[C2:BIGMAP DIPLOMACY V396A8_6R2] rejected non-canonical external bmElements5.g16 path='{external}' sha256='{Sha256File(external)}'");
                if(IsSha256V396A8_6(bundled,Final14BmElements5G16Sha256)) path=bundled;
            }
            if(string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] canonical final-1.4 bmElements5.g16 unavailable expectedSha256={Final14BmElements5G16Sha256}");
                return null;
            }
            if(frame==0)
                Debug.Log($"[C2:BIGMAP DIPLOMACY V396A8_6R2] bmElements5 source='{path}' canonicalFinal14=YES sha256={Final14BmElements5G16Sha256} decoder=GN16 frames=2 expectedDims=229x213,227x145");

            if(!MelinojaCodecBridge.LoadG16ToMemory(path,out var e,false))
            {
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] bmElements5 LoadG16 fail '{path}' {e}");
                return null;
            }
            if(!MelinojaCodecBridge.TryGetG16FrameRGBA(path,frame,out int w,out int h,out byte[] rgba,out var e2))
            {
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] bmElements5 frame={frame} fail '{path}' {e2}");
                return null;
            }
            rgba=FlipRgbaRowsV396A2(rgba,w,h);
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
            tex.filterMode=FilterMode.Point; tex.wrapMode=TextureWrapMode.Clamp;
            tex.LoadRawTextureData(rgba); tex.Apply(false,false);
            Sprite sp=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(0,1),1f);
            sp.name="Interf3_TotalWarGraph_bmElements5_"+frame;
            _spriteCache[key]=sp;
            return sp;
        }

        private Sprite LoadCanonicalFinal14DiplFlagsV396A8_6R2(int frame)
        {
            string key="FINAL14_DIPL_FLAGS|"+frame;
            if(_spriteCache.TryGetValue(key,out Sprite cached) && cached!=null) return cached;

            string path=ResolveCanonicalFinal14DiplFlagsPathV396A8_6R2();
            if(string.IsNullOrEmpty(path)) return null;
            int frameCount=ReadG16FrameCountV396A8_6R2(path);
            if(frameCount<10 || frame<0 || frame>=frameCount)
            {
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] lva_Flags frame contract failed path='{path}' frame={frame} frames={frameCount} required>=10");
                return null;
            }
            if(!_diplFlagsAuditLogged)
            {
                _diplFlagsAuditLogged=true;
                Debug.Log($"[C2:BIGMAP DIPLOMACY V396A8_6R2] lva_Flags source='{path}' canonicalFinal14=YES sha256={Final14DiplFlagsG16Sha256} decoder=GU16 frames={frameCount} requiredFrames=10 stale7Rejected=YES");
            }

            if(!MelinojaCodecBridge.LoadG16ToMemory(path,out var e,false))
            {
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] lva_Flags LoadG16 fail '{path}' {e}");
                return null;
            }
            if(!MelinojaCodecBridge.TryGetG16FrameRGBA(path,frame,out int w,out int h,out byte[] rgba,out var e2))
            {
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] lva_Flags frame={frame} fail '{path}' {e2}");
                return null;
            }
            rgba=FlipRgbaRowsV396A2(rgba,w,h);
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
            tex.filterMode=FilterMode.Point; tex.wrapMode=TextureWrapMode.Clamp;
            tex.LoadRawTextureData(rgba); tex.Apply(false,false);
            Sprite sp=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(0,1),1f);
            sp.name="Interf3_TotalWarGraph_lva_Flags_FINAL14_"+frame;
            _spriteCache[key]=sp;
            return sp;
        }

        private string ResolveCanonicalFinal14DiplFlagsPathV396A8_6R2()
        {
            if(_diplFlagsResolved) return _diplFlagsCanonicalPath;
            _diplFlagsResolved=true;
            string normalized="Interf3_TotalWarGraph_lva_Flags.g16";
            string external=Path.Combine(_fs.DataRoot,"Cash",normalized);
            string bundled=Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data","Cash",normalized);
            if(IsCanonicalDiplFlagsV396A8_6R2(external)) _diplFlagsCanonicalPath=external;
            else
            {
                if(File.Exists(external))
                    Debug.LogWarning($"[C2:BIGMAP DIPLOMACY V396A8_6R2] rejected stale/non-canonical external lva_Flags.g16 path='{external}' sha256='{Sha256File(external)}' frames={ReadG16FrameCountV396A8_6R2(external)} expectedFrames>=10");
                if(IsCanonicalDiplFlagsV396A8_6R2(bundled)) _diplFlagsCanonicalPath=bundled;
            }
            if(string.IsNullOrEmpty(_diplFlagsCanonicalPath))
                Debug.LogError($"[C2:BIGMAP DIPLOMACY V396A8_6R2] canonical final-1.4 lva_Flags.g16 unavailable expectedSha256={Final14DiplFlagsG16Sha256} expectedFrames>=10");
            return _diplFlagsCanonicalPath;
        }

        private bool IsCanonicalDiplFlagsV396A8_6R2(string path)
        {
            return IsSha256V396A8_6(path,Final14DiplFlagsG16Sha256) && ReadG16FrameCountV396A8_6R2(path)>=10;
        }

        private static int ReadG16FrameCountV396A8_6R2(string path)
        {
            try
            {
                if(string.IsNullOrEmpty(path) || !File.Exists(path)) return -1;
                using(var fs=File.OpenRead(path))
                using(var br=new BinaryReader(fs))
                {
                    if(fs.Length<12) return -1;
                    uint magic=br.ReadUInt32();
                    br.ReadUInt32(); // block size
                    // GN16: ushort NSprites at offset 8.
                    if(magic==0x36314E47u) return br.ReadUInt16();
                    // GU16: byte NFramesPerSegment at 8, ushort NSprites at 9.
                    if(magic==0x36315547u) { br.ReadByte(); return br.ReadUInt16(); }
                }
            }
            catch(Exception) { }
            return -1;
        }

        private static bool IsSha256V396A8_6(string path,string expected)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path) &&
                   string.Equals(Sha256File(path),expected,StringComparison.OrdinalIgnoreCase);
        }

        private void BuildPages()
        {
            string[] keys={"#CWV_WorldMap","#CWV_Diplomacy","#CWV_Personal","#CWV_Market","#CWV_Messages"};
            Sprite passive = LoadGp(@"Interf3\TotalWarGraph\lva_Pages",3);
            Sprite active = LoadGp(@"Interf3\TotalWarGraph\lva_Pages",2);
            float w = passive != null ? passive.rect.width : 112f;
            float h = passive != null ? passive.rect.height : 27f;
            for(int i=0;i<5;i++)
            {
                int page=i; float x=75+i*(w-1f);
                Button b=MakeSourceGpButton(_root,"Page_"+i,x,653,@"Interf3\TotalWarGraph\lva_Pages",3,3,Resolve(keys[i]),13,-2,
                    new Color32(109,104,98,255),new Color32(109,104,98,255),()=>SetPage(page));
                if(b!=null)
                {
                    _pageButtonImages[i]=b.targetGraphic as Image;
                    BigMapGpHoverVisual hv=b.GetComponent<BigMapGpHoverVisual>();
                    _pageGpText[i]=hv!=null?hv.GpLabel:null;
                    if(_pageButtonImages[i]!=null) { _pageButtonImages[i].sprite=passive; _pageButtonImages[i].preserveAspect=false; }
                }
            }
            // Do not render synthetic beige pages. Real Diplomacy/Personal/Market/Messages
            // will be restored from their original classes in later passes.
        }

        private void BuildBottomButtons()
        {
            Sprite back=LoadGp(@"Interf3\elements\button_back",1);
            if(back!=null)
            {
                Image bi=MakeImage(_root,"BigMapBottomBack_button_back_1",0,680,back.rect.width,back.rect.height);
                bi.sprite=back; bi.preserveAspect=false; bi.raycastTarget=false;
            }
            // ParentFrame::addGP_TextButton sets Sprite=6 and Sprite1=-1. Therefore
            // dMessage frame 6 is drawn ONLY while MouseOver; passive state is the
            // button_back background plus Yellow text. Active text is White.
            MakeSourceGpButton(_root,"EndTurn",289,713,@"Interf3\TotalWarGraph\dMessage",-1,6,Resolve("#CWB_EndOfTurn"),14,-1,
                C2Yellow,C2White,OnEndTurn);
            MakeSourceGpButton(_root,"QuitCampaign",532,713,@"Interf3\TotalWarGraph\dMessage",-1,6,Resolve("#CWB_Quit"),14,-1,
                C2Yellow,C2White,OnQuit);
        }

        private void RefreshAll()
        {
            RefreshResources(); RefreshSectorMarkers(); RefreshSector(); SetPage(_activePage);
        }

        private void RefreshResources()
        {
            int[] r=_profile.resources ?? new int[7];
            for(int i=0;i<_resourceGpText.Length;i++)
                if(_resourceGpText[i]!=null) SetGpText(_resourceGpText[i],(i<r.Length?r[i]:0).ToString());
        }

        private void SelectSector(int id)
        {
            _selectedSector=id; _profile.m_iCurSecId=id; C2ProfileRuntime14.SaveCurrent();
            RefreshSectorOverlayColors();
            RefreshSector();
            RefreshDiplomacyVisualV396A8_6();
            Debug.Log($"[C2:BIGMAP V395] sector selected id={id}");
        }

        private void RefreshSector()
        {
            var s=C2BigMapData14.SectorById(_data,_selectedSector); if(s==null)return;
            var ps=C2BigMapData14.StateFor(_profile,_selectedSector);
            string nm=ResolveFinal14BigMapTextV396A8_4(s.SectorName, out _, out _);

            int owner=ps!=null?ps.owner:s.Owner;
            int def=ps!=null?ps.defence:s.Defence;
            int rawPop=ps!=null?ps.population:s.Population;

            // CSectorMenu::SetMenuData in both 1.1 and final 1.4: invalid
            // population is reset to level 0 for display, then the local variable
            // is incremented and reused as the population multiplier (1..3).
            int displayPop=(rawPop<0||rawPop>2)?0:rawPop;
            int populationScale=displayPop+1;
            int maxRecruit=C2BigMapData14.Get(_data,"#SECT_MAX_RECRTS",120);
            int rec=ps!=null?ps.recruits:populationScale*maxRecruit;

            if(_countryNameGp!=null) SetGpText(_countryNameGp, owner>=0 ? ResolveFinal14CountryNameV396A8_4(owner, out _, out _) : Resolve("#REBEL"));
            if(_sectorTitleGp!=null) SetGpText(_sectorTitleGp, Resolve("#CDT_Area") + "  {R FF000000}" + nm + "{C}");
            if(_populationGp!=null) SetGpText(_populationGp, Resolve("#Population#") + " {R FF000000}" + Resolve("#CWT_PopltnLVL"+displayPop) + "{C}");
            if(_recruitsGp!=null) SetGpText(_recruitsGp, Resolve("#Mobilization#") + " {R FF000000}" + rec + "/" + (populationScale*maxRecruit) + "{C}");
            if(_defenceGp!=null)
            {
                string defText=Final14DefenceLevelText(def);
                SetGpText(_defenceGp, Resolve("#Defence#") + " {R FF000000}" + defText + "{C}");
            }

            if (_sectorPreview != null)
            {
                string miniAtlas = s.Id >= C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart
                    ? @"Interf3\TotalWarGraph\lva_SectMiniMP2" : @"Interf3\TotalWarGraph\lva_SectMiniMP";
                int miniFrame = s.Id >= C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart
                    ? s.Id - C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart : s.Id;
                _sectorPreview.sprite = LoadGp(miniAtlas, Mathf.Max(0, miniFrame));
                _sectorPreview.enabled = _sectorPreview.sprite != null;
                if(_sectorPreview.sprite!=null) _sectorPreview.rectTransform.sizeDelta = new Vector2(_sectorPreview.sprite.rect.width,_sectorPreview.sprite.rect.height);
            }
            if (_nationFlag != null)
            {
                int frame=(owner>=0 && owner<C2Bfe14ContractV396A.CountryCount)?owner+1:0;
                _nationFlag.sprite=LoadGp(@"Interf3\TotalWarGraph\lva_Flags",frame);
                _nationFlag.enabled=_nationFlag.sprite!=null;
                if(_nationFlag.sprite!=null) _nationFlag.rectTransform.sizeDelta=new Vector2(_nationFlag.sprite.rect.width,_nationFlag.sprite.rect.height);
            }

            // Final 1.4 CSectorMenu::SetMenuData: six base incomes, recruits by
            // populationScale, then the sector's one special resource replaces its
            // displayed slot with GetGoldForRess(resourceId) = base + adding.
            int[] inc = new int[7];
            inc[0]=C2BigMapData14.Get(_data,"#SECT_INCOME_WOOD",750);
            inc[1]=C2BigMapData14.Get(_data,"#SECT_INCOME_FOOD",2000);
            inc[2]=C2BigMapData14.Get(_data,"#SECT_INCOME_STONE",750);
            inc[3]=C2BigMapData14.Get(_data,"#SECT_INCOME_GOLD",500);
            inc[4]=C2BigMapData14.Get(_data,"#SECT_INCOME_IRON",0);
            inc[5]=C2BigMapData14.Get(_data,"#SECT_INCOME_COAL",0);
            inc[6]=populationScale*C2BigMapData14.Get(_data,"#SECT_REG_RECRTS",30);

            int resourceId=ps!=null?ps.resource:s.Resource;
            switch(resourceId)
            {
                case 1: inc[1]=C2BigMapData14.Get(_data,"#SECT_INCOME_FOOD",2000)+C2BigMapData14.Get(_data,"#SECT_ADDING_FOOD",4000); break;
                case 2: inc[3]=C2BigMapData14.Get(_data,"#SECT_INCOME_GOLD",500)+C2BigMapData14.Get(_data,"#SECT_ADDING_GOLD",1500); break;
                case 3: inc[4]=C2BigMapData14.Get(_data,"#SECT_INCOME_IRON",0)+C2BigMapData14.Get(_data,"#SECT_ADDING_IRON",1000); break;
                case 4: inc[5]=C2BigMapData14.Get(_data,"#SECT_INCOME_COAL",0)+C2BigMapData14.Get(_data,"#SECT_ADDING_COAL",1000); break;
            }

            // m_iSabotageID decimal digits are not gameplay invented by Unity:
            // digit 0 zeroes recruit income; digit 1 zeroes all six resources.
            // VISUAL3 only projects/persists that source state; it does not execute
            // diversion gameplay yet.
            int sabotageId=ps!=null?ps.sabotageId:0;
            if(sabotageId%10>0) inc[6]=0;
            if((sabotageId%100)/10>0) for(int i=0;i<6;i++) inc[i]=0;
            for(int i=0;i<_sectorIncomeGpText.Length;i++) if(_sectorIncomeGpText[i]!=null) SetGpText(_sectorIncomeGpText[i],"+"+inc[i]);

            bool own=owner==_profile.campaignNation;
            if(_defenceButton!=null)
            {
                // Original/final CheckDefenceButton keeps the own-sector button
                // enabled; max/low-population is reported by the action/hint path.
                // Do not synthesize a Unity disabled visual state here.
                _defenceButton.gameObject.SetActive(own);
                SetSourceGpButtonEnabled(_defenceButton,own);
            }
            if(_diversionButton!=null)
            {
                _diversionButton.gameObject.SetActive(!own);
                SetSourceGpButtonEnabled(_diversionButton,!own); // action runtime is restored in a later pass.
            }
            RefreshSectorOverlayColors();
        }

        private string Final14DefenceLevelText(int defence)
        {
            // DefLvl_ENUM is registered as #CWT_DefenceLvl0..3 in 1.1 and the
            // final-1.4 executable still resolves the same Enumerator before Get().
            if(defence<0||defence>3)
            {
                Debug.LogWarning($"[C2:BIGMAP VISUAL3 V396A8_3R1] invalid defence enum value={defence} sector={_selectedSector}");
                return string.Empty;
            }
            return Resolve("#CWT_DefenceLvl"+defence);
        }
        private void OnUpgradeDefence()
        {
            var s = C2BigMapData14.SectorById(_data, _selectedSector);
            var ps = C2BigMapData14.StateFor(_profile, _selectedSector);
            if (s == null || ps == null || ps.owner != _profile.campaignNation) return;
            if (ps.defence >= 3 || ps.defence > ps.population) return;

            int next = ps.defence + 1;
            int mult = next < 2 ? 1 : (next == 2 ? C2BigMapData14.Get(_data, "#SECT_DEF_mult2_f", 2) : C2BigMapData14.Get(_data, "#SECT_DEF_mult3_f", 5));
            int wood = C2BigMapData14.Get(_data, "#SECT_DEFENCE_WOOD", 5000) * mult;
            int stone = C2BigMapData14.Get(_data, "#SECT_DEFENCE_STONE", 5000) * mult;
            int gold = C2BigMapData14.Get(_data, "#SECT_DEFENCE_GOLD", 5000) * mult;
            int recruits = C2BigMapData14.Get(_data, "#SECT_DEFENCE_RECRTS", 240) * mult;

            int[] r = _profile.resources ?? new int[7];
            if (r.Length < 7) return;
            if (r[C2BigMapData14.Wood] < wood || r[C2BigMapData14.Stone] < stone || r[C2BigMapData14.Gold] < gold || r[C2BigMapData14.Recruits] < recruits)
            {
                Debug.Log($"[C2:BIGMAP V395] UpgradeDefence blocked sector={_selectedSector} need wood={wood} stone={stone} gold={gold} recruits={recruits}");
                return;
            }

            r[C2BigMapData14.Wood] -= wood;
            r[C2BigMapData14.Stone] -= stone;
            r[C2BigMapData14.Gold] -= gold;
            C2BigMapData14.DeleteRecruitsForDefence(_profile, _data, _selectedSector, recruits);
            ps.defence = next;
            _profile.resources[C2BigMapData14.Recruits] = C2BigMapData14.TotalRecruits(_profile);
            C2ProfileRuntime14.SaveCurrent();
            Debug.Log($"[C2:BIGMAP V395] UpgradeDefence sector={_selectedSector} newDef={next} mult={mult} wood={wood} stone={stone} gold={gold} recruits={recruits}");
            RefreshAll();
        }

        private void OnDiversionChromeOnly()
        {
            Debug.Log($"[C2:BIGMAP CHROME1 V396A8] diversion click sector={_selectedSector} runtime=PENDING_ORIGINAL_ACTION_MENU");
        }

        private void SetPage(int page)
        {
            _activePage=Mathf.Clamp(page,0,C2Bfe14ContractV396A.MainPageCount - 1);
            _profile.m_iCurMenuId=_activePage;
            C2ProfileRuntime14.SaveCurrent();

            // Original ChangeActiveMenu: map remains visible on Diplomacy (page 1),
            // hidden on Personal/Market/Messages. Sector menu is visible only on Map.
            bool mapVisible = _activePage==0 || _activePage==1;
            if(_mapViewportRoot!=null) _mapViewportRoot.SetActive(mapVisible);
            if(_mapBorder!=null) _mapBorder.gameObject.SetActive(mapVisible);
            if(_sectorMenuRoot!=null) _sectorMenuRoot.SetActive(_activePage==0);
            if(_diplomacyRoot!=null) _diplomacyRoot.SetActive(_activePage==1);
            if(_activePage==1) RefreshDiplomacyVisualV396A8_6();

            Sprite act=LoadGp(@"Interf3\TotalWarGraph\lva_Pages",2);
            Sprite pas=LoadGp(@"Interf3\TotalWarGraph\lva_Pages",3);
            for(int i=0;i<5;i++)
            {
                bool active=i==_activePage;
                Color32 stateText=active?new Color32(106,48,0,255):new Color32(109,104,98,255);
                Sprite stateSprite=active?act:pas;
                if(_pageButtonImages[i]!=null)
                {
                    _pageButtonImages[i].sprite=stateSprite;
                    BigMapGpHoverVisual hv=_pageButtonImages[i].GetComponent<BigMapGpHoverVisual>();
                    if(hv!=null)
                    {
                        // ChangeActiveButton source: selected/unselected page does
                        // not change on mouse-over; both Sprite/Sprite1 and both
                        // fonts are the same for the current page state.
                        hv.NormalSprite=stateSprite; hv.HoverSprite=stateSprite;
                        hv.NormalText=stateText; hv.HoverText=stateText;
                    }
                }
                if(_pageGpText[i]!=null) SetGpTextColor(_pageGpText[i],stateText);
            }
            RefreshSectorOverlayColors();
            ShowHelpIfNeededV396A5(false);
        }

        private void ShowHelpIfNeededV396A5(bool force)
        {
            if (_profile == null || _root == null) return;
            int page = Mathf.Clamp(_activePage, 0, C2Bfe14ContractV396A.MainPageCount - 1);
            int bit = 1 << page;
            bool visited = (_profile.bigMapHelpVisitedMask & bit) != 0;
            if (!force && visited) return;
            if (_helpRenderer.IsHelpOpen) return;

            bool shown = _helpRenderer.ShowBigMapHelp(_fs, _loc, null, page, () =>
            {
                Debug.Log($"[C2:BFE14 HELP V396A5] closed page={page}");
            });
            if (!shown) return;

            // Original CBigMapHelp::Refresh marks the page visited when the help opens.
            _profile.bigMapHelpVisitedMask |= bit;
            C2ProfileRuntime14.SaveCurrent();
            Debug.Log($"[C2:BFE14 HELP V396A5] auto={(force ? 0 : 1)} page={page} visitedMask=0x{_profile.bigMapHelpVisitedMask:X}");
        }

        public void ToggleHelpFromHotkeyV396A5()
        {
            if (_helpRenderer.IsHelpOpen)
            {
                _helpRenderer.Close();
                Debug.Log($"[C2:BFE14 HELP V396A5] hotkey-close page={_activePage}");
            }
            else
            {
                ShowHelpIfNeededV396A5(true);
            }
        }

        public void CloseHelpFromEscapeV396A5()
        {
            if (!_helpRenderer.IsHelpOpen) return;
            _helpRenderer.Close();
            Debug.Log($"[C2:BFE14 HELP V396A5] escape-close page={_activePage}");
        }

        private void OnEndTurn()
        {
            C2BigMapData14.EndTurn(_profile,_data); RefreshAll();
        }

        private void OnQuit()
        {
            _helpRenderer.Close();
            C2ProfileRuntime14.SaveCurrent();
            MenuBootstrap boot=UnityEngine.Object.FindFirstObjectByType<MenuBootstrap>();
            if(boot!=null) boot.RenderByScreenId("Single");
        }

        private void OnMapPanChanged(Vector2 p)
        {
            _profile.m_iCurMX0=Mathf.RoundToInt(p.x); _profile.m_iCurMY0=Mathf.RoundToInt(-p.y);
        }

        private string ResolveFinal14BigMapTextV396A8_4(string key, out string source, out bool ambiguous)
        {
            if(_final14Text!=null)
                return _final14Text.Resolve(key,out source,out ambiguous);
            source="LocDbFallback"; ambiguous=false;
            return Resolve(key);
        }

        private string ResolveFinal14CountryNameV396A8_4(int id, out string source, out bool ambiguous)
        {
            source=string.Empty; ambiguous=false;
            if(id<0||id>=C2Bfe14ContractV396A.BigMapCountryTextKeys.Length) return string.Empty;

            string key=C2Bfe14ContractV396A.BigMapCountryTextKeys[id];
            string value=ResolveFinal14BigMapTextV396A8_4(key,out source,out ambiguous);
            if(!ambiguous&&!string.Equals(value,key,StringComparison.OrdinalIgnoreCase)) return value;

            // The installed data and source use both EGYPT/EGIPET spellings.
            // This is the only alias accepted; no country label is invented.
            if(id==5)
            {
                string alias=ResolveFinal14BigMapTextV396A8_4("#EGIPET",out string aliasSource,out bool aliasAmbiguous);
                if(!aliasAmbiguous&&!string.Equals(alias,"#EGIPET",StringComparison.OrdinalIgnoreCase))
                {
                    source=aliasSource; ambiguous=false; return alias;
                }
            }
            return key;
        }

        private void AuditFinal14NameProjectionV396A8_4()
        {
            if(_data==null) return;

            int countryResolved=0, countryAmbiguous=0;
            var countryMissing=new List<string>();
            for(int i=0;i<C2Bfe14ContractV396A.BigMapCountryTextKeys.Length;i++)
            {
                string key=C2Bfe14ContractV396A.BigMapCountryTextKeys[i];
                string value=ResolveFinal14CountryNameV396A8_4(i,out string src,out bool amb);
                bool ok=!amb&&!string.IsNullOrWhiteSpace(value)&&!string.Equals(value,key,StringComparison.OrdinalIgnoreCase);
                if(ok) countryResolved++; else if(amb) countryAmbiguous++; else countryMissing.Add(key);

                if(i>=6)
                    Debug.Log($"[C2:BIGMAP TEXT14 V396A8_4] country index={i} key='{key}' value='{value}' source='{src}' ambiguous={(amb?"YES":"NO")}");
            }

            int sectorResolved=0, sectorAmbiguous=0;
            var sectorMissing=new List<string>();
            for(int i=0;i<_data.Sectors.Count;i++)
            {
                C2BigMapData14.SectorDefinition sec=_data.Sectors[i];
                if(sec==null) continue;
                string key=sec.SectorName??string.Empty;
                string value=ResolveFinal14BigMapTextV396A8_4(key,out string src,out bool amb);
                bool ok=!amb&&!string.IsNullOrWhiteSpace(value)&&!string.Equals(value,key,StringComparison.OrdinalIgnoreCase);
                if(ok) sectorResolved++; else if(amb) sectorAmbiguous++; else sectorMissing.Add($"{sec.Id}:{key}");

                if(sec.Id>=C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart)
                    Debug.Log($"[C2:BIGMAP TEXT14 V396A8_4] sector id={sec.Id} key='{key}' value='{value}' source='{src}' ambiguous={(amb?"YES":"NO")}");
            }

            string status=(countryMissing.Count==0&&sectorMissing.Count==0&&countryAmbiguous==0&&sectorAmbiguous==0)?"PASS":"PENDING";
            Debug.Log($"[C2:BIGMAP TEXT14 V396A8_4] audit status={status} countries={countryResolved}/{C2Bfe14ContractV396A.CountryCount} countryAmbiguous={countryAmbiguous} sectors={sectorResolved}/{_data.Sectors.Count} sectorAmbiguous={sectorAmbiguous} projection=GetTextByID source=FINAL14_TEXT_DATA noHardcodedNames=YES");
            if(countryMissing.Count>0) Debug.LogWarning($"[C2:BIGMAP TEXT14 V396A8_4] unresolved countries={string.Join(",",countryMissing)}");
            if(sectorMissing.Count>0) Debug.LogWarning($"[C2:BIGMAP TEXT14 V396A8_4] unresolved sectors={string.Join(",",sectorMissing)}");
        }

        private string Resolve(string key)
        {
            if(string.IsNullOrWhiteSpace(key))return string.Empty;
            string v=_loc!=null?_loc.Resolve(key):key;
            return string.IsNullOrWhiteSpace(v)?key:v;
        }

        private Sprite LoadGp(string rel,int frame)
        {
            string key=rel+"|"+frame; if(_spriteCache.TryGetValue(key,out Sprite cached)&&cached!=null)return cached;
            string path=FindG16(rel); if(string.IsNullOrEmpty(path))return null;
            if(!MelinojaCodecBridge.LoadG16ToMemory(path,out var e,false)){Debug.LogWarning($"[C2:BIGMAP V395] LoadG16 fail '{path}' {e}");return null;}
            if(!MelinojaCodecBridge.TryGetG16FrameRGBA(path,frame,out int w,out int h,out byte[] rgba,out var e2)){Debug.LogWarning($"[C2:BIGMAP V395] frame={frame} fail '{path}' {e2}");return null;}
            // Melinoja exposes G16 scanlines in the original top-to-bottom order,
            // while Unity raw Texture2D data is displayed bottom-to-top.  Without
            // this conversion every BigMap GP frame is vertically inverted.
            rgba = FlipRgbaRowsV396A2(rgba, w, h);
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false); tex.filterMode=FilterMode.Point; tex.wrapMode=TextureWrapMode.Clamp; tex.LoadRawTextureData(rgba); tex.Apply(false,false);
            Sprite sp=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(0,1),1f); sp.name=Path.GetFileNameWithoutExtension(path)+"_"+frame;
            _spriteCache[key]=sp; return sp;
        }


        private static byte[] FlipRgbaRowsV396A2(byte[] src, int w, int h)
        {
            if (src == null || w <= 0 || h <= 1 || src.Length < w * h * 4) return src;
            int stride = w * 4;
            byte[] dst = new byte[src.Length];
            for (int y = 0; y < h; y++)
                Buffer.BlockCopy(src, y * stride, dst, (h - 1 - y) * stride, stride);
            return dst;
        }

        private string FindG16(string rel)
        {
            string normalized=rel.Replace('\\','_').Replace('/','_')+".g16";
            string[] p={
                _fs.ResolvePath(rel+".g16"),
                Path.Combine(_fs.DataRoot,"Cash",normalized),
                Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data",rel.Replace('\\',Path.DirectorySeparatorChar)+".g16"),
                Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data","Cash",normalized)
            };
            foreach(string s in p) if(!string.IsNullOrEmpty(s)&&File.Exists(s))return s;
            return string.Empty;
        }

        private string ResolveFinal14TurnMap()
        {
            const string rel=@"Interf3\TotalWarGraph\lva_Turn_Map.bmp";
            string direct=_fs!=null?_fs.ResolvePath(rel):string.Empty;
            string bundled=Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data","Interf3","TotalWarGraph","lva_Turn_Map.bmp");

            if(IsCanonicalFinal14TurnMap(direct)) return direct;
            if(!string.IsNullOrEmpty(direct)&&File.Exists(direct))
                Debug.LogWarning($"[C2:BIGMAP VISUAL3 V396A8_3R1] rejected non-canonical external lva_Turn_Map path='{direct}' sha256='{Sha256File(direct)}'");

            if(IsCanonicalFinal14TurnMap(bundled)) return bundled;
            if(!string.IsNullOrEmpty(bundled)&&File.Exists(bundled))
                Debug.LogError($"[C2:BIGMAP VISUAL3 V396A8_3R1] bundled lva_Turn_Map is not the audited final-1.4 asset path='{bundled}' sha256='{Sha256File(bundled)}'");

            return string.Empty;
        }

        private static bool IsCanonicalFinal14TurnMap(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path) &&
                   string.Equals(Sha256File(path),Final14TurnMapSha256,StringComparison.OrdinalIgnoreCase);
        }

        private static string Sha256File(string path)
        {
            try
            {
                using(SHA256 sha=SHA256.Create())
                using(FileStream stream=File.OpenRead(path))
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-",string.Empty).ToLowerInvariant();
            }
            catch(Exception)
            {
                return string.Empty;
            }
        }

        private string ResolveAsset(string rel,string bundledName)
        {
            string direct=_fs.ResolvePath(rel); if(File.Exists(direct))return direct;
            string cache=Path.Combine(_fs.DataRoot,"Cash",rel.Replace('\\','_').Replace('/','_')); if(File.Exists(cache))return cache;
            string b=Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data","Interf3","TotalWarGraph",bundledName); if(File.Exists(b))return b;
            return string.Empty;
        }

        private Image MakeImage(RectTransform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);RectTransform rt=(RectTransform)go.transform;Place(rt,x,y,w,h);Image im=go.GetComponent<Image>();im.preserveAspect=true;im.raycastTarget=false;return im;
        }

        private TextMeshProUGUI MakeText(RectTransform parent,string name,float x,float y,float w,float h,string text,float size,TextAlignmentOptions align,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);RectTransform rt=(RectTransform)go.transform;Place(rt,x,y,w,h);
            TextMeshProUGUI t=go.GetComponent<TextMeshProUGUI>();t.font=Resources.Load<TMP_FontAsset>(_opt.FontResourcePath);t.fontSize=size;t.text=text;t.color=color;t.alignment=align;t.richText=false;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;
        }

        private float GpFontHeight(string gp, char ch, float fallback)
        {
            Sprite sp=LoadGp(gp,(byte)ch);
            return sp!=null?sp.rect.height:fallback;
        }

        private Button MakeSourceGpButton(RectTransform parent,string name,float x,float y,string gp,int normalFrame,int hoverFrame,string text,float size,float fontDy,Color32 normalText,Color32 hoverText,UnityEngine.Events.UnityAction click)
        {
            Sprite normal=normalFrame>=0?LoadGp(gp,normalFrame):null;
            Sprite hover=hoverFrame>=0?LoadGp(gp,hoverFrame):null;
            float w=normal!=null?normal.rect.width:(hover!=null?hover.rect.width:112f);
            float h=normal!=null?normal.rect.height:(hover!=null?hover.rect.height:27f);
            if(normal==null && hover==null)
                Debug.LogError($"[C2:BIGMAP VISUAL3 V396A8_3R1] missing source button gp='{gp}' frames={normalFrame}/{hoverFrame}");
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(BigMapGpHoverVisual));
            go.transform.SetParent(parent,false); RectTransform rt=(RectTransform)go.transform; Place(rt,x,y,w,h);
            Image im=go.GetComponent<Image>(); im.sprite=normal; im.preserveAspect=false; im.raycastTarget=true; im.color=normal!=null?Color.white:new Color(1,1,1,0.001f);
            Button b=go.GetComponent<Button>(); b.targetGraphic=im; b.transition=Selectable.Transition.None; if(click!=null)b.onClick.AddListener(click);

            // Source button font family is determined by the caller's original font colors:
            // lva_Pages -> FontC14 (Gray/Orange); bottom dMessage -> FontG14 (Yellow/White);
            // dActions -> FontC14 (Orange/Red/Gray).
            bool bottom = string.Equals(name,"EndTurn",StringComparison.Ordinal) || string.Equals(name,"QuitCampaign",StringComparison.Ordinal);
            GpFontSpec font = bottom ? FontG14(normalText) : FontC14(normalText);
            float lineH = font.LineHeight;
            float textW = MeasureGpText(text,font);
            float tx = Mathf.Floor((w-textW)*0.5f);
            float ty = Mathf.Floor(((h-1f)-lineH)*0.5f)+fontDy;
            GpTextHandle label = CreateGpText(rt,"Text_GP",tx,ty,text,font,GpTextAlign.Left);

            BigMapGpHoverVisual hv=go.GetComponent<BigMapGpHoverVisual>();
            hv.Background=im; hv.Button=b; hv.NormalSprite=normal; hv.HoverSprite=hover; hv.GpLabel=label; hv.NormalText=normalText; hv.HoverText=hoverText; hv.DisabledText=new Color32(109,104,98,255);
            return b;
        }

        public enum GpTextAlign { Left, Center, Right }

        public sealed class GpFontSpec
        {
            public string GPFile;
            public Color32 Color;
            public int Top;
            public int Bottom;
            public int YShift;
            public float LineHeight => Mathf.Max(1, Bottom-Top);
        }

        public sealed class GpTextHandle
        {
            public RectTransform Root;
            public string Text;
            public GpFontSpec Font;
            public GpTextAlign Align;
            public float BoxWidth;
            public bool ParseLegacyColors;
        }

        private static readonly Color32 C2Black   = new Color32(0x2E,0x23,0x17,0xFF);
        private static readonly Color32 C2Red     = new Color32(0x8A,0x10,0x00,0xFF);
        private static readonly Color32 C2Yellow  = new Color32(0xD4,0xC1,0x9C,0xFF);
        private static readonly Color32 C2White   = new Color32(0xFF,0xF7,0xEF,0xFF);
        private static readonly Color32 C2Gray    = new Color32(0x6D,0x68,0x62,0xFF);
        private static readonly Color32 C2Orange  = new Color32(0x6A,0x30,0x00,0xFF);

        private static GpFontSpec FontG14White() => FontG14(C2White);
        private static GpFontSpec FontG14(Color32 c) => new GpFontSpec { GPFile=@"interf3\Fonts\FontG14", Color=c, Top=3, Bottom=14, YShift=8 };
        private static GpFontSpec FontG16White() => new GpFontSpec { GPFile=@"interf3\Fonts\FontG16", Color=C2White, Top=10, Bottom=23, YShift=6 };
        private static GpFontSpec FontG18(Color32 c) => new GpFontSpec { GPFile=@"interf3\Fonts\FontG18", Color=c, Top=9, Bottom=23, YShift=12 };
        private static GpFontSpec FontC14(Color32 c) => new GpFontSpec { GPFile=@"interf3\Fonts\FontC14", Color=c, Top=12, Bottom=23, YShift=6 };

        private GpTextHandle CreateGpText(RectTransform parent,string name,float x,float y,string text,GpFontSpec font,GpTextAlign align,bool parseLegacyColors=false)
        {
            var go=new GameObject(name,typeof(RectTransform));
            go.transform.SetParent(parent,false);
            RectTransform rt=(RectTransform)go.transform;
            rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(x,-y); rt.sizeDelta=Vector2.zero;
            var h=new GpTextHandle{Root=rt,Text=text??string.Empty,Font=font,Align=align,BoxWidth=0,ParseLegacyColors=parseLegacyColors};
            RebuildGpText(h);
            return h;
        }

        private void SetGpText(GpTextHandle h,string text)
        {
            if(h==null||h.Root==null)return;
            text=text??string.Empty;
            if(string.Equals(h.Text,text,StringComparison.Ordinal))return;
            h.Text=text; RebuildGpText(h);
        }

        private void SetGpTextColor(GpTextHandle h,Color32 color)
        {
            if(h==null||h.Root==null||h.Font==null)return;
            h.Font.Color=color;
            for(int i=0;i<h.Root.childCount;i++)
            {
                Image im=h.Root.GetChild(i).GetComponent<Image>();
                if(im!=null) im.color=color;
            }
        }

        private void RebuildGpText(GpTextHandle h)
        {
            if(h==null||h.Root==null||h.Font==null)return;
            for(int i=h.Root.childCount-1;i>=0;i--) UnityEngine.Object.Destroy(h.Root.GetChild(i).gameObject);
            string source=h.Text??string.Empty;
            string plain=C2LegacyText14.StripFormatting(source);
            float total=MeasureGpText(plain,h.Font);
            float xx=0f;
            if(h.BoxWidth>0f && h.Align==GpTextAlign.Center) xx=Mathf.Floor((h.BoxWidth-total)*0.5f);
            else if(h.BoxWidth>0f && h.Align==GpTextAlign.Right) xx=h.BoxWidth-total;
            // ParentFrame::addTextButton(..., Align=1/2) treats px as the text
            // center/right anchor when no fixed text box is supplied. The source
            // uses Align=1 for all seven CSectorMenu income labels.
            else if(h.BoxWidth<=0f && h.Align==GpTextAlign.Center) xx=-Mathf.Floor(total*0.5f);
            else if(h.BoxWidth<=0f && h.Align==GpTextAlign.Right) xx=-total;

            Color32 defaultColor=h.Font.Color;
            Color32 currentColor=defaultColor;
            for(int i=0;i<source.Length;i++)
            {
                char ch=source[i];
                if(ch=='{' )
                {
                    int close=source.IndexOf('}',i+1);
                    if(close>=0)
                    {
                        string token=source.Substring(i+1,close-i-1).Trim();
                        if(h.ParseLegacyColors)
                        {
                            if(token.Equals("C",StringComparison.OrdinalIgnoreCase)) currentColor=defaultColor;
                            else if(token.StartsWith("R ",StringComparison.OrdinalIgnoreCase))
                            {
                                string hex=token.Substring(2).Trim();
                                if(uint.TryParse(hex,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out uint argb))
                                    currentColor=ArgbToColor32(argb);
                            }
                        }
                        i=close;
                        continue;
                    }
                }
                if(ch=='\r') continue;
                if(ch=='\n' || ch=='\\') continue; // single-line CSectorMenu fields only
                C2LegacyText14.TryEncodeChar(ch,out byte code);
                if(code==0x20||code==0xA0){xx+=MeasureGpSpace(h.Font);continue;}
                if(code==0x09){xx+=MeasureGpSpace(h.Font)*4f;continue;}
                Sprite sp=LoadGlyphDirect(h.Font,code);
                if(sp==null){xx+=MeasureGpSpace(h.Font);continue;}
                var go=new GameObject("G"+code.ToString("D3"),typeof(RectTransform),typeof(Image));
                go.transform.SetParent(h.Root,false);
                RectTransform rt=(RectTransform)go.transform;
                rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1);
                float gy=Mathf.Floor(h.Font.LineHeight-h.Font.Bottom);
                rt.anchoredPosition=new Vector2(xx,-gy); rt.sizeDelta=new Vector2(sp.rect.width,sp.rect.height);
                Image im=go.GetComponent<Image>(); im.sprite=sp; im.type=Image.Type.Simple; im.preserveAspect=false; im.color=currentColor; im.raycastTarget=false; im.useSpriteMesh=false;
                xx+=sp.rect.width;
            }
            h.Root.sizeDelta=new Vector2(Mathf.Max(1,total),h.Font.LineHeight);
        }

        private float GpGlyphPixelHeight(GpFontSpec font,char ch,float fallback)
        {
            if(font==null)return fallback;
            C2LegacyText14.TryEncodeChar(ch,out byte code);
            Sprite sp=LoadGlyphDirect(font,code);
            return sp!=null?sp.rect.height:fallback;
        }

        private float MeasureGpText(string text,GpFontSpec font)
        {
            if(font==null)return 0f;
            byte[] bytes=C2LegacyText14.EncodeCp1251(C2LegacyText14.StripFormatting(text??string.Empty));
            float width=0f;
            for(int i=0;i<bytes.Length;i++)
            {
                int code=bytes[i];
                if(code==0x20||code==0xA0){width+=MeasureGpSpace(font);continue;}
                if(code==0x09){width+=MeasureGpSpace(font)*4f;continue;}
                Sprite sp=LoadGlyphDirect(font,code);
                if(sp!=null) width+=sp.rect.width;
            }
            return width;
        }

        private float MeasureGpSpace(GpFontSpec font)
        {
            Sprite c=LoadGlyphDirect(font,(byte)'c');
            return c!=null&&c.rect.width>0?c.rect.width:4f;
        }

        private Sprite LoadGlyphDirect(GpFontSpec font,int code)
        {
            if(font==null||string.IsNullOrWhiteSpace(font.GPFile)||code<0||code>255)return null;
            string key=font.GPFile+"|"+code;
            if(_glyphCache.TryGetValue(key,out Sprite cached)&&cached!=null)return cached;
            string path=ResolveOriginalFontBankPath(font.GPFile);
            if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))return null;
            if(!_fontBanks.TryGetValue(path,out C2DirectSpriteBank bank)||bank==null)
            {
                bank=new C2DirectSpriteBank();
                if(!bank.Load(path,out string err))
                {
                    Debug.LogWarning($"[C2:BIGMAP VISUAL3 V396A8_3R1] font bank load failed source='{font.GPFile}' path='{path}' err='{err}'");
                    return null;
                }
                _fontBanks[path]=bank;
                Debug.Log($"[C2:BIGMAP VISUAL3 V396A8_3R1] font source='{font.GPFile}' path='{path}' frames={bank.FrameCount} decoder=direct_GN16_CP1251");
            }
            if(code>=bank.FrameCount)return null;
            if(!bank.RenderFrame(code,out C2RenderedFrame frame,out string renderError)||frame==null||frame.Width<=0||frame.Height<=0||frame.Rgba==null)
            {
                Debug.LogWarning($"[C2:BIGMAP VISUAL3 V396A8_3R1] font frame failed source='{font.GPFile}' frame={code} err='{renderError}'");
                return null;
            }
            byte[] rgba=FlipRgbaRowsV396A2(frame.Rgba,frame.Width,frame.Height);
            var tex=new Texture2D(frame.Width,frame.Height,TextureFormat.RGBA32,false); tex.wrapMode=TextureWrapMode.Clamp; tex.filterMode=FilterMode.Point; tex.LoadRawTextureData(rgba); tex.Apply(false,false);
            Sprite sp=Sprite.Create(tex,new Rect(0,0,frame.Width,frame.Height),new Vector2(0,1),1f); sp.name=Path.GetFileNameWithoutExtension(path)+"_glyph_"+code.ToString("D3");
            _glyphCache[key]=sp; return sp;
        }

        private string ResolveOriginalFontBankPath(string gpFile)
        {
            string rel=(gpFile??string.Empty).Trim();
            if(rel.Length==0)return string.Empty;
            string direct=_fs!=null?_fs.ResolvePath(rel+".g16"):string.Empty;
            if(!string.IsNullOrWhiteSpace(direct)&&File.Exists(direct))return direct;
            string normalized=rel.Replace('\\','_').Replace('/','_')+".g16";
            if(_fs!=null&&!string.IsNullOrWhiteSpace(_fs.DataRoot))
            {
                string cache=Path.Combine(_fs.DataRoot,"Cash",normalized); if(File.Exists(cache))return cache;
            }
            string sa=Path.Combine(Application.streamingAssetsPath,"Cossacks2","Data","Cash",normalized); if(File.Exists(sa))return sa;
            return string.Empty;
        }

        private static void SetSourceGpButtonEnabled(Button b,bool enabled)
        {
            if(b==null)return;
            b.interactable=enabled;
            BigMapGpHoverVisual hv=b.GetComponent<BigMapGpHoverVisual>();
            if(hv!=null) hv.ApplyCurrentState(false);
        }

        private static void Place(RectTransform rt,float x,float y,float w,float h){rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);}
    }

    public sealed class BigMapGpHoverVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Background;
        public Button Button;
        public Sprite NormalSprite;
        public Sprite HoverSprite;
        public C2BigMapRenderer14.GpTextHandle GpLabel;
        public Color32 NormalText;
        public Color32 HoverText;
        public Color32 DisabledText;
        public Color32 NormalBackgroundTint = new Color32(255,255,255,255);
        public Color32 HoverBackgroundTint = new Color32(255,255,255,255);
        public Color32 DisabledBackgroundTint = new Color32(230,230,230,230);
        public void ApplyCurrentState(bool hover)
        {
            bool enabled=Button==null || Button.interactable;
            if(Background!=null)
            {
                Sprite sp=enabled && hover && HoverSprite!=null ? HoverSprite : NormalSprite;
                Background.sprite=sp;
                if(sp==null) Background.color=new Color(1f,1f,1f,0.001f);
                else if(enabled) Background.color=hover?HoverBackgroundTint:NormalBackgroundTint;
                else Background.color=DisabledBackgroundTint;
            }
            if(GpLabel!=null && GpLabel.Root!=null)
            {
                Color32 c=enabled ? (hover?HoverText:NormalText) : DisabledText;
                for(int i=0;i<GpLabel.Root.childCount;i++)
                {
                    Image im=GpLabel.Root.GetChild(i).GetComponent<Image>();
                    if(im!=null) im.color=c;
                }
                if(GpLabel.Font!=null) GpLabel.Font.Color=c;
            }
        }
        public void OnPointerEnter(PointerEventData eventData) { ApplyCurrentState(true); }
        public void OnPointerExit(PointerEventData eventData) { ApplyCurrentState(false); }
    }

    public sealed class BigMapHelpHotkeyV396A5 : MonoBehaviour
    {
        private C2BigMapRenderer14 _owner;
        public void Initialize(C2BigMapRenderer14 owner) { _owner = owner; }

        private void Update()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            bool escape = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (kb != null) pressed = kb.f1Key.wasPressedThisFrame || kb.hKey.wasPressedThisFrame;
#else
            bool escape = Input.GetKeyDown(KeyCode.Escape);
            pressed = Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.H);
#endif
            if (pressed) _owner?.ToggleHelpFromHotkeyV396A5();
            else if (escape) _owner?.CloseHelpFromEscapeV396A5();
        }
    }

    public sealed class BigMapPanController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _content;
        private float _viewW,_viewH,_mapW,_mapH;
        private Action<Vector2> _changed;
        private Vector2 _startPointer,_startPos;

        public void Initialize(RectTransform content,float vw,float vh,float mw,float mh,Action<Vector2> changed)
        { _content=content;_viewW=vw;_viewH=vh;_mapW=mw;_mapH=mh;_changed=changed;Clamp(); }
        public void OnBeginDrag(PointerEventData e){_startPointer=e.position;_startPos=_content!=null?_content.anchoredPosition:Vector2.zero;}
        public void OnDrag(PointerEventData e){if(_content==null)return;Vector2 d=e.position-_startPointer;_content.anchoredPosition=_startPos+d;Clamp();_changed?.Invoke(_content.anchoredPosition);}
        public void OnEndDrag(PointerEventData e){if(_content!=null)_changed?.Invoke(_content.anchoredPosition);}
        private void Clamp(){if(_content==null)return;Vector2 p=_content.anchoredPosition;p.x=Mathf.Clamp(p.x,_viewW-_mapW,0);p.y=Mathf.Clamp(p.y,0,_mapH-_viewH);_content.anchoredPosition=p;}
    }
}
