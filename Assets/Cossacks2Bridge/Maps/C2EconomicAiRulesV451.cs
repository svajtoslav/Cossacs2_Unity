using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // AI_Scripts.h/cpp: the Condition array is OR, each derived predicate is
    // ANDed with its nested array. Empty arrays are true. Never flatten the tree.
    internal sealed class C2EconomicAiScriptV451
    {
        internal XElement Root;
        internal string Source;
        internal readonly Dictionary<string,string> Aliases = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        internal static C2EconomicAiScriptV451 Parse(string text,string source)
        {
            var script=new C2EconomicAiScriptV451 {Root=XElement.Parse(text.Replace("<>","<AI>").Replace("</>","</AI>")),Source=source};
            foreach(var e in script.Root.Descendants("UnitDefinition")) {
                string key=(string)e.Element("ShortName"),id=(string)e.Element("UnitID");
                if(!string.IsNullOrEmpty(key)&&!string.IsNullOrEmpty(id))script.Aliases[key]=id;
            }
            return script;
        }
        internal string Resolve(string id)=>id!=null&&Aliases.TryGetValue(id,out var result)?result:id??"";
        internal static C2EconomicAiScriptV451 LoadOriginal(string nation,int variant=0)
        {
            string root=@"C:\GSC Game World\Cossacks II\Data";
            var router=Parse(File.ReadAllText(Path.Combine(root,"AI/router.xml"),Encoding.GetEncoding(1251)),"router");
            var route=router.Root.Elements("OneAI_route").FirstOrDefault(e=>string.Equals((string)e.Element("Nation"),nation,StringComparison.OrdinalIgnoreCase));
            var paths=route?.Descendants("ScriptName").Select(e=>e.Value).ToArray();
            if(paths==null||paths.Length==0)throw new InvalidDataException("No native AI route for "+nation);
            string path=Path.GetFullPath(Path.Combine(root,paths[Math.Abs(variant%paths.Length)].Replace('\\',Path.DirectorySeparatorChar)));
            if(!path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("AI route outside Data");
            return Parse(File.ReadAllText(path,Encoding.GetEncoding(1251)),path);
        }
    }

    internal sealed partial class C2EconomicAiBrainV451
    {
        internal C2EconomicAiDriverV451 Driver;
        internal C2EconomicAiScriptV451 Script;
        internal int Nation,Difficulty;
        // Explicit option: original scripts contain AddResources cheats, including
        // a OnceInPeriod implementation which ignores its nested difficulty gate.
        // Ordinary fair play leaves the option off; do not silently inject stock.
        internal bool AllowScriptBonuses;
        internal readonly int[] DesiredWorkers={1,0,1,1,0,0};
        internal readonly int[] SettlementPriority={100,100,100,100,100,100};
        internal readonly Dictionary<string,int> Ready=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string,int> Alive=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        internal readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> Workers=new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        internal readonly List<C2SettlementBuildingSelectableV1LikeOriginal> Buildings=new List<C2SettlementBuildingSelectableV1LikeOriginal>();
        internal Vector2 Centre;
        internal int Builds,Orders,Trades,Researches,Assignments;
        private readonly Dictionary<XElement,float> periods=new Dictionary<XElement,float>();
        private readonly HashSet<string> warnings=new HashSet<string>();
        private uint random;
        internal static int Int(XElement e,string field,int fallback=0)=>int.TryParse((string)e?.Element(field),out int n)?n:fallback;
        internal static bool Bool(XElement e,string field)=>string.Equals((string)e?.Element(field),"true",StringComparison.OrdinalIgnoreCase);
        internal static int Resource(XElement e,string field)=>C2ResourceStorageV445.ResourceId((string)e?.Element(field)??"");
        internal static bool Compare(int a,int b,string op)
        {
            switch(op) {case "more":return a>b;case "less":return a<b;case "more equal":return a>=b;
                case "less equal":return a<=b;case "equal":return a==b;case "not equal":return a!=b;default:return false;}
        }
        internal static int DifficultyNumber(string value)
        {switch(value){case "Easy":return 0;case "Normal":return 1;case "Hard":return 2;case "VeryHard":return 3;default:return -1;}}
        private void Warn(string key){if(warnings.Add(key))Debug.LogWarning("[C2:AI V451] owner="+Nation+" "+key+" source="+Script.Source);}
        internal int Count(Dictionary<string,int> counts,string id)=>counts.TryGetValue(Script.Resolve(id),out int n)?n:0;
        private void Add(Dictionary<string,int> counts,string id){if(string.IsNullOrEmpty(id))return;counts[id]=Count(counts,id)+1;}
        internal void Snapshot()
        {
            Ready.Clear();Alive.Clear();Workers.Clear();Buildings.Clear();Vector2 centre=Vector2.zero;int anchors=0;
            foreach(var u in Driver.Units) {
                if(u==null||u.OwnerMode!=Driver.Map||u.Nation!=Nation||u.IsDeadLikeOriginal)continue;
                string id=C2OriginalProduceCatalogV13.ResolveMemberIdForSelectedUnit(u);Add(Alive,id);Add(Ready,id);
                if(!u.SettlementAiControlledLikeOriginal&&C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u).Peasant)Workers.Add(u);
            }
            foreach(var b in Driver.Buildings) {
                if(b==null||b.OwnerMode!=Driver.Map||b.Nation!=Nation||b.NotSelectable||b.ReadyLikeOriginal&&b.LifeLikeOriginal<=0)continue;
                Buildings.Add(b);string id=C2OriginalProduceCatalogV13.ResolveMemberIdForSelectedBuilding(b);Add(Alive,id);
                if(C2ResourceStorageV445.Ready(b))Add(Ready,id);
                // Town centres anchor the city. Fallback to buildings, then workers.
                if(id.StartsWith("BldCen(",StringComparison.OrdinalIgnoreCase)){centre+=new Vector2(b.RealX/16f,b.RealY/16f);anchors++;}
            }
            if(anchors==0)foreach(var b in Buildings){centre+=new Vector2(b.RealX/16f,b.RealY/16f);anchors++;}
            if(anchors==0)foreach(var u in Workers){centre+=new Vector2(u.RealX/16f,u.RealY/16f);anchors++;}
            if(anchors>0)Centre=centre/anchors;
        }
        internal bool AnyCondition(XElement list,float seconds)
        {
            if(list==null)return true;
            bool any=false,passed=false;
            // C++ uses |=, not ||: later timers must advance even if an earlier
            // sibling already passed. This matters for stateful OnceInPeriod.
            foreach(var e in list.Elements()) {if(e.Name.LocalName=="Description")continue;any=true;passed|=Condition(e,seconds);}
            return passed||!any;
        }
        internal bool Condition(XElement e,float seconds)
        {
            string kind=e.Name.LocalName,op=(string)e.Element("OpType");int value=Int(e,"Value");bool pass;
            switch(kind) {
                case "Condition":case "FolderCondition":return AnyCondition(e.Element("Condition")??e,seconds);
                case "AmountOfReadyUnits":pass=Compare(Count(Ready,(string)e.Element("UnitType")),value,op);break;
                case "UnitsProduced":pass=Compare(Driver.Statistics.Produced(Nation,Script.Resolve((string)e.Element("UnitType"))),value,op);break;
                case "UnitsLost":pass=Compare(Driver.Statistics.Lost(Nation,Script.Resolve((string)e.Element("UnitType"))),value,op);break;
                case "CurResources":int r=Resource(e,"ResType");pass=r>=0&&Compare(Stock(r),value,op);break;
                case "FreeLivePlaces":pass=Compare(C2NationCityRuntimeV384ALikeOriginal.GetMaxUnitsLikeOriginal(Nation)-C2NationCityRuntimeV384ALikeOriginal.GetCurrentUnitsLikeOriginal(Nation),value,op);break;
                case "TotalLivePlaces":pass=Compare(C2NationCityRuntimeV384ALikeOriginal.GetMaxUnitsLikeOriginal(Nation),value,op);break;
                case "LivePlacesUsed":pass=Compare(C2NationCityRuntimeV384ALikeOriginal.GetCurrentUnitsLikeOriginal(Nation),value,op);break;
                case "GamingTime":pass=Compare((int)seconds,Int(e,"Time"),op);break;
                case "Difficulty":int d=DifficultyNumber((string)e.Element("DifficultyValue"));pass=d>=0&&Compare(Difficulty,d,op);break;
                case "UpgradeIsRun":pass=C2UpgradeEconomyV445.IsRunV451(Nation,(string)e.Element("UpgradeID"));break;
                case "AmountOfSettlements":
                    int resource=Resource(e,"ResType"),level=0;
                    if(Bool(e,"CheckUpgrades")&&!int.TryParse(((string)e.Element("Level")??"").Replace("Level",""),out level))return false;
                    int settlements=0;
                    foreach(var v in Driver.Villages)if(v!=null&&v.MapV451==Driver.Map&&v.OwnerV445==Nation&&v.ResourceV445==resource&&v.WorkerLimitV445>0&&
                        (!Bool(e,"CheckUpgrades")||v.UpgradesV445.Count(u=>u.Running||u.Done)>=level))settlements++;
                    pass=resource>=0&&Compare(settlements,value,op);break;
                case "OnceInPeriod":
                    // Source uses 24*256 ticks here, versus 25*256 in GamingTime,
                    // and returns directly, WITHOUT evaluating its nested array.
                    float period=Mathf.Max(.04f,Int(e,"Period",10)*24f/25f);
                    periods.TryGetValue(e,out float start);if(seconds-start<=period)return false;
                    periods[e]=seconds-start>period*2?seconds:start+period;return true;
                case "EnemyNearBase":int enemies=EnemyCount(e);pass=enemies>=Int(e,"MinAmount",10)&&enemies<=Int(e,"MaxAmount",10000);break;
                default:Warn("unsupported condition: "+kind);return false;
            }
            return pass&&AnyCondition(e.Element("Condition"),seconds);
        }
        private int EnemyCount(XElement e)
        {
            int count=0;float radius=Int(e,"Radius",3000);radius*=radius;
            foreach(var u in Driver.Units) {
                if(u==null||u.OwnerMode!=Driver.Map||u.IsDeadLikeOriginal||(C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(u)&(1<<Nation))!=0)continue;
                if((new Vector2(u.RealX/16f,u.RealY/16f)-Centre).sqrMagnitude>=radius)continue;
                var md=C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(u);
                // Native EnemyNearBase also includes peasants in its infantry branch.
                if(Bool(e,md.Artilery?"Artillery":u.MotionDist>48?"Cavalery":"Infantry"))count++;
            }
            foreach(var b in Driver.Buildings)if(b!=null&&b.OwnerMode==Driver.Map&&b.Nation!=Nation&&C2ResourceStorageV445.Ready(b)&&
                (new Vector2(b.RealX/16f,b.RealY/16f)-Centre).sqrMagnitude<radius) {
                var md=C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(b);
                if(Bool(e,md.Damage0>0?"Towers":"Buildings"))count++;
            }
            return count;
        }
        internal int Stock(int resource)=>C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(Nation,resource);
        internal bool WithinBudget(int[] price,int percent)
        {
            if(price==null||percent<0)return false;
            for(int r=0;r<Math.Min(6,price.Length);r++)if(price[r]>0&&(long)price[r]*100>(long)Stock(r)*percent)return false;
            return true;
        }
        internal bool Chance(int percent)
        {
            if(percent<=0)return false;if(percent>=100)return true;
            if(random==0)random=(uint)(Nation+1)*7919;
            random=unchecked(random*1664525+1013904223);return random%100<percent;
        }
        internal C2MilitaryAiV452 MilitaryV452;
        internal void Tick(float seconds)
        {
            Snapshot();
            foreach(var folder in Script.Root.Element("Folders")?.Elements("AI_Folder")??Enumerable.Empty<XElement>()) {
                if(!Condition(folder.Element("FolderCondition")??new XElement("FolderCondition"),seconds))continue;
                foreach(var rule in folder.Element("Rules")?.Elements("AI_Trigger")??Enumerable.Empty<XElement>())
                    if(Condition(rule.Element("Condition")??new XElement("Condition"),seconds))
                        foreach(var action in rule.Element("Action")?.Elements()??Enumerable.Empty<XElement>())Execute(action);
            }
            DistributeWorkers();
            MilitaryV452?.Tick();
        }
        internal void Execute(XElement action)
        {
            int r;
            switch(action.Name.LocalName) {
                case "Build":PlanBuilding(action);break;
                case "Produce":Produce(action);break;
                case "PeonsDistribution":DesiredWorkers[0]=Math.Max(0,Int(action,"OnWood",10));DesiredWorkers[2]=Math.Max(0,Int(action,"OnStone",10));DesiredWorkers[3]=Math.Max(0,Int(action,"OnFood"));break;
                case "PerformUpgrade":
                    var upgrade=C2UpgradeEconomyV445.Get((string)action.Element("UpgradeID"));
                    if(upgrade!=null&&WithinBudget(upgrade.Price,Int(action,"CostPercent",100)))
                        foreach(var b in Buildings)if(C2UpgradeEconomyV445.StartForNationV449(b,upgrade,Nation)){Researches++;break;}
                    break;
                case "BuyResource":
                    foreach(var b in Buildings)if(C2MarketEconomyV445.Exchange(b,Nation,Resource(action,"ResTypeToSell"),Resource(action,"ResTypeToBuy"),Int(action,"Amount"))){Trades++;break;}
                    break;
                case "UpgradeSettlements":
                    r=Resource(action,"ResType");string level=(string)action.Element("UpgLevel");
                    if(!int.TryParse((level??"").Replace("Level",""),out int number)||number<1)break;
                    // vdf_RunUpgInSettlement tries every earlier level as well,
                    // and can start one upgrade in EACH qualifying settlement.
                    foreach(var village in Driver.Villages)if(village!=null&&village.MapV451==Driver.Map&&village.OwnerV445==Nation&&village.ResourceV445==r&&
                        (village.CentreV451-Centre).sqrMagnitude<=Math.Pow(Int(action,"MaxDistance",1000000),2))
                        for(int levelIndex=0;levelIndex<Math.Min(number,village.UpgradesV445.Count);levelIndex++)
                            if(WithinBudget(village.UpgradesV445[levelIndex].Price,Int(action,"CostPercent",100))&&village.BuyUpgradeForNationV451(levelIndex,Nation)) {Researches++;break;}
                    break;
                case "SettlementsPriory":r=Resource(action,"ResType");if(r>=0)SettlementPriority[r]=Int(action,"Prio",100);break;
                case "AddResources":
                    r=Resource(action,"ResType");if(AllowScriptBonuses&&r>=0)C2NationResourceEconomyV348LikeOriginal.AddResourceLikeOriginal(Nation,r,Int(action,"Amount"),"native_AI_script_bonus");
                    else Warn("script resource bonuses disabled for fair play");break;
                default:Warn("unsupported action: "+action.Name.LocalName);break;
            }
        }
    }
}
