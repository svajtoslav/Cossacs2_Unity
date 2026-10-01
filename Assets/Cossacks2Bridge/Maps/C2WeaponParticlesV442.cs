using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // sgEffect.cpp operators present in Pushka_Vistrel_01/Gaubitsa_Vistrel_01.
    // Unsupported graphs fail explicitly. This does not implement explosion
    // triggers, terrain decals, or the rocket's WeaponSystem XML graph.
    internal sealed class C2WeaponParticlesV442 : MonoBehaviour
    {
        internal sealed class Op
        {
            internal string Tag,Name,Texture;
            internal int Parent,Count,Repeat=1,Axis=7,Cols=1,Rows=1,Blend,Intensity,Align;
            internal int[] Children;
            internal float Start,End,Life,LifeVar,Rate,Speed,SpeedVar,Cone,Min,Max,Frequency,Shift;
            internal bool World,Planar,Bind,Rotate;
            internal Vector3 Vector,Angular,AngularVar,Ref;
            internal Color Tint=Color.white;
            internal List<float> Times=new List<float>(),Values=new List<float>();
            internal List<Color> Colors=new List<Color>();
            internal uint Flags;
        }
        private sealed class Particle
        {
            internal Vector3 Pos,Velocity,Size=Vector3.one*20,Angular;
            internal float Age,Life,Roll;
            internal Color Color=Color.white;
            internal int Frame;
        }
        private sealed class Emitter
        {
            internal Op Def; internal Particle Parent; internal float Age,Accumulator;
            internal bool Burst;
            internal readonly List<Particle> Particles=new List<Particle>();
        }
        private sealed class Batch
        {
            internal Mesh Mesh; internal Material Material; internal Op Render;
            internal readonly List<Vector3> Vertices=new List<Vector3>();
            internal readonly List<Vector2> UV=new List<Vector2>();
            internal readonly List<Color> Colors=new List<Color>();
            internal readonly List<int> Indices=new List<int>();
        }
        private static readonly Dictionary<string,Op[]> Cache=new Dictionary<string,Op[]>(StringComparer.OrdinalIgnoreCase);
        private Op[] _ops;
        private C2BattleTerrainMode _map;
        private readonly List<Emitter> _emitters=new List<Emitter>();
        private readonly Dictionary<Op,Batch> _batches=new Dictionary<Op,Batch>();
        private readonly System.Random _random=new System.Random();
        private Vector3 _origin;
        private Particle _rootParticle;
        private float _cos,_sin,_last,_clock,_scale;
        internal static int Spawned,RenderedParticles;

        internal static bool Spawn(C2BattleTerrainMode map,string md,string model,Vector3 origin,int dir)
        {
            GameObject go=null;
            try
            {
                string root=Directory.GetParent(Path.GetDirectoryName(md)).FullName;
                string path=Path.Combine(root,model.Replace('\\',Path.DirectorySeparatorChar));
                if(!Cache.TryGetValue(path,out var ops))Cache[path]=ops=Read(File.ReadAllBytes(path));
                go=new GameObject("C2_WeaponParticlesV442_"+Path.GetFileNameWithoutExtension(model));
                var fx=go.AddComponent<C2WeaponParticlesV442>();fx._ops=ops;fx._map=map;fx._origin=origin;
                fx._cos=Mathf.Cos(dir*Mathf.PI/128);fx._sin=Mathf.Sin(dir*Mathf.PI/128);
                fx._last=C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
                fx._scale=map.SettlementSkewVectorToWorldV435LikeOriginal(1,0,0).magnitude;
                var rootParticle=new Particle{Life=ops[0].Life};
                fx._rootParticle=rootParticle;
                // The modular root contains one static particle; its operators
                // include the source offset/direction inherited by child emitters.
                fx.Initialize(rootParticle,ops[0]);
                foreach(int child in ops[0].Children)if(IsEmitter(ops[child]))fx.AddEmitter(ops[child],rootParticle);
                Spawned++;Debug.Log("[C2 V442 PARTICLES] "+model+" nodes="+ops.Length);
                return true;
            }
            catch(Exception e){if(go!=null)Destroy(go);Debug.LogWarning("[C2 V442 PARTICLES] "+model+": "+e.Message);return false;}
        }
        private static bool IsEmitter(Op op)=>op.Tag=="2BMI"||op.Tag=="2CMI";
        private static Vector3 Vec(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
        private static Color ArgB(uint c)=>new Color32((byte)(c>>16),(byte)(c>>8),(byte)c,(byte)(c>>24));
        private static string Str(BinaryReader r)=>Encoding.ASCII.GetString(r.ReadBytes(r.ReadInt32()));
        internal static Op[] Read(byte[] bytes)
        {
            var nodes=C2AnimatedModelV437LikeOriginal.ReadNodes(bytes);var result=new Op[nodes.Length];
            using(var r=new BinaryReader(new MemoryStream(bytes)))for(int i=0;i<nodes.Length;i++)
            {
                var n=nodes[i];r.BaseStream.Position=n.Payload;
                var o=result[i]=new Op{Tag=n.Tag,Name=n.Name,Parent=n.Parent,Children=n.Children};
                switch(o.Tag)
                {
                    case "2EFF":case "2BMI":case "2CMI":
                        o.Start=r.ReadSingle();o.End=r.ReadSingle();o.Flags=r.ReadByte();float probability=r.ReadSingle();
                        o.Life=r.ReadSingle();o.LifeVar=r.ReadSingle();o.World=r.ReadBoolean();
                        if(probability!=1 || o.Flags!=0)throw new InvalidDataException("Emitter flags/probability: "+o.Name);
                        if(o.Tag=="2BMI")o.Count=r.ReadInt32();if(o.Tag=="2CMI")o.Rate=r.ReadSingle();break;
                    case "2BRE":
                        o.Texture=Str(r);if(Str(r)!="")throw new InvalidDataException("Second texture: "+o.Name);
                        o.Blend=r.ReadByte();o.Intensity=r.ReadByte();o.Tint=ArgB(r.ReadUInt32());o.Flags=r.ReadUInt32();
                        r.ReadInt32();o.Align=r.ReadByte();o.Ref=Vec(r);break;
                    case "2PCS":case "2PRS":case "2PDS":
                        o.Speed=r.ReadSingle();o.SpeedVar=r.ReadSingle();o.Angular=Vec(r);o.AngularVar=Vec(r);o.Rotate=r.ReadBoolean();
                        if(o.Tag=="2PCS"){o.Cone=r.ReadSingle();o.Planar=r.ReadBoolean();}
                        if(o.Tag=="2PRS"){o.Bind=r.ReadBoolean();o.Planar=r.ReadBoolean();}
                        if(o.Tag=="2PDS")o.Vector=Vec(r);break;
                    case "2SIR":case "2VPP":case "2ALR":case "2ACR":
                        int count=r.ReadInt32();if(count<1||count>64)throw new InvalidDataException("Ramp count");
                        for(int k=0;k<count;k++){o.Times.Add(r.ReadSingle());if(o.Tag=="2ACR")o.Colors.Add(ArgB(r.ReadUInt32()));else o.Values.Add(r.ReadSingle());}
                        if(o.Tag=="2SIR"||o.Tag=="2VPP"){o.Min=r.ReadSingle();o.Max=r.ReadSingle();}
                        o.Repeat=r.ReadInt32();if(o.Tag=="2SIR")o.Axis=r.ReadByte();break;
                    case "2FZI":o.Cols=r.ReadInt32();o.Rows=r.ReadInt32();o.Flags=r.ReadByte();break;
                    case "2FOR":case "2PPP":o.Vector=Vec(r);break;
                    case "2WIN":o.Vector=Vec(r);o.Frequency=r.ReadSingle();o.Shift=r.ReadSingle();break;
                    default:throw new InvalidDataException("Unsupported native operator "+o.Tag+" ("+o.Name+")");
                }
                if(r.BaseStream.Position!=n.End)throw new InvalidDataException("Operator boundary "+o.Tag+" "+o.Name);
            }
            return result;
        }
        private void AddEmitter(Op d,Particle parent)
        {
            _emitters.Add(new Emitter{Def=d,Parent=parent});
            if(_batches.ContainsKey(d))return;
            foreach(int ci in d.Children)if(_ops[ci].Tag=="2BRE")
            {
                var render=_ops[ci];var tex=_map.WeaponParticleTextureV442(render.Texture);
                if(tex==null)throw new InvalidDataException("Missing particle texture "+render.Texture);
                var go=new GameObject(d.Name);go.layer=7;go.transform.SetParent(transform,false);
                var mesh=new Mesh{name=d.Name+" V442"};mesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var mat=new Material(Shader.Find("Cossacks2/WeaponParticlesV442"));mat.mainTexture=tex;
                mat.SetFloat("_DstBlend",render.Blend==1?(float)BlendMode.One:(float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_Intensity",1<<render.Intensity);mat.SetFloat("_ZTest",(render.Flags&1)!=0?(float)CompareFunction.LessEqual:(float)CompareFunction.Always);
                var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.sortingOrder=5201;
                _batches[d]=new Batch{Mesh=mesh,Material=mat,Render=render};break;
            }
        }
        private float R(float a,float b)=>a+(b-a)*(float)_random.NextDouble();
        // PEffectMgr::GetParticleWorldTM: a child emitter's Z axis follows
        // its parent particle velocity (the muzzle root's Direct operator).
        private static Vector3 ParentDirection(Particle parent,Vector3 v)
        {
            if(parent==null)return v;
            Vector3 z=parent.Velocity.sqrMagnitude>0.000001f?parent.Velocity.normalized:Vector3.forward;
            Vector3 y=Vector3.Cross(z,Vector3.right).normalized;
            if(y.sqrMagnitude<0.000001f)y=Vector3.Cross(z,Vector3.up).normalized;
            Vector3 x=Vector3.Cross(y,z);
            return x*v.x+y*v.y+z*v.z;
        }
        private void Initialize(Particle p,Op def,Particle parent=null)
        {
            foreach(int ci in def.Children)
            {
                var o=_ops[ci];
                if(o.Tag=="2PPP")p.Pos+=ParentDirection(parent,o.Vector);
                if(o.Tag=="2FZI")p.Frame=(o.Flags&2)!=0?0:_random.Next(Math.Max(1,o.Cols*o.Rows));
                if(o.Tag!="2PCS"&&o.Tag!="2PRS"&&o.Tag!="2PDS")continue;
                Vector3 dir;
                if(o.Tag=="2PDS")dir=o.Vector;
                else if(o.Tag=="2PRS"&&o.Bind&&p.Pos.sqrMagnitude>0)dir=p.Pos.normalized;
                else
                {
                    float phi=R(0,Mathf.PI*2),z=o.Tag=="2PCS"?1-R(0,1)*(1-Mathf.Cos(o.Cone*Mathf.Deg2Rad)):R(-1,1);
                    float radius=Mathf.Sqrt(Mathf.Max(0,1-z*z));dir=new Vector3(radius*Mathf.Cos(phi),radius*Mathf.Sin(phi),z);
                    if(o.Planar){phi=o.Tag=="2PCS"?R(-o.Cone,o.Cone)*Mathf.Deg2Rad:R(0,Mathf.PI*2);dir=o.Tag=="2PCS"?new Vector3(Mathf.Sin(phi),0,Mathf.Cos(phi)):new Vector3(Mathf.Cos(phi),Mathf.Sin(phi),0);}
                }
                p.Velocity+=ParentDirection(parent,dir).normalized*R(o.Speed-o.SpeedVar,o.Speed+o.SpeedVar);
                p.Angular=new Vector3(R(o.Angular.x-o.AngularVar.x,o.Angular.x+o.AngularVar.x),R(o.Angular.y-o.AngularVar.y,o.Angular.y+o.AngularVar.y),R(o.Angular.z-o.AngularVar.z,o.Angular.z+o.AngularVar.z));
            }
        }
        private static int Ramp(Op op,float t,out float f)
        {
            t=Mathf.Repeat(t*op.Repeat,1);int i=0;while(i+1<op.Times.Count&&op.Times[i+1]<t)i++;
            if(i+1>=op.Times.Count){f=0;return i;}f=Mathf.InverseLerp(op.Times[i],op.Times[i+1],t);return i;
        }
        private static float Value(Op op,float t){int i=Ramp(op,t,out float f);return Mathf.Lerp(op.Values[i],op.Values[Math.Min(i+1,op.Values.Count-1)],f);}
        private void Update()
        {
            if(_map==null){Destroy(gameObject);return;}
            float now=C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal,delta=Mathf.Max(0,now-_last);_last=now;
            while(delta>0){float dt=Mathf.Min(.04f,delta);Step(dt);delta-=dt;}
            if(_clock>12 && _emitters.TrueForAll(e=>e.Particles.Count==0))Destroy(gameObject);
        }
        private void Step(float dt)
        {
            _clock+=dt;
            _rootParticle.Age+=dt;_rootParticle.Pos+=_rootParticle.Velocity*dt;
            for(int ei=0;ei<_emitters.Count;ei++)
            {
                var e=_emitters[ei];var d=e.Def;e.Age+=dt;
                int births=0;
                if(e.Parent.Age<e.Parent.Life && e.Age>=d.Start)
                {
                    if(d.Tag=="2BMI"&&!e.Burst){births=d.Count;e.Burst=true;}
                    if(d.Tag=="2CMI"&&e.Age<=d.End){e.Accumulator+=dt*d.Rate;births=(int)e.Accumulator;e.Accumulator-=births;}
                }
                for(int j=0;j<births;j++)
                {
                    var p=new Particle{Pos=e.Parent.Pos,Life=Mathf.Max(.001f,R(d.Life-d.LifeVar,d.Life+d.LifeVar))};Initialize(p,d,e.Parent);e.Particles.Add(p);
                    foreach(int ci in d.Children)if(IsEmitter(_ops[ci]))AddEmitter(_ops[ci],p);
                }
                for(int j=e.Particles.Count-1;j>=0;j--)
                {
                    var p=e.Particles[j];p.Age+=dt;if(p.Age>=p.Life){e.Particles.RemoveAt(j);continue;}
                    foreach(int ci in d.Children)
                    {
                        var o=_ops[ci];float t=p.Age/p.Life;
                        switch(o.Tag)
                        {
                            case "2VPP":p.Velocity=p.Velocity.normalized*Mathf.Lerp(o.Min,o.Max,Value(o,t));break;
                            case "2SIR":float size=Mathf.Lerp(o.Min,o.Max,Value(o,t));if((o.Axis&1)!=0)p.Size.x=size;if((o.Axis&2)!=0)p.Size.y=size;break;
                            case "2ALR":p.Color.a=Value(o,t);break;
                            case "2ACR":int k=Ramp(o,t,out float f);var col=Color.Lerp(o.Colors[k],o.Colors[Math.Min(k+1,o.Colors.Count-1)],f);col.a=p.Color.a;p.Color=col;break;
                            case "2FOR":p.Velocity+=o.Vector*dt;break;
                            // Native uses its PerlinNoise implementation. Unity noise
                            // is a visual approximation pending the native noise port.
                            case "2WIN":p.Velocity+=o.Vector*dt*(Mathf.PerlinNoise(o.Shift*o.Frequency,e.Age*o.Frequency)*2-1);break;
                        }
                    }
                    p.Pos+=p.Velocity*dt;p.Roll+=p.Angular.x*dt;
                }
            }
        }
        private Vector3 World(Vector3 p)=>_map.ArtilleryPointV437(_origin.x+p.x*_cos-p.y*_sin,_origin.y+p.x*_sin+p.y*_cos,_origin.z+p.z);
        private void LateUpdate()
        {
            if(_map==null)return;
            Camera cam=null;foreach(var c in Camera.allCameras)if(c.isActiveAndEnabled&&c.name.Contains("C2_BattleTerrainCamera_Iso")){cam=c;break;}
            if(cam==null)cam=Camera.main;if(cam==null)return;
            foreach(var b in _batches.Values){b.Vertices.Clear();b.UV.Clear();b.Colors.Clear();b.Indices.Clear();}
            foreach(var e in _emitters)
            {
                if(!_batches.TryGetValue(e.Def,out var b))continue;
                int cols=1,rows=1;foreach(int ci in e.Def.Children)if(_ops[ci].Tag=="2FZI"){cols=_ops[ci].Cols;rows=_ops[ci].Rows;}
                foreach(var p in e.Particles)
                {
                    var center=World(p.Pos);float cr=Mathf.Cos(p.Roll),sr=Mathf.Sin(p.Roll);
                    Vector3 right=cam.transform.right*_scale,up=cam.transform.up*_scale;
                    int first=b.Vertices.Count;float u=(p.Frame%cols)/(float)cols,v=(p.Frame/cols)/(float)rows;
                    for(int k=0;k<4;k++)
                    {
                        float x=((k%2)-.5f-b.Render.Ref.x)*p.Size.x,y=((k/2)-.5f-b.Render.Ref.y)*p.Size.y;
                        b.Vertices.Add(center+right*(cr*x-sr*y)+up*(sr*x+cr*y));
                        b.UV.Add(new Vector2(u+(k%2)/(float)cols,1-v-(1-k/2)/(float)rows));b.Colors.Add(p.Color*b.Render.Tint);
                    }
                    b.Indices.Add(first);b.Indices.Add(first+2);b.Indices.Add(first+1);b.Indices.Add(first+1);b.Indices.Add(first+2);b.Indices.Add(first+3);RenderedParticles++;
                }
            }
            foreach(var b in _batches.Values){b.Mesh.Clear();b.Mesh.SetVertices(b.Vertices);b.Mesh.SetUVs(0,b.UV);b.Mesh.SetColors(b.Colors);b.Mesh.SetTriangles(b.Indices,0);}
        }
        private void OnDestroy(){foreach(var b in _batches.Values){Destroy(b.Mesh);Destroy(b.Material);}}
    }
    public sealed partial class C2BattleTerrainMode
    {
        internal Texture2D WeaponParticleTextureV442(string file)=>C2OriginalTextureService.TryLoadTextureByCandidates(_bootstrap.Fs,
            new[]{"Textures/"+file,file},"C2Particle_"+file,C2OriginalTexturePolicy.WorldTextureLikeOriginal,out string audit);
    }
    public sealed partial class C2CombatRuntimeV334LikeOriginal
    {
        private static readonly Dictionary<string,Dictionary<string,string>> ParticleModelsV442=new Dictionary<string,Dictionary<string,string>>();
        private void EmitArtilleryMuzzleV442(int type,int x,int y,int z,int targetX,int targetY)
        {
            string root=Directory.GetParent(Path.GetDirectoryName(_md.Path)).FullName;
            if(!ParticleModelsV442.TryGetValue(root,out var models))
            {
                models=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);ParticleModelsV442[root]=models;
                foreach(string line in File.ReadAllLines(Path.Combine(root,"weapon.ads")))
                {
                    var t=SplitDataTokensLikeOriginal(CleanDataLineLikeOriginal(line));
                    if(t.Length>=4&&t[0].StartsWith("!"))models["#"+t[0].Substring(1)]=t[3];
                }
            }
            var defs=LoadWeaponDefinitionsLikeOriginal(Path.Combine(root,"weapon.nds"));
            var effect=ResolveWeaponEffectLikeOriginal(_md,type);if(defs==null)return;
            var todo=new Queue<string>();var seen=new HashSet<string>();todo.Enqueue(effect.RootName);
            while(todo.Count>0)
            {
                var name=todo.Dequeue();if(!seen.Add(name)||!defs.TryGetValue(name,out var d))continue;
                // Sync weapons start together. Child/explosion graphs are deferred.
                if((d.AnimationName=="#DIM32"||d.AnimationName=="#DIM32P"||d.AnimationName=="#GAUBVISTREL")&&models.TryGetValue(d.AnimationName,out var model))
                    C2WeaponParticlesV442.Spawn(_unit.OwnerMode,_md.Path,model,new Vector3(x,y,z),C2OriginalMovementMathV352.GetDir(targetX/16-x,targetY/16-y));
                foreach(var sync in d.Sync)todo.Enqueue(sync);
            }
        }
    }
}
