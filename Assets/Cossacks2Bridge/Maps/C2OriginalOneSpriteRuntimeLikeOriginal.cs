// C2OriginalOneSpriteRuntimeLikeOriginal.cs
// V1G: invisible original OneSprite runtime identity layer.
// Rendering may stay batched, but gameplay uses this component as the original engine did:
// OneSprite -> ObjCharacter -> ResType / WorkRadius / ResPerWork.

using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2OriginalOneSpriteRuntimeLikeOriginal : MonoBehaviour
    {
        [Header("Original OneSprite identity")]
        public string ExactKey = string.Empty;       // SIGN:SGINDEX:X:Y
        public string SignSpriteKey = string.Empty;  // SIGN:SGINDEX
        public string Sign = string.Empty;           // GA / TS / OC
        public int SpriteIndex;                      // SGIndex from original OneSprite
        public string ObjectId = string.Empty;       // D338 / ST1 / FW4
        public int OriginalX;
        public int OriginalY;
        public int OriginalZ;
        public int OriginalOrder;
        public string OriginalSection = string.Empty;
        public int NIndex;
        public int Locking;
        public bool HasMatrix;

        [Header("ObjCharacter resource")]
        public byte ResourceId = 255;                // 0 WOOD, 2 STONE, 3 FOOD, 0xFE none, 0xFF remove
        public string ResourceName = "EMPTY";
        public int ResPerWork;
        public int WorkRadius;
        public int WorkAmount;
        public int WorkNextIndex = -1;
        public int TimeAmount;
        public int TimeNextIndex = -1;

        [Header("Complex/field metadata")]
        public bool IsFieldPath;
        public int FieldWidth;
        public int FieldHeight;
        public int FieldGrowStage;
        public int FieldYScale;

        [Header("Unity runtime bridge")]
        public Vector3 WorldPosition;
        public Renderer LinkedBatchRenderer;
        public string LinkedBatchRendererName = string.Empty;
        public float LinkedBatchDistance;
        public bool LinkedBatchBoundsContains;

        public bool IsWoodLikeOriginal
        {
            get { return ResourceId == C2BattleTerrainMode.C2OriginalResourceWoodV1LikeOriginal; }
        }

        public bool IsStoneLikeOriginal
        {
            get { return ResourceId == C2BattleTerrainMode.C2OriginalResourceStoneV1LikeOriginal; }
        }

        public bool IsFoodLikeOriginal
        {
            get { return ResourceId == C2BattleTerrainMode.C2OriginalResourceFoodV1LikeOriginal; }
        }

        public void ConfigureLikeOriginal(
            string exactKey,
            string signSpriteKey,
            string sign,
            int spriteIndex,
            string objectId,
            int originalX,
            int originalY,
            int originalZ,
            int originalOrder,
            string originalSection,
            int nIndex,
            int locking,
            bool hasMatrix,
            byte resourceId,
            string resourceName,
            int resPerWork,
            int workRadius,
            int workAmount,
            int workNextIndex,
            int timeAmount,
            int timeNextIndex,
            bool isFieldPath,
            int fieldWidth,
            int fieldHeight,
            int fieldGrowStage,
            int fieldYScale,
            Vector3 worldPosition,
            Renderer linkedBatchRenderer)
        {
            ExactKey = exactKey ?? string.Empty;
            SignSpriteKey = signSpriteKey ?? string.Empty;
            Sign = sign ?? string.Empty;
            SpriteIndex = spriteIndex;
            ObjectId = objectId ?? string.Empty;
            OriginalX = originalX;
            OriginalY = originalY;
            OriginalZ = originalZ;
            OriginalOrder = originalOrder;
            OriginalSection = originalSection ?? string.Empty;
            NIndex = nIndex;
            Locking = locking;
            HasMatrix = hasMatrix;
            ResourceId = resourceId;
            ResourceName = resourceName ?? string.Empty;
            ResPerWork = resPerWork;
            WorkRadius = workRadius;
            WorkAmount = workAmount;
            WorkNextIndex = workNextIndex;
            TimeAmount = timeAmount;
            TimeNextIndex = timeNextIndex;
            IsFieldPath = isFieldPath;
            FieldWidth = fieldWidth;
            FieldHeight = fieldHeight;
            FieldGrowStage = fieldGrowStage;
            FieldYScale = fieldYScale;
            WorldPosition = worldPosition;
            LinkedBatchRenderer = linkedBatchRenderer;
            LinkedBatchRendererName = linkedBatchRenderer != null && linkedBatchRenderer.gameObject != null ? linkedBatchRenderer.gameObject.name : string.Empty;
            transform.position = worldPosition;
        }

        public override string ToString()
        {
            return "C2OneSpriteRuntime " + ResourceName + " " + Sign + ":" + ObjectId +
                   " sg=" + SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                   " xy=(" + OriginalX.ToString(CultureInfo.InvariantCulture) + "," + OriginalY.ToString(CultureInfo.InvariantCulture) + ")" +
                   " workRadius=" + WorkRadius.ToString(CultureInfo.InvariantCulture) +
                   " resPerWork=" + ResPerWork.ToString(CultureInfo.InvariantCulture);
        }

        private void OnDrawGizmosSelected()
        {
            if (IsStoneLikeOriginal)
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            else if (IsFoodLikeOriginal)
                Gizmos.DrawWireCube(transform.position, new Vector3(2f, 2f, 2f));
            else if (IsWoodLikeOriginal)
                Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
    }
}
