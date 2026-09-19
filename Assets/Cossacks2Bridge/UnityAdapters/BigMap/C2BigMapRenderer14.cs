using System;
using System.Collections.Generic;
using System.IO;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;
using Cossacks2Bridge.UnityAdapters.Profiles;
using Cossacks2Bridge.UnityAdapters.Renderers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TemnyLessCodec;

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
        private CoreFileSystem _fs;
        private LocDb _loc;
        private BaseUiRenderer.RenderOptions _opt;
        private RectTransform _root;
        private C2BigMapData14.Data _data;
        private C2ProfileRuntime14.ProfileRecord _profile;
        private RectTransform _mapContent;
        private TextMeshProUGUI _sectorTitle;
        private TextMeshProUGUI _sectorInfo;
        private Image _sectorPreview;
        private Image _nationFlag;
        private readonly TextMeshProUGUI[] _resourceText = new TextMeshProUGUI[7];
        private Image _headerNationFlag;
        private Button _defenceButton;
        private TextMeshProUGUI _turnText;
        private readonly List<GameObject> _pagePanels = new List<GameObject>();
        private int _selectedSector;
        private int _activePage;
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
            C2BigMapData14.EnsureCampaignInitialized(_profile, _data);
            _selectedSector = Mathf.Clamp(_profile.m_iCurSecId, 0, Math.Max(0, _data.Sectors.Count - 1));
            _activePage = Mathf.Clamp(_profile.m_iCurMenuId, 0, C2Bfe14ContractV396A.MainPageCount - 1);

            _root = CreateCanvas("C2_BigMapCanvas", opt);
            BuildBackground();
            BuildCampaignHeader();
            BuildResourceHeader();
            BuildMap();
            BuildRightInfo();
            BuildPages();
            BuildBottomButtons();
            BigMapHelpHotkeyV396A5 hotkey = _root.gameObject.AddComponent<BigMapHelpHotkeyV396A5>();
            hotkey.Initialize(this);
            RefreshAll();

            Debug.Log($"[C2:BIGMAP V396A] ProcessBigMap shell ready profile='{_profile.m_chName}' campaignNation={_profile.campaignNation} sectors={_data.Sectors.Count} map={_data.MapWidth}x{_data.MapHeight} battleRuntime=disabled aiRuntime=disabled dataStatus={_data.Audit?.Status ?? "UNKNOWN"}");
            Debug.Log($"[C2:BIGMAP V396A6] g16Orientation=flipY flagFrame=countryId+1 flagCountries=9 secondMiniAtlasStart=24 pixelStage=1024x768 pointFilter=YES screen={Screen.width}x{Screen.height}");
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
            // V396A6: keep the original 1024x768 pixel grid.  Expand gives scale=1
            // for a 1044x768 Free-Aspect GameView instead of the fractional ~1.0097
            // produced by MatchWidthOrHeight.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024,768);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform screen = go.GetComponent<RectTransform>();
            screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one; screen.offsetMin = Vector2.zero; screen.offsetMax = Vector2.zero;

            var stageGo = new GameObject("C2_BigMapStage_1024x768", typeof(RectTransform));
            stageGo.transform.SetParent(screen, false);
            RectTransform rt = (RectTransform)stageGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
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
            string p = ResolveAsset(@"Interf3\TotalWarGraph\lva_Turn_Map.bmp", "lva_Turn_Map.bmp");
            if (File.Exists(p))
            {
                var tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.LoadImage(File.ReadAllBytes(p)); ri.texture = tex;
            }
        }

        private void BuildCampaignHeader()
        {
            string fmt = Resolve("#CWT_BigMapHead");
            string head = string.IsNullOrWhiteSpace(fmt) ? _profile.m_chName : fmt.Replace("%s", _profile.m_chName ?? string.Empty);
            // DrawMultilineText control commands (for example {C}) are not literal UI text.
            head = C2LegacyText14.StripFormatting(head);
            MakeText(_root, "BigMapCampaignHead", 245, 58, 520, 28, head, 17, TextAlignmentOptions.Center, new Color32(239,222,188,255));
            _headerNationFlag = MakeImage(_root, "BigMapCampaignFlag", 770, 57, 32, 24);
            int flagFrame = (_profile.campaignNation >= 0 && _profile.campaignNation < C2Bfe14ContractV396A.CountryCount)
                ? _profile.campaignNation + 1
                : 0;
            _headerNationFlag.sprite = LoadGp(@"Interf3\TotalWarGraph\lva_Flags", Mathf.Clamp(flagFrame, 0, C2Bfe14ContractV396A.CountryCount));
            _headerNationFlag.enabled = _headerNationFlag.sprite != null;

            _turnText = MakeText(_root, "BigMapTurn", 824, 58, 120, 26, string.Empty, 16, TextAlignmentOptions.Center, new Color32(160,45,35,255));
        }

        private void BuildResourceHeader()
        {
            // Original CResPanel_BM::CreateElements(): x = 75 + 130*res,
            // GP Interf3\res_pic frame res+7, text at icon.x1+4 / y=7.
            for (int res = 0; res < 7; res++)
            {
                float x0 = 75 + 130 * res;
                Sprite icon = LoadGp(@"Interf3\res_pic", res + 7);
                float tx = x0 + 54;
                if (icon != null)
                {
                    Image im = MakeImage(_root, "BigMapResIcon" + res, x0, -4, icon.rect.width, icon.rect.height);
                    im.sprite = icon;
                    im.enabled = true;
                    tx = x0 + icon.rect.width + 4;
                }
                _resourceText[res] = MakeText(_root, "BigMapRes" + res, tx, 7, 70, 24, "0", 16, TextAlignmentOptions.Left, new Color32(225,220,200,255));
            }
        }

        private void BuildMap()
        {
            // Exact ProcessBigMap map viewport: x=75, y=113, w=600, h=530.
            var vpGo = new GameObject("BigMapViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(BigMapPanController));
            vpGo.transform.SetParent(_root,false);
            RectTransform vp = (RectTransform)vpGo.transform; Place(vp,75,113,600,530);
            Image vi = vpGo.GetComponent<Image>(); vi.color = new Color(1,1,1,0.001f); vi.raycastTarget = true;

            var contentGo = new GameObject("BigMapContent", typeof(RectTransform));
            contentGo.transform.SetParent(vp,false);
            _mapContent = (RectTransform)contentGo.transform;
            _mapContent.anchorMin = _mapContent.anchorMax = new Vector2(0,1); _mapContent.pivot = new Vector2(0,1);
            _mapContent.anchoredPosition = new Vector2(_profile.m_iCurMX0, -_profile.m_iCurMY0);
            _mapContent.sizeDelta = new Vector2(_data.MapWidth, _data.MapHeight);

            // Original CPicesPict(a_dsMenu,3,3,0x50): nine frames, row-major.
            float[] colW = new float[3]; float[] rowH = new float[3];
            Sprite[] pieces = new Sprite[9];
            for (int i=0;i<9;i++)
            {
                pieces[i] = LoadGp(@"Interf3\TotalWarGraph\lva_Europe00", i);
                if (pieces[i] != null)
                {
                    int row=i/3,col=i%3;
                    colW[col]=Mathf.Max(colW[col],pieces[i].rect.width);
                    rowH[row]=Mathf.Max(rowH[row],pieces[i].rect.height);
                }
            }
            float[] colX={0,colW[0],colW[0]+colW[1]};
            float[] rowY={0,rowH[0],rowH[0]+rowH[1]};
            for (int i=0;i<9;i++)
            {
                if (pieces[i]==null) continue;
                int row=i/3,col=i%3;
                var g=new GameObject("EuropePiece_"+i,typeof(RectTransform),typeof(Image)); g.transform.SetParent(_mapContent,false);
                RectTransform rt=(RectTransform)g.transform; Place(rt,colX[col],rowY[row],pieces[i].rect.width,pieces[i].rect.height);
                Image im=g.GetComponent<Image>(); im.sprite=pieces[i]; im.preserveAspect=false; im.raycastTarget=false; im.color=Color.white;
            }

            BuildSectorMarkers();
            BuildGeneralMarkers();

            BigMapPanController pan = vpGo.GetComponent<BigMapPanController>();
            pan.Initialize(_mapContent, 600,530,_data.MapWidth,_data.MapHeight, OnMapPanChanged);
        }

        private void BuildSectorMarkers()
        {
            foreach (var s in _data.Sectors)
            {
                int id=s.Id;

                // Original CSectStatData::SetSityType + DeposeTo: population
                // frame centered exactly on SityXY.
                Sprite city=LoadGp(@"Interf3\TotalWarGraph\bmPopulat", Mathf.Clamp(s.Population,0,2));
                float cw=city!=null?city.rect.width:18f, ch=city!=null?city.rect.height:18f;
                var go = new GameObject("SectorCity_"+s.Id, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_mapContent,false);
                RectTransform rt=(RectTransform)go.transform;
                Place(rt,s.CityX-cw/2f,s.CityY-ch/2f,cw,ch);
                Image img=go.GetComponent<Image>();
                if(city!=null){img.sprite=city;img.color=Color.white;} else img.color=new Color(0.95f,0.85f,0.55f,0.9f);
                img.preserveAspect=true;
                go.GetComponent<Button>().onClick.AddListener(()=>SelectSector(id));

                // Original fort/defence marker uses bmDefence frame=m_inDefence
                // and is centered on FortXY.
                Sprite fort=LoadGp(@"Interf3\TotalWarGraph\bmDefence", Mathf.Clamp(s.Defence,0,3));
                if(fort!=null)
                {
                    var fg=new GameObject("SectorFort_"+s.Id,typeof(RectTransform),typeof(Image),typeof(Button));
                    fg.transform.SetParent(_mapContent,false);
                    RectTransform fr=(RectTransform)fg.transform;
                    Place(fr,s.FortX-fort.rect.width/2f,s.FortY-fort.rect.height/2f,fort.rect.width,fort.rect.height);
                    Image fim=fg.GetComponent<Image>(); fim.sprite=fort; fim.color=Color.white; fim.preserveAspect=true;
                    fg.GetComponent<Button>().onClick.AddListener(()=>SelectSector(id));
                }
            }
        }

        private void BuildGeneralMarkers()
        {
            // Until tactical battles/AI are ported, each of the six original
            // campaign countries keeps its initial commander in its capital.
            Sprite army=LoadGp(@"Interf3\TotalWarGraph\lva_Army",0);
            for(int nation=0;nation<C2Bfe14ContractV396A.CountryCount;nation++)
            {
                C2BigMapData14.SectorDefinition cap=null;
                foreach(var s in _data.Sectors) if(s.Owner==nation){cap=s;break;}
                if(cap==null) continue;
                var go=new GameObject("General_"+nation,typeof(RectTransform),typeof(Image)); go.transform.SetParent(_mapContent,false);
                RectTransform rt=(RectTransform)go.transform; Place(rt,cap.FortX-18,cap.FortY-40,42,88);
                Image im=go.GetComponent<Image>(); im.sprite=army; im.preserveAspect=true; im.raycastTarget=false;
                im.color=nation==_profile.campaignNation?Color.white:new Color(0.86f,0.86f,0.86f,0.95f);
            }
        }

        private void BuildRightInfo()
        {
            _sectorTitle=MakeText(_root,"SectorTitle",705,162,210,35,"",22,TextAlignmentOptions.Left,new Color32(135,35,28,255));
            _nationFlag=MakeImage(_root,"CampaignFlag",905,164,32,24);
            _sectorPreview=MakeImage(_root,"SectorPreview",707,215,230,115);
            _sectorInfo=MakeText(_root,"SectorInfo",705,340,240,175,"",14,TextAlignmentOptions.TopLeft,new Color32(80,65,55,255));
            _defenceButton=MakeButton(_root,"UpgradeDefence",707,528,232,25,Resolve("#UpgradeDef#"),14,OnUpgradeDefence);
        }

        private void BuildPages()
        {
            string[] keys={"#CWV_WorldMap","#CWV_Diplomacy","#CWV_Personal","#CWV_Market","#CWV_Messages"};
            float x=75;
            for(int i=0;i<5;i++)
            {
                int page=i; string label=Resolve(keys[i]);
                Button b=MakeButton(_root,"Page_"+i,x,704,112,27,label,13,()=>SetPage(page));
                x+=111;
            }

            // Non-map pages are deliberately implemented as campaign-layer
            // shells now; tactical battle/AI is not invoked.
            for(int i=1;i<5;i++)
            {
                var panel=new GameObject("BigMapPagePanel_"+i,typeof(RectTransform),typeof(Image)); panel.transform.SetParent(_root,false);
                RectTransform rt=(RectTransform)panel.transform; Place(rt,75,113,600,530);
                Image bg=panel.GetComponent<Image>(); bg.color=new Color(0.92f,0.89f,0.82f,0.94f); bg.raycastTarget=true;
                string title=i==1?Resolve("#CWV_Diplomacy"):i==2?Resolve("#CWV_Personal"):i==3?Resolve("#CWV_Market"):Resolve("#CWV_Messages");
                string body;
                if(i==1) body="Дипломатия\n\nСтраны, отношения и договоры будут использовать сохранённое состояние кампании. Боевой ИИ пока отключён.";
                else if(i==2) body="Командиры\n\nНа карте уже созданы стартовые генералы стран кампании. Перемещение и состав армий будут расширяться без запуска тактического боя.";
                else if(i==3) body="Рынок\n\nЭкономические ресурсы режима уже считаются из BigMapConst.dat. Торговые операции будут подключены к этим же значениям.";
                else body="Сообщения\n\nЖурнал событий кампании. Пока фиксируются смена хода, выбор сектора и блокированные боевые переходы.";
                MakeText(rt,"PageText",30,30,540,440,title+"\n\n"+body,18,TextAlignmentOptions.TopLeft,new Color32(80,45,35,255));
                _pagePanels.Add(panel);
            }
        }

        private void BuildBottomButtons()
        {
            MakeButton(_root,"EndTurn",289,713,210,38,Resolve("#CWB_EndOfTurn"),18,OnEndTurn);
            MakeButton(_root,"QuitCampaign",532,713,210,38,Resolve("#CWB_Quit"),18,OnQuit);
        }

        private void RefreshAll()
        {
            RefreshResources(); RefreshSector(); SetPage(_activePage);
        }

        private void RefreshResources()
        {
            int[] r=_profile.resources ?? new int[7];
            for(int i=0;i<_resourceText.Length;i++) if(_resourceText[i]!=null) _resourceText[i].text=(i<r.Length?r[i]:0).ToString();
            if (_turnText != null) _turnText.text = Resolve("#CWT_CurrentTurn") + " " + (_profile.m_inCurTurn + 2).ToString();
        }

        private void SelectSector(int id)
        {
            _selectedSector=id; _profile.m_iCurSecId=id; C2ProfileRuntime14.SaveCurrent(); RefreshSector();
            Debug.Log($"[C2:BIGMAP V395] sector selected id={id}");
        }

        private void RefreshSector()
        {
            var s=C2BigMapData14.SectorById(_data,_selectedSector); if(s==null)return;
            var ps=C2BigMapData14.StateFor(_profile,_selectedSector);
            string nm=Resolve(s.SectorName); if(string.IsNullOrWhiteSpace(nm)||nm==s.SectorName) nm=s.SectorName;
            _sectorTitle.text=nm;
            if (_sectorPreview != null)
            {
                string miniAtlas = s.Id >= C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart
                    ? @"Interf3\TotalWarGraph\lva_SectMiniMP2"
                    : @"Interf3\TotalWarGraph\lva_SectMiniMP";
                int miniFrame = s.Id >= C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart
                    ? s.Id - C2Bfe14ContractV396A.SectorSecondMinimapAtlasStart
                    : s.Id;
                _sectorPreview.sprite = LoadGp(miniAtlas, Mathf.Max(0, miniFrame));
                _sectorPreview.enabled = _sectorPreview.sprite != null;
            }

            int owner=ps!=null?ps.owner:s.Owner; int def=ps!=null?ps.defence:s.Defence; int pop=ps!=null?ps.population:s.Population; int rec=ps!=null?ps.recruits:(pop+1)*C2BigMapData14.Get(_data,"#SECT_MAX_RECRTS",120);
            if (_nationFlag != null)
            {
                // Original 1.1 and final 1.4 use lva_Flags frame = countryId + 1.
                // Frame 0 is not country 0.  V396A accidentally used owner directly,
                // which made France display the English flag and shifted every country.
                int ownerFlagFrame = (owner >= 0 && owner < C2Bfe14ContractV396A.CountryCount) ? owner + 1 : 0;
                _nationFlag.sprite = LoadGp(@"Interf3\TotalWarGraph\lva_Flags", ownerFlagFrame);
                _nationFlag.enabled = _nationFlag.sprite != null;
            }
            string ownerName=Resolve(C2BigMapData14.CampaignNationTextKey(owner));
            string resourceName=s.Resource==1?"Пища":s.Resource==2?"Золото":s.Resource==3?"Железо":s.Resource==4?"Уголь":"Нет";
            _sectorInfo.text=$"Владелец: {ownerName}\nСектор: {nm}\n\nНаселение: {pop}\nРекруты: {rec}/{(pop+1)*C2BigMapData14.Get(_data,"#SECT_MAX_RECRTS",120)}\nДеревни: {s.Villages}\nРесурс: {resourceName}\n\nЗащита сектора: {def}\n\nДоход за ход:\n+{C2BigMapData14.Get(_data,"#SECT_INCOME_WOOD",750)} дерево\n+{C2BigMapData14.Get(_data,"#SECT_INCOME_FOOD",2000)} пища\n+{C2BigMapData14.Get(_data,"#SECT_INCOME_STONE",750)} камень\n+{C2BigMapData14.Get(_data,"#SECT_INCOME_GOLD",500)} золото";

            if (_defenceButton != null)
            {
                bool own = owner == _profile.campaignNation;
                bool allowed = own && def < 3 && def <= pop;
                _defenceButton.gameObject.SetActive(own);
                _defenceButton.interactable = allowed;
            }
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

        private void SetPage(int page)
        {
            _activePage=Mathf.Clamp(page,0,C2Bfe14ContractV396A.MainPageCount - 1);
            _profile.m_iCurMenuId=_activePage;
            C2ProfileRuntime14.SaveCurrent();
            if(_mapContent!=null && _mapContent.parent!=null) _mapContent.parent.gameObject.SetActive(_activePage==0);
            for(int i=0;i<_pagePanels.Count;i++) if(_pagePanels[i]!=null) _pagePanels[i].SetActive(_activePage==i+1);
            if(_sectorTitle!=null) _sectorTitle.transform.parent.gameObject.SetActive(true);
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

        private Button MakeButton(RectTransform parent,string name,float x,float y,float w,float h,string text,float size,UnityEngine.Events.UnityAction click)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(Outline));go.transform.SetParent(parent,false);RectTransform rt=(RectTransform)go.transform;Place(rt,x,y,w,h);
            Image im=go.GetComponent<Image>();im.color=new Color(0.34f,0.06f,0.04f,0.90f);Outline ol=go.GetComponent<Outline>();ol.effectColor=new Color(0.80f,0.66f,0.32f,0.95f);ol.effectDistance=new Vector2(1,-1);
            Button b=go.GetComponent<Button>();b.targetGraphic=im;if(click!=null)b.onClick.AddListener(click);
            TextMeshProUGUI t=MakeText(rt,"Text",0,0,w,h,text,size,TextAlignmentOptions.Center,new Color32(239,222,188,255));
            return b;
        }

        private static void Place(RectTransform rt,float x,float y,float w,float h){rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);}
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
