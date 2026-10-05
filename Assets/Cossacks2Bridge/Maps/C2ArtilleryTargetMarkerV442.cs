using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Navorots.cpp::ShowDestn: Interf3/exitpoint, 17 frames, 40 ms each.
    internal sealed class C2ArtilleryTargetMarkerV442 : MonoBehaviour
    {
        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private GameObject _canvas;
        private Image _image;
        internal int DisplayedFrameV442 { get; private set; }
        internal static void Ensure(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if(unit.GetComponent<C2ArtilleryTargetMarkerV442>()==null)
                unit.EnsureUnityProxyLikeOriginal().AddComponent<C2ArtilleryTargetMarkerV442>()._unit=unit;
        }
        private void LateUpdate()
        {
            var order=_unit?.RuntimeLinkCachedLikeOriginal?.Runtime?.ArtilleryPointOrderV439;
            bool show=_unit!=null && !_unit.IsDeadLikeOriginal && _unit.IsSelected && order!=null;
            if(!show){if(_image!=null)_image.enabled=false;return;}
            var camera=Camera.main;
            foreach(var c in Camera.allCameras)
                if(c.isActiveAndEnabled && c.name.Contains("C2_BattleTerrainCamera_Iso")){camera=c;break;}
            if(camera==null||_unit.OwnerMode==null)return;
            if(_image==null)
            {
                _canvas=new GameObject("GameplayHud_ArtilleryTarget_V442",typeof(Canvas));
                var canvas=_canvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32766;
                var child=new GameObject("Interf3_exitpoint",typeof(RectTransform),typeof(Image));child.transform.SetParent(_canvas.transform,false);
                _image=child.GetComponent<Image>();_image.raycastTarget=false;
                _image.rectTransform.sizeDelta=new Vector2(64,64);
            }
            Vector3 p=camera.WorldToScreenPoint(_unit.OwnerMode.SettlementMapPointToWorldV336LikeOriginal(order.X/16f,order.Y/16f));
            _image.enabled=p.z>0;_image.rectTransform.position=new Vector3(p.x,p.y,0);
            DisplayedFrameV442=(int)(Time.realtimeSinceStartup*25)%17;
            _image.sprite=C2GameplayOriginalSpriteCacheV1.LoadSprite("Interf3/exitpoint",DisplayedFrameV442,"artillery_target");
        }
        private void OnDestroy(){if(_canvas!=null)Destroy(_canvas);}
    }
}
