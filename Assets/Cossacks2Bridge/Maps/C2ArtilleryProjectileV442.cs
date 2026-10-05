using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2FlightChildrenV442
    {
        internal int Min, Max;
        internal string[] Names;
    }
    internal sealed class C2FlightWeaponV442
    {
        internal string Name, Animation;
        internal int Damage, Radius, Speed, Propagation, Gravity, ReflectAngle, DamageHeight, Times;
        internal int MinChildren, MaxChildren, HotFrame;
        internal bool FullParent, Splash;
        internal int RandomAngle, RandomAngleZX=16, DamageDec;
        internal string[] Children,Sync;
        internal readonly Dictionary<string,C2FlightChildrenV442> Custom = new Dictionary<string,C2FlightChildrenV442>(StringComparer.OrdinalIgnoreCase);
        // ADS sprites remain alive through movement/ricochets. Native 3D
        // explosion graphs have independent particle lifetimes.
        internal bool HasBallSprite => Animation == "#NUCL1" || Animation == "#YADROGAUB";
        internal bool HasMovingSprite => HasBallSprite || Animation == "#OSKOL";
    }

    // Weapon.cpp::Create3DAnmObjectEX / ProcessExpl: WEPSH=14,
    // FrmDec=2, SpeedSh=1. Independent of rendering, so tests exercise exactly
    // the same integer flight/terrain/child simulation as the live projectile.
    internal sealed class C2BallFlightV442
    {
        internal readonly Dictionary<string,C2FlightWeaponV442> Weapons;
        internal C2FlightWeaponV442 Weapon { get; private set; }
        internal int X,Y,Z,Vx,Vy,Vz,Az, Bounces, Impacts;
        internal bool Finished { get; private set; }
        internal Vector3 Position => new Vector3(X/16384f,Y/16384f,Z/16384f);
        internal Action<C2FlightWeaponV442,Vector3> Damage;
        internal Func<C2FlightWeaponV442,Vector3,int,bool> ContactDamageV444;
        private bool DamageHereV444()
        {
            Damage?.Invoke(Weapon,Position);
            int decay=Weapon.Times>0?Math.Max(0,_age-1)*Weapon.DamageDec/100:0;
            return ContactDamageV444?.Invoke(Weapon,Position,decay)??false;
        }
        internal Action<C2FlightWeaponV442,Vector3,string> Impact;
        internal Action<string,Vector3> WaterVisual;
        internal Action<C2FlightWeaponV442,Vector3> SpawnChild;
        private readonly Func<int,int,int> _height;
        private readonly Func<int,int,string> _media;
        private readonly Func<int> _random;
        private readonly int _tx,_ty,_tz;
        private int _age, _transitions;

        internal C2BallFlightV442(Dictionary<string,C2FlightWeaponV442> weapons,string name,Vector3 start,Vector3 target,
            Func<int,int,int> height,Func<int,int,string> media,Func<int> random)
        {
            Weapons=weapons;_height=height;_media=media;_random=random;
            X=(int)start.x<<14;Y=(int)start.y<<14;Z=(int)start.z<<14;
            _tx=(int)target.x;_ty=(int)target.y;_tz=(int)target.z;
            StartWeapon(weapons[name]);
        }

        private void StartWeapon(C2FlightWeaponV442 next)
        {
            Weapon=next;_age=0;
            if(++_transitions>32){Finished=true;return;}
            _random(); // Native ASerial=rando(), before velocity setup.
            int x=X>>14,y=Y>>14,z=Z>>14,h=_height(x,y);
            if(z<h)z=h+1;
            X=x<<14;Y=y<<14;Z=z<<14;Az=-next.Gravity*8000;
            if(next.Propagation==7)
            {
                bool reflects=ReflectVelocity(ref Vx,ref Vy,ref Vz,next.Speed,next.ReflectAngle,
                    _height(x-32,y)-_height(x+32,y),_height(x,y+32)-_height(x,y-32));
                if(reflects)Bounces++;
                else Explode(false,null);
                return;
            }
            if(next.Propagation==0 || next.Propagation==1){Vx=Vy=0;Vz=next.Propagation==1?(next.Speed*2)<<14:0;return;}
            if(next.Propagation==2 || next.Propagation==6)
            {int speed=next.Speed<<1;Vx=(_random()-16384)*speed;Vy=(_random()-16384)*speed;Vz=(_random()*speed)>>1;return;}
            int dx=_tx-x,dy=_ty-y;
            int dist=(int)Math.Sqrt((double)dx*dx+(double)dy*dy);
            if(next.Propagation==3 || next.Propagation==8)
            {
                int time=dist/Math.Max(1,next.Speed<<1);
                Vx=(dx<<14)/(time+1);Vy=(dy<<14)/(time+1);
                if(next.Propagation==8 && time==0)
                {
                    // Native SECTOR clamps time to 1 before computing velocity.
                    time=1;Vx=(dx<<14)/(time+1);Vy=(dy<<14)/(time+1);
                }
                Vz=((_tz-z)<<14)/(time+1)-((Az*(time+2))>>1);
                if(next.Propagation==8)
                {
                    int ax=Vx>>8,ay=Vy>>8,az=Vz>>8;
                    int v=(int)Math.Sqrt((double)ax*ax+(double)ay*ay+(double)az*az);
                    int d=((((_random()>>7)-128)*v)>>7)*next.RandomAngle/100;Vx+=d<<8;
                    d=((((_random()>>7)-128)*v)>>7)*next.RandomAngle/100;Vy+=d<<8;
                    d=((((_random()>>7)-128)*v)>>7)*next.RandomAngle/100;Vz+=(d<<8)*next.RandomAngleZX/256;
                }
                return;
            }
            if(next.Propagation==5)
            {
                double tangent=Math.Tan(next.Speed*3.1415/180);
                int rise=_tz-z-(int)(dist*tangent);
                if(rise>=0||Az==0){Finished=true;return;}
                int time=Math.Max(1,(int)(4*Math.Sqrt((double)(rise<<11)/Az)));
                Vx=(dx<<14)/time;Vy=(dy<<14)/time;
                Vz=(int)((int)Math.Sqrt((double)Vx*Vx+(double)Vy*Vy)*tangent);
                return;
            }
            Finished=true;
        }

        internal static bool ReflectVelocity(ref int vx,ref int vy,ref int vz,int speed,int limit,int nx,int ny)
        {
            // Preserve both truncation stages, including the arithmetic shift
            // of negative velocity. Speed is damping, not a new launch speed.
            vx>>=16;vy>>=16;vz>>=16;int angle=0;
            if(vx!=0||vy!=0||vz!=0)
            {
                int norm=(int)Math.Sqrt((double)nx*nx+(double)ny*ny+1024);
                int vn=2*(vx*nx+vy*ny+vz*32)/norm;
                int length=(int)Math.Sqrt((double)vx*vx+(double)vy*vy+(double)vz*vz);
                vx-=vn*nx/norm;vy-=vn*ny/norm;vz-=vn*32/norm;
                angle=(vn<<5)/length;
            }
            vx=(vx*speed)<<10;vy=(vy*speed)<<10;vz=(vz*speed)<<10;
            return Math.Abs(angle)<=limit;
        }

        internal void Tick(Func<Vector3,Vector3,bool> buildingCollision=null)
        {
            if(Finished)return;
            Vector3 previous=Position;
            Vz+=Az;X+=Vx;Y+=Vy;Z+=Vz;_age++;
            int p=Weapon.Propagation;
            bool aimed=p==3||p==5;
            long distance=Math.Abs((long)X-((long)_tx<<14))+Math.Abs((long)Y-((long)_ty<<14))+Math.Abs((long)Z-((long)_tz<<14));
            // The original permits ricochet near the aim point as well as at
            // actual terrain contact; it does not erase the ball at t=1.
            if(aimed&&distance<65536*4){Explode(true,null);return;}
            // Weapon.cpp checks 3D bars for propagation 2..5, including RANDOM
            // howitzer fragments and their debris. Restricting this to aimed
            // balls lets those damaging children pass through walls and roofs.
            if(p>=2&&p<=5&&buildingCollision!=null&&buildingCollision(previous,Position)){Explode(false,"BUILDING");return;}
            int h=_height(X>>14,Y>>14);
            if(Weapon.DamageHeight>0 && h>(Z>>14)-Weapon.DamageHeight)
            {
                bool hit=DamageHereV444();
                // Weapon.cpp: SECTOR pellets stop at the first contact; balls
                // continue through the ranks and retain their ricochet chain.
                if(hit && p==8){Explode(false,null);return;}
            }
            if(h>(Z>>14)){Explode(true,null);return;}
            if(p==8 && Weapon.Times>0 && _age>=Weapon.Times){Finished=true;return;}
            if((p==0||p==1)&&Weapon.Times>0)
            {
                // #NUCL1 has one frame. SHIPIT: Times=20, HotFrame=254 (end).
                // PUWALL: Times=1, HotFrame=0 (its first and last frame).
                if(Weapon.HotFrame==0 && !Weapon.HasBallSprite)
                {
                    // Single-frame #POINT emits its children on each hot frame
                    // (NUCLM: two cycles). Native 3D effects have hot frame 0
                    // once; their independent renderer finishes the visual.
                    bool repeat=Weapon.Animation=="#POINT" && _age<Weapon.Times;
                    Explode(false,null,repeat);
                }
                else if(_age>=Weapon.Times)Explode(false,null);
            }
        }

        private void Explode(bool landing,string media,bool repeat=false)
        {
            if(Finished)return;
            Impacts++;DamageHereV444();
            media=media??(_height(X>>14,Y>>14)<=0?"WATER":_media(X>>14,Y>>14));
            Impact?.Invoke(Weapon,Position,media);
            string[] choices=Weapon.Children;int min=Weapon.MinChildren,max=Weapon.MaxChildren;
            C2FlightChildrenV442 custom=null;
            bool customExplosion=media!=null&&Weapon.Custom.TryGetValue(media,out custom);
            if(customExplosion)
            {choices=custom.Names;min=custom.Min;max=custom.Max;}
            var parent=Weapon;bool continuing=false;
            // Cannon/default and water chains each have one child. Keep the
            // random draw even when MinChild==MaxChild.
            int count=choices==null||choices.Length==0?0:min+(((max-min)*_random())>>15);
            for(int n=0;n<count;n++)
            {
            C2FlightWeaponV442 child=null;
            for(int tries=0;tries<20;tries++)
            {
                if(!Weapons.TryGetValue(choices[(_random()*choices.Length)>>15],out child))break;
                if(landing||child.Propagation!=7)break;
            }
            if(child==null||(!landing&&child.Propagation==7))continue;
            if(media=="WATER")
            {
                WaterVisual?.Invoke(child.Animation,Position);continue;
            }
            if(child.HasBallSprite && count==1 && !repeat){StartWeapon(child);continuing=!Finished;}
            else SpawnChild?.Invoke(child,Position);
            }
            // Weapon.cpp::ExplodeAnmObject suppresses SyncWeapon only when a
            // custom medium branch replaces the default explosion.
            if(!customExplosion && parent.Sync!=null)
                foreach(string name in parent.Sync)if(Weapons.TryGetValue(name,out var sync))SpawnChild?.Invoke(sync,Position);
            if(!continuing && !repeat)Finished=true;
        }
    }

    internal sealed class C2ArtilleryProjectileV442 : MonoBehaviour
    {
        private C2BattleTerrainMode _map;
        private C2BallFlightV442 _flight;
        private C2CombatMuzzleSmokeV390LikeOriginal _visual;
        private float _born;
        private int _ticks;
        internal static int CreatedV442,ImpactedV442,WaterImpactsV442,RicochetsV442;
        internal static int ExplosionsV443,FragmentsV443,CanisterPelletsV444;
        internal C2BallFlightV442 FlightV442 => _flight;
        internal static void Spawn(C2BattleTerrainMode map,string md,Dictionary<string,C2FlightWeaponV442> weapons,string weapon,
            Vector3 start,Vector3 target,C2NeutralPeasantUnitInfoV2LikeOriginal source,
            Func<C2FlightWeaponV442,C2SettlementBuildingSelectableV1LikeOriginal,float,float,int,bool> damage,int depth=0,
            bool suppressRootVisual=false)
        {
            if(depth>8)return;
            var go=new GameObject("C2_ArtilleryProjectileV442_"+weapon);
            var p=go.AddComponent<C2ArtilleryProjectileV442>();p._map=map;
            p._born=C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
            p._flight=new C2BallFlightV442(weapons,weapon,start,target,map.C2OriginalFogTerrainHeightV1LikeOriginal,
                map.ArtillerySurfaceMediaV442,()=>C2RetailRandomV407LikeOriginal.Rando(source));
            p._flight.ContactDamageV444=(w,pos,decay)=>w.Damage!=0 && w.Radius>0 && damage(w,p._stepBuilding,pos.x*16,pos.y*16,decay);
            p._flight.SpawnChild=(w,pos)=>Spawn(map,md,weapons,w.Name,pos,target,source,damage,depth+1);
            p._flight.Impact=(w,pos,media)=>ImpactedV442++;
            p._flight.WaterVisual=(animation,pos)=>{
                WaterImpactsV442++;var world=map.ArtilleryPointV437(pos.x,pos.y,0);
                C2CombatMuzzleSmokeV390LikeOriginal.SpawnLikeOriginal(world,world,md,animation,0,map,Vector2.right,true);
            };
            go.transform.position=map.ArtilleryPointV437(start.x,start.y,start.z);
            var profile=weapons[weapon];
            if(profile.HasMovingSprite)
                p._visual=C2CombatMuzzleSmokeV390LikeOriginal.SpawnLikeOriginal(go.transform.position,go.transform.position,
                    md,profile.Animation,0,map,Vector2.right,true,true);
            else if(profile.Animation!="#POINT" && !suppressRootVisual)
            {
                if(C2WeaponParticlesV442.SpawnAnimationV443(map,md,profile.Animation,start,0))ExplosionsV443++;
            }
            if(profile.Animation=="#OSKOL")FragmentsV443++;
            if(profile.Propagation==8)CanisterPelletsV444++;
            if(p._visual!=null)p._visual.transform.SetParent(go.transform,true);
            CreatedV442++;
        }
        private C2SettlementBuildingSelectableV1LikeOriginal _stepBuilding;
        private bool BuildingCollision(Vector3 from,Vector3 to)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.TryIntersect3DBarSegmentV378LikeOriginal(_map,
                _map.ArtilleryPointV437(from.x,from.y,from.z),_map.ArtilleryPointV437(to.x,to.y,to.z),
                out _stepBuilding,out float rx,out float ry);
        }
        private void Update()
        {
            if(_map==null){Destroy(gameObject);return;}
            int targetTicks=Mathf.FloorToInt((C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal-_born)*25);
            try
            {
                while(_ticks<targetTicks&&!_flight.Finished)
                {
                    _ticks++;_stepBuilding=null;int previousBounces=_flight.Bounces;
                    _flight.Tick(BuildingCollision);RicochetsV442+=_flight.Bounces-previousBounces;
                }
                Vector3 pos=_flight.Position;
                transform.position=_map.ArtilleryPointV437(pos.x,pos.y,pos.z);
            }
            finally { if(_flight.Finished)Destroy(gameObject); }
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        private string[] _artilleryTextureMediaV442;
        internal string ArtillerySurfaceMediaV442(int x,int y)
        {
            // Read media names: the renderer's old numeric texture enum is
            // different from Nature.dat's explosion-media enum.
            if(_artilleryTextureMediaV442==null)
            {
                _artilleryTextureMediaV442=new string[256];
                if(_bootstrap!=null&&_bootstrap.Fs!=null&&_bootstrap.Fs.Exists("textures.lst"))
                foreach(string line in _bootstrap.Fs.ReadAllText("textures.lst",System.Text.Encoding.ASCII).Split('\n'))
                {
                    var t=line.Trim().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                    if(t.Length<2||t[0].StartsWith("/")||t[0].StartsWith("#")||!int.TryParse(t[1],out int id)||id<0||id>=256)continue;
                    int marker=Array.IndexOf(t,"#");
                    if(marker>=0&&marker+1<t.Length)_artilleryTextureMediaV442[id]=t[marker+1];
                }
            }
            var map=ResolveLiteralITerraRuntimeMapLikeOriginal()??_map;
            if(map==null||map.TexMap==null||map.VertInLine<=0||map.MaxTH<=0)return null;
            // Weapon.cpp: TriUnit=16; VertInLine is native MaxTH+1.
            int col=Mathf.Clamp((x+16)/32,0,map.VertInLine-1);
            int row=Mathf.Clamp((y+((col&1)==0?16:0))/32,0,map.MaxTH-1);
            int index=col+row*map.VertInLine;
            return index<map.TexMap.Length?_artilleryTextureMediaV442[map.TexMap[index]]:null;
        }
    }

    public sealed partial class C2CombatRuntimeV334LikeOriginal
    {
        private static readonly Dictionary<string,Dictionary<string,C2FlightWeaponV442>> FlightMetadataV442=
            new Dictionary<string,Dictionary<string,C2FlightWeaponV442>>(StringComparer.OrdinalIgnoreCase);
        internal static Dictionary<string,C2FlightWeaponV442> LoadFlightWeaponsV442(string path)
        {
            if(FlightMetadataV442.TryGetValue(path,out var result))return result;
            var defs=LoadWeaponDefinitionsLikeOriginal(path);
            if(defs==null)return null;
            result=new Dictionary<string,C2FlightWeaponV442>(StringComparer.OrdinalIgnoreCase);
            foreach(var d in defs.Values)result[d.Name]=new C2FlightWeaponV442 {
                Name=d.Name,Animation=d.AnimationName,Damage=d.Damage,Radius=d.Radius,Speed=d.Speed,
                Propagation=d.Propagation,FullParent=d.FullParent,Children=d.Children.ToArray(),Sync=d.Sync.ToArray()
            };
            string section="";
            foreach(string line in File.ReadAllLines(path))
            {
                var t=SplitDataTokensLikeOriginal(CleanDataLineLikeOriginal(line));if(t.Length==0)continue;
                if(t[0].StartsWith("[")){section=t[0];continue;}
                if(section=="[MEMBERS]" && t.Length==8 && t[0]=="NUCLM" && t[1]=="#POINT")
                {
                    // Retail weapon.nds omits the radius on this carrier row.
                    // Preserve its declared damage but use no invented blast
                    // radius; the explicit NUCLMOSKOL children do the damage.
                    result[t[0]]=new C2FlightWeaponV442{Name=t[0],Animation=t[1],Damage=int.Parse(t[2]),Radius=0,
                        Gravity=5,Propagation=1,Speed=int.Parse(t[6]),Times=int.Parse(t[7])};
                }
                if(!result.TryGetValue(t[0],out var w))continue;
                if(section=="[MEMBERS]"&&t.Length>=9)
                {
                    w.Gravity=t[5]=="NO_GRAVITY"?0:t[5]=="LO_GRAVITY"?1:t[5]=="HI_GRAVITY"?2:t[5]=="HI_GRAVITY1"?3:t[5]=="HI_GRAVITY2"?4:5;
                    int.TryParse(t[8],out w.Times);
                    // stRecog.cpp: tail = RandomAngle, DamageDec, RandomAngleZX.
                    // REFLECT uses RandomAngle as its reflection limit here.
                    if(t.Length>9){int.TryParse(t[9],out w.ReflectAngle);w.RandomAngle=w.ReflectAngle;}
                    if(t.Length>10)int.TryParse(t[10],out w.DamageDec);
                    if(t.Length>11)int.TryParse(t[11],out w.RandomAngleZX);
                }
                if(section=="[CHILDWEAPON]"&&t.Length>=5)
                {int.TryParse(t[1],out w.MinChildren);int.TryParse(t[2],out w.MaxChildren);int.TryParse(t[3],out w.HotFrame);
                    int childCount=int.Parse(t[4]);w.Children=new string[childCount];Array.Copy(t,5,w.Children,0,childCount);}
                if(section=="[EXPARAMS]"&&t.Length>1&&t[1]=="SPLASH")w.Splash=true;
                if(section=="[DAMAGEHEIGHT]"&&t.Length>1)int.TryParse(t[1],out w.DamageHeight);
                if(section=="[CUSTOMEXPLOSION]"&&t.Length>6&&int.TryParse(t[5],out int n)&&t.Length>=6+n)
                {
                    var children=new C2FlightChildrenV442 {Names=new string[n]};
                    Array.Copy(t,6,children.Names,0,n);int.TryParse(t[2],out children.Min);int.TryParse(t[3],out children.Max);
                    w.Custom[t[1]]=children;
                }
            }
            FlightMetadataV442[path]=result;return result;
        }
        private bool TryEmitArtilleryProjectileV442(int type,int xs,int ys,int zs,int tx,int ty,int tz)
        {
            string path=Path.Combine(Directory.GetParent(Path.GetDirectoryName(_md.Path)).FullName,"weapon.nds");
            var defs=LoadFlightWeaponsV442(path);var effect=ResolveWeaponEffectLikeOriginal(_md,type);
            if(defs==null||!defs.TryGetValue(effect.DamageWeaponName??"",out var shot))return false;
            bool canister=shot.Propagation==8;
            if(!canister && ((shot.Propagation!=3&&shot.Propagation!=5)||!shot.HasBallSprite))return false;
            if(canister && !defs.TryGetValue(effect.RootName,out shot))return false;
            var source=_unit;int parentDamage=effect.Damage;
            C2ArtilleryProjectileV442.Spawn(_unit.OwnerMode,_md.Path,defs,shot.Name,
                new Vector3(xs,ys,zs),new Vector3(tx/16f,ty/16f,tz),source,
                (weapon,building,rx,ry,decay)=>{
                    if(source==null)return false;
                    int amount=Math.Max(1,(weapon.FullParent?parentDamage:weapon.Damage)-decay);
                    int savedMode=_attackMode;var savedUnit=_unit;var savedTarget=_targetUnit;var savedBuilding=_targetBuilding;bool savedActive=_active;
                    _unit=source;_attackMode=type;_targetUnit=null;_targetBuilding=null;
                    try
                    {
                        bool hit=C2ArtilleryDamageV444.Apply(source,type,weapon,amount,(int)rx,(int)ry,
                            (b,d)=>ApplyDamageToTargetLikeOriginal(d,0,0,0,b));
                        // Native DestObj receives direct damage in addition to
                        // MakeRoundDamage; nearby buildings use the circle above.
                        if(building!=null){ApplyDamageToTargetLikeOriginal(amount,0,rx,ry,building);hit=true;}
                        return hit;
                    }
                    finally { _attackMode=savedMode;_unit=savedUnit;_targetUnit=savedTarget;_targetBuilding=savedBuilding;_active=savedActive; }
                },0,canister); // muzzle smoke was already emitted by the shot order
            return true;
        }
    }
}
