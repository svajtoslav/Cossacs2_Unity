using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // FireBolt is a WeaponSystem weapon, not ROCKETKON from weapon.nds.
    // Read the retail graph's active branch rather than substituting a cannon.
    internal sealed class C2RocketProfileV443
    {
        internal sealed class Blast
        {
            internal int Percent,Radius,MaxUnits,PushForce;
            internal bool OnlyEnemy,Buildings;
        }
        internal int Speed,Gravity,Scatter,FlightPercent,MaxMilliseconds,HeightLess,HeightMore,AbsHeightMore;
        internal string Model,Animation,Trail,Start,Explosion;
        internal float ModelScale;
        internal readonly List<Blast> Blasts=new List<Blast>();
        private static readonly Dictionary<string,C2RocketProfileV443> Cache=new Dictionary<string,C2RocketProfileV443>();
        internal static C2RocketProfileV443 Load(string md)
        {
            string path=Path.Combine(Directory.GetParent(Path.GetDirectoryName(md)).FullName,"WeaponSystem/base.ws.xml");
            if(Cache.TryGetValue(path,out var p))return p;
            // The engine serializes an unnamed root, which XML libraries reject.
            string xml=File.ReadAllText(path).Replace("<>","<Weapons>").Replace("</>","</Weapons>");
            var nodes=XDocument.Parse(xml).Root.Elements("WeaponModificator").ToList();
            Func<string,XElement> recipe=name=>nodes.First(n=>(string)n.Element("Name")==name);
            var root=recipe("FireBolt");
            var birth=root.Descendants("BirthNew").First(n=>Int(n,"DamageChange")>0);
            var flight=recipe((string)birth.Element("NewWeaponModificator"));
            var motion=flight.Descendants("BalisticMotion").Single();
            var draws=flight.Descendants("DrawOne").Select(n=>n.Element("Anim")).ToList();
            var impact=flight.Descendants("WeaponProcess").First(n=>n.Descendants("BirthNew").Any());
            var condition=impact.Element("Event").Element("Conditions");
            p=new C2RocketProfileV443{
                Speed=Int(motion,"ConstSpeed"),Gravity=Int(motion,"g"),Scatter=Int(birth,"To_RandomPosInRadius"),
                FlightPercent=Int(birth,"DamageChange"),Model=(string)draws[0].Element("ModelID"),
                Animation=(string)draws[0].Element("AnimationID"),ModelScale=Float(draws[0],"Scale"),
                Trail=(string)draws[1].Element("ModelID"),
                HeightLess=Int(condition,"HeightLess"),HeightMore=Int(condition,"HeightMore"),AbsHeightMore=Int(condition,"AbsHeightMore"),
                MaxMilliseconds=flight.Descendants("LifeTimeMore").Select(n=>int.Parse(n.Value)).Max(),
                Start=(string)recipe("start").Descendants("ModelID").Single(),
                Explosion=(string)recipe("Explode").Descendants("ModelID").Single()
            };
            var damageBirth=impact.Descendants("BirthNew").First(n=>Int(n,"DamageChange")>0);
            string next=(string)damageBirth.Element("NewWeaponModificator");
            // Percentages compound: 10000 -> 2000 -> 200 -> 100.
            while(!string.IsNullOrEmpty(next))
            {
                var node=recipe(next);var damage=node.Descendants("MakeDamage").Single();
                p.Blasts.Add(new Blast{Percent=Int(damageBirth,"DamageChange"),Radius=Int(damage,"InRadius"),
                    MaxUnits=Int(damage,"MaxUnits"),PushForce=Int(damage,"PushUnitsForce"),
                    OnlyEnemy=(string)damage.Element("OnlyEnemyUnits")=="true",Buildings=(string)damage.Element("DamageBuilding")=="true"});
                damageBirth=node.Descendants("BirthNew").FirstOrDefault();next=(string)damageBirth?.Element("NewWeaponModificator");
            }
            if(p.Speed<=0||p.Gravity<=0||p.Blasts.Count!=3||p.MaxMilliseconds<=0)
                throw new InvalidDataException("Unsupported FireBolt configuration");
            Cache[path]=p;return p;
        }
        private static int Int(XElement n,string name)=>int.Parse(n.Element(name).Value,CultureInfo.InvariantCulture);
        private static float Float(XElement n,string name)=>float.Parse(n.Element(name).Value,CultureInfo.InvariantCulture);
    }

    // WeaponSystem.cpp::BalisticMotion, in 1/16-pixel coordinates and native
    // TRUETIME milliseconds (AnimTime*10/64; one simulation tick = 40 ms).
    internal sealed class C2RocketFlightV443
    {
        internal readonly Vector3 Start,Target;
        internal readonly int Direction,Vx,Vy,Vz,Gravity;
        internal Vector3 Position {get;private set;}
        internal float Pitch {get;private set;}
        internal C2RocketFlightV443(C2RocketProfileV443 p,Vector3 start,Vector3 target,Func<int> random,Func<int,int,int> height)
        {
            Start=start;int tx=(int)target.x*16,ty=(int)target.y*16;
            if(p.Scatter>0){tx+=random()%p.Scatter-p.Scatter/2;ty+=random()%p.Scatter-p.Scatter/2;}
            Target=new Vector3(tx/16f,ty/16f,Math.Max(0,height(tx>>4,ty>>4)));
            int dx=tx-(int)start.x*16,dy=ty-(int)start.y*16;
            int distance=(int)Math.Sqrt((double)dx*dx+(double)dy*dy),mt=Math.Max(1,distance*100/p.Speed);
            Direction=C2OriginalMovementMathV352.GetDir(dx,dy);
            Vx=(p.Speed*C2OriginalMovementMathV352.TCos[Direction])>>8;
            Vy=(p.Speed*C2OriginalMovementMathV352.TSin[Direction])>>8;
            Vz=p.Gravity*mt/200+100*((int)Target.z*16-(int)start.z*16)/mt;
            Gravity=p.Gravity;Position=start;
        }
        internal Vector3 At(int milliseconds)
        {
            int x=(int)Start.x*16+Vx*milliseconds/100;
            int y=(int)Start.y*16+Vy*milliseconds/100;
            // Retain integer truncation without overflowing during the graph's
            // 18-second lifetime when no valid ground impact has occurred.
            int z=(int)((long)(int)Start.z*16+(long)Vz*milliseconds/100-(long)Gravity*milliseconds*milliseconds/20000);
            Position=new Vector3(x/16f,y/16f,z/16f);
            Pitch=Mathf.Atan2(Vz-Gravity*milliseconds/100,Mathf.Sqrt(Vx*Vx+Vy*Vy));
            return Position;
        }
    }

    internal sealed class C2RocketProjectileV443 : MonoBehaviour
    {
        internal static int Launched,Impacts,DamageEvents;
        internal static Vector3 LastImpact;
        private C2BattleTerrainMode _map;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _source;
        private C2RocketProfileV443 _profile;
        private C2RocketFlightV443 _flight;
        private C2WeaponParticlesV442 _trail;
        private C2AnimatedModelV437LikeOriginal _model;
        private Matrix4x4[] _matrices;
        private Mesh[] _meshes;
        private Vector3[][] _vertices,_normals;
        private string _md;
        private int _damage,_type,_born,_ticks;
        private Action<C2SettlementBuildingSelectableV1LikeOriginal,int> _buildingDamage;
        internal C2RocketFlightV443 Flight=>_flight;
        private static readonly Dictionary<string,int> MediaRadius=new Dictionary<string,int>();

        internal static void Spawn(C2BattleTerrainMode map,string md,Vector3 start,Vector3 target,
            C2NeutralPeasantUnitInfoV2LikeOriginal source,int damage,int type,Action<C2SettlementBuildingSelectableV1LikeOriginal,int> buildingDamage)
        {
            GameObject go=null;
            try
            {
                var profile=C2RocketProfileV443.Load(md);
                go=new GameObject("C2_RocketProjectileV443_FireBolt");var rocket=go.AddComponent<C2RocketProjectileV443>();
                rocket._profile=profile;rocket._map=map;rocket._source=source;rocket._md=md;rocket._damage=damage*profile.FlightPercent/100;rocket._type=type;
                rocket._buildingDamage=buildingDamage;rocket._born=C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal;
                rocket._flight=new C2RocketFlightV443(profile,start,target,()=>C2RetailRandomV407LikeOriginal.Rando(source),map.C2OriginalFogTerrainHeightV1LikeOriginal);
                rocket.CreateModel();
                C2WeaponParticlesV442.Spawn(map,md,profile.Start,start,rocket._flight.Direction);
                rocket._trail=C2WeaponParticlesV442.CreateV443(map,md,profile.Trail,start,rocket._flight.Direction);
                if(rocket._trail==null)throw new InvalidDataException("Rocket trail failed to load");
                Launched++;Debug.Log("[C2 V443 ROCKET] FireBolt launched owner="+source.CombatNationLikeOriginal+" target="+rocket._flight.Target);
            }
            catch(Exception e){if(go!=null)Destroy(go);Debug.LogError("[C2 V443 ROCKET] "+e);}
        }
        private void CreateModel()
        {
            _model=_map.ArtilleryModelV437(_profile.Model);_matrices=new Matrix4x4[_model.Nodes.Length];
            // DrawOne.Frame is zero in this graph, including its animation.
            _model.Evaluate(_map.ArtilleryClipV437(_profile.Animation),0,_matrices);
            _meshes=new Mesh[_model.Meshes.Length];_vertices=new Vector3[_meshes.Length][];_normals=new Vector3[_meshes.Length][];
            if(_meshes.Length==0)throw new InvalidDataException("Empty rocket model");
            for(int i=0;i<_meshes.Length;i++)
            {
                var g=_model.Meshes[i];var child=new GameObject("Rocket_"+i);child.layer=7;child.transform.SetParent(transform,false);
                var mesh=_meshes[i]=new Mesh{name="Original FireBolt"};mesh.MarkDynamic();mesh.vertices=g.Vertices;mesh.uv=g.UV;mesh.colors32=g.Colors;mesh.triangles=g.Triangles;
                _vertices[i]=new Vector3[g.Vertices.Length];_normals[i]=new Vector3[g.Normals.Length];
                child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=_map.ArtilleryMaterialV437(g,_source.CombatNationLikeOriginal);
            }
        }
        private void Update()
        {
            if(_map==null||_flight==null){Destroy(gameObject);return;}
            int tick=C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal-_born;
            while(_ticks<tick)
            {
                Vector3 previous=_flight.Position;int ms=++_ticks*40;Vector3 current=_flight.At(ms);
                if(ms>_profile.MaxMilliseconds){Destroy(gameObject);return;}
                if(C2BuildingRuntimeInfoV247LikeOriginal.TryIntersect3DBarSegmentV378LikeOriginal(_map,
                    _map.ArtilleryPointV437(previous.x,previous.y,previous.z),_map.ArtilleryPointV437(current.x,current.y,current.z),
                    out var building,out float rx,out float ry))
                {Explode(new Vector3(rx/16,ry/16,current.z),building);return;}
                // Sweep the narrow native 0..10 height band so a 40ms update
                // cannot jump over it and lose the explosion on descending flight.
                int steps=Math.Max(1,Mathf.CeilToInt((current-previous).magnitude/4));
                for(int s=1;s<=steps;s++)
                {
                    var p=Vector3.Lerp(previous,current,s/(float)steps);
                    int h=Math.Max(0,_map.C2OriginalFogTerrainHeightV1LikeOriginal((int)p.x,(int)p.y));
                    float height=p.z-h;
                    if(p.z>_profile.AbsHeightMore&&height>_profile.HeightMore&&height<_profile.HeightLess)
                    {Explode(p,null);return;}
                }
                if(_trail!=null)_trail.MoveSourceV443(current);
            }
        }
        private void Explode(Vector3 position,C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            LastImpact=position;Impacts++;
            C2WeaponParticlesV442.Spawn(_map,_md,_profile.Explosion,position,0);
            if(_source!=null)
            {
                int damage=_damage;
                foreach(var blast in _profile.Blasts)
                {
                    damage=damage*blast.Percent/100;
                    if(blast.Buildings&&building!=null)_buildingDamage?.Invoke(building,damage);
                    int count=0;
                    foreach(var unit in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
                    {
                        if(unit==null||unit.IsDeadLikeOriginal||!unit.isActiveAndEnabled)continue;
                        if(blast.MaxUnits>0&&count>=blast.MaxUnits)break;
                        if(blast.OnlyEnemy&&!C2CombatCoreV408LikeOriginal.CanAttackRelationV408LikeOriginal(_source,unit))continue;
                        int distance=C2OriginalMovementMathV352.Norma((int)position.x-(int)unit.RealXFloat/16,(int)position.y-(int)unit.RealYFloat/16);
                        if(distance>=blast.Radius+ExplosionMediaRadius(unit))continue;
                        int dealt=C2CombatCoreV408LikeOriginal.MakeDamageV408LikeOriginal(unit,damage,_source,_type,true,!blast.OnlyEnemy);
                        if(dealt>0)DamageEvents++;count++;
                        // PushUnitsForce is retained in the profile. Its native
                        // FallDown order is not yet exposed by the unit adapter.
                    }
                }
            }
            Debug.Log("[C2 V443 ROCKET] impact="+position+" damageEvents="+DamageEvents);Destroy(gameObject);
        }
        private static int ExplosionMediaRadius(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            string path=C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit).Path;
            if(MediaRadius.TryGetValue(path,out int radius))return radius;
            foreach(string line in File.ReadLines(path))
            {
                var t=line.Trim().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(t.Length>=3&&t[0]=="EXPLMEDIA")int.TryParse(t[2],out radius);
            }
            MediaRadius[path]=radius;return radius;
        }
        private void LateUpdate()
        {
            if(_flight==null||_meshes==null||_map==null)return;
            var p=_flight.Position;float a=_flight.Direction*Mathf.PI/128,c=Mathf.Cos(a),s=Mathf.Sin(a),cp=Mathf.Cos(_flight.Pitch),sp=Mathf.Sin(_flight.Pitch);
            Vector3 x=new Vector3(c*cp,s*cp,sp),y=new Vector3(-s,c,0),z=Vector3.Cross(x,y);
            var origin=_map.ArtilleryPointV437(p.x,p.y,p.z);var placement=Matrix4x4.identity;
            Vector3[] axes={x,y,z};for(int i=0;i<3;i++){var v=axes[i]*_profile.ModelScale;placement.SetColumn(i,_map.ArtilleryPointV437(p.x+v.x,p.y+v.y,p.z+v.z)-origin);}
            placement.SetColumn(3,new Vector4(origin.x,origin.y,origin.z,1));
            for(int i=0;i<_meshes.Length;i++)
            {
                var g=_model.Meshes[i];var m=placement*_matrices[g.NodeIndex];var normal=m.inverse.transpose;
                for(int j=0;j<g.Vertices.Length;j++){_vertices[i][j]=m.MultiplyPoint3x4(g.Vertices[j]);_normals[i][j]=normal.MultiplyVector(g.Normals[j]).normalized;}
                _meshes[i].vertices=_vertices[i];_meshes[i].normals=_normals[i];_meshes[i].RecalculateBounds();
            }
        }
        private void OnDestroy()
        {
            if(_trail!=null)_trail.StopSourceV443();
            if(_meshes!=null)foreach(var mesh in _meshes)if(mesh!=null)Destroy(mesh);
        }
    }
    public sealed partial class C2CombatRuntimeV334LikeOriginal
    {
        private bool TryEmitRocketV443(int type,int x,int y,int z,int tx,int ty,int tz)
        {
            var traits=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(_unit);
            if(traits.WeaponName[type]!="FireBolt")return false;
            var source=_unit;
            C2RocketProjectileV443.Spawn(_unit.OwnerMode,_md.Path,new Vector3(x,y,z),new Vector3(tx/16,ty/16,tz),_unit,traits.MaxDamage[type],type,
                (building,damage)=>{
                    var oldUnit=_unit;var oldTarget=_targetUnit;var oldBuilding=_targetBuilding;int oldMode=_attackMode;bool oldActive=_active;
                    try{_unit=source;_targetUnit=null;_targetBuilding=null;_attackMode=type;ApplyDamageToTargetLikeOriginal(damage,0,0,0,building);}
                    finally{_unit=oldUnit;_targetUnit=oldTarget;_targetBuilding=oldBuilding;_attackMode=oldMode;_active=oldActive;}
                });
            return true;
        }
    }
}
