using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // borders.xml Hint / DrawForms.cpp::DrawFilledRect + SmallBlackFont.
    internal static class C2NativeHintV453
    {
        private static readonly Dictionary<int,Texture2D> Frames=new Dictionary<int,Texture2D>();
        private static string previous;
        private static float previousWidth;
        private static readonly List<string> Lines=new List<string>();
        private static Texture2D Frame(int n)
        {
            if(!Frames.TryGetValue(n,out var t))
                Frames[n]=t=C2GameplayOriginalSpriteCacheV1.LoadTexture("interf3\\elements\\border_TT",n,"native_hint_"+n);
            return t;
        }
        internal static void Draw(Rect anchor,string text)
        {
            float width=Mathf.Min(400,Screen.width-40);
            if(previous!=text||previousWidth!=width)
            {
                previous=text;previousWidth=width;Lines.Clear();
                foreach(var paragraph in text.Split('\n'))
                {
                    string line="";
                    foreach(var word in paragraph.Split(' '))
                    {
                        string next=line.Length==0?word:line+" "+word;
                        if(line.Length>0&&C2HudBitmapTextV448.MeasureGuiV453(next)>width)
                        {Lines.Add(line);line=word;}else line=next;
                    }
                    Lines.Add(line);
                }
            }
            float height=Lines.Count*12+10;
            float x=Mathf.Clamp(anchor.center.x-width/2,12,Mathf.Max(12,Screen.width-width-28));
            float y=Mathf.Clamp(anchor.y-height-10,12,Mathf.Max(12,Screen.height-height-12));
            Color old=GUI.color;GUI.color=Color.white;
            DrawBorder(new Rect(x,y,width+14,height));
            GUI.color=new Color32(0x2e,0x23,0x17,255);
            for(int i=0;i<Lines.Count;i++)C2HudBitmapTextV448.DrawGuiV453(new Vector2(x+7,y+5+i*12),Lines[i]);
            GUI.color=old;
        }
        private static void Tile(int frame,Rect clip,Vector2 start)
        {
            var t=Frame(frame);if(t==null||clip.width<=0||clip.height<=0)return;
            GUI.BeginGroup(clip);
            for(float y=start.y;y<clip.yMax;y+=t.height)
            for(float x=start.x;x<clip.xMax;x+=t.width)
                GUI.DrawTexture(new Rect(x-clip.x,y-clip.y,t.width,t.height),t);
            GUI.EndGroup();
        }
        private static void Corner(int frame,float x,float y)
        {var t=Frame(frame);GUI.DrawTexture(new Rect(x,y,t.width,t.height),t);}
        internal static void DrawBorder(Rect r)
        {
            float hw=Frame(3).width/2f, hh=Frame(3).height/2f;
            float bw=Frame(0).width/2f, bh=Frame(0).height/2f;
            Tile(8,r,r.position);
            Tile(7,new Rect(r.x+hw,r.y-hh,r.width-2*hw,2*hh),new Vector2(r.x+hw,r.y-hh));
            Tile(6,new Rect(r.x+bw,r.yMax-bh,r.width-2*bw,2*bh),new Vector2(r.x+bw,r.yMax-bh));
            Tile(9,new Rect(r.x-bw,r.y+hh,2*bw,r.height-hh-bh),new Vector2(r.x-bw,r.y+hh));
            Tile(10,new Rect(r.xMax-bw,r.y+hh,2*bw,r.height-hh-bh),new Vector2(r.xMax-bw,r.y+hh));
            Corner(3,r.x-hw,r.y-hh);Corner(5,r.xMax-hw,r.y-hh);
            Corner(0,r.x-bw,r.yMax-bh);Corner(2,r.xMax-bw,r.yMax-bh);
        }
    }
}
