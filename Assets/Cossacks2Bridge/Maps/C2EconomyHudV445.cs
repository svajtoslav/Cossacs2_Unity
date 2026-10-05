using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2EconomyHudBindingV445 : MonoBehaviour
    {
        internal Action Refresh;
        private float next;
        private void Update() { if (Time.unscaledTime < next) return; next=Time.unscaledTime+.1f; Refresh?.Invoke(); }
    }

    public sealed partial class C2GameplayHudV1
    {
        private Image EconomyImageV445(string name,string file,int frame,int x,int y,int w,int h,bool hit)
        {
            var sprite=C2GameplayOriginalSpriteCacheV1.LoadSprite(file,frame,name);
            return AddG16ImageSinglePassV140ALikeOriginal(name,sprite,file,frame,x,y,w,h,255,hit,false,false);
        }
        private void EconomyTooltipV445(GameObject go,string text)
        {
            var relay=go.AddComponent<C2HudTooltipRelayV13I>();relay.Owner=this;
            relay.Item=new C2OriginalProduceItemV13 {DisplayText=text};
        }
        private sealed class MarketViewV445
        {
            internal int Sell=-1, Buy=-1, Amount;
            internal C2SettlementBuildingSelectableV1LikeOriginal Building;
            internal readonly List<Action> Refresh = new List<Action>();
        }
        private bool BuildEconomyPanelV445(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            var village=C2SettlementDipVillageV336LikeOriginal.ForBuildingV445(building);
            if(village!=null) { BuildVillagePanelV445(village); return true; }
            if(!C2MarketEconomyV445.IsMarket(building) || !C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(building.Nation)) return false;
            string path=@"C:\GSC Game World\Cossacks II\Data\Dialogs\v\MARKET.DIALOGSDESK.DIALOGS.XML";
            if(!File.Exists(path)) return false;
            var xml=XElement.Parse(File.ReadAllText(path,Encoding.GetEncoding(1251)));
            var view=new MarketViewV445 { Building=building };
            RenderMarketV445(xml,0,0,view,-1,false);
            var binding=NewUi("market_refresh").AddComponent<C2EconomyHudBindingV445>();
            binding.Refresh=()=>{ foreach(var action in view.Refresh)action(); };
            binding.Refresh();
            return true;
        }
        private static int UIIntV445(XElement e,string key,int fallback=0) => int.TryParse((string)e.Element(key),out int n)?n:fallback;
        private void RenderMarketV445(XElement e,int parentX,int parentY,MarketViewV445 v,int resource,bool selling)
        {
            int x=parentX+UIIntV445(e,"x"),y=parentY+UIIntV445(e,"y"),w=UIIntV445(e,"Width",1),h=UIIntV445(e,"Height",1);
            string action=(string)e.Element("v_ActionID")??"", name=(string)e.Element("Name")??"";
            bool select=action=="Market sell select"||action=="Market buy select";
            if(select){resource=C2ResourceStorageV445.ResourceId(name);selling=action=="Market sell select";}
            string file=(string)e.Element("FileID")??(string)e.Element("GP_File");
            int frame=UIIntV445(e,"SpriteID",UIIntV445(e,"Sprite",UIIntV445(e,"PassiveFrame",UIIntV445(e,"SpritePassive0"))));
            Image picture=null;
            string id="market_"+action.Replace(' ','_')+"_"+name+"_"+x+"_"+y;
            if(!string.IsNullOrEmpty(file)) picture=EconomyImageV445(id,file,frame,x,y,w,h,false);
            if(resource>=0 && name=="active" && picture!=null)
            {
                int r=resource;bool s=selling;var pic=picture;
                v.Refresh.Add(()=>pic.enabled=(s?v.Sell:v.Buy)==r);
            }
            if(resource>=0 && e.Name.LocalName=="VitButton" && picture!=null)
            {
                int r=resource;bool s=selling;var pic=picture;
                Sprite off=pic.sprite,on=C2GameplayOriginalSpriteCacheV1.LoadSprite(file,UIIntV445(e,"SpritePassive1",frame),id+"_selected");
                v.Refresh.Add(()=>pic.sprite=((s?v.Sell:v.Buy)==r&&on!=null)?on:off);
            }
            string message=(string)e.Element("Message");
            if(!string.IsNullOrEmpty(message))
            {
                if(message=="#Buy_Resource")message="Обменять";
                if(message=="#Reset_Resource")message="Сброс";
                Text label=AddLabel(id+"_text",message,x,y+UIIntV445(e,"FontDy"),w,h,13,TextAnchor.MiddleCenter,Color.white);
                label.raycastTarget=false;
                if(action=="Market sell amount")v.Refresh.Add(()=>{
                    C2MarketEconomyV445.Quote(v.Building.Nation,v.Sell,v.Buy,v.Amount,out int sold);
                    label.text=(v.Sell<0?0:Math.Min(v.Amount,C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(v.Building.Nation,v.Sell))).ToString();
                });
                if(action=="Market buy amount")v.Refresh.Add(()=>label.text=C2MarketEconomyV445.Quote(v.Building.Nation,v.Sell,v.Buy,v.Amount,out _).ToString());
            }
            // Children are drawn before a single transparent action hit area so
            // every part of a resource icon responds to the same selection.
            foreach(var child in e.Element("ChildDialogs")?.Elements()??Enumerable.Empty<XElement>()) RenderMarketV445(child,x,y,v,resource,selling);
            if(select || action=="Market BUY" || action=="Market reset" || action=="Market change amount")
            {
                var hit=AddSolid(id+"_hit",new Color(0,0,0,0),x,y,w,h,true);
                var button=hit.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
                int r=resource,step=UIIntV445(e,"ID");bool s=selling;
                button.onClick.AddListener(()=>{
                    if(v.Building==null||!C2ResourceStorageV445.Ready(v.Building)||!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(v.Building.Nation))return;
                    if(select){if(s){v.Sell=r;if(v.Buy==r)v.Buy=-1;}else if(v.Sell!=r)v.Buy=r;}
                    else if(action=="Market reset")v.Amount=0;
                    else if(action=="Market change amount") {
                        bool shift=false;
#if ENABLE_INPUT_SYSTEM
                        var keyboard=UnityEngine.InputSystem.Keyboard.current;
                        shift=keyboard!=null&&(keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed);
#else
                        shift=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
#endif
                        v.Amount=Mathf.Clamp(v.Amount+step*(shift?100:1),0,500000);
                    }
                    else if(C2MarketEconomyV445.Exchange(v.Building,v.Building.Nation,v.Sell,v.Buy,v.Amount))v.Amount=0;
                    foreach(var refresh in v.Refresh)refresh();
                });
                if(action=="Market BUY")v.Refresh.Add(()=>button.interactable=C2MarketEconomyV445.Quote(v.Building.Nation,v.Sell,v.Buy,v.Amount,out _)>0);
            }
        }
        private void BuildVillageLeftCardV447(C2SettlementDipVillageV336LikeOriginal village)
        {
            // AlexVisualSystem.cpp::AddStlInfoPanel/AddBaseInfoPanel. These are
            // native GP pixel coordinates, relative to RealLy-1, not cropped art.
            int portrait=village.ResourceV445==3?83:village.ResourceV445==4?82:village.ResourceV445==5?84:81;
            var frame=VillageNativeImageV448("village_card_box","Interf3\\FormInterface",26,0,463,false);
            int top=767-Mathf.RoundToInt(frame.sprite.rect.height);
            Place(frame.rectTransform,0,top,Mathf.RoundToInt(frame.sprite.rect.width),Mathf.RoundToInt(frame.sprite.rect.height));
            var picture=VillageNativeImageV448("village_portrait","Interf3\\build",portrait,25,top+47,true);
            Color32 black=new Color32(0x2E,0x23,0x17,255),yellow=new Color32(0xD4,0xC1,0x9C,255);
            VillageLabelV448("village_card_title",C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal("Setl_Settlement"),90,top+21,"FontG12",yellow,true);
            VillageLabelV448("village_resource_label",C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal("Setl_Goods"),50,top+216,"FontG12",black);
            VillageNativeImageV448("village_resource_icon","Interf3\\ResPanel",10+village.ResourceV445,97,top+203,false);
            VillageLabelV448("village_workers_label",C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal("Setl_Workers"),40,top+236,"FontG12",black);
            var workers=VillageLabelV448("village_workers","",120,top+238,"FontG12",black,true);
            VillageLabelV448("village_owner_label",C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal("Setl_Owner"),37,top+266,"FontG12",black);
            var owner=VillageLabelV448("village_owner","",93,top+268,"FontC12",black);
            // Extra diagnostic/economic detail belongs in a hover hint, not in
            // a synthetic permanent panel that covers the original command area.
            EconomyTooltipV445(picture.gameObject,"");
            var hint=picture.GetComponent<C2HudTooltipRelayV13I>();
            var binding=workers.gameObject.AddComponent<C2EconomyHudBindingV445>();
            binding.Refresh=()=>{
                if(village==null)return;
                workers.Value=village.WorkerCountV445+"/"+village.WorkerLimitV445;
                owner.Value=village.OwnerV445==7?"Нейтрал":"Игрок "+(village.OwnerV445+1);
                hint.Item.DisplayText="Ополчение: "+village.DefendersV447+"\nЗапас: "+village.StockV445+" / "+village.StockLimitV445+
                    "\nДобыча: "+village.PerMinuteV447.ToString("0.#")+"/мин\nОбоз: "+village.CargoV445+" — "+village.CaravanStatusV447+
                    (village.AttackedV447?"\nСело атаковано — работа остановлена":"");
            };
            binding.Refresh();
        }

        private Image VillageNativeImageV448(string name,string file,int frame,int x,int y,bool hit)
        {
            var sprite=C2GameplayOriginalSpriteCacheV1.LoadSprite(file,frame,name);
            return AddG16ImageSinglePassV140ALikeOriginal(name,sprite,file,frame,x,y,
                Mathf.RoundToInt(sprite.rect.width),Mathf.RoundToInt(sprite.rect.height),255,hit,false,false);
        }
        private C2HudBitmapTextV448 VillageLabelV448(string name,string value,int x,int y,string font,Color color,bool centered=false)
        {
            var go=NewUi(name);Place(go.GetComponent<RectTransform>(),x,y,1,16);
            var label=go.AddComponent<C2HudBitmapTextV448>();label.raycastTarget=false;
            label.Centered=centered;label.color=color;label.SetFont(font);label.Value=value;return label;
        }

        private void BuildVillagePanelV445(C2SettlementDipVillageV336LikeOriginal village)
        {
            if(!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(village.OwnerV445))return;
            var binding=NewUi("village_economy").AddComponent<C2EconomyHudBindingV445>();
            var refresh=new List<Action>();
            for(int i=0;i<village.UpgradesV445.Count;i++)
            {
                int index=i;var upgrade=village.UpgradesV445[i];if(upgrade.Done)continue;
                // Source draws later entries over earlier entries at the same
                // cell (successive tiers). Show the top unfinished tier only.
                if(village.UpgradesV445.Skip(i+1).Any(u=>!u.Done&&u.X==upgrade.X&&u.Y==upgrade.Y))continue;
                int x=55+upgrade.X*65,y=767-135+upgrade.Y*65;
                var box=VillageNativeImageV448("village_upgrade_box_"+i,"Interf3\\FormInterface",18,x,y,false);
                var icon=VillageNativeImageV448("village_upgrade_"+i,upgrade.IconFile,upgrade.Icon,x+3,y+4,true);
                var normal=box.sprite;
                var running=C2GameplayOriginalSpriteCacheV1.LoadSprite("Interf3\\FormInterface",19,"village_upgrade_running");
                // Native Canvas is relative to RealLy, while the GP frame is
                // relative to RealLy-1: the bar starts one pixel lower.
                var progress=AddSolidSinglePassV140ALikeOriginal("village_upgrade_progress_"+i,new Color32(0x20,255,0x20,255),x+59,y+8,2,46,false);
                progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Vertical;progress.fillOrigin=0;
                var progressEdge=AddSolidSinglePassV140ALikeOriginal("village_upgrade_progress_edge_"+i,Color.black,x+61,y+8,1,46,false);
                progressEdge.type=Image.Type.Filled;progressEdge.fillMethod=Image.FillMethod.Vertical;progressEdge.fillOrigin=0;
                var button=icon.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
                button.onClick.AddListener(()=>village.BuyUpgradeV445(index,village.OwnerV445));
                string hint=C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal(upgrade.Hint);
                EconomyTooltipV445(icon.gameObject,hint+"\n"+C2UpgradeEconomyV445.CostText(upgrade.Price));
                refresh.Add(()=>{
                    bool available=village.CanUpgradeV447(index,village.OwnerV445);
                    button.interactable=available;
                    C2HudAvailabilityV453.Apply(icon,available||upgrade.Running);
                    var sprite=upgrade.Running?running:normal;
                    if(box.sprite!=sprite){box.sprite=sprite;Place(box.rectTransform,x,y,Mathf.RoundToInt(sprite.rect.width),Mathf.RoundToInt(sprite.rect.height));}
                    progress.enabled=upgrade.Running;progress.fillAmount=Mathf.Clamp01((Time.time-upgrade.Started)/upgrade.Duration);
                    progressEdge.enabled=upgrade.Running;progressEdge.fillAmount=progress.fillAmount;
                });
            }
            binding.Refresh=()=>{if(village!=null)foreach(var action in refresh)action();};binding.Refresh();
        }
    }
}
