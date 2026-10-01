using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private sealed class ArtilleryUiV439
        {
            internal C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            internal string Action,File,Hotkey;
            internal int Id,Sprite,X,Y,W,H;
            internal Image Image,Shade;
            internal RectTransform Click;
        }
        private readonly List<ArtilleryUiV439> _artilleryUiV439=new List<ArtilleryUiV439>();
        private static DialogNode _artilleryXmlV439;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _artilleryAimV439;

        private bool BuildArtilleryPanelV439(C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info,int count)
        {
            if(_artilleryXmlV439==null)
            {
                foreach(string root in C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal())
                {
                    string path=Path.Combine(root,"Dialogs","v","Weapons.DialogsDesk.Dialogs.xml");
                    if(!File.Exists(path))continue;
                    _artilleryXmlV439=DialogNode.Parse(File.ReadAllText(path));
                    Debug.Log("[C2 V439 HUD XML] "+path+" cannon="+ContainsActionV125LikeOriginal(_artilleryXmlV439,"va_W_CannonSet"));break;
                }
            }
            if(_artilleryXmlV439==null)throw new InvalidOperationException("Missing original Weapons.DialogsDesk.Dialogs.xml");
            int shift=Mathf.Max(0,count-1)*OriginalSelPointSideWidthV137LikeOriginal;
            RenderArtilleryXmlV439(_artilleryXmlV439,shift,0,false,unit,info);
            // BASE.UA.xml CANNONAUTOSHOT: original ability icon, separate from
            // the one-shot/point-fire weapon card.
            int ax=shift+184+108,ay=724;
            var autoImage=AddG16Image("artillery_v442_auto","Interf3/f_icons",17,ax,ay,36,36,255,false,false,false);
            if(autoImage!=null){
                var go=NewUi("artillery_v442_auto_click");var hit=go.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
                var rect=go.GetComponent<RectTransform>();Place(rect,ax,ay,36,36);
                var relay=go.AddComponent<C2ArtilleryHudClickV439>();relay.Owner=this;relay.Action="auto";
                _artilleryUiV439.Add(new ArtilleryUiV439{Unit=unit,Action="auto",File="Interf3/f_icons",Sprite=17,Image=autoImage,Click=rect,X=ax,Y=ay,W=36,H=36});
            }
            Debug.Log("[C2 V439 HUD XML] bindings="+_artilleryUiV439.Count+" root="+_artilleryXmlV439.Children[0].Name);
            // One input consumer per HUD, also when several artillery cards are built.
            var input=GetComponent<C2ArtilleryHudInputV439>();
            if(input==null)input=gameObject.AddComponent<C2ArtilleryHudInputV439>();
            input.Owner=this;
            RefreshArtilleryPanelV439();
            return true;
        }

        private void RenderArtilleryXmlV439(DialogNode node,int ox,int oy,bool cannonTree,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,C2OriginalProduceCatalogV13.C2MdIconInfoV13 info)
        {
            int x=ox+node.Int("x",0),y=oy+node.Int("y",0),w=node.Int("Width",0),h=node.Int("Height",0);
            string action=NodeHasActionV125LikeOriginal(node,"va_W_CannonSet")?"set":
                NodeHasActionV125LikeOriginal(node,"va_CannonFire")?"fire":
                NodeHasActionV125LikeOriginal(node,"va_CannonReload")?"reload":
                NodeHasActionV125LikeOriginal(node,"va_CannonReloadStage")?"stage":
                NodeHasActionV125LikeOriginal(node,"cva_SP_Stop_Cannon")?"stop":
                NodeHasActionV125LikeOriginal(node,"va_CannonFillUnits")?"fill":null;
            cannonTree|=action=="set";
            // Build the subtree even for an empty gun. Visibility is refreshed
            // after crew changes without requiring the player to reselect it.
            bool draw=cannonTree||action!=null;
            // Unlink still requires the connection order. Fill visibility follows TestFillingAbility.
            if(draw && (node.Name=="GPPicture" || action=="stage") && w>0 && h>0)
            {
                string file=node.TextOf("FileID");int sprite=node.Int("SpriteID",0);
                if(action==null && file.EndsWith("BigWeapon",StringComparison.OrdinalIgnoreCase))
                {file=info.BigFireWeaponFile;sprite=info.BigFireWeaponSprite;}
                string name="artillery_v439_"+(action??"picture")+"_"+x+"_"+y;
                Image image=action=="stage"
                    ?AddSolidSinglePassV140ALikeOriginal(name,Color.green,x,y,w,h,false)
                    :AddG16Image(name,file,sprite,x,y,w,h,255,false,false,false);
                if(image!=null)
                {
                    var binding=new ArtilleryUiV439{Unit=unit,Action=action??"picture",Id=node.Int("ID",0),
                        File=file,Sprite=sprite,X=x,Y=y,W=w,H=h,Image=image,Hotkey=node.TextOf("HotKey")};
                    if(action=="reload")
                    {
                        DialogNode shade=ArtilleryFirstPictureV439(node);
                        if(shade!=null)binding.Shade=AddG16Image(name+"_shade",shade.TextOf("FileID"),shade.Int("SpriteID",0),
                            x+shade.Int("x",0),y+shade.Int("y",0),shade.Int("Width",34),shade.Int("Height",34),255,false,false,false);
                    }
                    if(action!=null && action!="stage")
                    {
                        var go=NewUi(name+"_click");var hit=go.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
                        binding.Click=go.GetComponent<RectTransform>();Place(binding.Click,x,y,w,h);
                        var relay=go.AddComponent<C2ArtilleryHudClickV439>();relay.Owner=this;relay.Action=action;relay.Id=binding.Id;
                    }
                    _artilleryUiV439.Add(binding);
                }
                if(action=="reload")return; // shade was bound above, once.
            }
            // Only actual dialog child containers carry layout; skip v_Actions,
            // transforms and scalar metadata so they cannot introduce controls.
            foreach(var child in node.Children)
                if(child.Name=="ChildDialogs" || child.Name=="DialogsDesk" || child.Name=="GPPicture" || child.Name=="Canvas" || child.Name=="RootBlock")
                    RenderArtilleryXmlV439(child,x,y,cannonTree,unit,info);
        }

        private static DialogNode ArtilleryFirstPictureV439(DialogNode node)
        {
            foreach(var children in node.Children)if(children.Name=="ChildDialogs")
                foreach(var child in children.Children)if(child.Name=="GPPicture")return child;
            return null;
        }

        private void RefreshArtilleryPanelV439()
        {
            foreach(var b in _artilleryUiV439)
            {
                var rt=b.Unit!=null?b.Unit.RuntimeLinkCachedLikeOriginal?.Runtime:null;
                bool valid=C2UnitOriginalRuntimeAndRendererV1.GetArtilleryChargeV439(rt,out int type,out int percent);
                bool own=valid && C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(b.Unit.Nation);
                bool show=valid && !C2UnitOriginalRuntimeAndRendererV1.CannonNeedsCrewV439(rt);
                if(b.Shade!=null)b.Shade.enabled=false;
                if(b.Action=="reload" && show)
                {
                    var traits=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(b.Unit);
                    int n=0;for(int i=0;i<3;i++)if(traits.MaxDamage[i]+C2CombatCoreV408LikeOriginal.WeaponIndexV439(traits.WeaponKind[i])>0)n++;
                    show=n>1 && traits.MaxDamage[b.Id]+C2CombatCoreV408LikeOriginal.WeaponIndexV439(traits.WeaponKind[b.Id])>0;
                    if(b.Shade!=null)b.Shade.enabled=show && type!=b.Id;
                }
                if(b.Action=="fill")show=valid && own && C2UnitOriginalRuntimeAndRendererV1.CannonNeedsCrewV439(rt);
                if(b.Action=="stop" || b.Action=="auto")show &= own;
                if(b.Image!=null)
                {
                    b.Image.enabled=show;b.Image.color=new Color(1,1,1,own?1:128f/255);
                    if(b.Action=="stage")
                    {
                        int height=b.H*percent/100;
                        b.Image.enabled=show&&height>0;b.Image.color=Color.green;
                        Place(b.Image.rectTransform,b.X,b.Y+b.H-height,b.W,height);
                    }
                    if(b.Action=="auto")b.Image.color=rt!=null&&rt.ArtilleryAutoFireV442?Color.white:new Color(.5f,.5f,.5f,1);
                    if(b.Action=="fire")b.Image.sprite=C2GameplayOriginalSpriteCacheV1.LoadSprite(b.File,b.Sprite+(rt?.ArtilleryPointOrderV439!=null?1:0),"cannon_fire");
                }
                if(b.Click!=null)b.Click.gameObject.SetActive(show);
                if(b.Action=="fire" && (!valid || type!=0))_artilleryAimV439=null;
            }
            if(_artilleryAimV439!=null && (!_artilleryAimV439.IsSelected || _artilleryAimV439.IsDeadLikeOriginal))_artilleryAimV439=null;
        }

        internal void ArtilleryCommandV439(string action,int id,bool right=false)
        {
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            // Only va_CannonFire defines RightClick (CancelAttack). A right
            // click on a charge selector must not silently issue Stop.
            if(right && action!="fire")return;
            var selected=FirstSelectedUnit();
            if(selected==null || !C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(selected.Nation))return;
            var first=selected.RuntimeLinkCachedLikeOriginal?.Runtime;
            if(!C2UnitOriginalRuntimeAndRendererV1.GetArtilleryChargeV439(first,out int type,out int percent))return;
            if(action=="fire" && !right && type==0){_artilleryAimV439=selected;return;}
            _artilleryAimV439=null;
            foreach(var unit in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
            {
                if(unit==null || !unit.IsSelected || !C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(unit.Nation))continue;
                var link=unit.RuntimeLinkCachedLikeOriginal;if(link?.Runtime?.OriginalComplexObjectV430LikeOriginal==null)continue;
                if(action=="stop" || right)link.Owner.StopArtilleryV439(link.Runtime);
                else if(action=="auto")link.Owner.ToggleArtilleryAutoFireV442(link.Runtime);
                else if(action=="fill")link.Owner.FillComplexCrewV441(link.Runtime);
                else if(action=="reload")link.Owner.SetArtilleryChargeV439(link.Runtime,id);
                else if(action=="fire")
                {
                    int dir=link.Runtime.RealDirPrecise&255;
                    link.Owner.AttackArtilleryPointV439(link.Runtime,
                        (((int)unit.RealXFloat>>4)+C2OriginalMovementMathV352.TCos[dir])*16,
                        (((int)unit.RealYFloat>>4)+C2OriginalMovementMathV352.TSin[dir])*16,1);
                }
            }
        }

        internal void PollArtilleryInputV439()
        {
            bool left=false,right=false;Vector2 pos=Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(mouse!=null){left=mouse.leftButton.wasPressedThisFrame;right=mouse.rightButton.wasPressedThisFrame;pos=mouse.position.ReadValue();}
            var keys=UnityEngine.InputSystem.Keyboard.current;
            if(keys!=null && (EventSystem.current==null || EventSystem.current.currentSelectedGameObject==null || EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()==null))
                foreach(var b in _artilleryUiV439)
                    if(b.Click!=null && b.Click.gameObject.activeSelf && Enum.TryParse(b.Hotkey,true,out UnityEngine.InputSystem.Key key) && key!=UnityEngine.InputSystem.Key.None && keys[key].wasPressedThisFrame)
                    {ArtilleryCommandV439(b.Action,b.Id);break;}
#elif ENABLE_LEGACY_INPUT_MANAGER
            left=Input.GetMouseButtonDown(0);right=Input.GetMouseButtonDown(1);pos=Input.mousePosition;
            foreach(var b in _artilleryUiV439)
                if(b.Click!=null && b.Click.gameObject.activeSelf && Enum.TryParse(b.Hotkey,true,out KeyCode key) && key!=KeyCode.None && Input.GetKeyDown(key))
                {ArtilleryCommandV439(b.Action,b.Id);break;}
#endif
            var aim=_artilleryAimV439;
            if(aim==null)return;
            if(right){_artilleryAimV439=null;C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();return;}
            if(!left || (EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject()))return;
            var map=aim.OwnerMode;
            if(map==null || !map.C2TryGameplayGroundScreenPickLikeOriginal(pos,out var world,out float x,out float y,out string audit))return;
            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            foreach(var unit in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
                if(unit!=null && unit.IsSelected && C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(unit.Nation))
                    unit.RuntimeLinkCachedLikeOriginal?.Owner?.AttackArtilleryPointV439(unit.RuntimeLinkCachedLikeOriginal.Runtime,(int)x*16,(int)y*16,20000);
            _artilleryAimV439=null;
        }
    }

    internal sealed class C2ArtilleryHudClickV439:MonoBehaviour,IPointerClickHandler
    {
        internal C2GameplayHudV1 Owner;internal string Action;internal int Id;
        public void OnPointerClick(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left && data.button!=PointerEventData.InputButton.Right)return;
            Owner.ArtilleryCommandV439(Action,Id,data.button==PointerEventData.InputButton.Right);data.Use();
        }
    }
    [DefaultExecutionOrder(-1000)]
    internal sealed class C2ArtilleryHudInputV439:MonoBehaviour
    {
        internal C2GameplayHudV1 Owner;
        private void Update(){if(Owner!=null)Owner.PollArtilleryInputV439();}
    }
}
