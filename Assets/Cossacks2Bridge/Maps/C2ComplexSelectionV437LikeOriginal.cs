using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2ComplexSelectionV437LikeOriginal
    {
        internal string Texture;
        internal int X,Y,Width,Height,CenterX,CenterY;
        internal float Scale,RotationSpeed;
        internal Material Material;
    }
    public sealed partial class C2BattleTerrainMode
    {
        private Dictionary<string,C2ComplexSelectionV437LikeOriginal> _complexSelectionsV437;
        internal C2ComplexSelectionV437LikeOriginal ComplexSelectionV437(string name)
        {
            if(string.IsNullOrEmpty(name))return null;
            if(_complexSelectionsV437==null)
            {
                var text=Encoding.UTF8.GetString(_bootstrap.Fs.ReadAllBytes("Dialogs\\SelType.xml"));
                // The engine's serializer writes anonymous <> roots, not XML names.
                var xml=XElement.Parse(Regex.Match(text,@"<Selections>[\s\S]*?</Selections>").Value);
                _complexSelectionsV437=new Dictionary<string,C2ComplexSelectionV437LikeOriginal>();
                foreach(var item in xml.Elements("OneSelectionType"))
                    _complexSelectionsV437.Add((string)item.Element("Name"),new C2ComplexSelectionV437LikeOriginal
                    {Texture=(string)item.Element("TextureID"),X=(int)item.Element("x"),Y=(int)item.Element("y"),Width=(int)item.Element("Lx"),Height=(int)item.Element("Ly"),
                     CenterX=(int)item.Element("CenterX"),CenterY=(int)item.Element("CenterY"),Scale=(float)item.Element("StandartScale"),RotationSpeed=(float)item.Element("RotationSpeed")});
            }
            if(!_complexSelectionsV437.TryGetValue(name,out var result))throw new InvalidOperationException("Unknown original selection type "+name);
            if(result.Material==null)
            {
                var tex=C2OriginalTextureService.TryLoadTexture(_bootstrap.Fs,result.Texture,"C2ComplexSelection_"+name,C2OriginalTexturePolicy.WorldTextureLikeOriginal,out string audit);
                if(tex==null)throw new InvalidOperationException("Missing selection texture "+result.Texture);
                result.Material=new Material(Shader.Find("C2/UnitSelectionRingAlways")){name="C2_SELTYPE_"+name,mainTexture=tex,renderQueue=2449};
                result.Material.SetInt("_ZTest",4);result.Material.SetInt("_ZWrite",0);
            }
            return result;
        }
        internal void ClearArtilleryGraphicsV437()
        {
            foreach(var material in _artilleryMaterialsV437.Values)if(material!=null)Destroy(material);
            _artilleryMaterialsV437.Clear();_artilleryModelsV437.Clear();_artilleryClipsV437.Clear();
            if(_complexSelectionsV437!=null)foreach(var selection in _complexSelectionsV437.Values)if(selection.Material!=null)Destroy(selection.Material);
            _complexSelectionsV437=null;
            if(_cannonRangeMaterialV439!=null)Destroy(_cannonRangeMaterialV439);
            _cannonRangeMaterialV439=null;_cannonHeightBonusV439=null;
        }
    }
    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        private void UpdateComplexSelectionV437(C2UnitOriginalRuntime u)
        {
            var v=u.ComplexVisualV437;
            bool show=u.Selected&&!u.Info.IsDeadLikeOriginal;
            if(v.SelectionRoot!=null)v.SelectionRoot.SetActive(show);
            if(!show)return;
            var selection=v.Selection??(v.Selection=_battle.ComplexSelectionV437(u.Md.SelectionTypeV437));
            if(selection==null)return;
            float width=selection.Width*u.Md.SelectionScaleXV437*selection.Scale;
            float height=selection.Height*u.Md.SelectionScaleYV437*selection.Scale;
            int nx=Mathf.Clamp((int)(width/32),1,32),ny=Mathf.Clamp((int)(height/32),1,32);
            if(v.SelectionRoot==null)
            {
                v.SelectionRoot=new GameObject("C2_SELTYPE_"+u.Md.SelectionTypeV437);v.SelectionRoot.transform.SetParent(v.Root.transform,false);
                v.SelectionMesh=new Mesh{name=v.SelectionRoot.name};v.SelectionMesh.MarkDynamic();
                v.SelectionVertices=new Vector3[(nx+1)*(ny+1)];
                var uv=new Vector2[v.SelectionVertices.Length];var triangles=new int[nx*ny*6];int ti=0;
                var tex=selection.Material.mainTexture;
                for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++)uv[j*(nx+1)+i]=new Vector2((selection.X+selection.Width*(float)i/nx)/tex.width,1-(selection.Y+selection.Height*(float)j/ny)/tex.height);
                for(int j=0;j<ny;j++)for(int i=0;i<nx;i++)
                {
                    int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;
                    if((i&1)!=0){triangles[ti++]=a;triangles[ti++]=b;triangles[ti++]=c;triangles[ti++]=c;triangles[ti++]=b;triangles[ti++]=d;}
                    else{triangles[ti++]=a;triangles[ti++]=b;triangles[ti++]=d;triangles[ti++]=a;triangles[ti++]=d;triangles[ti++]=c;}
                }
                v.SelectionMesh.vertices=v.SelectionVertices;v.SelectionMesh.uv=uv;v.SelectionMesh.triangles=triangles;
                v.SelectionRoot.AddComponent<MeshFilter>().sharedMesh=v.SelectionMesh;
                v.SelectionRoot.AddComponent<MeshRenderer>().sharedMaterial=selection.Material;
            }
            var quant=u.OriginalComplexObjectV430LikeOriginal.Quants[0];int dir=u.RealDirPrecise&255;
            float x=(int)quant.Xc/16,y=quant.Yc/16,z=0;
            // OneObject::GetAttY iterates four terrain-height corrections.
            for(int i=0;i<4;i++){float h=_battle.C2OriginalFogTerrainHeightV1LikeOriginal((int)x,(int)y);y+=(h-z)*2;z=h;}
            x+=u.Md.SelectionShiftV437*C2OriginalMovementMathV352.TCos[dir]/256;
            y=(int)(y*16)/16+u.Md.SelectionShiftV437*C2OriginalMovementMathV352.TSin[dir]/256;
            float angle=dir*Mathf.PI/128+Time.realtimeSinceStartup*100*selection.RotationSpeed*Mathf.Deg2Rad;
            float cos=Mathf.Cos(angle),sin=Mathf.Sin(angle);
            for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++)
            {
                float dx=width*((float)i/nx-(float)selection.CenterX/selection.Width),dy=height*((float)j/ny-(float)selection.CenterY/selection.Height);
                float wx=x+dx*cos-dy*sin,wy=y+dx*sin+dy*cos;
                v.SelectionVertices[j*(nx+1)+i]=_battle.ArtilleryPointV437(wx,wy,_battle.C2OriginalFogTerrainHeightV1LikeOriginal((int)wx,(int)wy))+Vector3.up*0.05f;
            }
            v.SelectionMesh.vertices=v.SelectionVertices;v.SelectionMesh.RecalculateBounds();
        }
    }
}
