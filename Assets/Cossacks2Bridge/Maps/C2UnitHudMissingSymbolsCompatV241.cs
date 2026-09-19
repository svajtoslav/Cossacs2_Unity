// C2UnitHudMissingSymbolsCompatV248.cs
// V248: only keeps the shared HUD state fields required by C2GameplayHudV1.
// The building HUD methods are restored from C2GameplayHudBuildingsV1.cs.
// Do not keep stub ResolveBuildingTitle/AttachBuildingConstructionProgressUpdater here,
// otherwise the real building menu/production panel is shadowed or duplicated.

using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private string _lastGlobalBrigDialogStateKeyV172LikeOriginal = string.Empty;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _hoverBrigCreateUnitV165LikeOriginal;
        private C2SettlementBuildingSelectableV1LikeOriginal _hoverBrigCreateBuildingV166LikeOriginal;
    }
}
