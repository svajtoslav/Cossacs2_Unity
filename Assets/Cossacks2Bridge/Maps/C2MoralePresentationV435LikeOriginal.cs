using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2MoralePresentationV435LikeOriginal
    {
        private static float _pulseStart=-1;
        internal static Color32 ColorForSegment(int segment,int morale)
        {
            if(segment==2)return new Color32(253,196,7,143);
            if(segment>2)return new Color32(175,0,0,255);
            int mul=255;
            if(morale<45)
            {
                if(_pulseStart<0)_pulseStart=Time.realtimeSinceStartup;
                mul+=(int)(Mathf.Sin((Time.realtimeSinceStartup-_pulseStart)*5)*60-20);
            }
            // ClassEditor.h::MulDWORD multiplies every ARGB byte, including alpha.
            Color32 c=segment==0?new Color32(253,40,40,255):new Color32(253,196,7,255);
            return new Color32(Scale(c.r,mul),Scale(c.g,mul),Scale(c.b,mul),Scale(c.a,mul));
        }
        private static byte Scale(byte v,int mul){return (byte)Mathf.Min(255,(v*mul)>>8);}
    }
}
