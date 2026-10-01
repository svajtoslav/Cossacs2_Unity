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
        internal bool FullParent;
        internal string[] Children;
        internal readonly Dictionary<string,C2FlightChildrenV442> Custom = new Dictionary<string,C2FlightChildrenV442>(StringComparer.OrdinalIgnoreCase);
        // Only the single-frame ADS ball sprites. 3D explosion graphs,
        // fragments and detonators are a separate unfinished part of the port.
        internal bool HasBallSprite => Animation == "#NUCL1" || Animation == "#YADROGAUB";
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
        internal Action<C2FlightWeaponV442,Vector3,string> Impact;
        internal Action<string,Vector3> WaterVisual;
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
            if(++_transitions>32 || !next.HasBallSprite){Finished=true;return;}
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
            if(next.Propagation==0 || next.Propagation==1){Vx=Vy=Vz=0;return;}
            int dx=_tx-x,dy=_ty-y;
            int dist=(int)Math.Sqrt((double)dx*dx+(double)dy*dy);
            if(next.Propagation==3)
            {
                int time=dist/Math.Max(1,next.Speed<<1);
                Vx=(dx<<14)/(time+1);Vy=(dy<<14)/(time+1);
                Vz=((_tz-z)<<14)/(time+1)-((Az*(time+2))>>1);
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
            if(aimed&&buildingCollision!=null&&buildingCollision(previous,Position)){Explode(false,"BUILDING");return;}
            int h=_height(X>>14,Y>>14);
            if(Weapon.DamageHeight>0 && h>(Z>>14)-Weapon.DamageHeight)
                Damage?.Invoke(Weapon,Position); // low flight can hit later ranks
            if(h>(Z>>14)){Explode(true,null);return;}
            if((p==0||p==1)&&Weapon.Times>0)
            {
                // #NUCL1 has one frame. SHIPIT: Times=20, HotFrame=254 (end).
                // PUWALL: Times=1, HotFrame=0 (its first and last frame).
                if(_age>=Weapon.Times)Explode(false,null);
            }
        }

        private void Explode(bool landing,string media)
        {
            if(Finished)return;
            Impacts++;Damage?.Invoke(Weapon,Position);
            media=media??(_height(X>>14,Y>>14)<=0?"WATER":_media(X>>14,Y>>14));
            Impact?.Invoke(Weapon,Position,media);
            string[] choices=Weapon.Children;int min=Weapon.MinChildren,max=Weapon.MaxChildren;
            if(media!=null&&Weapon.Custom.TryGetValue(media,out var custom))
            {choices=custom.Names;min=custom.Min;max=custom.Max;}
            if(choices==null||choices.Length==0){Finished=true;return;}
            // Cannon/default and water chains each have one child. Keep the
            // random draw even when MinChild==MaxChild.
            int count=min+(((max-min)*_random())>>15);
            if(count!=1){Finished=true;return;}
            C2FlightWeaponV442 child=null;
            for(int tries=0;tries<20;tries++)
            {
                if(!Weapons.TryGetValue(choices[(_random()*choices.Length)>>15],out child))break;
                if(landing||child.Propagation!=7)break;
            }
            if(child==null||(!landing&&child.Propagation==7)){Finished=true;return;}
            if(media=="WATER")
            {
                WaterVisual?.Invoke(child.Animation,Position);Finished=true;return;
            }
            if(!child.HasBallSprite){Finished=true;return;}
            StartWeapon(child);
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
        internal C2BallFlightV442 FlightV442 => _flight;
        internal static void Spawn(C2BattleTerrainMode map,string md,Dictionary<string,C2FlightWeaponV442> weapons,string weapon,
            Vector3 start,Vector3 target,C2NeutralPeasantUnitInfoV2LikeOriginal source,
            Action<C2FlightWeaponV442,C2SettlementBuildingSelectableV1LikeOriginal,float,float> damage)
        {
            var go=new GameObject("C2_ArtilleryProjectileV442_"+weapon);
            var p=go.AddComponent<C2ArtilleryProjectileV442>();p._map=map;
            p._born=C2CombatCoreV408LikeOriginal.SimulationSecondsV408LikeOriginal;
            p._flight=new C2BallFlightV442(weapons,weapon,start,target,map.C2OriginalFogTerrainHeightV1LikeOriginal,
                map.ArtillerySurfaceMediaV442,()=>C2RetailRandomV407LikeOriginal.Rando(source));
            p._flight.Damage=(w,pos)=>damage(w,p._stepBuilding,pos.x*16,pos.y*16);
            p._flight.Impact=(w,pos,media)=>ImpactedV442++;
            p._flight.WaterVisual=(animation,pos)=>{
                WaterImpactsV442++;var world=map.ArtilleryPointV437(pos.x,pos.y,0);
                C2CombatMuzzleSmokeV390LikeOriginal.SpawnLikeOriginal(world,world,md,animation,0,map,Vector2.right,true);
            };
            go.transform.position=map.ArtilleryPointV437(start.x,start.y,start.z);
            p._visual=C2CombatMuzzleSmokeV390LikeOriginal.SpawnLikeOriginal(go.transform.position,go.transform.position,
                md,weapons[weapon].Animation,0,map,Vector2.right,true,true);
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
                Propagation=d.Propagation,FullParent=d.FullParent,Children=d.Children.ToArray()
            };
            string section="";
            foreach(string line in File.ReadAllLines(path))
            {
                var t=SplitDataTokensLikeOriginal(CleanDataLineLikeOriginal(line));if(t.Length==0)continue;
                if(t[0].StartsWith("[")){section=t[0];continue;}
                if(!result.TryGetValue(t[0],out var w))continue;
                if(section=="[MEMBERS]"&&t.Length>=9)
                {
                    w.Gravity=t[5]=="NO_GRAVITY"?0:t[5]=="LO_GRAVITY"?1:t[5]=="HI_GRAVITY"?2:t[5]=="HI_GRAVITY1"?3:t[5]=="HI_GRAVITY2"?4:5;
                    int.TryParse(t[8],out w.Times);
                    if(t.Length>9)int.TryParse(t[9],out w.ReflectAngle);
                }
                if(section=="[CHILDWEAPON]"&&t.Length>=5)
                {int.TryParse(t[1],out w.MinChildren);int.TryParse(t[2],out w.MaxChildren);int.TryParse(t[3],out w.HotFrame);}
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
            if(defs==null||!defs.TryGetValue(effect.DamageWeaponName??"",out var shot)||
                (shot.Propagation!=3&&shot.Propagation!=5)||!shot.HasBallSprite)return false;
            var source=_unit;int parentDamage=effect.Damage;
            C2ArtilleryProjectileV442.Spawn(_unit.OwnerMode,_md.Path,defs,shot.Name,
                new Vector3(xs,ys,zs),new Vector3(tx/16f,ty/16f,tz),source,
                (weapon,building,rx,ry)=>{
                    if(source==null)return;
                    int savedMode=_attackMode;var savedUnit=_unit;var savedTarget=_targetUnit;var savedBuilding=_targetBuilding;bool savedActive=_active;
                    _unit=source;_attackMode=type;_targetUnit=null;_targetBuilding=null;
                    try { ApplyDamageToTargetLikeOriginal(weapon.FullParent?parentDamage:weapon.Damage,weapon.Radius,rx,ry,building); }
                    finally { _attackMode=savedMode;_unit=savedUnit;_targetUnit=savedTarget;_targetBuilding=savedBuilding;_active=savedActive; }
                });
            return true;
        }
    }
}
