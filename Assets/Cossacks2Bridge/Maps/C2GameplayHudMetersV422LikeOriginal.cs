using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private sealed class MeterBindingV422
        {
            internal C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            internal bool Life;
            internal int X, Y, W, H, Value = int.MinValue, Maximum = int.MinValue;
            internal Image[] Images;
        }
        private sealed class MoraleTextBindingV422
        {
            internal C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            internal Text Text;
            internal int Value = int.MinValue, Maximum = int.MinValue;
        }
        private readonly List<MeterBindingV422> _metersV422 = new List<MeterBindingV422>();
        private readonly List<MoraleTextBindingV422> _moraleTextsV422 = new List<MoraleTextBindingV422>();

        private void BindMoraleTextV422(C2NeutralPeasantUnitInfoV2LikeOriginal unit, Text text)
        {
            if (unit != null && text != null) _moraleTextsV422.Add(new MoraleTextBindingV422 {Unit=unit, Text=text});
        }
        private void BindMeterV422(C2NeutralPeasantUnitInfoV2LikeOriginal unit, bool life, int x, int y, int w, int h)
        {
            var b = new MeterBindingV422 {Unit=unit,Life=life,X=x,Y=y,W=w,H=h,Images=new Image[life?1:3+9*(h+1)]};
            for(int i=0;i<b.Images.Length;i++)
            {
                Color32 color = life ? new Color32(0,255,0,255) : C2MoralePresentationV435LikeOriginal.ColorForSegment(i,100);
                b.Images[i]=AddSolidSinglePassV140ALikeOriginal("sp_meter_v422_"+i,color,x,y,0,h,false);
            }
            _metersV422.Add(b);
            RefreshMeterV422(b);
        }
        // VUI_Actions.cpp::SetMorale: three fill segments and diagonal marks for
        // each full hundred of morale. These are not experience awards.
        internal static RectInt MoraleSegmentV422(int index,int w,int h,int morale,int maximum)
        {
            int n=Mathf.Max(0,morale/100),m=Mathf.Clamp(morale%100,0,100),M=Mathf.Clamp(maximum-n*100,0,100);
            if(w<=0||h<=0||maximum<=0||n+M<=0||n>=10)return default(RectInt);
            int lx=Mathf.Clamp(m*w/100,0,w), lm=Mathf.Clamp(M*w/100,0,w);
            int lr=n==0?Mathf.Clamp(Mathf.Min(m,32)*w/100,0,w):0;
            if(index==0)return new RectInt(0,0,lr,h);
            if(index==1)return new RectInt(lr,0,Mathf.Max(0,lx-lr),h);
            if(index==2)return new RectInt(lx,0,Mathf.Max(0,lm-lx),h);
            int row=(index-3)%(h+1),tick=(index-3)/(h+1),tw=Mathf.Max(0,h-1),start=(w-(n+n-1)*tw)/2;
            return tick<n?new RectInt(start+tick*2*tw+h-row,row,tw,1):default(RectInt);
        }
        private void RefreshMeterV422(MeterBindingV422 b)
        {
            int value=0,maximum=0;
            if(b.Life) C2FormationRuntimeV167LikeOriginal.TryGetFormationAverageLifeV404LikeOriginal(b.Unit,out value,out maximum);
            else {value=ResolveMoraleCurrentLikeOriginal(b.Unit);maximum=ResolveMoraleMaxLikeOriginal(b.Unit);}
            if(b.Value==value&&b.Maximum==maximum&&(b.Life||value>=45))return;
            b.Value=value;b.Maximum=maximum;
            for(int i=0;i<b.Images.Length;i++)
            {
                var image=b.Images[i];if(image==null)continue;
                int fill=maximum>0&&value>0?Mathf.Clamp(b.H*value/maximum,0,b.H):0;
                RectInt rect=b.Life?new RectInt(0,b.H-fill,Mathf.Max(1,b.W),fill):MoraleSegmentV422(i,b.W,b.H,value,maximum);
                Place(image.rectTransform,b.X+rect.x,b.Y+rect.y,rect.width,rect.height);
                image.enabled=rect.width>0&&rect.height>0;
                if(!b.Life)image.color=C2MoralePresentationV435LikeOriginal.ColorForSegment(i,value);
            }
        }
        private void RefreshMetersV422()
        {
            for(int i=0;i<_metersV422.Count;i++)RefreshMeterV422(_metersV422[i]);
            for(int i=0;i<_moraleTextsV422.Count;i++)
            {
                var b=_moraleTextsV422[i];if(b.Text==null)continue;
                int value=ResolveMoraleCurrentLikeOriginal(b.Unit),maximum=ResolveMoraleMaxLikeOriginal(b.Unit);
                if(b.Value==value&&b.Maximum==maximum)continue;
                b.Value=value;b.Maximum=maximum;b.Text.text=ResolveMoraleTextLikeOriginal(b.Unit);
            }
        }
    }
}
