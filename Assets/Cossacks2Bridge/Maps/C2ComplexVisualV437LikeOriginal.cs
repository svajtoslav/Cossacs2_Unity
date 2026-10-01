using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntime
    {
        internal C2ComplexVisualV437LikeOriginal ComplexVisualV437;
    }
    internal sealed class C2ComplexVisualV437LikeOriginal : IDisposable
    {
        internal sealed class Part
        {
            internal GameObject Root;
            internal C2UnitOriginalRuntime Sprite;
            internal C2AnimatedModelV437LikeOriginal Model;
            internal C2ModelAnimationV437LikeOriginal Clip;
            internal Matrix4x4[] Matrices;
            internal Mesh[] Meshes;
            internal MeshRenderer[] Renderers;
            internal int ColorId = -1;
            internal Vector3[][] Vertices;
            internal Vector3[][] Normals;
        }
        internal readonly Dictionary<string, Part> Parts = new Dictionary<string, Part>();
        internal readonly List<C2ComplexPartPoseV437LikeOriginal> Poses = new List<C2ComplexPartPoseV437LikeOriginal>();
        internal GameObject Root;
        internal GameObject SelectionRoot;
        internal Mesh SelectionMesh;
        internal Vector3[] SelectionVertices;
        internal C2ComplexSelectionV437LikeOriginal Selection;
        internal C2ArtilleryRangeV439LikeOriginal RangeV439;
        public void Dispose()
        {
            foreach (var part in Parts.Values)
            {
                if (part.Meshes != null) foreach (var mesh in part.Meshes) if (mesh != null) UnityEngine.Object.Destroy(mesh);
                if (part.Sprite?.Mesh != null) UnityEngine.Object.Destroy(part.Sprite.Mesh);
            }
            if (Root != null) UnityEngine.Object.Destroy(Root);
            if (SelectionMesh != null) UnityEngine.Object.Destroy(SelectionMesh);
            RangeV439?.Dispose();
            Parts.Clear();
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        private readonly Dictionary<string,C2AnimatedModelV437LikeOriginal> _artilleryModelsV437 = new Dictionary<string,C2AnimatedModelV437LikeOriginal>();
        private readonly Dictionary<string,C2ModelAnimationV437LikeOriginal> _artilleryClipsV437 = new Dictionary<string,C2ModelAnimationV437LikeOriginal>();
        private readonly Dictionary<string,Material> _artilleryMaterialsV437 = new Dictionary<string,Material>();

        internal C2AnimatedModelV437LikeOriginal ArtilleryModelV437(string path)
        {
            C2AnimatedModelV437LikeOriginal model;
            if (!_artilleryModelsV437.TryGetValue(path, out model))
                _artilleryModelsV437.Add(path, model = C2AnimatedModelV437LikeOriginal.Read(_bootstrap.Fs.ReadAllBytes(path)));
            return model;
        }
        internal C2ModelAnimationV437LikeOriginal ArtilleryClipV437(string path)
        {
            if (string.Equals(path,"none",StringComparison.OrdinalIgnoreCase)) return null;
            C2ModelAnimationV437LikeOriginal clip;
            if (!_artilleryClipsV437.TryGetValue(path, out clip))
                _artilleryClipsV437.Add(path, clip = C2ModelAnimationV437LikeOriginal.Read(_bootstrap.Fs.ReadAllBytes(path)));
            return clip;
        }
        internal Material ArtilleryMaterialV437(C2AnimatedModelV437LikeOriginal.Geometry geometry, int nation=0)
        {
            string texture=geometry.Texture;
            string key=texture+"|"+geometry.EnvironmentTexture+"|"+geometry.DeviceState+"|"+C2PlayerColorsLikeOriginal.GetPlayerColorId(nation);
            Material material;
            if (_artilleryMaterialsV437.TryGetValue(key, out material)) return material;
            string audit;
            var image = TryLoadWallC2MTXRETextureV48LikeOriginal(new WallC2MParsedMeshV23LikeOriginal {TextureName=texture}, out audit);
            if (image == null) throw new InvalidOperationException("Artillery texture missing: " + audit);
            var state=XElement.Parse(Encoding.UTF8.GetString(_bootstrap.Fs.ReadAllBytes("Shaders\\DeviceStates\\"+geometry.DeviceState+".xml")));
            var render=state.Element("RenderState");
            XElement stage0=null;
            foreach(var stage in state.Elements("TextureStageState"))if((string)stage.Attribute("Stage")=="0")stage0=stage;
            bool environment=geometry.DeviceState=="env_mapped";
            var shader=Shader.Find("Cossacks2Bridge/OriginalModelV437");
            if(shader==null)throw new InvalidOperationException("OriginalModelV437 shader missing");
            material = new Material(shader);
            material.name="C2_ArtilleryV437_"+texture; material.mainTexture=image;
            material.SetColor("_Nation",C2PlayerColorsLikeOriginal.GetNatColorByPlayer(nation));
            string settings=Encoding.UTF8.GetString(_bootstrap.Fs.ReadAllBytes("EngineSettings.xml"));
            material.SetColor("_Ambient",ArtilleryLightColorV437(settings,"LightAmbient"));
            material.SetColor("_Diffuse",ArtilleryLightColorV437(settings,"LightDiffuse"));
            material.SetFloat("_Modulate",(string)stage0?.Element("ColorOp")=="Modulate2x"?2:1);
            string alpha=(string)render.Element("AlphaRef");
            material.SetFloat("_AlphaCutoff",Convert.ToInt32(alpha,16)/255f);
            material.SetFloat("_Environment",environment?1:0);
            material.SetFloat("_Specular",(string)render.Element("SpecularEnable")=="True"?1:0);
            // Native GameLight is in the skewed world. Undo its shear before
            // converting to Unity's world so the light is not tilted twice.
            Vector3 lightOriginal=new Vector3(10,5+2.5f/0.8660254f,5/0.8660254f);
            var direction=(ArtilleryPointV437(lightOriginal.x,lightOriginal.y,lightOriginal.z)-ArtilleryPointV437(0,0,0)).normalized;
            material.SetVector("_LightDirection",direction);
            if(environment)
            {
                if(string.IsNullOrEmpty(geometry.EnvironmentTexture))throw new InvalidOperationException("Environment material has no stage 1 texture");
                var env=TryLoadWallC2MTXRETextureV48LikeOriginal(new WallC2MParsedMeshV23LikeOriginal{TextureName=geometry.EnvironmentTexture},out audit);
                if(env==null)throw new InvalidOperationException("Environment texture missing: "+audit);
                material.SetTexture("_EnvTex",env);
            }
            _artilleryMaterialsV437.Add(key,material);return material;
        }
        private static Color32 ArtilleryLightColorV437(string settings,string name)
        {
            var value=Regex.Match(settings,"<"+name+">([0-9A-Fa-f]+)</"+name+">");
            if(!value.Success)throw new InvalidOperationException("Missing native light setting "+name);
            uint color=uint.Parse(value.Groups[1].Value,NumberStyles.HexNumber,CultureInfo.InvariantCulture);
            return new Color32((byte)(color>>16),(byte)(color>>8),(byte)color,(byte)(color>>24));
        }
        internal Vector3 ArtilleryPointV437(float x,float y,float z)
        {
            return OriginalWallXYZToWorldV6LikeOriginal(x,y,z)-new Vector3(0,C2WallObjectsV1YOffsetLikeOriginal,0);
        }
        internal Matrix4x4 ArtilleryMatrixV437(C2ComplexPartPoseV437LikeOriginal pose)
        {
            float x=pose.OriginalX,y=pose.OriginalY;
            float z=GetStrictTotalHeightLikeOriginal(pose.OriginalX,pose.OriginalY)+pose.Animation.AddHeight;
            float angle=(pose.Direction+pose.Animation.AddDirection)*3.1415f/128;
            Vector3 vz=Vector3.forward, vx=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0),vy=new Vector3(-Mathf.Sin(angle),Mathf.Cos(angle),0);
            // NewAnimation::DrawAt aligns ground axes, then restores vertical Z.
            if (z-pose.Animation.AddHeight>0)
            {
                float hx=GetStrictTotalHeightLikeOriginal((int)x-32,(int)y)-GetStrictTotalHeightLikeOriginal((int)x+32,(int)y);
                float hy=GetStrictTotalHeightLikeOriginal((int)x,(int)y-32)-GetStrictTotalHeightLikeOriginal((int)x,(int)y+32);
                var normal=new Vector3(hx/64,hy/64,2).normalized;
                vy=Vector3.Cross(normal,vx).normalized;vx=Vector3.Cross(vy,normal);
            }
            var origin=ArtilleryPointV437(x,y,z);float scale=pose.Animation.Scale;
            var result=Matrix4x4.identity;
            result.SetColumn(0,(ArtilleryPointV437(x+vx.x*scale,y+vx.y*scale,z+vx.z*scale)-origin));
            result.SetColumn(1,(ArtilleryPointV437(x+vy.x*scale,y+vy.y*scale,z+vy.z*scale)-origin));
            result.SetColumn(2,(ArtilleryPointV437(x,y,z+scale)-origin));result.SetColumn(3,new Vector4(origin.x,origin.y,origin.z,1));
            return result;
        }
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal bool TryComplexScreenRectV437(C2UnitOriginalRuntime u, in UnitScreenProjectionV376LikeOriginal projection, out Rect rect, out Vector2 anchor)
        {
            rect=default;anchor=default;bool found=false;
            if(u?.ComplexVisualV437==null||!u.ActiveLikeOriginal)return false;
            foreach(var p in u.ComplexVisualV437.Parts.Values)
            {
                if(!p.Root.activeInHierarchy)continue;
                // Embedded crew is not selectable. The owner is selected on its
                // model; helper graphics must not enlarge that footprint.
                if(p.Model!=null&&p.Vertices!=null)foreach(var verts in p.Vertices)
                {
                    var matrix=Matrix4x4.identity;
                    if(projection.ProjectQuad(in matrix,verts,out var r,out var a))MergeComplexRectV437(ref rect,ref anchor,ref found,r,a);
                }
            }
            return found;
        }
        private static void MergeComplexRectV437(ref Rect rect,ref Vector2 anchor,ref bool found,Rect other,Vector2 at)
        {
            if(!found){rect=other;anchor=at;found=true;return;}
            rect=Rect.MinMaxRect(Mathf.Min(rect.xMin,other.xMin),Mathf.Min(rect.yMin,other.yMin),Mathf.Max(rect.xMax,other.xMax),Mathf.Max(rect.yMax,other.yMax));
        }
        internal bool TryComplexPixelHitV437(C2UnitOriginalRuntime u,Camera camera,Vector3 screen,out float alpha,out Vector2 uv)
        {
            alpha=0;uv=default;
            if(u?.ComplexVisualV437==null||camera==null||!u.ActiveLikeOriginal)return false;
            Ray ray=camera.ScreenPointToRay(screen);
            foreach(var p in u.ComplexVisualV437.Parts.Values)
            {
                if(!p.Root.activeInHierarchy)continue;
                if(p.Model!=null)for(int i=0;i<p.Model.Meshes.Length;i++)
                {
                    var mesh=p.Model.Meshes[i];
                    var material=_battle.ArtilleryMaterialV437(mesh,u.Info.Nation);
                    var texture=material.mainTexture as Texture2D;
                    if(HitComplexTrianglesV437(ray,Matrix4x4.identity,p.Vertices[i],mesh.UV,mesh.Triangles,texture,
                        material.GetFloat("_Environment")>0.5f,material.GetFloat("_AlphaCutoff"),out alpha,out uv))return true;
                }
            }
            return false;
        }
        private static bool HitComplexTrianglesV437(Ray ray,Matrix4x4 matrix,Vector3[] vertices,Vector2[] uvs,int[] triangles,Texture2D texture,bool environment,float cutoff,out float alpha,out Vector2 uv)
        {
            alpha=0;uv=default;
            if(vertices==null||texture==null)return false;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int ai=triangles[i],bi=triangles[i+1],ci=triangles[i+2];
                var a=matrix.MultiplyPoint3x4(vertices[ai]);var e1=matrix.MultiplyPoint3x4(vertices[bi])-a;var e2=matrix.MultiplyPoint3x4(vertices[ci])-a;
                var h=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<0.0000001f)continue;
                float inv=1/det;var s=ray.origin-a;float b=inv*Vector3.Dot(s,h);if(b<0||b>1)continue;
                var q=Vector3.Cross(s,e1);float c=inv*Vector3.Dot(ray.direction,q);if(c<0||b+c>1||inv*Vector3.Dot(e2,q)<0)continue;
                uv=uvs[ai]*(1-b-c)+uvs[bi]*b+uvs[ci]*c;
                // env.jpg is opaque; its alpha replaces stage 0 alpha. The
                // diffuse alpha on a metal barrel must not make it unpickable.
                alpha=environment?1:Mathf.Min(1,texture.GetPixelBilinear(uv.x,uv.y).a*2);
                if(alpha>=cutoff)return true;
            }
            return false;
        }
        private static bool HasRuntimeVisualV437(MdModel md, string path, int idle)
        {
            if (!string.IsNullOrEmpty(md.ComplexObjectIdLikeOriginal))
                return TryResolveComplexUnitDescV430LikeOriginal(new C2UnitOriginalRuntime {Md=md,MdPath=path},out var desc)
                    && desc.Chain != null && desc.Chain.Length > 0 && desc.AnimationsV437 != null;
            return idle >= 0 && idle < md.Animations.Count && md.Animations[idle].Frames.Count > 0;
        }
        private void RenderComplexRuntimeV437(C2UnitOriginalRuntime u, bool visible)
        {
            if (u.OriginalDeathExpiredV435LikeOriginal || !u.ActiveLikeOriginal) visible=false;
            if (u.ComplexVisualV437 == null && visible) u.ComplexVisualV437=CreateComplexVisualV437(u.Md.ComplexObjectIdLikeOriginal);
            if (u.ComplexVisualV437 == null) return;
            u.ComplexVisualV437.Root.SetActive(visible);
            if (visible)
            {
                DrawComplexVisualV437(u.OriginalComplexObjectV430LikeOriginal,u.ComplexVisualV437,_battle,u.Info.Nation);
                UpdateComplexSelectionV437(u);
                UpdateArtilleryRangeV439(u);
            }
        }
        internal C2ComplexVisualV437LikeOriginal CreateComplexVisualV437(string name)
        {
            var root=new GameObject("C2_ComplexV437_"+name);
            root.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : transform,false);
            return new C2ComplexVisualV437LikeOriginal {Root=root};
        }
        internal void DrawComplexVisualV437(C2ComplexObjectRuntimeV430LikeOriginal cob, C2ComplexVisualV437LikeOriginal visual, C2BattleTerrainMode map, int nation)
        {
            EvaluateComplexPartsV437LikeOriginal(cob,visual.Poses);
            foreach (var part in visual.Parts.Values) part.Root.SetActive(false);
            foreach (var pose in visual.Poses)
            {
                var a=pose.Animation;
                string key=pose.Quant+":"+pose.Part+":"+a.Id;
                C2ComplexVisualV437LikeOriginal.Part part;
                if (!visual.Parts.TryGetValue(key,out part))
                {
                    part=new C2ComplexVisualV437LikeOriginal.Part {Root=new GameObject(a.Id)};
                    part.Root.transform.SetParent(visual.Root.transform,false);
                    if(a.IsModel)
                    {
                        part.Model=map.ArtilleryModelV437(a.ModelPath);part.Clip=map.ArtilleryClipV437(a.AnimationPath);
                        part.Matrices=new Matrix4x4[part.Model.Nodes.Length];part.Meshes=new Mesh[part.Model.Meshes.Length];part.Vertices=new Vector3[part.Meshes.Length][];part.Normals=new Vector3[part.Meshes.Length][];
                        part.Renderers=new MeshRenderer[part.Meshes.Length];
                        for(int i=0;i<part.Meshes.Length;i++)
                        {
                            var source=part.Model.Meshes[i];var go=new GameObject(part.Model.Nodes[source.NodeIndex].Name);go.transform.SetParent(part.Root.transform,false);
                            var mesh=new Mesh {name="C2_Artillery_"+go.name};mesh.MarkDynamic();
                            part.Meshes[i]=mesh;part.Vertices[i]=new Vector3[source.Vertices.Length];
                            part.Normals[i]=new Vector3[source.Vertices.Length];
                            mesh.vertices=source.Vertices;mesh.uv=source.UV;mesh.colors32=source.Colors;mesh.triangles=source.Triangles;
                            go.AddComponent<MeshFilter>().sharedMesh=mesh;
                            part.Renderers[i]=go.AddComponent<MeshRenderer>();
                        }
                    }
                    else
                    {
                        var anim=new AnimModel {Name=a.Id,Rotations=a.Rotations,Inverse=a.Inverse};
                        for(int i=0;i<a.FrameCount;i++)anim.Frames.Add(new FrameModel {Package=a.Package,SpriteId=a.StartFrame+i,Dx=a.Dx,Dy=a.Dy});
                        var md=new MdModel {Name=a.Id};md.Animations.Add(anim);
                        var u=new C2UnitOriginalRuntime {Root=part.Root,Md=md,Probe=new UnitProbe {Nation=nation,MonsterId=a.Id},
                            CurrentAnimIndex=0,OctantInfo=0xFF,ActiveLikeOriginal=true,State=C2UnitOriginalState.Stand};
                        // This is a drawing part only: never register it in gameplay
                        // _units or attach its proxy to the owner's unit Info.
                        part.Sprite=u;EnsureIndividualUnitRenderersLikeOriginal(u,0);
                    }
                    visual.Parts.Add(key,part);
                }
                part.Root.SetActive(true);
                if(a.IsModel)
                {
                    int colorId=C2PlayerColorsLikeOriginal.GetPlayerColorId(nation);
                    if(part.ColorId!=colorId)
                    {
                        for(int i=0;i<part.Renderers.Length;i++)part.Renderers[i].sharedMaterial=map.ArtilleryMaterialV437(part.Model.Meshes[i],nation);
                        part.ColorId=colorId;
                    }
                    part.Model.Evaluate(part.Clip,pose.Frame*(part.Clip?.Duration??0)/a.FrameCount,part.Matrices);
                    Matrix4x4 placement=map.ArtilleryMatrixV437(pose);
                    for(int i=0;i<part.Meshes.Length;i++)
                    {
                        var source=part.Model.Meshes[i];Matrix4x4 matrix=placement*part.Matrices[source.NodeIndex];
                        var normalMatrix=matrix.inverse.transpose;
                        for(int v=0;v<source.Vertices.Length;v++)
                        {part.Vertices[i][v]=matrix.MultiplyPoint3x4(source.Vertices[v]);part.Normals[i][v]=normalMatrix.MultiplyVector(source.Normals[v]).normalized;}
                        part.Meshes[i].vertices=part.Vertices[i];part.Meshes[i].normals=part.Normals[i];part.Meshes[i].RecalculateBounds();
                    }
                }
                else
                {
                    var u=part.Sprite;u.RealDirPrecise=pose.Direction;u.OriginalRealDirPrecise256LikeOriginal=pose.Direction<<8;u.OctantInfo=0xFF;
                    u.CurrentFrameLong=pose.Frame<<8;u.Probe.Nation=nation;
                    u.WorldPosition=map.ArtilleryPointV437(pose.OriginalX,pose.OriginalY,
                        map.C2OriginalFogTerrainHeightV1LikeOriginal(pose.OriginalX,pose.OriginalY)+a.AddHeight);
                    u.Root.transform.position=u.WorldPosition;
                    if(!ApplyUnitFrameLikeOriginal(u,"complex_part_v437"))throw new InvalidOperationException("Complex GP frame missing: "+a.Id+" frame="+pose.Frame);
                    // The shared frame routine also fills quad arrays when the
                    // gameplay batch is enabled. These visual parts own a mesh.
                    u.Mesh.vertices=u.BodyQuadVerticesLikeOriginal;u.Mesh.uv=u.BodyQuadUvsLikeOriginal;
                    u.Mesh.triangles=C2UnitOriginalRuntime.BodyQuadTrianglesLikeOriginal;u.Mesh.RecalculateBounds();
                    u.MeshRenderer.sharedMaterial=GetUnitBodyMaterialForTextureLikeOriginal(u.LastTexture);
                    u.MeshRenderer.sortingOrder=SortingOrderBase+pose.OriginalY;
                    ApplyViewerLikePixelPerfectTransformLikeOriginal(u);
                }
            }
        }
    }
}
