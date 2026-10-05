using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2MarketEconomyV445
    {
        private static readonly double[,] Buy = new double[6,3], Sell = new double[6,3];
        private static readonly double[] CurrentBuy = new double[6], CurrentSell = new double[6], BuyChange = new double[6], SellChange = new double[6];
        private static bool loaded;
        internal static void Reset() { loaded = false; }
        internal static bool IsMarket(C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            if (!C2ResourceStorageV445.Ready(b)) return false;
            string path = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(b).Path;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            foreach (var line in File.ReadLines(path)) if (line.Trim().Equals("TORG",StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            Array.Clear(CurrentBuy,0,6); Array.Clear(CurrentSell,0,6);
            string path = @"C:\GSC Game World\Cossacks II\Data\economy.txt";
            if (!File.Exists(path)) return;
            int resource = -1;
            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("/")) continue;
                var p = line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 1) { resource = C2ResourceStorageV445.ResourceId(p[0]); continue; }
                if (resource < 0 || p.Length < 7 || (p[0] != "BUY" && p[0] != "SELL")) continue;
                var values = p[0] == "BUY" ? Buy : Sell;
                for (int i=0;i<3;i++) values[resource,i] = double.Parse(p[i+1],CultureInfo.InvariantCulture);
                double change = double.Parse(p[6],CultureInfo.InvariantCulture) / (double.Parse(p[5],CultureInfo.InvariantCulture)*100);
                if (p[0] == "BUY") { CurrentBuy[resource]=values[resource,1]; BuyChange[resource]=change; }
                else { CurrentSell[resource]=values[resource,1]; SellChange[resource]=change; }
            }
        }
        internal static int Quote(int nation,int sell,int buy,int requested,out int amount)
        {
            Load(); amount=0;
            if (nation<0 || nation>=7 || sell<0 || buy<0 || sell>=6 || buy>=6 || sell==buy || requested<=0 || CurrentBuy[buy]<=0) return 0;
            amount=Math.Min(requested,C2NationResourceEconomyV348LikeOriginal.GetResourceLikeOriginal(nation,sell));
            return (int)Math.Min(int.MaxValue,amount*CurrentSell[sell]/CurrentBuy[buy]);
        }
        internal static bool Exchange(C2SettlementBuildingSelectableV1LikeOriginal market,int nation,int sell,int buy,int requested)
        {
            if (market==null || market.Nation!=nation || !IsMarket(market)) return false;
            int received=Quote(nation,sell,buy,requested,out int sold);
            if (received<=0 || !C2NationResourceEconomyV348LikeOriginal.TryExchangeV445(nation,sell,sold,buy,received)) return false;
            // cvi_Market::PerformOperation updates BOTH prices for each traded resource.
            double v=received*BuyChange[buy];
            CurrentBuy[buy]=(CurrentBuy[buy]+Buy[buy,2]*v)/(1+v);
            CurrentSell[buy]=(CurrentSell[buy]+Sell[buy,2]*v)/(1+v);
            v=sold*SellChange[sell];
            CurrentBuy[sell]=(CurrentBuy[sell]+Buy[sell,0]*v)/(1+v);
            CurrentSell[sell]=(CurrentSell[sell]+Sell[sell,0]*v)/(1+v);
            return true;
        }
    }
}
