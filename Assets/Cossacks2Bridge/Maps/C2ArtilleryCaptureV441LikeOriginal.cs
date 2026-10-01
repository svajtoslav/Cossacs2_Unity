using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private bool _captureSettingsLoadedV441;
        private bool _enableCapturingV441, _defendOnlyWithFormationsV441;

        internal bool CaptureSettingsV441(out bool defendOnlyWithFormations)
        {
            if (!_captureSettingsLoadedV441)
            {
                _captureSettingsLoadedV441 = true;
                // EngineSettings.h defaults are false. Use the active game's FS,
                // not another installed version's loose EngineSettings.xml.
                try
                {
                    // GSC XML has an anonymous <> root, which System.Xml rejects.
                    string xml = Encoding.UTF8.GetString(_bootstrap.Fs.ReadAllBytes("EngineSettings.xml"));
                    bool.TryParse(Regex.Match(xml, @"<EnableCapturing>\s*(true|false)\s*</EnableCapturing>").Groups[1].Value, out _enableCapturingV441);
                    bool.TryParse(Regex.Match(xml, @"<DefendOnlyWithFormations>\s*(true|false)\s*</DefendOnlyWithFormations>").Groups[1].Value, out _defendOnlyWithFormationsV441);
                }
                catch (Exception e) { Debug.LogWarning("[C2 CAPTURE V441] EngineSettings unavailable: " + e.Message); }
            }
            defendOnlyWithFormations = _defendOnlyWithFormationsV441;
            return _enableCapturingV441;
        }
    }

    public sealed partial class C2UnitOriginalRuntime
    {
        internal int LastCaptureTickV441 = -1;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        // NewMon.cpp::CheckCapture, restricted to abandoned complex artillery.
        // This does not implement building/peasant capture or native AI surrender.
        private void StepArtilleryCaptureV441(C2UnitOriginalRuntime u)
        {
            int tick = C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal;
            if ((tick & 31) != (u.Info.C2ObjectIndexV408LikeOriginal & 31) || u.LastCaptureTickV441 == tick) return;
            u.LastCaptureTickV441 = tick;
            CheckArtilleryCaptureV441(u);
        }

        internal bool CheckArtilleryCaptureV441(C2UnitOriginalRuntime u)
        {
            var gun = u?.Info;
            if (gun == null || gun.IsDeadLikeOriginal || !gun.isActiveAndEnabled ||
                u.HiddenInsideBuildingLikeOriginal || !IsComplexFreeV441(u)) return false;
            var traits = C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(gun);
            if (!traits.Artilery || !traits.Capture || traits.NeverCaptureV441 ||
                !gun.OwnerMode.CaptureSettingsV441(out bool defendOnlyFormation)) return false;

            byte mask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(gun);
            int cx = (int)gun.RealXFloat >> 11, cy = (int)gun.RealYFloat >> 11;
            int radius = 3 + (traits.AddShotRadius + 64) / 128;
            int distance = (360 + traits.AddShotRadius) * 16;
            C2NeutralPeasantUnitInfoV2LikeOriginal captor = null;
            // Native scans y-major cells and the first eligible object in each
            // cell. BrigadeID is required by the outer CheckCapture condition,
            // even when CaptureOnlyWithFormations is false.
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                var cell = C2LiveUnitCellIndex.GetCellWithPotentialEnemies(x, y, mask);
                if (cell == null) continue;
                foreach (var other in cell)
                {
                    if (other == null || !other.isActiveAndEnabled || other.IsDeadLikeOriginal || other.Nation == 7 ||
                        (C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(other) & mask) != 0) continue;
                    var ot = C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(other);
                    if (ot.Capture || ot.CantCaptureV441) continue;
                    if (InFormationForCaptureV441(other) && CaptureDistanceV441(gun, other) < distance) captor = other;
                    break;
                }
            }
            if (captor == null) return false;
            if (gun.ActivityStateV413LikeOriginal == 1)
            { captor.PlayDeathOneShotLikeOriginal(captor.RealDir); return false; }
            radius++; distance += 128 * 16;
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                var cell = C2LiveUnitCellIndex.GetCell(x, y);
                if (cell == null) continue;
                foreach (var other in cell)
                {
                    if (other == null || !other.isActiveAndEnabled || other.IsDeadLikeOriginal ||
                        (C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(other) & mask) == 0 ||
                        C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(other).Capture ||
                        (defendOnlyFormation && !InFormationForCaptureV441(other))) continue;
                    if (CaptureDistanceV441(gun, other) < distance) return false;
                    break;
                }
            }

            int oldNation = gun.Nation;
            gun.GetComponent<C2CombatRuntimeV334LikeOriginal>()?.CancelForExternalOrderLikeOriginal("CheckCapture");
            StopArtilleryV439(u);
            gun.SetSelected(false);
            gun.C2RenewIdentityAfterCaptureV441();
            gun.SettlementAiControlledLikeOriginal = false;
            gun.SettlementAllegianceNationLikeOriginal = -1;
            gun.Nation = captor.Nation;
            if (u.Probe != null) u.Probe.Nation = gun.Nation;
            gun.ControllableByPlayer = C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(gun.Nation);
            C2NationCityRuntimeV384ALikeOriginal.RefreshNowLikeOriginal("artillery_capture");
            Debug.Log("[C2 CAPTURE V441] gun=" + gun.SourceMonsterId + " old=" + oldNation + " new=" + gun.Nation +
                " captor=" + captor.C2ObjectIndexV408LikeOriginal + " serial=" + gun.C2ObjectSerialLikeOriginal);
            return true;
        }

        private static bool InFormationForCaptureV441(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        { return C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out _); }

        private static int CaptureDistanceV441(C2NeutralPeasantUnitInfoV2LikeOriginal a, C2NeutralPeasantUnitInfoV2LikeOriginal b)
        { return C2OriginalMovementMathV352.Norma((int)(a.RealXFloat - b.RealXFloat), (int)(a.RealYFloat - b.RealYFloat)); }
    }
}
