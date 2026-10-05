using System.Globalization;
using System.Xml.Linq;
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private sealed partial class ParsedMap
        {
            internal int[,] StartResourcesV447;
        }
        // SaveNewMap.cpp::Load3DMap initializes each player's stock from MOPT.
        // Missing entries keep CurrentMapOptions.h's 5000 default; zero is valid.
        private static void ParseEconomyMapOptionsV447(XElement options,ParsedMap map)
        {
            map.StartResourcesV447 = ReadStartingResourcesV447(options);
        }
        internal static int[,] ReadStartingResourcesV447(XElement options)
        {
            var result=new int[8,6];
            string[] names={"Wood","Gold","Stone","Food","Iron","Coal"};
            for(int n=0;n<8;n++) {
                var start=options?.Element("Players")?.Element("Player"+n)?.Element("StartRes");
                for(int r=0;r<6;r++) {
                    result[n,r]=-1;
                    if(int.TryParse((string)start?.Element(names[r]),NumberStyles.Integer,CultureInfo.InvariantCulture,out int amount)&&amount>=0)
                        result[n,r]=amount;
                }
            }
            return result;
        }
        private void ApplyMapEconomyV447()
        {
            C2NationResourceEconomyV348LikeOriginal.ApplyMapStartingResourcesV447(_map?.StartResourcesV447);
        }
    }
}
