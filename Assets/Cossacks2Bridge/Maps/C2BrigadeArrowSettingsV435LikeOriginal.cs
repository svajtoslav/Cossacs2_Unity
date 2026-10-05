using System;
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2BrigadeArrowSettingsV435LikeOriginal
    {
        internal float StartWidth=80, FinalWidth=32;
        internal Color StartColor=new Color32(0,0,255,64), FinalColor=new Color32(255,0,0,128);
    }
    public sealed partial class C2BattleTerrainMode
    {
        private C2BrigadeArrowSettingsV435LikeOriginal _arrowSettingsV435;
        internal C2BrigadeArrowSettingsV435LikeOriginal GetBrigadeArrowSettingsV435LikeOriginal()
        {
            if(_arrowSettingsV435!=null)return _arrowSettingsV435;
            var value=new C2BrigadeArrowSettingsV435LikeOriginal();
            var fs=_bootstrap!=null?_bootstrap.Fs:null;
            if(fs==null)return value;
            _arrowSettingsV435=value;
            string path=fs.Exists("EngineSettings.xml")?"EngineSettings.xml":"enginesettings.xml";
            if(!fs.Exists(path))return value;
            try
            {
                // ClassEngine XML uses an anonymous <> root, which is not XML
                // accepted by System.Xml. Read the named settings subtree.
                string source=fs.ReadAllText(path);
                const string open="<BrigadesArrowParam>",close="</BrigadesArrowParam>";
                int begin=source.IndexOf(open,StringComparison.Ordinal),end=source.IndexOf(close,StringComparison.Ordinal);
                if(begin<0||end<begin)return value;
                var node=XElement.Parse(source.Substring(begin,end+close.Length-begin));
                float parsed;
                if(float.TryParse((string)node.Element("StartArrowWidth"),NumberStyles.Float,CultureInfo.InvariantCulture,out parsed))value.StartWidth=parsed;
                if(float.TryParse((string)node.Element("FinalArrowWidth"),NumberStyles.Float,CultureInfo.InvariantCulture,out parsed))value.FinalWidth=parsed;
                value.StartColor=ParseArrowArgbV435((string)node.Element("StartArrowColor"),value.StartColor);
                value.FinalColor=ParseArrowArgbV435((string)node.Element("FinalArrowColor"),value.FinalColor);
                Debug.Log("[C2 ARROW V435] source="+path+" widths="+value.StartWidth+","+value.FinalWidth);
            }
            catch(Exception e){Debug.LogWarning("[C2 ARROW V435] Cannot read arrow settings: "+e.Message);}
            return value;
        }
        private static Color ParseArrowArgbV435(string text,Color fallback)
        {
            uint argb;
            if(!uint.TryParse(text,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out argb))return fallback;
            return new Color32((byte)(argb>>16),(byte)(argb>>8),(byte)argb,(byte)(argb>>24));
        }
    }
}
