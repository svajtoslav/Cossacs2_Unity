using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2HudAvailabilityV453
    {
        private static Material grayscale;
        internal static void Apply(Image image, bool available)
        {
            if(image==null)return;
            if(!available&&grayscale==null)
                grayscale=new Material(Shader.Find("Cossacks2/UIGrayscaleV453")) {name="C2_DisabledIcon_Grayscale"};
            image.material=available?null:grayscale;
            image.color=Color.white;
        }
    }
}
