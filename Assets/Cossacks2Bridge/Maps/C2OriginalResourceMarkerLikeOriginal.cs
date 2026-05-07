// C2OriginalResourceMarkerLikeOriginal.cs
// V1F: debug/runtime identity marker for original TRE2 resource objects.
// This component is intentionally data-only. It lets Unity scene objects carry the same identity
// the original engine used: sign + SGIndex + original map XY + ResType.

using System;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2OriginalResourceMarkerLikeOriginal : MonoBehaviour
    {
        [Header("Original Cossacks 2 identity")]
        public string ExactKey = string.Empty;      // SIGN:SGINDEX:X:Y
        public string SignSpriteKey = string.Empty; // SIGN:SGINDEX
        public string Sign = string.Empty;          // GA / TS / OC
        public int SpriteIndex;
        public string ObjectId = string.Empty;      // D362 / ST1 / FW4...
        public int OriginalX;
        public int OriginalY;
        public int OriginalOrder;

        [Header("Resource")]
        public byte ResourceId = 255;
        public string ResourceName = "EMPTY";
        public int WorkRadius;
        public int ResPerWork;

        public void ConfigureLikeOriginal(
            string exactKey,
            string signSpriteKey,
            string sign,
            int spriteIndex,
            string objectId,
            int originalX,
            int originalY,
            byte resourceId,
            string resourceName,
            int workRadius,
            int resPerWork,
            int originalOrder)
        {
            ExactKey = exactKey ?? string.Empty;
            SignSpriteKey = signSpriteKey ?? string.Empty;
            Sign = sign ?? string.Empty;
            SpriteIndex = spriteIndex;
            ObjectId = objectId ?? string.Empty;
            OriginalX = originalX;
            OriginalY = originalY;
            ResourceId = resourceId;
            ResourceName = resourceName ?? string.Empty;
            WorkRadius = workRadius;
            ResPerWork = resPerWork;
            OriginalOrder = originalOrder;
        }

        public override string ToString()
        {
            return "C2ResourceMarker " + ResourceName + " " + Sign + ":" +
                   ObjectId + " sg=" + SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                   " xy=(" + OriginalX.ToString(CultureInfo.InvariantCulture) + "," + OriginalY.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
