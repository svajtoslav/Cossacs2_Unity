using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2UpgradeResearchV445 : MonoBehaviour
    {
        internal C2SettlementBuildingSelectableV1LikeOriginal Building;
        internal C2UpgradeEconomyV445.Definition Research;
        internal int Nation;
        internal float Started;
        internal int Level=2, PersonalDelay=100;
        internal float Progress => Research==null?0:Mathf.Clamp01((Time.time-Started)/Research.Seconds);
        private void Update()
        {
            if(Research==null)return;
            if(!C2ResourceStorageV445.Ready(Building)||Building.Nation!=Nation)
            { C2UpgradeEconomyV445.CancelRunning(Nation,Research);Research=null;return; }
            if(Progress<1)return;
            var done=Research;Research=null;C2UpgradeEconomyV445.Complete(this,done);
            C2GameplayHudV1.ForceRefreshLikeOriginal();
        }
        private void OnDestroy(){if(Research!=null)C2UpgradeEconomyV445.CancelRunning(Nation,Research);}
    }
    internal static class C2UpgradeEconomyV445
    {
        internal sealed class Definition
        {
            internal string Id,Effect="",File,Hint;
            internal int Level=1;
            internal int[] Price=new int[8];
            internal float Seconds;
            internal bool Enabled,Private;
            internal bool Supported => Effect.StartsWith("ENABLE UNITS ")||Effect.StartsWith("EFFECT_WOOD ")||Effect.StartsWith("EFFECT_STONE ")||
                Effect.StartsWith("COST ")||Effect.StartsWith("BUILD ")||Effect.StartsWith("DAMAGE ")||Effect.StartsWith("SHIELD ")||
                Effect.StartsWith("LIFE ")||Effect.StartsWith("HEALING ")||Effect.StartsWith("ATTRANGE ")||Effect.StartsWith("RAZBROS ");
        }
        private static readonly Dictionary<string,Definition> Defs=new Dictionary<string,Definition>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string,string[]> Requirements=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Members=new HashSet<string>(StringComparer.OrdinalIgnoreCase),Enabled=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Done=new HashSet<string>(StringComparer.OrdinalIgnoreCase),Running=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<KeyValuePair<int,Definition>> Applied=new List<KeyValuePair<int,Definition>>();
        private static readonly Dictionary<string,string[]> Auto = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string,string[]> Places = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;
        internal static int Revision {get;private set;}
        internal static void Reset(){Done.Clear();Running.Clear();Applied.Clear();Revision++;}
        private static string Key(int nation,string id)=>nation+":"+id;
        internal static bool IsRunV451(int nation,string id)=>id!=null&&(Done.Contains(Key(nation,id))||Running.Contains(Key(nation,id)));
        internal static Definition Get(string id){Load();Defs.TryGetValue(id,out var d);return d;}
        private static void Load()
        {
            if(loaded)return;loaded=true;
            string root=@"C:\GSC Game World\Cossacks II\Data";
            foreach(string name in new[]{"Austria.NDS","France.NDS","England.NDS","Russia.nds","Prussia.nds","Poland.NDS","Spain.NDS","Rein.NDS","Egipet.nds"})
            {
                string path=Path.Combine(root,name);if(!File.Exists(path))continue;
                string section="",reqId=null;int reqLeft=0;var req=new List<string>();Definition current=null;
                foreach(string raw in File.ReadLines(path))
                {
                    string line=C2OriginalProduceCatalogV13.CleanLineForSiblingLoadersLikeOriginal(raw);
                    if(line.Length==0)continue;
                    if(line[0]=='['){section=line;current=null;reqId=null;reqLeft=0;continue;}
                    var p=line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                    if(section=="[MEMBERS]")Members.Add(p[0]);
                    else if(section=="[ENABLED]")foreach(var id in p)Enabled.Add(id);
                    else if(section=="[UNITLOCK]" && p.Length>=3 && int.TryParse(p[1],out int maximum))
                        C2ProductionLimitsV450.RegisterLock(p[0],p[2],maximum);
                    else if(section=="[UPGRADE]")
                    {
                        if(line.Contains("#COST"))
                        {
                            current=new Definition{Id=p[0],Hint=p[1],File=name,Seconds=1};Defs[current.Id]=current;
                            for(int i=2;i<p.Length;i++)
                            {
                                if(p[i]=="#LEVEL"&&i+1<p.Length)int.TryParse(p[++i],out current.Level);
                                else if(p[i]=="#TIME"&&i+1<p.Length)current.Seconds=Mathf.Max(.04f,int.Parse(p[++i])/25f);
                                else if(p[i]=="#COST"&&i+1<p.Length)
                                {
                                    int count=int.Parse(p[++i]);
                                    for(int n=0;n<count&&i+2<p.Length;n++){int resource=C2ResourceStorageV445.ResourceId(p[++i]);int amount=int.Parse(p[++i]);if(resource>=0)current.Price[resource]=Mathf.Max(0,amount);}
                                }
                            }
                        }
                        else if(current!=null)current.Effect=string.Join(" ",p);
                    }
                    else if(section=="[UPGRADEENABLE]"){foreach(var id in p)if(Defs.TryGetValue(id,out var d))d.Enabled=true;}
                    else if(section=="[PRIVATE]"){foreach(var id in p)if(Defs.TryGetValue(id,out var d))d.Private=true;}
                    else if(section=="[ACCESSCONTROL]"||section=="[AUTOUPGRADE]"||section=="[UPGRADEPLACE]")
                    {
                        int start=0;
                        if(reqLeft==0){if(p.Length<2||!int.TryParse(p[1],out reqLeft))continue;reqId=p[0];req.Clear();start=2;}
                        for(int i=start;i<p.Length&&reqLeft>0;i++,reqLeft--)req.Add(p[i]);
                        if(reqLeft==0&&reqId!=null) {
                            var target=section=="[AUTOUPGRADE]"?Auto:section=="[UPGRADEPLACE]"?Places:Requirements;
                            target[reqId]=req.ToArray();
                        }
                    }
                }
            }
        }
        internal static bool CanProduce(C2BattleTerrainMode map,int nation,string id)
        {
            Load();if(string.IsNullOrEmpty(id))return false;
            bool enabled=!Members.Contains(id)||Enabled.Contains(id);
            foreach(var pair in Applied)if(pair.Key==nation&&pair.Value.Effect.StartsWith("ENABLE UNITS ")&&Targets(pair.Value.Effect).Contains(id,StringComparer.OrdinalIgnoreCase))enabled=true;
            if(!enabled)return false;
            return RequirementsMet(map,nation,id) && C2ProductionLimitsV450.Allows(nation,id,out _);
        }
        private static bool RequirementsMet(C2BattleTerrainMode map,int nation,string id)
        {
            if(!Requirements.TryGetValue(id,out var needed))return true;
            var buildings=UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsSortMode.None);
            foreach(string requirement in needed)
            {
                if(Defs.ContainsKey(requirement)){if(!Done.Contains(Key(nation,requirement)))return false;continue;}
                bool found=false;
                foreach(var b in buildings)if(b.OwnerMode==map&&b.Nation==nation&&C2ResourceStorageV445.Ready(b)&&string.Equals(b.SourceMonsterId,requirement,StringComparison.OrdinalIgnoreCase)){found=true;break;}
                if(!found)return false;
            }
            return true;
        }
        internal static bool Visible(C2SettlementBuildingSelectableV1LikeOriginal b,Definition d)
        {
            if(b==null||d==null||!d.Enabled)return false;
            if(!Places.TryGetValue(b.SourceMonsterId??"",out var offered)||!offered.Contains(d.Id,StringComparer.OrdinalIgnoreCase))return false;
            if(d.Private){var r=b.GetComponent<C2UpgradeResearchV445>();return (r!=null?r.Level:2)==d.Level;}
            return !Done.Contains(Key(b.Nation,d.Id));
        }
        internal static bool CanStart(C2SettlementBuildingSelectableV1LikeOriginal b,Definition d)
        {
            return b!=null && C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(b.Nation) && CanStartForNationV449(b,d,b.Nation);
        }
        internal static bool CanStartForNationV449(C2SettlementBuildingSelectableV1LikeOriginal b,Definition d,int issuingNation)
        {
            if(!C2ResourceStorageV445.Ready(b)||b.Nation!=issuingNation||!Visible(b,d)||!d.Supported)return false;
            if(!RequirementsMet(b.OwnerMode,b.Nation,d.Id))return false;
            var runtime=b.GetComponent<C2UpgradeResearchV445>();
            if(runtime!=null&&runtime.Research!=null||!d.Private&&Running.Contains(Key(b.Nation,d.Id)))return false;
            return C2NationResourceEconomyV348LikeOriginal.CanAffordLikeOriginal(b.Nation,d.Price,out _);
        }
        internal static bool Start(C2SettlementBuildingSelectableV1LikeOriginal b,Definition d)
        {
            if(!CanStart(b,d))return false;
            return StartForNationV449(b,d,b.Nation);
        }
        internal static bool StartForNationV449(C2SettlementBuildingSelectableV1LikeOriginal b,Definition d,int issuingNation)
        {
            if(!CanStartForNationV449(b,d,issuingNation))return false;
            if(!C2NationResourceEconomyV348LikeOriginal.ApplyCostLikeOriginal(b.Nation,d.Price,"PerformUpgrade:"+d.Id,out _))return false;
            var runtime=b.GetComponent<C2UpgradeResearchV445>()??b.gameObject.AddComponent<C2UpgradeResearchV445>();
            runtime.Building=b;runtime.Nation=b.Nation;runtime.Research=d;runtime.Started=Time.time;
            if(!d.Private)Running.Add(Key(b.Nation,d.Id));return true;
        }
        internal static void CancelRunning(int nation,Definition d){if(!d.Private)Running.Remove(Key(nation,d.Id));}
        internal static void Complete(C2UpgradeResearchV445 runtime,Definition d)
        {
            CancelRunning(runtime.Nation,d);
            if(d.Private){runtime.Level++;runtime.PersonalDelay=Value(runtime.PersonalDelay,d.Effect.Split(' ').Last());}
            else ApplyCompleted(runtime.Nation,d);
            Revision++;
        }
        private static void ApplyCompleted(int nation,Definition d)
        {
            // NewUpgrade.cpp::PerformNewUpgrade recursively performs AutoPerform
            // effects. These are part of the paid research, not separate purchases.
            if(!Done.Add(Key(nation,d.Id)))return;
            Applied.Add(new KeyValuePair<int,Definition>(nation,d));
            ApplyLiveObjectsV449(nation,d);
            if(Auto.TryGetValue(d.Id,out var children))foreach(string id in children)
                if(Defs.TryGetValue(id,out var child))ApplyCompleted(nation,child);
        }
        internal static int[] UnitPrice(int nation,string id,int[] original)
        {
            int[] price=(int[])original.Clone();
            foreach(var pair in Applied) {
                var p=pair.Value.Effect.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(pair.Key!=nation||p.Length<7||p[0]!="COST"||!Targets(pair.Value.Effect).Contains(id,StringComparer.OrdinalIgnoreCase))continue;
                int first=Array.IndexOf(p,"GRP");int group=Array.IndexOf(p,"GRP",first+1);
                if(group<0||group+1>=p.Length||!int.TryParse(p[group+1],out int n))continue;
                int start=group+2;
                // COST supports a common value after the resource group and
                // distinct resource/value pairs (UPG32(EG)).
                bool pairs=start+1<p.Length && C2ResourceStorageV445.ResourceId(p[start+1])<0 && n>1;
                for(int i=0;i<n;i++) {
                    int at=start+i*(pairs?2:1);if(at>=p.Length-1)break;
                    int resource=C2ResourceStorageV445.ResourceId(p[at]);
                    string value=pairs&&at+1<p.Length?p[at+1]:p[p.Length-1];
                    if(resource>=0&&resource<price.Length)price[resource]=Value(price[resource],value);
                }
            }
            return price;
        }
        internal static int Value(int before,string token)
        {
            bool percent=token.EndsWith("%");string number=percent?token.Substring(0,token.Length-1):token;
            if(!int.TryParse(number,out int n))return before;
            int value=percent?(int)((long)before*n/100):n;
            return Mathf.Max(0,number.StartsWith("+")||number.StartsWith("-")?before+value:value);
        }
        internal static int Gathered(int nation,int resource,int amount)
        {
            int multiplier=100;string effect=resource==0?"EFFECT_WOOD ":resource==2?"EFFECT_STONE ":null;
            if(effect!=null)foreach(var pair in Applied)if(pair.Key==nation&&pair.Value.Effect.StartsWith(effect))multiplier=Value(multiplier,pair.Value.Effect.Split(' ').Last());
            return (int)Math.Min(int.MaxValue,(long)amount*multiplier/100);
        }
        internal static int ProduceStages(int nation,string id,int stages)
        {
            foreach(var pair in Applied)if(pair.Key==nation&&pair.Value.Effect.StartsWith("BUILD ")&&Targets(pair.Value.Effect).Contains(id,StringComparer.OrdinalIgnoreCase))stages=Value(stages,pair.Value.Effect.Split(' ').Last());
            return Mathf.Max(1,stages);
        }
        private static string[] Targets(string effect)
        {
            var p=effect.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);int at=Array.IndexOf(p,"GRP");
            if(at>=0&&at+1<p.Length&&int.TryParse(p[at+1],out int n))return p.Skip(at+2).Take(n).ToArray();
            if(p[0]=="ENABLE")return p.Skip(2).ToArray();
            return p.Length>=3?new[]{p[p.Length-2]}:Array.Empty<string>();
        }
        internal static int LifeV449(int nation,string id,int original)
        {
            int life=original;
            foreach(var pair in Applied)if(pair.Key==nation&&pair.Value.Effect.StartsWith("LIFE ")&&Targets(pair.Value.Effect).Contains(id,StringComparer.OrdinalIgnoreCase))
                life=Math.Min(65535,Value(life,pair.Value.Effect.Split(' ').Last()));
            return Mathf.Max(1,life);
        }
        private static void ApplyLiveObjectsV449(int nation,Definition d)
        {
            bool life=d.Effect.StartsWith("LIFE "), build=d.Effect.StartsWith("BUILD "), heal=d.Effect.StartsWith("HEALING ");
            if(!life&&!build&&!heal)return;
            var targets=Targets(d.Effect);
            foreach(var u in C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal())
            {
                if(u==null||u.Nation!=nation||u.IsDeadLikeOriginal||u.LifeLikeOriginal<=0||!targets.Contains(u.SourceMonsterId,StringComparer.OrdinalIgnoreCase))continue;
                if(life) {
                    int old=Mathf.Max(1,u.MaxLifeLikeOriginal), next=Math.Min(65535,Value(old,d.Effect.Split(' ').Last()));
                    u.LifeLikeOriginal=(int)Math.Min(next,(long)u.LifeLikeOriginal*next/old);u.MaxLifeLikeOriginal=next;
                }
                // HealUnits is a one-time +10% to existing units, not regeneration.
                if(heal)u.LifeLikeOriginal=Math.Min(u.MaxLifeLikeOriginal,u.LifeLikeOriginal+u.MaxLifeLikeOriginal/10);
            }
            foreach(var b in UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(FindObjectsSortMode.None))
            {
                if(b.Nation!=nation||b.NotSelectable||(b.LifeLikeOriginal<=0&&b.ReadyLikeOriginal)||!targets.Contains(b.SourceMonsterId,StringComparer.OrdinalIgnoreCase))continue;
                var site=b.GetComponent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();
                if(site!=null){site.RefreshUpgradesV449();continue;}
                if(life){int old=Mathf.Max(1,b.LifeMaxLikeOriginal), next=Math.Min(65535,Value(old,d.Effect.Split(' ').Last()));
                    b.LifeLikeOriginal=(int)Math.Min(next,(long)b.LifeLikeOriginal*next/old);b.LifeMaxLikeOriginal=next;
                    var proxy=b.GetComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();if(proxy!=null){proxy.Life=b.LifeLikeOriginal;proxy.LifeMax=next;}
                }
            }
        }
        private static readonly Dictionary<(int Nation,string Id,C2CombatCoreV408LikeOriginal.MdTraits Base),C2CombatCoreV408LikeOriginal.MdTraits> CombatCacheV449 =
            new Dictionary<(int,string,C2CombatCoreV408LikeOriginal.MdTraits),C2CombatCoreV408LikeOriginal.MdTraits>();
        private static int combatRevisionV449=-1;
        internal static C2CombatCoreV408LikeOriginal.MdTraits CombatTraitsV449(int nation,string id,C2CombatCoreV408LikeOriginal.MdTraits original)
        {
            if(combatRevisionV449!=Revision){CombatCacheV449.Clear();combatRevisionV449=Revision;}
            var key=(nation,id??"",original);if(CombatCacheV449.TryGetValue(key,out var found))return found;
            C2CombatCoreV408LikeOriginal.MdTraits value=original;
            foreach(var pair in Applied)
            {
                string e=pair.Value.Effect;
                if(pair.Key!=nation||(!e.StartsWith("ATTRANGE ")&&!e.StartsWith("RAZBROS "))||!Targets(e).Contains(id,StringComparer.OrdinalIgnoreCase))continue;
                if(ReferenceEquals(value,original))value=original.CopyForResearchV449();
                var p=e.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(p[0]=="RAZBROS")value.Razbros=Value(value.Razbros,p[p.Length-1]);
                else if(p.Length>3&&p[1]=="ATTINDEX"&&int.TryParse(p[2],out int slot)&&(uint)slot<value.AttackRadius2.Length)
                {
                    string amount=p[p.Length-1];
                    value.AttackRadius2[slot]=Value(value.AttackRadius2[slot],amount);
                    value.DetRadius2[slot]=Value(value.DetRadius2[slot],amount);
                    // UpgradeAttRange also moves the next attack state's lower boundary.
                    if(slot+1<value.AttackRadius1.Length){value.AttackRadius1[slot+1]=Value(value.AttackRadius1[slot+1],amount);
                        value.DetRadius1[slot+1]=Value(value.DetRadius1[slot+1],amount);}
                }
            }
            CombatCacheV449[key]=value;return value;
        }
        internal static int Damage(int nation,string id,string kind,int damage)
        {
            foreach(var pair in Applied){string e=pair.Value.Effect;if(pair.Key==nation&&e.StartsWith("DAMAGE WEAPONKIND ")&&e.Split(' ')[2]==kind&&Targets(e).Contains(id,StringComparer.OrdinalIgnoreCase))damage=Value(damage,e.Split(' ').Last());}
            return damage;
        }
        internal static int Range(int nation,string id,int slot,int range)
        {
            foreach(var pair in Applied)
            {
                string e=pair.Value.Effect;
                var p=e.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(pair.Key==nation&&p.Length>3&&p[0]=="ATTRANGE"&&p[1]=="ATTINDEX"&&p[2]==slot.ToString()&&Targets(e).Contains(id,StringComparer.OrdinalIgnoreCase))range=Value(range,p[p.Length-1]);
            }
            return range;
        }
        internal static int Shield(int nation,string id)
        {
            int shield=0;
            foreach(var pair in Applied){string e=pair.Value.Effect;if(pair.Key==nation&&e.StartsWith("SHIELD ")&&Targets(e).Contains(id,StringComparer.OrdinalIgnoreCase))shield=Value(shield,e.Split(' ').Last());}
            return shield;
        }
        internal static string CostText(int[] price)
        {
            string[] names={"дерево","золото","камень","еда","железо","уголь"};var parts=new List<string>();
            for(int i=0;i<6;i++)if(price[i]>0)parts.Add(names[i]+" "+price[i]);return string.Join(", ",parts);
        }
    }
}
