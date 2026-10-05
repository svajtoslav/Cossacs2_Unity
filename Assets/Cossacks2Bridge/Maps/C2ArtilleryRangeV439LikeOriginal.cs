using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2ArtilleryRangeV439LikeOriginal : System.IDisposable
    {
        internal GameObject Root;
        internal Mesh Mesh;
        internal readonly List<Vector3> Vertices=new List<Vector3>();
        internal readonly List<Color32> Colors=new List<Color32>();
        internal readonly List<int> Indices=new List<int>();
        internal int X=int.MinValue,Y,Direction,Min,Max;
        public void Dispose(){if(Root!=null)Object.Destroy(Root);if(Mesh!=null)Object.Destroy(Mesh);}
    }

    public sealed partial class C2BattleTerrainMode
    {
        private Material _cannonRangeMaterialV439;
        private int? _cannonHeightBonusV439;
        internal int CannonHeightBonusV439
        {
            get
            {
                if(!_cannonHeightBonusV439.HasValue)
                {
                    var text=Encoding.UTF8.GetString(_bootstrap.Fs.ReadAllBytes("EngineSettings.xml"));
                    var match=Regex.Match(text,@"<CannonAddShotDistPer100_Height>(\d+)</CannonAddShotDistPer100_Height>");
                    if(!match.Success)throw new System.InvalidOperationException("Missing CannonAddShotDistPer100_Height");
                    _cannonHeightBonusV439=int.Parse(match.Groups[1].Value);
                }
                return _cannonHeightBonusV439.Value;
            }
        }
        internal Material CannonRangeMaterialV439()
        {
            if(_cannonRangeMaterialV439==null)_cannonRangeMaterialV439=new Material(Shader.Find("Cossacks2Bridge/ArtilleryRangeV439"));
            return _cannonRangeMaterialV439;
        }
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        private int _cannonSelectionFrameV439=-1,_cannonSelectionCountV439;
        private void UpdateArtilleryRangeV439(C2UnitOriginalRuntime u)
        {
            if(_cannonSelectionFrameV439!=Time.frameCount)
            {
                _cannonSelectionFrameV439=Time.frameCount;_cannonSelectionCountV439=0;
                foreach(var unit in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
                    if(unit!=null&&unit.IsSelected&&!unit.IsDeadLikeOriginal)_cannonSelectionCountV439++;
            }
            var visual=u.ComplexVisualV437;
            var traits=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(u.Info);
            int type=ComplexLocalNewStateV430LikeOriginal(u);
            int r1=traits.AttackRadius1[type],r2=traits.AttackRadius2[type];
            bool show=u.Selected&&!u.Info.IsDeadLikeOriginal&&_cannonSelectionCountV439==1&&r2>r1;
            if(visual.RangeV439!=null)visual.RangeV439.Root.SetActive(show);
            if(!show)return;
            if(visual.RangeV439==null)
            {
                var range=new C2ArtilleryRangeV439LikeOriginal();visual.RangeV439=range;
                range.Root=new GameObject("C2_ShowAttackRangePreview");range.Root.transform.SetParent(visual.Root.transform,false);
                range.Mesh=new Mesh{name="C2_DrawTrapezia"};range.Mesh.MarkDynamic();
                range.Root.AddComponent<MeshFilter>().sharedMesh=range.Mesh;
                range.Root.AddComponent<MeshRenderer>().sharedMaterial=_battle.CannonRangeMaterialV439();
            }
            var v=visual.RangeV439;
            int x=(int)u.RuntimeRealXLikeOriginal>>4,y=(int)u.RuntimeRealYLikeOriginal>>4;
            int direction=((u.RealDirPrecise+8+512)>>4)&15;
            if(v.X==x&&v.Y==y&&v.Direction==direction&&v.Min==r1&&v.Max==r2)return;
            v.X=x;v.Y=y;v.Direction=direction;v.Min=r1;v.Max=r2;
            int a1=(direction*16-8)&255,a2=(a1+16)&255;
            // Mechanics.cpp::ShowAttackRangePreview: eight terrain iterations,
            // with the extra range only when the minimum radius is above 64.
            if(r1>64)
            {
                int a=(a1+8)&255,h0=(int)_battle.C2OriginalFogTerrainHeightV1LikeOriginal(x,y),baseR=r2;
                for(int i=0;i<8;i++)
                {
                    int x1=x+C2OriginalMovementMathV352.TCos[a]*r2/256,y1=y+C2OriginalMovementMathV352.TSin[a]*r2/256;
                    int h1=(int)_battle.C2OriginalFogTerrainHeightV1LikeOriginal(x1,y1);
                    if(h1<h0)r2=baseR+(h0-h1)*_battle.CannonHeightBonusV439/100;
                }
            }
            v.Vertices.Clear();v.Colors.Clear();v.Indices.Clear();
            ArtilleryTrapeziaV439(v,x,y,r1,r2,a1,a2,0x55ff0000,0x35ff0000,0x35ff0000,0x55ff0000);
            if(r1==0)
            {
                int r3=r2*11/6;
                ArtilleryTrapeziaV439(v,x,y,r1,r2,a1-16,a1,0x00ff0000,0x00ff0000,0x35ff0000,0x55ff0000);
                ArtilleryTrapeziaV439(v,x,y,r1,r2,a2,a2+16,0x55ff0000,0x35ff0000,0x00ff0000,0x10ff0000);
                ArtilleryTrapeziaV439(v,x,y,r2,r3,a1-16,a1,0x00ff0000,0x00ff0000,0x00ff0000,0x35ff0000);
                ArtilleryTrapeziaV439(v,x,y,r2,r3,a2,a2+16,0x35ff0000,0x00ff0000,0x00ff0000,0x00ff0000);
                ArtilleryTrapeziaV439(v,x,y,r2,r3,a1,a2,0x35ff0000,0x00ff0000,0x00ff0000,0x35ff0000);
            }
            v.Mesh.Clear();v.Mesh.SetVertices(v.Vertices);v.Mesh.SetColors(v.Colors);v.Mesh.SetTriangles(v.Indices,0);v.Mesh.RecalculateBounds();
        }

        private void ArtilleryTrapeziaV439(C2ArtilleryRangeV439LikeOriginal v,int x,int y,int r1,int r2,int a1,int a2,uint c0,uint c1,uint c2,uint c3)
        {
            a1&=255;a2&=255;
            int x0=x+((C2OriginalMovementMathV352.TCos[a1]*r1)>>8),y0=y+((C2OriginalMovementMathV352.TSin[a1]*r1)>>8);
            int x1=x+((C2OriginalMovementMathV352.TCos[a1]*r2)>>8),y1=y+((C2OriginalMovementMathV352.TSin[a1]*r2)>>8);
            int x2=x+((C2OriginalMovementMathV352.TCos[a2]*r2)>>8),y2=y+((C2OriginalMovementMathV352.TSin[a2]*r2)>>8);
            int x3=x+((C2OriginalMovementMathV352.TCos[a2]*r1)>>8),y3=y+((C2OriginalMovementMathV352.TSin[a2]*r1)>>8);
            int n=3+(r2-r1)/40;
            for(int i=0;i<n;i++)
            {
                int at=v.Vertices.Count,w=i*255/n,next=(i+1)*255/n;
                AddArtilleryRangePointV439(v,x0+(x1-x0)*i/n,y0+(y1-y0)*i/n,MixArtilleryColorV439(c1,c0,w));
                AddArtilleryRangePointV439(v,x0+(x1-x0)*(i+1)/n,y0+(y1-y0)*(i+1)/n,MixArtilleryColorV439(c1,c0,next));
                // Preserve DrawFillRect(V1,V2,V4,V3,C0,C1,C2,C3) ordering.
                AddArtilleryRangePointV439(v,x3+(x2-x3)*i/n,y3+(y2-y3)*i/n,MixArtilleryColorV439(c2,c3,next));
                AddArtilleryRangePointV439(v,x3+(x2-x3)*(i+1)/n,y3+(y2-y3)*(i+1)/n,MixArtilleryColorV439(c2,c3,w));
                v.Indices.Add(at);v.Indices.Add(at+1);v.Indices.Add(at+2);v.Indices.Add(at+1);v.Indices.Add(at+3);v.Indices.Add(at+2);
            }
        }
        private void AddArtilleryRangePointV439(C2ArtilleryRangeV439LikeOriginal v,int x,int y,Color32 color)
        {
            v.Vertices.Add(_battle.ArtilleryPointV437(x,y,Mathf.Max(0,_battle.C2OriginalFogTerrainHeightV1LikeOriginal(x,y)))+Vector3.up*0.04f);
            v.Colors.Add(color);
        }
        private static Color32 MixArtilleryColorV439(uint a,uint b,int weight)
        {
            // MixDWORD uses weights with a sum of 255 and divides by 256.
            return new Color32((byte)((((a>>16)&255)*weight+((b>>16)&255)*(255-weight))>>8),
                (byte)((((a>>8)&255)*weight+((b>>8)&255)*(255-weight))>>8),
                (byte)(((a&255)*weight+(b&255)*(255-weight))>>8),
                (byte)(((a>>24)*weight+(b>>24)*(255-weight))>>8));
        }
    }
}
