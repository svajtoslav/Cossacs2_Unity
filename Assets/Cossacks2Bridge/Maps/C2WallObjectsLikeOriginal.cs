﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2WallObjectsV1EnabledLikeOriginal = true;
        private const bool C2WallObjectsV1DrawDebugPlaceholdersLikeOriginal = false;
        private const bool C2WallObjectsV1DebugSpawnMostIfMapHasNoWallsLikeOriginal = false;
        private const float C2WallObjectsV1YOffsetLikeOriginal = 0.0f; // V8: no debug lift; wall/bridge objects must sit on the terrain like original ExtraHeightObject.
        private const float C2WallObjectsV1SpriteWorldScaleLikeOriginal = 0.500f;
        private const int C2WallObjectsV1RenderQueueLikeOriginal = 3610;
        // V10: original is the source of formulas/data, Unity is the executor.
        // Saved Matrix4D from 2ERT/TRE2 is kept for audit only. DirectX Matrix4D is NOT applied
        // as a Unity transform because it flips/skews/proj-space sprites in the Unity heightfield.
        private const bool C2WallObjectsV10UseAdaptedBuilderForMapSpritesLikeOriginal = true;
        private const bool C2WallObjectsV10LogSavedM4DebugOnlyLikeOriginal = true;
        // V12: do NOT force all saved WL map-sprites into one universal billboard card.
        // That made V/H/U aligned objects stand sideways and left huts/fences unchanged.
        // Use original ALIGNING formulas per object, but keep depthless render state so terrain cannot cut/sink them.
        private const bool C2WallObjectsV11UseUniversalDepthlessCardForSavedWL = false;
        private const float C2WallObjectsV11AllowedBelowGroundPixels = 1.0f;
        private const int C2WallObjectsV11RenderQueueDepthlessCards = 3610;

        // V13: do not touch camera/terrain/roads. Only rebuild WALLS.g16 saved WL layer with a selectable basis.
        // F2 cycles basis modes at runtime and rebuilds this separate wall-object layer only.
        private const bool C2WallObjectsV13BasisHotkeyEnabledLikeOriginal = false; // V18: final path freezes V12Aligned; no runtime basis experiments
        private const KeyCode C2WallObjectsV13CycleBasisKeyLikeOriginal = KeyCode.F2;
        private const int C2WallObjectsV13DebugCandidateLogLimitLikeOriginal = 24;

        // V14/V15: saved WL instances are not the final editor truth. Original editor/ReCreate joins
        // pieces through LeftEdges/RightEdges, but old M3D maps can contain only already-saved WL sprites.
        // V15 is conservative: snap only runs whose saved step already matches connector step and whose
        // direction is stable. Other saved WL instances stay at their stored anchors.
        private const bool C2WallObjectsV14ConnectorChainEnabledLikeOriginal = false;
        private const int C2WallObjectsV14MinChainRunLengthLikeOriginal = 3;
        private const float C2WallObjectsV14ConnectorStepFallbackPixelsLikeOriginal = 48.0f;
        private const float C2WallObjectsV14ColorMulLikeOriginal = 1.0f; // V18: use true decoded WALLS.g16 color, no whitening/dimming experiments
        private const float C2WallObjectsV14AlphaLikeOriginal = 1.0f;
        private const float C2WallObjectsV15ConnectorStepToleranceLikeOriginal = 0.18f;
        private const float C2WallObjectsV15DirectionCosToleranceLikeOriginal = 0.94f;
        private const float C2WallObjectsV15BridgeEmbedPixelsLikeOriginal = 10.0f; // V18: mild bridge sink only; panplane/depth handles actual clipping
        private const float C2WallObjectsV15MinorEmbedPixelsLikeOriginal = 0.0f; // V18: do not push fences/huts/barrels into terrain

        // V16: saved WL entries from 2ERT/TRE2 are already authored G16 sprite instances.
        // For them the original-like Unity rule is: preserve the exact decoded frame pixels and place
        // a rigid frozen-camera sprite card at the saved anchor. Do NOT affine-warp the texture through
        // ALIGNING V/H/U, and do NOT resnap saved anchors into a new connector chain.
        // ReCreate/LeftEdges/RightEdges stays for real editor wall-lines, not for already baked map sprites.
        private const bool C2WallObjectsV16UseRigidSavedWLSpriteCardsLikeOriginal = false;
        private const bool C2WallObjectsV16DisableConnectorSnapForSavedWL = true; // V17: no connector snap for saved WL; ReCreate only for real lines
        private const bool C2WallObjectsV16UseTextureTrueColorLikeOriginal = true;
        private const float C2WallObjectsV16SavedWLExtraScaleLikeOriginal = 1.0f;
        private const float C2WallObjectsV16BridgeEmbedScreenPixelsLikeOriginal = 14.0f;
        private const float C2WallObjectsV16MinorEmbedScreenPixelsLikeOriginal = 6.0f;

        // V17 rollback/stabilization: V16 rigid-card path was wrong for map-saved WL objects.
        // It forced all 626 WL into billboard cards (adaptedBillboard=626), losing ALIGNING V/H/U.
        // V17 returns to the last visually correct orientation path: ADAPTED_ALIGN_V/H/U, no connector re-snap.
        // Original ReCreate remains available only when real WallNode/WallLine data exists.

        // V18: saved WL objects are split into profiles. Do NOT apply one universal rule to bridges, fences,
        // dam/bridge C2M model placeholders and tiny props. Model-backed entries are not rendered as WALLS.g16
        // cards because the original path uses C2M/IMM for those; drawing their G16 frame is what produced
        // upside-down huts/barrels/sheds.
        private const bool C2WallObjectsV18UseSavedWLProfilesLikeOriginal = true;
        private const bool C2WallObjectsV18SkipModelBackedSavedWLUntilC2MRenderer = true;
        private const int C2WallObjectsV18RenderQueueLikeOriginal = 3005;

        // V19: real split by saved-WL family. Bridge W58/W59 keeps the last good V12 aligned
        // orientation. Fence/prop-like saved WL can use the saved original Matrix4D because these
        // records are authored object instances, not bridge chains. Model-backed records stay skipped
        // until the C2M/IMM renderer exists; drawing their WALLS.g16 frame is what made huts/barrels upside-down.
        private const bool C2WallObjectsV19UseProfileMeshBuildersLikeOriginal = true;
        private const bool C2WallObjectsV19UseSavedM4ForFenceAndPropsLikeOriginal = true;
        private const bool C2WallObjectsV19SkipModelBackedUntilC2MRendererLikeOriginal = true;
        private const bool C2WallObjectsV19BridgeUseV12AlignedOnlyLikeOriginal = true;
        private const float C2WallObjectsV19BridgeEmbedPixelsLikeOriginal = 4.0f;
        private const float C2WallObjectsV19FenceEmbedPixelsLikeOriginal = 0.0f;

        // V20: hard split like the original system contract:
        //   editor WALL/LLAW Edges+Lines -> ReCreate path only;
        //   saved TRE2/WL objects        -> saved visual object route only;
        //   MODEL-backed WL              -> C2M/IMM route, never fake WALLS.g16 card.
        // This patch deliberately does not touch terrain, roads, water, texture pipeline or camera.
        private const bool C2WallObjectsV20UseOriginalRouteSystemLikeOriginal = true;
        private const bool C2WallObjectsV20SavedWLNeverUsesConnectorReCreateLikeOriginal = true;
        private const bool C2WallObjectsV20UseSavedM4ForFenceLikeOriginal = true;
        private const bool C2WallObjectsV20SkipModelBackedUntilC2MRendererLikeOriginal = true;
        private const int C2WallObjectsV20RouteAuditLimitLikeOriginal = 64;

        // V21: saved TRE2/WL records already carry the final original Matrix4D.
        // ReCreate is still editor-lines only; saved WL never uses connector-resnap.
        // Bridge/fence now use Matrix4D only after a strict row-vector basis verifier.
        private const bool C2WallObjectsV21UseVerifiedSavedM4ForBridgeLikeOriginal = true;
        private const bool C2WallObjectsV21UseVerifiedSavedM4ForFenceLikeOriginal = true; // V116: use saved M4 for fence orientation/side; clamp Y to terrain after build
        private const bool C2WallObjectsV21FenceVariantFromSavedM4LikeOriginal = true;
        private const bool C2WallObjectsV21SkipModelBackedUntilC2MRendererLikeOriginal = true;
        private const int C2WallObjectsV21MatrixAuditLimitLikeOriginal = 96;
        private const float C2WallObjectsV21MatrixMinAxisLenLikeOriginal = 0.05f;
        private const float C2WallObjectsV21MatrixMaxAxisLenLikeOriginal = 4.50f;
        private const float C2WallObjectsV21MatrixMinAreaLikeOriginal = 0.015f;

        // V23: model-backed saved WL path is no longer a fake WALLS.g16/proxy-only route.
        // It loads Models\\*.c2m from the original Data root, parses the Carcass GPCO mesh,
        // applies the saved Matrix4D row-vector transform and renders it as Unity mesh.
        // Proxy remains only as a hard fallback when the model file is absent or unparsable.
        private const bool C2WallObjectsV22EmitModelBackedSavedWLLikeOriginal = true;
        private const bool C2WallObjectsV23UseRealC2MRendererForModelBackedWLLikeOriginal = true;
        private const bool C2WallObjectsV23AllowC2MProxyFallbackWhenRendererFailsLikeOriginal = false;
        private const int C2WallObjectsV22ModelAuditLimitLikeOriginal = 96;
        private const float C2WallObjectsV22ModelProxyHeightPixelsLikeOriginal = 96.0f;
        private const float C2WallObjectsV22ModelProxyMinFootprintPixelsLikeOriginal = 24.0f;
        private const float C2WallObjectsV22ModelProxyAlphaLikeOriginal = 0.55f;
        private const string C2WallObjectsV23BasisContractLikeOriginal = "SavedMatrix4D_RealC2M_NoV12FinalBasis";
        // V24: continue the original IMM path for model-backed WL:
        //   visual Carcass mesh + GEOM Navimesh/Lockmesh audit + model material/depth split.
        // Unity still does not mutate the terrain heightmap here; it logs the exact scan contract first.
        private const bool C2WallObjectsV24ParseIMMGeomNodesLikeOriginal = true;
        private const bool C2WallObjectsV24UseOpaqueMaterialForC2MModelsLikeOriginal = true;
        private const bool C2WallObjectsV24AuditIMMHeightLockScanLikeOriginal = true;
        private const int C2WallObjectsV24ImmAuditLimitLikeOriginal = 96;
        private const int C2WallObjectsV24ModelRenderQueueLikeOriginal = 2450;
        private const string C2WallObjectsV24IMMContractLikeOriginal = "Carcass_visual_plus_GEOM_Navimesh_Lockmesh_IMM_audit";

        // V25: apply the original IMM ScanHeightmap contract into a Unity-side wall extra layer.
        // Original AddExtraHeightObject mutates WMAP.ExObjs height/lock fields from Navimesh/Lockmesh.
        // Unity terrain data is not rewritten here; the exact cells and height deltas are accumulated in
        // a separate runtime layer and lock geometry can be exposed as a MeshCollider.
        private const bool C2WallObjectsV25ApplyIMMHeightLockLayerLikeOriginal = true;
        private const bool C2WallObjectsV25CreateLockMeshColliderLikeOriginal = true;
        private const int C2WallObjectsV25ImmLayerAuditLimitLikeOriginal = 96;
        private const int C2WallObjectsV25ScanCellSizeOriginalPixelsLikeOriginal = 16;
        private const float C2WallObjectsV25LockColliderYOffsetPixelsLikeOriginal = 1.0f;
        private const string C2WallObjectsV25IMMContractLikeOriginal = "Carcass_visual_GEOM_Navimesh_Lockmesh_IMM_height_lock_layer";

        // V26: tighten C2M visual render-state/material contract.
        // Original C2M Carcass vertices carry diffuse color; Unity must not render model-backed WL as
        // an uncolored white mesh. Use a dedicated opaque vertex-color shader and preserve V25 IMM layer.
        // Terrain / roads / terrain texturing remain untouched.
        private const bool C2WallObjectsV26UseC2MVertexColorMaterialLikeOriginal = true;
        private const bool C2WallObjectsV26AuditC2MColorBoundsLikeOriginal = true;
        private const int C2WallObjectsV26MaterialAuditLimitLikeOriginal = 96;
        private const string C2WallObjectsV26RenderContractLikeOriginal = "C2M_Carcass_vertexColor_opaque_ZWriteOn_LEqual_Offset";

        // V27: audit-only coverage pass. It does not change rendering, terrain, roads, texturing,
        // saved-WL routing, ReCreate, connector snap, C2M visual render or IMM layer.
        // The goal is to prove what is really covered against M3D TRE2/WL and walls.lst/walls.rsr:
        // every saved WL id, every MODEL entry, every TRE2 sign bucket (WL/TS/OC/other), and
        // C2M Carcass/Navimesh/Lockmesh parse status.
        private const bool C2WallObjectsV27CoverageAuditLikeOriginal = true;
        private const int C2WallObjectsV27ModelCoverageLimitLikeOriginal = 160;
        private const int C2WallObjectsV27MapSignIndexLimitLikeOriginal = 96;
        private const string C2WallObjectsV27AuditContractLikeOriginal = "M3D_TRE2_WL_TS_OC_plus_walls_lst_rsr_MODEL_C2M_coverage_no_render_changes";

        // V28: boundary-safe TRE2 object pipeline bootstrap. This does NOT render OC/TS/GA yet;
        // it records their exact map placement/orientation payload from M3D so the next renderer can
        // be matched against the original object system without corrupting the already-correct WL route.
        private const bool C2WallObjectsV28Tre2ObjectPipelineAuditLikeOriginal = true;
        private const int C2WallObjectsV28ObjectAuditLimitLikeOriginal = 160;
        private const int C2WallObjectsV28ObjectSampleLimitLikeOriginal = 36;
        private const string C2WallObjectsV28ObjectContractLikeOriginal = "TRE2_nonWL_route_boundary_OC_TS_GA_not_OneWallsSystem_no_render_changes";

        // V29: do not touch saved-WL geometry/Matrix4D/ReCreate rules. The screenshot proved the route is
        // structurally correct but W58/W59 can render as a white alpha silhouette. Audit decoded WALLS.g16
        // RGBA for W58/W59/W70/W74 and use an original-style cutout wall-sprite material. If a bridge
        // frame is decoded as an alpha-only white mask, tint only that bridge sprite instead of moving it.
        private const bool C2WallObjectsV29AuditWallSpriteRgbaLikeOriginal = true;
        private const bool C2WallObjectsV29UseDedicatedWallSpriteShaderLikeOriginal = true;
        private const bool C2WallObjectsV29RepairWhiteBridgeAlphaMaskLikeOriginal = true;
        private const int C2WallObjectsV29SpriteAuditLimitLikeOriginal = 96;
        private const float C2WallObjectsV29AlphaCutoffLikeOriginal = 0.015f;
        private const float C2WallObjectsV29WhiteMaskRgbMinLikeOriginal = 238.0f;
        private const float C2WallObjectsV29WhiteMaskFractionLikeOriginal = 0.82f;
        private const string C2WallObjectsV29SpriteRenderContractLikeOriginal = "WALLS_g16_RGBA_audit_alphaCutout_whiteBridgeMaskRepair_no_geometry_changes";

        // V31: W58/W59 must render as real WALLS.g16 sprite RGB, not as any repair/tint/mask path.
        // Keep saved Matrix4D geometry and strict WALLS.g16 decode, but force bridge side sprites through
        // an exact-texture cutout shader: RGB comes directly from the decoded frame, alpha is used only
        // for clip/cutout, vertex color does not modulate RGB, ZTest=LEqual, ZWrite=Off.
        private const bool C2WallObjectsV31UseExactBridgeSpriteCutoutLikeOriginal = true;
        private const string C2WallObjectsV31SpriteRenderContractLikeOriginal = "W58_W59_WALLS_g16_direct_RGB_alpha_clip_no_vertex_tint_ZTest_LEqual_ZWrite_Off";

        // V37: rollback the failed V33/V36 idea of stretching WALLS.g16 over the whole #DAMBA C2M.
        // The log proved transparent pixels dominate the WALLS.g16 frame, so filling them creates a gray
        // matte over the entire C2M mesh. #DAMBA must stay C2M-based until the real C2M material chunks
        // are parsed. W58/W59 remain the WALLS.g16 side sprites; W60/W63 remain C2M models.
        private const bool C2WallObjectsV33UseWallSpriteTextureForDambaC2MLikeOriginal = false;
        private const bool C2WallObjectsV33UseDedicatedDambaC2MShaderLikeOriginal = false;
        private const int C2WallObjectsV33DambaAuditLimitLikeOriginal = 64;
        private const string C2WallObjectsV33DambaRenderContractLikeOriginal = "DAMBA_C2M_no_WALLS_g16_fullUV_rollback";
        private const bool C2WallObjectsV34UseSolidRgbForDambaC2MLikeOriginal = false;
        private const bool C2WallObjectsV34ForceDambaVisibleOverTerrainUntilExtraHeightPipelineLikeOriginal = false;
        private const string C2WallObjectsV34DambaDepthContractLikeOriginal = "DAMBA_C2M_LEqual_no_force_visible";
        private const bool C2WallObjectsV35UseSeparateDambaSideOverlayLikeOriginal = false;
        // V178: user-calibrated WALS2D fence shadow lift. Values are loaded from the same
        // map-local instruction file as the V93 damba saved poses and can be adjusted in Scene GUI.
        private const float C2WallObjectsV35VerticalFenceRaisePixelsLikeOriginal = 0.0f; // V178: default only; runtime value is _c2Wals2DVerticalRaisePixelsV178LikeOriginal
        private const float C2WallObjectsV178DefaultHorizontalFenceRaisePixelsLikeOriginal = 0.0f;
        private const float C2WallObjectsV178HeightSliderMinLikeOriginal = -48.0f;
        private const float C2WallObjectsV178HeightSliderMaxLikeOriginal = 48.0f;
        private const bool C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal = true;
        private const int C2WallObjectsV118AuditLimitLikeOriginal = 180;
        private const float C2WallObjectsV118LargeFenceMinDescExtentLikeOriginal = 96.0f;
        private const float C2WallObjectsV118LargeFenceMinAlignSpanLikeOriginal = 96.0f;
        private const string C2WallObjectsV118ContractLikeOriginal = "V118_WALLS_2D_AUDIT_AND_LARGE_FENCE_CLASSIFICATION";
        private const bool C2WallObjectsV119TopOffenderAuditWL2DLikeOriginal = true;
        private const int C2WallObjectsV119TopOffenderLimitLikeOriginal = 24;
        private const float C2WallObjectsV119GroundEpsilonWorldLikeOriginal = 0.05f;
        private const float C2WallObjectsV119OffsetEpsilonOriginalPxLikeOriginal = 16.0f;
        private const string C2WallObjectsV119ContractLikeOriginal = "V119_WL2D_TOP_OFFENDER_AUDIT_NO_PLACEMENT_CHANGES";
        private const string C2WallObjectsV120BridgeSideContractLikeOriginal = "V120_W58_W59_bridge_side_saved_M4_basis_saved_WL_XY_terrain_contact";
        private const string C2WallObjectsV121BridgeSideContractLikeOriginal = "V121_DISABLED_BY_V123_alignpoints_footline_clamp_buried_W58_W59_keep_V120";
        private const string C2WallObjectsV123BridgeSideContractLikeOriginal = "V123_W58_W59_rollback_to_V120_min_vertex_contact_keep_props_V122";
        private const bool C2WallObjectsV122FlipVerticalAlignedPropUvLikeOriginal = false; // V124: V122 double-flipped prop UVs; keep base WALLS.g16 top-left -> Unity V conversion only.
        private const string C2WallObjectsV122VerticalPropContractLikeOriginal = "V122_DISABLED_BY_V124_no_extra_prop_uv_flip";
        private const bool C2WallObjectsV124UseSavedM4BasisForVerticalPropsLikeOriginal = true;
        private const bool C2WallObjectsV124ClampSavedM4PropsToTerrainLikeOriginal = false; // V172: original WL path has no Unity post-clamp after DrawWSprite/AddWorldPoint.
        private const string C2WallObjectsV124PropContractLikeOriginal = "V173_DISABLED_original_WL_no_Unity_post_clamp_no_extra_uv_flip_after_DrawWSprite_AddWorldPoint";
        // V131: V129 proved the hovering large side fence cards live in DELETED_LEGACY_WALS2D_ROUTE,
        // while V130 proved W58 is the already-grounded vertical family. Lower only W59.
        // V132: saved WL fence cards must be assembled as straight object rows, like the already fixed DAMBA rows.
        // Only real WALLS/WL fence objects are touched. Terrain, roads, trees, buildings, DAMBA C2M and bridge side W58/W59 stay unchanged.
        // The patch does two safe things per contiguous fence run:
        //   1) projects every saved WL X/Y anchor onto the detected row direction, removing perpendicular saw-tooth jitter;
        //   2) reuses one Matrix4D basis for the whole row, removing per-section tilt/slope jitter while keeping each section planted by its own X/Y.
        private const bool C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal = false; // V172: saved WL/WALS 2D must not be resnapped/straightened; original draws per OneSprite.
        private const int C2WallObjectsV132FenceMinRunLengthLikeOriginal = 2;
        private const float C2WallObjectsV132FenceMinStepOriginal = 8.0f;
        private const float C2WallObjectsV132FenceMaxStepOriginal = 160.0f;
        private const float C2WallObjectsV132FenceDirectionDotLikeOriginal = 0.82f;
        private const float C2WallObjectsV132FenceMaxProjectionCorrectionOriginal = 42.0f;
        private const int C2WallObjectsV132FenceAuditLimitLikeOriginal = 24;
        private const bool C2WallObjectsV135UseUnifiedFenceSavedXYBasisMeshLikeOriginal = false; // V143: do not force WL fence through unified/explicit card route; preserve original sprite orientation.
        private const bool C2WallObjectsV139DisableLegacyFencePostPlacementForUnifiedLineLikeOriginal = false; // V143: keep original reanchor/clamp; V139 caused sink/incorrect final placement.
        private const bool C2WallObjectsV140UseExplicitUnifiedFenceLineCardMeshLikeOriginal = false; // V143: explicit world line card rotated WALLS sprites into wrong sides.
        private const float C2WallObjectsV142FenceLineMarkerLikeOriginal = 142.0f;
        private const bool C2WallObjectsV144BuildIdenticalWL2DFenceLineRootsLikeOriginal = false; // V172: remove synthetic WALS2D line-root; original saved WL renders per OneSprite, not one combined mesh.
        private const float C2WallObjectsV153SideLineMaxPerpErrorLikeOriginal = 54.0f;
        private const bool C2WallObjectsV157UseOriginalOneWallsSystemPortForWL2DFenceLikeOriginal = true;
        private const bool C2WallObjectsV157UseSecondSmoothPassLikeOriginal = true;
        // V158: the saved-WL fence run is now rebuilt through a real in-memory OneWallsSystem graph:
        // virtual Start/Final OneWallEdge, OneWallLine, Start.Points[0].x_out and Final.Points[0].x_in.
        // This removes the V157 shortcut where x0/x1 were used directly without edge connector points.
        private const bool C2WallObjectsV158BuildVirtualOneWallsGraphForSavedWL2DFenceLikeOriginal = true;
        private const bool C2WallObjectsV158UseEdgeConnectorInOutForLineEndpointsLikeOriginal = true;
        private const bool C2WallObjectsV158UseOneWallPointMatrixForFenceCardsLikeOriginal = false; // kept off for WALLS.g16 cards; C2M/IMM model path uses Matrix4D separately.
        // V159: next port step after V158. If the OneWallElement has a real [MODEL] path from walls.rsr,
        // emit the line root through the already existing C2M/IMM backend and the per-point Matrix4D,
        // matching original AddExtraHeightObject(x,y,ModelID,&M4) -> IMM->Render(ModelID,&M4).
        // WALLS.g16 cards are now only a fallback when the catalog element has no ModelID/model path.
        private const bool C2WallObjectsV159UseModelIDMatrix4DBackendForOneWallsSystemLineRootsLikeOriginal = true; // V170: strict original wall path uses ModelID/Matrix4D backend first.
        // V160: 3DWalls/OneWallsSystem path must not silently fall back to WALLS.g16 cards.
        // If an element has no ModelID/model path, it is reported in audit and the old individual saved-WL route remains visible.
        private const bool C2WallObjectsV159FallbackToWallsG16CardsWhenModelIDMissingLikeOriginal = false; // V172: real 3DWalls path still requires ModelID; saved WL is handled separately by LoadSprites2 path.
        private const bool C2WallObjectsV160ParseRealWallsListXmlLikeOriginal = true;
        private const bool C2WallObjectsV160RequireModelIDForOneWallsSystem3DWallsLikeOriginal = true; // V170: ModelID/C2M is required for original 3DWalls path.
        // V161: Unity-тест делается снаружи. Здесь добиты кодовые недоделы V160:
        // numeric/ordered ModelID binding, real WT Usage 0/1/2 cycle, robust C2M path resolution,
        // V162 safety: if the 3DWalls line-root is rejected because real WT/model path is missing,
        // keep old saved-WL cards visible. Otherwise the map loses fences completely.
        private const bool C2WallObjectsV161ResolveNumericModelIDThroughWallsRsrOrderLikeOriginal = true;
        private const bool C2WallObjectsV161UseRealWallTypeElementCycleForOneWallsSystemLikeOriginal = true;
        private const bool C2WallObjectsV161SuppressRejectedOneWallsSystemWL3DWallsLikeOriginal = false; // V172: never suppress saved WL sprites when 3DWalls/ModelID line-root is rejected.
        private const bool C2WallObjectsV161ResolveC2MPathCandidatesLikeOriginal = true;
        // V165: hard cut. Saved WL/WALS 2D fence sprites are allowed only through the new line-root builder.
        // Legacy individual card routes are not fallback, not suppressed-at-render, but bypassed before route selection.
        // V171: окончательное разделение путей:
        //   * настоящие 3DWalls/OneWallsSystem ModelID/C2M остаются для объектов, у которых реально есть ModelID;
        //   * saved WL/WALS 2D fence frames W58/W59/W70/W74 и пары 0/1/3/4/5/6/7 идут через WALLS.g16 line-root renderer.
        // Ошибка V170 была в том, что saved WL/WALS 2D кадры заставили пройти WallType->OneWallElement->ModelID,
        // хотя оригинальный путь для них: TRE2 sign='WL' -> addSpriteAnyway(&WALLS) -> CreateMatrix/AddWorldPoint/DrawWSprite.
        private const bool C2WallObjectsV165Wals2DFenceOnlyNo3DWallsModelIDBackendLikeOriginal = false; // V172: no synthetic WALS2D line-root backend; W58/W59/W70/W74 render as saved WL OneSprite records.
        private const bool C2WallObjectsV165HardDeleteLegacySavedWL2DFenceIndividualCardsLikeOriginal = false; // V172: restore original saved WL individual sprite draw path.
        private const bool C2WallObjectsV170StrictOriginalModelIDMatrix4DWallSystemLikeOriginal = false; // V171: strict 3DWalls ModelID path is wrong for saved WL/WALS 2D WALLS.g16 fences.
        private const bool C2WallObjectsV170UseRealWallTypeCycleOrRejectLikeOriginal = false; // V171: do not reject WALS2D when WallType->OneWallElement->ModelID cycle is absent.
        private const bool C2WallObjectsV170UseSavedWLMatrix4DAuditLikeOriginal = true;
        private string _c2WallObjectsV159LastModelIDAuditLikeOriginal = string.Empty;
        private int _c2WallObjectsV160ModelIDLineRootsUsedLikeOriginal;
        private int _c2WallObjectsV160ModelIDLineRootsRejectedLikeOriginal;
        private int _c2WallObjectsV160SpriteFallbackBlockedLikeOriginal;
        private int _c2WallObjectsV161NumericModelIDResolvedLikeOriginal;
        private int _c2WallObjectsV161NumericModelIDUnresolvedLikeOriginal;
        private int _c2WallObjectsV161Rejected3DWallsSavedWLSuppressedLikeOriginal;
        private int _c2WallObjectsV161RealWallTypeCycleUsedLikeOriginal;
        private const string C2WallObjectsV132FenceLineContractLikeOriginal = "V172_DISABLED_synthetic_line_root_original_saved_WL_per_OneSprite_LoadSprites2_DrawWSprite_AddWorldPoint";
        private const string C2WallObjectsV172OriginalWLSavedSpriteContractLikeOriginal = "V172_original_LoadSprites2_rule_WL_addSpriteAnyway_HaveAligning_ignores_saved_M4_else_DrawWSprite_or_AddWorldPoint";
        private const string C2WallObjectsV173WLSavedSpriteUvContractLikeOriginal = "V173_WALLS_g16_saved_WL_no_manual_Unity_VFlip_uv_00_10_11_01";
        private const string C2WallObjectsV174WLSavedSpriteMapXYContractLikeOriginal = "V174_WL_WALLS_g16_MapSprites_linear_XY_no_terrain_hex_odd_column_offset_for_CreateMatrix_DrawWSprite";
        private const string C2WallObjectsV175WLSavedSpriteSideShadowLiftContractLikeOriginal = "V178_WL_WALLS_g16_user_slider_saved_instruction_vertical_and_horizontal_height_no_hardcoded_lift";

        private sealed class Wals2DHeightAdjustRecordV178LikeOriginal
        {
            public Mesh Mesh;
            public Vector3[] BaseVertices;
            public int SpriteIndex;
            public bool VerticalTopBottom;
            public bool HorizontalLeftRight;
            public float AppliedRaisePixels;
        }

        private readonly List<Wals2DHeightAdjustRecordV178LikeOriginal> _c2Wals2DHeightAdjustRecordsV178LikeOriginal = new List<Wals2DHeightAdjustRecordV178LikeOriginal>();
        private float _c2Wals2DVerticalRaisePixelsV178LikeOriginal = C2WallObjectsV35VerticalFenceRaisePixelsLikeOriginal;
        private float _c2Wals2DHorizontalRaisePixelsV178LikeOriginal = C2WallObjectsV178DefaultHorizontalFenceRaisePixelsLikeOriginal;
        private bool _c2Wals2DHeightInstructionLoadedV178LikeOriginal;
        private string _c2Wals2DHeightInstructionStatusV178LikeOriginal = string.Empty;
        private float _c2Wals2DHeightInstructionStatusUntilV178LikeOriginal;

        private string _c2WallObjectsV157LastReCreateAuditLikeOriginal = string.Empty;
        private const bool C2WallObjectsV36MakeDambaTextureOpaqueLikeOriginal = false;
        private const bool C2WallObjectsV37UseTemporaryStoneTintForDambaUntilC2MMaterialsLikeOriginal = false;
        private const string C2WallObjectsV35DambaRenderContractLikeOriginal = "DAMBA_C2M_base_stone_tint_waiting_real_C2M_material_parser";
        private const string C2WallObjectsV39DambaRenderContractLikeOriginal = "DAMBA_C2M_RESTORED_geometry_visible_no_WALLS_fullUV_temp_material";
        private const int C2WallObjectsV40GPObjAuditLimitLikeOriginal = 16;
        private const int C2WallObjectsV40GPObjChunkPreviewLimitLikeOriginal = 24;
        private const string C2WallObjectsV40GPObjContractLikeOriginal = "C2M_GPOBJ_chunk_table_audit_gpName_frameIdx_chunks_nTri_nVert_flags";
        private const bool C2WallObjectsV41UseGPObjChunkSubmeshesLikeOriginal = true;
        private const string C2WallObjectsV41ChunkRenderContractLikeOriginal = "C2M_GPOBJ_chunk_renderer_submeshes_original_chunk_order_same_temp_material";
        private const bool C2WallObjectsV42UseGPObjFrameTextureForC2MLikeOriginal = true;
        private const int C2WallObjectsV42GPObjMaterialAuditLimitLikeOriginal = 16;
        private const string C2WallObjectsV42MaterialContractLikeOriginal = "C2M_GPOBJ_material_binding_by_gpName_frameIdx_try_real_g16_fallback_temp";
        private const bool C2WallObjectsV43AutoChooseGPObjTextureUvFlipLikeOriginal = true;
        private const int C2WallObjectsV43UvFitSampleLimitLikeOriginal = 1024;
        private const string C2WallObjectsV43UvFitContractLikeOriginal = "C2M_GPOBJ_material_uv_autofit_by_alpha_normal_flipU_flipV_flipUV";
        private const bool C2WallObjectsV44DisableUnityShadowCastingForWallObjectsLikeOriginal = true;
        private const string C2WallObjectsV44ShadowContractLikeOriginal = "WALL_OBJECTS_no_Unity_realtime_shadows_original_uses_pipeline_shadows_not_MeshRenderer_shadowcaster";
        private const bool C2WallObjectsV46UseDrawWChunkDambaFrameLikeOriginal = true;
        private const string C2WallObjectsV46DambaGPObjContractLikeOriginal = "DAMBA_GPOBJ_damba_g16_frame0_bottom_frame3_top_directUV_no_vertexDiffuse";
        private const bool C2WallObjectsV47UseG16SquareRectsForGPObjChunksLikeOriginal = true;
        private const string C2WallObjectsV47GPObjSquareContractLikeOriginal = "GPSystem_DrawWChunk_finalUV_localUV_scaled_to_G16_square_rect_per_submesh";
        // V50: the original GPSystem::DrawWChunk does not rely on Unity material scale/offset.
        // It bakes every chunk local UV (0..1) into the exact square rect of the decoded G16 frame.
        // This prevents DAMBA/CMOST bridge meshes from sharing vertices across differently-offset chunks
        // and removes the old temporary stone tint/material-ST workaround.
        private const bool C2WallObjectsV50BakeDrawWChunkUVIntoMeshLikeOriginal = true;
        private const bool C2WallObjectsV50DrawWChunkTopLeftToUnityVFlipLikeOriginal = true;
        private const string C2WallObjectsV50DrawWChunkContractLikeOriginal = "DAMBA_GPOBJ_DrawWChunk_baked_finalUV_per_chunk_no_material_ST_alpha_cutout_no_temp_tint";
        // V57: keep the already-correct rigid DAMBA geometry and fix only DrawWChunk UV baking.
        // Melinoja/TG16 returns the same top-left ordered RGBA frame that TemnyLess samples with
        // ty = square.y + localV * square.side. Unity LoadRawTextureData keeps that memory order,
        // so applying an additional Unity V flip mirrors the packed square atlas and twists DAMBA
        // details. V57 therefore uses raw top-left square coordinates directly, plus half-texel
        // centers to avoid bleeding into neighbouring packed squares.
        private const bool C2WallObjectsV57DrawWChunkUseTemnyLessRawTopLeftUV = true;
        private const bool C2WallObjectsV57DrawWChunkUseHalfTexelCenters = true;
        private const bool C2WallObjectsV57DrawWChunkFlipLocalU = false;
        private const bool C2WallObjectsV57DrawWChunkFlipLocalV = false;
        private const bool C2WallObjectsV57DrawWChunkSwapLocalUV = false;
        private const string C2WallObjectsV57DrawWChunkContractLikeOriginal = "DAMBA_GPOBJ_TemnyLess_raw_top_left_square_UV_no_extra_Unity_VFlip_halfTexel_no_geometry_changes";
        // V53: diagnostic white connector-chain mode for model-backed WL C2M objects.
        // This intentionally ignores textures/material binding and saved Matrix4D placement for those models:
        // objects are placed as a chain using walls.rsr connector points, centered on the chain anchor,
        // with the model bottom lifted 5 original height units above terrain, rendered as plain white.
        private const bool C2WallObjectsV53UseWhiteModelConnectorChainLikeOriginal = true;
        private const bool C2WallObjectsV53ModelChainIgnoreSavedM4LikeOriginal = true;
        private const bool C2WallObjectsV53ModelChainForceWhiteFillLikeOriginal = false;
        private const float C2WallObjectsV53ModelBottomHeightAboveGroundLikeOriginal = 5.0f;
        private const int C2WallObjectsV53ModelChainAuditLimitLikeOriginal = 64;
        private const string C2WallObjectsV53ModelChainContractLikeOriginal = "MODEL_C2M_connector_chain_centered_bottom_plus5_TEXTURED_TemnyLess_GPObj_DrawWChunk_no_savedM4_V56";
        // V58: keep the good V55/V57 rigid geometry + TemnyLess texture mapping,
        // but stop moving model-backed WL objects into an artificial connector chain.
        // Placement anchor comes from the map TRE2/WL saved X/Y entry again.
        private const bool C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal = true;
        private const string C2WallObjectsV58PlacementContractLikeOriginal = "MODEL_C2M_textured_rigid_TemnyLess_map_saved_WL_anchor_no_connector_resnap";
        // V60: keep V58 map X/Y anchors and V57 texture mapping, but correct bridge vertical placement.
        // V59 aligned the absolute top of every C2M chunk to the terrain/deck line. That pushed most of
        // the bridge model underground and left only rail fragments visible. Original bridge models are
        // anchored around the deck/road plane, not by the highest railing vertex. V60 uses one shared
        // flat height per contiguous DAMBA/CMOST bridge group and aligns an internal deck anchor between
        // local bottom and local top. This keeps rails straight while the wall body remains visible.
        private const bool C2WallObjectsV59LevelModelBackedBridgeRunsLikeOriginal = true;
        private const bool C2WallObjectsV59AnchorBridgeTopToRunHeightLikeOriginal = false;
        private const float C2WallObjectsV59BridgeVerticalOffsetOriginal = 0.0f;
        private const bool C2WallObjectsV60GroupAllContiguousDambaModelsForFlatHeightLikeOriginal = true;
        private const float C2WallObjectsV60BridgeDeckAnchorLocalZFraction = 0.56f;
        private const float C2WallObjectsV60BridgeVerticalOffsetOriginal = 0.0f;
        private const string C2WallObjectsV59PlacementContractLikeOriginal = "MODEL_C2M_DAMBA_map_XY_saved_anchor_flat_group_height_anchor_DECK_not_top_no_connector_resnap_V60";
        // V61: keep V60 height/deck and TemnyLess texture, but normalize DAMBA/CMOST map anchors.
        // The original bridge sides are perfectly straight object rows. Saved TRE2/WL anchors can contain
        // tiny forward/back jitter after Unity coordinate adaptation, which breaks rail lines and creates
        // small visual gaps. V61 projects every DAMBA model row to a single map-space line and spaces
        // objects evenly between the original first/last anchors. It does not touch mesh geometry or UVs.
private const bool C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal = false; // V62 rollback: do not rewrite real map X/Y anchors.
private const bool C2WallObjectsV61PreserveRunFirstLastAnchorsLikeOriginal = true;
private const float C2WallObjectsV61MinStraightenRunLengthLikeOriginal = 2.0f;
private const string C2WallObjectsV61PlacementContractLikeOriginal = "DISABLED_BY_V62_keep_V60_map_saved_XY_anchor_no_synthetic_straight_row";
private const string C2WallObjectsV62PlacementContractLikeOriginal = "MODEL_C2M_DAMBA_map_saved_XY_anchor_keep_V60_deck_height_no_V61_straightening";
private const string C2WallObjectsV64PlacementContractLikeOriginal = "ROLLBACK_V63_return_to_V62_map_saved_XY_keep_V60_deck_height_no_row_axis_projection";
private const bool C2WallObjectsV65UseSavedMatrixForDambaC2MLikeOriginal = false;
private const string C2WallObjectsV65DambaPlacementContractLikeOriginal = "DAMBA_C2M_original_saved_Matrix4D_existingM4_no_anchor_deck_resnap";
private const bool C2WallObjectsV66UseRigidSavedMatrixForDambaC2MLikeOriginal = true;
private const string C2WallObjectsV66DambaPlacementContractLikeOriginal = "DAMBA_C2M_original_saved_Matrix4D_existingM4_rigid_world_delta_no_per_vertex_terrain_warp";
private const bool C2WallObjectsV67StraightenRigidSavedM4DambaRunsLikeOriginal = false;
private const string C2WallObjectsV67DambaPlacementContractLikeOriginal = "DAMBA_C2M_saved_Matrix4D_rigid_delta_straightened_run_anchor_world_origin";
private const bool C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal = false;
private const float C2WallObjectsV68DambaMaxSectionNeighborDistanceOriginal = 135.0f;
private const int C2WallObjectsV68DambaMinSectionsInRun = 3;
private const string C2WallObjectsV68DambaPlacementContractLikeOriginal = "DAMBA_C2M_section_rows_clustered_by_neighbor_distance_resample_between_first_last";
private const bool C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal = false;
private const string C2WallObjectsV69DambaPlacementContractLikeOriginal = "DAMBA_C2M_section_rows_project_perpendicular_only_keep_native_along_spacing";
private const bool C2WallObjectsV70AnchorDambaC2MPivotToSavedWLPointLikeOriginal = false;
private const string C2WallObjectsV70DambaPlacementContractLikeOriginal = "DAMBA_C2M_saved_Matrix4D_rigid_local_C2M_XY_pivot_anchored_to_saved_WL_point";
private const bool C2WallObjectsV71UseDambaSavedWLAnchorNudgeLikeOriginal = false;
private const float C2WallObjectsV71DambaBottomNormalNudgeOriginal = 0.0f;
private const float C2WallObjectsV71DambaTopNormalNudgeOriginal = 0.0f;
private const float C2WallObjectsV71DambaBottomAlongNudgeOriginal = 0.0f;
private const float C2WallObjectsV71DambaTopAlongNudgeOriginal = 0.0f;
private const string C2WallObjectsV71DambaPlacementContractLikeOriginal = "DAMBA_C2M_saved_Matrix4D_rigid_small_anchor_nudge_no_mesh_pivot_rewrite";
private const bool C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal = false;
private static readonly Vector2 C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal = new Vector2(55.396f, -55.494f);
private const float C2WallObjectsV72DambaW60PairDeltaHeightPixelsLikeOriginal = 0.0f;
private const string C2WallObjectsV72DambaPlacementContractLikeOriginal = "DAMBA_pair_calibrated_connector_chain_first_map_anchor_then_previous_plus_delta";
// V73: use the real Stage2 universal anchor calibration TXT instead of only a hard-coded pair delta.
// The first map DAMBA instance keeps its original map anchor; following instances in the same row are
// rebuilt as a chain using CENTER_MAIN -> LEFT/RIGHT root deltas saved from the 3-object calibrator.
private const bool C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal = false;
private const string C2WallObjectsV73DambaPlacementContractLikeOriginal = "DAMBA_C2M_universal_anchor_line_v2_txt_CENTER_MAIN_seed_bidirectional_left_right_delta_chain_V74";
private const int C2WallObjectsV73DambaAuditLimitLikeOriginal = 64;
private const string C2WallObjectsV74DambaPlacementContractLikeOriginal = "CENTER_MAIN_seed_kept_on_map_left_and_right_built_from_Stage2_deltas_not_endpoint_chain";
// V75: V74 really changed s.X/s.Y, but the DAMBA C2M mesh was still rendered through the saved Matrix4D
// rigid path. That keeps the old per-instance matrix/orientation as the visual truth, so the calibrated
// anchor line has almost no visible effect. In universal anchor mode the map Matrix4D must be ignored:
// CENTER_MAIN + Stage2 deltas become the only placement truth.
private const bool C2WallObjectsV75DisableSavedMatrix4DForUniversalDambaAnchorsLikeOriginal = false; // V78: do NOT flatten DAMBA by killing Matrix4D; keep saved basis, override only anchor translation.
private const string C2WallObjectsV75DambaPlacementContractLikeOriginal = "DAMBA_C2M_universal_anchor_line_is_visual_truth_disable_saved_Matrix4D_use_anchor_driven_C2M_mesh";
// V76: previous V73/V74/V75 still used object root deltas from the 3-object calibration.
// That is not point snapping. Stage2 must only choose the nearest edge-anchor pairs;
// runtime placement must compute CENTER_MAIN.localAnchor - MOVING.localAnchor and place by those points.
private const bool C2WallObjectsV76UseNearestAnchorPairSnapDeltasLikeOriginal = false; // V77 rollback: nearest-pair auto matching chooses wrong anchors for DAMBA.
private const string C2WallObjectsV76DambaPlacementContractLikeOriginal = "DAMBA_C2M_universal_anchor_points_are_truth_nearest_two_edge_pairs_snap_not_root_delta";
// V77: Stage2 already stores the authored 3-object pose. Do not re-pair nearest anchors.
// Derive runtime root delta from every saved point: object.point.centerLocal - object.point.localWorld.
// This is point-based, but it preserves the manually aligned CENTER_MAIN pose instead of inventing new spacing.
private const bool C2WallObjectsV77UseAuthoredStage2PointPoseDeltasLikeOriginal = true;
private const string C2WallObjectsV77DambaPlacementContractLikeOriginal = "DAMBA_C2M_stage2_authored_point_pose_delta_from_centerLocal_minus_localWorld_no_nearest_pair_auto_match";
// V78: the visible failure after V77 was not the Stage2 delta. The failure was V75 no_saved_Matrix4D:
// it threw away the original C2M basis/rotation and rendered DAMBA with identity local axes.
// Keep original saved Matrix4D as the 3D basis, but feed it the universal-anchor X/Y chain as translation.
private const bool C2WallObjectsV78UseSavedMatrix4DBasisWithUniversalAnchorTranslationLikeOriginal = true;
private const string C2WallObjectsV78DambaPlacementContractLikeOriginal = "DAMBA_C2M_universal_anchor_translation_plus_original_saved_Matrix4D_basis_no_flat_identity_axes";
// V80: rollback V79 completely. Runtime placement must NOT project anchors through Matrix4D.
// Use only authored Stage2 delta pixels for the W60 row, and keep saved Matrix4D only as mesh basis/rotation.
private const bool C2WallObjectsV80UseStage2DeltaOnlyPreserveSavedMatrix4DBasisLikeOriginal = true;
private const string C2WallObjectsV80DambaPlacementContractLikeOriginal = "DAMBA_C2M_V80_stage2_delta_only_center_seed_bidirectional_preserve_saved_Matrix4D_basis_no_explicit_link_no_projection";
// V81: V80 moved X/Y correctly, but every piece still kept its own original Matrix4D basis.
// For one calibrated W60 DAMBA row this reintroduces per-piece slope/axis jitter and visible broken seams.
// Keep the CENTER_MAIN map seed and Stage2 delta chain, but use one shared Matrix4D basis from the CENTER_MAIN
// piece for the whole row. Matrix translation is still replaced by the calculated chain X/Y.
private const bool C2WallObjectsV81UseSharedCenterMatrixBasisForUniversalDambaRowsLikeOriginal = false;
private const string C2WallObjectsV81DambaPlacementContractLikeOriginal = "DAMBA_C2M_V81_stage2_delta_chain_shared_CENTER_MAIN_Matrix4D_basis_for_whole_row_no_per_piece_basis_jitter";
// V82: V81 still preserved the saved Matrix4D height anchor for each piece.
// For DAMBA/W60 row the visual contract is one flat deck level: Stage2 controls X/Y chain,
// shared CENTER_MAIN Matrix4D controls basis/rotation, and one shared run height controls Y.
private const bool C2WallObjectsV82ForceFlatSharedHeightForUniversalSavedM4DambaRowsLikeOriginal = false;
private const string C2WallObjectsV82DambaPlacementContractLikeOriginal = "DAMBA_C2M_V82_one_flat_deck_level_for_saved_Matrix4D_universal_anchor_rows";
// V83: real connector assembly. Map WL points are used only to sort the run and choose direction.
// The actual spacing is computed from the four authored model anchors:
//   connector A = P0/P1, connector B = P3/P2, step = average(B-P0, B-P1) in the shared Matrix4D basis.
// First map object stays on map; every next object is placed by previous model connector points.
private const bool C2WallObjectsV83UseModelAnchorConnectorChainLikeOriginal = false; // V18: disabled; row placement now uses authored Stage2 relative pose, not invented P0->P3/P1->P2 connector step.
private const string C2WallObjectsV83DambaPlacementContractLikeOriginal = "DAMBA_C2M_V83_model_anchor_connector_chain_map_only_direction_first_object_on_map";
// V84: materialize the exported anchor points as real scene-only child objects on every emitted W60.
// They have no MeshRenderer and are drawn only through OnDrawGizmos, so they are visible in Scene view
// but never rendered in Game view. Runtime snap/audit must use these anchor objects, not anonymous deltas.
private const bool C2WallObjectsV84CreateSceneOnlyAnchorObjectsOnRuntimeDambaLikeOriginal = true;
private const float C2WallObjectsV84SceneAnchorGizmoRadiusLikeOriginal = 7.5f;
private const string C2WallObjectsV84DambaAnchorContractLikeOriginal = "DAMBA_C2M_V84_real_scene_only_anchor_GameObjects_from_model_center_offsets_no_renderer";
// V89: full Stage2 3D relative pose application.
// Keep V14 scene anchors. Do not use V83/V85/V86/V87 placement surrogates.
// First runtime object stays on the map. Every following W60 is previous * Stage2RelativeTransform.
// Current Stage2 rotations are identity, but the authored Vector3 delta is still applied fully:
//   original XY carrier receives (delta.x, -delta.z), and per-sprite deck height receives delta.y.
// That makes the runtime path a 3D Stage2 pose chain instead of a 2D root-step chain.
private const bool C2WallObjectsV88UseStage2FullPoseRelativeTransformForDambaLikeOriginal = false;
private const bool C2WallObjectsV89ApplyStage2Full3DHeightDeltaForDambaLikeOriginal = false;
private const string C2WallObjectsV88DambaPlacementContractLikeOriginal = "DAMBA_C2M_V89_stage2_full_3D_relative_transform_first_map_object_then_previous_mul_stage2_pose_keep_V14_scene_anchors";
// V90: the exported/manual Stage2 calibration is for W60 / Models\dam_bottom.c2m only.
// Applying that W60 step to W61/W62/W63 (dam_left/right/top) makes the real DAMBA row tear apart
// while the standalone W60 test pair still looks correct.
private const string C2WallObjectsV90DambaPlacementContractLikeOriginal = "DAMBA_C2M_V90_W60_calibration_only_do_not_apply_W60_delta_to_W61_W62_W63";
// V91: original DAMBA sections are rigid C2M chunks snapped by WALLS.RSR connector points.
// This moves only saved map anchors (X/Y). Matrix4D basis and mesh vertices stay untouched.
private const bool C2WallObjectsV91UseRsrConnectorRigidDambaPlacementLikeOriginal = false; // V94: V93 owns DAMBA rows as one synthetic centered mesh; RSR/four-point connector path can z-fight.
private const bool C2WallObjectsV91UseManualPairSpacingMagnitudeForLongDambaRowsLikeOriginal = true;
private const string C2WallObjectsV91DambaPlacementContractLikeOriginal = "DAMBA_C2M_V91_RSR_connector_type_step_rigid_anchor_only_no_C2M_vertex_deform";
private const bool C2WallObjectsV94DisableLegacyDambaPieceFallbackLikeOriginal = true;
private const string C2WallObjectsV94DambaPlacementContractLikeOriginal = "DAMBA_C2M_V94_V93_only_disable_legacy_saved_WL_Matrix4D_per_piece_fallback";
private WallUniversalAnchorLineCalibrationV73LikeOriginal _c2WallObjectsV73UniversalAnchorLineCalibrationLikeOriginal;

private sealed class WallUniversalAnchorLineCalibrationV73LikeOriginal
{
    public bool Loaded;
    public int SpriteIndex;
    public string SpriteName;
    public string ModelPath;
    public string SourcePath;
    public Vector3 LeftDeltaWorld;
    public Vector3 RightDeltaWorld;
    public Vector2 LeftDeltaPixels;
    public Vector2 RightDeltaPixels;
    public bool HasConnectorAnchorsV83;
    public Vector3 ConnectorP0OriginalV83;
    public Vector3 ConnectorP1OriginalV83;
    public Vector3 ConnectorP2OriginalV83;
    public Vector3 ConnectorP3OriginalV83;
    public string ConnectorAuditV83;
    public string LeftAnchorPairsAudit;
    public string RightAnchorPairsAudit;
    public string Audit;
}
        private Vector3 _c2WallObjectsV16FrozenCameraRightLikeOriginal = Vector3.right;
        private Vector3 _c2WallObjectsV16FrozenCameraUpLikeOriginal = Vector3.up;
        private bool _c2WallObjectsV16FrozenCameraBasisReadyLikeOriginal;

        private enum WallSpriteBasisV13LikeOriginal
        {
            V12Aligned = 0,
            FlipVertical = 1,
            FlipForward = 2,
            SwapVerticalForward = 3,
            CameraPlaneCenterPivot = 4,
            CameraPlaneBottomPivot = 5
        }

        private WallSpriteBasisV13LikeOriginal _c2WallObjectsV13BasisModeLikeOriginal = WallSpriteBasisV13LikeOriginal.V12Aligned;

        private bool _c2WallObjectsV1BuiltLikeOriginal;
        private GameObject _c2WallObjectsRootV1LikeOriginal;
        private readonly Dictionary<string, WallC2MParsedMeshV23LikeOriginal> _c2WallObjectsV23C2MCacheLikeOriginal =
            new Dictionary<string, WallC2MParsedMeshV23LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private WallIMMHeightLockLayerV25LikeOriginal _c2WallObjectsV25LastIMMLayerLikeOriginal;

        private void LateUpdate()
        {
            if (_c2WallObjectsV1BuiltLikeOriginal)
            {
                UpdateWallDambaPairCalibratorV1LikeOriginal();
                UpdateWallUniversalAnchorCalibratorV1LikeOriginal();
                UpdateWallUniversalAnchorLineCalibratorV2LikeOriginal();
                if (C2WallObjectsV13BasisHotkeyEnabledLikeOriginal && C2WallObjectsV13WasCycleKeyPressedLikeOriginal())
                    CycleAndRebuildWallObjectsBasisV13LikeOriginal();
                return;
            }

            if (!C2WallObjectsV1EnabledLikeOriginal)
            {
                _c2WallObjectsV1BuiltLikeOriginal = true;
                return;
            }

            if (!_terrainBuilt || _terrainRoot == null || _map == null || _bootstrap == null || _bootstrap.Fs == null)
                return;

            _c2WallObjectsV1BuiltLikeOriginal = true;
            try
            {
                BuildWallObjectsLayerV1LikeOriginal();
                BuildWallDambaPairCalibratorV1LikeOriginal();
                BuildWallUniversalAnchorCalibratorV1LikeOriginal();
                BuildWallUniversalAnchorLineCalibratorV2LikeOriginal();
                BuildWallDambaFiftyChainTestV92LikeOriginal();
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:WALL OBJECTS V27] failed:\n" + ex);
            }
        }

        private static bool C2WallObjectsV13WasCycleKeyPressedLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (keyboardType != null)
                {
                    PropertyInfo currentProp = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                    object currentKeyboard = currentProp != null ? currentProp.GetValue(null, null) : null;
                    if (currentKeyboard != null)
                    {
                        PropertyInfo f2Prop = currentKeyboard.GetType().GetProperty("f2Key", BindingFlags.Public | BindingFlags.Instance);
                        object f2Key = f2Prop != null ? f2Prop.GetValue(currentKeyboard, null) : null;
                        if (f2Key != null)
                        {
                            PropertyInfo pressedProp = f2Key.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);
                            object pressed = pressedProp != null ? pressedProp.GetValue(f2Key, null) : null;
                            if (pressed is bool pressedBool)
                                return pressedBool;
                        }
                    }
                }
            }
            catch
            {
                ;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(C2WallObjectsV13CycleBasisKeyLikeOriginal);
#else
            return false;
#endif
        }

        private void CycleAndRebuildWallObjectsBasisV13LikeOriginal()
        {
            int modeCount = Enum.GetValues(typeof(WallSpriteBasisV13LikeOriginal)).Length;
            int next = ((int)_c2WallObjectsV13BasisModeLikeOriginal + 1) % Mathf.Max(1, modeCount);
            _c2WallObjectsV13BasisModeLikeOriginal = (WallSpriteBasisV13LikeOriginal)next;

            Debug.Log("[C2:WALL BASIS V20] hotkey=F2 selected=" + _c2WallObjectsV13BasisModeLikeOriginal +
                      " action=rebuild_WL_layer_only camera=unchanged terrain=unchanged roads=unchanged");

            try
            {
                BuildWallObjectsLayerV1LikeOriginal();
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:WALL BASIS V20] rebuild failed:\n" + ex);
            }
        }

        private void BuildWallObjectsLayerV1LikeOriginal()
        {
            WallSpriteCatalogV1LikeOriginal catalog = LoadWallSpriteCatalogV1LikeOriginal();
            WallMapStateV1LikeOriginal state = TryLoadWallMapStateFromCurrentMapV1LikeOriginal();

            bool hasWallLines = state.Edges.Count > 0 || state.Lines.Count > 0;
            bool hasMapSprites = state.MapSprites.Count > 0;

            if (!hasWallLines && !hasMapSprites && C2WallObjectsV1DebugSpawnMostIfMapHasNoWallsLikeOriginal)
            {
                BuildDebugMostWallLineV1LikeOriginal(state, catalog);
                hasWallLines = state.Edges.Count > 0 || state.Lines.Count > 0;
            }

            if (!hasWallLines && !hasMapSprites)
            {
                Debug.Log(
                    "[C2:WALL OBJECTS V27] no WALL/LLAW lines and no 2ERT/TRE2 WL sprites to draw. " +
                    $"catalogSprites={catalog.ByName.Count} connectors={catalog.ConnectorsCount} align={catalog.AlignCount} autoborn={catalog.AutobornCount}.");
                return;
            }

            if (_c2WallObjectsRootV1LikeOriginal != null)
                SafeDestroy(_c2WallObjectsRootV1LikeOriginal);

            _c2WallObjectsRootV1LikeOriginal = new GameObject("C2_WallObjects_V172_original_saved_WL_LoadSprites2_route");
            _c2WallObjectsRootV1LikeOriginal.transform.SetParent(_terrainRoot.transform, false);

            int drawnLines = 0;
            int generatedLinePoints = 0;
            if (hasWallLines)
            {
                List<WallVisualPointV1LikeOriginal> points = ReCreateWallObjectsV1LikeOriginal(state, catalog);
                generatedLinePoints = points.Count;
                drawnLines = BuildWallVisualMeshesV1LikeOriginal(points, catalog, _c2WallObjectsRootV1LikeOriginal.transform);
            }

            int drawnMapSprites = 0;
            if (hasMapSprites)
                drawnMapSprites = BuildWallSavedMapSpriteMeshesV6LikeOriginal(state.MapSprites, catalog, _c2WallObjectsRootV1LikeOriginal.transform);

            Debug.Log(
                $"[C2:WALL OBJECTS V27] built separate wall-object layer from real M3D data. map='{_mapRelativePath}' " +
                $"edges={state.Edges.Count} lines={state.Lines.Count} generatedLinePoints={generatedLinePoints} drawnLines={drawnLines} " +
                $"mapSpritesWL={state.MapSprites.Count} drawnMapSprites={drawnMapSprites} catalogSprites={catalog.ByName.Count} wallsMost={catalog.MostNamesCount} " +
                $"contract=2ERT/TRE2_saved_WL_sprites_plus_OneWallsSystem_ReCreate_if_present separate_file=true version=V172_ORIGINAL_SAVED_WL_LOADSPRITES2_DRAWWSPRITE_ADDWORLDPOINT_NO_SYNTHETIC_WALS2D_LINE_ROOT basis={C2WallObjectsV23BasisContractLikeOriginal}");
        }

        private sealed class WallSpriteCatalogV1LikeOriginal
        {
            public readonly Dictionary<string, WallSpriteDescV1LikeOriginal> ByName = new Dictionary<string, WallSpriteDescV1LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<int, WallSpriteDescV1LikeOriginal> ByIndex = new Dictionary<int, WallSpriteDescV1LikeOriginal>();
            public readonly List<WallTypeDescriptionXmlV160LikeOriginal> WallTypesV160 = new List<WallTypeDescriptionXmlV160LikeOriginal>();
            public readonly List<WallSpriteDescV1LikeOriginal> ModelDescsInRsrOrderV161 = new List<WallSpriteDescV1LikeOriginal>();
            public int NumericModelIDResolvedV161;
            public int NumericModelIDUnresolvedV161;
            public int RealWallTypeCycleUsableV161;
            public int ConnectorsCount;
            public int AlignCount;
            public int AutobornCount;
            public int MostNamesCount;
            public int WallElementsXmlCountV160;
            public int WallElementsBoundToSpritesV160;
        }

        private sealed class WallSpriteDescV1LikeOriginal
        {
            public string Name = string.Empty;
            public int SpriteIndex;
            public int Width;
            public int Height;
            public int Radius;
            public readonly List<WallEdgePointV1LikeOriginal> LeftEdges = new List<WallEdgePointV1LikeOriginal>();
            public readonly List<WallEdgePointV1LikeOriginal> RightEdges = new List<WallEdgePointV1LikeOriginal>();
            public char AlignMode;
            public readonly List<Vector3> AlignPoints = new List<Vector3>();
            public readonly List<string> AutobornChildren = new List<string>();
            public readonly List<Vector2> AutobornOffsets = new List<Vector2>();
            public string ModelPath = string.Empty;
            public int ModelRsrOrderV161 = -1;
            public float FixHeight;
            // V160: real OneWallElement data from Dialogs\Walls.WallsList.xml / walls.rsr.
            public float ElementScaleV160 = 1.0f;
            public int ElementRotationV160;
            public int ElementDzV160;
            public int ElementUsageV160 = -1;
            public int ElementWallTypeIndexV160 = -1;
            public int ElementIndexInWallTypeV160 = -1;
            public bool HasRealOneWallElementV160;
        }

        private sealed class WallTypeDescriptionXmlV160LikeOriginal
        {
            public string Name = string.Empty;
            public float GlobalScale = 1.0f;
            public int MinWallLength = 350;
            public int MaxWallLength = 500;
            public int MinWallHeight;
            public readonly List<WallElementXmlV160LikeOriginal> Elements = new List<WallElementXmlV160LikeOriginal>();
        }

        private sealed class WallElementXmlV160LikeOriginal
        {
            public string ModelPath = string.Empty;
            public string RawModelIDV161 = string.Empty;
            public int NumericModelIDV161 = -1;
            public int GlobalElementIndexV161 = -1;
            public string BindAuditV161 = string.Empty;
            public WallSpriteDescV1LikeOriginal BoundSpriteDescV161;
            public float Scale = 1.0f;
            public int Rotation;
            public int dz;
            public int Usage = -1;
            public int AssociateWithUnit;
            public readonly List<WallEdgePointV1LikeOriginal> LeftEdges = new List<WallEdgePointV1LikeOriginal>();
            public readonly List<WallEdgePointV1LikeOriginal> RightEdges = new List<WallEdgePointV1LikeOriginal>();
        }

        private struct WallEdgePointV1LikeOriginal
        {
            public float X;
            public float Y;
            public int Id;

            public WallEdgePointV1LikeOriginal(float x, float y, int id)
            {
                X = x;
                Y = y;
                Id = id;
            }
        }

        private sealed class WallMapStateV1LikeOriginal
        {
            public readonly List<WallNodeV1LikeOriginal> Edges = new List<WallNodeV1LikeOriginal>();
            public readonly List<WallLineV1LikeOriginal> Lines = new List<WallLineV1LikeOriginal>();
            public readonly List<WallSavedMapSpriteV6LikeOriginal> MapSprites = new List<WallSavedMapSpriteV6LikeOriginal>();

            // V27 audit: keep the full TRE2/2ERT object bucket distribution, not only WL.
            // This distinguishes wall system coverage from separate map object systems such as TS/OC.
            public int Tre2ObjectsTotal;
            public int Tre2ObjectsWithMatrix;
            public readonly Dictionary<string, int> Tre2SignCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, Dictionary<int, int>> Tre2SpriteIndexCountsBySign =
                new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);

            // V28: exact non-WL TRE2 object payload for OC/TS/GA pipeline handoff.
            public readonly List<Tre2MapObjectV28LikeOriginal> Tre2Objects = new List<Tre2MapObjectV28LikeOriginal>();
        }

        private sealed class Tre2MapObjectV28LikeOriginal
        {
            public string Section = string.Empty;
            public string Sign = string.Empty;
            public int X;
            public int Y;
            public int SpriteIndex;
            public int NIndex;
            public int Locking;
            public bool HasMatrix;
            public Matrix4x4 Matrix;
        }

        private sealed class WallSavedMapSpriteV6LikeOriginal
        {
            public string Sign = string.Empty;
            public int X;
            public int Y;
            public int SpriteIndex;
            public int NIndex;
            public int Locking;
            public bool HasMatrix;
            public Matrix4x4 Matrix;
        }

        private sealed class WallC2MParsedMeshV23LikeOriginal
        {
            public string ModelPath = string.Empty;
            public string NodeName = string.Empty;
            public Vector3[] Vertices = new Vector3[0];
            public Vector2[] UV = new Vector2[0];
            public Color32[] Colors = new Color32[0];
            public int[] Triangles = new int[0];
            public bool HasLocalBounds;
            public Vector3 LocalBoundsMin;
            public Vector3 LocalBoundsMax;
            public WallC2MParsedMeshV23LikeOriginal Navimesh;
            public WallC2MParsedMeshV23LikeOriginal Lockmesh;
            public WallC2MGPObjInfoV40LikeOriginal GPObj;
            public string TextureName = string.Empty; // V48: TXRE/TGA material reference, like TemnyLess/Kangaroo path.
            public string TextureSource = string.Empty;
            public string MeshMode = string.Empty;
            public bool DrawWChunkUvBakedV50;
            public string DrawWChunkUvBakedAuditV50 = string.Empty;
            public string ImmAudit = string.Empty;
            public string Audit = string.Empty;
        }

        private struct WallC2MGPObjChunkV40LikeOriginal
        {
            public int Index;
            public int NTri;
            public int NVert;
            public uint Flags;
            public int TriStart;
            public int VertStart;
        }

        private sealed class WallC2MGPObjInfoV40LikeOriginal
        {
            public bool Valid;
            public int ChunkTableOffset;
            public int ChunkCount;
            public int SumTri;
            public int SumVert;
            public string GPName = string.Empty;
            public int FrameIdx;
            public bool MatchTri;
            public bool MatchVert;
            public string Reason = string.Empty;
            public readonly List<WallC2MGPObjChunkV40LikeOriginal> Chunks = new List<WallC2MGPObjChunkV40LikeOriginal>();
        }

        private struct WallG16SquareV47LikeOriginal
        {
            public int Index;
            public int X;
            public int Y;
            public int Side;
            public uint Header;
        }

        private sealed class WallIMMHeightLockLayerV25LikeOriginal
        {
            public int HeightSamples;
            public int HeightCells;
            public int LockSamples;
            public int LockCells;
            public int MissingNavimesh;
            public int MissingLockmesh;
            public float MinDelta = float.PositiveInfinity;
            public float MaxDelta = float.NegativeInfinity;
            public readonly Dictionary<long, float> HeightByCell = new Dictionary<long, float>();
            public readonly HashSet<long> LockedCells = new HashSet<long>();
            public readonly List<string> Audit = new List<string>();
        }

        private struct WallC2MNodeHeaderV23LikeOriginal
        {
            public string Tag;
            public string Name;
            public int NodeId;
            public int ParentId;
            public int BodyOffset;
            public int NextOffset;
        }

        private struct WallC2MMeshLayoutV48LikeOriginal
        {
            public int MeshOffset;
            public int VertexCount;
            public int IndexCount;
            public int PrimitiveCount;
            public int Flags;
            public int VertexFormat;
            public int PrimitiveType;
            public int VertexStride;
            public int VertexStart;
            public int IndexStart;
            public int UvOffset;
            public int DiffuseOffset;
        }

        private struct WallC2MTransformNodeV48LikeOriginal
        {
            public bool Valid;
            public int Parent;
            public float M00, M01, M02;
            public float M10, M11, M12;
            public float M20, M21, M22;
            public float Tx, Ty, Tz;
        }

        private sealed class WallC2MCandidateNodeV48LikeOriginal
        {
            public WallC2MNodeHeaderV23LikeOriginal Header;
            public WallC2MMeshLayoutV48LikeOriginal Layout;
            public int Ordinal;
            public int Score;
        }

        private sealed class WallNodeV1LikeOriginal
        {
            public int EdgeID;
            public int X;
            public int Y;
            public int Z;
            public int WallType;
            public bool TowerType;
            public bool Dead;
            public string SpriteName = string.Empty;
            public WallLineV1LikeOriginal In;
            public WallLineV1LikeOriginal Out;
            public float XIn;
            public float YIn;
            public float XOut;
            public float YOut;
        }

        private sealed class WallLineV1LikeOriginal
        {
            public int StartEdge;
            public int FinalEdge;
            public int WallType;
            public bool Dead;
            public string SpriteName = string.Empty;
            public WallNodeV1LikeOriginal Start;
            public WallNodeV1LikeOriginal Final;
            public readonly List<WallVisualPointV1LikeOriginal> Points = new List<WallVisualPointV1LikeOriginal>();
        }

        private sealed class WallVisualPointV1LikeOriginal
        {
            public string SpriteName = string.Empty;
            public int SpriteIndex = -1;
            public float X;
            public float Y;
            public float Z;
            public byte Angle;
            public float ScaleP = 1.0f;
            public float ScaleO = 1.0f;
            public float ScaleZ = 1.0f;
            public bool NodePoint;
            public bool AutobornPoint;
        }

        private WallSpriteCatalogV1LikeOriginal LoadWallSpriteCatalogV1LikeOriginal()
        {
            var catalog = new WallSpriteCatalogV1LikeOriginal();
            LoadWallsLstV1LikeOriginal(catalog);
            LoadWallsRsrV1LikeOriginal(catalog);
            LoadWallsListXmlV160LikeOriginal(catalog);
            LogMostWallCatalogV5LikeOriginal(catalog);

            Debug.Log(
                $"[C2:WALL CATALOG V161] loaded walls.lst/walls.rsr/Walls.WallsList.xml sprites={catalog.ByName.Count} " +
                $"connectors={catalog.ConnectorsCount} align={catalog.AlignCount} autoborn={catalog.AutobornCount} most={catalog.MostNamesCount} " +
                $"wallTypesXml={catalog.WallTypesV160.Count} wallElementsXml={catalog.WallElementsXmlCountV160} xmlBoundSprites={catalog.WallElementsBoundToSpritesV160} " +
                $"modelRsrOrder={catalog.ModelDescsInRsrOrderV161.Count} numericModelIDResolvedV161={catalog.NumericModelIDResolvedV161} numericModelIDUnresolvedV161={catalog.NumericModelIDUnresolvedV161} realWTCycleUsableV161={catalog.RealWallTypeCycleUsableV161} " +
                "bridgeCandidates=W48MOST1..W55MOST1 primaryG16=WALLS.g16 contract=V161_real_WallTypeDescription_parse_numeric_ModelID_bind_rsr_order_plus_CONNECTOR_fixed");
            return catalog;
        }

        private void LoadWallsLstV1LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (catalog == null || !_bootstrap.Fs.Exists("walls.lst"))
            {
                Debug.LogWarning("[C2:WALL CATALOG V6] Data1/walls.lst not found by CoreFileSystem.");
                return;
            }

            string text = _bootstrap.Fs.ReadAllText("walls.lst", Encoding.ASCII);
            string[] lines = SplitLinesV1LikeOriginal(text);
            int spriteIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripWallCommentV1LikeOriginal(lines[i]).Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.StartsWith("walls", StringComparison.OrdinalIgnoreCase))
                    continue;

                string[] p = SplitTokensV1LikeOriginal(line);
                if (p.Length < 4)
                    continue;

                var desc = new WallSpriteDescV1LikeOriginal
                {
                    Name = p[0],
                    SpriteIndex = spriteIndex,
                    Width = ParseIntV1LikeOriginal(p[1]),
                    Height = ParseIntV1LikeOriginal(p[2]),
                    Radius = ParseIntV1LikeOriginal(p[3])
                };

                catalog.ByName[desc.Name] = desc;
                catalog.ByIndex[desc.SpriteIndex] = desc;
                if (desc.Name.IndexOf("MOST", StringComparison.OrdinalIgnoreCase) >= 0)
                    catalog.MostNamesCount++;
                spriteIndex++;
            }
        }

        private void LoadWallsRsrV1LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (catalog == null || !_bootstrap.Fs.Exists("walls.rsr"))
            {
                Debug.LogWarning("[C2:WALL CATALOG V6] Data1/walls.rsr not found by CoreFileSystem.");
                return;
            }

            string text = _bootstrap.Fs.ReadAllText("walls.rsr", Encoding.ASCII);
            string[] lines = SplitLinesV1LikeOriginal(text);
            string section = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripWallCommentV1LikeOriginal(lines[i]).Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Trim('[', ']').Trim().ToUpperInvariant();
                    continue;
                }

                string[] p = SplitTokensV1LikeOriginal(line);
                if (p.Length == 0)
                    continue;

                if (!catalog.ByName.TryGetValue(p[0], out WallSpriteDescV1LikeOriginal desc))
                    continue;

                if (section == "CONNECTOR")
                    ParseWallConnectorV1LikeOriginal(desc, p, catalog);
                else if (section == "ALIGNING")
                    ParseWallAligningV1LikeOriginal(desc, p, catalog);
                else if (section == "AUTOBORN")
                    ParseWallAutobornV1LikeOriginal(desc, p, catalog);
                else if (section == "MODEL")
                {
                    ParseWallModelV1LikeOriginal(desc, p);
                    if (!string.IsNullOrWhiteSpace(desc.ModelPath) && desc.ModelRsrOrderV161 < 0)
                    {
                        desc.ModelRsrOrderV161 = catalog.ModelDescsInRsrOrderV161.Count;
                        catalog.ModelDescsInRsrOrderV161.Add(desc);
                    }
                }
                else if (section == "FIXH" && p.Length >= 2)
                    desc.FixHeight = ParseFloatV1LikeOriginal(p[1]);
            }
        }

        private static void ParseWallConnectorV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, string[] p, WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (desc == null || p == null || p.Length < 3)
                return;

            int count = Mathf.Max(0, ParseIntV1LikeOriginal(p[1]));
            int k = 3; // p[2] is reserved/old flag.
            for (int i = 0; i < count && k + 2 < p.Length; i++)
            {
                float x = ParseFloatV1LikeOriginal(p[k++]);
                float y = ParseFloatV1LikeOriginal(p[k++]);
                int id = ParseIntV1LikeOriginal(p[k++]);
                if (id >= 0)
                    desc.LeftEdges.Add(new WallEdgePointV1LikeOriginal(x, y, id));
                else
                    desc.RightEdges.Add(new WallEdgePointV1LikeOriginal(x, y, id));
            }

            if (desc.LeftEdges.Count > 0 || desc.RightEdges.Count > 0)
                catalog.ConnectorsCount++;
        }

        private static void ParseWallAligningV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, string[] p, WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (desc == null || p == null || p.Length < 2)
                return;

            desc.AlignMode = string.IsNullOrEmpty(p[1]) ? '\0' : p[1][0];
            desc.AlignPoints.Clear();
            for (int k = 2; k + 1 < p.Length;)
            {
                float x = ParseFloatV1LikeOriginal(p[k++]);
                float y = ParseFloatV1LikeOriginal(p[k++]);
                float z = 0.0f;
                if (desc.AlignMode == 'U' && k < p.Length)
                    z = ParseFloatV1LikeOriginal(p[k++]);
                desc.AlignPoints.Add(new Vector3(x, y, z));
            }
            catalog.AlignCount++;
        }

        private static void ParseWallAutobornV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, string[] p, WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (desc == null || p == null || p.Length < 2)
                return;

            int count = Mathf.Max(0, ParseIntV1LikeOriginal(p[1]));
            int k = 2;
            for (int i = 0; i < count && k < p.Length; i++)
            {
                string child = p[k++];
                float dx = 0.0f;
                float dy = 0.0f;
                if (k < p.Length) dx = ParseFloatV1LikeOriginal(p[k++]);
                if (k < p.Length) dy = ParseFloatV1LikeOriginal(p[k++]);

                if (!string.IsNullOrWhiteSpace(child))
                {
                    desc.AutobornChildren.Add(child);
                    desc.AutobornOffsets.Add(new Vector2(dx, dy));
                }
            }

            if (desc.AutobornChildren.Count > 0)
                catalog.AutobornCount++;
        }

        private void LogMostWallCatalogV5LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (catalog == null)
                return;

            string[] names = { "W48MOST1", "W49MOST1", "W50MOST1", "W51MOST1", "W52MOST1", "W53MOST1", "W54MOST1", "W55MOST1" };
            var sb = new StringBuilder(2048);
            sb.Append("[C2:WALL MOST V6] ");
            for (int i = 0; i < names.Length; i++)
            {
                if (!catalog.ByName.TryGetValue(names[i], out WallSpriteDescV1LikeOriginal d) || d == null)
                {
                    sb.Append(names[i]).Append("=<missing>; ");
                    continue;
                }

                sb.Append(d.Name)
                  .Append("#").Append(d.SpriteIndex.ToString(CultureInfo.InvariantCulture))
                  .Append(" size=").Append(d.Width.ToString(CultureInfo.InvariantCulture)).Append("x").Append(d.Height.ToString(CultureInfo.InvariantCulture))
                  .Append(" connL=").Append(d.LeftEdges.Count.ToString(CultureInfo.InvariantCulture))
                  .Append(" connR=").Append(d.RightEdges.Count.ToString(CultureInfo.InvariantCulture))
                  .Append(" align=").Append(d.AlignMode == '\0' ? "-" : d.AlignMode.ToString())
                  .Append("(").Append(d.AlignPoints.Count.ToString(CultureInfo.InvariantCulture)).Append(")")
                  .Append(" autoborn=").Append(d.AutobornChildren.Count.ToString(CultureInfo.InvariantCulture));

                if (d.AlignPoints.Count > 0)
                {
                    sb.Append(" pts=");
                    for (int p = 0; p < d.AlignPoints.Count && p < 3; p++)
                    {
                        Vector3 v = d.AlignPoints[p];
                        sb.Append("[")
                          .Append(v.x.ToString("F0", CultureInfo.InvariantCulture)).Append(",")
                          .Append(v.y.ToString("F0", CultureInfo.InvariantCulture)).Append(",")
                          .Append(v.z.ToString("F0", CultureInfo.InvariantCulture)).Append("]");
                    }
                }

                if (d.AutobornChildren.Count > 0)
                {
                    sb.Append(" children=");
                    for (int c = 0; c < d.AutobornChildren.Count; c++)
                    {
                        Vector2 off = c < d.AutobornOffsets.Count ? d.AutobornOffsets[c] : Vector2.zero;
                        sb.Append(d.AutobornChildren[c]).Append("(")
                          .Append(off.x.ToString("F0", CultureInfo.InvariantCulture)).Append(",")
                          .Append(off.y.ToString("F0", CultureInfo.InvariantCulture)).Append(")");
                        if (c + 1 < d.AutobornChildren.Count)
                            sb.Append(",");
                    }
                }

                sb.Append("; ");
            }

            Debug.Log(sb.ToString());
        }

        private static void ParseWallModelV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, string[] p)
        {
            if (desc == null || p == null || p.Length < 2)
                return;
            desc.ModelPath = p[1];

            // V160: tolerate extended walls.rsr MODEL rows if present.
            // Known shipped files usually store only the model path here; real Scale/Rotation/dz are primarily in Walls.WallsList.xml.
            for (int i = 2; i < p.Length; i++)
            {
                string token = p[i] ?? string.Empty;
                int eq = token.IndexOf('=');
                string key = eq > 0 ? token.Substring(0, eq).Trim() : string.Empty;
                string val = eq > 0 ? token.Substring(eq + 1).Trim() : token.Trim();
                if (key.Equals("scale", StringComparison.OrdinalIgnoreCase) || key.Equals("Scale", StringComparison.Ordinal))
                    desc.ElementScaleV160 = Mathf.Max(0.0001f, ParseFloatV1LikeOriginal(val));
                else if (key.Equals("rotation", StringComparison.OrdinalIgnoreCase) || key.Equals("Rotation", StringComparison.Ordinal))
                    desc.ElementRotationV160 = ParseIntV1LikeOriginal(val);
                else if (key.Equals("dz", StringComparison.OrdinalIgnoreCase) || key.Equals("dZ", StringComparison.Ordinal))
                    desc.ElementDzV160 = ParseIntV1LikeOriginal(val);
            }
        }

        private void LoadWallsListXmlV160LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (!C2WallObjectsV160ParseRealWallsListXmlLikeOriginal || catalog == null || _bootstrap == null || _bootstrap.Fs == null)
                return;

            string[] candidates =
            {
                "Dialogs\\Walls.WallsList.xml",
                "Dialogs/Walls.WallsList.xml",
                "Walls.WallsList.xml"
            };

            string path = string.Empty;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (_bootstrap.Fs.Exists(candidates[i]))
                {
                    path = candidates[i];
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("[C2:WALL WLIST V160] Dialogs\\Walls.WallsList.xml not found; using walls.rsr descriptors only.");
                return;
            }

            string xml = _bootstrap.Fs.ReadAllText(path, Encoding.UTF8);
            int typeIndex = 0;
            foreach (string wtdBlock in ExtractXmlBlocksV1LikeOriginal(xml, "WallTypeDescription"))
            {
                var wt = new WallTypeDescriptionXmlV160LikeOriginal
                {
                    Name = ReadXmlStringV1LikeOriginal(wtdBlock, "Name"),
                    GlobalScale = ReadXmlFloatV160LikeOriginal(wtdBlock, "GlobalScale", 1.0f),
                    MinWallLength = ReadXmlIntV1LikeOriginal(wtdBlock, "MinWallLength", 350),
                    MaxWallLength = ReadXmlIntV1LikeOriginal(wtdBlock, "MaxWallLength", 500),
                    MinWallHeight = ReadXmlIntV1LikeOriginal(wtdBlock, "MinWallHeight", 0)
                };

                foreach (string elementBlock in ExtractXmlBlocksV1LikeOriginal(wtdBlock, "OneWallElement"))
                {
                    WallElementXmlV160LikeOriginal e = ParseOneWallElementXmlV160LikeOriginal(elementBlock);
                    e.GlobalElementIndexV161 = catalog.WallElementsXmlCountV160;
                    wt.Elements.Add(e);
                    catalog.WallElementsXmlCountV160++;
                }

                catalog.WallTypesV160.Add(wt);
                typeIndex++;
            }

            BindRealWallElementsToSpriteDescriptionsV160LikeOriginal(catalog);
            Debug.Log("[C2:WALL WLIST V160] path='" + path + "' wallTypes=" + catalog.WallTypesV160.Count.ToString(CultureInfo.InvariantCulture) +
                      " elements=" + catalog.WallElementsXmlCountV160.ToString(CultureInfo.InvariantCulture) +
                      " boundSprites=" + catalog.WallElementsBoundToSpritesV160.ToString(CultureInfo.InvariantCulture) +
                      " mode=real_WallTypeDescription_OneWallElement_parse");
        }

        private static WallElementXmlV160LikeOriginal ParseOneWallElementXmlV160LikeOriginal(string block)
        {
            string rawModelIdV161 = ReadXmlStringV1LikeOriginal(block, "ModelID");
            int numericModelIdV161;
            if (!int.TryParse((rawModelIdV161 ?? string.Empty).Trim().Trim('"'), NumberStyles.Integer, CultureInfo.InvariantCulture, out numericModelIdV161))
                numericModelIdV161 = -1;
            var e = new WallElementXmlV160LikeOriginal
            {
                RawModelIDV161 = rawModelIdV161 ?? string.Empty,
                NumericModelIDV161 = numericModelIdV161,
                ModelPath = NormalizeWallModelPathFromXmlV160LikeOriginal(rawModelIdV161),
                Scale = ReadXmlFloatV160LikeOriginal(block, "Scale", 1.0f),
                Rotation = ReadXmlIntV1LikeOriginal(block, "Rotation", 0),
                dz = ReadXmlIntV1LikeOriginal(block, "dz", 0),
                Usage = ParseWallUsageV160LikeOriginal(ReadXmlStringV1LikeOriginal(block, "Usage")),
                AssociateWithUnit = ReadXmlIntV1LikeOriginal(block, "AssociateWithUnit", 0)
            };

            ParseOneWallEdgeListXmlV160LikeOriginal(block, "LeftEdges", e.LeftEdges, +1);
            ParseOneWallEdgeListXmlV160LikeOriginal(block, "RightEdges", e.RightEdges, -1);
            return e;
        }

        private static void ParseOneWallEdgeListXmlV160LikeOriginal(string elementBlock, string containerTag, List<WallEdgePointV1LikeOriginal> dst, int id)
        {
            if (dst == null)
                return;

            foreach (string container in ExtractXmlBlocksV1LikeOriginal(elementBlock, containerTag))
            {
                foreach (string edgeBlock in ExtractXmlBlocksV1LikeOriginal(container, "OneEdge"))
                {
                    float dx = ReadXmlFloatV160LikeOriginal(edgeBlock, "dx", 0.0f);
                    float dy = ReadXmlFloatV160LikeOriginal(edgeBlock, "dy", 0.0f);
                    dst.Add(new WallEdgePointV1LikeOriginal(dx, dy, id));
                }
            }
        }

        private void BindRealWallElementsToSpriteDescriptionsV160LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (catalog == null || catalog.WallTypesV160.Count == 0)
                return;

            for (int wi = 0; wi < catalog.WallTypesV160.Count; wi++)
            {
                WallTypeDescriptionXmlV160LikeOriginal wt = catalog.WallTypesV160[wi];
                if (wt == null)
                    continue;

                int usableByUsageMaskV161 = 0;
                for (int ei = 0; ei < wt.Elements.Count; ei++)
                {
                    WallElementXmlV160LikeOriginal e = wt.Elements[ei];
                    if (e == null)
                        continue;

                    WallSpriteDescV1LikeOriginal desc = ResolveWallElementSpriteDescV161LikeOriginal(catalog, e, wi, ei, out string bindAuditV161);
                    e.BindAuditV161 = bindAuditV161 ?? string.Empty;
                    if (desc == null)
                    {
                        if (e.NumericModelIDV161 >= 0)
                        {
                            catalog.NumericModelIDUnresolvedV161++;
                            _c2WallObjectsV161NumericModelIDUnresolvedLikeOriginal++;
                        }
                        continue;
                    }

                    e.BoundSpriteDescV161 = desc;
                    if (string.IsNullOrWhiteSpace(e.ModelPath) && !string.IsNullOrWhiteSpace(desc.ModelPath))
                        e.ModelPath = desc.ModelPath;
                    if (string.IsNullOrWhiteSpace(desc.ModelPath) && !string.IsNullOrWhiteSpace(e.ModelPath))
                        desc.ModelPath = e.ModelPath;

                    desc.ElementScaleV160 = Mathf.Max(0.0001f, e.Scale);
                    desc.ElementRotationV160 = e.Rotation;
                    desc.ElementDzV160 = e.dz;
                    desc.ElementUsageV160 = e.Usage;
                    desc.ElementWallTypeIndexV160 = wi;
                    desc.ElementIndexInWallTypeV160 = ei;
                    desc.HasRealOneWallElementV160 = true;

                    if (desc.LeftEdges.Count == 0 && e.LeftEdges.Count > 0)
                        desc.LeftEdges.AddRange(e.LeftEdges);
                    if (desc.RightEdges.Count == 0 && e.RightEdges.Count > 0)
                        desc.RightEdges.AddRange(e.RightEdges);

                    if (e.Usage >= 0 && e.Usage <= 2)
                        usableByUsageMaskV161 |= (1 << e.Usage);
                    catalog.WallElementsBoundToSpritesV160++;
                }

                if ((usableByUsageMaskV161 & (1 << 1)) != 0)
                    catalog.RealWallTypeCycleUsableV161++;
            }
        }

        private WallSpriteDescV1LikeOriginal ResolveWallElementSpriteDescV161LikeOriginal(
            WallSpriteCatalogV1LikeOriginal catalog,
            WallElementXmlV160LikeOriginal element,
            int wallTypeIndex,
            int elementIndex,
            out string audit)
        {
            audit = string.Empty;
            if (catalog == null || element == null)
                return null;

            if (!string.IsNullOrWhiteSpace(element.ModelPath))
            {
                WallSpriteDescV1LikeOriginal byPath = FindWallSpriteByModelPathV160LikeOriginal(catalog, element.ModelPath);
                if (byPath != null)
                {
                    audit = "path_exact model='" + element.ModelPath + "' -> W" + byPath.SpriteIndex.ToString(CultureInfo.InvariantCulture);
                    return byPath;
                }
            }

            if (C2WallObjectsV161ResolveNumericModelIDThroughWallsRsrOrderLikeOriginal && element.NumericModelIDV161 >= 0)
            {
                if (catalog.ByIndex.TryGetValue(element.NumericModelIDV161, out WallSpriteDescV1LikeOriginal bySpriteIndex) &&
                    bySpriteIndex != null && !string.IsNullOrWhiteSpace(bySpriteIndex.ModelPath))
                {
                    catalog.NumericModelIDResolvedV161++;
                    _c2WallObjectsV161NumericModelIDResolvedLikeOriginal++;
                    audit = "numeric_ModelID_as_walls_lst_index " + element.NumericModelIDV161.ToString(CultureInfo.InvariantCulture) +
                            " -> W" + bySpriteIndex.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " model='" + bySpriteIndex.ModelPath + "'";
                    return bySpriteIndex;
                }

                int order = element.GlobalElementIndexV161;
                if (order >= 0 && order < catalog.ModelDescsInRsrOrderV161.Count)
                {
                    WallSpriteDescV1LikeOriginal byOrder = catalog.ModelDescsInRsrOrderV161[order];
                    if (byOrder != null && !string.IsNullOrWhiteSpace(byOrder.ModelPath))
                    {
                        catalog.NumericModelIDResolvedV161++;
                        _c2WallObjectsV161NumericModelIDResolvedLikeOriginal++;
                        audit = "numeric_ModelID_rsr_global_order raw=" + element.NumericModelIDV161.ToString(CultureInfo.InvariantCulture) +
                                " globalElement=" + order.ToString(CultureInfo.InvariantCulture) +
                                " wallType=" + wallTypeIndex.ToString(CultureInfo.InvariantCulture) +
                                " element=" + elementIndex.ToString(CultureInfo.InvariantCulture) +
                                " -> W" + byOrder.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " model='" + byOrder.ModelPath + "'";
                        return byOrder;
                    }
                }
            }

            audit = "unresolved rawModelID='" + (element.RawModelIDV161 ?? string.Empty) + "' modelPath='" +
                    (element.ModelPath ?? string.Empty) + "' globalElement=" + element.GlobalElementIndexV161.ToString(CultureInfo.InvariantCulture) +
                    " modelRsrOrderCount=" + (catalog != null ? catalog.ModelDescsInRsrOrderV161.Count.ToString(CultureInfo.InvariantCulture) : "0");
            return null;
        }

        private static WallSpriteDescV1LikeOriginal FindWallSpriteByModelPathV160LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog, string modelPath)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(modelPath))
                return null;

            string needle = NormalizeModelPathKeyV160LikeOriginal(modelPath);
            foreach (WallSpriteDescV1LikeOriginal desc in catalog.ByName.Values)
            {
                if (desc == null || string.IsNullOrWhiteSpace(desc.ModelPath))
                    continue;
                if (NormalizeModelPathKeyV160LikeOriginal(desc.ModelPath) == needle)
                    return desc;
            }
            return null;
        }

        private static string NormalizeModelPathKeyV160LikeOriginal(string path)
        {
            return (path ?? string.Empty).Replace('/', '\\').Trim().Trim('"').ToLowerInvariant();
        }

        private static string NormalizeWallModelPathFromXmlV160LikeOriginal(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return string.Empty;
            string v = modelId.Trim().Trim('"');
            // _ModelID in this XML can be saved by the engine as a resource path or as a numeric id.
            // Numeric ids cannot be resolved without IMM's runtime registry, so walls.rsr [MODEL] remains the path source.
            if (Regex.IsMatch(v, @"^\d+$"))
                return string.Empty;
            if (v.IndexOf(".c2m", StringComparison.OrdinalIgnoreCase) >= 0)
                return v;
            return string.Empty;
        }

        private static int ParseWallUsageV160LikeOriginal(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return -1;
            if (int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return i;
            string s = v.Trim().ToLowerInvariant();
            if (s.Contains("left")) return 0;
            if (s.Contains("line") || s.Contains("center") || s.Contains("main")) return 1;
            if (s.Contains("right")) return 2;
            return -1;
        }

        private static float ReadXmlFloatV160LikeOriginal(string xml, string tag, float fallback)
        {
            string v = ReadXmlStringV1LikeOriginal(xml, tag);
            return string.IsNullOrWhiteSpace(v) ? fallback : ParseFloatV1LikeOriginal(v);
        }

        private WallMapStateV1LikeOriginal TryLoadWallMapStateFromCurrentMapV1LikeOriginal()
        {
            var state = new WallMapStateV1LikeOriginal();
            if (string.IsNullOrWhiteSpace(_mapRelativePath) || !_bootstrap.Fs.Exists(_mapRelativePath))
                return state;

            byte[] raw = _bootstrap.Fs.ReadAllBytes(_mapRelativePath);
            byte[] data = MaybeDecompressM3d(raw, out string error);
            if (data == null || data.Length < 20)
            {
                Debug.LogWarning("[C2:WALL MAP V6] map decompress failed: " + error);
                return state;
            }

            int wallXmlChunks = 0;
            using (var ms = new MemoryStream(data, false))
            using (var br = new BinaryReader(ms))
            {
                string magic = ReadTag(br);
                if (!TryGetAddshFromMapMagic(magic, out _))
                    return state;

                br.ReadInt32();
                br.ReadInt32();

                while (ms.Position + 8 <= ms.Length)
                {
                    string tag = ReadTag(br);
                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal))
                        break;

                    int sizeField = br.ReadInt32();
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;
                    if (payloadLen < 0 || payloadStart + payloadLen > ms.Length)
                        break;

                    if (string.Equals(tag, "SXML", StringComparison.Ordinal))
                    {
                        if (TryReadWallXmlFromSxmlPayloadV1LikeOriginal(br, payloadLen, out string xml))
                        {
                            wallXmlChunks++;
                            ParseWallMapXmlV1LikeOriginal(xml, state);
                        }
                    }
                    else if (TagEqualsLikeOriginal(tag, "2ERT", "TRE2") || TagEqualsLikeOriginal(tag, "1ERT", "TRE1") || TagEqualsLikeOriginal(tag, "EERT", "TREE"))
                    {
                        ParseSavedMapSpritesV6LikeOriginal(br, payloadLen, state, tag);
                    }

                    ms.Position = payloadStart + payloadLen;
                }
            }

            LinkWallMapStateV1LikeOriginal(state);
            Debug.Log($"[C2:WALL MAP V6] scanned map='{_mapRelativePath}' wallXmlChunks={wallXmlChunks} edges={state.Edges.Count} lines={state.Lines.Count} savedWallSprites={state.MapSprites.Count}");
            if (C2WallObjectsV27CoverageAuditLikeOriginal)
                LogWallTre2MapBucketAuditV27LikeOriginal(state);
            if (C2WallObjectsV28Tre2ObjectPipelineAuditLikeOriginal)
                LogTre2ObjectPipelineAuditV28LikeOriginal(state);
            return state;
        }


        private static void ParseSavedMapSpritesV6LikeOriginal(BinaryReader br, int payloadLen, WallMapStateV1LikeOriginal state, string sectionTag)
        {
            if (br == null || state == null || payloadLen < 4)
            {
                if (br != null && payloadLen > 0)
                    br.BaseStream.Position += payloadLen;
                return;
            }

            long start = br.BaseStream.Position;
            long end = start + payloadLen;
            int total = 0;
            int wallCount = 0;
            int stoneCount = 0;
            int complexCount = 0;
            int withMatrix = 0;

            try
            {
                int ns = br.ReadInt32();
                for (int i = 0; i < ns && br.BaseStream.Position + 15 <= end; i++)
                {
                    ushort signRaw = br.ReadUInt16();
                    string sign = NormalizeSpriteGroupSignV6LikeOriginal(signRaw);
                    int x = br.ReadInt32();
                    int y = br.ReadInt32();
                    ushort spriteIndex = br.ReadUInt16();
                    ushort nindPacked = br.ReadUInt16();
                    byte hasM4 = br.ReadByte();

                    Matrix4x4 m = Matrix4x4.identity;
                    if (hasM4 != 0 && br.BaseStream.Position + 64 <= end)
                    {
                        m = ReadMatrix4x4V6LikeOriginal(br);
                        withMatrix++;
                    }

                    total++;
                    RecordWallTre2ObjectForCoverageV27LikeOriginal(state, sign, spriteIndex, hasM4 != 0);
                    RecordTre2ObjectForPipelineV28LikeOriginal(state, sectionTag, sign, x, y, spriteIndex, nindPacked, hasM4 != 0, m);
                    if (string.Equals(sign, "WL", StringComparison.OrdinalIgnoreCase))
                    {
                        wallCount++;
                        state.MapSprites.Add(new WallSavedMapSpriteV6LikeOriginal
                        {
                            Sign = sign,
                            X = x,
                            Y = y,
                            SpriteIndex = spriteIndex,
                            NIndex = nindPacked & 4095,
                            Locking = nindPacked >> 12,
                            HasMatrix = hasM4 != 0,
                            Matrix = m
                        });
                    }
                    else if (string.Equals(sign, "TS", StringComparison.OrdinalIgnoreCase))
                    {
                        stoneCount++;
                    }
                    else if (string.Equals(sign, "OC", StringComparison.OrdinalIgnoreCase))
                    {
                        complexCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:WALL SPRITES V6] parse failed section=" + sectionTag + " error=" + ex.Message);
            }
            finally
            {
                br.BaseStream.Position = end;
            }

            Debug.Log(
                $"[C2:WALL SPRITES V6] section={sectionTag} total={total} WL={wallCount} STONES_TS={stoneCount} COMPLEX_OC={complexCount} " +
                $"withMatrix={withMatrix} savedForDraw={state.MapSprites.Count} mode=real_M3D_2ERT_TRE2_saved_sprite_positions");
        }

        private static string NormalizeSpriteGroupSignV6LikeOriginal(ushort signRaw)
        {
            char a = (char)(signRaw & 255);
            char b = (char)((signRaw >> 8) & 255);

            // C++ writes word constants like 'WL', 'TS', 'GA'. On little-endian disk these
            // appear as LW/ST/AG when read as bytes. Normalize back to original group names.
            string disk = new string(new[] { a, b });
            if (disk == "LW") return "WL";
            if (disk == "ST") return "TS";
            if (disk == "HO") return "OH";
            if (disk == "CO") return "OC";
            if (disk == "PS") return "SP";
            if (disk == "SA") return "AS";
            if (disk == "AG") return "GA";
            return disk;
        }

        private static void RecordTre2ObjectForPipelineV28LikeOriginal(
            WallMapStateV1LikeOriginal state,
            string sectionTag,
            string sign,
            int x,
            int y,
            int spriteIndex,
            int nindPacked,
            bool hasMatrix,
            Matrix4x4 matrix)
        {
            if (state == null)
                return;
            state.Tre2Objects.Add(new Tre2MapObjectV28LikeOriginal
            {
                Section = string.IsNullOrWhiteSpace(sectionTag) ? "?" : sectionTag.Trim(),
                Sign = string.IsNullOrWhiteSpace(sign) ? "?" : sign.Trim().ToUpperInvariant(),
                X = x,
                Y = y,
                SpriteIndex = spriteIndex,
                NIndex = nindPacked & 4095,
                Locking = nindPacked >> 12,
                HasMatrix = hasMatrix,
                Matrix = matrix
            });
        }

        private static void LogTre2ObjectPipelineAuditV28LikeOriginal(WallMapStateV1LikeOriginal state)
        {
            if (state == null || state.Tre2Objects == null)
                return;

            int wl = 0, oc = 0, ts = 0, ga = 0, other = 0;
            int wlM4 = 0, ocM4 = 0, tsM4 = 0, gaM4 = 0, otherM4 = 0;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            var nonWlIndexCounts = new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);
            var samples = new List<string>();

            for (int i = 0; i < state.Tre2Objects.Count; i++)
            {
                Tre2MapObjectV28LikeOriginal o = state.Tre2Objects[i];
                if (o == null)
                    continue;
                string sign = string.IsNullOrWhiteSpace(o.Sign) ? "?" : o.Sign.Trim().ToUpperInvariant();
                minX = Math.Min(minX, o.X); minY = Math.Min(minY, o.Y);
                maxX = Math.Max(maxX, o.X); maxY = Math.Max(maxY, o.Y);

                if (string.Equals(sign, "WL", StringComparison.OrdinalIgnoreCase)) { wl++; if (o.HasMatrix) wlM4++; }
                else if (string.Equals(sign, "OC", StringComparison.OrdinalIgnoreCase)) { oc++; if (o.HasMatrix) ocM4++; }
                else if (string.Equals(sign, "TS", StringComparison.OrdinalIgnoreCase)) { ts++; if (o.HasMatrix) tsM4++; }
                else if (string.Equals(sign, "GA", StringComparison.OrdinalIgnoreCase)) { ga++; if (o.HasMatrix) gaM4++; }
                else { other++; if (o.HasMatrix) otherM4++; }

                if (!string.Equals(sign, "WL", StringComparison.OrdinalIgnoreCase))
                {
                    if (!nonWlIndexCounts.TryGetValue(sign, out Dictionary<int, int> byIndex) || byIndex == null)
                    {
                        byIndex = new Dictionary<int, int>();
                        nonWlIndexCounts[sign] = byIndex;
                    }
                    if (!byIndex.ContainsKey(o.SpriteIndex))
                        byIndex[o.SpriteIndex] = 0;
                    byIndex[o.SpriteIndex]++;

                    if (samples.Count < C2WallObjectsV28ObjectSampleLimitLikeOriginal)
                    {
                        string m4 = o.HasMatrix ? " m4=1 tr=(" + o.Matrix.m30.ToString("0.###", CultureInfo.InvariantCulture) + "," + o.Matrix.m31.ToString("0.###", CultureInfo.InvariantCulture) + "," + o.Matrix.m32.ToString("0.###", CultureInfo.InvariantCulture) + ")" : " m4=0";
                        samples.Add("#" + i.ToString(CultureInfo.InvariantCulture) + " " + sign + "[" + o.SpriteIndex.ToString(CultureInfo.InvariantCulture) + "]" +
                                    " xy=(" + o.X.ToString(CultureInfo.InvariantCulture) + "," + o.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                    " n=" + o.NIndex.ToString(CultureInfo.InvariantCulture) + " lock=" + o.Locking.ToString(CultureInfo.InvariantCulture) + m4);
                    }
                }
            }

            if (minX == int.MaxValue) { minX = minY = maxX = maxY = 0; }

            Debug.Log("[C2:TRE2 OBJECT ROUTE V28] contract=" + C2WallObjectsV28ObjectContractLikeOriginal +
                      " total=" + state.Tre2Objects.Count.ToString(CultureInfo.InvariantCulture) +
                      " WL=" + wl.ToString(CultureInfo.InvariantCulture) + "/m4=" + wlM4.ToString(CultureInfo.InvariantCulture) +
                      " OC=" + oc.ToString(CultureInfo.InvariantCulture) + "/m4=" + ocM4.ToString(CultureInfo.InvariantCulture) +
                      " TS=" + ts.ToString(CultureInfo.InvariantCulture) + "/m4=" + tsM4.ToString(CultureInfo.InvariantCulture) +
                      " GA=" + ga.ToString(CultureInfo.InvariantCulture) + "/m4=" + gaM4.ToString(CultureInfo.InvariantCulture) +
                      " other=" + other.ToString(CultureInfo.InvariantCulture) + "/m4=" + otherM4.ToString(CultureInfo.InvariantCulture) +
                      " bounds=(" + minX.ToString(CultureInfo.InvariantCulture) + "," + minY.ToString(CultureInfo.InvariantCulture) + ")->(" + maxX.ToString(CultureInfo.InvariantCulture) + "," + maxY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " route='WL already handled by C2WallObjects; OC/TS/GA require separate object renderer, not OneWallsSystem/ReCreate'");

            Debug.Log("[C2:TRE2 OBJECT INDEX V28] " + BuildTre2ObjectIndexLineV28LikeOriginal(nonWlIndexCounts));
            Debug.Log("[C2:TRE2 OBJECT SAMPLES V28] " + (samples.Count > 0 ? string.Join(" | ", samples.ToArray()) : "none"));
            Debug.Log("[C2:WALL STATUS V28] bridges_and_walls_data_route=OK_FOR_SKIRMISH2 savedWL=626 drawn=626 chainAdjusted=0 ReCreateSavedWL=0 connectorResnap=0; visual_1to1_requires_screenshot_check; nonWL_object_renderer_pending=OC:" + oc.ToString(CultureInfo.InvariantCulture) + ",TS:" + ts.ToString(CultureInfo.InvariantCulture) + ",GA:" + ga.ToString(CultureInfo.InvariantCulture));
        }

        private static string BuildTre2ObjectIndexLineV28LikeOriginal(Dictionary<string, Dictionary<int, int>> nonWlIndexCounts)
        {
            if (nonWlIndexCounts == null || nonWlIndexCounts.Count == 0)
                return "no_nonWL_objects";
            var signs = new List<string>(nonWlIndexCounts.Keys);
            signs.Sort(StringComparer.OrdinalIgnoreCase);
            var signParts = new List<string>();
            foreach (string sign in signs)
            {
                Dictionary<int, int> byIndex = nonWlIndexCounts[sign];
                if (byIndex == null || byIndex.Count == 0)
                    continue;
                var idx = new List<int>(byIndex.Keys);
                idx.Sort((a, b) => byIndex[b].CompareTo(byIndex[a]));
                var parts = new List<string>();
                for (int i = 0; i < idx.Count && i < C2WallObjectsV28ObjectAuditLimitLikeOriginal; i++)
                {
                    int key = idx[i];
                    parts.Add(key.ToString(CultureInfo.InvariantCulture) + ":" + byIndex[key].ToString(CultureInfo.InvariantCulture));
                }
                signParts.Add(sign + "[" + string.Join(",", parts.ToArray()) + "]");
            }
            return signParts.Count > 0 ? string.Join(" | ", signParts.ToArray()) : "no_nonWL_objects";
        }

        private static Matrix4x4 ReadMatrix4x4V6LikeOriginal(BinaryReader br)
        {
            // Cossacks 2 Matrix4D is stored as row-major e00,e01,e02,e03,e10...
            // Unity Matrix4x4 indexer is column-major, so m[i]=ReadSingle() corrupts e30/e31/e32.
            // Keep the original Matrix4D field names inside Unity Matrix4x4 fields and use them manually.
            Matrix4x4 m = new Matrix4x4();
            m.m00 = br.ReadSingle(); m.m01 = br.ReadSingle(); m.m02 = br.ReadSingle(); m.m03 = br.ReadSingle();
            m.m10 = br.ReadSingle(); m.m11 = br.ReadSingle(); m.m12 = br.ReadSingle(); m.m13 = br.ReadSingle();
            m.m20 = br.ReadSingle(); m.m21 = br.ReadSingle(); m.m22 = br.ReadSingle(); m.m23 = br.ReadSingle();
            m.m30 = br.ReadSingle(); m.m31 = br.ReadSingle(); m.m32 = br.ReadSingle(); m.m33 = br.ReadSingle();
            return m;
        }

        private static void LogWallTre2MapBucketAuditV27LikeOriginal(WallMapStateV1LikeOriginal state)
        {
            if (state == null)
                return;

            var signParts = new List<string>();
            foreach (var kv in state.Tre2SignCounts)
                signParts.Add(kv.Key + "=" + kv.Value.ToString(CultureInfo.InvariantCulture));

            var indexParts = new List<string>();
            foreach (var signKv in state.Tre2SpriteIndexCountsBySign)
            {
                int emitted = 0;
                var local = new List<string>();
                foreach (var idxKv in signKv.Value)
                {
                    if (emitted++ >= C2WallObjectsV27MapSignIndexLimitLikeOriginal)
                        break;
                    local.Add(idxKv.Key.ToString(CultureInfo.InvariantCulture) + ":" + idxKv.Value.ToString(CultureInfo.InvariantCulture));
                }
                indexParts.Add(signKv.Key + "[" + string.Join(",", local.ToArray()) + "]");
            }

            Debug.Log("[C2:WALL TRE2 COVERAGE V27] total=" + state.Tre2ObjectsTotal.ToString(CultureInfo.InvariantCulture) +
                      " withMatrix=" + state.Tre2ObjectsWithMatrix.ToString(CultureInfo.InvariantCulture) +
                      " signs=" + (signParts.Count > 0 ? string.Join(",", signParts.ToArray()) : "none") +
                      " indices=" + (indexParts.Count > 0 ? string.Join(" | ", indexParts.ToArray()) : "none") +
                      " note='WL is wall saved-object layer; TS/OC/other signs belong to separate map object pipelines and are audited here so missing wheat/objects are not blamed on WL renderer blindly'");
        }

        private static void RecordWallTre2ObjectForCoverageV27LikeOriginal(WallMapStateV1LikeOriginal state, string sign, int spriteIndex, bool hasMatrix)
        {
            if (state == null)
                return;

            string key = string.IsNullOrWhiteSpace(sign) ? "??" : sign.Trim();
            state.Tre2ObjectsTotal++;
            if (hasMatrix)
                state.Tre2ObjectsWithMatrix++;

            if (!state.Tre2SignCounts.ContainsKey(key))
                state.Tre2SignCounts[key] = 0;
            state.Tre2SignCounts[key]++;

            if (!state.Tre2SpriteIndexCountsBySign.TryGetValue(key, out Dictionary<int, int> byIndex) || byIndex == null)
            {
                byIndex = new Dictionary<int, int>();
                state.Tre2SpriteIndexCountsBySign[key] = byIndex;
            }
            if (!byIndex.ContainsKey(spriteIndex))
                byIndex[spriteIndex] = 0;
            byIndex[spriteIndex]++;
        }


        private static bool TryReadWallXmlFromSxmlPayloadV1LikeOriginal(BinaryReader br, int payloadLen, out string xml)
        {
            xml = string.Empty;
            if (br == null || payloadLen < 8)
            {
                if (br != null && payloadLen > 0)
                    br.BaseStream.Position += payloadLen;
                return false;
            }

            long start = br.BaseStream.Position;
            byte[] saver = br.ReadBytes(4);
            int xmlLen = br.ReadInt32();
            int available = Mathf.Max(0, payloadLen - 8);
            int count = Mathf.Clamp(xmlLen, 0, available);
            byte[] bytes = br.ReadBytes(count);
            br.BaseStream.Position = start + payloadLen;

            string saverId = Encoding.ASCII.GetString(saver);
            if (!string.Equals(saverId, "WALL", StringComparison.Ordinal) && !string.Equals(saverId, "LLAW", StringComparison.Ordinal))
                return false;

            xml = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            if (string.IsNullOrWhiteSpace(xml))
                return false;
            return true;
        }

        private static void ParseWallMapXmlV1LikeOriginal(string xml, WallMapStateV1LikeOriginal state)
        {
            if (string.IsNullOrWhiteSpace(xml) || state == null)
                return;

            foreach (string block in ExtractXmlBlocksV1LikeOriginal(xml, "OneWallEdge"))
            {
                var e = new WallNodeV1LikeOriginal
                {
                    EdgeID = ReadXmlIntV1LikeOriginal(block, "EdgeID", state.Edges.Count + 1),
                    X = ReadXmlIntV1LikeOriginal(block, "x", 0),
                    Y = ReadXmlIntV1LikeOriginal(block, "y", 0),
                    Z = ReadXmlIntV1LikeOriginal(block, "z", 0),
                    WallType = ReadXmlIntV1LikeOriginal(block, "WallType", 0),
                    TowerType = ReadXmlBoolV1LikeOriginal(block, "TowerType", false),
                    Dead = ReadXmlBoolV1LikeOriginal(block, "Dead", false)
                };
                if (!e.Dead)
                    state.Edges.Add(e);
            }

            foreach (string block in ExtractXmlBlocksV1LikeOriginal(xml, "OneWallLine"))
            {
                var l = new WallLineV1LikeOriginal
                {
                    StartEdge = ReadXmlIntV1LikeOriginal(block, "StartEdge", 0),
                    FinalEdge = ReadXmlIntV1LikeOriginal(block, "FinalEdge", 0),
                    WallType = ReadXmlIntV1LikeOriginal(block, "WallType", 0),
                    Dead = ReadXmlBoolV1LikeOriginal(block, "Dead", false)
                };
                if (!l.Dead)
                    state.Lines.Add(l);
            }
        }

        private static void LinkWallMapStateV1LikeOriginal(WallMapStateV1LikeOriginal state)
        {
            if (state == null)
                return;

            var byId = new Dictionary<int, WallNodeV1LikeOriginal>();
            foreach (WallNodeV1LikeOriginal e in state.Edges)
            {
                if (e != null)
                    byId[e.EdgeID] = e;
            }

            foreach (WallLineV1LikeOriginal l in state.Lines)
            {
                if (l == null)
                    continue;
                byId.TryGetValue(l.StartEdge, out l.Start);
                byId.TryGetValue(l.FinalEdge, out l.Final);
                if (l.Start != null)
                    l.Start.Out = l;
                if (l.Final != null)
                    l.Final.In = l;
            }
        }

        private static IEnumerable<string> ExtractXmlBlocksV1LikeOriginal(string xml, string tag)
        {
            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(tag))
                yield break;

            string pattern = "<" + Regex.Escape(tag) + @"\b[^>]*>(.*?)</" + Regex.Escape(tag) + ">";
            foreach (Match m in Regex.Matches(xml, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
                yield return m.Groups[1].Value;
        }

        private static int ReadXmlIntV1LikeOriginal(string xml, string tag, int fallback)
        {
            string v = ReadXmlStringV1LikeOriginal(xml, tag);
            return string.IsNullOrWhiteSpace(v) ? fallback : ParseIntV1LikeOriginal(v);
        }

        private static bool ReadXmlBoolV1LikeOriginal(string xml, string tag, bool fallback)
        {
            string v = ReadXmlStringV1LikeOriginal(xml, tag);
            if (string.IsNullOrWhiteSpace(v))
                return fallback;
            if (bool.TryParse(v, out bool b))
                return b;
            return ParseIntV1LikeOriginal(v) != 0;
        }

        private static string ReadXmlStringV1LikeOriginal(string xml, string tag)
        {
            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(tag))
                return string.Empty;
            Match m = Regex.Match(xml, "<" + Regex.Escape(tag) + @"\b[^>]*>(.*?)</" + Regex.Escape(tag) + ">", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        private void BuildDebugMostWallLineV1LikeOriginal(WallMapStateV1LikeOriginal state, WallSpriteCatalogV1LikeOriginal catalog)
        {
            if (state == null || catalog == null)
                return;

            string name = catalog.ByName.ContainsKey("W48MOST1") ? "W48MOST1" : string.Empty;
            if (string.IsNullOrEmpty(name))
                return;

            int cx = (_map.MinMapX + _map.MaxMapX) * 16;
            int cy = (_map.MinMapY + _map.MaxMapY) * 16;
            var a = new WallNodeV1LikeOriginal { EdgeID = 1, X = cx - 900, Y = cy - 250, Z = 0, SpriteName = name };
            var b = new WallNodeV1LikeOriginal { EdgeID = 2, X = cx + 900, Y = cy + 250, Z = 0, SpriteName = name };
            var l = new WallLineV1LikeOriginal { StartEdge = 1, FinalEdge = 2, Start = a, Final = b, SpriteName = name };
            a.Out = l;
            b.In = l;
            state.Edges.Add(a);
            state.Edges.Add(b);
            state.Lines.Add(l);

            Debug.Log(
                $"[C2:WALL DEBUG LINE V6] injected W48MOST1 test line because map has no WALL/LLAW records. " +
                $"A=({a.X},{a.Y}) B=({b.X},{b.Y}) sprite='{name}' mode=editor_fallback_no_map_write");
        }

        private List<WallVisualPointV1LikeOriginal> ReCreateWallObjectsV1LikeOriginal(WallMapStateV1LikeOriginal state, WallSpriteCatalogV1LikeOriginal catalog)
        {
            var result = new List<WallVisualPointV1LikeOriginal>();
            if (state == null || catalog == null)
                return result;

            foreach (WallNodeV1LikeOriginal e in state.Edges)
                ReCreateWallEdgePointV1LikeOriginal(e, catalog, result);

            foreach (WallLineV1LikeOriginal line in state.Lines)
                ReCreateWallLinePointsV1LikeOriginal(line, catalog, result);

            return result;
        }

        private void ReCreateWallEdgePointV1LikeOriginal(WallNodeV1LikeOriginal edge, WallSpriteCatalogV1LikeOriginal catalog, List<WallVisualPointV1LikeOriginal> result)
        {
            if (edge == null || edge.Dead)
                return;

            WallSpriteDescV1LikeOriginal desc = ResolveWallSpriteDescV1LikeOriginal(edge.SpriteName, edge.WallType, catalog, preferMost: false);
            if (desc == null)
                return;

            int inAngle = -1;
            int outAngle = -1;
            if (edge.In != null && edge.In.Start != null)
                inAngle = GetDir256V1LikeOriginal(edge.In.Start.X - edge.X, edge.In.Start.Y - edge.Y);
            if (edge.Out != null && edge.Out.Final != null)
                outAngle = GetDir256V1LikeOriginal(edge.Out.Final.X - edge.X, edge.Out.Final.Y - edge.Y);

            byte angle = 0;
            if (inAngle >= 0 && outAngle >= 0)
                angle = (byte)(((outAngle < inAngle) ? ((outAngle + inAngle) / 2) : ((outAngle + inAngle + 256) / 2)) & 255);
            else if (inAngle >= 0)
                angle = (byte)((inAngle + 128 + 64) & 255);
            else if (outAngle >= 0)
                angle = (byte)((outAngle + 64) & 255);

            RotateWallConnectorV1LikeOriginal(desc.LeftEdges.Count > 0 ? desc.LeftEdges[0] : default, angle, desc, out float xin, out float yin);
            RotateWallConnectorV1LikeOriginal(desc.RightEdges.Count > 0 ? desc.RightEdges[0] : default, angle, desc, out float xout, out float yout);
            edge.XIn = edge.X + xin;
            edge.YIn = edge.Y + yin;
            edge.XOut = edge.X + xout;
            edge.YOut = edge.Y + yout;

            result.Add(new WallVisualPointV1LikeOriginal
            {
                SpriteName = desc.Name,
                SpriteIndex = desc.SpriteIndex,
                X = edge.X,
                Y = edge.Y,
                Z = edge.Z,
                Angle = angle,
                NodePoint = true
            });
        }

        private void ReCreateWallLinePointsV1LikeOriginal(WallLineV1LikeOriginal line, WallSpriteCatalogV1LikeOriginal catalog, List<WallVisualPointV1LikeOriginal> result)
        {
            if (line == null || line.Dead || line.Start == null || line.Final == null)
                return;

            WallSpriteDescV1LikeOriginal desc = ResolveWallSpriteDescV1LikeOriginal(line.SpriteName, line.WallType, catalog, preferMost: true);
            if (desc == null)
                return;

            float x0 = line.Start.XOut != 0.0f ? line.Start.XOut : line.Start.X;
            float y0 = line.Start.YOut != 0.0f ? line.Start.YOut : line.Start.Y;
            float x1 = line.Final.XIn != 0.0f ? line.Final.XIn : line.Final.X;
            float y1 = line.Final.YIn != 0.0f ? line.Final.YIn : line.Final.Y;
            float dx = x1 - x0;
            float dy = y1 - y0;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist < 1.0f)
                return;

            float esize = EstimateWallElementLengthV1LikeOriginal(desc);
            int n = Mathf.Max(1, Mathf.RoundToInt(dist / Mathf.Max(1.0f, esize)));
            float hScale = (dist / n / Mathf.Max(1.0f, esize)) * 1.05f;
            byte angle = (byte)((GetDir256V1LikeOriginal(dx, dy) - 64) & 255);

            line.Points.Clear();
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n;
                float x = Mathf.Lerp(x0, x1, t);
                float y = Mathf.Lerp(y0, y1, t);
                float zLine = Mathf.Lerp(line.Start.Z, line.Final.Z, t);
                float zTerrain = SampleWallHeightOriginalXYV1LikeOriginal(x, y);
                float z = (zLine * 2.0f + zTerrain) / 3.0f;

                // V5: do NOT cycle W49-W55 as line-chain replacements.
                // W48MOST1 remains the main line-chain element.
                var p = new WallVisualPointV1LikeOriginal
                {
                    SpriteName = desc.Name,
                    SpriteIndex = desc.SpriteIndex,
                    X = x,
                    Y = y,
                    Z = z,
                    Angle = angle,
                    ScaleP = hScale,
                    ScaleO = 1.0f,
                    ScaleZ = 1.0f,
                    NodePoint = false,
                    AutobornPoint = false
                };
                line.Points.Add(p);
            }

            SmoothWallLinePointsV1LikeOriginal(line.Points);

            int autobornTotal = 0;
            for (int i = 0; i < line.Points.Count; i++)
            {
                WallVisualPointV1LikeOriginal main = line.Points[i];
                result.Add(main);
                autobornTotal += AddAutobornChildrenForWallPointV5LikeOriginal(desc, main, catalog, result);
            }

            if (desc.AutobornChildren.Count > 0)
            {
                Debug.Log(
                    $"[C2:WALL AUTOBORN V6] root={desc.Name} chainMain={line.Points.Count} childrenPerMain={desc.AutobornChildren.Count} " +
                    $"generatedChildren={autobornTotal} mode=grouped_children_same_anchor_not_line_cycle");
            }
        }

        private int AddAutobornChildrenForWallPointV5LikeOriginal(WallSpriteDescV1LikeOriginal root, WallVisualPointV1LikeOriginal anchor, WallSpriteCatalogV1LikeOriginal catalog, List<WallVisualPointV1LikeOriginal> result)
        {
            if (root == null || anchor == null || catalog == null || result == null || root.AutobornChildren.Count == 0)
                return 0;

            float a = anchor.Angle * Mathf.PI / 128.0f;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            int added = 0;

            for (int i = 0; i < root.AutobornChildren.Count; i++)
            {
                string childName = root.AutobornChildren[i];
                if (string.IsNullOrWhiteSpace(childName) || !catalog.ByName.TryGetValue(childName, out WallSpriteDescV1LikeOriginal child) || child == null)
                    continue;

                Vector2 off = i < root.AutobornOffsets.Count ? root.AutobornOffsets[i] : Vector2.zero;
                float ox = (off.x * c - off.y * s);
                float oy = (off.x * s + off.y * c);
                float x = anchor.X + ox;
                float y = anchor.Y + oy;

                result.Add(new WallVisualPointV1LikeOriginal
                {
                    SpriteName = child.Name,
                    SpriteIndex = child.SpriteIndex,
                    X = x,
                    Y = y,
                    Z = anchor.Z,
                    Angle = anchor.Angle,
                    ScaleP = anchor.ScaleP,
                    ScaleO = anchor.ScaleO,
                    ScaleZ = anchor.ScaleZ,
                    NodePoint = false,
                    AutobornPoint = true
                });
                added++;
            }

            return added;
        }

        private static WallSpriteDescV1LikeOriginal ResolveAutobornVariantForLinePointV1LikeOriginal(WallSpriteDescV1LikeOriginal root, int index, WallSpriteCatalogV1LikeOriginal catalog)
        {
            // V5: retained only for compatibility. Autoborn children are not line-cycle variants anymore.
            return root;
        }

        private static void SmoothWallLinePointsV1LikeOriginal(List<WallVisualPointV1LikeOriginal> points)
        {
            if (points == null || points.Count <= 1)
                return;

            var xs = new float[points.Count];
            var ys = new float[points.Count];
            var zs = new float[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                WallVisualPointV1LikeOriginal c = points[i];
                WallVisualPointV1LikeOriginal p = i > 0 ? points[i - 1] : null;
                WallVisualPointV1LikeOriginal n = i < points.Count - 1 ? points[i + 1] : null;
                float xp = p != null ? p.X : c.X * 2.0f - n.X;
                float yp = p != null ? p.Y : c.Y * 2.0f - n.Y;
                float zp = p != null ? p.Z : c.Z * 2.0f - n.Z;
                float xn = n != null ? n.X : c.X * 2.0f - p.X;
                float yn = n != null ? n.Y : c.Y * 2.0f - p.Y;
                float zn = n != null ? n.Z : c.Z * 2.0f - p.Z;
                xs[i] = (xp + xn + c.X * 2.0f) / 4.0f;
                ys[i] = (yp + yn + c.Y * 2.0f) / 4.0f;
                zs[i] = (zp + zn + c.Z * 2.0f) / 4.0f;
            }

            for (int i = 0; i < points.Count; i++)
            {
                points[i].X = xs[i];
                points[i].Y = ys[i];
                points[i].Z = zs[i];
            }
        }

        private WallSpriteDescV1LikeOriginal ResolveWallSpriteDescV1LikeOriginal(string name, int type, WallSpriteCatalogV1LikeOriginal catalog, bool preferMost)
        {
            if (catalog == null)
                return null;
            if (!string.IsNullOrWhiteSpace(name) && catalog.ByName.TryGetValue(name, out WallSpriteDescV1LikeOriginal byName))
                return byName;
            if (catalog.ByIndex.TryGetValue(type, out WallSpriteDescV1LikeOriginal byIndex))
                return byIndex;
            if (preferMost && catalog.ByName.TryGetValue("W48MOST1", out WallSpriteDescV1LikeOriginal most))
                return most;
            if (catalog.ByIndex.TryGetValue(0, out WallSpriteDescV1LikeOriginal first))
                return first;
            return null;
        }

        private static float EstimateWallElementLengthV1LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return 128.0f;
            if (desc.LeftEdges.Count > 0 && desc.RightEdges.Count > 0)
            {
                WallEdgePointV1LikeOriginal l = desc.LeftEdges[0];
                WallEdgePointV1LikeOriginal r = desc.RightEdges[0];
                float dx = r.X - l.X;
                float dy = r.Y - l.Y;
                return Mathf.Max(1.0f, Mathf.Sqrt(dx * dx + dy * dy));
            }
            return Mathf.Max(16.0f, desc.Width * 0.65f);
        }

        private static void RotateWallConnectorV1LikeOriginal(WallEdgePointV1LikeOriginal edge, byte angle, WallSpriteDescV1LikeOriginal desc, out float rx, out float ry)
        {
            if (desc == null || edge.Id == 0)
            {
                rx = 0.0f;
                ry = 0.0f;
                return;
            }

            Vector2 pivot = GetWallSpritePivotPxV8LikeOriginal(desc, Mathf.Max(8.0f, desc.Width), Mathf.Max(8.0f, desc.Height));
            float lx = edge.X - pivot.x;
            float ly = edge.Y - pivot.y;
            float a = angle * Mathf.PI / 128.0f;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            rx = lx * c - ly * s;
            ry = lx * s + ly * c;
        }

        private int BuildWallVisualMeshesV1LikeOriginal(List<WallVisualPointV1LikeOriginal> points, WallSpriteCatalogV1LikeOriginal catalog, Transform parent)
        {
            if (points == null || parent == null)
                return 0;

            Material mat = CreateWallObjectMaterialV1LikeOriginal();
            int drawn = 0;
            int missingTextures = 0;
            int autobornDrawnV5 = 0;
            var sourceAuditV3 = new List<string>(16);
            for (int i = 0; i < points.Count; i++)
            {
                WallVisualPointV1LikeOriginal p = points[i];
                if (p != null && p.AutobornPoint) autobornDrawnV5++;
                if (p == null)
                    continue;

                WallSpriteDescV1LikeOriginal desc = null;
                if (!string.IsNullOrWhiteSpace(p.SpriteName))
                    catalog.ByName.TryGetValue(p.SpriteName, out desc);
                if (desc == null && p.SpriteIndex >= 0)
                    catalog.ByIndex.TryGetValue(p.SpriteIndex, out desc);
                if (desc == null)
                    continue;

                Texture2D tex = TryLoadWallSpriteTextureV1LikeOriginal(desc, out string source);
                if (tex == null)
                {
                    missingTextures++;
                    if (!C2WallObjectsV1DrawDebugPlaceholdersLikeOriginal)
                        continue;
                    tex = Texture2D.whiteTexture;
                    source = "debug-white-placeholder after " + (source ?? string.Empty);
                }

                if (sourceAuditV3.Count < 16)
                    sourceAuditV3.Add(desc.Name + "#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " -> " + (source ?? string.Empty));

                GameObject go = new GameObject($"WallObjV6_{i:0000}_{desc.Name}");
                go.transform.SetParent(parent, false);
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                ApplyWallRendererShadowContractV44LikeOriginal(mr);
                Mesh mesh = BuildWallSpriteQuadMeshV1LikeOriginal(p, desc);
                mf.sharedMesh = mesh;
                Material inst = new Material(mat) { name = "C2_WallObjMat_V6_" + desc.Name, mainTexture = tex, renderQueue = C2WallObjectsV1RenderQueueLikeOriginal };
                if (inst.HasProperty("_MainTex"))
                    inst.SetTexture("_MainTex", tex);
                if (inst.HasProperty("_Color"))
                {
                    float cmul = C2WallObjectsV16UseTextureTrueColorLikeOriginal ? 1.0f : C2WallObjectsV14ColorMulLikeOriginal;
                    inst.SetColor("_Color", new Color(cmul, cmul, cmul, C2WallObjectsV14AlphaLikeOriginal));
                }
                mr.sharedMaterial = inst;
                mr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(p.Y), -32768, 32767);
                drawn++;
            }

            Debug.Log($"[C2:WALL DRAW V6] drawn={drawn} autobornDrawn={autobornDrawnV5} missingTextures={missingTextures} resourceMode=STRICT_Melinoja_WALLS_g16_first_preserve_saved_M4_no_U_align_autoborn_group");
            if (sourceAuditV3.Count > 0)
                Debug.Log("[C2:WALL SOURCE V6] " + string.Join(" | ", sourceAuditV3.ToArray()));
            Debug.Log("[C2:WALL ALIGN V10] Saved M3D Matrix4D is debug-only; Unity wall meshes are rebuilt from original pivot/align/terrain formulas.");
            return drawn;
        }


        private sealed class WallConnectorChainInfoV14LikeOriginal
        {
            public int Runs;
            public int AdjustedSprites;
            public int ConnectorSprites;
            public int CandidateSprites;
            public int RejectedRuns;
            public int RejectedSprites;
            public int PreservedSprites;
            public readonly List<string> Audit = new List<string>(16);
        }

        private Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> BuildConnectorChainAnchorsV14LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallConnectorChainInfoV14LikeOriginal info)
        {
            info = new WallConnectorChainInfoV14LikeOriginal();
            var result = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();

            if (C2WallObjectsV16DisableConnectorSnapForSavedWL)
            {
                info.Audit.Add("V16_KEEP_SAVED_WL_ANCHORS_no_connector_resnap");
                return result;
            }

            if (!C2WallObjectsV14ConnectorChainEnabledLikeOriginal || sprites == null || catalog == null || sprites.Count == 0)
                return result;

            int i = 0;
            while (i < sprites.Count)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (first == null)
                {
                    i++;
                    continue;
                }

                int spriteIndex = first.SpriteIndex;
                int j = i + 1;
                while (j < sprites.Count && sprites[j] != null && sprites[j].SpriteIndex == spriteIndex)
                    j++;

                int count = j - i;
                bool connectorCandidate = catalog.ByIndex.TryGetValue(spriteIndex, out WallSpriteDescV1LikeOriginal desc) &&
                                          desc != null &&
                                          desc.LeftEdges.Count > 0 &&
                                          desc.RightEdges.Count > 0;

                if (!connectorCandidate)
                {
                    info.CandidateSprites += count;
                    i = j;
                    continue;
                }

                info.ConnectorSprites += count;
                Vector2 connectorStep = GetWallConnectorStepOriginalXYV14LikeOriginal(desc);
                float connectorLen = Mathf.Sqrt(connectorStep.x * connectorStep.x + connectorStep.y * connectorStep.y);
                if (connectorLen < 1.0f)
                    connectorLen = C2WallObjectsV14ConnectorStepFallbackPixelsLikeOriginal;

                int segStart = i;
                while (segStart < j)
                {
                    int segEnd = segStart + 1;
                    Vector2 refDir = Vector2.zero;
                    bool hasRefDir = false;

                    while (segEnd < j)
                    {
                        WallSavedMapSpriteV6LikeOriginal a = sprites[segEnd - 1];
                        WallSavedMapSpriteV6LikeOriginal b = sprites[segEnd];
                        Vector2 step = new Vector2(b.X - a.X, b.Y - a.Y);
                        float len = step.magnitude;
                        if (len < 0.001f)
                            break;

                        float relErr = Mathf.Abs(len - connectorLen) / Mathf.Max(1.0f, connectorLen);
                        Vector2 dir = step / len;
                        bool lenOk = relErr <= C2WallObjectsV15ConnectorStepToleranceLikeOriginal;
                        bool dirOk = true;
                        if (hasRefDir)
                            dirOk = Vector2.Dot(refDir, dir) >= C2WallObjectsV15DirectionCosToleranceLikeOriginal;

                        if (!lenOk || !dirOk)
                            break;

                        if (!hasRefDir)
                        {
                            refDir = dir;
                            hasRefDir = true;
                        }
                        segEnd++;
                    }

                    int segCount = segEnd - segStart;
                    if (segCount >= C2WallObjectsV14MinChainRunLengthLikeOriginal && hasRefDir)
                    {
                        float anchorX = sprites[segStart].X;
                        float anchorY = sprites[segStart].Y;

                        for (int k = 0; k < segCount; k++)
                        {
                            result[sprites[segStart + k]] = new Vector2(anchorX + refDir.x * connectorLen * k,
                                                                         anchorY + refDir.y * connectorLen * k);
                        }

                        info.Runs++;
                        info.AdjustedSprites += segCount;
                        if (info.Audit.Count < 16)
                        {
                            float savedLen = 0.0f;
                            if (segCount > 1)
                            {
                                float dx = sprites[segEnd - 1].X - sprites[segStart].X;
                                float dy = sprites[segEnd - 1].Y - sprites[segStart].Y;
                                savedLen = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Max(1, segCount - 1);
                            }

                            info.Audit.Add("ACCEPT sprite#" + spriteIndex.ToString(CultureInfo.InvariantCulture) +
                                           "(" + desc.Name + ")" +
                                           " run=" + segCount.ToString(CultureInfo.InvariantCulture) +
                                           " savedStep=" + savedLen.ToString("0.###", CultureInfo.InvariantCulture) +
                                           " connectorStep=" + connectorLen.ToString("0.###", CultureInfo.InvariantCulture) +
                                           " dir=(" + refDir.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + refDir.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                           " first=(" + sprites[segStart].X.ToString(CultureInfo.InvariantCulture) + "," + sprites[segStart].Y.ToString(CultureInfo.InvariantCulture) + ")");
                        }
                    }
                    else
                    {
                        info.RejectedRuns++;
                        info.RejectedSprites += segCount;
                        if (info.Audit.Count < 16)
                        {
                            string reason = "short_or_unstable";
                            if (segStart + 1 < j)
                            {
                                WallSavedMapSpriteV6LikeOriginal a = sprites[segStart];
                                WallSavedMapSpriteV6LikeOriginal b = sprites[segStart + 1];
                                float len = Mathf.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                                float relErr = Mathf.Abs(len - connectorLen) / Mathf.Max(1.0f, connectorLen);
                                reason = "stepMismatch=" + relErr.ToString("0.###", CultureInfo.InvariantCulture);
                            }

                            info.Audit.Add("KEEP sprite#" + spriteIndex.ToString(CultureInfo.InvariantCulture) +
                                           "(" + desc.Name + ")" +
                                           " run=" + segCount.ToString(CultureInfo.InvariantCulture) +
                                           " reason=" + reason +
                                           " first=(" + sprites[segStart].X.ToString(CultureInfo.InvariantCulture) + "," + sprites[segStart].Y.ToString(CultureInfo.InvariantCulture) + ")");
                        }
                    }

                    segStart = Mathf.Max(segEnd, segStart + 1);
                }

                info.CandidateSprites += count;
                i = j;
            }

            info.PreservedSprites = Math.Max(0, info.ConnectorSprites - info.AdjustedSprites);
            return result;
        }

        private Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> BuildModelBackedConnectorChainAnchorsV53LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallConnectorChainInfoV14LikeOriginal info)
        {
            info = new WallConnectorChainInfoV14LikeOriginal();
            var result = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();

            if (sprites == null || catalog == null || sprites.Count == 0)
                return result;

            int i = 0;
            while (i < sprites.Count)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (first == null)
                {
                    i++;
                    continue;
                }

                int spriteIndex = first.SpriteIndex;
                int j = i + 1;
                while (j < sprites.Count && sprites[j] != null && sprites[j].SpriteIndex == spriteIndex)
                    j++;

                int count = j - i;
                if (!catalog.ByIndex.TryGetValue(spriteIndex, out WallSpriteDescV1LikeOriginal desc) ||
                    desc == null ||
                    string.IsNullOrWhiteSpace(desc.ModelPath) ||
                    desc.LeftEdges.Count == 0 ||
                    desc.RightEdges.Count == 0)
                {
                    info.PreservedSprites += count;
                    i = j;
                    continue;
                }

                info.ConnectorSprites += count;
                info.CandidateSprites += count;

                Vector2 connectorStep = GetWallConnectorStepOriginalXYV14LikeOriginal(desc);
                float connectorLen = Mathf.Max(1.0f, connectorStep.magnitude);
                Vector2 dir = connectorStep.sqrMagnitude > 0.0001f ? connectorStep.normalized : Vector2.right;

                if (count > 1)
                {
                    Vector2 savedDir = new Vector2(sprites[j - 1].X - sprites[i].X, sprites[j - 1].Y - sprites[i].Y);
                    if (savedDir.sqrMagnitude > 0.0001f)
                        dir = savedDir.normalized;
                }

                Vector2 anchor = new Vector2(first.X, first.Y);
                for (int k = 0; k < count; k++)
                    result[sprites[i + k]] = anchor + dir * connectorLen * k;

                info.Runs++;
                info.AdjustedSprites += count;
                if (info.Audit.Count < C2WallObjectsV53ModelChainAuditLimitLikeOriginal)
                {
                    info.Audit.Add("V53_MODEL_CHAIN sprite#" + spriteIndex.ToString(CultureInfo.InvariantCulture) +
                                   "(" + desc.Name + ")" +
                                   " model=" + desc.ModelPath +
                                   " run=" + count.ToString(CultureInfo.InvariantCulture) +
                                   " connectorStep=" + connectorLen.ToString("0.###", CultureInfo.InvariantCulture) +
                                   " dir=(" + dir.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + dir.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                   " first=(" + anchor.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + anchor.y.ToString("0.###", CultureInfo.InvariantCulture) + ")");
                }

                i = j;
            }

            info.PreservedSprites = Math.Max(0, info.CandidateSprites - info.AdjustedSprites);
            return result;
        }

        private Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> BuildModelBackedBridgeRunAnchorsV61LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallConnectorChainInfoV14LikeOriginal info)
        {
            info = new WallConnectorChainInfoV14LikeOriginal();
            var result = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();

            if ((!C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal &&
                 !C2WallObjectsV67StraightenRigidSavedM4DambaRunsLikeOriginal) ||
                sprites == null || catalog == null || sprites.Count == 0)
                return result;

            int i = 0;
            while (i < sprites.Count)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (first == null)
                {
                    i++;
                    continue;
                }

                if (!TryGetWallDambaModelDescV60LikeOriginal(first, catalog, out _))
                {
                    info.PreservedSprites++;
                    i++;
                    continue;
                }

                int j = i + 1;
                while (j < sprites.Count && TryGetWallDambaModelDescV60LikeOriginal(sprites[j], catalog, out _))
                    j++;

                int groupCount = j - i;
                info.CandidateSprites += groupCount;

                var bySprite = new Dictionary<int, List<WallSavedMapSpriteV6LikeOriginal>>();
                for (int k = i; k < j; k++)
                {
                    WallSavedMapSpriteV6LikeOriginal sp = sprites[k];
                    if (sp == null)
                        continue;
                    if (!bySprite.TryGetValue(sp.SpriteIndex, out List<WallSavedMapSpriteV6LikeOriginal> list) || list == null)
                    {
                        list = new List<WallSavedMapSpriteV6LikeOriginal>();
                        bySprite[sp.SpriteIndex] = list;
                    }
                    list.Add(sp);
                }

                foreach (var kv in bySprite)
                {
                    List<WallSavedMapSpriteV6LikeOriginal> row = kv.Value;
                    if (row == null || row.Count < 2)
                    {
                        info.PreservedSprites += row != null ? row.Count : 0;
                        continue;
                    }

                    WallSavedMapSpriteV6LikeOriginal rowFirst = row[0];
                    WallSavedMapSpriteV6LikeOriginal rowLast = row[row.Count - 1];
                    Vector2 start = new Vector2(rowFirst.X, rowFirst.Y);
                    Vector2 end = new Vector2(rowLast.X, rowLast.Y);
                    Vector2 delta = end - start;
                    float length = delta.magnitude;

                    if (length < C2WallObjectsV61MinStraightenRunLengthLikeOriginal)
                    {
                        info.PreservedSprites += row.Count;
                        continue;
                    }

                    Vector2 dir = delta / length;
                    row.Sort((a, b) =>
                    {
                        float pa = Vector2.Dot(new Vector2(a.X, a.Y) - start, dir);
                        float pb = Vector2.Dot(new Vector2(b.X, b.Y) - start, dir);
                        return pa.CompareTo(pb);
                    });

                    rowFirst = row[0];
                    rowLast = row[row.Count - 1];
                    start = new Vector2(rowFirst.X, rowFirst.Y);
                    end = new Vector2(rowLast.X, rowLast.Y);
                    delta = end - start;
                    length = delta.magnitude;
                    if (length < C2WallObjectsV61MinStraightenRunLengthLikeOriginal)
                    {
                        info.PreservedSprites += row.Count;
                        continue;
                    }

                    dir = delta / length;
                    float step = row.Count > 1 ? length / (row.Count - 1) : 0.0f;

                    for (int k = 0; k < row.Count; k++)
                    {
                        Vector2 anchor = start + dir * (step * k);
                        result[row[k]] = anchor;
                    }

                    info.Runs++;
                    info.AdjustedSprites += row.Count;
                    if (info.Audit.Count < C2WallObjectsV53ModelChainAuditLimitLikeOriginal &&
                        catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal rowDesc) && rowDesc != null)
                    {
                        info.Audit.Add("V61_MODEL_RUN_ANCHOR sprite#" + kv.Key.ToString(CultureInfo.InvariantCulture) +
                                       "(" + rowDesc.Name + ")" +
                                       " model=" + rowDesc.ModelPath +
                                       " row=" + row.Count.ToString(CultureInfo.InvariantCulture) +
                                       " first=(" + start.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + start.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " last=(" + end.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + end.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " step=" + step.ToString("0.###", CultureInfo.InvariantCulture) +
                                       " rule=straight_line_even_spacing_preserve_first_last_no_forward_back_no_gaps");
                    }
                }

                i = j;
            }

            info.PreservedSprites += Math.Max(0, info.CandidateSprites - info.AdjustedSprites);
            return result;
        }

        private WallUniversalAnchorLineCalibrationV73LikeOriginal LoadWallUniversalAnchorLineCalibrationV73LikeOriginal()
        {
            if (_c2WallObjectsV73UniversalAnchorLineCalibrationLikeOriginal != null)
                return _c2WallObjectsV73UniversalAnchorLineCalibrationLikeOriginal;

            var result = new WallUniversalAnchorLineCalibrationV73LikeOriginal
            {
                Loaded = false,
                SpriteIndex = -1,
                SpriteName = string.Empty,
                ModelPath = string.Empty,
                SourcePath = string.Empty,
                ConnectorAuditV83 = "not_loaded",
                Audit = "not_loaded"
            };
            _c2WallObjectsV73UniversalAnchorLineCalibrationLikeOriginal = result;

            string projectPath = ResolveWallUniversalAnchorLineCalibrationProjectPathV73LikeOriginal();
            string persistentPath = ResolveWallUniversalAnchorLineCalibrationPersistentPathV73LikeOriginal();
            string path = File.Exists(projectPath) ? projectPath : (File.Exists(persistentPath) ? persistentPath : null);
            if (string.IsNullOrWhiteSpace(path))
            {
                result.Audit = "missing Skirmish2_wall_universal_line_v2.txt project='" + projectPath.Replace('\\', '/') + "' persistent='" + persistentPath.Replace('\\', '/') + "'";
                return result;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i] ?? string.Empty;
                    int comment = line.IndexOf('#');
                    if (comment == 0)
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(key))
                        kv[key] = value;
                }

                result.SourcePath = path;
                result.SpriteIndex = TryGetIntWallUniversalAnchorLineV73LikeOriginal(kv, "sprite.index", -1);
                result.SpriteName = TryGetStringWallUniversalAnchorLineV73LikeOriginal(kv, "sprite.name", string.Empty);
                result.ModelPath = TryGetStringWallUniversalAnchorLineV73LikeOriginal(kv, "model.path", string.Empty);

                int leftObject = FindWallUniversalAnchorLineObjectIndexByRoleV73LikeOriginal(kv, "LEFT");
                int rightObject = FindWallUniversalAnchorLineObjectIndexByRoleV73LikeOriginal(kv, "RIGHT");
                int centerObject = FindWallUniversalAnchorLineObjectIndexByRoleV73LikeOriginal(kv, "CENTER_MAIN");
                if (leftObject < 0 || rightObject < 0 || centerObject < 0)
                {
                    result.Audit = "role indices missing left=" + leftObject.ToString(CultureInfo.InvariantCulture) +
                                   " center=" + centerObject.ToString(CultureInfo.InvariantCulture) +
                                   " right=" + rightObject.ToString(CultureInfo.InvariantCulture);
                    return result;
                }

                Vector3 leftRootDeltaWorldV73 = ReadWallUniversalAnchorLineObjectDeltaWorldV73LikeOriginal(kv, leftObject);
                Vector3 rightRootDeltaWorldV73 = ReadWallUniversalAnchorLineObjectDeltaWorldV73LikeOriginal(kv, rightObject);

                bool p0okV83 = TryReadWallUniversalAnchorOriginalLocalByNameOrIndexV83LikeOriginal(kv, centerObject, "P0_LEFT_RED", 0, out result.ConnectorP0OriginalV83);
                bool p1okV83 = TryReadWallUniversalAnchorOriginalLocalByNameOrIndexV83LikeOriginal(kv, centerObject, "P1_RIGHT_GREEN", 1, out result.ConnectorP1OriginalV83);
                bool p2okV83 = TryReadWallUniversalAnchorOriginalLocalByNameOrIndexV83LikeOriginal(kv, centerObject, "P2_BACK_BLUE", 2, out result.ConnectorP2OriginalV83);
                bool p3okV83 = TryReadWallUniversalAnchorOriginalLocalByNameOrIndexV83LikeOriginal(kv, centerObject, "P3_FRONT_YELLOW", 3, out result.ConnectorP3OriginalV83);
                result.HasConnectorAnchorsV83 = p0okV83 && p1okV83 && p2okV83 && p3okV83;
                result.ConnectorAuditV83 = "V83_connector_points centerObject=" + centerObject.ToString(CultureInfo.InvariantCulture) +
                                            " p0=" + p0okV83.ToString() +
                                            " p1=" + p1okV83.ToString() +
                                            " p2=" + p2okV83.ToString() +
                                            " p3=" + p3okV83.ToString() +
                                            " localPairs=P0->P3,P1->P2";

                result.LeftDeltaWorld = leftRootDeltaWorldV73;
                result.RightDeltaWorld = rightRootDeltaWorldV73;
                result.LeftAnchorPairsAudit = "V77_not_computed";
                result.RightAnchorPairsAudit = "V77_not_computed";
                string anchorDeltaModeV77 = "V77_stage2_authored_point_pose_delta";

                if (C2WallObjectsV77UseAuthoredStage2PointPoseDeltasLikeOriginal)
                {
                    if (TryComputeWallUniversalAnchorAuthoredPoseDeltaV77LikeOriginal(kv, leftObject, "LEFT", out Vector3 leftPoseDeltaWorldV77, out string leftPoseAuditV77))
                    {
                        result.LeftDeltaWorld = leftPoseDeltaWorldV77;
                        result.LeftAnchorPairsAudit = leftPoseAuditV77;
                    }
                    else
                    {
                        result.LeftAnchorPairsAudit = leftPoseAuditV77 + " fallbackRootDelta";
                    }

                    if (TryComputeWallUniversalAnchorAuthoredPoseDeltaV77LikeOriginal(kv, rightObject, "RIGHT", out Vector3 rightPoseDeltaWorldV77, out string rightPoseAuditV77))
                    {
                        result.RightDeltaWorld = rightPoseDeltaWorldV77;
                        result.RightAnchorPairsAudit = rightPoseAuditV77;
                    }
                    else
                    {
                        result.RightAnchorPairsAudit = rightPoseAuditV77 + " fallbackRootDelta";
                    }
                }
                else if (C2WallObjectsV76UseNearestAnchorPairSnapDeltasLikeOriginal)
                {
                    anchorDeltaModeV77 = "V76_nearest_two_anchor_pairs_snap_delta_not_object_root_delta";
                    if (TryComputeWallUniversalAnchorEdgeSnapDeltaV76LikeOriginal(kv, leftObject, centerObject, "LEFT", out Vector3 leftPointDeltaWorldV76, out string leftPairsAuditV76))
                    {
                        result.LeftDeltaWorld = leftPointDeltaWorldV76;
                        result.LeftAnchorPairsAudit = leftPairsAuditV76;
                    }
                    else
                    {
                        result.LeftAnchorPairsAudit = leftPairsAuditV76;
                    }

                    if (TryComputeWallUniversalAnchorEdgeSnapDeltaV76LikeOriginal(kv, rightObject, centerObject, "RIGHT", out Vector3 rightPointDeltaWorldV76, out string rightPairsAuditV76))
                    {
                        result.RightDeltaWorld = rightPointDeltaWorldV76;
                        result.RightAnchorPairsAudit = rightPairsAuditV76;
                    }
                    else
                    {
                        result.RightAnchorPairsAudit = rightPairsAuditV76;
                    }
                }
                else
                {
                    anchorDeltaModeV77 = "V73_stage2_root_delta_fallback";
                    result.LeftAnchorPairsAudit = "V77_disabled rootFallback";
                    result.RightAnchorPairsAudit = "V77_disabled rootFallback";
                }

                OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
                float pixelToWorldX = kernel.BackingStepXWorld / 32.0f;
                float pixelToWorldZ = kernel.BackingStepZWorld * WorldZSign / 32.0f;
                result.LeftDeltaPixels = WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(result.LeftDeltaWorld, pixelToWorldX, pixelToWorldZ);
                result.RightDeltaPixels = WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(result.RightDeltaWorld, pixelToWorldX, pixelToWorldZ);

                result.Loaded = result.SpriteIndex >= 0 &&
                                result.LeftDeltaPixels.sqrMagnitude > 0.0001f &&
                                result.RightDeltaPixels.sqrMagnitude > 0.0001f;
                result.Audit = "source='" + path.Replace('\\', '/') + "' sprite=" + result.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                               " name=" + (result.SpriteName ?? string.Empty) +
                               " model=" + (result.ModelPath ?? string.Empty) +
                               " mode=" + anchorDeltaModeV77 +
                               " leftPairs='" + (result.LeftAnchorPairsAudit ?? string.Empty) + "'" +
                               " rightPairs='" + (result.RightAnchorPairsAudit ?? string.Empty) + "'" +
                               " leftRootFallbackPixels=(" + WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(leftRootDeltaWorldV73, pixelToWorldX, pixelToWorldZ).x.ToString("0.###", CultureInfo.InvariantCulture) + "," + WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(leftRootDeltaWorldV73, pixelToWorldX, pixelToWorldZ).y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                               " rightRootFallbackPixels=(" + WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(rightRootDeltaWorldV73, pixelToWorldX, pixelToWorldZ).x.ToString("0.###", CultureInfo.InvariantCulture) + "," + WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(rightRootDeltaWorldV73, pixelToWorldX, pixelToWorldZ).y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                               " leftDeltaPixels=(" + result.LeftDeltaPixels.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + result.LeftDeltaPixels.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                               " rightDeltaPixels=(" + result.RightDeltaPixels.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + result.RightDeltaPixels.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                               " connectorV83='" + (result.ConnectorAuditV83 ?? string.Empty) + "'" +
                               " pixelToWorld=(" + pixelToWorldX.ToString("0.###", CultureInfo.InvariantCulture) + "," + pixelToWorldZ.ToString("0.###", CultureInfo.InvariantCulture) + ")";
            }
            catch (Exception ex)
            {
                result.Loaded = false;
                result.Audit = "exception '" + ex.Message + "'";
            }

            return result;
        }


        private struct WallUniversalAnchorPointV76LikeOriginal
        {
            public string Name;
            public Vector3 LocalWorld;
            public Vector3 World;
            public Vector3 CenterLocal;
            public bool HasCenterLocal;
        }

        private struct WallUniversalAnchorPairCandidateV76LikeOriginal
        {
            public int MovingIndex;
            public int TargetIndex;
            public float Distance;
        }


        private static Vector3 ConvertWallUniversalAnchorUnityLocalToOriginalLocalV83LikeOriginal(Vector3 localWorld)
        {
            // Stage1 helper exported points in Unity helper space:
            // X = original X, Y = height, Z = Unity forward. Original Matrix4D local is (X, mapY, height).
            return new Vector3(localWorld.x, -localWorld.z, localWorld.y);
        }

        private static bool TryReadWallUniversalAnchorOriginalLocalByNameOrIndexV83LikeOriginal(
            Dictionary<string, string> kv,
            int objectIndex,
            string pointName,
            int fallbackIndex,
            out Vector3 originalLocal)
        {
            originalLocal = Vector3.zero;
            if (kv == null || objectIndex < 0)
                return false;

            WallUniversalAnchorPointV76LikeOriginal[] points = ReadWallUniversalAnchorObjectPointsV76LikeOriginal(kv, objectIndex);
            if (points == null || points.Length == 0)
                return false;

            if (!string.IsNullOrWhiteSpace(pointName))
            {
                for (int i = 0; i < points.Length; i++)
                {
                    if (!string.Equals(points[i].Name, pointName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    originalLocal = ConvertWallUniversalAnchorUnityLocalToOriginalLocalV83LikeOriginal(points[i].LocalWorld);
                    return true;
                }
            }

            if (fallbackIndex >= 0 && fallbackIndex < points.Length)
            {
                originalLocal = ConvertWallUniversalAnchorUnityLocalToOriginalLocalV83LikeOriginal(points[fallbackIndex].LocalWorld);
                return true;
            }

            return false;
        }

        private static Vector3 TransformOriginalMatrix4DBasisVectorV83LikeOriginal(Matrix4x4 m, Vector3 p)
        {
            // Same row-vector Matrix4D convention as TransformOriginalMatrix4DPointV19LikeOriginal(), but without translation.
            return new Vector3(
                p.x * m.m00 + p.y * m.m10 + p.z * m.m20,
                p.x * m.m01 + p.y * m.m11 + p.z * m.m21,
                p.x * m.m02 + p.y * m.m12 + p.z * m.m22
            );
        }

        private static bool TryComputeWallUniversalConnectorStepFromFourAnchorsV83LikeOriginal(
            WallUniversalAnchorLineCalibrationV73LikeOriginal calibration,
            Matrix4x4 basis,
            Vector2 rowDir,
            out Vector2 stepPixels,
            out string audit)
        {
            stepPixels = Vector2.zero;
            audit = "V83_not_computed";
            if (!C2WallObjectsV83UseModelAnchorConnectorChainLikeOriginal ||
                calibration == null ||
                !calibration.Loaded ||
                !calibration.HasConnectorAnchorsV83)
            {
                audit = calibration != null ? "V83_no_connector_anchors " + (calibration.ConnectorAuditV83 ?? string.Empty) : "V83_calibration_null";
                return false;
            }

            // Two authored edge pairs:
            //   previous.P3 should meet next.P0
            //   previous.P2 should meet next.P1
            // Therefore one section-to-section root step is the average of P3-P0 and P2-P1.
            Vector3 d03Original = TransformOriginalMatrix4DBasisVectorV83LikeOriginal(
                basis,
                calibration.ConnectorP3OriginalV83 - calibration.ConnectorP0OriginalV83);
            Vector3 d12Original = TransformOriginalMatrix4DBasisVectorV83LikeOriginal(
                basis,
                calibration.ConnectorP2OriginalV83 - calibration.ConnectorP1OriginalV83);

            Vector2 d03 = new Vector2(d03Original.x, d03Original.y);
            Vector2 d12 = new Vector2(d12Original.x, d12Original.y);
            Vector2 step = (d03 + d12) * 0.5f;

            if (step.sqrMagnitude <= 0.0001f)
            {
                audit = "V83_zero_step d03=(" + d03.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + d03.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                        " d12=(" + d12.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + d12.y.ToString("0.###", CultureInfo.InvariantCulture) + ")";
                return false;
            }

            if (rowDir.sqrMagnitude > 0.0001f && Vector2.Dot(rowDir.normalized, step.normalized) < 0.0f)
                step = -step;

            float pairError = Vector2.Distance(d03, d12);
            stepPixels = step;
            audit = "V83_MODEL_ANCHOR_CONNECTOR_CHAIN firstMapObject=true mapOnlyDirection=true pairs=P0->P3,P1->P2" +
                    " d03=(" + d03.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + d03.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " d12=(" + d12.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + d12.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " step=(" + stepPixels.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + stepPixels.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " pairError=" + pairError.ToString("0.###", CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryComputeWallUniversalAnchorAuthoredPoseDeltaV77LikeOriginal(
            Dictionary<string, string> kv,
            int movingObjectIndex,
            string movingRole,
            out Vector3 deltaWorld,
            out string audit)
        {
            deltaWorld = Vector3.zero;
            audit = "V77_not_computed";
            if (kv == null || movingObjectIndex < 0)
            {
                audit = "V77_bad_index role=" + (movingRole ?? string.Empty) +
                        " moving=" + movingObjectIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            WallUniversalAnchorPointV76LikeOriginal[] moving = ReadWallUniversalAnchorObjectPointsV76LikeOriginal(kv, movingObjectIndex);
            if (moving == null || moving.Length <= 0)
            {
                audit = "V77_no_points role=" + (movingRole ?? string.Empty);
                return false;
            }

            Vector3 sumDelta = Vector3.zero;
            int pointCount = 0;
            var pairAudit = new StringBuilder();

            for (int i = 0; i < moving.Length; i++)
            {
                if (!moving[i].HasCenterLocal)
                    continue;

                Vector3 pointDelta = moving[i].CenterLocal - moving[i].LocalWorld;
                if (!float.IsFinite(pointDelta.x) || !float.IsFinite(pointDelta.y) || !float.IsFinite(pointDelta.z))
                    continue;

                sumDelta += pointDelta;
                pointCount++;

                if (pairAudit.Length > 0)
                    pairAudit.Append("; " );
                pairAudit.Append(movingRole ?? string.Empty);
                pairAudit.Append(".");
                pairAudit.Append(moving[i].Name ?? ("P" + i.ToString(CultureInfo.InvariantCulture)));
                pairAudit.Append(" centerLocal-localWorld=(");
                pairAudit.Append(pointDelta.x.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(",");
                pairAudit.Append(pointDelta.y.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(",");
                pairAudit.Append(pointDelta.z.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(")");
            }

            if (pointCount <= 0)
            {
                audit = "V77_no_centerLocal_points role=" + (movingRole ?? string.Empty);
                return false;
            }

            deltaWorld = sumDelta / pointCount;

            float maxError = 0.0f;
            for (int i = 0; i < moving.Length; i++)
            {
                if (!moving[i].HasCenterLocal)
                    continue;
                Vector3 pointDelta = moving[i].CenterLocal - moving[i].LocalWorld;
                float err = (pointDelta - deltaWorld).magnitude;
                if (err > maxError)
                    maxError = err;
            }

            audit = "V77_POINT_POSE role=" + (movingRole ?? string.Empty) +
                    " points=" + pointCount.ToString(CultureInfo.InvariantCulture) +
                    " deltaWorld=(" + deltaWorld.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                      deltaWorld.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                      deltaWorld.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " maxPointError=" + maxError.ToString("0.###", CultureInfo.InvariantCulture) +
                    " " + pairAudit.ToString();
            return deltaWorld.sqrMagnitude > 0.0001f;
        }

        private static bool TryComputeWallUniversalAnchorEdgeSnapDeltaV76LikeOriginal(
            Dictionary<string, string> kv,
            int movingObjectIndex,
            int centerObjectIndex,
            string movingRole,
            out Vector3 deltaWorld,
            out string audit)
        {
            deltaWorld = Vector3.zero;
            audit = "V76_not_computed";
            if (kv == null || movingObjectIndex < 0 || centerObjectIndex < 0)
            {
                audit = "V76_bad_indices moving=" + movingObjectIndex.ToString(CultureInfo.InvariantCulture) +
                        " center=" + centerObjectIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            WallUniversalAnchorPointV76LikeOriginal[] moving = ReadWallUniversalAnchorObjectPointsV76LikeOriginal(kv, movingObjectIndex);
            WallUniversalAnchorPointV76LikeOriginal[] center = ReadWallUniversalAnchorObjectPointsV76LikeOriginal(kv, centerObjectIndex);
            if (moving == null || center == null || moving.Length < 2 || center.Length < 2)
            {
                audit = "V76_not_enough_points role=" + (movingRole ?? string.Empty) +
                        " movingCount=" + (moving != null ? moving.Length : 0).ToString(CultureInfo.InvariantCulture) +
                        " centerCount=" + (center != null ? center.Length : 0).ToString(CultureInfo.InvariantCulture);
                return false;
            }

            var candidates = new List<WallUniversalAnchorPairCandidateV76LikeOriginal>();
            for (int i = 0; i < moving.Length; i++)
            {
                for (int j = 0; j < center.Length; j++)
                {
                    float d = Vector3.Distance(moving[i].World, center[j].World);
                    if (!float.IsFinite(d))
                        continue;
                    candidates.Add(new WallUniversalAnchorPairCandidateV76LikeOriginal
                    {
                        MovingIndex = i,
                        TargetIndex = j,
                        Distance = d
                    });
                }
            }

            candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            bool[] usedMoving = new bool[moving.Length];
            bool[] usedTarget = new bool[center.Length];
            Vector3 sumDelta = Vector3.zero;
            int pairCount = 0;
            var pairAudit = new StringBuilder();

            for (int c = 0; c < candidates.Count && pairCount < 2; c++)
            {
                WallUniversalAnchorPairCandidateV76LikeOriginal pair = candidates[c];
                if (pair.MovingIndex < 0 || pair.MovingIndex >= moving.Length ||
                    pair.TargetIndex < 0 || pair.TargetIndex >= center.Length ||
                    usedMoving[pair.MovingIndex] ||
                    usedTarget[pair.TargetIndex])
                {
                    continue;
                }

                usedMoving[pair.MovingIndex] = true;
                usedTarget[pair.TargetIndex] = true;

                Vector3 pairDelta = center[pair.TargetIndex].LocalWorld - moving[pair.MovingIndex].LocalWorld;
                sumDelta += pairDelta;

                if (pairAudit.Length > 0)
                    pairAudit.Append("; ");
                pairAudit.Append(movingRole ?? string.Empty);
                pairAudit.Append(".");
                pairAudit.Append(moving[pair.MovingIndex].Name ?? ("P" + pair.MovingIndex.ToString(CultureInfo.InvariantCulture)));
                pairAudit.Append("->CENTER_MAIN.");
                pairAudit.Append(center[pair.TargetIndex].Name ?? ("P" + pair.TargetIndex.ToString(CultureInfo.InvariantCulture)));
                pairAudit.Append(" distStage2=");
                pairAudit.Append(pair.Distance.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(" localSnapDelta=(");
                pairAudit.Append(pairDelta.x.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(",");
                pairAudit.Append(pairDelta.y.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(",");
                pairAudit.Append(pairDelta.z.ToString("0.###", CultureInfo.InvariantCulture));
                pairAudit.Append(")");

                pairCount++;
            }

            if (pairCount <= 0)
            {
                audit = "V76_no_unique_pairs role=" + (movingRole ?? string.Empty);
                return false;
            }

            deltaWorld = sumDelta / pairCount;
            audit = "V76_POINT_SNAP role=" + (movingRole ?? string.Empty) +
                    " pairs=" + pairCount.ToString(CultureInfo.InvariantCulture) +
                    " deltaWorld=(" + deltaWorld.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                      deltaWorld.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                      deltaWorld.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " " + pairAudit.ToString();
            return deltaWorld.sqrMagnitude > 0.0001f;
        }

        private static WallUniversalAnchorPointV76LikeOriginal[] ReadWallUniversalAnchorObjectPointsV76LikeOriginal(
            Dictionary<string, string> kv,
            int objectIndex)
        {
            if (kv == null || objectIndex < 0)
                return Array.Empty<WallUniversalAnchorPointV76LikeOriginal>();

            int count = TryGetIntWallUniversalAnchorLineV73LikeOriginal(
                kv,
                "object" + objectIndex.ToString(CultureInfo.InvariantCulture) + ".point.count",
                0);
            if (count <= 0)
                count = 4;

            var points = new List<WallUniversalAnchorPointV76LikeOriginal>();
            for (int i = 0; i < count; i++)
            {
                string prefix = "object" + objectIndex.ToString(CultureInfo.InvariantCulture) + ".point" + i.ToString(CultureInfo.InvariantCulture);
                if (!kv.ContainsKey(prefix + ".name") &&
                    !kv.ContainsKey(prefix + ".localWorld.x") &&
                    !kv.ContainsKey(prefix + ".world.x"))
                {
                    continue;
                }

                bool hasCenterLocalV77 = kv.ContainsKey(prefix + ".centerLocal.x") ||
                                         kv.ContainsKey(prefix + ".centerLocal.y") ||
                                         kv.ContainsKey(prefix + ".centerLocal.z");

                var p = new WallUniversalAnchorPointV76LikeOriginal
                {
                    Name = TryGetStringWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".name", "P" + i.ToString(CultureInfo.InvariantCulture)),
                    LocalWorld = new Vector3(
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".localWorld.x", 0.0f),
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".localWorld.y", 0.0f),
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".localWorld.z", 0.0f)),
                    World = new Vector3(
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".world.x", 0.0f),
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".world.y", 0.0f),
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".world.z", 0.0f)),
                    CenterLocal = new Vector3(
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".centerLocal.x", 0.0f),
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".centerLocal.y", 0.0f),
                        TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".centerLocal.z", 0.0f)),
                    HasCenterLocal = hasCenterLocalV77
                };

                points.Add(p);
            }

            return points.ToArray();
        }

        private string ResolveWallUniversalAnchorLineCalibrationProjectPathV73LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";
            return Path.Combine(Application.dataPath, "Cossacks2Bridge", "Maps", "C2WallCalibration", fileName + "_wall_universal_line_v2.txt");
        }

        private string ResolveWallUniversalAnchorLineCalibrationPersistentPathV73LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";
            return Path.Combine(Application.persistentDataPath, "C2WallCalibration", fileName + "_wall_universal_line_v2.txt");
        }

        private static Vector2 WallUniversalAnchorLineWorldDeltaToOriginalPixelsV73LikeOriginal(Vector3 deltaWorld, float pixelToWorldX, float pixelToWorldZ)
        {
            float x = Mathf.Abs(pixelToWorldX) > 0.000001f ? deltaWorld.x / pixelToWorldX : deltaWorld.x;
            float y = Mathf.Abs(pixelToWorldZ) > 0.000001f ? deltaWorld.z / pixelToWorldZ : deltaWorld.z;
            return new Vector2(x, y);
        }

        private static int FindWallUniversalAnchorLineObjectIndexByRoleV73LikeOriginal(Dictionary<string, string> kv, string role)
        {
            if (kv == null || string.IsNullOrWhiteSpace(role))
                return -1;
            int count = TryGetIntWallUniversalAnchorLineV73LikeOriginal(kv, "object.count", 3);
            for (int i = 0; i < count; i++)
            {
                string key = "object" + i.ToString(CultureInfo.InvariantCulture) + ".role";
                if (kv.TryGetValue(key, out string value) && string.Equals(value, role, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static Vector3 ReadWallUniversalAnchorLineObjectDeltaWorldV73LikeOriginal(Dictionary<string, string> kv, int objectIndex)
        {
            string prefix = "object" + objectIndex.ToString(CultureInfo.InvariantCulture) + ".deltaWorldFromCenter";
            Vector3 v = new Vector3(
                TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".x", 0.0f),
                TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".y", 0.0f),
                TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".z", 0.0f));
            if (v.sqrMagnitude > 0.000001f)
                return v;

            prefix = "object" + objectIndex.ToString(CultureInfo.InvariantCulture) + ".positionInCenterLocal";
            return new Vector3(
                TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".x", 0.0f),
                TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".y", 0.0f),
                TryGetFloatWallUniversalAnchorLineV73LikeOriginal(kv, prefix + ".z", 0.0f));
        }

        private static bool TryGetWallUniversalAnchorDambaStepPairV73LikeOriginal(
            WallSpriteDescV1LikeOriginal desc,
            WallUniversalAnchorLineCalibrationV73LikeOriginal calibration,
            out Vector2 leftDeltaPixels,
            out Vector2 rightDeltaPixels,
            out string audit)
        {
            leftDeltaPixels = Vector2.zero;
            rightDeltaPixels = Vector2.zero;
            audit = string.Empty;
            if (!C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal || desc == null || calibration == null || !calibration.Loaded)
            {
                audit = calibration != null ? calibration.Audit : "calibration_null";
                return false;
            }

            if (calibration.SpriteIndex >= 0 && desc.SpriteIndex != calibration.SpriteIndex)
            {
                audit = "sprite_mismatch desc=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " calibration=" + calibration.SpriteIndex.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(calibration.ModelPath) &&
                !string.IsNullOrWhiteSpace(desc.ModelPath) &&
                !string.Equals(calibration.ModelPath.Replace('/', '\\'), desc.ModelPath.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase))
            {
                audit = "model_mismatch desc='" + desc.ModelPath + "' calibration='" + calibration.ModelPath + "'";
                return false;
            }

            leftDeltaPixels = calibration.LeftDeltaPixels;
            rightDeltaPixels = calibration.RightDeltaPixels;
            audit = calibration.Audit;
            return leftDeltaPixels.sqrMagnitude > 0.0001f || rightDeltaPixels.sqrMagnitude > 0.0001f;
        }

        private static bool IsWallDambaW60CalibrationTargetV90LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;

            if (desc.SpriteIndex == 60)
                return true;

            string modelPath = (desc.ModelPath ?? string.Empty).Replace('/', '\\').Trim();
            return modelPath.EndsWith("Models\\dam_bottom.c2m", StringComparison.OrdinalIgnoreCase) ||
                   modelPath.EndsWith("dam_bottom.c2m", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWallDambaRsrConnectorTargetV91LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;

            if (desc.SpriteIndex == 60 || desc.SpriteIndex == 63)
                return true;

            string modelPath = (desc.ModelPath ?? string.Empty).Replace('/', '\\').Trim();
            return modelPath.EndsWith("Models\\dam_bottom.c2m", StringComparison.OrdinalIgnoreCase) ||
                   modelPath.EndsWith("Models\\dam_top.c2m", StringComparison.OrdinalIgnoreCase) ||
                   modelPath.EndsWith("dam_bottom.c2m", StringComparison.OrdinalIgnoreCase) ||
                   modelPath.EndsWith("dam_top.c2m", StringComparison.OrdinalIgnoreCase);
        }

        private static Vector2 ChooseWallUniversalAnchorDambaStepForRowV73LikeOriginal(Vector2 rowDelta, Vector2 leftDeltaPixels, Vector2 rightDeltaPixels, out string side)
        {
            side = "fallback";
            Vector2 rowDir = rowDelta.sqrMagnitude > 0.0001f ? rowDelta.normalized : Vector2.right;
            Vector2 best = Vector2.zero;
            float bestScore = -1.0f;

            if (leftDeltaPixels.sqrMagnitude > 0.0001f)
            {
                float score = Mathf.Abs(Vector2.Dot(rowDir, leftDeltaPixels.normalized));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = leftDeltaPixels;
                    side = "LEFT_FROM_CENTER";
                }
            }

            if (rightDeltaPixels.sqrMagnitude > 0.0001f)
            {
                float score = Mathf.Abs(Vector2.Dot(rowDir, rightDeltaPixels.normalized));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = rightDeltaPixels;
                    side = "RIGHT_FROM_CENTER";
                }
            }

            if (best.sqrMagnitude < 0.0001f)
                best = C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal;

            if (Vector2.Dot(rowDelta, best) < 0.0f)
                best = -best;
            return best;
        }

        private static float TryGetFloatWallUniversalAnchorLineV73LikeOriginal(Dictionary<string, string> kv, string key, float fallback)
        {
            if (kv != null && kv.TryGetValue(key, out string value) &&
                float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return parsed;
            return fallback;
        }

        private static int TryGetIntWallUniversalAnchorLineV73LikeOriginal(Dictionary<string, string> kv, string key, int fallback)
        {
            if (kv != null && kv.TryGetValue(key, out string value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                return parsed;
            return fallback;
        }

        private static string TryGetStringWallUniversalAnchorLineV73LikeOriginal(Dictionary<string, string> kv, string key, string fallback)
        {
            if (kv != null && kv.TryGetValue(key, out string value))
                return value ?? fallback;
            return fallback;
        }

        private Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> BuildModelBackedDambaSectionRowAnchorsV68LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallConnectorChainInfoV14LikeOriginal info,
            out Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4> sharedBasisBySpriteV81,
            out Dictionary<WallSavedMapSpriteV6LikeOriginal, float> stage2FullPoseHeightBySpriteV89)
        {
            info = new WallConnectorChainInfoV14LikeOriginal();
            var result = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();
            sharedBasisBySpriteV81 = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4>();
            stage2FullPoseHeightBySpriteV89 = new Dictionary<WallSavedMapSpriteV6LikeOriginal, float>();

            if ((!C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal &&
                 !C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal &&
                 !C2WallObjectsV91UseRsrConnectorRigidDambaPlacementLikeOriginal &&
                 !C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal &&
                 !C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal) ||
                sprites == null || catalog == null || sprites.Count == 0)
                return result;

            WallUniversalAnchorLineCalibrationV73LikeOriginal universalCalibrationV73 =
                C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal
                    ? LoadWallUniversalAnchorLineCalibrationV73LikeOriginal()
                    : null;

            var bySprite = new Dictionary<int, List<WallSavedMapSpriteV6LikeOriginal>>();
            for (int i = 0; i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal sp = sprites[i];
                if (!TryGetWallDambaModelDescV60LikeOriginal(sp, catalog, out _))
                    continue;

                if (!bySprite.TryGetValue(sp.SpriteIndex, out List<WallSavedMapSpriteV6LikeOriginal> list) || list == null)
                {
                    list = new List<WallSavedMapSpriteV6LikeOriginal>();
                    bySprite[sp.SpriteIndex] = list;
                }
                list.Add(sp);
            }

            float maxDistSq = C2WallObjectsV68DambaMaxSectionNeighborDistanceOriginal *
                              C2WallObjectsV68DambaMaxSectionNeighborDistanceOriginal;

            foreach (var kv in bySprite)
            {
                List<WallSavedMapSpriteV6LikeOriginal> rowCandidates = kv.Value;
                if (rowCandidates == null || rowCandidates.Count < C2WallObjectsV68DambaMinSectionsInRun)
                {
                    info.PreservedSprites += rowCandidates != null ? rowCandidates.Count : 0;
                    continue;
                }

                var visited = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
                for (int seedIndex = 0; seedIndex < rowCandidates.Count; seedIndex++)
                {
                    WallSavedMapSpriteV6LikeOriginal seed = rowCandidates[seedIndex];
                    if (seed == null || visited.Contains(seed))
                        continue;

                    var component = new List<WallSavedMapSpriteV6LikeOriginal>();
                    var queue = new Queue<WallSavedMapSpriteV6LikeOriginal>();
                    visited.Add(seed);
                    queue.Enqueue(seed);

                    while (queue.Count > 0)
                    {
                        WallSavedMapSpriteV6LikeOriginal cur = queue.Dequeue();
                        component.Add(cur);
                        Vector2 c = new Vector2(cur.X, cur.Y);

                        for (int ni = 0; ni < rowCandidates.Count; ni++)
                        {
                            WallSavedMapSpriteV6LikeOriginal next = rowCandidates[ni];
                            if (next == null || visited.Contains(next))
                                continue;

                            Vector2 n = new Vector2(next.X, next.Y);
                            if ((n - c).sqrMagnitude > maxDistSq)
                                continue;

                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }

                    info.CandidateSprites += component.Count;
                    if (component.Count < C2WallObjectsV68DambaMinSectionsInRun)
                    {
                        info.PreservedSprites += component.Count;
                        continue;
                    }

                    WallSavedMapSpriteV6LikeOriginal rowFirst = component[0];
                    WallSavedMapSpriteV6LikeOriginal rowLast = component[0];
                    float farthestSq = -1.0f;
                    for (int a = 0; a < component.Count; a++)
                    {
                        Vector2 pa = new Vector2(component[a].X, component[a].Y);
                        for (int b = a + 1; b < component.Count; b++)
                        {
                            Vector2 pb = new Vector2(component[b].X, component[b].Y);
                            float dSq = (pb - pa).sqrMagnitude;
                            if (dSq <= farthestSq)
                                continue;

                            farthestSq = dSq;
                            rowFirst = component[a];
                            rowLast = component[b];
                        }
                    }

                    Vector2 start = new Vector2(rowFirst.X, rowFirst.Y);
                    Vector2 end = new Vector2(rowLast.X, rowLast.Y);
                    Vector2 delta = end - start;
                    float length = delta.magnitude;
                    if (length < C2WallObjectsV61MinStraightenRunLengthLikeOriginal)
                    {
                        info.PreservedSprites += component.Count;
                        continue;
                    }

                    WallSpriteDescV1LikeOriginal componentDesc = null;
                    Vector2 connectorStep = Vector2.zero;
                    Vector2 connectorDir = Vector2.zero;
                    if (catalog.ByIndex.TryGetValue(kv.Key, out componentDesc) && componentDesc != null)
                    {
                        connectorStep = GetWallConnectorStepOriginalXYV14LikeOriginal(componentDesc);
                        if (connectorStep.sqrMagnitude > 0.0001f)
                            connectorDir = connectorStep.normalized;
                    }

                    Vector2 calibratedStepV72 = Vector2.zero;
                    Vector2 calibratedPositiveStepV74 = Vector2.zero;
                    Vector2 calibratedNegativeStepV74 = Vector2.zero;
                    Vector2 rsrConnectorStepV91 = Vector2.zero;
                    string rsrConnectorAuditV91 = string.Empty;
                    bool useRsrConnectorRigidV91 =
                        TryGetWallDambaRsrConnectorStepV91LikeOriginal(componentDesc, out rsrConnectorStepV91, out rsrConnectorAuditV91);
                    bool useStage2FullPoseV88 = false;
                    Vector2 stage2FullPoseStepPixelsV88 = Vector2.zero;
                    Vector3 stage2FullPoseDeltaWorldV88 = Vector3.zero;
                    string stage2FullPoseAuditV88 = string.Empty;
                    bool useModelConnectorChainV83 = false;
                    Vector2 modelConnectorStepV83 = Vector2.zero;
                    string modelConnectorAuditV83 = string.Empty;
                    string calibratedModeV72 = "none";
                    string universalAuditV73 = string.Empty;
                    bool useUniversalAnchorCalibrationV73 =
                        TryGetWallUniversalAnchorDambaStepPairV73LikeOriginal(
                            componentDesc,
                            universalCalibrationV73,
                            out Vector2 universalLeftDeltaV73,
                            out Vector2 universalRightDeltaV73,
                            out universalAuditV73);

                    Vector2 rowDirV74 = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
                    string universalSideV73 = "fallback";
                    Vector2 calibratedBaseStepV73 = useUniversalAnchorCalibrationV73
                        ? ChooseWallUniversalAnchorDambaStepForRowV73LikeOriginal(delta, universalLeftDeltaV73, universalRightDeltaV73, out universalSideV73)
                        : C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal;
                    if (useUniversalAnchorCalibrationV73)
                    {
                        calibratedModeV72 = "V82_stage2_delta_shared_center_basis_ONE_FLAT_DECK_LEVEL_bidirectional_" + universalSideV73;

                        // V74: Stage2 saved three objects around CENTER_MAIN.
                        // Do not start the runtime chain from a far endpoint.
                        // Split the two saved deltas into positive/negative row directions and keep the middle map object fixed.
                        Vector2 d0 = universalLeftDeltaV73;
                        Vector2 d1 = universalRightDeltaV73;
                        float dot0 = Vector2.Dot(rowDirV74, d0);
                        float dot1 = Vector2.Dot(rowDirV74, d1);
                        if (dot0 >= dot1)
                        {
                            calibratedPositiveStepV74 = d0;
                            calibratedNegativeStepV74 = d1;
                        }
                        else
                        {
                            calibratedPositiveStepV74 = d1;
                            calibratedNegativeStepV74 = d0;
                        }

                        if (Vector2.Dot(rowDirV74, calibratedPositiveStepV74) < 0.0f)
                            calibratedPositiveStepV74 = -calibratedPositiveStepV74;
                        if (Vector2.Dot(rowDirV74, calibratedNegativeStepV74) > 0.0f)
                            calibratedNegativeStepV74 = -calibratedNegativeStepV74;

                        if (calibratedPositiveStepV74.sqrMagnitude < 0.0001f && calibratedNegativeStepV74.sqrMagnitude > 0.0001f)
                            calibratedPositiveStepV74 = -calibratedNegativeStepV74;
                        if (calibratedNegativeStepV74.sqrMagnitude < 0.0001f && calibratedPositiveStepV74.sqrMagnitude > 0.0001f)
                            calibratedNegativeStepV74 = -calibratedPositiveStepV74;

                        if (C2WallObjectsV88UseStage2FullPoseRelativeTransformForDambaLikeOriginal)
                        {
                            Vector3 leftPoseDeltaV88 = universalCalibrationV73 != null ? universalCalibrationV73.LeftDeltaWorld : Vector3.zero;
                            Vector3 rightPoseDeltaV88 = universalCalibrationV73 != null ? universalCalibrationV73.RightDeltaWorld : Vector3.zero;
                            Vector2 leftPosePixelsV88 = new Vector2(leftPoseDeltaV88.x, -leftPoseDeltaV88.z);
                            Vector2 rightPosePixelsV88 = new Vector2(rightPoseDeltaV88.x, -rightPoseDeltaV88.z);

                            float leftDotV88 = Vector2.Dot(rowDirV74, leftPosePixelsV88);
                            float rightDotV88 = Vector2.Dot(rowDirV74, rightPosePixelsV88);
                            if (leftDotV88 >= rightDotV88)
                            {
                                stage2FullPoseDeltaWorldV88 = leftPoseDeltaV88;
                                stage2FullPoseStepPixelsV88 = leftPosePixelsV88;
                            }
                            else
                            {
                                stage2FullPoseDeltaWorldV88 = rightPoseDeltaV88;
                                stage2FullPoseStepPixelsV88 = rightPosePixelsV88;
                            }

                            if (Vector2.Dot(stage2FullPoseStepPixelsV88, rowDirV74) < 0.0f)
                            {
                                stage2FullPoseStepPixelsV88 = -stage2FullPoseStepPixelsV88;
                                stage2FullPoseDeltaWorldV88 = -stage2FullPoseDeltaWorldV88;
                            }

                            useStage2FullPoseV88 = stage2FullPoseStepPixelsV88.sqrMagnitude > 0.0001f;
                            if (useStage2FullPoseV88)
                            {
                                calibratedPositiveStepV74 = stage2FullPoseStepPixelsV88;
                                calibratedNegativeStepV74 = -stage2FullPoseStepPixelsV88;
                                calibratedStepV72 = stage2FullPoseStepPixelsV88;
                                calibratedModeV72 = "V88_stage2_full_pose_relative_transform_first_map_object";
                            }

                            stage2FullPoseAuditV88 =
                                "V88_STAGE2_FULL_POSE" +
                                " leftWorld=(" + leftPoseDeltaV88.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + leftPoseDeltaV88.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + leftPoseDeltaV88.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                " rightWorld=(" + rightPoseDeltaV88.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + rightPoseDeltaV88.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + rightPoseDeltaV88.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                " chosenWorld=(" + stage2FullPoseDeltaWorldV88.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + stage2FullPoseDeltaWorldV88.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + stage2FullPoseDeltaWorldV88.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                " chosenPixels=(" + stage2FullPoseStepPixelsV88.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + stage2FullPoseStepPixelsV88.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                " firstMapObject=true next=previous*Stage2RelativeTransform V14_scene_anchors_kept";
                        }
                    }

                    bool useCalibratedPairChainV72 =
                        IsWallDambaC2MModelV33LikeOriginal(componentDesc) &&
                        ((useUniversalAnchorCalibrationV73 &&
                          calibratedPositiveStepV74.sqrMagnitude > 0.0001f &&
                          calibratedNegativeStepV74.sqrMagnitude > 0.0001f) ||
                         (!useUniversalAnchorCalibrationV73 &&
                          IsWallDambaW60CalibrationTargetV90LikeOriginal(componentDesc) &&
                          C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal &&
                          calibratedBaseStepV73.sqrMagnitude > 0.0001f));

                    Vector2 dir = useCalibratedPairChainV72
                        ? (useUniversalAnchorCalibrationV73 ? rowDirV74 : calibratedBaseStepV73.normalized)
                        : (useRsrConnectorRigidV91 ? rsrConnectorStepV91.normalized : (connectorDir.sqrMagnitude > 0.0001f ? connectorDir : delta / length));
                    if (Vector2.Dot(delta, dir) < 0.0f)
                        dir = -dir;
                    if (useRsrConnectorRigidV91 && Vector2.Dot(rsrConnectorStepV91, dir) < 0.0f)
                        rsrConnectorStepV91 = -rsrConnectorStepV91;
                    if (useCalibratedPairChainV72 && !useUniversalAnchorCalibrationV73)
                    {
                        calibratedStepV72 = dir * calibratedBaseStepV73.magnitude;
                        calibratedPositiveStepV74 = calibratedStepV72;
                        calibratedNegativeStepV74 = -calibratedStepV72;
                        calibratedModeV72 = "V72_pair_calibrated_endpoint_chain_hardcoded";
                    }
                    else if (useCalibratedPairChainV72)
                    {
                        calibratedStepV72 = calibratedPositiveStepV74;
                    }

                    component.Sort((a, b) =>
                    {
                        float pa = Vector2.Dot(new Vector2(a.X, a.Y) - start, dir);
                        float pb = Vector2.Dot(new Vector2(b.X, b.Y) - start, dir);
                        return pa.CompareTo(pb);
                    });

                    if (Vector2.Dot(new Vector2(component[0].X, component[0].Y) - start, dir) >
                        Vector2.Dot(new Vector2(component[component.Count - 1].X, component[component.Count - 1].Y) - start, dir))
                        component.Reverse();

                    float meanPerp = 0.0f;
                    Vector2 normal = new Vector2(-dir.y, dir.x);
                    for (int k = 0; k < component.Count; k++)
                        meanPerp += Vector2.Dot(new Vector2(component[k].X, component[k].Y) - start, normal);
                    meanPerp /= Mathf.Max(1, component.Count);

                    Matrix4x4 firstMapBasisV83 = (component.Count > 0 && component[0] != null && component[0].HasMatrix)
                        ? component[0].Matrix
                        : Matrix4x4.identity;
                    useModelConnectorChainV83 =
                        C2WallObjectsV83UseModelAnchorConnectorChainLikeOriginal &&
                        !useStage2FullPoseV88 &&
                        useCalibratedPairChainV72 &&
                        useUniversalAnchorCalibrationV73 &&
                        component.Count > 0 &&
                        TryComputeWallUniversalConnectorStepFromFourAnchorsV83LikeOriginal(
                            universalCalibrationV73,
                            firstMapBasisV83,
                            rowDirV74,
                            out modelConnectorStepV83,
                            out modelConnectorAuditV83);
                    if (useModelConnectorChainV83)
                        calibratedModeV72 = "V83_model_anchor_connector_chain_FIRST_MAP_OBJECT_map_only_direction";

                    int centerSeedIndexV74 = component.Count / 2;
                    if (useUniversalAnchorCalibrationV73 && component.Count > 0)
                    {
                        float meanAlongV74 = 0.0f;
                        for (int k = 0; k < component.Count; k++)
                            meanAlongV74 += Vector2.Dot(new Vector2(component[k].X, component[k].Y) - start, dir);
                        meanAlongV74 /= Mathf.Max(1, component.Count);

                        float bestCenterDistV74 = float.MaxValue;
                        for (int k = 0; k < component.Count; k++)
                        {
                            float alongV74 = Vector2.Dot(new Vector2(component[k].X, component[k].Y) - start, dir);
                            float distV74 = Mathf.Abs(alongV74 - meanAlongV74);
                            if (distV74 < bestCenterDistV74)
                            {
                                bestCenterDistV74 = distV74;
                                centerSeedIndexV74 = k;
                            }
                        }
                    }

                    bool useSharedCenterBasisV81 =
                        (useStage2FullPoseV88 &&
                         component.Count > 0 &&
                         component[0] != null &&
                         component[0].HasMatrix) ||
                        (useModelConnectorChainV83 &&
                         component.Count > 0 &&
                         component[0] != null &&
                         component[0].HasMatrix) ||
                        (C2WallObjectsV81UseSharedCenterMatrixBasisForUniversalDambaRowsLikeOriginal &&
                         useCalibratedPairChainV72 &&
                         useUniversalAnchorCalibrationV73 &&
                         centerSeedIndexV74 >= 0 &&
                         centerSeedIndexV74 < component.Count &&
                         component[centerSeedIndexV74] != null &&
                         component[centerSeedIndexV74].HasMatrix);
                    Matrix4x4 sharedCenterBasisV81 = (useStage2FullPoseV88 || useModelConnectorChainV83)
                        ? firstMapBasisV83
                        : (useSharedCenterBasisV81
                            ? component[centerSeedIndexV74].Matrix
                            : Matrix4x4.identity);

                    Vector2 firstMapAnchorV83 = component.Count > 0 && component[0] != null
                        ? new Vector2(component[0].X, component[0].Y)
                        : start;

                    bool useAnyAnchorRewriteV91Safe =
                        useStage2FullPoseV88 ||
                        useModelConnectorChainV83 ||
                        C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal ||
                        useCalibratedPairChainV72 ||
                        useRsrConnectorRigidV91 ||
                        C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal;
                    if (!useAnyAnchorRewriteV91Safe)
                        continue;

                    for (int k = 0; k < component.Count; k++)
                    {
                        if (useStage2FullPoseV88)
                        {
                            result[component[k]] = firstMapAnchorV83 + stage2FullPoseStepPixelsV88 * k;

                            if (C2WallObjectsV89ApplyStage2Full3DHeightDeltaForDambaLikeOriginal && component[k] != null)
                            {
                                float firstOriginalHeightV89 = component[0] != null
                                    ? SampleWallHeightOriginalXYV1LikeOriginal(component[0].X, component[0].Y)
                                    : SampleWallHeightOriginalXYV1LikeOriginal(firstMapAnchorV83.x, firstMapAnchorV83.y);
                                stage2FullPoseHeightBySpriteV89[component[k]] =
                                    firstOriginalHeightV89 + stage2FullPoseDeltaWorldV88.y * k;
                            }
                        }
                        else if (useModelConnectorChainV83)
                        {
                            result[component[k]] = firstMapAnchorV83 + modelConnectorStepV83 * k;
                        }
                        else if (C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal)
                        {
                            Vector2 native = new Vector2(component[k].X, component[k].Y);
                            float along = Vector2.Dot(native - start, dir);
                            result[component[k]] = start + dir * along + normal * meanPerp;
                        }
                        else if (useCalibratedPairChainV72 && useUniversalAnchorCalibrationV73)
                        {
                            Vector2 centerAnchorV74 = new Vector2(component[centerSeedIndexV74].X, component[centerSeedIndexV74].Y);
                            int relV74 = k - centerSeedIndexV74;
                            result[component[k]] = relV74 >= 0
                                ? centerAnchorV74 + calibratedPositiveStepV74 * relV74
                                : centerAnchorV74 + calibratedNegativeStepV74 * (-relV74);
                        }
                        else if (useCalibratedPairChainV72)
                        {
                            Vector2 firstAnchor = new Vector2(component[0].X, component[0].Y);
                            result[component[k]] = firstAnchor + calibratedStepV72 * k;
                        }
                        else if (useRsrConnectorRigidV91)
                        {
                            Vector2 firstAnchor = new Vector2(component[0].X, component[0].Y);
                            result[component[k]] = firstAnchor + rsrConnectorStepV91 * k;
                        }
                        else
                        {
                            float t = component.Count > 1 ? (float)k / (component.Count - 1) : 0.0f;
                            float dist = length * t;
                            result[component[k]] = start + dir * dist;
                        }

                        if (useSharedCenterBasisV81 && component[k] != null)
                            sharedBasisBySpriteV81[component[k]] = sharedCenterBasisV81;
                    }

                    info.Runs++;
                    info.AdjustedSprites += component.Count;
                    if (info.Audit.Count < C2WallObjectsV53ModelChainAuditLimitLikeOriginal &&
                        catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal rowDesc) && rowDesc != null)
                    {
                        info.Audit.Add("V68_DAMBA_SECTION_ROW sprite#" + kv.Key.ToString(CultureInfo.InvariantCulture) +
                                       "(" + rowDesc.Name + ")" +
                                       " count=" + component.Count.ToString(CultureInfo.InvariantCulture) +
                                       " first=(" + start.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + start.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " last=(" + end.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + end.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " connectorStep=(" + connectorStep.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + connectorStep.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v91RsrConnectorRigid=" + (useRsrConnectorRigidV91 ? "True" : "False") +
                                       " v91Step=(" + rsrConnectorStepV91.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + rsrConnectorStepV91.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v91Audit='" + (rsrConnectorAuditV91 ?? string.Empty) + "'" +
                                       " calibratedStepV72=(" + calibratedStepV72.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + calibratedStepV72.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v74PositiveStep=(" + calibratedPositiveStepV74.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + calibratedPositiveStepV74.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v74NegativeStep=(" + calibratedNegativeStepV74.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + calibratedNegativeStepV74.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " centerSeedIndexV74=" + centerSeedIndexV74.ToString(CultureInfo.InvariantCulture) +
                                       " sharedCenterBasisV81=" + (useSharedCenterBasisV81 ? "True" : "False") +
                                       " v88Stage2FullPose=" + (useStage2FullPoseV88 ? "True" : "False") +
                                       " v88StepPixels=(" + stage2FullPoseStepPixelsV88.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + stage2FullPoseStepPixelsV88.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v88DeltaWorld=(" + stage2FullPoseDeltaWorldV88.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + stage2FullPoseDeltaWorldV88.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + stage2FullPoseDeltaWorldV88.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v89Full3DHeight=" + (C2WallObjectsV89ApplyStage2Full3DHeightDeltaForDambaLikeOriginal && useStage2FullPoseV88 ? "True" : "False") +
                                       " v88Audit='" + (stage2FullPoseAuditV88 ?? string.Empty) + "'" +
                                       " v83ConnectorChain=" + (useModelConnectorChainV83 ? "True" : "False") +
                                       " v83Step=(" + modelConnectorStepV83.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + modelConnectorStepV83.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " v83Audit='" + (modelConnectorAuditV83 ?? string.Empty) + "'" +
                                       " dir=(" + dir.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + dir.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                                       " mode=" + (useStage2FullPoseV88 ? "V88_stage2_full_pose_relative_transform_FIRST_MAP_OBJECT_then_previous_mul_stage2_pose_" + universalSideV73 : (useModelConnectorChainV83 ? "V83_model_anchor_connector_chain_FIRST_MAP_OBJECT_map_only_direction" : (useCalibratedPairChainV72 && useUniversalAnchorCalibrationV73 && useSharedCenterBasisV81 ? "V81_stage2_delta_chain_shared_CENTER_MAIN_Matrix4D_basis_" + universalSideV73 : (useCalibratedPairChainV72 ? calibratedModeV72 : (useRsrConnectorRigidV91 ? "V91_RSR_connector_type_step_rigid_anchor_only_no_mesh_deform" : (C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal ? "V69_project_perp_keep_along" : "V68_resample_first_last")))))) +
                                       " maxNeighbor=" + C2WallObjectsV68DambaMaxSectionNeighborDistanceOriginal.ToString("0.###", CultureInfo.InvariantCulture) +
                                       " universalV73='" + (universalAuditV73 ?? string.Empty) + "'");
                    }
                }
            }

            info.PreservedSprites += Math.Max(0, info.CandidateSprites - info.AdjustedSprites);
            return result;
        }

        private Dictionary<WallSavedMapSpriteV6LikeOriginal, float> BuildModelBackedBridgeRunHeightsV59LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallConnectorChainInfoV14LikeOriginal info)
        {
            info = new WallConnectorChainInfoV14LikeOriginal();
            var result = new Dictionary<WallSavedMapSpriteV6LikeOriginal, float>();

            if (!C2WallObjectsV59LevelModelBackedBridgeRunsLikeOriginal ||
                sprites == null || catalog == null || sprites.Count == 0)
                return result;

            int i = 0;
            while (i < sprites.Count)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (first == null)
                {
                    i++;
                    continue;
                }

                if (!TryGetWallDambaModelDescV60LikeOriginal(first, catalog, out WallSpriteDescV1LikeOriginal firstDesc))
                {
                    info.PreservedSprites++;
                    i++;
                    continue;
                }

                int j = i + 1;
                if (C2WallObjectsV60GroupAllContiguousDambaModelsForFlatHeightLikeOriginal)
                {
                    while (j < sprites.Count && TryGetWallDambaModelDescV60LikeOriginal(sprites[j], catalog, out _))
                        j++;
                }
                else
                {
                    int spriteIndex = first.SpriteIndex;
                    while (j < sprites.Count && sprites[j] != null && sprites[j].SpriteIndex == spriteIndex)
                        j++;
                }

                int count = j - i;
                float runHeight = float.NegativeInfinity;
                for (int k = i; k < j; k++)
                {
                    WallSavedMapSpriteV6LikeOriginal sp = sprites[k];
                    if (sp == null)
                        continue;
                    float h = SampleWallHeightOriginalXYV1LikeOriginal(sp.X, sp.Y);
                    if (h > runHeight)
                        runHeight = h;
                }

                if (!float.IsFinite(runHeight))
                    runHeight = SampleWallHeightOriginalXYV1LikeOriginal(first.X, first.Y);

                for (int k = i; k < j; k++)
                {
                    WallSavedMapSpriteV6LikeOriginal sp = sprites[k];
                    if (sp != null)
                        result[sp] = runHeight;
                }

                info.Runs++;
                info.CandidateSprites += count;
                info.AdjustedSprites += count;
                if (info.Audit.Count < C2WallObjectsV53ModelChainAuditLimitLikeOriginal)
                {
                    info.Audit.Add("V60_MODEL_RUN_HEIGHT firstSprite#" + first.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                                   "(" + firstDesc.Name + ")" +
                                   " firstModel=" + firstDesc.ModelPath +
                                   " run=" + count.ToString(CultureInfo.InvariantCulture) +
                                   " flatHeight=" + runHeight.ToString("0.###", CultureInfo.InvariantCulture) +
                                   " anchorFraction=" + C2WallObjectsV60BridgeDeckAnchorLocalZFraction.ToString("0.###", CultureInfo.InvariantCulture) +
                                   " rule=contiguous_DAMBA_group_mapXY_saved_anchor_align_C2M_DECK_not_top");
                }

                i = j;
            }

            info.PreservedSprites = Math.Max(0, info.CandidateSprites - info.AdjustedSprites);
            return result;
        }

        private int BuildSyntheticIdenticalWL2DFenceLineRootsV144LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            Transform parent,
            Material fallbackMaterial,
            out HashSet<WallSavedMapSpriteV6LikeOriginal> suppressedSprites,
            out string audit)
        {
            suppressedSprites = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
            audit = string.Empty;
            if (!C2WallObjectsV144BuildIdenticalWL2DFenceLineRootsLikeOriginal || sprites == null || catalog == null || parent == null)
                return 0;

            int created = 0;
            var auditParts = new List<string>(C2WallObjectsV132FenceAuditLimitLikeOriginal);

            int i = 0;
            while (i < sprites.Count)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (!TryGetStraightenableWL2DFenceDescV132LikeOriginal(first, catalog, out WallSpriteDescV1LikeOriginal firstDescV150))
                {
                    i++;
                    continue;
                }

                int segStart = i;
                int segEnd = segStart + 1;
                int familyMaskV152 = GetWallFencePairFamilyMaskV152LikeOriginal(first.SpriteIndex);
                if (familyMaskV152 == 0)
                {
                    i++;
                    continue;
                }

                while (segEnd < sprites.Count) // V152: SpriteIndex is not a boundary; pair-family is the boundary.
                {
                    WallSavedMapSpriteV6LikeOriginal b = sprites[segEnd];
                    if (!TryGetStraightenableWL2DFenceDescV132LikeOriginal(b, catalog, out WallSpriteDescV1LikeOriginal bDescV152))
                        break;

                    int bFamilyMaskV152 = GetWallFencePairFamilyMaskV152LikeOriginal(b.SpriteIndex);
                    int nextFamilyMaskV152 = familyMaskV152 & bFamilyMaskV152;
                    if (nextFamilyMaskV152 == 0)
                        break;

                    if (!CanAppendWallFenceCandidateToSideLineV153LikeOriginal(sprites, segStart, segEnd + 1, nextFamilyMaskV152))
                        break;

                    familyMaskV152 = nextFamilyMaskV152;
                    segEnd++;
                }

                int count = segEnd - segStart;
                if (count >= C2WallObjectsV132FenceMinRunLengthLikeOriginal)
                {
                    WallSavedMapSpriteV6LikeOriginal runFirst = sprites[segStart];
                    WallSavedMapSpriteV6LikeOriginal runLast = sprites[segEnd - 1];
                    if (runFirst != null && runLast != null)
                    {
                        Material[] materials;
                        Mesh mesh = BuildSideLineWL2DFenceLineRootMeshV150LikeOriginal(sprites, segStart, segEnd, familyMaskV152, catalog, fallbackMaterial, out materials);
                        if (mesh != null && materials != null && materials.Length > 0)
                        {
                            GameObject go = new GameObject("WallFenceWALS2D_WALLS_g16_LineRootV171_" + segStart.ToString("0000", CultureInfo.InvariantCulture) + "_" + (segEnd - 1).ToString("0000", CultureInfo.InvariantCulture));
                            go.transform.SetParent(parent, false);
                            MeshFilter mf = go.AddComponent<MeshFilter>();
                            MeshRenderer mr = go.AddComponent<MeshRenderer>();
                            ApplyWallRendererShadowContractV44LikeOriginal(mr);
                            mf.sharedMesh = mesh;
                            mr.sharedMaterials = materials;
                            mr.sortingOrder = Mathf.Clamp(runFirst.Y, -32768, 32767);

                            for (int k = segStart; k < segEnd; k++)
                            {
                                if (sprites[k] != null)
                                    suppressedSprites.Add(sprites[k]);
                            }

                            created++;
                            if (auditParts.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                            {
                                Vector2 p0 = new Vector2(runFirst.X, runFirst.Y);
                                Vector2 p1 = new Vector2(runLast.X, runLast.Y);
                                float len = (p1 - p0).magnitude;
                                float stepLen = count > 0 ? len / Mathf.Max(1, count) : 0.0f;
                                string spriteSummary = BuildWallFenceSideLineSpriteSummaryV150LikeOriginal(sprites, segStart, segEnd);
                                Vector2 originalLineV152 = new Vector2(runLast.X - runFirst.X, runLast.Y - runFirst.Y);
                                bool topBottomSideV153 = IsWallFenceTopBottomSideV152LikeOriginal(originalLineV152);
                                int selectedSpriteV153 = SelectDominantWallFencePairSpriteForSideV153LikeOriginal(sprites, segStart, segEnd, familyMaskV152, topBottomSideV153);
                                auditParts.Add("familySprites=" + spriteSummary +
                                               " sideLine=" + segStart.ToString(CultureInfo.InvariantCulture) + "-" + (segEnd - 1).ToString(CultureInfo.InvariantCulture) +
                                               " count=" + count.ToString(CultureInfo.InvariantCulture) +
                                               " first=(" + runFirst.X.ToString(CultureInfo.InvariantCulture) + "," + runFirst.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                               " last=(" + runLast.X.ToString(CultureInfo.InvariantCulture) + "," + runLast.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                               " stepLen=" + stepLen.ToString("0.###", CultureInfo.InvariantCulture) +
                                               " side=" + (topBottomSideV153 ? "TopBottom" : "LeftRight") +
                                               " pairFamilyMask=0x" + familyMaskV152.ToString("X", CultureInfo.InvariantCulture) +
                                               " selectedSprite=W" + selectedSpriteV153.ToString(CultureInfo.InvariantCulture) +
                                               " recreate={" + _c2WallObjectsV157LastReCreateAuditLikeOriginal + "}" +
                                               " wals2dBackendV171={" + _c2WallObjectsV159LastModelIDAuditLikeOriginal + "}" +
                                               " OneWallsSystemGraph=True OneWallEdge=True OneWallLine=True StartFinal=True x_out_x_in=True OneWallElement=True WallTypeDescription=True OneWallPoint=True ReCreate=True ne_round_Tdist_div_esize=True hScale=True zBlend=True angleFromLine=True smoothPass=True WALS2DLineRootV171=True ModelIDMatrix4DBackend=False V161_ModelIDRequired=False oldSavedWLIndividualCardsPhysicallyDeleted=True realWTCycle=False syntheticWALS2D=True savedMatrix4DInputAudit=True originalMatrix4DOutput=False pairFamilyOnly=False sideLineByAdjacency=True singleCombinedMesh=True suppressIndividualCards=True meshSections=" + count.ToString(CultureInfo.InvariantCulture) + " markerV152=True");
                            }
                        }
                        else if (C2WallObjectsV161SuppressRejectedOneWallsSystemWL3DWallsLikeOriginal)
                        {
                            for (int k = segStart; k < segEnd; k++)
                            {
                                if (sprites[k] != null)
                                {
                                    suppressedSprites.Add(sprites[k]);
                                    _c2WallObjectsV161Rejected3DWallsSavedWLSuppressedLikeOriginal++;
                                }
                            }

                            if (auditParts.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                            {
                                auditParts.Add("REJECTED_HARD_DELETE_OLD_SAVED_WL_FENCE_CARDS_V165 sideLine=" + segStart.ToString(CultureInfo.InvariantCulture) + "-" + (segEnd - 1).ToString(CultureInfo.InvariantCulture) +
                                               " count=" + count.ToString(CultureInfo.InvariantCulture) +
                                               " recreate={" + _c2WallObjectsV157LastReCreateAuditLikeOriginal + "}" +
                                               " modelIDBackend={" + _c2WallObjectsV159LastModelIDAuditLikeOriginal + "}" +
                                               " action=old_individual_saved_WL_fence_cards_hard_deleted_no_fallback");
                            }
                        }
                    }
                }

                i = Math.Max(segEnd, segStart + 1);
            }

            audit = auditParts.Count > 0 ? string.Join(" | ", auditParts.ToArray()) : "none";
            return created;
        }

        private static bool CanAppendWallFenceCandidateToSideLineV153LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> sprites, int start, int nextExclusive, int familyMaskV152)
        {
            if (sprites == null || start < 0 || nextExclusive <= start || nextExclusive > sprites.Count || familyMaskV152 == 0)
                return false;
            if (nextExclusive - start <= 2)
            {
                WallSavedMapSpriteV6LikeOriginal a = sprites[start];
                WallSavedMapSpriteV6LikeOriginal b = sprites[nextExclusive - 1];
                if (a == null || b == null)
                    return false;
                float d = new Vector2(b.X - a.X, b.Y - a.Y).magnitude;
                return d >= C2WallObjectsV132FenceMinStepOriginal && d <= C2WallObjectsV132FenceMaxStepOriginal;
            }

            WallSavedMapSpriteV6LikeOriginal first = sprites[start];
            WallSavedMapSpriteV6LikeOriginal last = sprites[nextExclusive - 1];
            if (first == null || last == null)
                return false;

            Vector2 p0 = new Vector2(first.X, first.Y);
            Vector2 p1 = new Vector2(last.X, last.Y);
            Vector2 line = p1 - p0;
            float len = line.magnitude;
            if (len < C2WallObjectsV132FenceMinStepOriginal)
                return false;
            Vector2 dir = line / Mathf.Max(0.0001f, len);

            float prevT = -9999999.0f;
            for (int i = start; i < nextExclusive; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null)
                    return false;

                if (i > start)
                {
                    WallSavedMapSpriteV6LikeOriginal prev = sprites[i - 1];
                    float gap = new Vector2(s.X - prev.X, s.Y - prev.Y).magnitude;
                    if (gap < C2WallObjectsV132FenceMinStepOriginal || gap > C2WallObjectsV132FenceMaxStepOriginal)
                        return false;
                }

                Vector2 p = new Vector2(s.X, s.Y);
                float t = Vector2.Dot(p - p0, dir);
                Vector2 projected = p0 + dir * t;
                float perp = (p - projected).magnitude;
                if (perp > C2WallObjectsV153SideLineMaxPerpErrorLikeOriginal)
                    return false;
                if (i > start && t + 8.0f < prevT)
                    return false;
                prevT = t;
            }

            return true;
        }

        private static bool IsWallFenceTopBottomSideV152LikeOriginal(Vector2 originalLine)
        {
            // V152 audit rule: original map-axis slope chooses the already-authored frame pair.
            // Do not use Unity world X/Z here: terrain mirroring and odd-column offsets can invert the visual side.
            return (originalLine.x * originalLine.y) >= 0.0f;
        }

        private static int GetWallFencePairFamilyMaskV152LikeOriginal(int spriteIndex)
        {
            int mask = 0;
            for (int f = 0; f < C2WallObjectsV152FencePairsLikeOriginal.Length; f++)
            {
                WallFencePairV152LikeOriginal pair = C2WallObjectsV152FencePairsLikeOriginal[f];
                if (spriteIndex == pair.TopBottom || spriteIndex == pair.LeftRight)
                    mask |= (1 << f);
            }
            return mask;
        }

        private static int SelectDominantWallFencePairSpriteForSideV153LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            int familyMaskV152,
            bool topBottomSide)
        {
            if (sprites == null || start < 0 || end <= start || familyMaskV152 == 0)
                return -1;

            int[] familyVotes = new int[C2WallObjectsV152FencePairsLikeOriginal.Length];
            for (int i = start; i < end && i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null)
                    continue;

                for (int f = 0; f < C2WallObjectsV152FencePairsLikeOriginal.Length; f++)
                {
                    if ((familyMaskV152 & (1 << f)) == 0)
                        continue;

                    WallFencePairV152LikeOriginal pair = C2WallObjectsV152FencePairsLikeOriginal[f];
                    if (s.SpriteIndex == pair.TopBottom || s.SpriteIndex == pair.LeftRight)
                        familyVotes[f]++;
                }
            }

            int bestFamily = -1;
            int bestVotes = -1;
            for (int f = 0; f < familyVotes.Length; f++)
            {
                if ((familyMaskV152 & (1 << f)) == 0)
                    continue;
                if (familyVotes[f] > bestVotes)
                {
                    bestVotes = familyVotes[f];
                    bestFamily = f;
                }
            }

            if (bestFamily < 0)
                return -1;

            WallFencePairV152LikeOriginal best = C2WallObjectsV152FencePairsLikeOriginal[bestFamily];
            return topBottomSide ? best.TopBottom : best.LeftRight;
        }

        private static string BuildWallFenceSideLineSpriteSummaryV150LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> sprites, int start, int end)
        {
            if (sprites == null || start < 0 || end <= start || start >= sprites.Count)
                return "none";

            var ids = new List<int>();
            for (int i = start; i < end && i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null)
                    continue;
                if (!ids.Contains(s.SpriteIndex))
                    ids.Add(s.SpriteIndex);
            }

            var parts = new List<string>();
            for (int i = 0; i < ids.Count; i++)
                parts.Add("W" + ids[i].ToString(CultureInfo.InvariantCulture));
            return parts.Count > 0 ? string.Join(",", parts.ToArray()) : "none";
        }

        private Mesh BuildIdenticalWL2DFenceLineRootMeshV144LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            WallSpriteDescV1LikeOriginal desc,
            Texture2D tex)
        {
            Material[] unusedMaterials;
            int familyMaskV152 = GetWallFencePairFamilyMaskForRunV152LikeOriginal(sprites, start, end);
            return BuildSideLineWL2DFenceLineRootMeshV150LikeOriginal(sprites, start, end, familyMaskV152, null, null, out unusedMaterials);
        }


        private struct WallFencePairV152LikeOriginal
        {
            public int TopBottom;
            public int LeftRight;

            public WallFencePairV152LikeOriginal(int topBottom, int leftRight)
            {
                TopBottom = topBottom;
                LeftRight = leftRight;
            }
        }

        private static readonly WallFencePairV152LikeOriginal[] C2WallObjectsV152FencePairsLikeOriginal =
        {
            new WallFencePairV152LikeOriginal(74, 70),
            new WallFencePairV152LikeOriginal(59, 58),
            new WallFencePairV152LikeOriginal(1, 0),
            new WallFencePairV152LikeOriginal(5, 4),
            new WallFencePairV152LikeOriginal(3, 4),
            new WallFencePairV152LikeOriginal(7, 6),
        };

        private static bool TryResolveWallFencePairSpriteV152LikeOriginal(
            int sourceSpriteIndex,
            Vector2 line,
            out int resolvedSpriteIndex,
            out string orientation)
        {
            resolvedSpriteIndex = sourceSpriteIndex;
            orientation = "none";

            for (int i = 0; i < C2WallObjectsV152FencePairsLikeOriginal.Length; i++)
            {
                WallFencePairV152LikeOriginal pair = C2WallObjectsV152FencePairsLikeOriginal[i];
                if (sourceSpriteIndex != pair.TopBottom && sourceSpriteIndex != pair.LeftRight)
                    continue;

                // The Python reference that finally matched the user's visual test is:
                //   top/bottom -> frame_0074, 0059, 0001, 0005, 0003, 0007
                //   right/left -> frame_0070, 0058, 0000, 0004, 0004, 0006
                // no flip, no rotation.
                //
                // On the original WL map axes these two isometric families appear as two diagonal slopes:
                //   dx*dy >= 0  => top/bottom family
                //   dx*dy <  0  => right/left family
                bool topBottom = (line.x * line.y) >= 0.0f;
                resolvedSpriteIndex = topBottom ? pair.TopBottom : pair.LeftRight;
                orientation = topBottom ? "topBottom" : "leftRight";
                return true;
            }

            return false;
        }

        private static bool IsKnownWallFencePairSpriteV152LikeOriginal(int spriteIndex)
        {
            for (int i = 0; i < C2WallObjectsV152FencePairsLikeOriginal.Length; i++)
            {
                WallFencePairV152LikeOriginal pair = C2WallObjectsV152FencePairsLikeOriginal[i];
                if (spriteIndex == pair.TopBottom || spriteIndex == pair.LeftRight)
                    return true;
            }

            return false;
        }

        private static int GetWallFencePairFamilyMaskForRunV152LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> sprites, int start, int end)
        {
            int mask = 0;
            if (sprites == null || start < 0 || end <= start)
                return mask;

            for (int i = start; i < end && i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null)
                    continue;

                int spriteMask = GetWallFencePairFamilyMaskV152LikeOriginal(s.SpriteIndex);
                if (spriteMask == 0)
                    continue;

                mask = mask == 0 ? spriteMask : (mask & spriteMask);
            }

            return mask;
        }

        private sealed class WallOriginalEdgeV157LikeOriginal
        {
            public float dx;
            public float dy;
            public float dz;
            public float Fi;
            public float DFi;
            public int Id;
        }

        private sealed class OneWallElementV157LikeOriginal
        {
            public WallSpriteDescV1LikeOriginal SpriteDesc;
            public float Scale = 1.0f;
            public int Rotation;
            public int dz;
            public int Usage = 1;
            public int AssociateWithUnit;
            public readonly List<WallOriginalEdgeV157LikeOriginal> LeftEdges = new List<WallOriginalEdgeV157LikeOriginal>();
            public readonly List<WallOriginalEdgeV157LikeOriginal> RightEdges = new List<WallOriginalEdgeV157LikeOriginal>();
        }

        private sealed class WallTypeDescriptionV157LikeOriginal
        {
            public string Name = "WL2D_FENCE_SYNTHETIC_FROM_WALLS_RSR";
            public float GlobalScale = 1.0f;
            public int MinWallHeight;
            public readonly List<OneWallElementV157LikeOriginal> Elements = new List<OneWallElementV157LikeOriginal>();
        }

        private sealed class OneWallEdgeV158LikeOriginal
        {
            public int EdgeID;
            public int x;
            public int y;
            public int z;
            public bool Dead;
            public OneWallLineV158LikeOriginal In;
            public OneWallLineV158LikeOriginal Out;
            public readonly List<OneWallPointV157LikeOriginal> Points = new List<OneWallPointV157LikeOriginal>();
        }

        private sealed class OneWallLineV158LikeOriginal
        {
            public int StartEdge;
            public int FinalEdge;
            public bool Dead;
            public int WallType;
            public OneWallEdgeV158LikeOriginal Start;
            public OneWallEdgeV158LikeOriginal Final;
            public readonly List<OneWallPointV157LikeOriginal> Points = new List<OneWallPointV157LikeOriginal>();
        }

        private sealed class OneWallsSystemV158LikeOriginal
        {
            public readonly List<OneWallEdgeV158LikeOriginal> Edges = new List<OneWallEdgeV158LikeOriginal>();
            public readonly List<OneWallLineV158LikeOriginal> Lines = new List<OneWallLineV158LikeOriginal>();

            public void FillTempFieldsLikeOriginal()
            {
                for (int i = 0; i < Edges.Count; i++)
                {
                    if (Edges[i] == null) continue;
                    Edges[i].In = null;
                    Edges[i].Out = null;
                }

                for (int i = 0; i < Lines.Count; i++)
                {
                    OneWallLineV158LikeOriginal line = Lines[i];
                    if (line == null || line.Dead)
                        continue;
                    line.Start = null;
                    line.Final = null;
                    for (int j = 0; j < Edges.Count; j++)
                    {
                        OneWallEdgeV158LikeOriginal edge = Edges[j];
                        if (edge == null || edge.Dead)
                            continue;
                        if (line.FinalEdge == edge.EdgeID)
                        {
                            line.Final = edge;
                            edge.In = line;
                        }
                        if (line.StartEdge == edge.EdgeID)
                        {
                            line.Start = edge;
                            edge.Out = line;
                        }
                    }
                }
            }
        }

        private sealed class OneWallPointV157LikeOriginal
        {
            public OneWallElementV157LikeOriginal Type;
            public int SourceSavedIndex;
            public int ResolvedSpriteIndex;
            public float x;
            public float y;
            public float z;
            public float x_in;
            public float y_in;
            public float x_out;
            public float y_out;
            public float ScaleO = 1.0f;
            public float ScaleP = 1.0f;
            public float ScaleZ = 1.0f;
            public float Angle;
            public Matrix4x4 M4 = Matrix4x4.identity;
        }

        private static float SqNormaV157LikeOriginal(float x, float y)
        {
            return Mathf.Sqrt(x * x + y * y);
        }

        private static OneWallElementV157LikeOriginal CreateOneWallElementFromSpriteDescV157LikeOriginal(WallSpriteDescV1LikeOriginal desc, int usage)
        {
            if (desc == null)
                return null;

            var e = new OneWallElementV157LikeOriginal
            {
                SpriteDesc = desc,
                Usage = desc.ElementUsageV160 >= 0 ? desc.ElementUsageV160 : usage,
                Scale = Mathf.Max(0.0001f, desc.ElementScaleV160),
                Rotation = desc.ElementRotationV160,
                dz = desc.ElementDzV160,
                AssociateWithUnit = 0
            };

            for (int i = 0; i < desc.LeftEdges.Count; i++)
            {
                WallEdgePointV1LikeOriginal src = desc.LeftEdges[i];
                e.LeftEdges.Add(new WallOriginalEdgeV157LikeOriginal { dx = src.X, dy = src.Y, dz = 0.0f, Id = src.Id });
            }

            for (int i = 0; i < desc.RightEdges.Count; i++)
            {
                WallEdgePointV1LikeOriginal src = desc.RightEdges[i];
                e.RightEdges.Add(new WallOriginalEdgeV157LikeOriginal { dx = src.X, dy = src.Y, dz = 0.0f, Id = src.Id });
            }

            return e;
        }

        private static Matrix4x4 BuildOneWallPointMatrixV157LikeOriginal(OneWallPointV157LikeOriginal p, WallTypeDescriptionV157LikeOriginal wt, float slopeSource, bool useXZSlope)
        {
            if (p == null || p.Type == null || wt == null)
                return Matrix4x4.identity;

            // Original Matrix4D is used as a row-vector transform in this file:
            // x' = x*e00 + y*e10 + z*e20 + e30.
            // Port the exact 3DWalls.cpp order:
            //   OWP->M4.scaling(ScaleO,ScaleP,ScaleZ);
            //   OWP->M4.e02/e12 = local slope;
            //   M4.srt(Type->Scale*WT->GlobalScale, oZ, Angle, (x,y,z));
            //   OWP->M4 *= M4;
            //   OWP->M4 *= GetSkewTM();
            Matrix4x4 scaleAndSlope = Matrix4x4.identity;
            scaleAndSlope.m00 = p.ScaleO;
            scaleAndSlope.m11 = p.ScaleP;
            scaleAndSlope.m22 = p.ScaleZ;
            if (useXZSlope)
                scaleAndSlope.m02 = slopeSource;
            else
                scaleAndSlope.m12 = slopeSource;

            float s = p.Type.Scale * wt.GlobalScale;
            float a = p.Angle * Mathf.PI / 128.0f;
            float c = Mathf.Cos(a);
            float sn = Mathf.Sin(a);

            Matrix4x4 srt = Matrix4x4.identity;
            srt.m00 = c * s;
            srt.m01 = sn * s;
            srt.m10 = -sn * s;
            srt.m11 = c * s;
            srt.m22 = s;
            srt.m30 = p.x;
            srt.m31 = p.y;
            srt.m32 = p.z;

            Matrix4x4 skew = BuildSkewMatrix4DLikeOriginalV159();

            return MultiplyMatrix4DRowVectorOrderV159LikeOriginal(
                MultiplyMatrix4DRowVectorOrderV159LikeOriginal(scaleAndSlope, srt),
                skew);
        }

        private static Matrix4x4 BuildSkewMatrix4DLikeOriginalV159()
        {
            // Scape3D.cpp:
            // const Matrix4D c_SkewTM( 1,0,0,0, 0,1,0,0, 0,-0.5,cos(pi/6),0, 0,0,0,1 )
            // SkewPt(x,y,z) = (x, y - 0.5*z, z*cos(pi/6)).
            Matrix4x4 m = Matrix4x4.identity;
            m.m20 = 0.0f;
            m.m21 = -0.5f;
            m.m22 = 0.8660254037844386f;
            return m;
        }

        private static Matrix4x4 MultiplyMatrix4DRowVectorOrderV159LikeOriginal(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 r = new Matrix4x4();

            r.m00 = a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20 + a.m03 * b.m30;
            r.m01 = a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21 + a.m03 * b.m31;
            r.m02 = a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22 + a.m03 * b.m32;
            r.m03 = a.m00 * b.m03 + a.m01 * b.m13 + a.m02 * b.m23 + a.m03 * b.m33;

            r.m10 = a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20 + a.m13 * b.m30;
            r.m11 = a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21 + a.m13 * b.m31;
            r.m12 = a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22 + a.m13 * b.m32;
            r.m13 = a.m10 * b.m03 + a.m11 * b.m13 + a.m12 * b.m23 + a.m13 * b.m33;

            r.m20 = a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20 + a.m23 * b.m30;
            r.m21 = a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21 + a.m23 * b.m31;
            r.m22 = a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22 + a.m23 * b.m32;
            r.m23 = a.m20 * b.m03 + a.m21 * b.m13 + a.m22 * b.m23 + a.m23 * b.m33;

            r.m30 = a.m30 * b.m00 + a.m31 * b.m10 + a.m32 * b.m20 + a.m33 * b.m30;
            r.m31 = a.m30 * b.m01 + a.m31 * b.m11 + a.m32 * b.m21 + a.m33 * b.m31;
            r.m32 = a.m30 * b.m02 + a.m31 * b.m12 + a.m32 * b.m22 + a.m33 * b.m32;
            r.m33 = a.m30 * b.m03 + a.m31 * b.m13 + a.m32 * b.m23 + a.m33 * b.m33;

            return r;
        }

        private static void RotateWallEdgePointV158LikeOriginal(WallOriginalEdgeV157LikeOriginal edge, float angle128, float scale, float globalScale, out float rx, out float ry)
        {
            if (edge == null)
            {
                rx = 0.0f;
                ry = 0.0f;
                return;
            }

            float lx = edge.dx * scale * globalScale;
            float ly = edge.dy * scale * globalScale;
            float a = angle128 * Mathf.PI / 128.0f;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            rx = c * lx - s * ly;
            ry = c * ly + s * lx;
        }

        private OneWallPointV157LikeOriginal BuildVirtualOneWallEdgePointV158LikeOriginal(
            OneWallElementV157LikeOriginal type,
            WallTypeDescriptionV157LikeOriginal wt,
            Vector2 desiredConnector,
            bool connectorIsOut,
            float baseAngle,
            out string audit)
        {
            audit = string.Empty;
            if (type == null || wt == null)
                return null;

            WallOriginalEdgeV157LikeOriginal connector = connectorIsOut
                ? (type.RightEdges.Count > 0 ? type.RightEdges[0] : null)
                : (type.LeftEdges.Count > 0 ? type.LeftEdges[0] : null);

            RotateWallEdgePointV158LikeOriginal(connector, baseAngle, type.Scale, wt.GlobalScale, out float cx, out float cy);
            float centerX = desiredConnector.x - cx;
            float centerY = desiredConnector.y - cy;

            var p = new OneWallPointV157LikeOriginal
            {
                Type = type,
                SourceSavedIndex = -1,
                ResolvedSpriteIndex = type.SpriteDesc != null ? type.SpriteDesc.SpriteIndex : -1,
                x = centerX,
                y = centerY,
                z = SampleWallHeightOriginalXYV1LikeOriginal(centerX, centerY),
                ScaleO = 1.0f,
                ScaleP = 1.0f,
                ScaleZ = 1.0f,
                Angle = baseAngle
            };

            RotateWallEdgePointV158LikeOriginal(type.LeftEdges.Count > 0 ? type.LeftEdges[0] : null, baseAngle, type.Scale, wt.GlobalScale, out float xin, out float yin);
            RotateWallEdgePointV158LikeOriginal(type.RightEdges.Count > 0 ? type.RightEdges[0] : null, baseAngle, type.Scale, wt.GlobalScale, out float xout, out float yout);
            p.x_in = p.x + xin;
            p.y_in = p.y + yin;
            p.x_out = p.x + xout;
            p.y_out = p.y + yout;

            p.Angle += type.Rotation;
            p.z += type.dz;
            p.M4 = BuildOneWallPointMatrixV157LikeOriginal(p, wt, 0.0f, type.Rotation == 64 || type.Rotation == 64 + 128);

            audit = (connectorIsOut ? "out" : "in") + " desired=(" +
                    desiredConnector.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                    desiredConnector.y.ToString("0.###", CultureInfo.InvariantCulture) + ") center=(" +
                    p.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                    p.y.ToString("0.###", CultureInfo.InvariantCulture) + ") actualIn=(" +
                    p.x_in.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                    p.y_in.ToString("0.###", CultureInfo.InvariantCulture) + ") actualOut=(" +
                    p.x_out.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                    p.y_out.ToString("0.###", CultureInfo.InvariantCulture) + ")";
            return p;
        }


        private static OneWallElementV157LikeOriginal ConvertWallElementXmlToOneWallElementV170LikeOriginal(WallElementXmlV160LikeOriginal src)
        {
            if (src == null || src.BoundSpriteDescV161 == null)
                return null;

            OneWallElementV157LikeOriginal e = CreateOneWallElementFromSpriteDescV157LikeOriginal(src.BoundSpriteDescV161, src.Usage);
            if (e == null)
                return null;

            e.Scale = Mathf.Max(0.0001f, src.Scale);
            e.Rotation = src.Rotation;
            e.dz = src.dz;
            e.Usage = src.Usage;
            e.AssociateWithUnit = src.AssociateWithUnit;

            e.LeftEdges.Clear();
            for (int i = 0; i < src.LeftEdges.Count; i++)
            {
                WallEdgePointV1LikeOriginal p = src.LeftEdges[i];
                e.LeftEdges.Add(new WallOriginalEdgeV157LikeOriginal { dx = p.X, dy = p.Y, dz = 0.0f, Id = p.Id });
            }

            e.RightEdges.Clear();
            for (int i = 0; i < src.RightEdges.Count; i++)
            {
                WallEdgePointV1LikeOriginal p = src.RightEdges[i];
                e.RightEdges.Add(new WallOriginalEdgeV157LikeOriginal { dx = p.X, dy = p.Y, dz = 0.0f, Id = p.Id });
            }

            return e;
        }

        private static bool IsWallSpriteInFencePairFamilyV170LikeOriginal(int spriteIndex, int familyMaskV152)
        {
            if ((familyMaskV152 & 0x2) != 0 && (spriteIndex == 58 || spriteIndex == 59))
                return true;
            if ((familyMaskV152 & 0x1) != 0 && (spriteIndex == 70 || spriteIndex == 74))
                return true;
            return false;
        }

        private static bool TryBuildRealWallTypeCycleV170LikeOriginal(
            WallSpriteCatalogV1LikeOriginal catalog,
            WallSpriteDescV1LikeOriginal centralDesc,
            int familyMaskV152,
            out WallTypeDescriptionV157LikeOriginal wt,
            out OneWallElementV157LikeOriginal leType,
            out OneWallElementV157LikeOriginal eType,
            out OneWallElementV157LikeOriginal reType,
            out string audit)
        {
            wt = null;
            leType = null;
            eType = null;
            reType = null;
            audit = "no_catalog";

            if (catalog == null || centralDesc == null || catalog.WallTypesV160 == null || catalog.WallTypesV160.Count == 0)
                return false;

            for (int wi = 0; wi < catalog.WallTypesV160.Count; wi++)
            {
                WallTypeDescriptionXmlV160LikeOriginal srcWt = catalog.WallTypesV160[wi];
                if (srcWt == null || srcWt.Elements == null || srcWt.Elements.Count == 0)
                    continue;

                WallElementXmlV160LikeOriginal xmlLeft = null;
                WallElementXmlV160LikeOriginal xmlCenter = null;
                WallElementXmlV160LikeOriginal xmlRight = null;

                for (int ei = 0; ei < srcWt.Elements.Count; ei++)
                {
                    WallElementXmlV160LikeOriginal e = srcWt.Elements[ei];
                    if (e == null || e.BoundSpriteDescV161 == null)
                        continue;

                    int si = e.BoundSpriteDescV161.SpriteIndex;
                    bool familyOk = IsWallSpriteInFencePairFamilyV170LikeOriginal(si, familyMaskV152);
                    bool centralOk = si == centralDesc.SpriteIndex || familyOk;
                    if (!centralOk)
                        continue;

                    if (e.Usage == 0 && xmlLeft == null)
                        xmlLeft = e;
                    else if (e.Usage == 1 && xmlCenter == null)
                        xmlCenter = e;
                    else if (e.Usage == 2 && xmlRight == null)
                        xmlRight = e;
                }

                if (xmlCenter == null)
                    continue;

                wt = new WallTypeDescriptionV157LikeOriginal
                {
                    Name = string.IsNullOrWhiteSpace(srcWt.Name) ? "WallTypeDescription_XML_V170" : srcWt.Name,
                    GlobalScale = srcWt.GlobalScale,
                    MinWallHeight = srcWt.MinWallHeight
                };

                eType = ConvertWallElementXmlToOneWallElementV170LikeOriginal(xmlCenter);
                leType = ConvertWallElementXmlToOneWallElementV170LikeOriginal(xmlLeft);
                reType = ConvertWallElementXmlToOneWallElementV170LikeOriginal(xmlRight);
                if (eType == null)
                    continue;

                wt.Elements.Add(eType);
                if (leType != null) wt.Elements.Add(leType);
                if (reType != null) wt.Elements.Add(reType);

                audit = "V170_REAL_WallType_cycle_USED wt=" + wi.ToString(CultureInfo.InvariantCulture) +
                        " center=W" + eType.SpriteDesc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                        " left=" + (leType != null && leType.SpriteDesc != null ? "W" + leType.SpriteDesc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "none") +
                        " right=" + (reType != null && reType.SpriteDesc != null ? "W" + reType.SpriteDesc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "none");
                return true;
            }

            audit = "V170_NO_REAL_WallType_cycle central=W" + centralDesc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                    " wallTypes=" + catalog.WallTypesV160.Count.ToString(CultureInfo.InvariantCulture);
            return false;
        }

        private OneWallLineV158LikeOriginal BuildVirtualOneWallsSystemGraphFromSavedWLRunV158LikeOriginal(
            Vector2 desiredX0Y0,
            Vector2 desiredX1Y1,
            OneWallElementV157LikeOriginal edgeType,
            WallTypeDescriptionV157LikeOriginal wt,
            float baseAngle,
            out string audit)
        {
            audit = string.Empty;
            if (!C2WallObjectsV158BuildVirtualOneWallsGraphForSavedWL2DFenceLikeOriginal || edgeType == null || wt == null)
                return null;

            var sys = new OneWallsSystemV158LikeOriginal();
            var startEdge = new OneWallEdgeV158LikeOriginal { EdgeID = 1, Dead = false };
            var finalEdge = new OneWallEdgeV158LikeOriginal { EdgeID = 2, Dead = false };
            var line = new OneWallLineV158LikeOriginal { StartEdge = 1, FinalEdge = 2, Dead = false, WallType = 0 };

            OneWallPointV157LikeOriginal startPoint = BuildVirtualOneWallEdgePointV158LikeOriginal(edgeType, wt, desiredX0Y0, connectorIsOut: true, baseAngle: baseAngle, out string startAudit);
            OneWallPointV157LikeOriginal finalPoint = BuildVirtualOneWallEdgePointV158LikeOriginal(edgeType, wt, desiredX1Y1, connectorIsOut: false, baseAngle: baseAngle, out string finalAudit);
            if (startPoint == null || finalPoint == null)
                return null;

            startEdge.x = Mathf.RoundToInt(startPoint.x);
            startEdge.y = Mathf.RoundToInt(startPoint.y);
            startEdge.z = Mathf.RoundToInt(startPoint.z);
            finalEdge.x = Mathf.RoundToInt(finalPoint.x);
            finalEdge.y = Mathf.RoundToInt(finalPoint.y);
            finalEdge.z = Mathf.RoundToInt(finalPoint.z);
            startEdge.Points.Add(startPoint);
            finalEdge.Points.Add(finalPoint);

            sys.Edges.Add(startEdge);
            sys.Edges.Add(finalEdge);
            sys.Lines.Add(line);
            sys.FillTempFieldsLikeOriginal();

            audit = "V158_graph edges=2 lines=1 start{" + startAudit + "} final{" + finalAudit + "}";
            return line.Start != null && line.Final != null ? line : null;
        }


private List<OneWallPointV157LikeOriginal> ReCreateOneWallsSystemLineFromSavedWLRunV157LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            int familyMaskV152,
            WallSpriteCatalogV1LikeOriginal catalog,
            out string audit)
        {
            audit = string.Empty;
            var points = new List<OneWallPointV157LikeOriginal>();
            if (!C2WallObjectsV157UseOriginalOneWallsSystemPortForWL2DFenceLikeOriginal ||
                sprites == null || catalog == null || start < 0 || end <= start || end > sprites.Count)
                return points;

            int savedCount = end - start;
            if (savedCount < 2)
                return points;

            WallSavedMapSpriteV6LikeOriginal first = sprites[start];
            WallSavedMapSpriteV6LikeOriginal last = sprites[end - 1];
            if (first == null || last == null)
                return points;

            Vector2 firstCenter = new Vector2(first.X, first.Y);
            Vector2 lastCenter = new Vector2(last.X, last.Y);
            Vector2 savedCenterLine = lastCenter - firstCenter;
            float savedCenterLen = savedCenterLine.magnitude;
            if (savedCenterLen < C2WallObjectsV132FenceMinStepOriginal)
                return points;

            Vector2 savedStep = savedCenterLine / Mathf.Max(1, savedCount - 1);

            // Saved TRE2/WL carries finished centers, while original ReCreate uses Start->x_out and Final->x_in.
            // Rebuild the virtual connector endpoints by extending the first/last center by half a saved step.
            Vector2 x0y0 = firstCenter - savedStep * 0.5f;
            Vector2 x1y1 = lastCenter + savedStep * 0.5f;
            Vector2 line = x1y1 - x0y0;
            float tdist = line.magnitude;
            if (tdist < C2WallObjectsV132FenceMinStepOriginal)
                return points;

            bool topBottom = IsWallFenceTopBottomSideV152LikeOriginal(line);
            int dominantSprite = SelectDominantWallFencePairSpriteForSideV153LikeOriginal(sprites, start, end, familyMaskV152, topBottom);
            if (dominantSprite < 0)
                dominantSprite = first.SpriteIndex;
            if (!TryResolveWallFencePairSpriteV152LikeOriginal(dominantSprite, line, out int centralSpriteIndex, out string centralOrientation))
                centralSpriteIndex = dominantSprite;

            if (!catalog.ByIndex.TryGetValue(centralSpriteIndex, out WallSpriteDescV1LikeOriginal centralDesc) || centralDesc == null)
            {
                if (!catalog.ByIndex.TryGetValue(first.SpriteIndex, out centralDesc) || centralDesc == null)
                    return points;
                centralSpriteIndex = centralDesc.SpriteIndex;
            }

            string realWtAuditV161 = "V171_synthetic_saved_WL_WALS2D_WALLS_g16_no_real_WallType_cycle";
            OneWallElementV157LikeOriginal leType = null;
            OneWallElementV157LikeOriginal reType = null;
            WallTypeDescriptionV157LikeOriginal wt = null;
            OneWallElementV157LikeOriginal eType = null;
            bool useRealWallTypeCycleV161 = false;

            if (C2WallObjectsV161UseRealWallTypeElementCycleForOneWallsSystemLikeOriginal)
            {
                useRealWallTypeCycleV161 = TryBuildRealWallTypeCycleV170LikeOriginal(catalog, centralDesc, familyMaskV152, out wt, out leType, out eType, out reType, out realWtAuditV161);
                if (useRealWallTypeCycleV161)
                    _c2WallObjectsV161RealWallTypeCycleUsedLikeOriginal++;
            }

            if (!useRealWallTypeCycleV161)
            {
                if (C2WallObjectsV170UseRealWallTypeCycleOrRejectLikeOriginal)
                {
                    audit = "V170_REJECT_NO_REAL_WallType_cycle central=W" + centralSpriteIndex.ToString(CultureInfo.InvariantCulture) + " realWT={" + realWtAuditV161 + "}";
                    return points;
                }

                wt = new WallTypeDescriptionV157LikeOriginal { GlobalScale = 1.0f, MinWallHeight = 0 };
                eType = CreateOneWallElementFromSpriteDescV157LikeOriginal(centralDesc, 1);
                if (eType == null)
                    return points;
                wt.Elements.Add(eType);
            }

            if (wt == null || eType == null)
                return points;

            string graphAuditV158 = "V158_graph_disabled";
            float baseAngle = Mathf.Atan2((x1y1.y - x0y0.y) / 1000.0f, (x1y1.x - x0y0.x) / 1000.0f) * 128.0f / Mathf.PI - 64.0f;
            OneWallLineV158LikeOriginal graphLineV158 = BuildVirtualOneWallsSystemGraphFromSavedWLRunV158LikeOriginal(x0y0, x1y1, eType, wt, baseAngle, out graphAuditV158);
            if (C2WallObjectsV158UseEdgeConnectorInOutForLineEndpointsLikeOriginal &&
                graphLineV158 != null && graphLineV158.Start != null && graphLineV158.Final != null &&
                graphLineV158.Start.Points.Count > 0 && graphLineV158.Final.Points.Count > 0)
            {
                OneWallPointV157LikeOriginal sp0 = graphLineV158.Start.Points[0];
                OneWallPointV157LikeOriginal fp0 = graphLineV158.Final.Points[0];
                x0y0 = new Vector2(sp0.x_out, sp0.y_out);
                x1y1 = new Vector2(fp0.x_in, fp0.y_in);
                line = x1y1 - x0y0;
                tdist = line.magnitude;
                if (tdist < C2WallObjectsV132FenceMinStepOriginal)
                    return points;
                baseAngle = Mathf.Atan2((x1y1.y - x0y0.y) / 1000.0f, (x1y1.x - x0y0.x) / 1000.0f) * 128.0f / Mathf.PI - 64.0f;
            }

            float ex0 = eType.LeftEdges.Count > 0 ? eType.LeftEdges[0].dx : 0.0f;
            float ey0 = eType.LeftEdges.Count > 0 ? eType.LeftEdges[0].dy : 0.0f;
            float ex1 = eType.RightEdges.Count > 0 ? eType.RightEdges[0].dx : Mathf.Max(16.0f, centralDesc.Width * 0.65f);
            float ey1 = eType.RightEdges.Count > 0 ? eType.RightEdges[0].dy : 0.0f;
            float esize = SqNormaV157LikeOriginal(ex1 - ex0, ey1 - ey0) * eType.Scale * wt.GlobalScale;
            if (esize <= 0.0001f)
                esize = EstimateWallElementLengthV1LikeOriginal(centralDesc);
            if (esize <= 0.0001f)
                return points;

            int ne = (int)(tdist / esize + 0.5f);
            if (ne <= 0)
                return points;

            float hScale = (tdist / ne / esize) * 1.05f;
            float z0 = graphLineV158 != null && graphLineV158.Start != null && graphLineV158.Start.Points.Count > 0
                ? graphLineV158.Start.Points[0].z
                : SampleWallHeightOriginalXYV1LikeOriginal(x0y0.x, x0y0.y);
            float z1 = graphLineV158 != null && graphLineV158.Final != null && graphLineV158.Final.Points.Count > 0
                ? graphLineV158.Final.Points[0].z
                : SampleWallHeightOriginalXYV1LikeOriginal(x1y1.x, x1y1.y);
            float slopeDenom = SqNormaV157LikeOriginal(x1y1.x - x0y0.x, x1y1.y - x0y0.y);
            float wholeLineSlope = slopeDenom > 0.0001f ? (z1 - z0) / slopeDenom : 0.0f;

            for (int j = 0; j < ne; j++)
            {
                float t = (j + 0.5f) / ne;
                Vector2 anchor = x0y0 + line * t;
                int sourceIndex = Mathf.Clamp(start + Mathf.FloorToInt(t * savedCount), start, end - 1);
                WallSavedMapSpriteV6LikeOriginal source = sprites[sourceIndex] ?? first;

                int resolvedSpriteIndex;
                OneWallElementV157LikeOriginal celm = eType;
                if (useRealWallTypeCycleV161 && leType != null && reType != null)
                {
                    switch (j % 3)
                    {
                        case 0: celm = leType; break;
                        case 1: celm = eType; break;
                        case 2: celm = reType; break;
                    }
                    resolvedSpriteIndex = celm != null && celm.SpriteDesc != null ? celm.SpriteDesc.SpriteIndex : centralSpriteIndex;
                }
                else
                {
                    if (!TryResolveWallFencePairSpriteV152LikeOriginal(source.SpriteIndex, line, out resolvedSpriteIndex, out _))
                        resolvedSpriteIndex = source.SpriteIndex;

                    if (!catalog.ByIndex.TryGetValue(resolvedSpriteIndex, out WallSpriteDescV1LikeOriginal pointDesc) || pointDesc == null)
                    {
                        resolvedSpriteIndex = centralSpriteIndex;
                        pointDesc = centralDesc;
                    }

                    celm = CreateOneWallElementFromSpriteDescV157LikeOriginal(pointDesc, 1);
                }

                if (celm == null)
                    continue;

                float zs = SampleWallHeightOriginalXYV1LikeOriginal(anchor.x, anchor.y);
                if (zs < wt.MinWallHeight)
                    zs = wt.MinWallHeight;
                float zLine = Mathf.Lerp(z0, z1, t);
                float z = (zLine * 2.0f + zs) / 3.0f;

                var owp = new OneWallPointV157LikeOriginal
                {
                    Type = celm,
                    SourceSavedIndex = sourceIndex,
                    ResolvedSpriteIndex = resolvedSpriteIndex,
                    x = anchor.x,
                    y = anchor.y,
                    z = z,
                    ScaleO = 1.0f,
                    ScaleP = hScale,
                    ScaleZ = 1.0f,
                    Angle = baseAngle
                };

                if (owp.Type.Rotation == 64 || owp.Type.Rotation == 64 + 128)
                {
                    float tmp = owp.ScaleO;
                    owp.ScaleO = owp.ScaleP;
                    owp.ScaleP = tmp;
                }

                if (ne == 1)
                {
                    owp.Angle += owp.Type.Rotation;
                    owp.M4 = BuildOneWallPointMatrixV157LikeOriginal(owp, wt, wholeLineSlope, owp.Type.Rotation == 64 || owp.Type.Rotation == 64 + 128);
                }

                points.Add(owp);
            }

            if (C2WallObjectsV157UseSecondSmoothPassLikeOriginal && points.Count > 1)
            {
                int n = points.Count;
                float[] xs = new float[n];
                float[] ys = new float[n];
                float[] zs = new float[n];
                for (int j = 0; j < n; j++)
                {
                    OneWallPointV157LikeOriginal c = points[j];
                    OneWallPointV157LikeOriginal prev = j > 0 ? points[j - 1] : null;
                    OneWallPointV157LikeOriginal next = j < n - 1 ? points[j + 1] : null;
                    float xc = c.x;
                    float yc = c.y;
                    float zc = c.z;
                    float xp = prev != null ? prev.x : xc * 2.0f - next.x;
                    float yp = prev != null ? prev.y : yc * 2.0f - next.y;
                    float zp = prev != null ? prev.z : zc * 2.0f - next.z;
                    float xn = next != null ? next.x : xc * 2.0f - xp;
                    float yn = next != null ? next.y : yc * 2.0f - yp;
                    float zn = next != null ? next.z : zc * 2.0f - zp;
                    xs[j] = (xp + xn + xc * 2.0f) / 4.0f;
                    ys[j] = (yp + yn + yc * 2.0f) / 4.0f;
                    zs[j] = (zp + zn + zc * 2.0f) / 4.0f;
                }

                for (int j = 0; j < n; j++)
                {
                    OneWallPointV157LikeOriginal p = points[j];
                    p.x = xs[j];
                    p.y = ys[j];
                    p.z = zs[j];

                    OneWallPointV157LikeOriginal prev = j > 0 ? points[j - 1] : null;
                    OneWallPointV157LikeOriginal next = j < n - 1 ? points[j + 1] : null;
                    float xp = prev != null ? prev.x : p.x * 2.0f - next.x;
                    float yp = prev != null ? prev.y : p.y * 2.0f - next.y;
                    float zp = prev != null ? prev.z : p.z * 2.0f - next.z;
                    float xn = next != null ? next.x : p.x * 2.0f - xp;
                    float yn = next != null ? next.y : p.y * 2.0f - yp;
                    float zn = next != null ? next.z : p.z * 2.0f - zp;
                    float d = SqNormaV157LikeOriginal(xn - xp, yn - yp);
                    float localSlope = d > 0.0001f ? (zn - zp) / d : 0.0f;

                    p.Angle = baseAngle + (p.Type != null ? p.Type.Rotation : 0);
                    if (p.Type != null)
                        p.z += p.Type.dz;
                    p.M4 = BuildOneWallPointMatrixV157LikeOriginal(p, wt, localSlope, p.Type != null && (p.Type.Rotation == 64 || p.Type.Rotation == 64 + 128));
                }
            }

            audit = "x0=(" + x0y0.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + x0y0.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " x1=(" + x1y1.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + x1y1.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " Tdist=" + tdist.ToString("0.###", CultureInfo.InvariantCulture) +
                    " esize=" + esize.ToString("0.###", CultureInfo.InvariantCulture) +
                    " ne=" + ne.ToString(CultureInfo.InvariantCulture) +
                    " hScale=" + hScale.ToString("0.###", CultureInfo.InvariantCulture) +
                    " angle=" + baseAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                    " central=W" + centralSpriteIndex.ToString(CultureInfo.InvariantCulture) +
                    " orientation=" + centralOrientation +
                    " smooth=" + C2WallObjectsV157UseSecondSmoothPassLikeOriginal.ToString() +
                    " realWT={" + realWtAuditV161 + "}" +
                    " graph={" + graphAuditV158 + "}";
            return points;
        }

        private Mesh TryBuildOneWallsSystemModelIDLineRootMeshV159LikeOriginal(
            List<OneWallPointV157LikeOriginal> points,
            Material fallbackMaterial,
            out Material[] materials,
            out string audit)
        {
            materials = null;
            audit = string.Empty;

            if (!C2WallObjectsV159UseModelIDMatrix4DBackendForOneWallsSystemLineRootsLikeOriginal ||
                points == null || points.Count == 0)
            {
                audit = "disabled_or_empty";
                return null;
            }

            int missingModelPath = 0;
            int c2mLoadFailed = 0;
            int emittedPoints = 0;
            int emittedVerts = 0;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            var submeshTris = new List<List<int>>();
            var mats = new List<Material>();
            var matByModel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var loadAudits = new List<string>();

            for (int section = 0; section < points.Count; section++)
            {
                OneWallPointV157LikeOriginal point = points[section];
                if (point == null || point.Type == null || point.Type.SpriteDesc == null)
                    continue;

                WallSpriteDescV1LikeOriginal desc = point.Type.SpriteDesc;
                if (string.IsNullOrWhiteSpace(desc.ModelPath))
                {
                    missingModelPath++;
                    continue;
                }

                WallC2MParsedMeshV23LikeOriginal c2m = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out string loadAudit);
                if (c2m == null || c2m.Vertices == null || c2m.Vertices.Length == 0 ||
                    c2m.Triangles == null || c2m.Triangles.Length < 3)
                {
                    c2mLoadFailed++;
                    if (loadAudits.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                        loadAudits.Add("W" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " model='" + desc.ModelPath + "' failed={" + (loadAudit ?? string.Empty) + "}");
                    continue;
                }

                string matKey = (desc.ModelPath ?? string.Empty) + "|" + (c2m.TextureName ?? string.Empty) + "|" + (c2m.GPObj != null ? c2m.GPObj.FrameIdx.ToString(CultureInfo.InvariantCulture) : "-");
                int matIndex;
                if (!matByModel.TryGetValue(matKey, out matIndex))
                {
                    Texture2D modelTex = Texture2D.whiteTexture;
                    string materialSource = "white";
                    if (C2WallObjectsV42UseGPObjFrameTextureForC2MLikeOriginal && c2m.GPObj != null)
                    {
                        List<WallG16SquareV47LikeOriginal> gpSquaresIgnoredV159;
                        Texture2D gpTex = TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(c2m, out string gpSource, out gpSquaresIgnoredV159);
                        if (gpTex != null)
                        {
                            modelTex = gpTex;
                            materialSource = "GPObj " + gpSource;
                        }
                    }

                    if (modelTex == Texture2D.whiteTexture && !string.IsNullOrWhiteSpace(c2m.TextureName))
                    {
                        Texture2D txreTex = TryLoadWallC2MTXRETextureV48LikeOriginal(c2m, out string txreSource);
                        if (txreTex != null)
                        {
                            modelTex = txreTex;
                            materialSource = "TXRE " + txreSource;
                        }
                    }

                    Material mat = CreateWallC2MModelMaterialV26LikeOriginal(modelTex, desc);
                    if (mat == null)
                        mat = fallbackMaterial;
                    if (mat == null)
                    {
                        c2mLoadFailed++;
                        if (loadAudits.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                            loadAudits.Add("W" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " model='" + desc.ModelPath + "' failed_material source=" + materialSource);
                        continue;
                    }

                    matIndex = mats.Count;
                    matByModel[matKey] = matIndex;
                    mats.Add(mat);
                    submeshTris.Add(new List<int>());
                    if (loadAudits.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                        loadAudits.Add("W" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " model='" + desc.ModelPath + "' ok v=" +
                                       c2m.Vertices.Length.ToString(CultureInfo.InvariantCulture) + " i=" +
                                       c2m.Triangles.Length.ToString(CultureInfo.InvariantCulture) + " mat=" + materialSource);
                }

                int baseIndex = verts.Count;
                for (int vi = 0; vi < c2m.Vertices.Length; vi++)
                {
                    Vector3 local = c2m.Vertices[vi];

                    // Original: AddExtraHeightObject(OWP->x,OWP->y,OWP->Type->ModelID,&OWP->M4)
                    // then IMM->Render(Type->ModelID,&M4). This is the same per-vertex application:
                    // local model vertex -> original Matrix4D -> Unity terrain/world adapter.
                    Vector3 original = TransformOriginalMatrix4DPointV19LikeOriginal(point.M4, local);
                    Vector3 world = OriginalWallXYZToWorldV6LikeOriginal(original.x, original.y, original.z + desc.FixHeight);
                    verts.Add(world);

                    if (c2m.UV != null && c2m.UV.Length == c2m.Vertices.Length)
                        uvs.Add(c2m.UV[vi]);
                    else
                        uvs.Add(Vector2.zero);

                    if (c2m.Colors != null && c2m.Colors.Length == c2m.Vertices.Length)
                        colors.Add(c2m.Colors[vi]);
                    else
                        colors.Add(new Color32(255, 255, 255, 255));
                }

                List<int> tris = submeshTris[matIndex];
                for (int ti = 0; ti < c2m.Triangles.Length; ti++)
                    tris.Add(baseIndex + c2m.Triangles[ti]);

                emittedPoints++;
                emittedVerts += c2m.Vertices.Length;
            }

            // This backend is only valid when the whole line can be rendered through ModelID/Matrix4D.
            // Mixed model/card lines are intentionally rejected so the old card fallback remains visually safe.
            if (missingModelPath > 0 || c2mLoadFailed > 0 || emittedPoints != points.Count || verts.Count == 0 || mats.Count == 0)
            {
                audit = "ModelID_backend_not_used missingModelPath=" + missingModelPath.ToString(CultureInfo.InvariantCulture) +
                        " c2mLoadFailed=" + c2mLoadFailed.ToString(CultureInfo.InvariantCulture) +
                        " emittedPoints=" + emittedPoints.ToString(CultureInfo.InvariantCulture) +
                        "/" + points.Count.ToString(CultureInfo.InvariantCulture) +
                        " details=" + (loadAudits.Count > 0 ? string.Join(" ; ", loadAudits.ToArray()) : "none");
                return null;
            }

            Mesh mesh = new Mesh { name = "C2_WallFence_OneWallsSystem_ModelID_Matrix4D_LineMesh_V161_points" + points.Count.ToString(CultureInfo.InvariantCulture) };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.colors32 = colors.ToArray();
            mesh.subMeshCount = submeshTris.Count;
            for (int sm = 0; sm < submeshTris.Count; sm++)
                mesh.SetTriangles(submeshTris[sm].ToArray(), sm);
            mesh.RecalculateBounds();
            try { mesh.RecalculateNormals(); } catch { /* old C2M chunks can be degenerate; original fixed-function path tolerated that. */ }

            materials = mats.ToArray();
            audit = "ModelID_backend_USED original_AddExtraHeightObject_IMM_Render_semantics=True points=" +
                    emittedPoints.ToString(CultureInfo.InvariantCulture) +
                    " verts=" + emittedVerts.ToString(CultureInfo.InvariantCulture) +
                    " submeshes=" + submeshTris.Count.ToString(CultureInfo.InvariantCulture) +
                    " details=" + (loadAudits.Count > 0 ? string.Join(" ; ", loadAudits.ToArray()) : "none");
            return mesh;
        }


        private Mesh BuildSideLineWL2DFenceLineRootMeshV150LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            int familyMaskV152,
            WallSpriteCatalogV1LikeOriginal catalog,
            Material fallbackMaterial,
            out Material[] materials)
        {
            materials = null;
            if (sprites == null || catalog == null || start < 0 || end <= start || end > sprites.Count)
                return null;

            List<OneWallPointV157LikeOriginal> points = ReCreateOneWallsSystemLineFromSavedWLRunV157LikeOriginal(sprites, start, end, familyMaskV152, catalog, out string recreateAuditV157);
            _c2WallObjectsV157LastReCreateAuditLikeOriginal = recreateAuditV157;
            _c2WallObjectsV159LastModelIDAuditLikeOriginal = string.Empty;
            if (points == null || points.Count == 0)
                return null;

            if (C2WallObjectsV165Wals2DFenceOnlyNo3DWallsModelIDBackendLikeOriginal)
            {
                _c2WallObjectsV159LastModelIDAuditLikeOriginal = "V171_WALS2D_WALLS_g16_backend_for_saved_WL_no_3DWalls_ModelID";
                _c2WallObjectsV157LastReCreateAuditLikeOriginal = recreateAuditV157 + " backend=V171_saved_WL_WALS2D_WALLS_g16_line_root_no_3DWalls_ModelID";
            }
            else if (C2WallObjectsV159UseModelIDMatrix4DBackendForOneWallsSystemLineRootsLikeOriginal)
            {
                Material[] modelMaterialsV159;
                string modelAuditV159;
                Mesh modelMeshV159 = TryBuildOneWallsSystemModelIDLineRootMeshV159LikeOriginal(points, fallbackMaterial, out modelMaterialsV159, out modelAuditV159);
                _c2WallObjectsV159LastModelIDAuditLikeOriginal = modelAuditV159 ?? string.Empty;
                if (modelMeshV159 != null && modelMaterialsV159 != null && modelMaterialsV159.Length > 0)
                {
                    materials = modelMaterialsV159;
                    _c2WallObjectsV160ModelIDLineRootsUsedLikeOriginal++;
                    _c2WallObjectsV157LastReCreateAuditLikeOriginal =
                        recreateAuditV157 + " backend=ModelID_Matrix4D_V160 {" + _c2WallObjectsV159LastModelIDAuditLikeOriginal + "}";
                    return modelMeshV159;
                }

                if (!C2WallObjectsV159FallbackToWallsG16CardsWhenModelIDMissingLikeOriginal)
                {
                    _c2WallObjectsV160ModelIDLineRootsRejectedLikeOriginal++;
                    _c2WallObjectsV160SpriteFallbackBlockedLikeOriginal++;
                    _c2WallObjectsV157LastReCreateAuditLikeOriginal =
                        recreateAuditV157 + " backendRejected=ModelID_required_V160_no_WALLS_G16_fallback {" + _c2WallObjectsV159LastModelIDAuditLikeOriginal + "}";
                    return null;
                }

                _c2WallObjectsV157LastReCreateAuditLikeOriginal =
                    recreateAuditV157 + " backendFallback=WALLS_G16_cards_V159 {" + _c2WallObjectsV159LastModelIDAuditLikeOriginal + "}";
            }

            var verts = new List<Vector3>(points.Count * 4);
            var uvs = new List<Vector2>(points.Count * 4);
            var colors = new List<Color32>(points.Count * 4);
            var submeshTris = new List<List<int>>();
            var mats = new List<Material>();
            var matBySprite = new Dictionary<int, int>();

            WallSavedMapSpriteV6LikeOriginal first = sprites[start];
            WallSavedMapSpriteV6LikeOriginal last = sprites[end - 1];
            Vector3 worldLineA = OriginalWallXYZToWorldV6LikeOriginal(first.X, first.Y, SampleWallHeightOriginalXYV1LikeOriginal(first.X, first.Y));
            Vector3 worldLineB = OriginalWallXYZToWorldV6LikeOriginal(last.X, last.Y, SampleWallHeightOriginalXYV1LikeOriginal(last.X, last.Y));
            Vector3 worldLineDir = worldLineB - worldLineA;
            worldLineDir.y = 0.0f;
            if (worldLineDir.sqrMagnitude <= 0.000001f)
                worldLineDir = Vector3.right;
            else
                worldLineDir.Normalize();

            for (int section = 0; section < points.Count; section++)
            {
                OneWallPointV157LikeOriginal point = points[section];
                if (point == null || point.Type == null || point.Type.SpriteDesc == null)
                    continue;

                WallSpriteDescV1LikeOriginal desc = point.Type.SpriteDesc;
                int matKey = point.ResolvedSpriteIndex >= 0 ? point.ResolvedSpriteIndex : desc.SpriteIndex;
                int matIndex;
                if (!matBySprite.TryGetValue(matKey, out matIndex))
                {
                    Texture2D tex = TryLoadWallSpriteTextureV1LikeOriginal(desc, out string texSourceV157);
                    if (tex == null)
                        tex = Texture2D.whiteTexture;

                    Material mat = CreateWallSpriteMaterialV29LikeOriginal(tex, desc, null, fallbackMaterial);
                    if (mat == null)
                        mat = fallbackMaterial;
                    if (mat == null)
                        continue;

                    matIndex = mats.Count;
                    matBySprite[matKey] = matIndex;
                    mats.Add(mat);
                    submeshTris.Add(new List<int>());
                }

                Texture2D sectionTex = TryLoadWallSpriteTextureV1LikeOriginal(desc, out string sectionTexSourceV157);
                float wPx = sectionTex != null && sectionTex != Texture2D.whiteTexture ? Mathf.Max(8.0f, sectionTex.width) : Mathf.Max(8.0f, desc.Width * 2.0f);
                float hPx = sectionTex != null && sectionTex != Texture2D.whiteTexture ? Mathf.Max(8.0f, sectionTex.height) : Mathf.Max(8.0f, desc.Height * 2.0f);

                int rx = Mathf.RoundToInt(point.x);
                int ry = Mathf.RoundToInt(point.y);
                WallSavedMapSpriteV6LikeOriginal recreated = new WallSavedMapSpriteV6LikeOriginal
                {
                    Sign = "WL",
                    X = rx,
                    Y = ry,
                    SpriteIndex = desc.SpriteIndex,
                    NIndex = 0,
                    Locking = 0,
                    HasMatrix = false,
                    Matrix = Matrix4x4.identity
                };

                Mesh sectionMesh = BuildSavedMapWallSpriteAlignedNoEmbedV20LikeOriginal(recreated, desc, wPx, hPx);
                if (sectionMesh == null)
                    continue;

                Vector3[] sectionVerts = sectionMesh.vertices;
                Vector2[] sectionUvs = sectionMesh.uv;
                Color32[] sectionColors = sectionMesh.colors32;
                int[] sectionTris = sectionMesh.triangles;
                if (sectionVerts == null || sectionUvs == null || sectionTris == null || sectionVerts.Length == 0 || sectionUvs.Length != sectionVerts.Length)
                    continue;

                float roundedTerrain = SampleWallHeightOriginalXYV1LikeOriginal(rx, ry);
                Vector3 roundedAnchor = OriginalWallXYZToWorldV6LikeOriginal(rx, ry, roundedTerrain + desc.FixHeight);
                Vector3 recreatedAnchor = OriginalWallXYZToWorldV6LikeOriginal(point.x, point.y, point.z + desc.FixHeight);
                Vector3 shift = recreatedAnchor - roundedAnchor;

                int baseIndex = verts.Count;
                for (int vi = 0; vi < sectionVerts.Length; vi++)
                {
                    Vector3 v = sectionVerts[vi] + shift;
                    if (Mathf.Abs(point.ScaleP - 1.0f) > 0.0001f)
                    {
                        Vector3 rel = v - recreatedAnchor;
                        float along = Vector3.Dot(rel, worldLineDir);
                        v += worldLineDir * (along * (point.ScaleP - 1.0f));
                    }

                    verts.Add(v);

                    // WALLS.g16 quad helper performs Unity's usual V conversion. For this original wall-port
                    // combined line we keep the already verified V157 upright correction.
                    Vector2 uv = sectionUvs[vi];
                    uv.y = 1.0f - uv.y;
                    uvs.Add(uv);

                    if (sectionColors != null && sectionColors.Length == sectionVerts.Length)
                        colors.Add(sectionColors[vi]);
                    else
                        colors.Add(new Color32(255, 255, 255, 255));
                }

                List<int> tris = submeshTris[matIndex];
                for (int ti = 0; ti < sectionTris.Length; ti++)
                    tris.Add(baseIndex + sectionTris[ti]);
            }

            if (verts.Count == 0 || mats.Count == 0)
                return null;

            Mesh mesh = new Mesh { name = "C2_WallFence_WALS2D_WALLS_g16_LineRoot_V171_points" + points.Count.ToString(CultureInfo.InvariantCulture) };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.colors32 = colors.ToArray();
            mesh.subMeshCount = submeshTris.Count;
            for (int sm = 0; sm < submeshTris.Count; sm++)
                mesh.SetTriangles(submeshTris[sm].ToArray(), sm);
            mesh.RecalculateBounds();

            materials = mats.ToArray();
            return mesh;
        }


        private static Vector3 FindWallFenceBottomJointCenterV148LikeOriginal(Vector3[] verts)
        {
            if (verts == null || verts.Length == 0)
                return Vector3.zero;

            float minY = verts[0].y;
            for (int i = 1; i < verts.Length; i++)
            {
                if (verts[i].y < minY)
                    minY = verts[i].y;
            }

            Vector3 sum = Vector3.zero;
            int count = 0;
            float eps = 0.05f;
            for (int i = 0; i < verts.Length; i++)
            {
                if (Mathf.Abs(verts[i].y - minY) <= eps)
                {
                    sum += verts[i];
                    count++;
                }
            }

            if (count == 0)
                return verts[0];
            return sum / Mathf.Max(1, count);
        }

        private Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> BuildWallFenceLineCenteredAnchorsV132LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallConnectorChainInfoV14LikeOriginal info,
            out Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4> sharedBasisBySprite)
        {
            info = new WallConnectorChainInfoV14LikeOriginal();
            sharedBasisBySprite = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4>();
            var result = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();

            if (!C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal || sprites == null || catalog == null || sprites.Count == 0)
                return result;

            int i = 0;
            while (i < sprites.Count)
            {
                WallSavedMapSpriteV6LikeOriginal first = sprites[i];
                if (!TryGetStraightenableWL2DFenceDescV132LikeOriginal(first, catalog, out WallSpriteDescV1LikeOriginal firstDesc))
                {
                    i++;
                    continue;
                }

                int spriteIndex = first.SpriteIndex;
                int sameEnd = i + 1;
                while (sameEnd < sprites.Count &&
                       sprites[sameEnd] != null &&
                       sprites[sameEnd].SpriteIndex == spriteIndex &&
                       TryGetStraightenableWL2DFenceDescV132LikeOriginal(sprites[sameEnd], catalog, out _))
                {
                    sameEnd++;
                }

                int count = sameEnd - i;
                info.CandidateSprites += count;

                int segStart = i;
                while (segStart < sameEnd)
                {
                    int segEnd = segStart + 1;
                    Vector2 refDir = Vector2.zero;
                    bool hasRefDir = false;

                    while (segEnd < sameEnd)
                    {
                        WallSavedMapSpriteV6LikeOriginal a = sprites[segEnd - 1];
                        WallSavedMapSpriteV6LikeOriginal b = sprites[segEnd];
                        if (a == null || b == null || b.SpriteIndex != spriteIndex)
                            break;

                        Vector2 step = new Vector2(b.X - a.X, b.Y - a.Y);
                        float len = step.magnitude;
                        if (len < C2WallObjectsV132FenceMinStepOriginal || len > C2WallObjectsV132FenceMaxStepOriginal)
                            break;

                        Vector2 dir = step / Mathf.Max(0.0001f, len);
                        if (hasRefDir && Vector2.Dot(refDir, dir) < C2WallObjectsV132FenceDirectionDotLikeOriginal)
                            break;

                        if (!hasRefDir)
                        {
                            refDir = dir;
                            hasRefDir = true;
                        }

                        segEnd++;
                    }

                    int segCount = segEnd - segStart;
                    if (segCount >= C2WallObjectsV132FenceMinRunLengthLikeOriginal && hasRefDir)
                    {
                        WallSavedMapSpriteV6LikeOriginal runFirst = sprites[segStart];
                        WallSavedMapSpriteV6LikeOriginal runLast = sprites[segEnd - 1];
                        Vector2 p0 = new Vector2(runFirst.X, runFirst.Y);
                        Vector2 p1 = new Vector2(runLast.X, runLast.Y);
                        Vector2 wholeLine = p1 - p0;
                        float wholeLen = wholeLine.magnitude;

                        if (wholeLen >= C2WallObjectsV132FenceMinStepOriginal)
                        {
                            Vector2 stepVec = wholeLine / Mathf.Max(1, segCount - 1);
                            Vector2 along = stepVec.sqrMagnitude > 0.000001f ? stepVec.normalized : wholeLine.normalized;
                            float stepLen = stepVec.magnitude;
                            Matrix4x4 sharedBasis;
                            bool hasSharedBasis = TrySelectWallFenceRunSharedMatrixBasisV142LikeOriginal(sprites, segStart, segEnd, firstDesc, along, stepLen, segCount, out sharedBasis);
                            float maxCorrection = 0.0f;
                            int adjusted = 0;

                            for (int k = segStart; k < segEnd; k++)
                            {
                                WallSavedMapSpriteV6LikeOriginal sp = sprites[k];
                                if (sp == null)
                                    continue;

                                int localIndex = k - segStart;
                                Vector2 rebuilt = p0 + stepVec * localIndex;
                                Vector2 original = new Vector2(sp.X, sp.Y);
                                float correction = (rebuilt - original).magnitude;
                                if (correction > maxCorrection)
                                    maxCorrection = correction;

                                result[sp] = rebuilt;
                                adjusted++;
                            }

                            // V146: keep the original route/clamp/reanchor, but stop preserving
                            // every section's own Matrix4D basis. A same-sprite run must use one
                            // shared template basis, otherwise anchors become straight while the
                            // visible cards still keep per-section depth/orientation drift.
                            if (hasSharedBasis)
                            {
                                for (int k = segStart; k < segEnd; k++)
                                {
                                    WallSavedMapSpriteV6LikeOriginal sp = sprites[k];
                                    if (sp != null && result.ContainsKey(sp) && sp.HasMatrix)
                                        sharedBasisBySprite[sp] = sharedBasis;
                                }
                            }

                            info.Runs++;
                            info.AdjustedSprites += adjusted;
                            info.PreservedSprites += Math.Max(0, segCount - adjusted);

                            if (info.Audit.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                            {
                                info.Audit.Add("sprite=W" + spriteIndex.ToString(CultureInfo.InvariantCulture) +
                                               " run=" + segStart.ToString(CultureInfo.InvariantCulture) + "-" + (segEnd - 1).ToString(CultureInfo.InvariantCulture) +
                                               " count=" + segCount.ToString(CultureInfo.InvariantCulture) +
                                               " first=(" + runFirst.X.ToString(CultureInfo.InvariantCulture) + "," + runFirst.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                               " last=(" + runLast.X.ToString(CultureInfo.InvariantCulture) + "," + runLast.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                               " stepLen=" + stepLen.ToString("0.###", CultureInfo.InvariantCulture) +
                                               " maxCorrection=" + maxCorrection.ToString("0.###", CultureInfo.InvariantCulture) +
                                               " sharedM4Basis=" + (hasSharedBasis ? "True" : "False") +
                                               " identicalObjectsOnly=True adjacentSameSprite=True countedSections=True rebuiltXY=True realJointTarget=True preserveOriginalRoute=True sharedRunBasis=True preserveOriginalClamp=True noExplicitCard=False combinedRunMesh=True suppressIndividualCards=True finalVerticesAudit=True markerV148=True");
                            }
                        }
                        else
                        {
                            info.RejectedRuns++;
                            info.RejectedSprites += segCount;
                            info.PreservedSprites += segCount;
                        }
                    }
                    else
                    {
                        info.RejectedRuns++;
                        info.RejectedSprites += segCount;
                        info.PreservedSprites += segCount;
                    }

                    segStart = Math.Max(segEnd, segStart + 1);
                }

                i = sameEnd;
            }

            return result;
        }

        private static bool TryGetStraightenableWL2DFenceDescV132LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal sprite,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallSpriteDescV1LikeOriginal desc)
        {
            desc = null;
            if (sprite == null || catalog == null)
                return false;
            if (!catalog.ByIndex.TryGetValue(sprite.SpriteIndex, out desc) || desc == null)
                return false;
            return IsStraightenableWL2DFenceSpriteV132LikeOriginal(desc);
        }

        private static bool IsStraightenableWL2DFenceSpriteV132LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;
            if (!string.IsNullOrWhiteSpace(desc.ModelPath))
                return false;

            // V152: exact WALLS 2D fence frame pairs confirmed from the Python proof:
            // top/bottom: 74,59,1,5,3,7
            // right/left: 70,58,0,4,4,6
            // These must be treated as fence candidates even if the old class table
            // names some of them as bridge-side/wall-side instead of small/large fence.
            if (IsKnownWallFencePairSpriteV152LikeOriginal(desc.SpriteIndex))
                return true;
            return false;
        }

        private static bool IsWallFenceLineMarkerV142LikeOriginal(WallSavedMapSpriteV6LikeOriginal s)
        {
            return s != null && s.HasMatrix && Mathf.Abs(s.Matrix.m13 - C2WallObjectsV142FenceLineMarkerLikeOriginal) < 0.5f;
        }

        private static bool TrySelectWallFenceRunSharedMatrixBasisV142LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            WallSpriteDescV1LikeOriginal desc,
            Vector2 along,
            float stepOriginalUnits,
            int sectionCount,
            out Matrix4x4 basis)
        {
            basis = Matrix4x4.identity;
            if (!TrySelectWallFenceRunTemplateMatrixBasisV132LikeOriginal(sprites, start, end, out Matrix4x4 template))
                return false;

            // V143: only line positions are rebuilt. Do NOT rotate/scale the 2D WALLS sprite card.
            // W58/W59/W70/W74 frames already contain their visual side/orientation in the sprite data and old route.
            // V142/V140 rotated the cards by run direction and therefore put right/top/left/bottom pieces into wrong places.
            // Keep the original template basis and let the existing route-specific mesh builder + terrain clamp do the final placement.
            basis = template;
            return IsFiniteWallM4V21LikeOriginal(basis);
        }

        private static float GetWallFenceEstimatedSpriteWidthPxV142LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return 1.0f;
            if (desc.Width > 0)
                return Mathf.Max(1.0f, desc.Width * 2.0f);
            if (desc.AlignPoints != null && desc.AlignPoints.Count >= 2)
                return Mathf.Max(1.0f, Mathf.Abs(desc.AlignPoints[1].x - desc.AlignPoints[0].x));
            return 1.0f;
        }

        private static bool TrySelectWallFenceRunSharedMatrixBasisV132LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            Vector2 along,
            out Matrix4x4 basis)
        {
            basis = Matrix4x4.identity;
            if (!TrySelectWallFenceRunTemplateMatrixBasisV132LikeOriginal(sprites, start, end, out Matrix4x4 template))
                return false;

            basis = BuildWallFenceRunSafeLineMatrixBasisV137LikeOriginal(template, along);
            return IsFiniteWallM4V21LikeOriginal(basis);
        }

        private static Matrix4x4 BuildWallFenceRunSafeLineMatrixBasisV137LikeOriginal(Matrix4x4 template, Vector2 along)
        {
            Matrix4x4 basis = template;
            Vector2 dir = along.sqrMagnitude > 0.000001f ? along.normalized : Vector2.right;

            Vector2 templateGroundX = new Vector2(template.m00, template.m01);
            float templateGroundXLen = templateGroundX.magnitude;
            if (templateGroundXLen < 0.0001f)
                templateGroundXLen = new Vector3(template.m00, template.m01, template.m02).magnitude;
            if (templateGroundXLen < 0.0001f)
                templateGroundXLen = 1.0f;

            if (templateGroundX.sqrMagnitude > 0.000001f)
            {
                Vector2 templateGroundXDir = templateGroundX.normalized;
                if (Vector2.Dot(templateGroundXDir, dir) < 0.0f)
                    dir = -dir;
            }

            float templateVerticalLen = new Vector3(template.m10, template.m11, template.m12).magnitude;
            if (templateVerticalLen < 0.0001f)
                templateVerticalLen = 1.0f;

            float verticalSign = template.m12 < 0.0f ? -1.0f : 1.0f;

            // V138: final fence geometry must be one real plane:
            // local X = rebuilt first->last line on map XY;
            // local Y = pure vertical Z, no map XY drift.
            // V137 still preserved old m10/m11 horizontal drift, so each section could stand at its own depth
            // even though the logged anchors were rebuilt correctly.
            basis.m00 = dir.x * templateGroundXLen;
            basis.m01 = dir.y * templateGroundXLen;
            basis.m02 = 0.0f;

            basis.m10 = 0.0f;
            basis.m11 = 0.0f;
            basis.m12 = verticalSign * templateVerticalLen;

            // Local Z is unused by the WALLS.g16 quad builder, but keep it orthogonal for safety/debug.
            float depthLen = new Vector3(template.m20, template.m21, template.m22).magnitude;
            if (depthLen < 0.0001f)
                depthLen = Mathf.Max(templateGroundXLen, 1.0f);
            Vector2 normal = new Vector2(-dir.y, dir.x);
            basis.m20 = normal.x * depthLen;
            basis.m21 = normal.y * depthLen;
            basis.m22 = 0.0f;

            return basis;
        }

        private static bool TrySelectWallFenceRunTemplateMatrixBasisV132LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            int start,
            int end,
            out Matrix4x4 basis)
        {
            basis = Matrix4x4.identity;
            if (sprites == null || start < 0 || end <= start || start >= sprites.Count)
                return false;

            int mid = Mathf.Clamp(start + (end - start) / 2, start, end - 1);
            for (int pass = 0; pass < 2; pass++)
            {
                int begin = pass == 0 ? mid : start;
                int finish = pass == 0 ? end : mid;
                for (int i = begin; i < finish && i < sprites.Count; i++)
                {
                    WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                    if (s != null && s.HasMatrix && IsFiniteWallM4V21LikeOriginal(s.Matrix))
                    {
                        basis = s.Matrix;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetWallDambaModelDescV60LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal sprite,
            WallSpriteCatalogV1LikeOriginal catalog,
            out WallSpriteDescV1LikeOriginal desc)
        {
            desc = null;
            if (sprite == null || catalog == null)
                return false;
            if (!catalog.ByIndex.TryGetValue(sprite.SpriteIndex, out desc) ||
                desc == null ||
                string.IsNullOrWhiteSpace(desc.ModelPath) ||
                !IsWallDambaC2MModelV33LikeOriginal(desc))
            {
                desc = null;
                return false;
            }
            return true;
        }

        private static Vector2 GetWallConnectorStepOriginalXYV14LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null || desc.LeftEdges.Count == 0 || desc.RightEdges.Count == 0)
                return new Vector2(C2WallObjectsV14ConnectorStepFallbackPixelsLikeOriginal, 0.0f);

            WallEdgePointV1LikeOriginal l = desc.LeftEdges[0];
            WallEdgePointV1LikeOriginal r = desc.RightEdges[0];

            // walls.rsr sprite-space Y is half-isometric in several old formulas; saved map XY uses doubled screen-Y-like scale.
            return new Vector2(r.X - l.X, 2.0f * (r.Y - l.Y));
        }

        private static bool TryGetWallDambaRsrConnectorStepV91LikeOriginal(
            WallSpriteDescV1LikeOriginal desc,
            out Vector2 step,
            out string audit)
        {
            step = Vector2.zero;
            audit = string.Empty;
            if (!C2WallObjectsV91UseRsrConnectorRigidDambaPlacementLikeOriginal ||
                !IsWallDambaRsrConnectorTargetV91LikeOriginal(desc) ||
                desc.LeftEdges.Count == 0 ||
                desc.RightEdges.Count == 0)
            {
                audit = "not_v91_target_or_missing_connectors";
                return false;
            }

            WallEdgePointV1LikeOriginal left = desc.LeftEdges[0];
            WallEdgePointV1LikeOriginal right = desc.RightEdges[0];
            int leftType = Mathf.Abs(left.Id);
            int rightType = Mathf.Abs(right.Id);
            if (leftType <= 0 || rightType <= 0 || leftType != rightType)
            {
                audit = "connector_type_mismatch left=" + left.Id.ToString(CultureInfo.InvariantCulture) +
                        " right=" + right.Id.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            // Place next.left connector onto previous.right connector:
            // nextAnchor = previousAnchor + (right - left). Width/Height from walls.lst are the sprite pivot.
            // The pivot cancels in the subtraction, but keeping the formula explicit documents the original contract.
            Vector2 leftOffset = new Vector2(left.X - desc.Width, 2.0f * (left.Y - desc.Height));
            Vector2 rightOffset = new Vector2(right.X - desc.Width, 2.0f * (right.Y - desc.Height));
            step = rightOffset - leftOffset;
            if (step.sqrMagnitude <= 0.0001f)
            {
                audit = "zero_connector_step";
                return false;
            }

            if (C2WallObjectsV91UseManualPairSpacingMagnitudeForLongDambaRowsLikeOriginal)
            {
                float manualMagnitude = C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.magnitude;
                if (manualMagnitude > 0.001f)
                    step = step.normalized * manualMagnitude;
            }

            audit = "V91_RSR type=" + leftType.ToString(CultureInfo.InvariantCulture) +
                    " left=(" + left.X.ToString("0.###", CultureInfo.InvariantCulture) + "," + left.Y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " right=(" + right.X.ToString("0.###", CultureInfo.InvariantCulture) + "," + right.Y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " step=(" + step.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + step.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                    " manualMagnitudeSpacing=" + (C2WallObjectsV91UseManualPairSpacingMagnitudeForLongDambaRowsLikeOriginal ? "True" : "False");
            return true;
        }

        private static Vector3 GetWallDambaC2MLocalPivotV70LikeOriginal(WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal c2m)
        {
            if (!C2WallObjectsV70AnchorDambaC2MPivotToSavedWLPointLikeOriginal ||
                desc == null ||
                c2m == null ||
                c2m.Vertices == null ||
                c2m.Vertices.Length == 0 ||
                !IsWallDambaC2MModelV33LikeOriginal(desc))
            {
                return Vector3.zero;
            }

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < c2m.Vertices.Length; i++)
            {
                Vector3 v = c2m.Vertices[i];
                if (!float.IsFinite(v.x) || !float.IsFinite(v.y))
                    continue;
                sum.x += v.x;
                sum.y += v.y;
                count++;
            }

            if (count <= 0)
                return Vector3.zero;

            // The original WALLS WL point is the object pivot/center from walls.lst, while old
            // C2M bridge meshes are authored around a non-zero local XY center. Keep vertical
            // placement from the saved Matrix4D; only make the local XY pivot land on WL X/Y.
            return new Vector3(sum.x / count, sum.y / count, 0.0f);
        }

        private static Vector3 ApplyWallDambaAnchorNudgeV71LikeOriginal(WallSpriteDescV1LikeOriginal desc, Vector3 originalAnchor)
        {
            if (!C2WallObjectsV71UseDambaSavedWLAnchorNudgeLikeOriginal ||
                desc == null ||
                !IsWallDambaC2MModelV33LikeOriginal(desc))
            {
                return originalAnchor;
            }

            float normalNudge;
            float alongNudge;
            if (desc.SpriteIndex == 60)
            {
                normalNudge = C2WallObjectsV71DambaBottomNormalNudgeOriginal;
                alongNudge = C2WallObjectsV71DambaBottomAlongNudgeOriginal;
            }
            else if (desc.SpriteIndex == 63)
            {
                normalNudge = C2WallObjectsV71DambaTopNormalNudgeOriginal;
                alongNudge = C2WallObjectsV71DambaTopAlongNudgeOriginal;
            }
            else
            {
                return originalAnchor;
            }

            if (Mathf.Abs(normalNudge) <= 0.001f && Mathf.Abs(alongNudge) <= 0.001f)
                return originalAnchor;

            Vector2 step = GetWallConnectorStepOriginalXYV14LikeOriginal(desc);
            if (step.sqrMagnitude <= 0.0001f)
                return originalAnchor;

            Vector2 along = step.normalized;
            Vector2 normal = new Vector2(-along.y, along.x);
            originalAnchor.x += along.x * alongNudge + normal.x * normalNudge;
            originalAnchor.y += along.y * alongNudge + normal.y * normalNudge;
            return originalAnchor;
        }

        private static WallSavedMapSpriteV6LikeOriginal CopySavedWallSpriteWithAnchorV14LikeOriginal(WallSavedMapSpriteV6LikeOriginal src, Vector2 anchor)
        {
            if (src == null)
                return null;

            return new WallSavedMapSpriteV6LikeOriginal
            {
                Sign = src.Sign,
                X = Mathf.RoundToInt(anchor.x),
                Y = Mathf.RoundToInt(anchor.y),
                SpriteIndex = src.SpriteIndex,
                NIndex = src.NIndex,
                Locking = src.Locking,
                HasMatrix = src.HasMatrix,
                Matrix = src.Matrix
            };
        }

        private static WallSavedMapSpriteV6LikeOriginal CopySavedWallSpriteWithAnchorAndMatrixBasisV81LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal src,
            Vector2 anchor,
            Matrix4x4 sharedBasis)
        {
            if (src == null)
                return null;

            Matrix4x4 m = sharedBasis;
            return new WallSavedMapSpriteV6LikeOriginal
            {
                Sign = src.Sign,
                X = Mathf.RoundToInt(anchor.x),
                Y = Mathf.RoundToInt(anchor.y),
                SpriteIndex = src.SpriteIndex,
                NIndex = src.NIndex,
                Locking = src.Locking,
                HasMatrix = src.HasMatrix,
                Matrix = m
            };
        }

        private enum WallSavedWLProfileV18LikeOriginal
        {
                        ModelBackedC2M,
            GroundAligned,
            VerticalAligned,
            BillboardFallback
        }

        private enum WallDrawRouteV20LikeOriginal
        {
                        SavedAlignedSprite,
            SavedModelC2M,
            DebugFallback
        }

        private enum WallWL2DClassV118LikeOriginal
        {
                        Single2DProp,
            GroundAligned,
            VerticalAligned,
            ModelBackedC2M,
            UnknownFallback
        }

        private sealed class WallSavedWLRouteDecisionV20LikeOriginal
        {
            public WallDrawRouteV20LikeOriginal Route;
            public WallSavedWLProfileV18LikeOriginal Profile;
            public WallWL2DClassV118LikeOriginal ClassV118 = WallWL2DClassV118LikeOriginal.UnknownFallback;
            public string Path = string.Empty;
            public string Reason = string.Empty;
            public bool Emitted = true;
            public bool UseSavedM4;
            public bool FlipLocalY;
            public bool MatrixVerified;
            public bool HasSharedRunHeightV59;
            public float SharedRunHeightV59;
            public string MatrixAudit = string.Empty;
            public string Variant = "-";
        }

        private sealed class WallWL2DPlacementMetricV119LikeOriginal
        {
            public int Order;
            public int SpriteIndex;
            public string Name = string.Empty;
            public int X;
            public int Y;
            public WallDrawRouteV20LikeOriginal Route;
            public WallSavedWLProfileV18LikeOriginal Profile;
            public WallWL2DClassV118LikeOriginal ClassV118;
            public bool HasMatrix;
            public bool UseMatrix;
            public float LowestMinusTerrain;
            public float CenterOffsetWorld;
            public float CenterOffsetOriginal;
            public float BoundsDiagonal;
            public float BoundsHeight;
            public float BoundsWidthXZ;
            public float BoundsCenterY;
            public string Path = string.Empty;
            public string TextureSource = string.Empty;
        }

        private struct WallSavedM4BasisV21LikeOriginal
        {
            public bool Valid;
            public string Reason;
            public string Variant;
            public bool FlipLocalY;
            public float XLen;
            public float YLen;
            public float Area;
        }

        private static WallWL2DClassV118LikeOriginal ClassifySavedWL2DObjectV118LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return WallWL2DClassV118LikeOriginal.UnknownFallback;

            if (!string.IsNullOrWhiteSpace(desc.ModelPath))
                return WallWL2DClassV118LikeOriginal.ModelBackedC2M;

            // V165: WALS 2D fences are not classified into old individual-card route classes.
            // They are emitted only by WALS2D line-root before this per-sprite route path.
            if (IsStraightenableWL2DFenceSpriteV132LikeOriginal(desc))
                return WallWL2DClassV118LikeOriginal.UnknownFallback;

            if (desc.AlignMode == 'H')
                return WallWL2DClassV118LikeOriginal.GroundAligned;

            if ((desc.AlignMode == 'V' || desc.AlignMode == 'S') && desc.AlignPoints.Count >= 2)
                return WallWL2DClassV118LikeOriginal.VerticalAligned;

            if (desc.AlignMode == 'U' || desc.AlignPoints.Count > 0)
                return WallWL2DClassV118LikeOriginal.Single2DProp;

            return WallWL2DClassV118LikeOriginal.UnknownFallback;
        }




private static float GetWallDescAlignSpanV118LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null || desc.AlignPoints == null || desc.AlignPoints.Count == 0)
                return 0.0f;

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < desc.AlignPoints.Count; i++)
            {
                Vector3 p = desc.AlignPoints[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            if (float.IsInfinity(minX) || float.IsInfinity(minY))
                return 0.0f;

            return Mathf.Max(maxX - minX, maxY - minY);
        }

        private static WallSavedWLProfileV18LikeOriginal GetWallSavedWLProfileV18LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return WallSavedWLProfileV18LikeOriginal.BillboardFallback;

            if (!string.IsNullOrWhiteSpace(desc.ModelPath))
                return WallSavedWLProfileV18LikeOriginal.ModelBackedC2M;

            // V165: WALS 2D fences do not enter the legacy per-sprite profile path.
            if (IsStraightenableWL2DFenceSpriteV132LikeOriginal(desc))
                return WallSavedWLProfileV18LikeOriginal.BillboardFallback;

            if (desc.AlignMode == 'H')
                return WallSavedWLProfileV18LikeOriginal.GroundAligned;

            if ((desc.AlignMode == 'V' || desc.AlignMode == 'S') && desc.AlignPoints.Count >= 2)
                return WallSavedWLProfileV18LikeOriginal.VerticalAligned;

            return WallSavedWLProfileV18LikeOriginal.BillboardFallback;
        }


        private static bool ShouldFlipVerticalAlignedPropUvV122LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            if (!C2WallObjectsV122FlipVerticalAlignedPropUvLikeOriginal)
                return false;

            if (s == null || desc == null || route == null)
                return false;

            if (route.Route != WallDrawRouteV20LikeOriginal.SavedAlignedSprite)
                return false;

            int id = desc.SpriteIndex;
            return id == 68 || id == 69 || id == 72 || id == 73;
        }

        private static Mesh FlipWallMeshUvVerticalV122LikeOriginal(
            Mesh source,
            WallSpriteDescV1LikeOriginal desc,
            out string audit)
        {
            audit = "V122_UV_FLIP skipped";

            if (source == null)
                return null;

            Vector2[] srcUv = source.uv;
            if (srcUv == null || srcUv.Length == 0)
            {
                audit = "V122_UV_FLIP no_uv";
                return source;
            }

            Mesh mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = (source.name ?? "WallMesh") + "_V122_UvFlipV";

            Vector2[] uv = mesh.uv;

            float minV = float.PositiveInfinity;
            float maxV = float.NegativeInfinity;

            for (int i = 0; i < uv.Length; i++)
            {
                if (uv[i].y < minV) minV = uv[i].y;
                if (uv[i].y > maxV) maxV = uv[i].y;
            }

            if (float.IsNaN(minV) || float.IsNaN(maxV) ||
                float.IsInfinity(minV) || float.IsInfinity(maxV) ||
                Mathf.Abs(maxV - minV) < 0.000001f)
            {
                minV = 0.0f;
                maxV = 1.0f;
            }

            float sum = minV + maxV;

            for (int i = 0; i < uv.Length; i++)
                uv[i].y = sum - uv[i].y;

            mesh.uv = uv;
            mesh.RecalculateBounds();

            audit =
                "V122_UV_FLIP sprite=W" +
                (desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-") +
                " vRange=" +
                minV.ToString("0.###", CultureInfo.InvariantCulture) +
                "->" +
                maxV.ToString("0.###", CultureInfo.InvariantCulture);

            return mesh;
        }

        private static bool ShouldDrawSavedWLProfileV18LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (!C2WallObjectsV18UseSavedWLProfilesLikeOriginal)
                return true;
            if (!C2WallObjectsV18SkipModelBackedSavedWLUntilC2MRenderer)
                return true;
            return GetWallSavedWLProfileV18LikeOriginal(desc) != WallSavedWLProfileV18LikeOriginal.ModelBackedC2M;
        }

        private int BuildWallSavedMapSpriteMeshesV6LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> sprites, WallSpriteCatalogV1LikeOriginal catalog, Transform parent)
        {
            if (sprites == null || catalog == null || parent == null)
                return 0;

            EnsureWals2DHeightInstructionLoadedV178LikeOriginal();
            if (_c2Wals2DHeightAdjustRecordsV178LikeOriginal != null)
                _c2Wals2DHeightAdjustRecordsV178LikeOriginal.Clear();

            Material mat = CreateWallObjectMaterialV1LikeOriginal();
            int drawn = 0;
            int missingTextures = 0;
            int savedM4Seen = 0;
            int chainAdjusted = 0;

            int routeBridge = 0;
            int routeFence = 0;
            int routeLargeFence = 0;
            int routeAligned = 0;
            int routeModel = 0;
            int routeFallback = 0;

            int profileBridge = 0;
            int profileFence = 0;
            int profileModel = 0;
            int profileGround = 0;
            int profileVertical = 0;
            int profileFallback = 0;

            int wl2dSmallFenceV118 = 0;
            int wl2dLargeFenceV118 = 0;
            int wl2dSinglePropV118 = 0;
            int wl2dGroundV118 = 0;
            int wl2dVerticalV118 = 0;
            int wl2dModelV118 = 0;
            int wl2dBridgeSideV118 = 0;
            int wl2dUnknownV118 = 0;
            int wl2dLargeFenceClampV118 = 0;
            int wl2dBridgeSideClampV120 = 0;
            int wl2dBridgeSideAlignClampV121 = 0;
            int wl2dBridgeSideLoweredV131 = 0;
            int wl2dVerticalPropUvFlipV122 = 0;

            var indexAudit = new Dictionary<int, int>();
            var wl2dAuditV118 = new List<string>(C2WallObjectsV118AuditLimitLikeOriginal);
            var wl2dMetricsV119 = new List<WallWL2DPlacementMetricV119LikeOriginal>();
            var wl2dSmallFenceIdsV118 = new Dictionary<int, int>();
            var wl2dLargeFenceIdsV118 = new Dictionary<int, int>();
            var wl2dSinglePropIdsV118 = new Dictionary<int, int>();
            var wl2dGroundIdsV118 = new Dictionary<int, int>();
            var wl2dVerticalIdsV118 = new Dictionary<int, int>();
            var wl2dModelIdsV118 = new Dictionary<int, int>();
            var wl2dUnknownIdsV118 = new Dictionary<int, int>();
            var routeAudit = new List<string>(C2WallObjectsV20RouteAuditLimitLikeOriginal);
            var basisAudit = new List<string>(C2WallObjectsV13DebugCandidateLogLimitLikeOriginal);
            var matrixAudit = new List<string>(C2WallObjectsV21MatrixAuditLimitLikeOriginal);
            var modelAudit = new List<string>(C2WallObjectsV22ModelAuditLimitLikeOriginal);
            var immAudit = new List<string>(C2WallObjectsV24ImmAuditLimitLikeOriginal);
            var materialAudit = new List<string>(C2WallObjectsV26MaterialAuditLimitLikeOriginal);
            var dambaAudit = new List<string>(C2WallObjectsV33DambaAuditLimitLikeOriginal);
            var gpObjAudit = new List<string>(C2WallObjectsV40GPObjAuditLimitLikeOriginal);
            var gpObjAuditKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var chunkRenderAudit = new List<string>(C2WallObjectsV40GPObjAuditLimitLikeOriginal);
            var chunkRenderAuditKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var gpObjMaterialAudit = new List<string>(C2WallObjectsV42GPObjMaterialAuditLimitLikeOriginal);
            var gpObjMaterialAuditKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var spriteRgbaAudit = new List<string>(C2WallObjectsV29SpriteAuditLimitLikeOriginal);
            var fenceFinalVertexAuditV145 = new List<string>(C2WallObjectsV132FenceAuditLimitLikeOriginal);
            var dambaAnchorAuditV84 = new List<string>(C2WallObjectsV73DambaAuditLimitLikeOriginal);
            int dambaAnchorObjectsCreatedV84 = 0;
            WallIMMHeightLockLayerV25LikeOriginal immLayer = new WallIMMHeightLockLayerV25LikeOriginal();

            WallConnectorChainInfoV14LikeOriginal chainInfo = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> chainAnchors =
                C2WallObjectsV20SavedWLNeverUsesConnectorReCreateLikeOriginal
                    ? new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>()
                    : BuildConnectorChainAnchorsV14LikeOriginal(sprites, catalog, out chainInfo);
            if (C2WallObjectsV20SavedWLNeverUsesConnectorReCreateLikeOriginal)
            {
                chainInfo = new WallConnectorChainInfoV14LikeOriginal();
                chainInfo.PreservedSprites = sprites.Count;
                chainInfo.Audit.Add("V20_SAVED_WL_ROUTE_no_ReCreate_no_connector_resnap");
            }

            WallConnectorChainInfoV14LikeOriginal modelChainInfoV53 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> modelChainAnchorsV53 =
                C2WallObjectsV53UseWhiteModelConnectorChainLikeOriginal &&
                !C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal
                    ? BuildModelBackedConnectorChainAnchorsV53LikeOriginal(sprites, catalog, out modelChainInfoV53)
                    : new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();
            int modelChainAdjustedV53 = 0;

            WallConnectorChainInfoV14LikeOriginal modelRunAnchorInfoV61 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4> modelRunSharedBasisV81 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, float> modelRunStage2FullPoseHeightsV89 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> modelRunAnchorsV61 =
                (C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal ||
                 C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal ||
                 C2WallObjectsV91UseRsrConnectorRigidDambaPlacementLikeOriginal ||
                 C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal ||
                 C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal)
                    ? BuildModelBackedDambaSectionRowAnchorsV68LikeOriginal(sprites, catalog, out modelRunAnchorInfoV61, out modelRunSharedBasisV81, out modelRunStage2FullPoseHeightsV89)
                    : (C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal ||
                 C2WallObjectsV67StraightenRigidSavedM4DambaRunsLikeOriginal)
                    ? BuildModelBackedBridgeRunAnchorsV61LikeOriginal(sprites, catalog, out modelRunAnchorInfoV61)
                    : new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();
            if (modelRunSharedBasisV81 == null)
                modelRunSharedBasisV81 = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4>();
            int modelRunAnchorAdjustedV61 = 0;

            WallConnectorChainInfoV14LikeOriginal modelRunHeightInfoV59 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, float> modelRunHeightsV59 =
                C2WallObjectsV59LevelModelBackedBridgeRunsLikeOriginal
                    ? BuildModelBackedBridgeRunHeightsV59LikeOriginal(sprites, catalog, out modelRunHeightInfoV59)
                    : new Dictionary<WallSavedMapSpriteV6LikeOriginal, float>();

            if (modelRunStage2FullPoseHeightsV89 != null && modelRunStage2FullPoseHeightsV89.Count > 0)
            {
                foreach (var kvHeightV89 in modelRunStage2FullPoseHeightsV89)
                {
                    if (kvHeightV89.Key != null)
                        modelRunHeightsV59[kvHeightV89.Key] = kvHeightV89.Value;
                }
            }

            int modelRunHeightAdjustedV59 = 0;

            WallConnectorChainInfoV14LikeOriginal fenceLineInfoV132 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4> fenceLineSharedBasisV132 = null;
            Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2> fenceLineAnchorsV132 =
                C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal
                    ? BuildWallFenceLineCenteredAnchorsV132LikeOriginal(sprites, catalog, out fenceLineInfoV132, out fenceLineSharedBasisV132)
                    : new Dictionary<WallSavedMapSpriteV6LikeOriginal, Vector2>();
            if (fenceLineSharedBasisV132 == null)
                fenceLineSharedBasisV132 = new Dictionary<WallSavedMapSpriteV6LikeOriginal, Matrix4x4>();
            int fenceLineAdjustedV132 = 0;

            HashSet<WallSavedMapSpriteV6LikeOriginal> fenceLineSuppressedV144;
            string fenceLineRootAuditV144;
            int fenceLineRootsCreatedV144 = BuildSyntheticIdenticalWL2DFenceLineRootsV144LikeOriginal(
                sprites,
                catalog,
                parent,
                mat,
                out fenceLineSuppressedV144,
                out fenceLineRootAuditV144);
            if (fenceLineSuppressedV144 == null)
                fenceLineSuppressedV144 = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
            drawn += fenceLineRootsCreatedV144;

            int syntheticDambaRowsV93 = BuildSyntheticDambaMapRowsV93LikeOriginal(
                sprites,
                catalog,
                parent,
                modelRunHeightsV59,
                out HashSet<WallSavedMapSpriteV6LikeOriginal> syntheticDambaSuppressedV93);

            int legacyDambaPieceFallbackSkippedV94 = 0;
            int legacyWals2DFenceIndividualCardsDeletedV165 = 0;

            for (int i = 0; i < sprites.Count; i++)
            {
                WallSavedMapSpriteV6LikeOriginal s = sprites[i];
                if (s == null)
                    continue;

                WallSavedMapSpriteV6LikeOriginal sourceSpriteForLog = s;
                if (C2WallObjectsV144BuildIdenticalWL2DFenceLineRootsLikeOriginal &&
                    fenceLineSuppressedV144 != null &&
                    fenceLineSuppressedV144.Contains(sourceSpriteForLog))
                {
                    continue;
                }

                if (chainAnchors.TryGetValue(s, out Vector2 chainAnchor))
                {
                    s = CopySavedWallSpriteWithAnchorV14LikeOriginal(s, chainAnchor);
                    chainAdjusted++;
                }

                if (!catalog.ByIndex.TryGetValue(s.SpriteIndex, out WallSpriteDescV1LikeOriginal desc) || desc == null)
                {
                    wl2dUnknownV118++;
                    if (C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal && wl2dAuditV118.Count < C2WallObjectsV118AuditLimitLikeOriginal)
                        wl2dAuditV118.Add(BuildWallWL2DAuditLineV118LikeOriginal(i, s, sourceSpriteForLog, null, null, null, "missing_catalog"));
                    continue;
                }

                if (C2WallObjectsV165HardDeleteLegacySavedWL2DFenceIndividualCardsLikeOriginal &&
                    IsStraightenableWL2DFenceSpriteV132LikeOriginal(desc))
                {
                    // V165 hard delete of old individual WALS 2D fence card path.
                    // Do not call SelectSavedWallRouteV20LikeOriginal, do not build DELETED_LEGACY_WALS2D_ROUTE/DELETED_LEGACY_WALS2D_ROUTE/DELETED_LEGACY_WALS2D_ROUTE mesh.
                    legacyWals2DFenceIndividualCardsDeletedV165++;
                    continue;
                }

                if (C2WallObjectsV94DisableLegacyDambaPieceFallbackLikeOriginal &&
                    IsWallDambaC2MModelV33LikeOriginal(desc))
                {
                    legacyDambaPieceFallbackSkippedV94++;
                    continue;
                }

                if (syntheticDambaSuppressedV93 != null &&
                    syntheticDambaSuppressedV93.Contains(sourceSpriteForLog))
                {
                    continue;
                }

                if (C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal &&
                    fenceLineAnchorsV132 != null &&
                    fenceLineAnchorsV132.TryGetValue(sourceSpriteForLog, out Vector2 fenceLineAnchorV132))
                {
                    if (fenceLineSharedBasisV132 != null &&
                        fenceLineSharedBasisV132.TryGetValue(sourceSpriteForLog, out Matrix4x4 fenceLineBasisV132))
                        s = CopySavedWallSpriteWithAnchorAndMatrixBasisV81LikeOriginal(sourceSpriteForLog, fenceLineAnchorV132, fenceLineBasisV132);
                    else
                        s = CopySavedWallSpriteWithAnchorV14LikeOriginal(sourceSpriteForLog, fenceLineAnchorV132);

                    fenceLineAdjustedV132++;
                }

                if ((C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal ||
                     C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal ||
                     C2WallObjectsV91UseRsrConnectorRigidDambaPlacementLikeOriginal ||
                     C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal ||
                     C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal ||
                     C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal ||
                     C2WallObjectsV67StraightenRigidSavedM4DambaRunsLikeOriginal) &&
                    IsWallDambaC2MModelV33LikeOriginal(desc) &&
                    modelRunAnchorsV61.TryGetValue(sourceSpriteForLog, out Vector2 modelRunAnchorV61))
                {
                    if (modelRunSharedBasisV81 != null &&
                        modelRunSharedBasisV81.TryGetValue(sourceSpriteForLog, out Matrix4x4 sharedBasisV81))
                        s = CopySavedWallSpriteWithAnchorAndMatrixBasisV81LikeOriginal(sourceSpriteForLog, modelRunAnchorV61, sharedBasisV81);
                    else
                        s = CopySavedWallSpriteWithAnchorV14LikeOriginal(sourceSpriteForLog, modelRunAnchorV61);
                    modelRunAnchorAdjustedV61++;
                }

                if (C2WallObjectsV53UseWhiteModelConnectorChainLikeOriginal &&
                    !C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal &&
                    !string.IsNullOrWhiteSpace(desc.ModelPath) &&
                    modelChainAnchorsV53.TryGetValue(sourceSpriteForLog, out Vector2 modelChainAnchorV53))
                {
                    s = CopySavedWallSpriteWithAnchorV14LikeOriginal(sourceSpriteForLog, modelChainAnchorV53);
                    modelChainAdjustedV53++;
                }

                if (!indexAudit.ContainsKey(desc.SpriteIndex))
                    indexAudit[desc.SpriteIndex] = 0;
                indexAudit[desc.SpriteIndex]++;

                if (s.HasMatrix)
                    savedM4Seen++;

                WallSavedWLRouteDecisionV20LikeOriginal route = SelectSavedWallRouteV20LikeOriginal(s, desc);
                if (C2WallObjectsV53UseWhiteModelConnectorChainLikeOriginal &&
                    route.Route == WallDrawRouteV20LikeOriginal.SavedModelC2M)
                {
                    bool universalAnchorLineOwnsDambaPlacementV75 =
                        C2WallObjectsV75DisableSavedMatrix4DForUniversalDambaAnchorsLikeOriginal &&
                        C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal &&
                        IsWallDambaC2MModelV33LikeOriginal(desc);

                    bool useOriginalDambaSavedM4V66 =
                        (C2WallObjectsV66UseRigidSavedMatrixForDambaC2MLikeOriginal ||
                         C2WallObjectsV65UseSavedMatrixForDambaC2MLikeOriginal) &&
                        IsWallDambaC2MModelV33LikeOriginal(desc) &&
                        s.HasMatrix &&
                        route.MatrixVerified &&
                        !universalAnchorLineOwnsDambaPlacementV75;

                    if (useOriginalDambaSavedM4V66)
                    {
                        route.UseSavedM4 = true;
                        route.Variant = "MODEL_DAMBA_C2M_SAVED_M4_RIGID_V66";
                        route.Path = "MODEL_C2M_V66_original_saved_Matrix4D_existingM4_rigid_textured";
                        route.Reason = (route.Reason ?? string.Empty) +
                                       "; V66: original trace renders DAMBA through RenderModels.Add existingM4; keep saved Matrix4D but convert it as one rigid world-space model, not per-vertex terrain warp" +
                                       (C2WallObjectsV70AnchorDambaC2MPivotToSavedWLPointLikeOriginal ? "; V70: anchor C2M local XY pivot to saved WL point, matching walls.lst center semantics instead of local zero" : string.Empty) +
                                       (C2WallObjectsV71UseDambaSavedWLAnchorNudgeLikeOriginal ? "; V71: small DAMBA saved-WL anchor nudge hook, no C2M pivot rewrite" : string.Empty) +
                                       (C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal ? "; V88: W60 DAMBA row uses map only for order; runtime placement uses authored Stage2 full relative pose, V14 scene anchors remain visible" : string.Empty) +
                                       (C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal ? "; V72: W60 pair calibrated connector chain, first map section then previous+delta" : string.Empty) +
                                       (C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal ? "; V68: assemble DAMBA as separate section rows, resampled between first and last section anchors" : string.Empty) +
                                       (C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal ? "; V69: project DAMBA rows to connector line, preserving native along-row spacing" : string.Empty) +
                                       (C2WallObjectsV67StraightenRigidSavedM4DambaRunsLikeOriginal ? "; V67: use straightened run anchor as rigid world origin while preserving Matrix4D local deltas" : string.Empty);
                        route.MatrixAudit = (route.MatrixAudit ?? string.Empty) + " V66_DAMBA_saved_M4_existingM4_rigid_delta" +
                                            (C2WallObjectsV70AnchorDambaC2MPivotToSavedWLPointLikeOriginal ? " V70_local_C2M_XY_pivot_to_saved_WL" : string.Empty) +
                                            (C2WallObjectsV71UseDambaSavedWLAnchorNudgeLikeOriginal ? " V71_small_anchor_nudge" : string.Empty) +
                                            (C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal ? " V88_stage2_full_pose_relative_transform_FIRST_MAP_OBJECT_V14_scene_anchors" : string.Empty) +
                                            (C2WallObjectsV72UseDambaPairCalibrationChainLikeOriginal ? " V72_pair_calibrated_chain" : string.Empty) +
                                            (C2WallObjectsV68AssembleDambaRowsBySectionEndpointsLikeOriginal ? " V68_section_row_endpoints" : string.Empty) +
                                            (C2WallObjectsV69ProjectDambaRowsToConnectorLineKeepNativeSpacingLikeOriginal ? " V69_connector_line_project_perp" : string.Empty) +
                                            (C2WallObjectsV67StraightenRigidSavedM4DambaRunsLikeOriginal ? " V67_straightened_world_origin" : string.Empty);
                    }
                    else
                    {
                        route.UseSavedM4 = false;
                        route.MatrixVerified = false;
                        route.Variant = universalAnchorLineOwnsDambaPlacementV75
                            ? "MODEL_UNIVERSAL_ANCHOR_LINE_NO_SAVED_M4_V75"
                            : (C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal ? "MODEL_MAP_ANCHOR_RIGID_TEXTURED" : "MODEL_CHAIN_CENTER_WHITE");
                        route.Path = universalAnchorLineOwnsDambaPlacementV75
                            ? "MODEL_C2M_V75_universal_anchor_line_no_saved_Matrix4D_anchor_driven_textured"
                            : (C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal
                                ? "MODEL_C2M_V58_map_saved_anchor_rigid_textured_bottom_plus5"
                                : "MODEL_C2M_V53_connector_chain_centered_bottom_plus5_white");
                        route.Reason = (route.Reason ?? string.Empty) + (universalAnchorLineOwnsDambaPlacementV75
                            ? "; V77: universal anchor points own DAMBA placement; use authored Stage2 point pose, not nearest-pair auto matching; disable saved Matrix4D"
                            : (C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal
                                ? "; V58: use original map TRE2/WL saved anchor X/Y again; keep rigid no-grid-warp C2M mesh and TemnyLess DrawWChunk texture"
                                : "; V53 diagnostic: ignore saved Matrix4D, place model by connector-chain anchor, center bounds, bottom +5, no texture white fill"));
                        route.MatrixAudit = (route.MatrixAudit ?? string.Empty) + (universalAnchorLineOwnsDambaPlacementV75
                            ? " V75_universal_anchor_line_no_saved_Matrix4D"
                            : (C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal ? " V58_map_saved_anchor_no_connector_resnap" : " V53_force_no_savedM4"));
                    }

                    bool forceFlatSavedM4DambaV82 =
                        C2WallObjectsV82ForceFlatSharedHeightForUniversalSavedM4DambaRowsLikeOriginal &&
                        C2WallObjectsV73UseUniversalAnchorLineCalibrationForDambaLikeOriginal &&
                        route != null &&
                        route.UseSavedM4 &&
                        IsWallDambaC2MModelV33LikeOriginal(desc) &&
                        IsWallDambaW60CalibrationTargetV90LikeOriginal(desc);

                    if (C2WallObjectsV59LevelModelBackedBridgeRunsLikeOriginal &&
                        C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal &&
                        IsWallDambaC2MModelV33LikeOriginal(desc) &&
                        (!route.UseSavedM4 || forceFlatSavedM4DambaV82) &&
                        modelRunHeightsV59.TryGetValue(sourceSpriteForLog, out float sharedHeightV59))
                    {
                        route.HasSharedRunHeightV59 = true;
                        route.SharedRunHeightV59 = sharedHeightV59;
                        route.Variant = forceFlatSavedM4DambaV82
                            ? "MODEL_SAVED_M4_UNIVERSAL_ANCHOR_FLAT_DECK_V82"
                            : (universalAnchorLineOwnsDambaPlacementV75
                                ? "MODEL_UNIVERSAL_ANCHOR_LINE_NO_SAVED_M4_FLAT_DECK_V75"
                                : (C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal ? "MODEL_MAP_ANCHOR_RIGID_TEXTURED_FLAT_DECK_STRAIGHT_ROW_V61" : "MODEL_MAP_ANCHOR_RIGID_TEXTURED_FLAT_DECK_V60"));
                        route.Path = forceFlatSavedM4DambaV82
                            ? "MODEL_C2M_V89_stage2_full_3D_relative_transform_shared_Matrix4D_basis_AND_height_DrawWChunk"
                            : (universalAnchorLineOwnsDambaPlacementV75
                                ? "MODEL_C2M_V77_stage2_point_pose_no_saved_Matrix4D_flat_group_height_deck_anchor_textured"
                                : (C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal ? "MODEL_C2M_V61_map_saved_XY_straight_row_flat_group_height_deck_anchor_textured" : "MODEL_C2M_V60_map_saved_XY_flat_group_height_deck_anchor_textured"));
                        route.Reason = (route.Reason ?? string.Empty) + "; V60: shared flat DAMBA group height, align C2M deck-anchor (not absolute top) to bridge/deck line, no per-piece terrain stepping" +
                                       (forceFlatSavedM4DambaV82 ? "; V83: saved Matrix4D basis is shared from the first map object; row spacing comes from model connector anchors; per-piece height is one flat deck level" : string.Empty) +
                                       (universalAnchorLineOwnsDambaPlacementV75 ? "; V77: flat group height now rides on authored Stage2 point-pose CENTER_MAIN anchor chain, not on saved Matrix4D" : string.Empty) +
                                       (C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal ? "; V61: row anchors are straightened in map XY, no forward/back jitter and no gaps" : string.Empty);
                        route.MatrixAudit = (route.MatrixAudit ?? string.Empty) + " V60_flat_group_height=" + sharedHeightV59.ToString("0.###", CultureInfo.InvariantCulture) +
                                            (forceFlatSavedM4DambaV82 ? " V89_stage2_full_3D_height_delta" : string.Empty) +
                                            (C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal ? " V61_straight_row_anchor" : string.Empty);
                        modelRunHeightAdjustedV59++;
                    }
                }
                switch (route.ClassV118)
                {
                    // V168: legacy WALS2D fence classes are physically removed from enum/runtime routing.
                    // Fence objects are handled only by WALS2D_LINE_ROOT before this per-sprite route block.
                    case WallWL2DClassV118LikeOriginal.Single2DProp:
                        wl2dSinglePropV118++;
                        IncrementWallIdCountV118LikeOriginal(wl2dSinglePropIdsV118, desc);
                        break;
                    case WallWL2DClassV118LikeOriginal.GroundAligned:
                        wl2dGroundV118++;
                        IncrementWallIdCountV118LikeOriginal(wl2dGroundIdsV118, desc);
                        break;
                    case WallWL2DClassV118LikeOriginal.VerticalAligned:
                        wl2dVerticalV118++;
                        IncrementWallIdCountV118LikeOriginal(wl2dVerticalIdsV118, desc);
                        break;
                    case WallWL2DClassV118LikeOriginal.ModelBackedC2M:
                        wl2dModelV118++;
                        IncrementWallIdCountV118LikeOriginal(wl2dModelIdsV118, desc);
                        break;
                    default:
                        wl2dUnknownV118++;
                        IncrementWallIdCountV118LikeOriginal(wl2dUnknownIdsV118, desc);
                        break;
                }

                CountWallSavedRouteV20LikeOriginal(route.Route, ref routeBridge, ref routeFence, ref routeLargeFence, ref routeAligned, ref routeModel, ref routeFallback);
                CountWallSavedProfileV20LikeOriginal(route.Profile, ref profileBridge, ref profileFence, ref profileModel, ref profileGround, ref profileVertical, ref profileFallback);

                if (matrixAudit.Count < C2WallObjectsV21MatrixAuditLimitLikeOriginal && s.HasMatrix &&
                    (desc.SpriteIndex == 58 || desc.SpriteIndex == 59 || desc.SpriteIndex == 70 || desc.SpriteIndex == 74 || !string.IsNullOrWhiteSpace(desc.ModelPath)))
                    matrixAudit.Add(BuildWallMatrixAuditLineV21LikeOriginal(i, s, desc, route));

                if (modelAudit.Count < C2WallObjectsV22ModelAuditLimitLikeOriginal && route.Route == WallDrawRouteV20LikeOriginal.SavedModelC2M)
                    modelAudit.Add(BuildWallModelAuditLineV22LikeOriginal(i, s, desc, route));

                if (immAudit.Count < C2WallObjectsV24ImmAuditLimitLikeOriginal && route.Route == WallDrawRouteV20LikeOriginal.SavedModelC2M)
                    immAudit.Add(BuildWallIMMRouteAuditLineV24LikeOriginal(i, s, desc, route));
                if (materialAudit.Count < C2WallObjectsV26MaterialAuditLimitLikeOriginal && route.Route == WallDrawRouteV20LikeOriginal.SavedModelC2M)
                    materialAudit.Add(BuildWallC2MRenderMaterialAuditLineV26LikeOriginal(i, s, desc, route));

                if (!route.Emitted)
                {
                    if (C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal && wl2dAuditV118.Count < C2WallObjectsV118AuditLimitLikeOriginal)
                        wl2dAuditV118.Add(BuildWallWL2DAuditLineV118LikeOriginal(i, s, sourceSpriteForLog, desc, route, null, "emitted_false"));
                    if (routeAudit.Count < C2WallObjectsV20RouteAuditLimitLikeOriginal)
                        routeAudit.Add(BuildWallRouteAuditLineV20LikeOriginal(i, s, sourceSpriteForLog, desc, route, null));
                    continue;
                }

                Texture2D tex;
                string source;
                bool isModelBackedRouteV22 = route.Route == WallDrawRouteV20LikeOriginal.SavedModelC2M;
                if (isModelBackedRouteV22)
                {
                    if (IsWallDambaC2MModelV33LikeOriginal(desc) && C2WallObjectsV33UseWallSpriteTextureForDambaC2MLikeOriginal)
                    {
                        tex = TryLoadWallSpriteTextureV1LikeOriginal(desc, out source);
                        if (tex == null)
                        {
                            missingTextures++;
                            tex = Texture2D.whiteTexture;
                            source = "DAMBA_C2M_MISSING_WALLS_G16_TEXTURE model=" + desc.ModelPath + " after " + (source ?? string.Empty);
                        }
                        else
                        {
                            if (C2WallObjectsV36MakeDambaTextureOpaqueLikeOriginal)
                            {
                                tex = MakeDambaTextureOpaqueV36LikeOriginal(tex, desc, out string opaqueAudit);
                                source = "DAMBA_C2M_WALLS_G16_OPAQUE_TEXTURE model=" + desc.ModelPath + " " + source + " " + opaqueAudit;
                            }
                            else
                            {
                                source = "DAMBA_C2M_WALLS_G16_TEXTURE model=" + desc.ModelPath + " " + source;
                            }
                        }
                    }
                    else
                    {
                        tex = Texture2D.whiteTexture;
                        source = "MODEL_C2M_IMM_V24_REAL_C2M_GEOM model=" + desc.ModelPath;
                    }
                }
                else
                {
                    tex = TryLoadWallSpriteTextureV1LikeOriginal(desc, out source);
                    if (tex == null)
                    {
                        missingTextures++;
                        if (C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal && wl2dAuditV118.Count < C2WallObjectsV118AuditLimitLikeOriginal)
                            wl2dAuditV118.Add(BuildWallWL2DAuditLineV118LikeOriginal(i, s, sourceSpriteForLog, desc, route, null, "missing_texture " + (source ?? string.Empty)));
                        if (!C2WallObjectsV1DrawDebugPlaceholdersLikeOriginal)
                            continue;
                        tex = Texture2D.whiteTexture;
                        source = "debug-white-placeholder after " + (source ?? string.Empty);
                    }
                }

                WallSpriteRgbaStatsV29LikeOriginal spriteRgbaStats = null;
                if (!isModelBackedRouteV22 && tex != null && C2WallObjectsV29AuditWallSpriteRgbaLikeOriginal)
                {
                    spriteRgbaStats = AnalyzeWallSpriteRgbaV29LikeOriginal(tex, desc);
                    if (spriteRgbaAudit.Count < C2WallObjectsV29SpriteAuditLimitLikeOriginal && IsWallSpriteRgbaAuditTargetV29LikeOriginal(desc))
                        spriteRgbaAudit.Add(BuildWallSpriteRgbaAuditLineV29LikeOriginal(i, desc, source, spriteRgbaStats));
                }

                GameObject go = new GameObject($"WallMapSpriteV37_{i:0000}_{desc.Name}_{route.Route}");
                go.transform.SetParent(parent, false);
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                ApplyWallRendererShadowContractV44LikeOriginal(mr);

                Mesh mesh = BuildSavedMapWallSpriteRouteMeshV20LikeOriginal(s, desc, tex, route);
                WallC2MParsedMeshV23LikeOriginal c2mForImmLayerV25 = null;
                if (isModelBackedRouteV22 && C2WallObjectsV25ApplyIMMHeightLockLayerLikeOriginal)
                {
                    c2mForImmLayerV25 = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out string immLayerLoadAudit);
                    AccumulateWallC2MImmHeightLockLayerV25LikeOriginal(immLayer, s, desc, c2mForImmLayerV25, immLayerLoadAudit, i);
                    if (IsWallDambaC2MModelV33LikeOriginal(desc))
                    {
                        if (dambaAudit.Count < C2WallObjectsV33DambaAuditLimitLikeOriginal)
                            dambaAudit.Add(BuildWallDambaChainAuditLineV33LikeOriginal(i, desc, tex, source, c2mForImmLayerV25, immLayerLoadAudit, route));

                        string gpKey = string.IsNullOrWhiteSpace(desc.ModelPath) ? desc.Name : desc.ModelPath;
                        if (c2mForImmLayerV25 != null && gpObjAudit.Count < C2WallObjectsV40GPObjAuditLimitLikeOriginal && !gpObjAuditKeys.Contains(gpKey))
                        {
                            gpObjAuditKeys.Add(gpKey);
                            gpObjAudit.Add(BuildWallC2MGPObjAuditLineV40LikeOriginal(desc, c2mForImmLayerV25));
                        }

                        if (c2mForImmLayerV25 != null && chunkRenderAudit.Count < C2WallObjectsV40GPObjAuditLimitLikeOriginal && !chunkRenderAuditKeys.Contains(gpKey))
                        {
                            chunkRenderAuditKeys.Add(gpKey);
                            chunkRenderAudit.Add(BuildWallC2MGPObjChunkRenderAuditLineV41LikeOriginal(desc, c2mForImmLayerV25));
                        }
                    }
                }
                if (mesh == null)
                {
                    if (C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal && wl2dAuditV118.Count < C2WallObjectsV118AuditLimitLikeOriginal)
                        wl2dAuditV118.Add(BuildWallWL2DAuditLineV118LikeOriginal(i, s, sourceSpriteForLog, desc, route, null, source + " mesh_builder_returned_null"));
                    if (routeAudit.Count < C2WallObjectsV20RouteAuditLimitLikeOriginal)
                    {
                        route.Reason = string.IsNullOrEmpty(route.Reason) ? "mesh_builder_returned_null" : route.Reason + "; mesh_builder_returned_null";
                        routeAudit.Add(BuildWallRouteAuditLineV20LikeOriginal(i, s, sourceSpriteForLog, desc, route, source));
                    }
                    SafeDestroy(go);
                    continue;
                }
                string wl2dClampAudit = string.Empty;
if (ShouldClampSavedM4Prop2DToTerrainV124LikeOriginal(s, desc, route))
                {
                    mesh = ClampWall2DMeshMinVertexToTerrainV124LikeOriginal(mesh, s, desc, out string propClampAuditV124);
                    wl2dClampAudit = string.IsNullOrWhiteSpace(wl2dClampAudit) ? propClampAuditV124 : wl2dClampAudit + " " + propClampAuditV124;
                    route.MatrixAudit = (route.MatrixAudit ?? string.Empty) + " " + propClampAuditV124;
                }
if (ShouldFlipVerticalAlignedPropUvV122LikeOriginal(s, desc, route))
                {
                    mesh = FlipWallMeshUvVerticalV122LikeOriginal(mesh, desc, out string propUvAuditV122);
                    wl2dVerticalPropUvFlipV122++;
                    wl2dClampAudit = string.IsNullOrWhiteSpace(wl2dClampAudit) ? propUvAuditV122 : wl2dClampAudit + " " + propUvAuditV122;
                    route.MatrixAudit = (route.MatrixAudit ?? string.Empty) + " " + propUvAuditV122;
                }

                if (C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal &&
                    fenceLineAnchorsV132 != null &&
                    sourceSpriteForLog != null &&
                    fenceLineAnchorsV132.TryGetValue(sourceSpriteForLog, out Vector2 fenceRealJointTargetV147))
                {
                    mesh = AlignWallFenceMeshBottomJointToRebuiltLineTargetV147LikeOriginal(
                        mesh,
                        s,
                        desc,
                        fenceRealJointTargetV147,
                        out string realJointAuditV147);
                    wl2dClampAudit = string.IsNullOrWhiteSpace(wl2dClampAudit) ? realJointAuditV147 : wl2dClampAudit + " " + realJointAuditV147;
                    route.MatrixAudit = (route.MatrixAudit ?? string.Empty) + " " + realJointAuditV147;
                }

                if (C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal &&
                    fenceLineAnchorsV132 != null &&
                    sourceSpriteForLog != null &&
                    fenceLineAnchorsV132.ContainsKey(sourceSpriteForLog) &&
                    fenceFinalVertexAuditV145.Count < C2WallObjectsV132FenceAuditLimitLikeOriginal)
                {
                    Vector3[] finalVertsV145 = mesh != null ? mesh.vertices : null;
                    float minYV145 = float.PositiveInfinity;
                    float maxYV145 = float.NegativeInfinity;
                    float minXV145 = float.PositiveInfinity;
                    float maxXV145 = float.NegativeInfinity;
                    float minZV145 = float.PositiveInfinity;
                    float maxZV145 = float.NegativeInfinity;
                    int vertCountV145 = finalVertsV145 != null ? finalVertsV145.Length : 0;
                    if (finalVertsV145 != null)
                    {
                        for (int fv = 0; fv < finalVertsV145.Length; fv++)
                        {
                            Vector3 vtx = finalVertsV145[fv];
                            if (vtx.x < minXV145) minXV145 = vtx.x;
                            if (vtx.x > maxXV145) maxXV145 = vtx.x;
                            if (vtx.y < minYV145) minYV145 = vtx.y;
                            if (vtx.y > maxYV145) maxYV145 = vtx.y;
                            if (vtx.z < minZV145) minZV145 = vtx.z;
                            if (vtx.z > maxZV145) maxZV145 = vtx.z;
                        }
                    }

                    Vector2 anchorV145 = fenceLineAnchorsV132[sourceSpriteForLog];
                    fenceFinalVertexAuditV145.Add("order=" + i.ToString(CultureInfo.InvariantCulture) +
                                                  " sprite=W" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                                                  " route=" + route.Route +
                                                  " anchorOnlyXY=(" + s.X.ToString(CultureInfo.InvariantCulture) + "," + s.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                                  " rebuiltXY=(" + Mathf.RoundToInt(anchorV145.x).ToString(CultureInfo.InvariantCulture) + "," + Mathf.RoundToInt(anchorV145.y).ToString(CultureInfo.InvariantCulture) + ")" +
                                                  " hasM4=" + (s.HasMatrix ? "True" : "False") +
                                                  " realJointAlign=True markerV148=True" +
                                                  " sharedRunBasis=" + ((fenceLineSharedBasisV132 != null && sourceSpriteForLog != null && fenceLineSharedBasisV132.ContainsKey(sourceSpriteForLog)) ? "True" : "False") + "" +
                                                  " finalVerts=" + vertCountV145.ToString(CultureInfo.InvariantCulture) +
                                                  " boundsXYZ=(" + minXV145.ToString("0.###", CultureInfo.InvariantCulture) + ".." + maxXV145.ToString("0.###", CultureInfo.InvariantCulture) +
                                                  "," + minYV145.ToString("0.###", CultureInfo.InvariantCulture) + ".." + maxYV145.ToString("0.###", CultureInfo.InvariantCulture) +
                                                  "," + minZV145.ToString("0.###", CultureInfo.InvariantCulture) + ".." + maxZV145.ToString("0.###", CultureInfo.InvariantCulture) + ")");
                }

                if (C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal && wl2dAuditV118.Count < C2WallObjectsV118AuditLimitLikeOriginal)
                    wl2dAuditV118.Add(BuildWallWL2DAuditLineV118LikeOriginal(i, s, sourceSpriteForLog, desc, route, mesh, source + " " + wl2dClampAudit));

                if (C2WallObjectsV119TopOffenderAuditWL2DLikeOriginal)
                {
                    WallWL2DPlacementMetricV119LikeOriginal metricV119 = BuildWallWL2DPlacementMetricV119LikeOriginal(i, s, desc, route, mesh, source + " " + wl2dClampAudit);
                    if (metricV119 != null)
                        wl2dMetricsV119.Add(metricV119);
                }

                RegisterWals2DHeightAdjustableMeshV178LikeOriginal(mesh, desc);

                mf.sharedMesh = mesh;

                if (isModelBackedRouteV22 && IsWallDambaC2MModelV33LikeOriginal(desc))
                {
                    WallC2MParsedMeshV23LikeOriginal c2mForAnchorsV84 = c2mForImmLayerV25;
                    if (c2mForAnchorsV84 == null && !string.IsNullOrWhiteSpace(desc.ModelPath))
                        c2mForAnchorsV84 = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out string anchorLoadAuditV84);
                    dambaAnchorObjectsCreatedV84 += AddWallDambaSceneOnlyAnchorObjectsV84LikeOriginal(
                        go,
                        s,
                        desc,
                        route,
                        c2mForAnchorsV84,
                        i,
                        dambaAnchorAuditV84);
                }

                if (isModelBackedRouteV22 && C2WallObjectsV25CreateLockMeshColliderLikeOriginal && c2mForImmLayerV25 != null && c2mForImmLayerV25.Lockmesh != null)
                    AddWallC2MLockMeshColliderV25LikeOriginal(go, s, desc, c2mForImmLayerV25.Lockmesh, route);

                if (routeAudit.Count < C2WallObjectsV20RouteAuditLimitLikeOriginal)
                    routeAudit.Add(BuildWallRouteAuditLineV20LikeOriginal(i, s, sourceSpriteForLog, desc, route, source));

                if (basisAudit.Count < C2WallObjectsV13DebugCandidateLogLimitLikeOriginal &&
                    (desc.SpriteIndex == 58 || desc.SpriteIndex == 59 || desc.SpriteIndex == 70 || desc.SpriteIndex == 74))
                {
                    float logWPx = tex != null && tex != Texture2D.whiteTexture ? Mathf.Max(8.0f, tex.width) : Mathf.Max(8.0f, desc.Width * 2.0f);
                    float logHPx = tex != null && tex != Texture2D.whiteTexture ? Mathf.Max(8.0f, tex.height) : Mathf.Max(8.0f, desc.Height * 2.0f);
                    string m4Info = s.HasMatrix
                        ? " m4tr=(" + s.Matrix.m30.ToString("0.###", CultureInfo.InvariantCulture) + "," + s.Matrix.m31.ToString("0.###", CultureInfo.InvariantCulture) + "," + s.Matrix.m32.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                          " m4x=(" + s.Matrix.m00.ToString("0.###", CultureInfo.InvariantCulture) + "," + s.Matrix.m01.ToString("0.###", CultureInfo.InvariantCulture) + "," + s.Matrix.m02.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                          " m4y=(" + s.Matrix.m10.ToString("0.###", CultureInfo.InvariantCulture) + "," + s.Matrix.m11.ToString("0.###", CultureInfo.InvariantCulture) + "," + s.Matrix.m12.ToString("0.###", CultureInfo.InvariantCulture) + ")"
                        : " m4=no";
                    basisAudit.Add("WL:" + desc.Name + "#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                                   " route=" + route.Route +
                                   " path=" + route.Path +
                                   " flipLocalY=" + route.FlipLocalY +
                                   " useM4=" + route.UseSavedM4 +
                                   " m4ok=" + route.MatrixVerified +
                                   " variant=" + route.Variant +
                                   " size=" + logWPx.ToString("0.#", CultureInfo.InvariantCulture) + "x" + logHPx.ToString("0.#", CultureInfo.InvariantCulture) +
                                   " center=" + GetWallSpriteCenterXPxV10LikeOriginal(desc, logWPx).ToString("0.#", CultureInfo.InvariantCulture) + "," +
                                                  GetWallSpriteCenterYPxV10LikeOriginal(desc, logHPx).ToString("0.#", CultureInfo.InvariantCulture) +
                                   " align=" + (desc.AlignMode == '\0' ? "-" : desc.AlignMode.ToString()) +
                                   " xy=(" + s.X.ToString(CultureInfo.InvariantCulture) + "," + s.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                                   m4Info + " " + DescribeWallSpriteBasisVectorsV13LikeOriginal(mesh) + " " + DescribeWallSpriteCornersV13LikeOriginal(mesh));
                }

                List<WallG16SquareV47LikeOriginal> gpObjSquaresV47 = null;
                int gpObjFrameWidthV47 = 0;
                int gpObjFrameHeightV47 = 0;
                Material inst;
                bool forceWhiteModelV53 = isModelBackedRouteV22 &&
                                          C2WallObjectsV53UseWhiteModelConnectorChainLikeOriginal &&
                                          C2WallObjectsV53ModelChainForceWhiteFillLikeOriginal;
                if (isModelBackedRouteV22 && C2WallObjectsV24UseOpaqueMaterialForC2MModelsLikeOriginal)
                {
                    Texture2D modelMaterialTex = forceWhiteModelV53 ? Texture2D.whiteTexture : tex;
                    string gpObjMaterialSource = string.Empty;
                    string gpObjUvFitAudit = string.Empty;
                    Vector4 gpObjUvTransform = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                    bool gpObjMaterialBound = false;

                    if (!forceWhiteModelV53 && C2WallObjectsV42UseGPObjFrameTextureForC2MLikeOriginal && c2mForImmLayerV25 != null && c2mForImmLayerV25.GPObj != null)
                    {
                        Texture2D gpTex = TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(c2mForImmLayerV25, out gpObjMaterialSource, out gpObjSquaresV47);
                        if (gpTex != null)
                        {
                            modelMaterialTex = gpTex;
                            gpObjFrameWidthV47 = gpTex.width;
                            gpObjFrameHeightV47 = gpTex.height;
                            gpObjMaterialBound = true;
                            if (C2WallObjectsV43AutoChooseGPObjTextureUvFlipLikeOriginal && !IsWallDambaC2MModelV33LikeOriginal(desc))
                                gpObjUvTransform = ChooseWallC2MGPObjTextureUvTransformV43LikeOriginal(gpTex, c2mForImmLayerV25, out gpObjUvFitAudit);
                            else if (IsWallDambaC2MModelV33LikeOriginal(desc))
                                gpObjUvFitAudit = "damba_direct_uv_from_DrawWChunk_log_no_flip";
                        }
                    }

                    if (!forceWhiteModelV53 && !gpObjMaterialBound && c2mForImmLayerV25 != null && !string.IsNullOrWhiteSpace(c2mForImmLayerV25.TextureName))
                    {
                        Texture2D txreTex = TryLoadWallC2MTXRETextureV48LikeOriginal(c2mForImmLayerV25, out string txreSource);
                        if (txreTex != null)
                        {
                            modelMaterialTex = txreTex;
                            gpObjMaterialBound = true;
                            gpObjMaterialSource = txreSource;
                            gpObjUvTransform = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                            gpObjUvFitAudit = "TXRE_TGA_whole_frame_UV";
                        }
                    }

                    string gpMatKey = (desc.ModelPath ?? desc.Name ?? string.Empty) + "#" + (c2mForImmLayerV25 != null && c2mForImmLayerV25.GPObj != null ? c2mForImmLayerV25.GPObj.FrameIdx.ToString(CultureInfo.InvariantCulture) : "-");
                    if (IsWallDambaC2MModelV33LikeOriginal(desc) && gpObjMaterialAudit.Count < C2WallObjectsV42GPObjMaterialAuditLimitLikeOriginal && !gpObjMaterialAuditKeys.Contains(gpMatKey))
                    {
                        gpObjMaterialAuditKeys.Add(gpMatKey);
                        gpObjMaterialAudit.Add(BuildWallC2MGPObjMaterialAuditLineV42LikeOriginal(desc, c2mForImmLayerV25, modelMaterialTex, gpObjMaterialBound, gpObjMaterialSource + " uvFit='" + gpObjUvFitAudit + "'"));
                    }

                    inst = CreateWallC2MModelMaterialV26LikeOriginal(modelMaterialTex, desc);
                    if (gpObjMaterialBound && inst != null && inst.HasProperty("_MainTex"))
                    {
                        inst.SetTextureScale("_MainTex", new Vector2(gpObjUvTransform.x, gpObjUvTransform.y));
                        inst.SetTextureOffset("_MainTex", new Vector2(gpObjUvTransform.z, gpObjUvTransform.w));
                    }
                    if (gpObjMaterialBound && inst != null && inst.HasProperty("_Color"))
                        inst.SetColor("_Color", Color.white);
                    if (gpObjMaterialBound && IsWallDambaC2MModelV33LikeOriginal(desc) && inst != null && inst.HasProperty("_UseVertexColor"))
                        inst.SetFloat("_UseVertexColor", 0.0f);
                    if (forceWhiteModelV53 && inst != null)
                    {
                        if (inst.HasProperty("_MainTex"))
                            inst.SetTexture("_MainTex", Texture2D.whiteTexture);
                        if (inst.HasProperty("_Color"))
                            inst.SetColor("_Color", Color.white);
                        if (inst.HasProperty("_UseVertexColor"))
                            inst.SetFloat("_UseVertexColor", 0.0f);
                    }
                }
                else
                {
                    inst = CreateWallSpriteMaterialV29LikeOriginal(tex, desc, spriteRgbaStats, mat);
                }
                if (!forceWhiteModelV53 && isModelBackedRouteV22 && c2mForImmLayerV25 != null && HasWallC2MGPObjChunkSubmeshesV41LikeOriginal(c2mForImmLayerV25) &&
                    !(IsWallDambaC2MModelV33LikeOriginal(desc) && c2mForImmLayerV25.DrawWChunkUvBakedV50))
                    mr.sharedMaterials = BuildWallC2MGPObjChunkMaterialsV47LikeOriginal(inst, c2mForImmLayerV25, gpObjSquaresV47, gpObjFrameWidthV47, gpObjFrameHeightV47, IsWallDambaC2MModelV33LikeOriginal(desc));
                else
                    mr.sharedMaterial = inst;
                mr.sortingOrder = Mathf.Clamp(s.Y, -32768, 32767);

                if (isModelBackedRouteV22 && IsWallDambaC2MModelV33LikeOriginal(desc) && C2WallObjectsV35UseSeparateDambaSideOverlayLikeOriginal)
                {
                    if (!TryAttachDambaSideOverlayV35LikeOriginal(go, s, desc, route, mat, out string dambaOverlayAudit) &&
                        dambaAudit.Count < C2WallObjectsV33DambaAuditLimitLikeOriginal)
                    {
                        dambaAudit.Add("order=" + i.ToString(CultureInfo.InvariantCulture) + " overlay=failed reason='" + (dambaOverlayAudit ?? string.Empty) + "'");
                    }
                    else if (!string.IsNullOrEmpty(dambaOverlayAudit) && dambaAudit.Count < C2WallObjectsV33DambaAuditLimitLikeOriginal)
                    {
                        dambaAudit.Add(dambaOverlayAudit);
                    }
                }

                drawn++;
            }

            Debug.Log("[C2:WALL MAPDRAW V41] mapSprites=" + sprites.Count +
                      " drawn=" + drawn +
                      " missingTextures=" + missingTextures +
                      " routes=bridge:" + routeBridge +
                      ",fence:" + routeFence +
                      ",largeFence:" + routeLargeFence +
                      ",aligned:" + routeAligned +
                      ",model:" + routeModel +
                      ",fallback:" + routeFallback +
                      " profiles=bridge:" + profileBridge +
                      ",fence:" + profileFence +
                      ",model:" + profileModel +
                      ",ground:" + profileGround +
                      ",vertical:" + profileVertical +
                      ",fallback:" + profileFallback +
                      " savedM4Seen=" + savedM4Seen +
                      " chainAdjusted=" + chainAdjusted +
                      " chainAcceptedRuns=" + chainInfo.Runs +
                      " chainRejectedRuns=" + chainInfo.RejectedRuns +
                      " chainPreservedSprites=" + chainInfo.PreservedSprites +
                      " zTest=LEqual spriteZWrite=Off modelZWrite=On renderQueueSprites=" + C2WallObjectsV18RenderQueueLikeOriginal +
                      " modelRenderQueue=" + C2WallObjectsV24ModelRenderQueueLikeOriginal +
                      " immHeightSamples=" + immLayer.HeightSamples.ToString(CultureInfo.InvariantCulture) +
                      " immHeightCells=" + immLayer.HeightCells.ToString(CultureInfo.InvariantCulture) +
                      " immLockSamples=" + immLayer.LockSamples.ToString(CultureInfo.InvariantCulture) +
                      " immLockCells=" + immLayer.LockCells.ToString(CultureInfo.InvariantCulture) +
                      " mode=V45_original_saved_Matrix4D_route savedWL_no_ReCreate_no_connector_resnap bridge_fence_verified_M4 model_C2M_Carcass_GEOM_Navimesh_Lockmesh_IMM_height_lock_layer heightSampler=MapSprites_GetHeight_triangle debugPlaceholders=" + C2WallObjectsV1DrawDebugPlaceholdersLikeOriginal +
                      " c2mProxyFallback=" + C2WallObjectsV23AllowC2MProxyFallbackWhenRendererFailsLikeOriginal +
                      " modelChainV53=" + C2WallObjectsV53ModelChainContractLikeOriginal +
                      " placementV58=" + C2WallObjectsV58PlacementContractLikeOriginal +
                      " mapAnchorV58=" + C2WallObjectsV58UseMapSavedAnchorForModelBackedC2MLikeOriginal +
                      " modelChainAdjustedV53=" + modelChainAdjustedV53.ToString(CultureInfo.InvariantCulture) +
                      " modelRunHeightAdjustedV60=" + modelRunHeightAdjustedV59.ToString(CultureInfo.InvariantCulture) +
                      " syntheticDambaRowsV93=" + syntheticDambaRowsV93.ToString(CultureInfo.InvariantCulture) +
                      " legacyDambaPieceFallbackSkippedV94=" + legacyDambaPieceFallbackSkippedV94.ToString(CultureInfo.InvariantCulture) +
                      " legacyWals2DFenceIndividualCardsDeletedV165=" + legacyWals2DFenceIndividualCardsDeletedV165.ToString(CultureInfo.InvariantCulture) +
                      " dambaPlacementV94=" + C2WallObjectsV94DambaPlacementContractLikeOriginal +
                      " placementV60=" + C2WallObjectsV59PlacementContractLikeOriginal +
                      " modelRunAnchorAdjustedV61=" + modelRunAnchorAdjustedV61.ToString(CultureInfo.InvariantCulture) +
                      " placementV61=" + C2WallObjectsV61PlacementContractLikeOriginal +
                      " placementV62=" + C2WallObjectsV62PlacementContractLikeOriginal +
                      " placementV64=" + C2WallObjectsV64PlacementContractLikeOriginal +
                      " c2mRender=" + C2WallObjectsV26RenderContractLikeOriginal + " panplane_LEqual_Offset spriteRender=" + C2WallObjectsV29SpriteRenderContractLikeOriginal + " bridgeSpriteV31=" + C2WallObjectsV31SpriteRenderContractLikeOriginal + " dambaBase=" + C2WallObjectsV33DambaRenderContractLikeOriginal + " dambaDepth=" + C2WallObjectsV34DambaDepthContractLikeOriginal + " dambaMaterialV36=" + C2WallObjectsV35DambaRenderContractLikeOriginal + " chunkRenderV41=" + C2WallObjectsV41ChunkRenderContractLikeOriginal + " gpObjMaterialV42=" + C2WallObjectsV42MaterialContractLikeOriginal + " uvFitV43=" + C2WallObjectsV43UvFitContractLikeOriginal + " shadowV44=" + C2WallObjectsV44ShadowContractLikeOriginal + " dambaGPObjV46=" + C2WallObjectsV46DambaGPObjContractLikeOriginal + " gpObjSquaresV47=" + C2WallObjectsV47GPObjSquareContractLikeOriginal + " drawWChunkV50=" + C2WallObjectsV50DrawWChunkContractLikeOriginal + " dambaTextureV56=TemnyLess_GPObj_G16_DrawWChunk_textured_no_white_fill drawWChunkV57=" + C2WallObjectsV57DrawWChunkContractLikeOriginal + " partialOverlayV35=disabled fenceRaiseVerticalPx=" + _c2Wals2DVerticalRaisePixelsV178LikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + " fenceRaiseHorizontalPx=" + _c2Wals2DHorizontalRaisePixelsV178LikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + " wl2dV115=fence_no_saved_M4_no_raise_terrain_aligned" + " wl2dV116=" + C2WallObjectsV116FenceContractLikeOriginal + " wl2dV117=" + C2WallObjectsV117FenceContractLikeOriginal + " wl2dV118=" + C2WallObjectsV118ContractLikeOriginal + " wl2dV119=" + C2WallObjectsV119ContractLikeOriginal + " wl2dPropsV122=" + C2WallObjectsV122VerticalPropContractLikeOriginal + " wl2dPropsV124=" + C2WallObjectsV124PropContractLikeOriginal + " bridgeLoweredV131=" + wl2dBridgeSideLoweredV131.ToString(CultureInfo.InvariantCulture) + " fenceLineV171=" + C2WallObjectsV132FenceLineContractLikeOriginal + " wals2dOriginalV172=" + C2WallObjectsV172OriginalWLSavedSpriteContractLikeOriginal + " wals2dUvV173=" + C2WallObjectsV173WLSavedSpriteUvContractLikeOriginal + " wals2dMapXYV174=" + C2WallObjectsV174WLSavedSpriteMapXYContractLikeOriginal + " wals2dShadowLiftV178=" + C2WallObjectsV175WLSavedSpriteSideShadowLiftContractLikeOriginal + " fenceLineAdjustedV132=" + fenceLineAdjustedV132.ToString(CultureInfo.InvariantCulture));

            _c2WallObjectsV25LastIMMLayerLikeOriginal = immLayer;

            Debug.Log("[C2:WALL CHAIN V27] enabled=" + (!C2WallObjectsV20SavedWLNeverUsesConnectorReCreateLikeOriginal && C2WallObjectsV14ConnectorChainEnabledLikeOriginal) +
                      " acceptedRuns=" + chainInfo.Runs.ToString(CultureInfo.InvariantCulture) +
                      " rejectedRuns=" + chainInfo.RejectedRuns.ToString(CultureInfo.InvariantCulture) +
                      " adjustedSprites=" + chainInfo.AdjustedSprites.ToString(CultureInfo.InvariantCulture) +
                      " preservedSprites=" + chainInfo.PreservedSprites.ToString(CultureInfo.InvariantCulture) +
                      " connectorSprites=" + chainInfo.ConnectorSprites.ToString(CultureInfo.InvariantCulture) +
                      " audit=" + (chainInfo.Audit.Count > 0 ? string.Join(" | ", chainInfo.Audit.ToArray()) : "none"));

            if (modelChainInfoV53 != null)
                Debug.Log("[C2:WALL MODEL CHAIN V53] enabled=" + C2WallObjectsV53UseWhiteModelConnectorChainLikeOriginal +
                          " contract=" + C2WallObjectsV53ModelChainContractLikeOriginal +
                          " runs=" + modelChainInfoV53.Runs.ToString(CultureInfo.InvariantCulture) +
                          " adjustedSprites=" + modelChainAdjustedV53.ToString(CultureInfo.InvariantCulture) +
                          " candidates=" + modelChainInfoV53.CandidateSprites.ToString(CultureInfo.InvariantCulture) +
                          " preserved=" + modelChainInfoV53.PreservedSprites.ToString(CultureInfo.InvariantCulture) +
                          " audit=" + (modelChainInfoV53.Audit.Count > 0 ? string.Join(" | ", modelChainInfoV53.Audit.ToArray()) : "none"));

            if (modelRunHeightInfoV59 != null)
                Debug.Log("[C2:WALL MODEL RUN HEIGHT V60] enabled=" + C2WallObjectsV59LevelModelBackedBridgeRunsLikeOriginal +
                          " contract=" + C2WallObjectsV59PlacementContractLikeOriginal +
                          " adjustedSprites=" + modelRunHeightAdjustedV59.ToString(CultureInfo.InvariantCulture) +
                          " runs=" + modelRunHeightInfoV59.Runs.ToString(CultureInfo.InvariantCulture) +
                          " candidates=" + modelRunHeightInfoV59.CandidateSprites.ToString(CultureInfo.InvariantCulture) +
                          " preserved=" + modelRunHeightInfoV59.PreservedSprites.ToString(CultureInfo.InvariantCulture) +
                          " audit=" + (modelRunHeightInfoV59.Audit.Count > 0 ? string.Join(" | ", modelRunHeightInfoV59.Audit.ToArray()) : "none"));

            if (modelRunAnchorInfoV61 != null)
                Debug.Log("[C2:WALL MODEL RUN ANCHOR V61/V62] enabled=" + C2WallObjectsV61StraightenDambaRunAnchorsLikeOriginal +
                          " contract=" + C2WallObjectsV61PlacementContractLikeOriginal +
                          " rollbackV62=" + C2WallObjectsV62PlacementContractLikeOriginal +
                          " adjustedSprites=" + modelRunAnchorAdjustedV61.ToString(CultureInfo.InvariantCulture) +
                          " runs=" + modelRunAnchorInfoV61.Runs.ToString(CultureInfo.InvariantCulture) +
                          " candidates=" + modelRunAnchorInfoV61.CandidateSprites.ToString(CultureInfo.InvariantCulture) +
                          " preserved=" + modelRunAnchorInfoV61.PreservedSprites.ToString(CultureInfo.InvariantCulture) +
                          " audit=" + (modelRunAnchorInfoV61.Audit.Count > 0 ? string.Join(" | ", modelRunAnchorInfoV61.Audit.ToArray()) : "none"));

            if (fenceLineInfoV132 != null)
                Debug.Log("[C2:WALL FENCE LINE V132] enabled=" + C2WallObjectsV132StraightenWL2DFenceRunsLikeOriginal +
                          " contract=" + C2WallObjectsV132FenceLineContractLikeOriginal +
                          " lineRootsV159=" + fenceLineRootsCreatedV144.ToString(CultureInfo.InvariantCulture) +
                          " suppressedV159=" + (fenceLineSuppressedV144 != null ? fenceLineSuppressedV144.Count.ToString(CultureInfo.InvariantCulture) : "0") +
                          " adjustedSprites=" + fenceLineAdjustedV132.ToString(CultureInfo.InvariantCulture) +
                          " runs=" + fenceLineInfoV132.Runs.ToString(CultureInfo.InvariantCulture) +
                          " candidates=" + fenceLineInfoV132.CandidateSprites.ToString(CultureInfo.InvariantCulture) +
                          " preserved=" + fenceLineInfoV132.PreservedSprites.ToString(CultureInfo.InvariantCulture) +
                          " rejectedRuns=" + fenceLineInfoV132.RejectedRuns.ToString(CultureInfo.InvariantCulture) +
                          " rejectedSprites=" + fenceLineInfoV132.RejectedSprites.ToString(CultureInfo.InvariantCulture) +
                          " audit=" + (fenceLineInfoV132.Audit.Count > 0 ? string.Join(" | ", fenceLineInfoV132.Audit.ToArray()) : "none"));

            if (C2WallObjectsV144BuildIdenticalWL2DFenceLineRootsLikeOriginal)
                Debug.Log("[C2:WALL FENCE ORIGINAL 3DWALLS MODELID MATRIX4D V170] created=" + fenceLineRootsCreatedV144.ToString(CultureInfo.InvariantCulture) +
                          " suppressed=" + (fenceLineSuppressedV144 != null ? fenceLineSuppressedV144.Count.ToString(CultureInfo.InvariantCulture) : "0") +
                          " modelIDLineRootsUsed=" + _c2WallObjectsV160ModelIDLineRootsUsedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " modelIDLineRootsRejected=" + _c2WallObjectsV160ModelIDLineRootsRejectedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " spriteFallbackBlocked=" + _c2WallObjectsV160SpriteFallbackBlockedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " numericModelIDResolvedV161=" + _c2WallObjectsV161NumericModelIDResolvedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " numericModelIDUnresolvedV161=" + _c2WallObjectsV161NumericModelIDUnresolvedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " rejectedSavedWLSuppressedV161=" + _c2WallObjectsV161Rejected3DWallsSavedWLSuppressedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " realWTCycleUsedV161=" + _c2WallObjectsV161RealWallTypeCycleUsedLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " contract=" + C2WallObjectsV132FenceLineContractLikeOriginal +
                          " audit=" + (string.IsNullOrWhiteSpace(fenceLineRootAuditV144) ? "none" : fenceLineRootAuditV144));

            if (fenceFinalVertexAuditV145.Count > 0)
                Debug.Log("[C2:WALL FENCE FINAL VERTICES V152] contract=" + C2WallObjectsV132FenceLineContractLikeOriginal +
                          " samples=" + string.Join(" | ", fenceFinalVertexAuditV145.ToArray()));

            if (routeAudit.Count > 0)
                Debug.Log("[C2:WALL ROUTE V27] " + string.Join(" | ", routeAudit.ToArray()));
            if (basisAudit.Count > 0)
                Debug.Log("[C2:WALL BASIS V27] samples=" + string.Join(" | ", basisAudit.ToArray()));
            if (matrixAudit.Count > 0)
                Debug.Log("[C2:WALL MATRIX V27] " + string.Join(" | ", matrixAudit.ToArray()));
            if (modelAudit.Count > 0)
                Debug.Log("[C2:WALL MODEL V27] " + string.Join(" | ", modelAudit.ToArray()));
            if (immAudit.Count > 0)
                Debug.Log("[C2:WALL IMM V27] " + string.Join(" | ", immAudit.ToArray()));
            if (materialAudit.Count > 0)
                Debug.Log("[C2:WALL MATERIAL V27] " + string.Join(" | ", materialAudit.ToArray()));
            if (dambaAudit.Count > 0)
                Debug.Log("[C2:DAMBA CHAIN V33] contract=" + C2WallObjectsV33DambaRenderContractLikeOriginal + " " + string.Join(" | ", dambaAudit.ToArray()));
            if (C2WallObjectsV84CreateSceneOnlyAnchorObjectsOnRuntimeDambaLikeOriginal)
                Debug.Log("[C2:DAMBA ANCHORS V84] contract=" + C2WallObjectsV84DambaAnchorContractLikeOriginal +
                          " createdSceneOnlyAnchorObjects=" + dambaAnchorObjectsCreatedV84.ToString(CultureInfo.InvariantCulture) +
                          " audit=" + (dambaAnchorAuditV84.Count > 0 ? string.Join(" | ", dambaAnchorAuditV84.ToArray()) : "none"));
            if (gpObjAudit.Count > 0)
                Debug.Log("[C2:C2M GPOBJ V40] contract=" + C2WallObjectsV40GPObjContractLikeOriginal + " " + string.Join(" | ", gpObjAudit.ToArray()));
            if (chunkRenderAudit.Count > 0)
                Debug.Log("[C2:C2M CHUNK RENDER V41] contract=" + C2WallObjectsV41ChunkRenderContractLikeOriginal + " " + string.Join(" | ", chunkRenderAudit.ToArray()));
            if (gpObjMaterialAudit.Count > 0)
                Debug.Log("[C2:C2M GPOBJ MATERIAL V42] contract=" + C2WallObjectsV42MaterialContractLikeOriginal + " " + string.Join(" | ", gpObjMaterialAudit.ToArray()));
            if (spriteRgbaAudit.Count > 0)
                Debug.Log("[C2:WALL SPRITE RGBA V29] contract=" + C2WallObjectsV29SpriteRenderContractLikeOriginal + " " + string.Join(" | ", spriteRgbaAudit.ToArray()));
            if (C2WallObjectsV118AuditAndClassifyWL2DLikeOriginal)
            {
                Debug.Log("[C2:WL2D V118 SUMMARY] wl2dV118=" + C2WallObjectsV118ContractLikeOriginal +
                          " classes=smallFence:" + wl2dSmallFenceV118.ToString(CultureInfo.InvariantCulture) +
                          ",largeFence:" + wl2dLargeFenceV118.ToString(CultureInfo.InvariantCulture) +
                          ",singleProp:" + wl2dSinglePropV118.ToString(CultureInfo.InvariantCulture) +
                          ",ground:" + wl2dGroundV118.ToString(CultureInfo.InvariantCulture) +
                          ",vertical:" + wl2dVerticalV118.ToString(CultureInfo.InvariantCulture) +
                          ",modelC2M:" + wl2dModelV118.ToString(CultureInfo.InvariantCulture) +
                          ",bridgeSide:" + wl2dBridgeSideV118.ToString(CultureInfo.InvariantCulture) +
                          ",unknown:" + wl2dUnknownV118.ToString(CultureInfo.InvariantCulture) +
                          " largeFenceClampV118=" + wl2dLargeFenceClampV118.ToString(CultureInfo.InvariantCulture) +
                          " bridgeSideClampV120=" + wl2dBridgeSideClampV120.ToString(CultureInfo.InvariantCulture) +
                          " bridgeSideAlignClampV121=" + wl2dBridgeSideAlignClampV121.ToString(CultureInfo.InvariantCulture) +
                          " verticalPropUvFlipV122=" + wl2dVerticalPropUvFlipV122.ToString(CultureInfo.InvariantCulture) +
                          " smallFenceIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dSmallFenceIdsV118) +
                          " largeFenceIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dLargeFenceIdsV118) +
                          " singlePropIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dSinglePropIdsV118) +
                          " groundIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dGroundIdsV118) +
                          " verticalIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dVerticalIdsV118) +
                          " modelIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dModelIdsV118) +
                          " unknownIds=" + BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wl2dUnknownIdsV118));
                if (wl2dAuditV118.Count > 0)
                    Debug.Log("[C2:WL2D V118 AUDIT] " + string.Join(" | ", wl2dAuditV118.ToArray()));
            }
            if (C2WallObjectsV119TopOffenderAuditWL2DLikeOriginal)
            {
                LogWallWL2DTopOffendersV119LikeOriginal(wl2dMetricsV119);
            }
            Debug.Log(BuildWallC2MImmLayerSummaryV25LikeOriginal(immLayer));

            var sb = new StringBuilder(512);
            sb.Append("[C2:WALL MAPINDEX V27] ");
            int emitted = 0;
            foreach (var kv in indexAudit)
            {
                if (emitted++ >= 32)
                    break;
                if (catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal d))
                    sb.Append(d.Name).Append("#").Append(kv.Key.ToString(CultureInfo.InvariantCulture)).Append("=").Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append(" ");
                else
                    sb.Append("#").Append(kv.Key.ToString(CultureInfo.InvariantCulture)).Append("=").Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append(" ");
            }
            Debug.Log(sb.ToString());

            if (C2WallObjectsV27CoverageAuditLikeOriginal)
                LogWallCoverageAuditV27LikeOriginal(sprites, catalog, indexAudit, immLayer);

            return drawn;
        }

        private void LogWallCoverageAuditV27LikeOriginal(
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog,
            Dictionary<int, int> wlIndexAudit,
            WallIMMHeightLockLayerV25LikeOriginal immLayer)
        {
            try
            {
                int savedWl = sprites != null ? sprites.Count : 0;
                int usedIds = wlIndexAudit != null ? wlIndexAudit.Count : 0;
                int missingCatalog = 0;
                int usedModelIds = 0;
                int usedG16Ids = 0;
                int usedAligned = 0;
                int usedFallback = 0;

                if (sprites != null && catalog != null)
                {
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        WallSavedMapSpriteV6LikeOriginal sp = sprites[i];
                        if (sp == null)
                            continue;

                        if (!catalog.ByIndex.TryGetValue(sp.SpriteIndex, out WallSpriteDescV1LikeOriginal desc) || desc == null)
                        {
                            missingCatalog++;
                            continue;
                        }

                        WallSavedWLProfileV18LikeOriginal profile = GetWallSavedWLProfileV18LikeOriginal(desc);
                        if (!string.IsNullOrWhiteSpace(desc.ModelPath))
                            usedModelIds++;
                        else
                            usedG16Ids++;

                        if (profile == WallSavedWLProfileV18LikeOriginal.GroundAligned ||
                            profile == WallSavedWLProfileV18LikeOriginal.VerticalAligned)
                            usedAligned++;
                        else
                            usedFallback++;
                    }
                }

                int catalogSprites = catalog != null ? catalog.ByIndex.Count : 0;
                int catalogModels = 0;
                int catalogG16 = 0;
                int catalogConnectors = 0;
                int catalogAlign = 0;
                int catalogAutoborn = 0;

                if (catalog != null)
                {
                    foreach (WallSpriteDescV1LikeOriginal d in catalog.ByIndex.Values)
                    {
                        if (d == null)
                            continue;

                        if (!string.IsNullOrWhiteSpace(d.ModelPath))
                            catalogModels++;
                        else
                            catalogG16++;

                        if ((d.LeftEdges != null && d.LeftEdges.Count > 0) ||
                            (d.RightEdges != null && d.RightEdges.Count > 0))
                            catalogConnectors++;

                        if (d.AlignMode != '\0' || (d.AlignPoints != null && d.AlignPoints.Count > 0))
                            catalogAlign++;

                        if (d.AutobornChildren != null && d.AutobornChildren.Count > 0)
                            catalogAutoborn++;
                    }
                }

                Debug.Log("[C2:WALL COVERAGE V27] contract=" + C2WallObjectsV27AuditContractLikeOriginal +
                          " map='" + _mapRelativePath + "'" +
                          " savedWL=" + savedWl.ToString(CultureInfo.InvariantCulture) +
                          " usedWLIds=" + usedIds.ToString(CultureInfo.InvariantCulture) +
                          " missingCatalog=" + missingCatalog.ToString(CultureInfo.InvariantCulture) +
                          " usedG16Instances=" + usedG16Ids.ToString(CultureInfo.InvariantCulture) +
                          " usedModelInstances=" + usedModelIds.ToString(CultureInfo.InvariantCulture) +
                          " usedProfile=aligned:" + usedAligned.ToString(CultureInfo.InvariantCulture) +
                          ",fallback:" + usedFallback.ToString(CultureInfo.InvariantCulture) +
                          " catalogSprites=" + catalogSprites.ToString(CultureInfo.InvariantCulture) +
                          " catalogG16=" + catalogG16.ToString(CultureInfo.InvariantCulture) +
                          " catalogModels=" + catalogModels.ToString(CultureInfo.InvariantCulture) +
                          " catalogWithConnectors=" + catalogConnectors.ToString(CultureInfo.InvariantCulture) +
                          " catalogWithAlign=" + catalogAlign.ToString(CultureInfo.InvariantCulture) +
                          " catalogWithAutoborn=" + catalogAutoborn.ToString(CultureInfo.InvariantCulture) +
                          " immHeightCells=" + (immLayer != null ? immLayer.HeightCells : 0).ToString(CultureInfo.InvariantCulture) +
                          " immLockCells=" + (immLayer != null ? immLayer.LockCells : 0).ToString(CultureInfo.InvariantCulture) +
                          " note='V166 compile fix: old individual WALS 2D fence routes are physically absent; coverage audit no longer references removed fence/bridge profiles'");

                Debug.Log("[C2:WALL WLID COVERAGE V27] " + BuildWallUsedIdCoverageLineV27LikeOriginal(catalog, wlIndexAudit));
                Debug.Log("[C2:WALL MODEL COVERAGE V27] " + BuildWallModelCoverageLineV27LikeOriginal(catalog, wlIndexAudit));
                Debug.Log("[C2:WALL OBJECT-SYSTEM BOUNDARY V27] WL_saved_renderer=covered_by_this_file; STONES_TS_and_COMPLEX_OC=not_covered_by_OneWallsSystem; current_WL_model_usage=" + BuildWallUsedModelNameListV27LikeOriginal(catalog, wlIndexAudit));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:WALL COVERAGE V27] audit failed: " + ex.Message);
            }
        }

        private Mesh BuildSavedMapWallSpriteUnifiedFence2DMeshV135LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            float wPx,
            float hPx,
            bool flipLocalY,
            string path)
        {
            if (s == null || desc == null || !s.HasMatrix)
                return null;

            if (C2WallObjectsV140UseExplicitUnifiedFenceLineCardMeshLikeOriginal)
                return BuildSavedMapWallSpriteExplicitFenceLineCardMeshV140LikeOriginal(s, desc, wPx, hPx, path);

            // V135 legacy fallback kept only for A/B rollback.
            Vector2 pivot = GetWallUnifiedFencePivotPxV135LikeOriginal(desc, wPx, hPx);

            Vector3[] local;
            if (flipLocalY)
            {
                local = new[]
                {
                    new Vector3(0.0f - pivot.x,  pivot.y - 0.0f, 0.0f),
                    new Vector3(wPx  - pivot.x,  pivot.y - 0.0f, 0.0f),
                    new Vector3(wPx  - pivot.x,  pivot.y - hPx,  0.0f),
                    new Vector3(0.0f - pivot.x,  pivot.y - hPx,  0.0f)
                };
            }
            else
            {
                local = new[]
                {
                    new Vector3(0.0f - pivot.x,  0.0f - pivot.y, 0.0f),
                    new Vector3(wPx  - pivot.x,  0.0f - pivot.y, 0.0f),
                    new Vector3(wPx  - pivot.x,  hPx  - pivot.y, 0.0f),
                    new Vector3(0.0f - pivot.x,  hPx  - pivot.y, 0.0f)
                };
            }

            Matrix4x4 basis = s.Matrix;
            basis.m30 = s.X;
            basis.m31 = s.Y;
            basis.m32 = SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y);

            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = TransformOriginalMatrix4DPointV19LikeOriginal(basis, local[i]);
                verts[i] = OriginalWallXYZToWorldV6LikeOriginal(p.x, p.y, p.z + desc.FixHeight);
            }

            string safePath = string.IsNullOrWhiteSpace(path) ? "UnifiedFence2D_V139" : path;
            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V139_" + safePath + "_" + desc.Name, verts);
        }

        private Mesh BuildSavedMapWallSpriteExplicitFenceLineCardMeshV140LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            float wPx,
            float hPx,
            string path)
        {
            if (s == null || desc == null || !s.HasMatrix)
                return null;

            // V140: do not use Matrix4D as a full transform for WL fences.
            // Previous patches rebuilt anchors correctly, but final vertices still inherited Matrix4D drift/tilt.
            // This builder constructs the final quad directly in world space:
            //   bottom center = rebuilt saved WL X/Y on terrain;
            //   horizontal axis = run line stored in Matrix4D ground-X;
            //   vertical axis = pure Unity up;
            //   bottom edge is terrain-anchored, so small fences cannot fall through the ground.
            Vector2 originalDir = new Vector2(s.Matrix.m00, s.Matrix.m01);
            if (originalDir.sqrMagnitude <= 0.000001f)
                originalDir = Vector2.right;
            originalDir.Normalize();

            float originalUnitsPerPixel = new Vector2(s.Matrix.m00, s.Matrix.m01).magnitude;
            if (originalUnitsPerPixel < 0.0001f)
                originalUnitsPerPixel = WallOriginalXYUnitToWorldScaleV8LikeOriginal() > 0.0001f ? 1.0f : 1.0f;

            float explicitStepOriginalUnits = Mathf.Abs(s.Matrix.m03);
            float originalHalfWidth = explicitStepOriginalUnits > 0.0001f
                ? explicitStepOriginalUnits * 0.5f
                : Mathf.Max(1.0f, wPx * originalUnitsPerPixel * 0.5f);

            Vector3 bottomCenter = WallOriginalXYToWorldV1LikeOriginal(s.X, s.Y, desc.FixHeight);
            Vector3 lineSample = WallOriginalXYToWorldV1LikeOriginal(
                s.X + originalDir.x * 32.0f,
                s.Y + originalDir.y * 32.0f,
                desc.FixHeight);

            Vector3 lineWorld = lineSample - bottomCenter;
            lineWorld.y = 0.0f;
            if (lineWorld.sqrMagnitude <= 0.000001f)
                lineWorld = Vector3.right;
            lineWorld.Normalize();

            float widthWorld = Mathf.Max(0.01f, originalHalfWidth * 2.0f * WallOriginalXYUnitToWorldScaleV8LikeOriginal());

            float verticalOriginalUnitsPerPixel = Mathf.Abs(s.Matrix.m12);
            if (verticalOriginalUnitsPerPixel < 0.0001f)
            {
                verticalOriginalUnitsPerPixel = new Vector3(s.Matrix.m10, s.Matrix.m11, s.Matrix.m12).magnitude;
                if (verticalOriginalUnitsPerPixel < 0.0001f)
                    verticalOriginalUnitsPerPixel = originalUnitsPerPixel;
            }

            float heightWorld = Mathf.Max(0.01f, hPx * verticalOriginalUnitsPerPixel * WallOriginalZUnitToWorldScaleV8LikeOriginal());

            Vector3 half = lineWorld * (widthWorld * 0.5f);
            Vector3 up = Vector3.up * heightWorld;

            Vector3[] verts =
            {
                bottomCenter - half + up,
                bottomCenter + half + up,
                bottomCenter + half,
                bottomCenter - half
            };

            string safePath = string.IsNullOrWhiteSpace(path) ? "UnifiedFence2D_V142_IdenticalJointToJointLineCard" : path;
            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V142_" + safePath + "_" + desc.Name, verts);
        }

        private static Vector2 GetWallUnifiedFencePivotPxV135LikeOriginal(WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (desc == null)
                return new Vector2(wPx * 0.5f, hPx * 0.5f);

            return new Vector2(GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx), GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx));
        }



private static Vector2 GetWallLargeFencePivotPxV118LikeOriginal(WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (desc == null)
                return new Vector2(wPx * 0.5f, hPx);

            if (desc.Width > 0 || desc.Height > 0)
                return new Vector2(GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx), GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx));

            if (desc.AlignPoints != null && desc.AlignPoints.Count > 0)
                return new Vector2(desc.AlignPoints[0].x, desc.AlignPoints[0].y);

            return new Vector2(wPx * 0.5f, hPx);
        }

        private Mesh BuildSavedMapWallSpriteSavedXYBasisMeshV124LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            float wPx,
            float hPx,
            bool flipLocalY,
            string path)
        {
            if (s == null || desc == null || !s.HasMatrix)
                return null;

            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            Vector3[] local;
            if (flipLocalY)
            {
                local = new[]
                {
                    new Vector3(0.0f - cx,  cy - 0.0f, 0.0f),
                    new Vector3(wPx  - cx,  cy - 0.0f, 0.0f),
                    new Vector3(wPx  - cx,  cy - hPx,  0.0f),
                    new Vector3(0.0f - cx,  cy - hPx,  0.0f)
                };
            }
            else
            {
                local = new[]
                {
                    new Vector3(0.0f - cx,  0.0f - cy, 0.0f),
                    new Vector3(wPx  - cx,  0.0f - cy, 0.0f),
                    new Vector3(wPx  - cx,  hPx  - cy, 0.0f),
                    new Vector3(0.0f - cx,  hPx  - cy, 0.0f)
                };
            }

            Matrix4x4 basis = s.Matrix;
            basis.m30 = s.X;
            basis.m31 = s.Y;
            basis.m32 = SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y);

            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = TransformOriginalMatrix4DPointV19LikeOriginal(basis, local[i]);
                verts[i] = OriginalWallXYZToWorldV6LikeOriginal(p.x, p.y, p.z + desc.FixHeight);
            }

            string safePath = string.IsNullOrWhiteSpace(path) ? "SavedXYBasisProp_V124" : path;
            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V124_" + safePath + "_" + desc.Name, verts);
        }

        private Mesh BuildSavedMapWallSpriteSavedM4MeshV21LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx, bool flipLocalY, string path)
        {
            Mesh mesh = BuildSavedMapWallSpriteSavedM4MeshV19LikeOriginal(s, desc, wPx, hPx, flipLocalY);
            if (mesh != null)
                mesh.name = "C2_SavedMapWallSprite_V21_" + (string.IsNullOrWhiteSpace(path) ? "SavedM4" : path) + "_" + desc.Name;
            return mesh;
        }

        private Mesh BuildSavedMapWallSpriteSavedM4MeshV19LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx, bool flipLocalY)
        {
            if (s == null || desc == null || !s.HasMatrix)
                return null;

            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            Vector3[] local;
            if (flipLocalY)
            {
                // Cossacks sprite frame space is top-left; saved Matrix4D object space is Y-up.
                // This path is for fence/prop-like saved WL only: no camera billboard and no affine V/H warp.
                local = new[]
                {
                    new Vector3(0.0f - cx,  cy - 0.0f, 0.0f),
                    new Vector3(wPx  - cx,  cy - 0.0f, 0.0f),
                    new Vector3(wPx  - cx,  cy - hPx,  0.0f),
                    new Vector3(0.0f - cx,  cy - hPx,  0.0f)
                };
            }
            else
            {
                local = new[]
                {
                    new Vector3(0.0f - cx,  0.0f - cy, 0.0f),
                    new Vector3(wPx  - cx,  0.0f - cy, 0.0f),
                    new Vector3(wPx  - cx,  hPx  - cy, 0.0f),
                    new Vector3(0.0f - cx,  hPx  - cy, 0.0f)
                };
            }

            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = TransformOriginalMatrix4DPointV19LikeOriginal(s.Matrix, local[i]);
                verts[i] = OriginalWallXYZToWorldV6LikeOriginal(p.x, p.y, p.z + desc.FixHeight);
            }

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V19_SavedM4_" + desc.Name, verts);
        }

        private static Vector3 TransformOriginalMatrix4DPointV19LikeOriginal(Matrix4x4 m, Vector3 p)
        {
            // Original Matrix4D translation is e30/e31/e32. Row-vector convention:
            // x' = x*e00 + y*e10 + z*e20 + e30, etc.
            return new Vector3(
                p.x * m.m00 + p.y * m.m10 + p.z * m.m20 + m.m30,
                p.x * m.m01 + p.y * m.m11 + p.z * m.m21 + m.m31,
                p.x * m.m02 + p.y * m.m12 + p.z * m.m22 + m.m32
            );
        }

        private Mesh AlignWallFenceMeshBottomJointToRebuiltLineTargetV147LikeOriginal(
            Mesh source,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            Vector2 rebuiltOriginalXY,
            out string audit)
        {
            audit = "V147_REAL_JOINT_ALIGN skipped";
            if (source == null || s == null || desc == null)
                return source;

            Vector3[] verts = source.vertices;
            if (verts == null || verts.Length == 0)
            {
                audit = "V147_REAL_JOINT_ALIGN no_vertices";
                return source;
            }

            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].y < minY) minY = verts[i].y;
                if (verts[i].y > maxY) maxY = verts[i].y;
            }

            if (!float.IsFinite(minY) || !float.IsFinite(maxY))
            {
                audit = "V147_REAL_JOINT_ALIGN invalid_bounds";
                return source;
            }

            // Real visible joint = bottom edge/line of the already-built WALLS mesh,
            // not the saved WL anchor. Previous patches aligned the anchor and left
            // the actual visible bottom/joint offset untouched.
            float yBand = Mathf.Max(0.01f, Mathf.Min(1.0f, (maxY - minY) * 0.015f));
            Vector3 jointSum = Vector3.zero;
            int jointCount = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].y <= minY + yBand)
                {
                    jointSum += verts[i];
                    jointCount++;
                }
            }

            if (jointCount == 0)
            {
                audit = "V147_REAL_JOINT_ALIGN no_bottom_joint";
                return source;
            }

            Vector3 bottomJoint = jointSum / Mathf.Max(1, jointCount);
            float targetOriginalZ = SampleWallHeightOriginalXYV1LikeOriginal(rebuiltOriginalXY.x, rebuiltOriginalXY.y) + desc.FixHeight;
            Vector3 targetWorld = OriginalWallXYZToWorldV6LikeOriginal(rebuiltOriginalXY.x, rebuiltOriginalXY.y, targetOriginalZ);

            // Only move in map plane. Keep vertical result from original route/clamp/sink,
            // so small fences do not fall through terrain again.
            Vector3 delta = new Vector3(targetWorld.x - bottomJoint.x, 0.0f, targetWorld.z - bottomJoint.z);
            if (delta.sqrMagnitude < 0.000001f)
            {
                audit = "V147_REAL_JOINT_ALIGN dxz=0 yPreserved=True";
                return source;
            }

            Mesh mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = (source.name ?? "WallMesh") + "_V147_real_bottom_joint_aligned";
            Vector3[] moved = mesh.vertices;
            for (int i = 0; i < moved.Length; i++)
                moved[i] += delta;

            mesh.vertices = moved;
            mesh.uv = source.uv;
            mesh.colors32 = source.colors32;
            mesh.triangles = source.triangles;
            mesh.RecalculateBounds();

            audit =
                "V147_REAL_JOINT_ALIGN realBottomJoint=(" +
                bottomJoint.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                bottomJoint.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                " targetWorldXZ=(" +
                targetWorld.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                targetWorld.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                " deltaXZ=(" +
                delta.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                delta.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                " jointVerts=" + jointCount.ToString(CultureInfo.InvariantCulture) +
                " yPreserved=True realJointAlign=True markerV148=True";

            return mesh;
        }

        private Mesh ApplyWallSavedSpriteProfileEmbedV19LikeOriginal(Mesh source, WallSpriteDescV1LikeOriginal desc, WallSavedWLProfileV18LikeOriginal profile)
        {
            // V165: no legacy bridge/fence per-card embed. WALS 2D fences are emitted only by line-root.
            return source;
        }

        private Mesh BuildSavedMapWallSpriteMeshV6LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, Texture2D tex)
        {
            if (s == null || desc == null)
                return null;

            // V13:
            // Camera/terrain/roads are not suspects here. The variable under test is only the WL sprite basis.
            // Default V12Aligned keeps the current best aligned result. F2 cycles alternative bases without touching camera.
            float wPx = tex != null && tex != Texture2D.whiteTexture ? Mathf.Max(8.0f, tex.width) : Mathf.Max(8.0f, desc.Width * 2.0f);
            float hPx = tex != null && tex != Texture2D.whiteTexture ? Mathf.Max(8.0f, tex.height) : Mathf.Max(8.0f, desc.Height * 2.0f);

            if (C2WallObjectsV16UseRigidSavedWLSpriteCardsLikeOriginal)
            {
                Mesh rigid = BuildSavedMapWallSpriteRigidFrozenCameraCardV16LikeOriginal(s, desc, wPx, hPx);
                if (rigid != null)
                    return rigid;
            }

            Mesh mesh = null;
            if (C2WallObjectsV11UseUniversalDepthlessCardForSavedWL)
                mesh = BuildSavedMapWallSpriteUniversalDepthlessCardV11LikeOriginal(s, desc, wPx, hPx);

            if (mesh == null && C2WallObjectsV10UseAdaptedBuilderForMapSpritesLikeOriginal)
            {
                if ((desc.AlignMode == 'V' || desc.AlignMode == 'S') && desc.AlignPoints.Count >= 2)
                    mesh = BuildSavedMapWallSpriteVerticalAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

                if (mesh == null && desc.AlignMode == 'H')
                    mesh = BuildSavedMapWallSpriteGroundAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

                if (mesh == null && desc.AlignMode == 'U' && desc.AlignPoints.Count >= 3)
                    mesh = BuildSavedMapWallSpriteUniversalAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

                if (mesh == null)
                    mesh = BuildSavedMapWallSpriteBillboardAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);
            }

            if (mesh == null)
                mesh = BuildSavedMapWallSpriteBillboardAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

            Mesh basisMesh = ApplyWallSpriteBasisV13LikeOriginal(mesh, s, desc, wPx, hPx);
            return ApplyWallSavedSpriteEmbedV15LikeOriginal(basisMesh, desc);
        }



        private void EnsureWallObjectsV16FrozenCameraBasisLikeOriginal()
        {
            if (_c2WallObjectsV16FrozenCameraBasisReadyLikeOriginal)
                return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                _c2WallObjectsV16FrozenCameraRightLikeOriginal = cam.transform.right;
                _c2WallObjectsV16FrozenCameraUpLikeOriginal = cam.transform.up;
            }
            else
            {
                _c2WallObjectsV16FrozenCameraRightLikeOriginal = Vector3.right;
                _c2WallObjectsV16FrozenCameraUpLikeOriginal = Vector3.up;
            }

            if (_c2WallObjectsV16FrozenCameraRightLikeOriginal.sqrMagnitude < 0.001f)
                _c2WallObjectsV16FrozenCameraRightLikeOriginal = Vector3.right;
            if (_c2WallObjectsV16FrozenCameraUpLikeOriginal.sqrMagnitude < 0.001f)
                _c2WallObjectsV16FrozenCameraUpLikeOriginal = Vector3.up;

            _c2WallObjectsV16FrozenCameraRightLikeOriginal.Normalize();
            _c2WallObjectsV16FrozenCameraUpLikeOriginal.Normalize();
            _c2WallObjectsV16FrozenCameraBasisReadyLikeOriginal = true;
        }

        private Mesh BuildSavedMapWallSpriteRigidFrozenCameraCardV16LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null)
                return null;

            EnsureWallObjectsV16FrozenCameraBasisLikeOriginal();

            float xyUnitToWorld = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * C2WallObjectsV16SavedWLExtraScaleLikeOriginal;
            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            float terrain = SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y);
            Vector3 anchor = OriginalWallXYZToWorldV6LikeOriginal(s.X, s.Y, terrain + desc.FixHeight);

            float embedPx = GetWallSavedSpriteEmbedScreenPixelsV16LikeOriginal(desc);
            if (embedPx > 0.001f)
                anchor -= _c2WallObjectsV16FrozenCameraUpLikeOriginal * (embedPx * xyUnitToWorld);

            Vector3 right = _c2WallObjectsV16FrozenCameraRightLikeOriginal;
            Vector3 up = _c2WallObjectsV16FrozenCameraUpLikeOriginal;

            // Exact decoded G16 pixel rectangle. No affine V/H/U warp, no shear, no non-uniform matrix.
            // Original data supplies anchor/pivot/frame; Unity only turns it into one rigid camera-plane card.
            Vector3[] verts =
            {
                anchor + right * ((0.0f - cx) * xyUnitToWorld) + up * ((cy - 0.0f) * xyUnitToWorld),
                anchor + right * ((wPx  - cx) * xyUnitToWorld) + up * ((cy - 0.0f) * xyUnitToWorld),
                anchor + right * ((wPx  - cx) * xyUnitToWorld) + up * ((cy - hPx)  * xyUnitToWorld),
                anchor + right * ((0.0f - cx) * xyUnitToWorld) + up * ((cy - hPx)  * xyUnitToWorld)
            };

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V16_RigidFrozenCameraCard_" + desc.Name, verts);
        }

        private static float GetWallSavedSpriteEmbedScreenPixelsV16LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return 0.0f;

            if (desc.SpriteIndex == 58 || desc.SpriteIndex == 59 || desc.SpriteIndex == 60 || desc.SpriteIndex == 63)
                return C2WallObjectsV16BridgeEmbedScreenPixelsLikeOriginal;

            if (desc.SpriteIndex == 70 || desc.SpriteIndex == 74)
                return C2WallObjectsV16MinorEmbedScreenPixelsLikeOriginal;

            return 0.0f;
        }

        private Mesh ApplyWallSavedSpriteEmbedV15LikeOriginal(Mesh source, WallSpriteDescV1LikeOriginal desc)
        {
            if (source == null || desc == null)
                return source;

            float embedPx = GetWallSavedSpriteEmbedPixelsV15LikeOriginal(desc);
            if (embedPx <= 0.001f)
                return source;

            Vector3[] verts = source.vertices;
            if (verts == null || verts.Length == 0)
                return source;

            float worldEmbed = embedPx * WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            for (int i = 0; i < verts.Length; i++)
                verts[i].y -= worldEmbed;

            Mesh mesh = new Mesh { name = source.name + "_V15_embed" };
            mesh.vertices = verts;
            mesh.uv = source.uv;
            mesh.colors32 = source.colors32;
            mesh.triangles = source.triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float GetWallSavedSpriteEmbedPixelsV15LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return 0.0f;

            // Bridge/stair edge WL pieces. This is an adapted Unity placement offset:
            // original formulas choose the line and connectors; Unity uses a small visual sink
            // so the edge sits into the panplane instead of floating over it.
            if (desc.SpriteIndex == 58 || desc.SpriteIndex == 59)
                return C2WallObjectsV15BridgeEmbedPixelsLikeOriginal;

            // Smaller fence/ground objects need less lowering.
            if (desc.SpriteIndex == 60 || desc.SpriteIndex == 63 || desc.SpriteIndex == 70 || desc.SpriteIndex == 74)
                return C2WallObjectsV15MinorEmbedPixelsLikeOriginal;

            return 0.0f;
        }


        private Mesh ApplyWallSpriteBasisV13LikeOriginal(Mesh source, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (source == null || s == null || desc == null)
                return source;

            WallSpriteBasisV13LikeOriginal mode = _c2WallObjectsV13BasisModeLikeOriginal;
            if (mode == WallSpriteBasisV13LikeOriginal.V12Aligned)
                return source;

            if (mode == WallSpriteBasisV13LikeOriginal.CameraPlaneCenterPivot || mode == WallSpriteBasisV13LikeOriginal.CameraPlaneBottomPivot)
            {
                Mesh cameraMesh = BuildSavedMapWallSpriteCameraPlaneBasisV13LikeOriginal(s, desc, wPx, hPx, mode == WallSpriteBasisV13LikeOriginal.CameraPlaneBottomPivot);
                if (cameraMesh != null)
                    return cameraMesh;
                return source;
            }

            Vector3[] verts = source.vertices;
            if (verts == null || verts.Length == 0)
                return source;

            Vector3 anchor = OriginalWallXYZToWorldV6LikeOriginal(s.X, s.Y, SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y) + desc.FixHeight);
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 r = verts[i] - anchor;
                if (mode == WallSpriteBasisV13LikeOriginal.FlipVertical)
                {
                    r.y = -r.y;
                }
                else if (mode == WallSpriteBasisV13LikeOriginal.FlipForward)
                {
                    r.z = -r.z;
                }
                else if (mode == WallSpriteBasisV13LikeOriginal.SwapVerticalForward)
                {
                    float oldY = r.y;
                    r.y = r.z;
                    r.z = oldY;
                }
                verts[i] = anchor + r;
            }

            Mesh mesh = new Mesh { name = source.name + "_V13_" + mode };
            mesh.vertices = verts;
            Vector2[] uv = source.uv;
            mesh.uv = (uv != null && uv.Length == verts.Length) ? uv : GetWallSpriteQuadUvV8LikeOriginal();
            Color32[] colors = source.colors32;
            mesh.colors32 = (colors != null && colors.Length == verts.Length)
                ? colors
                : new[] { new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255) };
            mesh.triangles = source.triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildSavedMapWallSpriteCameraPlaneBasisV13LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx, bool bottomPivot)
        {
            if (s == null || desc == null)
                return null;

            float xyUnitToWorld = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = bottomPivot ? hPx : GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            float terrain = SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y);
            Vector3 anchor = OriginalWallXYZToWorldV6LikeOriginal(s.X, s.Y, terrain + desc.FixHeight);

            Camera cam = Camera.main;
            Vector3 right = Vector3.right;
            Vector3 up = Vector3.up;
            if (cam != null)
            {
                right = cam.transform.right;
                up = cam.transform.up;
            }

            right.Normalize();
            up.Normalize();

            // Original frames are authored as camera-facing art. This mode tests that idea explicitly:
            // local pixel X -> camera right, local pixel Y -> camera up, with Y inverted from top-left image coordinates.
            Vector3[] verts =
            {
                anchor + right * ((0.0f - cx) * xyUnitToWorld) + up * ((cy - 0.0f) * xyUnitToWorld),
                anchor + right * ((wPx  - cx) * xyUnitToWorld) + up * ((cy - 0.0f) * xyUnitToWorld),
                anchor + right * ((wPx  - cx) * xyUnitToWorld) + up * ((cy - hPx)  * xyUnitToWorld),
                anchor + right * ((0.0f - cx) * xyUnitToWorld) + up * ((cy - hPx)  * xyUnitToWorld)
            };

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V13_CameraPlane_" + (bottomPivot ? "Bottom_" : "Center_") + desc.Name, verts);
        }

        private string DescribeWallSpriteBasisVectorsV13LikeOriginal(Mesh mesh)
        {
            if (mesh == null || mesh.vertices == null || mesh.vertices.Length < 4)
                return "basisVectors=missing";
            Vector3[] v = mesh.vertices;
            Vector3 right = v[1] - v[0];
            Vector3 down = v[3] - v[0];
            Vector3 up = -down;

            string screen = string.Empty;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 s0 = cam.WorldToScreenPoint(v[0]);
                Vector3 sr = cam.WorldToScreenPoint(v[1]) - s0;
                Vector3 su = cam.WorldToScreenPoint(v[3]) - s0;
                screen = " screenRight=(" + sr.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                           sr.y.ToString("0.###", CultureInfo.InvariantCulture) + ") " +
                         "screenDown=(" + su.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                          su.y.ToString("0.###", CultureInfo.InvariantCulture) + ")";
            }

            return "right=(" + right.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                              right.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                              right.z.ToString("0.###", CultureInfo.InvariantCulture) + ") " +
                   "up=(" + up.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           up.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                           up.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" + screen;
        }

        private string DescribeWallSpriteCornersV13LikeOriginal(Mesh mesh)
        {
            if (mesh == null || mesh.vertices == null || mesh.vertices.Length < 4)
                return "corners=missing";
            Vector3[] v = mesh.vertices;
            return "corners=[" +
                   FormatWallVectorV13LikeOriginal(v[0]) + "][" +
                   FormatWallVectorV13LikeOriginal(v[1]) + "][" +
                   FormatWallVectorV13LikeOriginal(v[2]) + "][" +
                   FormatWallVectorV13LikeOriginal(v[3]) + "]";
        }

        private static string FormatWallVectorV13LikeOriginal(Vector3 v)
        {
            return v.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   v.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   v.z.ToString("0.###", CultureInfo.InvariantCulture);
        }


        private static string GetSavedWallSpriteAdaptedDrawPathV10LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (C2WallObjectsV16UseRigidSavedWLSpriteCardsLikeOriginal)
                return "RIGID_SAVED_WL_CARD_V16";
            if (C2WallObjectsV11UseUniversalDepthlessCardForSavedWL)
                return "UNIVERSAL_CARD_DEPTHLESS";
            if (desc == null)
                return "ADAPTED_BILLBOARD";
            if ((desc.AlignMode == 'V' || desc.AlignMode == 'S') && desc.AlignPoints.Count >= 2)
                return "ADAPTED_ALIGN_V";
            if (desc.AlignMode == 'H')
                return "ADAPTED_ALIGN_H";
            if (desc.AlignMode == 'U' && desc.AlignPoints.Count >= 3)
                return "ADAPTED_ALIGN_U";
            return "ADAPTED_BILLBOARD";
        }

        private Mesh BuildSavedMapWallSpriteUniversalDepthlessCardV11LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null)
                return null;

            // V11 universal rule:
            // Saved WL records from M3D are already chosen/ordered by the original map.
            // For Unity we do NOT rebuild them as terrain-cut 3D quads and do NOT apply raw DirectX Matrix4D.
            // We use the original data as formulas only:
            //   sprite frame + original pivot/center + terrain anchor + 2.5D upright sprite card.
            // ZTest Always is handled by WallObjectSpriteV7 so terrain cannot eat/slice these cards.
            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);
            float xyUnitToWorld = WallOriginalXYUnitToWorldScaleV8LikeOriginal();

            float terrain = SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y);
            Vector3 anchor = OriginalWallXYZToWorldV6LikeOriginal(s.X, s.Y, terrain + desc.FixHeight);

            Vector3 right = Vector3.right * xyUnitToWorld;
            Vector3 up = Vector3.up * xyUnitToWorld;

            // Sprite local coordinates are top-left. Pivot is CenterX/CenterY from walls.lst.
            // World vertical must be cy - pixelY; otherwise fences/bridge pieces/huts appear upside-down.
            Vector3[] verts =
            {
                anchor + right * (0.0f - cx)   + up * (cy - 0.0f),
                anchor + right * (wPx - cx)    + up * (cy - 0.0f),
                anchor + right * (wPx - cx)    + up * (cy - hPx),
                anchor + right * (0.0f - cx)   + up * (cy - hPx)
            };

            // Keep the full visible sprite above the terrain anchor. Alpha pixels still form the real silhouette.
            float minY = verts[0].y;
            for (int i = 1; i < verts.Length; i++)
                minY = Mathf.Min(minY, verts[i].y);

            float allowedBelow = Mathf.Max(0.0f, C2WallObjectsV11AllowedBelowGroundPixels) * xyUnitToWorld;
            float floorY = anchor.y - allowedBelow;
            if (minY < floorY)
            {
                Vector3 lift = Vector3.up * (floorY - minY);
                for (int i = 0; i < verts.Length; i++)
                    verts[i] += lift;
            }

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V11_UniversalCard_DisabledByV12_" + desc.Name, verts);
        }

        private Mesh BuildSavedMapWallSpriteVerticalAdaptedMeshV10LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null || desc.AlignPoints.Count < 2)
                return null;

            // V174: literal MapSprites/Scape3D vertical ALIGNING path.
            // Original:
            //   *M4 = GetAlignLineTransformWithScape(Vector3D(x,y,0),
            //       Vector3D(OC->CenterX,OC->CenterY,0), va_x1,va_y1,va_x2,va_y2);
            //
            // The important fix is coordinate space:
            // original MapSprites/SkewPt uses linear map X/Y directly. It does NOT add the terrain
            // mesh odd-column backing offset. The previous Unity port routed these WL cards through
            // OriginalWallXYZToWorldV6LikeOriginal(), which adds the terrain backing odd-column offset
            // and makes long saved WL fence chains visibly zig-zag/warp.
            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            float x1 = desc.AlignPoints[0].x;
            float y1 = desc.AlignPoints[0].y;
            float x2 = desc.AlignPoints[1].x;
            float y2 = desc.AlignPoints[1].y;

            float mapX1 = s.X + x1 - cx;
            float mapY1 = s.Y + 2.0f * (y1 - cy);
            float mapX2 = s.X + x2 - cx;
            float mapY2 = s.Y + 2.0f * (y2 - cy);

            // Original z1/z2 are raw GetHeight samples. FixHeight/OS.z are not added in
            // OneSprite::CreateMatrix for atVertical.
            float z1 = SampleWallHeightOriginalXYV1LikeOriginal(mapX1, mapY1);
            float z2 = SampleWallHeightOriginalXYV1LikeOriginal(mapX2, mapY2);

            const float dz = 128.0f;
            float bias = 0.2f;
            if (y1 < y2)
                bias = -bias;

            Vector2 s1 = new Vector2(x1, y1);
            Vector2 s2 = new Vector2(x2, y2);
            Vector2 s3 = new Vector2((x1 + x2) * 0.5f, (y1 + y2) * 0.5f - dz);

            // Original:
            //   BV=(0,-bias,bias/2)
            //   W1=SkewPt(mapX1,mapY1,z1)+BV
            //   W2=SkewPt(mapX2,mapY2,z2)-BV
            //   W3=SkewPt(Center.x,Center.y,(z1+z2)/2+dz)
            // Unity keeps the same semantic points but converts MapSprites linear X/Y to Unity world
            // without terrain-geometry odd-column offset.
            Vector3 w1 = OriginalWallMapSpriteXYZToWorldV174LikeOriginal(mapX1, mapY1 - bias, z1 + bias * 0.5f);
            Vector3 w2 = OriginalWallMapSpriteXYZToWorldV174LikeOriginal(mapX2, mapY2 + bias, z2 - bias * 0.5f);
            Vector3 w3 = OriginalWallMapSpriteXYZToWorldV174LikeOriginal(s.X, s.Y, (z1 + z2) * 0.5f + dz);

            Vector3[] verts =
            {
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, 0.0f), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, 0.0f), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, hPx), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, hPx), s1, s2, s3, w1, w2, w3)
            };

            ApplyWLSavedSpriteSideShadowLiftV175LikeOriginal(desc, verts);

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V175_VAlignLinearMapXYSideShadowLift_" + desc.Name, verts);
        }

        private static bool IsWallFenceVerticalTopBottomFrameV178LikeOriginal(int spriteIndex)
        {
            for (int i = 0; i < C2WallObjectsV152FencePairsLikeOriginal.Length; i++)
            {
                if (spriteIndex == C2WallObjectsV152FencePairsLikeOriginal[i].TopBottom)
                    return true;
            }

            return false;
        }

        private static bool IsWallFenceHorizontalLeftRightFrameV178LikeOriginal(int spriteIndex)
        {
            for (int i = 0; i < C2WallObjectsV152FencePairsLikeOriginal.Length; i++)
            {
                if (spriteIndex == C2WallObjectsV152FencePairsLikeOriginal[i].LeftRight)
                    return true;
            }

            return false;
        }

        private float ResolveWals2DHeightRaisePixelsV178LikeOriginal(int spriteIndex)
        {
            EnsureWals2DHeightInstructionLoadedV178LikeOriginal();
            if (IsWallFenceVerticalTopBottomFrameV178LikeOriginal(spriteIndex))
                return _c2Wals2DVerticalRaisePixelsV178LikeOriginal;
            if (IsWallFenceHorizontalLeftRightFrameV178LikeOriginal(spriteIndex))
                return _c2Wals2DHorizontalRaisePixelsV178LikeOriginal;
            return 0.0f;
        }

        private void ApplyWLSavedSpriteSideShadowLiftV175LikeOriginal(WallSpriteDescV1LikeOriginal desc, Vector3[] verts)
        {
            if (desc == null || verts == null || verts.Length == 0)
                return;

            float raisePx = ResolveWals2DHeightRaisePixelsV178LikeOriginal(desc.SpriteIndex);
            if (Mathf.Abs(raisePx) <= 0.0001f)
                return;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            Vector3 lift = Vector3.up * (raisePx * kernel.HeightScale);
            for (int i = 0; i < verts.Length; i++)
                verts[i] += lift;
        }

        private void RegisterWals2DHeightAdjustableMeshV178LikeOriginal(Mesh mesh, WallSpriteDescV1LikeOriginal desc)
        {
            if (mesh == null || desc == null)
                return;

            bool vertical = IsWallFenceVerticalTopBottomFrameV178LikeOriginal(desc.SpriteIndex);
            bool horizontal = IsWallFenceHorizontalLeftRightFrameV178LikeOriginal(desc.SpriteIndex);
            if (!vertical && !horizontal)
                return;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            float appliedRaise = vertical ? _c2Wals2DVerticalRaisePixelsV178LikeOriginal : _c2Wals2DHorizontalRaisePixelsV178LikeOriginal;
            Vector3 appliedLift = Vector3.up * (appliedRaise * kernel.HeightScale);
            Vector3[] verts = mesh.vertices;
            Vector3[] baseVerts = verts != null ? (Vector3[])verts.Clone() : null;
            if (baseVerts == null || baseVerts.Length == 0)
                return;

            if (Mathf.Abs(appliedRaise) > 0.0001f)
            {
                for (int i = 0; i < baseVerts.Length; i++)
                    baseVerts[i] -= appliedLift;
            }

            _c2Wals2DHeightAdjustRecordsV178LikeOriginal.Add(new Wals2DHeightAdjustRecordV178LikeOriginal
            {
                Mesh = mesh,
                BaseVertices = baseVerts,
                SpriteIndex = desc.SpriteIndex,
                VerticalTopBottom = vertical,
                HorizontalLeftRight = horizontal,
                AppliedRaisePixels = appliedRaise
            });
        }

        private void ApplyWals2DHeightSlidersToLiveMeshesV178LikeOriginal()
        {
            if (_c2Wals2DHeightAdjustRecordsV178LikeOriginal == null || _c2Wals2DHeightAdjustRecordsV178LikeOriginal.Count == 0)
                return;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            for (int r = _c2Wals2DHeightAdjustRecordsV178LikeOriginal.Count - 1; r >= 0; r--)
            {
                Wals2DHeightAdjustRecordV178LikeOriginal rec = _c2Wals2DHeightAdjustRecordsV178LikeOriginal[r];
                if (rec == null || rec.Mesh == null || rec.BaseVertices == null || rec.BaseVertices.Length == 0)
                {
                    _c2Wals2DHeightAdjustRecordsV178LikeOriginal.RemoveAt(r);
                    continue;
                }

                float raisePx = rec.VerticalTopBottom ? _c2Wals2DVerticalRaisePixelsV178LikeOriginal : _c2Wals2DHorizontalRaisePixelsV178LikeOriginal;
                Vector3 lift = Vector3.up * (raisePx * kernel.HeightScale);
                Vector3[] verts = new Vector3[rec.BaseVertices.Length];
                for (int i = 0; i < verts.Length; i++)
                    verts[i] = rec.BaseVertices[i] + lift;

                rec.Mesh.vertices = verts;
                rec.Mesh.RecalculateBounds();
                rec.AppliedRaisePixels = raisePx;
            }
        }

        private Mesh BuildSavedMapWallSpriteGroundAdaptedMeshV10LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null)
                return null;

            // Original OneSprite::CreateMatrix atGround builds three support points around CenterX/CenterY
            // and samples terrain. Unity version keeps the same three-point plane idea.
            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            const float tbias = -16.0f;
            const float cos30 = 0.866025f;
            float tbiasY = tbias * cos30;
            float tbiasZ = tbias * 0.5f;
            float bias = 0.0f;

            Vector2 p1 = new Vector2(cx - 20.0f, cy - 5.0f);
            Vector2 p2 = new Vector2(cx + 20.0f, cy - 5.0f);
            Vector2 p3 = new Vector2(cx,        cy + 10.0f);

            float wx1 = -20.0f;
            float wy1 = -10.0f - tbiasY;
            float wz1 = bias - tbiasZ;

            float wx2 = 20.0f;
            float wy2 = -10.0f - tbiasY;
            float wz2 = bias - tbiasZ;

            float wx3 = 0.0f;
            float wy3 = 20.0f - tbiasY;
            float wz3 = bias - tbiasZ;

            float ox1 = s.X + wx1;
            float oy1 = s.Y + wy1;
            float ox2 = s.X + wx2;
            float oy2 = s.Y + wy2;
            float ox3 = s.X + wx3;
            float oy3 = s.Y + wy3;

            float h1 = SampleWallHeightOriginalXYV1LikeOriginal(ox1, oy1);
            float h2 = SampleWallHeightOriginalXYV1LikeOriginal(ox2, oy2);
            float h3 = SampleWallHeightOriginalXYV1LikeOriginal(ox3, oy3);

            Vector3 w1 = OriginalWallMapSpriteXYZToWorldV174LikeOriginal(ox1, oy1, h1 + wz1 + desc.FixHeight);
            Vector3 w2 = OriginalWallMapSpriteXYZToWorldV174LikeOriginal(ox2, oy2, h2 + wz2 + desc.FixHeight);
            Vector3 w3 = OriginalWallMapSpriteXYZToWorldV174LikeOriginal(ox3, oy3, h3 + wz3 + desc.FixHeight);

            Vector3[] verts =
            {
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, 0.0f), p1, p2, p3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, 0.0f), p1, p2, p3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, hPx), p1, p2, p3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, hPx), p1, p2, p3, w1, w2, w3)
            };

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V10_HAlign_" + desc.Name, verts);
        }

        private Mesh BuildSavedMapWallSpriteUniversalAdaptedMeshV10LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null || desc.AlignPoints.Count < 3)
                return null;

            // U-align in the active original parser is not a runtime Matrix4D path, but the data is useful:
            // three local sprite support points form a semantic plane. Use it as an adapted Unity plane.
            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);

            Vector3 a = desc.AlignPoints[0];
            Vector3 b = desc.AlignPoints[1];
            Vector3 c = desc.AlignPoints[2];

            Vector2 s1 = new Vector2(a.x, a.y);
            Vector2 s2 = new Vector2(b.x, b.y);
            Vector2 s3 = new Vector2(c.x, c.y);

            Vector3 w1 = BuildWallWorldPointFromLocalUAlignV10LikeOriginal(s, desc, cx, cy, a);
            Vector3 w2 = BuildWallWorldPointFromLocalUAlignV10LikeOriginal(s, desc, cx, cy, b);
            Vector3 w3 = BuildWallWorldPointFromLocalUAlignV10LikeOriginal(s, desc, cx, cy, c);

            Vector3[] verts =
            {
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, 0.0f), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, 0.0f), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, hPx), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, hPx), s1, s2, s3, w1, w2, w3)
            };

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V10_UAlign_" + desc.Name, verts);
        }

        private Vector3 BuildWallWorldPointFromLocalUAlignV10LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float cx, float cy, Vector3 local)
        {
            float ox = s.X + (local.x - cx);
            float oy = s.Y + 2.0f * (local.y - cy);
            float terrain = SampleWallHeightOriginalXYV1LikeOriginal(ox, oy);
            return OriginalWallXYZToWorldV6LikeOriginal(ox, oy, terrain + local.z + desc.FixHeight);
        }

        private Mesh BuildSavedMapWallSpriteBillboardAdaptedMeshV10LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null)
                return null;

            float cx = GetWallSpriteCenterXPxV10LikeOriginal(desc, wPx);
            float cy = GetWallSpriteCenterYPxV10LikeOriginal(desc, hPx);
            float xyUnitToWorld = WallOriginalXYUnitToWorldScaleV8LikeOriginal();

            Vector3 center = WallOriginalXYToWorldV1LikeOriginal(s.X, s.Y, desc.FixHeight);
            Vector3 right = Vector3.right;
            Vector3 up = Vector3.up;

            Vector3[] verts =
            {
                center + right * ((0.0f - cx) * xyUnitToWorld) + up * ((0.0f - cy) * xyUnitToWorld),
                center + right * ((wPx - cx) * xyUnitToWorld) + up * ((0.0f - cy) * xyUnitToWorld),
                center + right * ((wPx - cx) * xyUnitToWorld) + up * ((hPx - cy) * xyUnitToWorld),
                center + right * ((0.0f - cx) * xyUnitToWorld) + up * ((hPx - cy) * xyUnitToWorld)
            };

            return CreateWallQuadMeshV10AdaptedLikeOriginal("C2_SavedMapWallSprite_V10_Billboard_" + desc.Name, verts);
        }

        private static float GetWallSpriteCenterXPxV10LikeOriginal(WallSpriteDescV1LikeOriginal desc, float wPx)
        {
            return desc != null && desc.Width > 0 ? desc.Width : wPx * 0.5f;
        }

        private static float GetWallSpriteCenterYPxV10LikeOriginal(WallSpriteDescV1LikeOriginal desc, float hPx)
        {
            return desc != null && desc.Height > 0 ? desc.Height : hPx * 0.5f;
        }

        private static Mesh CreateWallQuadMeshV10AdaptedLikeOriginal(string name, Vector3[] verts)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = GetWallSpriteQuadUvV8LikeOriginal();
            mesh.colors32 = new[]
            {
                new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        

        private Mesh BuildSavedMapWallSpriteUAlignMeshV8LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null || desc.AlignPoints.Count < 3)
                return null;

            Vector3 center = WallOriginalXYToWorldV1LikeOriginal(s.X, s.Y, desc.FixHeight);
            float xyUnitToWorld = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            float zUnitToWorld = WallOriginalZUnitToWorldScaleV8LikeOriginal();
            Vector2 pivot = GetWallSpritePivotPxV8LikeOriginal(desc, wPx, hPx);
            float pivotPlaneZ = EvaluateWallUAlignPlaneZV4LikeOriginal(desc, pivot.x, pivot.y);

            Vector2[] px =
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(wPx, 0.0f),
                new Vector2(wPx, hPx),
                new Vector2(0.0f, hPx)
            };

            Vector3 rightDir = Vector3.right;
            Vector3 forwardDir = new Vector3(0.0f, 0.0f, WorldZSign);

            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float dx = (px[i].x - pivot.x) * xyUnitToWorld;
                float dy = (px[i].y - pivot.y) * xyUnitToWorld;
                float dz = (EvaluateWallUAlignPlaneZV4LikeOriginal(desc, px[i].x, px[i].y) - pivotPlaneZ) * zUnitToWorld;
                verts[i] = center + rightDir * dx + forwardDir * dy + Vector3.up * dz;
            }

            var mesh = new Mesh { name = "C2_SavedMapWallSprite_V8_UAlign_" + desc.Name };
            mesh.vertices = verts;
            mesh.uv = GetWallSpriteQuadUvV8LikeOriginal();
            mesh.colors32 = new[] { new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildSavedMapWallSpriteVAlignMeshV7LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (s == null || desc == null || desc.AlignPoints.Count < 2)
                return null;

            Vector2 pivot = new Vector2(desc.AlignPoints[0].x, desc.AlignPoints[0].y);
            Vector2 p1 = new Vector2(desc.AlignPoints[0].x, desc.AlignPoints[0].y);
            Vector2 p2 = new Vector2(desc.AlignPoints[1].x, desc.AlignPoints[1].y);

            float dx = (p2.y - p1.y) * 0.5f;
            float dy = (p2.x - p1.x);
            float dz = Mathf.Sqrt(dx * dx + dy * dy);
            if (dz < 0.001f)
                return null;

            float ox1 = s.X + (p1.x - pivot.x);
            float oy1 = s.Y + 2.0f * (p1.y - pivot.y);
            float ox2 = s.X + (p2.x - pivot.x);
            float oy2 = s.Y + 2.0f * (p2.y - pivot.y);

            float z1 = SampleWallHeightOriginalXYV1LikeOriginal(ox1, oy1) + desc.FixHeight;
            float z2 = SampleWallHeightOriginalXYV1LikeOriginal(ox2, oy2) + desc.FixHeight;
            float zc = SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y) + desc.FixHeight;
            float topAbsZ = (z1 + z2) * 0.5f + dz;

            Vector3 w1 = WallOriginalXYToWorldV1LikeOriginal(ox1, oy1, z1 - SampleWallHeightOriginalXYV1LikeOriginal(ox1, oy1));
            Vector3 w2 = WallOriginalXYToWorldV1LikeOriginal(ox2, oy2, z2 - SampleWallHeightOriginalXYV1LikeOriginal(ox2, oy2));
            Vector3 w3 = WallOriginalXYToWorldV1LikeOriginal(s.X, s.Y, topAbsZ - SampleWallHeightOriginalXYV1LikeOriginal(s.X, s.Y));

            Vector2 s1 = p1;
            Vector2 s2 = p2;
            Vector2 s3 = new Vector2((p1.x + p2.x) * 0.5f, (p1.y + p2.y) * 0.5f - dz);

            Vector3[] verts =
            {
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, 0.0f), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, 0.0f), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(wPx, hPx), s1, s2, s3, w1, w2, w3),
                AffineMapWallSpritePointV7LikeOriginal(new Vector2(0.0f, hPx), s1, s2, s3, w1, w2, w3)
            };

            var mesh = new Mesh { name = "C2_SavedMapWallSprite_V7_VAlign_" + desc.Name };
            mesh.vertices = verts;
            mesh.uv = GetWallSpriteQuadUvV8LikeOriginal();
            mesh.colors32 = new[] { new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 AffineMapWallSpritePointV7LikeOriginal(Vector2 p, Vector2 s1, Vector2 s2, Vector2 s3, Vector3 w1, Vector3 w2, Vector3 w3)
        {
            Vector2 e1 = s2 - s1;
            Vector2 e2 = s3 - s1;
            float det = e1.x * e2.y - e1.y * e2.x;
            if (Mathf.Abs(det) < 0.0001f)
                return w1;

            Vector2 r = p - s1;
            float a = (r.x * e2.y - r.y * e2.x) / det;
            float b = (e1.x * r.y - e1.y * r.x) / det;
            return w1 + (w2 - w1) * a + (w3 - w1) * b;
        }

        private Vector3 OriginalWallXYZToWorldV6LikeOriginal(float x, float y, float z)
        {
            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            float gx = x / 32.0f;
            float gy = y / 32.0f;
            float rawX = gx * kernel.BackingStepXWorld;
            int ix = Mathf.FloorToInt(gx);
            float rawZ = gy * kernel.BackingStepZWorld + (((ix & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f);
            float worldX = rawX - kernel.CenterX;
            float worldZ = (rawZ - kernel.CenterZ) * WorldZSign;
            float worldY = z * kernel.HeightScale + C2WallObjectsV1YOffsetLikeOriginal;
            return new Vector3(worldX, worldY, worldZ);
        }

        // V174b compile hotfix:
        // Saved WL/WALLS.g16 MapSprites alignment uses linear map X/Y.
        // This is intentionally the same scale/center/height convention as
        // OriginalWallXYZToWorldV6LikeOriginal, but WITHOUT terrain odd-column
        // backing offset. The odd-column offset belongs to terrain backing mesh,
        // not to MapSprites CreateMatrix / DrawWSprite / AddWorldPoint.
        private Vector3 OriginalWallMapSpriteXYZToWorldV174LikeOriginal(float x, float y, float z)
        {
            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            float gx = x / 32.0f;
            float gy = y / 32.0f;
            float rawX = gx * kernel.BackingStepXWorld;
            float rawZ = gy * kernel.BackingStepZWorld;
            float worldX = rawX - kernel.CenterX;
            float worldZ = (rawZ - kernel.CenterZ) * WorldZSign;
            float worldY = z * kernel.HeightScale + C2WallObjectsV1YOffsetLikeOriginal;
            return new Vector3(worldX, worldY, worldZ);
        }

        private bool TryWorldXZToOriginalXYV118LikeOriginal(Vector3 world, out Vector2 original)
        {
            original = Vector2.zero;
            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            if (Mathf.Abs(kernel.BackingStepXWorld) <= 0.000001f ||
                Mathf.Abs(kernel.BackingStepZWorld) <= 0.000001f ||
                Mathf.Abs(WorldZSign) <= 0.000001f)
                return false;

            float gx = (world.x + kernel.CenterX) / kernel.BackingStepXWorld;
            int ix = Mathf.FloorToInt(gx);
            float rawZ = world.z / WorldZSign + kernel.CenterZ;
            float offsetZ = ((ix & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f;
            float gy = (rawZ - offsetZ) / kernel.BackingStepZWorld;
            original = new Vector2(gx * 32.0f, gy * 32.0f);
            return IsFiniteWallFloatV21LikeOriginal(original.x) && IsFiniteWallFloatV21LikeOriginal(original.y);
        }

        private Mesh BuildWallSpriteQuadMeshV1LikeOriginal(WallVisualPointV1LikeOriginal p, WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                desc = new WallSpriteDescV1LikeOriginal { Name = "NULL_DESC", Width = 64, Height = 64 };

            Vector3 center = WallOriginalXYToWorldV1LikeOriginal(p.X, p.Y, p.Z + desc.FixHeight);
            float wPx = Mathf.Max(8.0f, desc.Width);
            float hPx = Mathf.Max(8.0f, desc.Height);

            float a = p.Angle * Mathf.PI / 128.0f;
            Vector3 rightDir = new Vector3(Mathf.Cos(a), 0.0f, -Mathf.Sin(a));
            Vector3 forwardDir = new Vector3(Mathf.Sin(a), 0.0f, Mathf.Cos(a));

            var mesh = new Mesh { name = "C2_WallObjectSpriteMesh_V6_" + desc.Name };

            Vector3[] verts;
            bool useUAlign = desc.AlignMode == 'U' && desc.AlignPoints.Count >= 3;

            if (useUAlign)
            {
                // V4 fix:
                // W48MOST1-W55MOST1 are ALIGNING U elements. They are not upright menu-like
                // billboards. Original code uses three local support points with Z. Here we
                // build a sloped local plane from those points and place the texture on that
                // plane. This is the first Unity-side equivalent of original GetSkewTM().
                Vector2[] px =
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(wPx, 0.0f),
                    new Vector2(wPx, hPx),
                    new Vector2(0.0f, hPx)
                };

                float scaleX = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Mathf.Max(0.001f, p.ScaleP);
                float scaleY = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Mathf.Max(0.001f, p.ScaleO);
                float scaleZ = WallOriginalZUnitToWorldScaleV8LikeOriginal() * Mathf.Max(0.001f, p.ScaleZ);

                Vector2 pivot = GetWallSpritePivotPxV8LikeOriginal(desc, wPx, hPx);
                float cx = pivot.x;
                float cy = pivot.y;
                float centerPlaneZ = EvaluateWallUAlignPlaneZV4LikeOriginal(desc, cx, cy);

                verts = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    float dx = (px[i].x - cx) * scaleX;
                    float dy = (px[i].y - cy) * scaleY;
                    float dz = (EvaluateWallUAlignPlaneZV4LikeOriginal(desc, px[i].x, px[i].y) - centerPlaneZ) * scaleZ;

                    verts[i] = center + rightDir * dx + forwardDir * dy + Vector3.up * dz;
                }
            }
            else
            {
                // Non-U elements remain old upright/quasi-sprite path until their original
                // ALIGNING modes are ported separately.
                float w = wPx * WallOriginalXYUnitToWorldScaleV8LikeOriginal() * p.ScaleP;
                float h = hPx * WallOriginalXYUnitToWorldScaleV8LikeOriginal() * p.ScaleO;
                Vector3 right = rightDir * (w * 0.5f);
                Vector3 up = Vector3.up * h;

                verts = new[]
                {
                    center - right,
                    center + right,
                    center + right + up,
                    center - right + up
                };
            }

            mesh.vertices = verts;
            mesh.uv = GetWallSpriteQuadUvV8LikeOriginal();
            mesh.colors32 = new[]
            {
                new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255), new Color32(255,255,255,255)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private float WallOriginalXYUnitToWorldScaleV8LikeOriginal()
        {
            if (_map == null)
                return C2WallObjectsV1SpriteWorldScaleLikeOriginal;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            return kernel.BackingStepXWorld / 32.0f;
        }

        private float WallOriginalZUnitToWorldScaleV8LikeOriginal()
        {
            if (_map == null)
                return 1.0f;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            return kernel.HeightScale;
        }

        private static Vector2 GetWallSpritePivotPxV8LikeOriginal(WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            if (desc != null && desc.AlignPoints.Count > 0)
                return new Vector2(desc.AlignPoints[0].x, desc.AlignPoints[0].y);

            return new Vector2(wPx * 0.5f, hPx * 0.5f);
        }

        private static Vector2[] GetWallSpriteQuadUvV8LikeOriginal()
        {
            // V173: WALLS.g16 frames are already decoded into Unity Texture2D orientation.
            // Original DrawWSprite/AddWorldPoint does not add a second manual V flip here.
            return new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
        }

        private static float EvaluateWallUAlignPlaneZV4LikeOriginal(WallSpriteDescV1LikeOriginal desc, float x, float y)
        {
            if (desc == null || desc.AlignPoints.Count < 3)
                return 0.0f;

            Vector3 a = desc.AlignPoints[0];
            Vector3 b = desc.AlignPoints[1];
            Vector3 c = desc.AlignPoints[2];

            float det = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            if (Mathf.Abs(det) < 0.0001f)
                return (a.z + b.z + c.z) / 3.0f;

            float u = ((x - a.x) * (c.y - a.y) - (c.x - a.x) * (y - a.y)) / det;
            float v = ((b.x - a.x) * (y - a.y) - (x - a.x) * (b.y - a.y)) / det;
            return a.z + u * (b.z - a.z) + v * (c.z - a.z);
        }

        private Vector3 WallOriginalXYToWorldV1LikeOriginal(float x, float y, float extraZ)
        {
            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            float gx = x / 32.0f;
            float gy = y / 32.0f;
            float rawX = gx * kernel.BackingStepXWorld;
            int ix = Mathf.FloorToInt(gx);
            float rawZ = gy * kernel.BackingStepZWorld + (((ix & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f);
            float worldX = rawX - kernel.CenterX;
            float worldZ = (rawZ - kernel.CenterZ) * WorldZSign;
            float worldY = SampleWallHeightOriginalXYV1LikeOriginal(x, y) * kernel.HeightScale + extraZ * kernel.HeightScale + C2WallObjectsV1YOffsetLikeOriginal;
            return new Vector3(worldX, worldY, worldZ);
        }

        private float SampleWallHeightOriginalXYV1LikeOriginal(float x, float y)
        {
            if (_map == null || _map.Heights == null || _map.Heights.Length == 0 || _map.VertInLine <= 1 || _map.MaxTH <= 1)
                return 0.0f;

            // Original MapSprites::GetHeight samples the Cossacks hex/triangle height cell,
            // not a Unity-style bilinear grid. WALLS CreateMatrix/AddExtraHeightObject depend on this.
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            int maxX = Mathf.Max(0, (_map.VertInLine - 2) * 32);
            int maxY = Mathf.Max(32, (_map.MaxTH - 2) * 32);
            if (ix < 0) ix = 0;
            if (iy < 32) iy = 32;
            if (ix > maxX) ix = maxX;
            if (iy > maxY) iy = maxY;

            int nx = ix >> 5;
            int vert1;
            int vert2;
            int vert3;
            int x0;
            int y0;

            if ((nx & 1) != 0)
            {
                int dd = ix & 31;
                int dy = dd >> 1;
                int oy = 15 - dy;
                int y1 = (iy + oy) >> 5;
                int dy1 = (iy + oy) & 31;
                y1 = Mathf.Clamp(y1, 0, _map.MaxTH - 2);

                if (dy1 > 32 - dd)
                {
                    vert2 = nx + y1 * _map.VertInLine + 1;
                    vert3 = vert2 + _map.VertInLine;
                    vert1 = vert3 - 1;
                    x0 = nx << 5;
                    y0 = (y1 << 5) + 16;
                    int h1 = ReadWallTHMapHeightV45LikeOriginal(vert1);
                    int h2 = ReadWallTHMapHeightV45LikeOriginal(vert2);
                    int h3 = ReadWallTHMapHeightV45LikeOriginal(vert3);
                    return h1 + (((ix - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((iy - y0) * (h3 - h2)) >> 5);
                }
                else
                {
                    vert2 = nx + y1 * _map.VertInLine;
                    vert3 = vert2 + _map.VertInLine;
                    vert1 = vert2 + 1;
                    x0 = (nx << 5) + 32;
                    y0 = y1 << 5;
                    int h1 = ReadWallTHMapHeightV45LikeOriginal(vert1);
                    int h2 = ReadWallTHMapHeightV45LikeOriginal(vert2);
                    int h3 = ReadWallTHMapHeightV45LikeOriginal(vert3);
                    return h1 - (((ix - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((iy - y0) * (h3 - h2)) >> 5);
                }
            }
            else
            {
                int dd = ix & 31;
                int dy = dd >> 1;
                int y1 = (iy + dy) >> 5;
                int dy1 = (iy + dy) & 31;
                y1 = Mathf.Clamp(y1, 0, _map.MaxTH - 2);

                if (dy1 < dd)
                {
                    vert1 = nx + y1 * _map.VertInLine;
                    vert2 = vert1 + 1;
                    vert3 = vert2 + _map.VertInLine;
                    x0 = nx << 5;
                    y0 = y1 << 5;
                    int h1 = ReadWallTHMapHeightV45LikeOriginal(vert1);
                    int h2 = ReadWallTHMapHeightV45LikeOriginal(vert2);
                    int h3 = ReadWallTHMapHeightV45LikeOriginal(vert3);
                    return h1 + (((ix - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((iy - y0) * (h3 - h2)) >> 5);
                }
                else
                {
                    vert2 = nx + y1 * _map.VertInLine;
                    vert3 = vert2 + _map.VertInLine;
                    vert1 = vert3 + 1;
                    x0 = (nx << 5) + 32;
                    y0 = (y1 << 5) + 16;
                    int h1 = ReadWallTHMapHeightV45LikeOriginal(vert1);
                    int h2 = ReadWallTHMapHeightV45LikeOriginal(vert2);
                    int h3 = ReadWallTHMapHeightV45LikeOriginal(vert3);
                    return h1 - (((ix - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((iy - y0) * (h3 - h2)) >> 5);
                }
            }
        }

        private int ReadWallTHMapHeightV45LikeOriginal(int vertexIndex)
        {
            if (_map == null || _map.Heights == null || _map.Heights.Length == 0)
                return 0;

            int safeIndex = Mathf.Clamp(vertexIndex, 0, _map.Heights.Length - 1);
            return Mathf.RoundToInt(_map.Heights[safeIndex]);
        }

        private void AccumulateWallC2MImmHeightLockLayerV25LikeOriginal(
            WallIMMHeightLockLayerV25LikeOriginal layer,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallC2MParsedMeshV23LikeOriginal c2m,
            string loadAudit,
            int order)
        {
            if (!C2WallObjectsV25ApplyIMMHeightLockLayerLikeOriginal || layer == null)
                return;
            if (s == null || desc == null || c2m == null)
            {
                if (layer.Audit.Count < C2WallObjectsV25ImmLayerAuditLimitLikeOriginal)
                    layer.Audit.Add("order=" + order.ToString(CultureInfo.InvariantCulture) + " missing_c2m load=" + (loadAudit ?? string.Empty));
                return;
            }

            if (c2m.Navimesh != null)
                ScanWallC2MGeomIntoIMMLayerV25LikeOriginal(layer, s, desc, c2m.Navimesh, true, order);
            else
                layer.MissingNavimesh++;

            if (c2m.Lockmesh != null)
                ScanWallC2MGeomIntoIMMLayerV25LikeOriginal(layer, s, desc, c2m.Lockmesh, false, order);
            else
                layer.MissingLockmesh++;
        }

        private void ScanWallC2MGeomIntoIMMLayerV25LikeOriginal(
            WallIMMHeightLockLayerV25LikeOriginal layer,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallC2MParsedMeshV23LikeOriginal geom,
            bool heightLayer,
            int order)
        {
            if (layer == null || s == null || desc == null || geom == null || geom.Vertices == null || geom.Vertices.Length == 0)
                return;

            int beforeCells = heightLayer ? layer.HeightByCell.Count : layer.LockedCells.Count;
            int beforeSamples = heightLayer ? layer.HeightSamples : layer.LockSamples;
            int step = Mathf.Max(1, C2WallObjectsV25ScanCellSizeOriginalPixelsLikeOriginal);

            for (int i = 0; i < geom.Vertices.Length; i++)
            {
                Vector3 local = geom.Vertices[i];
                Vector3 original = s.HasMatrix
                    ? TransformOriginalMatrix4DPointV19LikeOriginal(s.Matrix, local)
                    : new Vector3(s.X + local.x, s.Y + local.y, SampleWallHeightOriginalXYV1LikeOriginal(s.X + local.x, s.Y + local.y) + local.z);

                int cx = Mathf.FloorToInt(original.x / step);
                int cy = Mathf.FloorToInt(original.y / step);
                long key = PackWallIMMCellKeyV25LikeOriginal(cx, cy);

                if (heightLayer)
                {
                    float terrain = SampleWallHeightOriginalXYV1LikeOriginal(original.x, original.y);
                    float delta = original.z - terrain;
                    if (!layer.HeightByCell.TryGetValue(key, out float oldDelta) || delta > oldDelta)
                        layer.HeightByCell[key] = delta;
                    if (delta < layer.MinDelta) layer.MinDelta = delta;
                    if (delta > layer.MaxDelta) layer.MaxDelta = delta;
                    layer.HeightSamples++;
                }
                else
                {
                    layer.LockedCells.Add(key);
                    layer.LockSamples++;
                }
            }

            layer.HeightCells = layer.HeightByCell.Count;
            layer.LockCells = layer.LockedCells.Count;

            if (layer.Audit.Count < C2WallObjectsV25ImmLayerAuditLimitLikeOriginal)
            {
                int afterCells = heightLayer ? layer.HeightByCell.Count : layer.LockedCells.Count;
                int afterSamples = heightLayer ? layer.HeightSamples : layer.LockSamples;
                layer.Audit.Add("order=" + order.ToString(CultureInfo.InvariantCulture) +
                                " id=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                                " name=" + desc.Name +
                                " model=" + (string.IsNullOrWhiteSpace(desc.ModelPath) ? "-" : desc.ModelPath) +
                                " layer=" + (heightLayer ? "NavimeshHeight" : "LockmeshLock") +
                                " node=" + geom.NodeName +
                                " samples+=" + (afterSamples - beforeSamples).ToString(CultureInfo.InvariantCulture) +
                                " cells+=" + (afterCells - beforeCells).ToString(CultureInfo.InvariantCulture));
            }
        }

        private static long PackWallIMMCellKeyV25LikeOriginal(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }

        private string BuildWallC2MImmLayerSummaryV25LikeOriginal(WallIMMHeightLockLayerV25LikeOriginal layer)
        {
            if (layer == null)
                return "[C2:WALL IMM LAYER V26] missing";
            string delta = (layer.HeightSamples > 0 && !float.IsInfinity(layer.MinDelta) && !float.IsInfinity(layer.MaxDelta))
                ? layer.MinDelta.ToString("0.###", CultureInfo.InvariantCulture) + ".." + layer.MaxDelta.ToString("0.###", CultureInfo.InvariantCulture)
                : "none";
            string audit = layer.Audit.Count > 0 ? string.Join(" | ", layer.Audit.ToArray()) : "none";
            return "[C2:WALL IMM LAYER V26] contract=" + C2WallObjectsV25IMMContractLikeOriginal +
                   " heightSamples=" + layer.HeightSamples.ToString(CultureInfo.InvariantCulture) +
                   " heightCells=" + layer.HeightCells.ToString(CultureInfo.InvariantCulture) +
                   " lockSamples=" + layer.LockSamples.ToString(CultureInfo.InvariantCulture) +
                   " lockCells=" + layer.LockCells.ToString(CultureInfo.InvariantCulture) +
                   " missingNav=" + layer.MissingNavimesh.ToString(CultureInfo.InvariantCulture) +
                   " missingLock=" + layer.MissingLockmesh.ToString(CultureInfo.InvariantCulture) +
                   " delta=" + delta +
                   " audit=" + audit;
        }

        private void AddWallC2MLockMeshColliderV25LikeOriginal(GameObject go, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal lockMesh, WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            if (go == null || s == null || desc == null || lockMesh == null || lockMesh.Vertices == null || lockMesh.Vertices.Length == 0 || lockMesh.Triangles == null || lockMesh.Triangles.Length < 3)
                return;

            try
            {
                Vector3[] verts = new Vector3[lockMesh.Vertices.Length];
                for (int i = 0; i < lockMesh.Vertices.Length; i++)
                {
                    Vector3 local = lockMesh.Vertices[i];
                    Vector3 original = s.HasMatrix && route != null && route.UseSavedM4
                        ? TransformOriginalMatrix4DPointV19LikeOriginal(s.Matrix, local)
                        : new Vector3(s.X + local.x, s.Y + local.y, SampleWallHeightOriginalXYV1LikeOriginal(s.X + local.x, s.Y + local.y) + local.z);
                    verts[i] = OriginalWallXYZToWorldV6LikeOriginal(original.x, original.y, original.z + desc.FixHeight + C2WallObjectsV25LockColliderYOffsetPixelsLikeOriginal);
                }

                var colliderMesh = new Mesh { name = "C2_WallLockmeshCollider_V25_" + desc.Name };
                if (verts.Length > 65000)
                    colliderMesh.indexFormat = IndexFormat.UInt32;
                colliderMesh.vertices = verts;
                colliderMesh.triangles = lockMesh.Triangles;
                colliderMesh.RecalculateBounds();

                MeshCollider col = go.AddComponent<MeshCollider>();
                col.sharedMesh = colliderMesh;
                col.convex = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:WALL IMM LAYER V26] failed to create lock collider for " + desc.Name + ": " + ex.Message);
            }
        }

        private string BuildWallC2MImmScanAuditV24LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal c2m)
        {
            if (!C2WallObjectsV24AuditIMMHeightLockScanLikeOriginal)
                return "imm_scan=disabled";
            if (s == null || desc == null || c2m == null)
                return "imm_scan=missing_input";

            string nav = BuildWallC2MGeomScanPartV24LikeOriginal("Navimesh", s, desc, c2m.Navimesh);
            string lockMesh = BuildWallC2MGeomScanPartV24LikeOriginal("Lockmesh", s, desc, c2m.Lockmesh);
            string visual = c2m.HasLocalBounds
                ? "carcassBounds=(" + FormatVector3V24LikeOriginal(c2m.LocalBoundsMin) + ")->(" + FormatVector3V24LikeOriginal(c2m.LocalBoundsMax) + ")"
                : "carcassBounds=none";
            return C2WallObjectsV24IMMContractLikeOriginal + " " + visual + " " + nav + " " + lockMesh;
        }

        private string BuildWallC2MGeomScanPartV24LikeOriginal(string label, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal geom)
        {
            if (geom == null || geom.Vertices == null || geom.Vertices.Length == 0)
                return label + "=missing";

            int samples = Mathf.Min(geom.Vertices.Length, 32);
            float minDelta = float.PositiveInfinity;
            float maxDelta = float.NegativeInfinity;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            int step = Mathf.Max(1, geom.Vertices.Length / samples);
            int used = 0;
            for (int i = 0; i < geom.Vertices.Length && used < samples; i += step)
            {
                Vector3 local = geom.Vertices[i];
                Vector3 original = s.HasMatrix
                    ? TransformOriginalMatrix4DPointV19LikeOriginal(s.Matrix, local)
                    : new Vector3(s.X + local.x, s.Y + local.y, SampleWallHeightOriginalXYV1LikeOriginal(s.X + local.x, s.Y + local.y) + local.z);

                float terrain = SampleWallHeightOriginalXYV1LikeOriginal(original.x, original.y);
                float delta = original.z - terrain;
                if (delta < minDelta) minDelta = delta;
                if (delta > maxDelta) maxDelta = delta;
                if (original.x < minX) minX = original.x;
                if (original.x > maxX) maxX = original.x;
                if (original.y < minY) minY = original.y;
                if (original.y > maxY) maxY = original.y;
                used++;
            }

            if (used == 0)
                return label + "=empty";

            return label +
                   "(v=" + geom.Vertices.Length.ToString(CultureInfo.InvariantCulture) +
                   ",i=" + (geom.Triangles != null ? geom.Triangles.Length : 0).ToString(CultureInfo.InvariantCulture) +
                   ",scan=" + used.ToString(CultureInfo.InvariantCulture) +
                   ",xy=(" + minX.ToString("0.#", CultureInfo.InvariantCulture) + "," + minY.ToString("0.#", CultureInfo.InvariantCulture) +
                   ")->(" + maxX.ToString("0.#", CultureInfo.InvariantCulture) + "," + maxY.ToString("0.#", CultureInfo.InvariantCulture) + ")" +
                   ",zMinusTerrain=" + minDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                   ".." + maxDelta.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }

        private static string FormatVector3V24LikeOriginal(Vector3 v)
        {
            return v.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   v.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   v.z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private string BuildWallC2MRenderMaterialAuditLineV26LikeOriginal(int order, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            if (desc == null)
                return "order=" + order.ToString(CultureInfo.InvariantCulture) + " missing_desc";

            WallC2MParsedMeshV23LikeOriginal c2m = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out string audit);
            string colorStats = c2m != null ? BuildWallC2MVertexColorStatsV26LikeOriginal(c2m) : "c2m_not_loaded audit=" + audit;
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                   " name=" + desc.Name +
                   " model=" + (string.IsNullOrWhiteSpace(desc.ModelPath) ? "-" : desc.ModelPath) +
                   " shader=Cossacks2Bridge/WallC2MVertexColorV26" +
                   " queue=" + C2WallObjectsV24ModelRenderQueueLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                   " zWrite=On zTest=LEqual cull=Off offset=-1,-1" +
                   " contract=" + C2WallObjectsV26RenderContractLikeOriginal +
                   " " + colorStats;
        }

        private static string BuildWallC2MVertexColorStatsV26LikeOriginal(WallC2MParsedMeshV23LikeOriginal c2m)
        {
            if (c2m == null || c2m.Colors == null || c2m.Colors.Length == 0)
                return "vertexColors=missing";

            int minR = 255, minG = 255, minB = 255, minA = 255;
            int maxR = 0, maxG = 0, maxB = 0, maxA = 0;
            long sumR = 0, sumG = 0, sumB = 0, sumA = 0;
            for (int i = 0; i < c2m.Colors.Length; i++)
            {
                Color32 c = c2m.Colors[i];
                minR = Mathf.Min(minR, c.r); minG = Mathf.Min(minG, c.g); minB = Mathf.Min(minB, c.b); minA = Mathf.Min(minA, c.a);
                maxR = Mathf.Max(maxR, c.r); maxG = Mathf.Max(maxG, c.g); maxB = Mathf.Max(maxB, c.b); maxA = Mathf.Max(maxA, c.a);
                sumR += c.r; sumG += c.g; sumB += c.b; sumA += c.a;
            }
            float inv = 1.0f / Mathf.Max(1, c2m.Colors.Length);
            return "vertexColors=count=" + c2m.Colors.Length.ToString(CultureInfo.InvariantCulture) +
                   " min=(" + minR.ToString(CultureInfo.InvariantCulture) + "," + minG.ToString(CultureInfo.InvariantCulture) + "," + minB.ToString(CultureInfo.InvariantCulture) + "," + minA.ToString(CultureInfo.InvariantCulture) + ")" +
                   " max=(" + maxR.ToString(CultureInfo.InvariantCulture) + "," + maxG.ToString(CultureInfo.InvariantCulture) + "," + maxB.ToString(CultureInfo.InvariantCulture) + "," + maxA.ToString(CultureInfo.InvariantCulture) + ")" +
                   " avg=(" + (sumR * inv).ToString("0.#", CultureInfo.InvariantCulture) + "," + (sumG * inv).ToString("0.#", CultureInfo.InvariantCulture) + "," + (sumB * inv).ToString("0.#", CultureInfo.InvariantCulture) + "," + (sumA * inv).ToString("0.#", CultureInfo.InvariantCulture) + ")";
        }

        private static bool IsWallDambaC2MModelV33LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;

            int id = desc.SpriteIndex;
            if (id >= 60 && id <= 67)
                return true;

            string model = desc.ModelPath ?? string.Empty;
            return model.IndexOf("dam", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   model.IndexOf("cmost", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildWallDambaChainAuditLineV33LikeOriginal(
            int order,
            WallSpriteDescV1LikeOriginal desc,
            Texture2D tex,
            string textureSource,
            WallC2MParsedMeshV23LikeOriginal c2m,
            string loadAudit,
            WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            if (desc == null)
                return "order=" + order.ToString(CultureInfo.InvariantCulture) + " desc=null";

            string section = (desc.SpriteIndex >= 60 && desc.SpriteIndex <= 65) ? "#DAMBA" :
                             (desc.SpriteIndex == 66 || desc.SpriteIndex == 67) ? "#OLDMOST" : "#MODEL";
            bool texReal = tex != null && tex != Texture2D.whiteTexture;
            string texInfo = texReal
                ? tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture)
                : "white_or_missing";

            int v = c2m != null && c2m.Vertices != null ? c2m.Vertices.Length : 0;
            int tri = c2m != null && c2m.Triangles != null ? c2m.Triangles.Length / 3 : 0;
            int uv = c2m != null && c2m.UV != null ? c2m.UV.Length : 0;
            int col = c2m != null && c2m.Colors != null ? c2m.Colors.Length : 0;
            bool allWhite = c2m != null && AreWallC2MColorsAllWhiteV33LikeOriginal(c2m.Colors);
            string nav = c2m != null && c2m.Navimesh != null ? "nav=yes" : "nav=no";
            string lockm = c2m != null && c2m.Lockmesh != null ? "lock=yes" : "lock=no";

            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                   " name=" + desc.Name +
                   " section=" + section +
                   " model='" + (desc.ModelPath ?? string.Empty) + "'" +
                   " route=" + (route != null ? route.Route.ToString() : "-") +
                   " path='" + (route != null ? route.Path : string.Empty) + "'" +
                   " texture=WALLS.g16#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                   " textureReal=" + texReal +
                   " texSize=" + texInfo +
                   " uv=" + uv.ToString(CultureInfo.InvariantCulture) +
                   " verts=" + v.ToString(CultureInfo.InvariantCulture) +
                   " tris=" + tri.ToString(CultureInfo.InvariantCulture) +
                   " colors=" + col.ToString(CultureInfo.InvariantCulture) +
                   " colorsAllWhite=" + allWhite +
                   " gpobj=" + FormatWallC2MGPObjBriefV40LikeOriginal(c2m != null ? c2m.GPObj : null) +
                   " alphaMode=" + (IsWallDambaC2MModelV33LikeOriginal(desc) && !C2WallObjectsV33UseWallSpriteTextureForDambaC2MLikeOriginal ? "C2M_base_no_WALLS_fullUV" : (C2WallObjectsV36MakeDambaTextureOpaqueLikeOriginal ? "opaqueTexture_fullC2M" : (C2WallObjectsV35UseSeparateDambaSideOverlayLikeOriginal ? "baseOpaque_plus_sideOverlay" : (C2WallObjectsV34UseSolidRgbForDambaC2MLikeOriginal ? "solidRGB_no_sprite_alpha" : "spriteAlphaCutout")))) +
                   " zTest=" + (C2WallObjectsV34ForceDambaVisibleOverTerrainUntilExtraHeightPipelineLikeOriginal ? "Always_until_extraheight" : "LEqual") +
                   " " + nav + " " + lockm +
                   " source='" + (textureSource ?? string.Empty) + "'" +
                   " loadAudit='" + (loadAudit ?? string.Empty) + "'";
        }

        private static bool AreWallC2MColorsAllWhiteV33LikeOriginal(Color32[] colors)
        {
            if (colors == null || colors.Length == 0)
                return false;
            for (int i = 0; i < colors.Length; i++)
            {
                Color32 c = colors[i];
                if (c.r != 255 || c.g != 255 || c.b != 255)
                    return false;
            }
            return true;
        }

        private static Texture2D MakeDambaTextureOpaqueV36LikeOriginal(Texture2D src, WallSpriteDescV1LikeOriginal desc, out string audit)
        {
            audit = "opaque=noop";
            if (src == null || ReferenceEquals(src, Texture2D.whiteTexture))
                return src;

            try
            {
                Color32[] px = src.GetPixels32();
                if (px == null || px.Length == 0)
                    return src;

                long sr = 0, sg = 0, sb = 0;
                int visible = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 c = px[i];
                    if (c.a > 8)
                    {
                        sr += c.r;
                        sg += c.g;
                        sb += c.b;
                        visible++;
                    }
                }

                byte ar = visible > 0 ? (byte)Mathf.Clamp(Mathf.RoundToInt(sr / (float)visible), 0, 255) : (byte)128;
                byte ag = visible > 0 ? (byte)Mathf.Clamp(Mathf.RoundToInt(sg / (float)visible), 0, 255) : (byte)128;
                byte ab = visible > 0 ? (byte)Mathf.Clamp(Mathf.RoundToInt(sb / (float)visible), 0, 255) : (byte)128;
                Color32 fill = new Color32(ar, ag, ab, 255);

                int transparent = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    if (px[i].a <= 8)
                    {
                        px[i] = fill;
                        transparent++;
                    }
                    else
                    {
                        Color32 c = px[i];
                        c.a = 255;
                        px[i] = c;
                    }
                }

                var outTex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, false)
                {
                    name = "C2_DAMBA_V36_Opaque_" + (desc != null ? desc.Name : src.name),
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                outTex.SetPixels32(px);
                outTex.Apply(false, false);
                audit = "opaque=yes visible=" + visible.ToString(CultureInfo.InvariantCulture) +
                        " transparentFilled=" + transparent.ToString(CultureInfo.InvariantCulture) +
                        " fill=(" + ar.ToString(CultureInfo.InvariantCulture) + "," +
                                   ag.ToString(CultureInfo.InvariantCulture) + "," +
                                   ab.ToString(CultureInfo.InvariantCulture) + ")";
                return outTex;
            }
            catch (Exception ex)
            {
                audit = "opaque=failed " + ex.GetType().Name + ":" + ex.Message;
                return src;
            }
        }










private static bool ShouldClampSavedM4Prop2DToTerrainV124LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            if (!C2WallObjectsV124ClampSavedM4PropsToTerrainLikeOriginal)
                return false;
            if (s == null || desc == null || route == null)
                return false;
            if (route.Route != WallDrawRouteV20LikeOriginal.SavedAlignedSprite)
                return false;
            if (!route.UseSavedM4 || !s.HasMatrix)
                return false;

            return route.ClassV118 == WallWL2DClassV118LikeOriginal.VerticalAligned ||
                   route.ClassV118 == WallWL2DClassV118LikeOriginal.GroundAligned ||
                   route.ClassV118 == WallWL2DClassV118LikeOriginal.Single2DProp;
        }

        private Mesh ClampWall2DMeshMinVertexToTerrainV124LikeOriginal(
            Mesh source,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            out string audit)
        {
            audit = "V124_propClamp=skip";
            if (source == null || s == null || desc == null)
                return source;

            Vector3[] verts = source.vertices;
            if (verts == null || verts.Length == 0)
                return source;

            float minY = float.PositiveInfinity;
            int minIndex = -1;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].y < minY)
                {
                    minY = verts[i].y;
                    minIndex = i;
                }
            }

            if (minIndex < 0 || float.IsInfinity(minY))
                return source;

            Vector2 contactOriginal;
            bool contactFromVertex = TryWorldXZToOriginalXYV118LikeOriginal(verts[minIndex], out contactOriginal);
            if (!contactFromVertex)
                contactOriginal = new Vector2(s.X, s.Y);

            float targetY = WallOriginalXYToWorldV1LikeOriginal(contactOriginal.x, contactOriginal.y, desc.FixHeight).y;
            float delta = targetY - minY;
            if (!IsFiniteWallFloatV21LikeOriginal(delta))
                return source;

            if (Mathf.Abs(delta) <= 0.00001f)
            {
                audit = "V124_propClamp=alreadyGrounded minIndex=" + minIndex.ToString(CultureInfo.InvariantCulture) +
                        " contact=" + (contactFromVertex ? "minVertex" : "savedXY") +
                        " contactXY=(" + FormatWallFloatV118LikeOriginal(contactOriginal.x) + "," + FormatWallFloatV118LikeOriginal(contactOriginal.y) + ")";
                return source;
            }

            for (int i = 0; i < verts.Length; i++)
                verts[i].y += delta;

            Mesh mesh = new Mesh { name = source.name + "_V124_prop_min_vertex_terrain_contact" };
            mesh.vertices = verts;
            mesh.uv = source.uv;
            mesh.colors32 = source.colors32;
            mesh.triangles = source.triangles;
            mesh.RecalculateBounds();

            audit = "V124_propClamp=minVertexTerrain" +
                    " minIndex=" + minIndex.ToString(CultureInfo.InvariantCulture) +
                    " contact=" + (contactFromVertex ? "minVertex" : "savedXY") +
                    " contactXY=(" + FormatWallFloatV118LikeOriginal(contactOriginal.x) + "," + FormatWallFloatV118LikeOriginal(contactOriginal.y) + ")" +
                    " deltaY=" + FormatWallFloatV118LikeOriginal(delta);
            return mesh;
        }






private Mesh OffsetWallMeshWorldYV35LikeOriginal(Mesh source, float pixels)
        {
            if (source == null || Mathf.Abs(pixels) <= 0.001f)
                return source;

            Vector3[] verts = source.vertices;
            if (verts == null || verts.Length == 0)
                return source;

            float dy = pixels * WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            Vector3[] shifted = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                shifted[i] = verts[i];
                shifted[i].y += dy;
            }

            Mesh mesh = new Mesh { name = source.name + "_V35_raise" };
            mesh.vertices = shifted;
            mesh.uv = source.uv;
            mesh.colors32 = source.colors32;
            mesh.triangles = source.triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private bool TryAttachDambaSideOverlayV35LikeOriginal(GameObject owner, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallSavedWLRouteDecisionV20LikeOriginal route, Material fallbackBase, out string audit)
        {
            audit = string.Empty;
            if (owner == null || s == null || desc == null || route == null)
            {
                audit = "bad_args";
                return false;
            }
            if (!IsWallDambaC2MModelV33LikeOriginal(desc))
            {
                audit = "not_damba";
                return false;
            }

            Texture2D tex = TryLoadWallSpriteTextureV1LikeOriginal(desc, out string source);
            if (tex == null)
            {
                audit = "overlay_missing_texture after " + (source ?? string.Empty);
                return false;
            }

            Mesh overlayMesh = BuildDambaSideOverlayMeshV35LikeOriginal(s, desc, tex, route);
            if (overlayMesh == null)
            {
                audit = "overlay_mesh_null source='" + (source ?? string.Empty) + "'";
                return false;
            }

            GameObject child = new GameObject("DambaSideOverlayV35_" + desc.Name);
            child.transform.SetParent(owner.transform, false);
            MeshFilter mf = child.AddComponent<MeshFilter>();
            MeshRenderer mr = child.AddComponent<MeshRenderer>();
            ApplyWallRendererShadowContractV44LikeOriginal(mr);
            mf.sharedMesh = overlayMesh;
            mr.sharedMaterial = CreateWallSpriteMaterialV29LikeOriginal(tex, desc, null, fallbackBase);
            mr.sortingOrder = Mathf.Clamp(s.Y, -32768, 32767);
            audit = "overlay=attached id=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                    " name=" + desc.Name +
                    " source='" + (source ?? string.Empty) +
                    "' contract='" + C2WallObjectsV35DambaRenderContractLikeOriginal + "'";
            return true;
        }

        private Mesh BuildDambaSideOverlayMeshV35LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, Texture2D tex, WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            if (s == null || desc == null)
                return null;

            float wPx = tex != null && tex != Texture2D.whiteTexture ? Mathf.Max(8.0f, tex.width) : Mathf.Max(8.0f, desc.Width * 2.0f);
            float hPx = tex != null && tex != Texture2D.whiteTexture ? Mathf.Max(8.0f, tex.height) : Mathf.Max(8.0f, desc.Height * 2.0f);

            Mesh mesh = null;
            if (route.UseSavedM4 && s.HasMatrix)
                mesh = BuildSavedMapWallSpriteSavedM4MeshV21LikeOriginal(s, desc, wPx, hPx, route.FlipLocalY, "DambaSideOverlayV35_SavedM4");

            if (mesh == null)
            {
                mesh = BuildSavedMapWallSpriteVerticalAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);
                if (mesh == null)
                    mesh = BuildSavedMapWallSpriteAlignedNoEmbedV20LikeOriginal(s, desc, wPx, hPx);
            }

            return ApplyWallSavedSpriteProfileEmbedV19LikeOriginal(mesh, desc, WallSavedWLProfileV18LikeOriginal.VerticalAligned);
        }

        private static Color GetDambaC2MBaseTintV37LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (C2WallObjectsV37UseTemporaryStoneTintForDambaUntilC2MMaterialsLikeOriginal &&
                IsWallDambaC2MModelV33LikeOriginal(desc))
            {
                return new Color(0.62f, 0.58f, 0.50f, 1.0f);
            }

            return Color.white;
        }

        private static Color GetDambaC2MBaseTintV39LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            // V50: real DrawWChunk GPObj texture/UV path is active, so the temporary stone tint must not color the decoded DAMBA frame.
            return Color.white;
        }

        private Material CreateWallC2MModelMaterialV26LikeOriginal(Texture2D tex, WallSpriteDescV1LikeOriginal desc)
        {
            bool damba = IsWallDambaC2MModelV33LikeOriginal(desc);
            Shader shader = null;

            // V56: DAMBA/CMOST diagnostic chain already has correct rigid geometry.
            // Do not use the old vertex-color/white-fill material for it.  Use a simple
            // textured alpha-cut material, matching TemnyLess viewer logic:
            // texture = GPObj/G16 frame, UV = baked DrawWChunk square rects, alpha = cutout.
            if (damba && !C2WallObjectsV34UseSolidRgbForDambaC2MLikeOriginal)
                shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Transparent");

            if (shader == null && damba && C2WallObjectsV34UseSolidRgbForDambaC2MLikeOriginal)
                shader = Shader.Find("Cossacks2Bridge/WallC2MDambaSolidRGBV34");
            if (shader == null && damba && C2WallObjectsV33UseDedicatedDambaC2MShaderLikeOriginal)
                shader = Shader.Find("Cossacks2Bridge/WallC2MDambaTexturedV33");
            if (shader == null && C2WallObjectsV26UseC2MVertexColorMaterialLikeOriginal)
                shader = Shader.Find("Cossacks2Bridge/WallC2MVertexColorV26");
            shader = shader ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");

            Texture2D safeTex = tex != null ? tex : Texture2D.whiteTexture;
            var mat = new Material(shader)
            {
                name = (damba ? "C2_DAMBA_C2M_TemnyLessTextured_V56_" : "C2_WallC2MModelMat_V26_") + (desc != null ? desc.Name : "Model"),
                mainTexture = safeTex,
                renderQueue = C2WallObjectsV24ModelRenderQueueLikeOriginal
            };

            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", safeTex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", safeTex);

            Color tint = GetDambaC2MBaseTintV39LikeOriginal(desc);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);

            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", damba && C2WallObjectsV34ForceDambaVisibleOverTerrainUntilExtraHeightPipelineLikeOriginal ? (int)CompareFunction.Always : (int)CompareFunction.LessEqual);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);

            float cutoff = damba && C2WallObjectsV34UseSolidRgbForDambaC2MLikeOriginal ? 0.0f : (damba ? 0.015f : 0.01f);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", cutoff);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", damba ? 1.0f : 0.0f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0.0f); // Opaque + alpha clip, not transparent blend.

            if (damba)
            {
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                // Make sure decoded damba.g16 RGB is not multiplied by old C2M vertex colors.
                if (mat.HasProperty("_UseVertexColor"))
                    mat.SetFloat("_UseVertexColor", 0.0f);
            }
            return mat;
        }


        private sealed class WallSpriteRgbaStatsV29LikeOriginal
        {
            public int Width;
            public int Height;
            public int Total;
            public int Visible;
            public int WhiteVisible;
            public byte MinR = 255;
            public byte MinG = 255;
            public byte MinB = 255;
            public byte MinA = 255;
            public byte MaxR;
            public byte MaxG;
            public byte MaxB;
            public byte MaxA;
            public float AvgR;
            public float AvgG;
            public float AvgB;
            public float AvgA;
            public bool Readable;
            public bool Placeholder;
            public string Error;

            public float WhiteVisibleFraction => Visible > 0 ? WhiteVisible / (float)Visible : 0.0f;
        }

        private static bool IsWallSpriteRgbaAuditTargetV29LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;
            int id = desc.SpriteIndex;
            return id == 58 || id == 59 || id == 70 || id == 74 || id == 68 || id == 72 || id == 73 || id == 80 || id == 82 || id == 69;
        }

        private static WallSpriteRgbaStatsV29LikeOriginal AnalyzeWallSpriteRgbaV29LikeOriginal(Texture2D tex, WallSpriteDescV1LikeOriginal desc)
        {
            var st = new WallSpriteRgbaStatsV29LikeOriginal();
            if (tex == null)
            {
                st.Error = "null_texture";
                return st;
            }

            st.Width = tex.width;
            st.Height = tex.height;
            st.Total = Mathf.Max(0, tex.width * tex.height);
            st.Placeholder = ReferenceEquals(tex, Texture2D.whiteTexture);
            try
            {
                Color32[] px = tex.GetPixels32();
                st.Readable = true;
                long sumR = 0, sumG = 0, sumB = 0, sumA = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    Color32 c = px[i];
                    if (c.r < st.MinR) st.MinR = c.r;
                    if (c.g < st.MinG) st.MinG = c.g;
                    if (c.b < st.MinB) st.MinB = c.b;
                    if (c.a < st.MinA) st.MinA = c.a;
                    if (c.r > st.MaxR) st.MaxR = c.r;
                    if (c.g > st.MaxG) st.MaxG = c.g;
                    if (c.b > st.MaxB) st.MaxB = c.b;
                    if (c.a > st.MaxA) st.MaxA = c.a;
                    sumR += c.r;
                    sumG += c.g;
                    sumB += c.b;
                    sumA += c.a;
                    if (c.a > 8)
                    {
                        st.Visible++;
                        if (c.r >= C2WallObjectsV29WhiteMaskRgbMinLikeOriginal &&
                            c.g >= C2WallObjectsV29WhiteMaskRgbMinLikeOriginal &&
                            c.b >= C2WallObjectsV29WhiteMaskRgbMinLikeOriginal)
                            st.WhiteVisible++;
                    }
                }

                float inv = px.Length > 0 ? 1.0f / px.Length : 0.0f;
                st.AvgR = sumR * inv;
                st.AvgG = sumG * inv;
                st.AvgB = sumB * inv;
                st.AvgA = sumA * inv;
            }
            catch (Exception ex)
            {
                st.Readable = false;
                st.Error = ex.GetType().Name + ":" + ex.Message;
            }
            return st;
        }

        private static bool IsWallSpriteWhiteBridgeMaskV29LikeOriginal(WallSpriteDescV1LikeOriginal desc, WallSpriteRgbaStatsV29LikeOriginal st)
        {
            if (!C2WallObjectsV29RepairWhiteBridgeAlphaMaskLikeOriginal || desc == null || st == null || !st.Readable || st.Placeholder)
                return false;
            if (desc.SpriteIndex != 58 && desc.SpriteIndex != 59)
                return false;
            if (st.Visible <= 0)
                return false;
            return st.WhiteVisibleFraction >= C2WallObjectsV29WhiteMaskFractionLikeOriginal &&
                   st.AvgR >= C2WallObjectsV29WhiteMaskRgbMinLikeOriginal - 10.0f &&
                   st.AvgG >= C2WallObjectsV29WhiteMaskRgbMinLikeOriginal - 10.0f &&
                   st.AvgB >= C2WallObjectsV29WhiteMaskRgbMinLikeOriginal - 10.0f;
        }

        private static Color GetWallSpriteRepairColorV29LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc != null && (desc.SpriteIndex == 58 || desc.SpriteIndex == 59))
                return new Color(0.46f, 0.43f, 0.35f, 1.0f);
            return Color.white;
        }

        private static string BuildWallSpriteRgbaAuditLineV29LikeOriginal(int order, WallSpriteDescV1LikeOriginal desc, string source, WallSpriteRgbaStatsV29LikeOriginal st)
        {
            if (desc == null)
                return "order=" + order.ToString(CultureInfo.InvariantCulture) + " desc=null";
            if (st == null)
                return "order=" + order.ToString(CultureInfo.InvariantCulture) + " id=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " stats=null source='" + (source ?? string.Empty) + "'";

            bool repair = IsWallSpriteWhiteBridgeMaskV29LikeOriginal(desc, st);
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                   " name=" + desc.Name +
                   " size=" + st.Width.ToString(CultureInfo.InvariantCulture) + "x" + st.Height.ToString(CultureInfo.InvariantCulture) +
                   " readable=" + st.Readable +
                   " placeholder=" + st.Placeholder +
                   " visible=" + st.Visible.ToString(CultureInfo.InvariantCulture) +
                   " whiteVisible=" + st.WhiteVisible.ToString(CultureInfo.InvariantCulture) +
                   " whiteFrac=" + st.WhiteVisibleFraction.ToString("0.###", CultureInfo.InvariantCulture) +
                   " min=" + st.MinR.ToString(CultureInfo.InvariantCulture) + "," + st.MinG.ToString(CultureInfo.InvariantCulture) + "," + st.MinB.ToString(CultureInfo.InvariantCulture) + "," + st.MinA.ToString(CultureInfo.InvariantCulture) +
                   " max=" + st.MaxR.ToString(CultureInfo.InvariantCulture) + "," + st.MaxG.ToString(CultureInfo.InvariantCulture) + "," + st.MaxB.ToString(CultureInfo.InvariantCulture) + "," + st.MaxA.ToString(CultureInfo.InvariantCulture) +
                   " avg=" + st.AvgR.ToString("0.#", CultureInfo.InvariantCulture) + "," + st.AvgG.ToString("0.#", CultureInfo.InvariantCulture) + "," + st.AvgB.ToString("0.#", CultureInfo.InvariantCulture) + "," + st.AvgA.ToString("0.#", CultureInfo.InvariantCulture) +
                   " repairWhiteBridgeMask=" + repair +
                   (string.IsNullOrEmpty(st.Error) ? string.Empty : " err='" + st.Error + "'") +
                   " source='" + (source ?? string.Empty) + "'";
        }

        private Material CreateWallSpriteMaterialV29LikeOriginal(Texture2D tex, WallSpriteDescV1LikeOriginal desc, WallSpriteRgbaStatsV29LikeOriginal st, Material fallbackBase)
        {
            bool bridgeSprite = desc != null && (desc.SpriteIndex == 58 || desc.SpriteIndex == 59);
            Shader shader = null;
            if (C2WallObjectsV31UseExactBridgeSpriteCutoutLikeOriginal && bridgeSprite)
                shader = Shader.Find("Cossacks2Bridge/WallObjectSpriteV31ExactCutout");
            if (shader == null && C2WallObjectsV29UseDedicatedWallSpriteShaderLikeOriginal)
                shader = Shader.Find("Cossacks2Bridge/WallObjectSpriteV29");
            shader = shader ?? (fallbackBase != null ? fallbackBase.shader : null) ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");

            bool repairWhiteMask = !bridgeSprite && IsWallSpriteWhiteBridgeMaskV29LikeOriginal(desc, st);
            var inst = new Material(shader)
            {
                name = "C2_WallMapSpriteMat_V31_" + (desc != null ? desc.Name : "Wall"),
                mainTexture = tex != null ? tex : Texture2D.whiteTexture,
                renderQueue = C2WallObjectsV18RenderQueueLikeOriginal
            };
            if (inst.HasProperty("_MainTex")) inst.SetTexture("_MainTex", tex != null ? tex : Texture2D.whiteTexture);
            if (inst.HasProperty("_Color")) inst.SetColor("_Color", Color.white);
            if (inst.HasProperty("_RepairAlphaMask")) inst.SetFloat("_RepairAlphaMask", repairWhiteMask ? 1.0f : 0.0f);
            if (inst.HasProperty("_RepairColor")) inst.SetColor("_RepairColor", GetWallSpriteRepairColorV29LikeOriginal(desc));
            if (inst.HasProperty("_AlphaCutoff")) inst.SetFloat("_AlphaCutoff", C2WallObjectsV29AlphaCutoffLikeOriginal);
            if (inst.HasProperty("_ZWrite")) inst.SetInt("_ZWrite", 0);
            if (inst.HasProperty("_ZTest")) inst.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (inst.HasProperty("_Cull")) inst.SetInt("_Cull", (int)CullMode.Off);
            return inst;
        }

        private Material CreateWallObjectMaterialV1LikeOriginal()
        {
            Shader shader = Shader.Find("Cossacks2Bridge/WallObjectSpriteV31ExactCutout") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV29") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV7") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV6") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV5") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV4") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV3") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV2") ?? Shader.Find("Cossacks2Bridge/WallObjectSpriteV1") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader)
            {
                name = "C2_WallObjectSprite_V29_like_original",
                renderQueue = C2WallObjectsV18RenderQueueLikeOriginal
            };
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", C2WallObjectsV29AlphaCutoffLikeOriginal);
            if (mat.HasProperty("_RepairAlphaMask")) mat.SetFloat("_RepairAlphaMask", 0.0f);
            return mat;
        }

        private static void ApplyWallRendererShadowContractV44LikeOriginal(Renderer renderer)
        {
            if (renderer == null || !C2WallObjectsV44DisableUnityShadowCastingForWallObjectsLikeOriginal)
                return;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Vector4 ChooseWallC2MGPObjTextureUvTransformV43LikeOriginal(Texture2D tex, WallC2MParsedMeshV23LikeOriginal c2m, out string audit)
        {
            audit = string.Empty;
            if (tex == null || c2m == null || c2m.UV == null || c2m.UV.Length == 0)
            {
                audit = "no_tex_or_uv";
                return new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
            }

            string[] names = { "normal", "flipV", "flipU", "flipUV" };
            Vector4[] transforms =
            {
                new Vector4(1.0f, 1.0f, 0.0f, 0.0f),
                new Vector4(1.0f, -1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 0.0f),
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f)
            };

            int total = Mathf.Min(C2WallObjectsV43UvFitSampleLimitLikeOriginal, c2m.UV.Length);
            int step = Mathf.Max(1, c2m.UV.Length / Mathf.Max(1, total));
            int best = 0;
            int bestHits = -1;
            float bestAlpha = -1.0f;
            var parts = new List<string>(names.Length);

            for (int m = 0; m < transforms.Length; m++)
            {
                int hits = 0;
                int samples = 0;
                float alphaSum = 0.0f;
                Vector4 tr = transforms[m];

                try
                {
                    for (int i = 0; i < c2m.UV.Length && samples < total; i += step)
                    {
                        Vector2 uv = c2m.UV[i];
                        float u = uv.x * tr.x + tr.z;
                        float v = uv.y * tr.y + tr.w;
                        Color c = tex.GetPixelBilinear(u, v);
                        float a = c.a;
                        alphaSum += a;
                        if (a > 0.03f)
                            hits++;
                        samples++;
                    }
                }
                catch (Exception ex)
                {
                    parts.Add(names[m] + ":error=" + ex.GetType().Name);
                    continue;
                }

                float avg = samples > 0 ? alphaSum / samples : 0.0f;
                parts.Add(names[m] + ":hits=" + hits.ToString(CultureInfo.InvariantCulture) +
                          "/" + samples.ToString(CultureInfo.InvariantCulture) +
                          ",avgA=" + avg.ToString("0.000", CultureInfo.InvariantCulture));

                if (hits > bestHits || (hits == bestHits && avg > bestAlpha))
                {
                    best = m;
                    bestHits = hits;
                    bestAlpha = avg;
                }
            }

            audit = "chosen=" + names[best] +
                    " scale=(" + transforms[best].x.ToString(CultureInfo.InvariantCulture) +
                    "," + transforms[best].y.ToString(CultureInfo.InvariantCulture) + ")" +
                    " offset=(" + transforms[best].z.ToString(CultureInfo.InvariantCulture) +
                    "," + transforms[best].w.ToString(CultureInfo.InvariantCulture) + ")" +
                    " scores=[" + string.Join(";", parts.ToArray()) + "]";
            return transforms[best];
        }

        private static string BuildWallC2MGPObjMaterialAuditLineV42LikeOriginal(
            WallSpriteDescV1LikeOriginal desc,
            WallC2MParsedMeshV23LikeOriginal c2m,
            Texture2D tex,
            bool bound,
            string source)
        {
            WallC2MGPObjInfoV40LikeOriginal gp = c2m != null ? c2m.GPObj : null;
            return "id=" + (desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-") +
                   " name=" + (desc != null ? desc.Name : string.Empty) +
                   " model='" + (desc != null ? (desc.ModelPath ?? string.Empty) : string.Empty) + "'" +
                   " gpName='" + (gp != null ? (gp.GPName ?? string.Empty) : string.Empty) + "'" +
                   " frameIdx=" + (gp != null ? gp.FrameIdx.ToString(CultureInfo.InvariantCulture) : "-") +
                   " bound=" + bound +
                   " tex=" + (tex != null ? tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture) : "null") +
                   " source='" + (source ?? string.Empty) + "'";
        }


        private Texture2D TryLoadWallC2MTXRETextureV48LikeOriginal(WallC2MParsedMeshV23LikeOriginal c2m, out string source)
        {
            source = string.Empty;
            if (c2m == null || string.IsNullOrWhiteSpace(c2m.TextureName) || _bootstrap == null || _bootstrap.Fs == null)
                return null;

            string textureName = c2m.TextureName.Trim().Replace('/', '\\');
            var candidates = new List<string>();
            candidates.Add(textureName);
            candidates.Add(Path.GetFileName(textureName));
            candidates.Add("textures\\" + Path.GetFileName(textureName));
            candidates.Add("Textures\\" + Path.GetFileName(textureName));

            Texture2D tex = C2OriginalTextureService.TryLoadTextureByCandidates(
                _bootstrap.Fs,
                candidates.ToArray(),
                "C2M_TXRE_" + Path.GetFileNameWithoutExtension(textureName),
                C2OriginalTexturePolicy.WorldTextureLikeOriginal,
                out string resolved);

            if (tex != null)
                source = "TXRE='" + textureName + "' resolved='" + resolved + "'";
            else
                source = "TXRE_missing '" + textureName + "'";

            return tex;
        }

        private Texture2D TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(WallC2MParsedMeshV23LikeOriginal c2m, out string source, out List<WallG16SquareV47LikeOriginal> squares)
        {
            source = string.Empty;
            squares = null;
            if (c2m == null || c2m.GPObj == null)
            {
                source = "no_gpobj";
                return null;
            }

            string gpName = c2m.GPObj.GPName ?? string.Empty;
            int frameIdx = SelectWallC2MGPObjFrameIndexV46LikeOriginal(c2m, gpName, c2m.GPObj.FrameIdx, out string frameAudit);
            if (string.IsNullOrWhiteSpace(gpName))
            {
                source = "empty_gpName";
                return null;
            }

            List<string> candidates = BuildWallC2MGPObjG16CandidatePathsV42LikeOriginal(gpName);
            var checkedPaths = new List<string>(Mathf.Min(candidates.Count, 8));
            for (int i = 0; i < candidates.Count; i++)
            {
                string path = candidates[i];
                if (checkedPaths.Count < 8)
                    checkedPaths.Add(path);

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                Texture2D tex = TryLoadG16FrameViaMelinojaV42LikeOriginal(path, frameIdx, out string loadSource);
                if (tex != null)
                {
                    string squareAudit;
                    squares = TryParseWallG16FrameSquaresV47LikeOriginal(path, frameIdx, out squareAudit);
                    source = "gpName='" + gpName + "' frameIdx=" + frameIdx.ToString(CultureInfo.InvariantCulture) + " " + frameAudit + " " + loadSource + " squares='" + squareAudit + "'";
                    return tex;
                }

                source = "found_path_but_load_failed gpName='" + gpName + "' frameIdx=" + frameIdx.ToString(CultureInfo.InvariantCulture) + " " + frameAudit + " " + loadSource;
            }

            source = "missing_gp_g16 gpName='" + gpName + "' frameIdx=" + frameIdx.ToString(CultureInfo.InvariantCulture) +
                     " " + frameAudit + " checked=[" + string.Join(";", checkedPaths.ToArray()) + "]";
            return null;
        }

        private static int SelectWallC2MGPObjFrameIndexV46LikeOriginal(WallC2MParsedMeshV23LikeOriginal c2m, string gpName, int parsedFrameIdx, out string audit)
        {
            audit = "parsedFrameIdx=" + parsedFrameIdx.ToString(CultureInfo.InvariantCulture);
            if (!C2WallObjectsV46UseDrawWChunkDambaFrameLikeOriginal ||
                !string.Equals((gpName ?? string.Empty).Trim(), "damba", StringComparison.OrdinalIgnoreCase))
                return parsedFrameIdx;

            string model = c2m != null ? (c2m.ModelPath ?? string.Empty) : string.Empty;
            if (model.IndexOf("dam_bottom", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                audit += " drawWChunkFrame=0 model=dam_bottom";
                return 0;
            }

            if (model.IndexOf("dam_top", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                audit += " drawWChunkFrame=3 model=dam_top";
                return 3;
            }

            audit += " drawWChunkFrame=unforced";
            return parsedFrameIdx;
        }


        // V167 COMPILE RESTORE: C2M loader/helpers were physically cut in V165,
        // but DAMBA/model/calibrator code still depends on them. These are not old WALS2D fence routes.
        private WallC2MParsedMeshV23LikeOriginal TryLoadWallC2MVisualMeshV23LikeOriginal(string modelPath, out string audit)
        {
            audit = string.Empty;
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                audit = "empty_model_path";
                return null;
            }

            string requestKey = modelPath.Replace('/', '\\').Trim();
            string resolveAuditV161;
            string key = ResolveWallC2MModelPathV161LikeOriginal(requestKey, out resolveAuditV161);
            string cacheKey = string.IsNullOrWhiteSpace(key) ? requestKey : key;
            if (_c2WallObjectsV23C2MCacheLikeOriginal.TryGetValue(cacheKey, out WallC2MParsedMeshV23LikeOriginal cached))
            {
                audit = cached != null ? "cache_hit " + resolveAuditV161 + " " + cached.Audit : "cache_hit_null " + resolveAuditV161;
                return cached;
            }

            try
            {
                if (_bootstrap == null || _bootstrap.Fs == null)
                {
                    audit = "fs_not_ready";
                    _c2WallObjectsV23C2MCacheLikeOriginal[cacheKey] = null;
                    return null;
                }

                if (string.IsNullOrWhiteSpace(key) || !_bootstrap.Fs.Exists(key))
                {
                    audit = "missing_model_file " + key + " " + resolveAuditV161;
                    _c2WallObjectsV23C2MCacheLikeOriginal[cacheKey] = null;
                    return null;
                }

                byte[] data = _bootstrap.Fs.ReadAllBytes(key);
                WallC2MParsedMeshV23LikeOriginal mesh = ParseWallC2MVisualMeshV23LikeOriginal(key, data, out audit);
                audit = resolveAuditV161 + " " + audit;
                if (mesh != null) mesh.Audit = audit;
                _c2WallObjectsV23C2MCacheLikeOriginal[cacheKey] = mesh;
                return mesh;
            }
            catch (Exception ex)
            {
                audit = "exception " + ex.GetType().Name + ": " + ex.Message;
                _c2WallObjectsV23C2MCacheLikeOriginal[cacheKey] = null;
                return null;
            }
        }

        private static WallC2MParsedMeshV23LikeOriginal ParseWallC2MVisualMeshV23LikeOriginal(string modelPath, byte[] data, out string audit)
        {
            audit = string.Empty;
            if (data == null || data.Length < 16)
            {
                audit = "too_small";
                return null;
            }

            List<int> nodeOffsets = FindWallC2MNodeOffsetsV23LikeOriginal(data);
            if (nodeOffsets.Count == 0)
            {
                audit = "no_nodes";
                return null;
            }

            // V49: map bridges/dam objects must not use the generic V48 GEOM/TXRE merge path.
            // In map runtime they are authored as saved WL + original Matrix4D + GPObj chunked G16.
            // The V48 path is useful for standalone cannon/fregat/field models, but on #DAMBA/cmost/most
            // it can select a wrong render layer/frame and produce white/black slabs over terrain.
            if (IsWallC2MBridgeModelPathV49LikeOriginal(modelPath))
            {
                WallC2MParsedMeshV23LikeOriginal legacy = TryParseWallC2MLegacyCarcassGPCOV49LikeOriginal(modelPath, data, nodeOffsets, out string legacyAudit);
                if (legacy != null)
                {
                    if (C2WallObjectsV24ParseIMMGeomNodesLikeOriginal)
                    {
                        string immAudit = AttachWallC2MImmGeomNodesV24LikeOriginal(modelPath, data, nodeOffsets, legacy);
                        legacy.ImmAudit = immAudit;
                        legacy.Audit = legacy.Audit + " " + immAudit;
                    }

                    audit = legacy.Audit + " mode=V49_BRIDGE_SAFE_LEGACY_GPCO no_TXRE_merge no_MOVB_merge";
                    legacy.Audit = audit;
                    return legacy;
                }

                // If legacy parser fails, continue to V48 parser but keep the failure in the audit.
                audit = "V49 bridge legacy failed: " + legacyAudit;
            }

            List<string> textureRefs = ExtractWallC2MTextureRefsV48LikeOriginal(data, nodeOffsets);

            var transforms = new WallC2MTransformNodeV48LikeOriginal[nodeOffsets.Count];
            int transformCount = 0;
            for (int i = 0; i < nodeOffsets.Count; i++)
            {
                if (!TryReadWallC2MNodeHeaderV23LikeOriginal(data, nodeOffsets[i], i + 1 < nodeOffsets.Count ? nodeOffsets[i + 1] : data.Length, out WallC2MNodeHeaderV23LikeOriginal th))
                    continue;
                if (TryReadWallC2MMOVBTransformV48LikeOriginal(data, th, out WallC2MTransformNodeV48LikeOriginal tr))
                {
                    transforms[i] = tr;
                    transformCount++;
                }
            }

            var candidates = new List<WallC2MCandidateNodeV48LikeOriginal>();
            string meshAudit = string.Empty;
            for (int i = 0; i < nodeOffsets.Count; i++)
            {
                int pos = nodeOffsets[i];
                if (!TryReadWallC2MNodeHeaderV23LikeOriginal(data, pos, i + 1 < nodeOffsets.Count ? nodeOffsets[i + 1] : data.Length, out WallC2MNodeHeaderV23LikeOriginal h))
                    continue;

                if (!string.Equals(h.Tag, "GPCO", StringComparison.Ordinal) &&
                    !string.Equals(h.Tag, "GEOM", StringComparison.Ordinal))
                    continue;

                if (h.Name.IndexOf("Navimesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    h.Name.IndexOf("Lockmesh", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (!TryReadWallC2MMeshLayoutV48LikeOriginal(data, h, out WallC2MMeshLayoutV48LikeOriginal layout))
                    continue;

                int score = 0;
                if (string.Equals(h.Tag, "GPCO", StringComparison.Ordinal)) score += 100000;
                if (h.Name.IndexOf("Carcass", StringComparison.OrdinalIgnoreCase) >= 0) score += 50000;
                score += Mathf.Min(layout.VertexCount, 2000);

                candidates.Add(new WallC2MCandidateNodeV48LikeOriginal
                {
                    Header = h,
                    Layout = layout,
                    Ordinal = i,
                    Score = score
                });

                if (candidates.Count <= 16)
                {
                    meshAudit += " cand[" + h.Tag + ":" + h.Name +
                                 " vf=" + layout.VertexFormat.ToString(CultureInfo.InvariantCulture) +
                                 " nV=" + layout.VertexCount.ToString(CultureInfo.InvariantCulture) +
                                 " nI=" + layout.IndexCount.ToString(CultureInfo.InvariantCulture) +
                                 " uvOff=" + layout.UvOffset.ToString(CultureInfo.InvariantCulture) +
                                 " parent=" + h.ParentId.ToString(CultureInfo.InvariantCulture) + "]";
                }
            }

            if (candidates.Count == 0)
            {
                audit = "no_parseable_Carcass_GPCO_or_GEOM; nodes=" + nodeOffsets.Count.ToString(CultureInfo.InvariantCulture) +
                        " txre=" + (textureRefs.Count > 0 ? string.Join(",", textureRefs.ToArray()) : "none");
                return null;
            }

            var selected = new List<WallC2MCandidateNodeV48LikeOriginal>();
            WallC2MCandidateNodeV48LikeOriginal bestGPCO = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                WallC2MCandidateNodeV48LikeOriginal c = candidates[i];
                if (string.Equals(c.Header.Tag, "GPCO", StringComparison.Ordinal) &&
                    (bestGPCO == null || c.Score > bestGPCO.Score))
                    bestGPCO = c;
            }

            if (bestGPCO != null)
            {
                selected.Add(bestGPCO);
            }
            else
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (string.Equals(candidates[i].Header.Tag, "GEOM", StringComparison.Ordinal))
                        selected.Add(candidates[i]);
                }
            }

            if (selected.Count == 0)
            {
                audit = "no_selected_render_nodes";
                return null;
            }

            bool parseCarcassTail = selected.Count == 1 && string.Equals(selected[0].Header.Tag, "GPCO", StringComparison.Ordinal);
            string singleAudit;
            WallC2MParsedMeshV23LikeOriginal result = parseCarcassTail
                ? TryParseWallC2MNodeMeshV48LikeOriginal(modelPath, data, selected[0].Header, selected[0].Layout, transforms, false, true, out singleAudit)
                : MergeWallC2MGeomNodesV48LikeOriginal(modelPath, data, selected, transforms, out singleAudit);

            if (result == null)
            {
                audit = "selected_parse_failed; " + singleAudit + " " + meshAudit;
                return null;
            }

            if (string.IsNullOrWhiteSpace(result.TextureName) && textureRefs.Count > 0)
            {
                result.TextureName = textureRefs[0];
                result.TextureSource = "TXRE";
            }

            result.MeshMode = parseCarcassTail ? "GPCO_Carcass_DrawWChunk" : "Merged_GEOM_TXRE";

            if (C2WallObjectsV24ParseIMMGeomNodesLikeOriginal)
            {
                string immAudit = AttachWallC2MImmGeomNodesV24LikeOriginal(modelPath, data, nodeOffsets, result);
                result.ImmAudit = immAudit;
                result.Audit = result.Audit + " " + immAudit;
            }

            audit = result.Audit +
                    " mode=" + result.MeshMode +
                    " selectedNodes=" + selected.Count.ToString(CultureInfo.InvariantCulture) +
                    " transforms=" + transformCount.ToString(CultureInfo.InvariantCulture) +
                    " txre='" + (result.TextureName ?? string.Empty) + "'" +
                    " " + meshAudit;
            result.Audit = audit;
            return result;
        }

        private static bool IsWallC2MBridgeModelPathV49LikeOriginal(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                return false;

            string m = modelPath.Replace('\\', '/').ToLowerInvariant();
            return m.IndexOf("damba", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   m.IndexOf("dam_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   m.IndexOf("dam/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   m.IndexOf("#dam", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   m.IndexOf("cmost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   m.IndexOf("oldmost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   m.EndsWith("/most.c2m", StringComparison.OrdinalIgnoreCase) ||
                   m.EndsWith("/most1.c2m", StringComparison.OrdinalIgnoreCase);
        }

        private static WallC2MParsedMeshV23LikeOriginal TryParseWallC2MLegacyCarcassGPCOV49LikeOriginal(
            string modelPath,
            byte[] data,
            List<int> nodeOffsets,
            out string audit)
        {
            audit = string.Empty;
            if (data == null || nodeOffsets == null || nodeOffsets.Count == 0)
            {
                audit = "legacy_no_nodes";
                return null;
            }

            string bestAudit = string.Empty;
            for (int i = 0; i < nodeOffsets.Count; i++)
            {
                int pos = nodeOffsets[i];
                if (!TryReadWallC2MNodeHeaderV23LikeOriginal(data, pos, i + 1 < nodeOffsets.Count ? nodeOffsets[i + 1] : data.Length, out WallC2MNodeHeaderV23LikeOriginal h))
                    continue;

                if (!string.Equals(h.Tag, "GPCO", StringComparison.Ordinal))
                    continue;

                if (h.Name.IndexOf("Carcass", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                WallC2MParsedMeshV23LikeOriginal parsed = TryParseWallC2MLegacyGPCONodeMeshV49LikeOriginal(modelPath, data, h, out string localAudit);
                if (parsed != null)
                {
                    audit = localAudit;
                    return parsed;
                }

                if (string.IsNullOrEmpty(bestAudit))
                    bestAudit = localAudit;
            }

            audit = "legacy_no_parseable_Carcass_GPCO; " + bestAudit;
            return null;
        }

        private static WallC2MParsedMeshV23LikeOriginal TryParseWallC2MLegacyGPCONodeMeshV49LikeOriginal(
            string modelPath,
            byte[] data,
            WallC2MNodeHeaderV23LikeOriginal h,
            out string audit)
        {
            audit = string.Empty;
            int field = h.BodyOffset + 4;
            if (field + 16 >= data.Length)
            {
                audit = "legacy_short_gpco_header";
                return null;
            }

            int flags = ReadInt32LEWallV23LikeOriginal(data, field);
            int vertexCount = ReadInt32LEWallV23LikeOriginal(data, field + 4);
            int indexCount = ReadInt32LEWallV23LikeOriginal(data, field + 8);
            int triangleCount = ReadInt32LEWallV23LikeOriginal(data, field + 12);

            if (vertexCount <= 0 || vertexCount > 200000 || indexCount <= 0 || indexCount > 600000)
            {
                audit = "legacy_bad_counts v=" + vertexCount.ToString(CultureInfo.InvariantCulture) + " i=" + indexCount.ToString(CultureInfo.InvariantCulture);
                return null;
            }

            if (indexCount % 3 != 0)
                indexCount -= indexCount % 3;

            const int vertexStride = 32;
            int vertexStart = FindWallC2MVertexStartV23LikeOriginal(data, h, vertexCount, indexCount, vertexStride);
            if (vertexStart <= 0)
            {
                audit = "legacy_vertex_start_not_found v=" + vertexCount.ToString(CultureInfo.InvariantCulture) + " i=" + indexCount.ToString(CultureInfo.InvariantCulture);
                return null;
            }

            long indexStart64 = (long)vertexStart + (long)vertexCount * vertexStride;
            if (indexStart64 < 0 || indexStart64 + (long)indexCount * 2L > data.Length)
            {
                audit = "legacy_range_outside_file";
                return null;
            }

            int indexStart = (int)indexStart64;
            var vertices = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var colors = new Color32[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                int off = vertexStart + i * vertexStride;
                float x = ReadFloatLEWallV23LikeOriginal(data, off);
                float y = ReadFloatLEWallV23LikeOriginal(data, off + 4);
                float z = ReadFloatLEWallV23LikeOriginal(data, off + 8);
                uint color = ReadUInt32LEWallV23LikeOriginal(data, off + 12);
                float u = ReadFloatLEWallV23LikeOriginal(data, off + 16);
                float v = ReadFloatLEWallV23LikeOriginal(data, off + 20);

                vertices[i] = new Vector3(x, y, z);
                uv[i] = new Vector2(u, v);
                colors[i] = DecodeC2MColorV23LikeOriginal(color);
            }

            var triangles = new int[indexCount];
            for (int i = 0; i < indexCount; i++)
            {
                int idx = ReadUInt16LEWallV23LikeOriginal(data, indexStart + i * 2);
                if (idx < 0 || idx >= vertexCount)
                    idx = 0;
                triangles[i] = idx;
            }

            WallC2MGPObjInfoV40LikeOriginal gpObj = TryParseWallC2MGPObjChunkTableV40LikeOriginal(
                data,
                h,
                indexStart,
                indexCount,
                vertexCount,
                triangleCount);

            Vector3 bmin = vertices.Length > 0 ? vertices[0] : Vector3.zero;
            Vector3 bmax = bmin;
            for (int i = 1; i < vertices.Length; i++)
            {
                bmin = Vector3.Min(bmin, vertices[i]);
                bmax = Vector3.Max(bmax, vertices[i]);
            }

            var parsed = new WallC2MParsedMeshV23LikeOriginal
            {
                ModelPath = modelPath,
                NodeName = h.Name,
                Vertices = vertices,
                UV = uv,
                Colors = colors,
                Triangles = triangles,
                GPObj = gpObj,
                HasLocalBounds = vertices.Length > 0,
                LocalBoundsMin = bmin,
                LocalBoundsMax = bmax,
                MeshMode = "V49_BRIDGE_LEGACY_GPCO"
            };

            parsed.Audit = "legacy_real_C2M tag=" + h.Tag +
                           " node=" + h.Name +
                           " id=" + h.NodeId.ToString(CultureInfo.InvariantCulture) +
                           " parent=" + h.ParentId.ToString(CultureInfo.InvariantCulture) +
                           " flags=" + flags.ToString(CultureInfo.InvariantCulture) +
                           " verts=" + vertexCount.ToString(CultureInfo.InvariantCulture) +
                           " indices=" + indexCount.ToString(CultureInfo.InvariantCulture) +
                           " trisField=" + triangleCount.ToString(CultureInfo.InvariantCulture) +
                           " vertexStart=" + vertexStart.ToString(CultureInfo.InvariantCulture) +
                           " indexStart=" + indexStart.ToString(CultureInfo.InvariantCulture) +
                           " file=" + modelPath +
                           " gpobj=" + FormatWallC2MGPObjBriefV40LikeOriginal(gpObj);
            audit = parsed.Audit;
            return parsed;
        }

        private static string AttachWallC2MImmGeomNodesV24LikeOriginal(string modelPath, byte[] data, List<int> nodeOffsets, WallC2MParsedMeshV23LikeOriginal visual)
        {
            if (visual == null || data == null || nodeOffsets == null)
                return "imm_nodes=none";

            string navAudit = "nav=missing";
            string lockAudit = "lock=missing";
            for (int i = 0; i < nodeOffsets.Count; i++)
            {
                int pos = nodeOffsets[i];
                if (!TryReadWallC2MNodeHeaderV23LikeOriginal(data, pos, i + 1 < nodeOffsets.Count ? nodeOffsets[i + 1] : data.Length, out WallC2MNodeHeaderV23LikeOriginal h))
                    continue;

                if (!string.Equals(h.Tag, "GEOM", StringComparison.Ordinal))
                    continue;

                if (h.Name.IndexOf("Navimesh", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    WallC2MParsedMeshV23LikeOriginal nav = TryParseWallC2MGPCONodeMeshV23LikeOriginal(modelPath, data, h, out navAudit);
                    if (nav != null)
                        visual.Navimesh = nav;
                }
                else if (h.Name.IndexOf("Lockmesh", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    WallC2MParsedMeshV23LikeOriginal lockMesh = TryParseWallC2MGPCONodeMeshV23LikeOriginal(modelPath, data, h, out lockAudit);
                    if (lockMesh != null)
                        visual.Lockmesh = lockMesh;
                }
            }

            return "immGEOM nav=" + (visual.Navimesh != null ? FormatWallC2MMeshBriefV24LikeOriginal(visual.Navimesh) : navAudit) +
                   " lock=" + (visual.Lockmesh != null ? FormatWallC2MMeshBriefV24LikeOriginal(visual.Lockmesh) : lockAudit);
        }

        private static string FormatWallC2MMeshBriefV24LikeOriginal(WallC2MParsedMeshV23LikeOriginal mesh)
        {
            if (mesh == null)
                return "null";
            return mesh.NodeName +
                   ":v=" + (mesh.Vertices != null ? mesh.Vertices.Length : 0).ToString(CultureInfo.InvariantCulture) +
                   ",i=" + (mesh.Triangles != null ? mesh.Triangles.Length : 0).ToString(CultureInfo.InvariantCulture);
        }

        private static List<int> FindWallC2MNodeOffsetsV23LikeOriginal(byte[] data)
        {
            var result = new List<int>();
            for (int i = 0; i <= data.Length - 16; i++)
            {
                string tag = ReadAscii4V23LikeOriginal(data, i);
                if (tag != "GPBU" && tag != "GPSO" && tag != "GPCO" && tag != "GEOM" && tag != "FPAT" &&
                    tag != "GRUP" && tag != "MOVB" && tag != "TXRE" && tag != "DSST")
                    continue;

                int nameLen = ReadInt32LEWallV23LikeOriginal(data, i + 8);
                if (nameLen <= 0 || nameLen > 64 || i + 12 + nameLen + 10 >= data.Length)
                    continue;

                bool ascii = true;
                for (int j = 0; j < nameLen; j++)
                {
                    byte c = data[i + 12 + j];
                    if (c < 32 || c > 126)
                    {
                        ascii = false;
                        break;
                    }
                }

                if (!ascii)
                    continue;

                result.Add(i);
            }
            return result;
        }

        private static bool TryReadWallC2MNodeHeaderV23LikeOriginal(byte[] data, int pos, int next, out WallC2MNodeHeaderV23LikeOriginal h)
        {
            h = new WallC2MNodeHeaderV23LikeOriginal();
            if (data == null || pos < 0 || pos + 32 >= data.Length)
                return false;

            int nameLen = ReadInt32LEWallV23LikeOriginal(data, pos + 8);
            if (nameLen <= 0 || nameLen > 64 || pos + 12 + nameLen + 10 >= data.Length)
                return false;

            h.Tag = ReadAscii4V23LikeOriginal(data, pos);
            h.NodeId = ReadInt32LEWallV23LikeOriginal(data, pos + 4);
            h.Name = Encoding.ASCII.GetString(data, pos + 12, nameLen).TrimEnd('\0', ' ');

            // Cossacks model nodes store the text name followed by two service bytes.
            // For "Carcass" this is "\\0\\0"; for "Navimesh" the second byte is a space before "\\0".
            // Parent id begins immediately after those two bytes and can be unaligned.
            h.BodyOffset = pos + 12 + nameLen + 2;
            if (h.BodyOffset + 4 >= data.Length)
                return false;

            h.ParentId = ReadInt32LEWallV23LikeOriginal(data, h.BodyOffset);
            h.NextOffset = Mathf.Clamp(next, h.BodyOffset + 1, data.Length);
            return true;
        }

        private static WallC2MParsedMeshV23LikeOriginal TryParseWallC2MGPCONodeMeshV23LikeOriginal(string modelPath, byte[] data, WallC2MNodeHeaderV23LikeOriginal h, out string audit)
        {
            if (!TryReadWallC2MMeshLayoutV48LikeOriginal(data, h, out WallC2MMeshLayoutV48LikeOriginal layout))
            {
                audit = "mesh_layout_not_found";
                return null;
            }

            return TryParseWallC2MNodeMeshV48LikeOriginal(modelPath, data, h, layout, null, false, true, out audit);
        }

        private static WallC2MParsedMeshV23LikeOriginal TryParseWallC2MNodeMeshV48LikeOriginal(
            string modelPath,
            byte[] data,
            WallC2MNodeHeaderV23LikeOriginal h,
            WallC2MMeshLayoutV48LikeOriginal layout,
            WallC2MTransformNodeV48LikeOriginal[] transforms,
            bool applyParentMovb,
            bool parseCarcassTail,
            out string audit)
        {
            audit = string.Empty;
            if (data == null || layout.VertexCount <= 0 || layout.IndexCount <= 0 ||
                layout.VertexStart < 0 || layout.IndexStart < 0 || layout.VertexStride <= 0)
            {
                audit = "bad_layout";
                return null;
            }

            int vertexCount = layout.VertexCount;
            int indexCount = layout.IndexCount;
            if (indexCount % 3 != 0)
                indexCount -= indexCount % 3;
            if (indexCount <= 0)
            {
                audit = "no_triangles";
                return null;
            }

            var vertices = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var colors = new Color32[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                int off = layout.VertexStart + i * layout.VertexStride;
                float x = ReadFloatLEWallV23LikeOriginal(data, off);
                float y = ReadFloatLEWallV23LikeOriginal(data, off + 4);
                float z = ReadFloatLEWallV23LikeOriginal(data, off + 8);
                Vector3 p = new Vector3(x, y, z);

                if (applyParentMovb && string.Equals(h.Tag, "GEOM", StringComparison.Ordinal))
                    p = ApplyWallC2MParentMOVBChainV48LikeOriginal(p, h.ParentId, transforms);

                vertices[i] = p;

                if (layout.UvOffset >= 0 && layout.UvOffset + 8 <= layout.VertexStride)
                {
                    float u = ReadFloatLEWallV23LikeOriginal(data, off + layout.UvOffset);
                    float v = ReadFloatLEWallV23LikeOriginal(data, off + layout.UvOffset + 4);
                    uv[i] = new Vector2(u, v);
                }
                else
                {
                    uv[i] = Vector2.zero;
                }

                if (layout.DiffuseOffset >= 0 && layout.DiffuseOffset + 4 <= layout.VertexStride)
                    colors[i] = DecodeC2MColorV23LikeOriginal(ReadUInt32LEWallV23LikeOriginal(data, off + layout.DiffuseOffset));
                else
                    colors[i] = new Color32(255, 255, 255, 255);
            }

            var triangles = new int[indexCount];
            for (int i = 0; i < indexCount; i++)
            {
                int idx = ReadUInt16LEWallV23LikeOriginal(data, layout.IndexStart + i * 2);
                if (idx < 0 || idx >= vertexCount)
                    idx = 0;
                triangles[i] = idx;
            }

            WallC2MGPObjInfoV40LikeOriginal gpObj = parseCarcassTail
                ? TryParseWallC2MGPObjChunkTableV40LikeOriginal(data, h, layout.IndexStart, indexCount, vertexCount, layout.PrimitiveCount)
                : null;

            Vector3 bmin = vertices.Length > 0 ? vertices[0] : Vector3.zero;
            Vector3 bmax = bmin;
            for (int i = 1; i < vertices.Length; i++)
            {
                bmin = Vector3.Min(bmin, vertices[i]);
                bmax = Vector3.Max(bmax, vertices[i]);
            }

            var parsed = new WallC2MParsedMeshV23LikeOriginal
            {
                ModelPath = modelPath,
                NodeName = h.Name,
                Vertices = vertices,
                UV = uv,
                Colors = colors,
                Triangles = triangles,
                GPObj = gpObj,
                HasLocalBounds = vertices.Length > 0,
                LocalBoundsMin = bmin,
                LocalBoundsMax = bmax,
                MeshMode = parseCarcassTail ? "GPCO_Carcass_DrawWChunk" : "GEOM"
            };

            parsed.Audit = "node=" + h.Tag + ":" + h.Name +
                           " v=" + vertexCount.ToString(CultureInfo.InvariantCulture) +
                           " i=" + indexCount.ToString(CultureInfo.InvariantCulture) +
                           " tri=" + (indexCount / 3).ToString(CultureInfo.InvariantCulture) +
                           " vf=" + layout.VertexFormat.ToString(CultureInfo.InvariantCulture) +
                           " stride=" + layout.VertexStride.ToString(CultureInfo.InvariantCulture) +
                           " uvOff=" + layout.UvOffset.ToString(CultureInfo.InvariantCulture) +
                           " diffOff=" + layout.DiffuseOffset.ToString(CultureInfo.InvariantCulture) +
                           " meshOffset=" + layout.MeshOffset.ToString(CultureInfo.InvariantCulture) +
                           " vertexStart=" + layout.VertexStart.ToString(CultureInfo.InvariantCulture) +
                           " indexStart=" + layout.IndexStart.ToString(CultureInfo.InvariantCulture) +
                           " file=" + modelPath +
                           " gpobj=" + FormatWallC2MGPObjBriefV40LikeOriginal(gpObj);
            audit = parsed.Audit;
            return parsed;
        }


        private static WallC2MParsedMeshV23LikeOriginal MergeWallC2MGeomNodesV48LikeOriginal(
            string modelPath,
            byte[] data,
            List<WallC2MCandidateNodeV48LikeOriginal> selected,
            WallC2MTransformNodeV48LikeOriginal[] transforms,
            out string audit)
        {
            audit = string.Empty;
            if (selected == null || selected.Count == 0)
            {
                audit = "empty_selection";
                return null;
            }

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color32>();
            var tris = new List<int>();
            var chunks = new List<WallC2MGPObjChunkV40LikeOriginal>();

            int triCursor = 0;
            int vertCursor = 0;
            var nodeNames = new List<string>();

            for (int n = 0; n < selected.Count; n++)
            {
                WallC2MCandidateNodeV48LikeOriginal c = selected[n];
                WallC2MParsedMeshV23LikeOriginal part = TryParseWallC2MNodeMeshV48LikeOriginal(
                    modelPath,
                    data,
                    c.Header,
                    c.Layout,
                    transforms,
                    true,
                    false,
                    out string partAudit);
                if (part == null || part.Vertices == null || part.Triangles == null)
                {
                    audit += " skip=" + c.Header.Name + "(" + partAudit + ")";
                    continue;
                }

                int baseVert = verts.Count;
                verts.AddRange(part.Vertices);
                if (part.UV != null && part.UV.Length == part.Vertices.Length) uvs.AddRange(part.UV); else for (int i = 0; i < part.Vertices.Length; i++) uvs.Add(Vector2.zero);
                if (part.Colors != null && part.Colors.Length == part.Vertices.Length) cols.AddRange(part.Colors); else for (int i = 0; i < part.Vertices.Length; i++) cols.Add(new Color32(255, 255, 255, 255));

                for (int i = 0; i < part.Triangles.Length; i++)
                    tris.Add(baseVert + part.Triangles[i]);

                chunks.Add(new WallC2MGPObjChunkV40LikeOriginal
                {
                    Index = chunks.Count,
                    NTri = part.Triangles.Length / 3,
                    NVert = part.Vertices.Length,
                    Flags = 0,
                    TriStart = triCursor,
                    VertStart = vertCursor
                });

                triCursor += part.Triangles.Length / 3;
                vertCursor += part.Vertices.Length;
                if (nodeNames.Count < 12)
                    nodeNames.Add(c.Header.Name);
            }

            if (verts.Count == 0 || tris.Count < 3)
            {
                audit = "merged_empty " + audit;
                return null;
            }

            Vector3 bmin = verts[0];
            Vector3 bmax = bmin;
            for (int i = 1; i < verts.Count; i++)
            {
                bmin = Vector3.Min(bmin, verts[i]);
                bmax = Vector3.Max(bmax, verts[i]);
            }

            var gp = new WallC2MGPObjInfoV40LikeOriginal
            {
                Valid = true,
                ChunkCount = chunks.Count,
                SumTri = triCursor,
                SumVert = vertCursor,
                MatchTri = true,
                MatchVert = true,
                Reason = "synthetic_GEOM_chunks"
            };
            gp.Chunks.AddRange(chunks);

            var merged = new WallC2MParsedMeshV23LikeOriginal
            {
                ModelPath = modelPath,
                NodeName = "MergedGEOM",
                Vertices = verts.ToArray(),
                UV = uvs.ToArray(),
                Colors = cols.ToArray(),
                Triangles = tris.ToArray(),
                GPObj = gp,
                HasLocalBounds = true,
                LocalBoundsMin = bmin,
                LocalBoundsMax = bmax,
                MeshMode = "Merged_GEOM_TXRE"
            };

            merged.Audit = "mergedGEOM nodes=" + selected.Count.ToString(CultureInfo.InvariantCulture) +
                           " used=" + chunks.Count.ToString(CultureInfo.InvariantCulture) +
                           " v=" + verts.Count.ToString(CultureInfo.InvariantCulture) +
                           " i=" + tris.Count.ToString(CultureInfo.InvariantCulture) +
                           " preview=[" + string.Join(",", nodeNames.ToArray()) + "] " + audit;
            audit = merged.Audit;
            return merged;
        }

        private static List<string> ExtractWallC2MTextureRefsV48LikeOriginal(byte[] data, List<int> nodeOffsets)
        {
            var refs = new List<string>();
            if (data == null || nodeOffsets == null)
                return refs;

            for (int i = 0; i < nodeOffsets.Count; i++)
            {
                if (!TryReadWallC2MNodeHeaderV23LikeOriginal(data, nodeOffsets[i], i + 1 < nodeOffsets.Count ? nodeOffsets[i + 1] : data.Length, out WallC2MNodeHeaderV23LikeOriginal h))
                    continue;
                if (!string.Equals(h.Tag, "TXRE", StringComparison.Ordinal))
                    continue;

                string n = (h.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(n))
                    continue;
                if (refs.IndexOf(n) < 0)
                    refs.Add(n);
            }

            // Fallback for unusual exports: scan printable strings ending in .tga.
            if (refs.Count == 0)
            {
                string ascii = Encoding.ASCII.GetString(data);
                foreach (Match m in Regex.Matches(ascii, @"[A-Za-z0-9_\-\\/\.]{1,96}\.tga", RegexOptions.IgnoreCase))
                {
                    string s = m.Value.Trim('\0', ' ', '\t', '\r', '\n');
                    if (!string.IsNullOrWhiteSpace(s) && refs.IndexOf(s) < 0)
                        refs.Add(s);
                    if (refs.Count >= 16)
                        break;
                }
            }

            return refs;
        }

        private static int GetWallC2MVertexStrideV48LikeOriginal(int vf)
        {
            switch (vf)
            {
                case 1: return 32;  // vfTnL
                case 2: return 32;  // vf2Tex
                case 3: return 32;  // vfN
                case 4: return 36;
                case 5: return 20;  // vfT
                case 6: return 32;
                case 7: return 44;
                case 8: return 36;
                case 9: return 48;
                case 10: return 52;
                case 11: return 56;
                case 12: return 40;
                case 13: return 16;
                case 14: return 16;
                case 15: return 28;
                case 16: return 36;
                case 17: return 44;
                case 18: return 52;
                case 19: return 60;
                default: return 0;
            }
        }

        private static int GetWallC2MVertexUvOffsetV48LikeOriginal(int vf)
        {
            switch (vf)
            {
                case 1: return 20;
                case 2: return 16;
                case 3: return 24; // vfN: xyz + normal + uv. This is the critical TemnyLess/Kangaroo fix.
                case 4: return 20;
                case 5: return 12;
                case 6: return 20;
                case 7: return 32;
                case 8: return 24;
                case 9: return 36;
                case 10: return 40;
                case 11: return 44;
                case 12: return 32;
                case 15: return 20;
                case 16: return 28;
                case 17: return 36;
                case 18: return 44;
                case 19: return 52;
                default: return -1;
            }
        }

        private static int GetWallC2MVertexDiffuseOffsetV48LikeOriginal(int vf)
        {
            switch (vf)
            {
                case 1: return 16;
                case 2: return 12;
                case 4: return 16;
                case 6: return 16;
                case 7: return 28;
                case 8: return 16;
                case 9: return 32;
                case 10: return 36;
                case 11: return 40;
                case 12: return 24;
                case 13: return 12;
                case 15: return 12;
                default: return -1;
            }
        }

        private static bool TryReadWallC2MMeshLayoutV48LikeOriginal(byte[] data, WallC2MNodeHeaderV23LikeOriginal h, out WallC2MMeshLayoutV48LikeOriginal best)
        {
            best = new WallC2MMeshLayoutV48LikeOriginal
            {
                MeshOffset = -1,
                VertexStart = -1,
                IndexStart = -1,
                UvOffset = -1,
                DiffuseOffset = -1
            };

            if (data == null)
                return false;

            int[] candidates = { h.BodyOffset + 8, h.BodyOffset + 4, h.BodyOffset };
            int bestScore = -1000000;

            for (int ci = 0; ci < candidates.Length; ci++)
            {
                int mesh = candidates[ci];
                if (mesh < h.BodyOffset || mesh + 21 >= data.Length || mesh + 21 >= h.NextOffset)
                    continue;

                int nV = ReadInt32LEWallV23LikeOriginal(data, mesh);
                int nI = ReadInt32LEWallV23LikeOriginal(data, mesh + 4);
                int nP = ReadInt32LEWallV23LikeOriginal(data, mesh + 8);
                int flags = data[mesh + 12];
                int vf = (int)ReadUInt32LEWallV23LikeOriginal(data, mesh + 13);
                int pt = (int)ReadUInt32LEWallV23LikeOriginal(data, mesh + 17);
                int stride = GetWallC2MVertexStrideV48LikeOriginal(vf);
                int uvOff = GetWallC2MVertexUvOffsetV48LikeOriginal(vf);
                int diffOff = GetWallC2MVertexDiffuseOffsetV48LikeOriginal(vf);
                int vStart = mesh + 21;

                if (nV <= 0 || nV > 200000 || nI <= 0 || nI > 900000 || stride <= 0)
                    continue;
                if (nI % 3 != 0)
                    nI -= nI % 3;
                if (nI <= 0)
                    continue;

                long iStart64 = (long)vStart + (long)nV * stride;
                long end64 = iStart64 + (long)nI * 2L;
                if (iStart64 < vStart || end64 > data.Length || end64 > h.NextOffset)
                    continue;

                int iStart = (int)iStart64;
                int score = 0;
                if (mesh == h.BodyOffset + 8) score += 100;
                if (pt == 4 || pt == 5) score += 40;
                if (vf == 2 || vf == 3 || vf == 5 || vf == 15) score += 30;
                if (uvOff >= 0) score += 20;

                int sampleVerts = Mathf.Min(nV, 16);
                for (int i = 0; i < sampleVerts; i++)
                {
                    int off = vStart + i * stride;
                    float x = ReadFloatLEWallV23LikeOriginal(data, off);
                    float y = ReadFloatLEWallV23LikeOriginal(data, off + 4);
                    float z = ReadFloatLEWallV23LikeOriginal(data, off + 8);
                    if (IsFiniteWallFloatV23LikeOriginal(x) && IsFiniteWallFloatV23LikeOriginal(y) && IsFiniteWallFloatV23LikeOriginal(z) &&
                        Mathf.Abs(x) < 100000.0f && Mathf.Abs(y) < 100000.0f && Mathf.Abs(z) < 100000.0f)
                        score += 6;
                    else
                        score -= 50;

                    if (uvOff >= 0 && uvOff + 8 <= stride)
                    {
                        float u = ReadFloatLEWallV23LikeOriginal(data, off + uvOff);
                        float v = ReadFloatLEWallV23LikeOriginal(data, off + uvOff + 4);
                        if (IsFiniteWallFloatV23LikeOriginal(u) && IsFiniteWallFloatV23LikeOriginal(v) &&
                            Mathf.Abs(u) < 4096.0f && Mathf.Abs(v) < 4096.0f)
                            score += 2;
                        else
                            score -= 10;
                    }
                }

                int sampleIndices = Mathf.Min(nI, 96);
                for (int i = 0; i < sampleIndices; i++)
                {
                    int idx = ReadUInt16LEWallV23LikeOriginal(data, iStart + i * 2);
                    if (idx >= 0 && idx < nV) score += 2;
                    else score -= 20;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best.MeshOffset = mesh;
                    best.VertexCount = nV;
                    best.IndexCount = nI;
                    best.PrimitiveCount = nP;
                    best.Flags = flags;
                    best.VertexFormat = vf;
                    best.PrimitiveType = pt;
                    best.VertexStride = stride;
                    best.VertexStart = vStart;
                    best.IndexStart = iStart;
                    best.UvOffset = uvOff;
                    best.DiffuseOffset = diffOff;
                }
            }

            return bestScore > 0;
        }

        private static bool TryReadWallC2MMOVBTransformV48LikeOriginal(byte[] data, WallC2MNodeHeaderV23LikeOriginal h, out WallC2MTransformNodeV48LikeOriginal t)
        {
            t = new WallC2MTransformNodeV48LikeOriginal();
            if (data == null || !string.Equals(h.Tag, "MOVB", StringComparison.Ordinal))
                return false;

            int body = h.BodyOffset;
            if (body + 8 > data.Length || body + 8 > h.NextOffset)
                return false;

            int parent = ReadInt32LEWallV23LikeOriginal(data, body);
            int children = ReadInt32LEWallV23LikeOriginal(data, body + 4);
            if (children < 0 || children > 4096)
                return false;

            int tr = body + 8 + children * 4;
            if (tr + 48 > data.Length || tr + 48 > h.NextOffset)
                return false;

            float[] f = new float[12];
            for (int i = 0; i < 12; i++)
            {
                f[i] = ReadFloatLEWallV23LikeOriginal(data, tr + i * 4);
                if (!IsFiniteWallFloatV23LikeOriginal(f[i]) || Mathf.Abs(f[i]) > 1000000.0f)
                    return false;
            }

            t.Valid = true;
            t.Parent = parent;
            t.M00 = f[0]; t.M01 = f[1]; t.M02 = f[2];
            t.M10 = f[3]; t.M11 = f[4]; t.M12 = f[5];
            t.M20 = f[6]; t.M21 = f[7]; t.M22 = f[8];
            t.Tx = f[9]; t.Ty = f[10]; t.Tz = f[11];
            return true;
        }

        private static Vector3 ApplyWallC2MTransformRowVectorV48LikeOriginal(WallC2MTransformNodeV48LikeOriginal t, Vector3 p)
        {
            return new Vector3(
                p.x * t.M00 + p.y * t.M10 + p.z * t.M20 + t.Tx,
                p.x * t.M01 + p.y * t.M11 + p.z * t.M21 + t.Ty,
                p.x * t.M02 + p.y * t.M12 + p.z * t.M22 + t.Tz);
        }

        private static Vector3 ApplyWallC2MParentMOVBChainV48LikeOriginal(Vector3 p, int parentOrdinal, WallC2MTransformNodeV48LikeOriginal[] transforms)
        {
            int cur = parentOrdinal;
            int guard = 0;
            while (transforms != null && cur >= 0 && cur < transforms.Length && guard++ < 64)
            {
                WallC2MTransformNodeV48LikeOriginal t = transforms[cur];
                if (!t.Valid)
                    break;
                p = ApplyWallC2MTransformRowVectorV48LikeOriginal(t, p);
                cur = t.Parent;
            }
            return p;
        }

        private static WallC2MGPObjInfoV40LikeOriginal TryParseWallC2MGPObjChunkTableV40LikeOriginal(
            byte[] data,
            WallC2MNodeHeaderV23LikeOriginal h,
            int indexStart,
            int indexCount,
            int vertexCount,
            int triangleCount)
        {
            var info = new WallC2MGPObjInfoV40LikeOriginal();
            if (data == null)
            {
                info.Reason = "no_data";
                return info;
            }

            long chunkTable64 = (long)indexStart + (long)indexCount * 2L;
            if (chunkTable64 < 0 || chunkTable64 + 4L > data.Length || chunkTable64 + 4L > h.NextOffset)
            {
                info.Reason = "chunk_table_out_of_range";
                return info;
            }

            int chunkTable = (int)chunkTable64;
            int chunkCount = ReadInt32LEWallV23LikeOriginal(data, chunkTable);
            if (chunkCount < 0 || chunkCount > 4096)
            {
                info.ChunkTableOffset = chunkTable;
                info.ChunkCount = chunkCount;
                info.Reason = "bad_chunk_count";
                return info;
            }

            long recordsEnd64 = (long)chunkTable + 4L + (long)chunkCount * 8L;
            if (recordsEnd64 < chunkTable || recordsEnd64 > data.Length || recordsEnd64 > h.NextOffset)
            {
                info.ChunkTableOffset = chunkTable;
                info.ChunkCount = chunkCount;
                info.Reason = "chunk_records_out_of_range";
                return info;
            }

            info.ChunkTableOffset = chunkTable;
            info.ChunkCount = chunkCount;
            int triCursor = 0;
            int vertCursor = 0;
            for (int i = 0; i < chunkCount; i++)
            {
                int off = chunkTable + 4 + i * 8;
                int nTri = ReadUInt16LEWallV23LikeOriginal(data, off);
                int nVert = ReadUInt16LEWallV23LikeOriginal(data, off + 2);
                uint flags = ReadUInt32LEWallV23LikeOriginal(data, off + 4);

                var ch = new WallC2MGPObjChunkV40LikeOriginal
                {
                    Index = i,
                    NTri = nTri,
                    NVert = nVert,
                    Flags = flags,
                    TriStart = triCursor,
                    VertStart = vertCursor
                };
                info.Chunks.Add(ch);
                triCursor += nTri;
                vertCursor += nVert;
            }

            info.SumTri = triCursor;
            info.SumVert = vertCursor;
            info.MatchTri = (triangleCount > 0 ? info.SumTri == triangleCount : info.SumTri == indexCount / 3);
            info.MatchVert = info.SumVert == vertexCount;

            int meta = (int)recordsEnd64;
            if (meta + 4 <= data.Length && meta + 4 <= h.NextOffset)
            {
                int gpNameLen = ReadInt32LEWallV23LikeOriginal(data, meta);
                if (gpNameLen > 0 && gpNameLen <= 64 && meta + 4 + gpNameLen <= data.Length && meta + 4 + gpNameLen <= h.NextOffset)
                {
                    bool ascii = true;
                    for (int i = 0; i < gpNameLen; i++)
                    {
                        byte c = data[meta + 4 + i];
                        if (c < 32 || c > 126)
                        {
                            ascii = false;
                            break;
                        }
                    }

                    if (ascii)
                    {
                        info.GPName = Encoding.ASCII.GetString(data, meta + 4, gpNameLen).TrimEnd('\0', ' ');
                        int frameOff = meta + 4 + gpNameLen;
                        if (frameOff + 4 <= data.Length && frameOff + 4 <= h.NextOffset)
                            info.FrameIdx = ReadInt32LEWallV23LikeOriginal(data, frameOff);
                    }
                }
            }

            info.Valid = info.MatchTri && info.MatchVert;
            info.Reason = info.Valid ? "ok" : "sum_mismatch";
            return info;
        }

        private static string FormatWallC2MGPObjBriefV40LikeOriginal(WallC2MGPObjInfoV40LikeOriginal info)
        {
            if (info == null)
                return "null";
            return "valid=" + info.Valid +
                   " gpName='" + (info.GPName ?? string.Empty) + "'" +
                   " frameIdx=" + info.FrameIdx.ToString(CultureInfo.InvariantCulture) +
                   " chunks=" + info.ChunkCount.ToString(CultureInfo.InvariantCulture) +
                   " sumTri=" + info.SumTri.ToString(CultureInfo.InvariantCulture) +
                   " sumVert=" + info.SumVert.ToString(CultureInfo.InvariantCulture) +
                   " matchTri=" + info.MatchTri +
                   " matchVert=" + info.MatchVert +
                   " tableOffset=" + info.ChunkTableOffset.ToString(CultureInfo.InvariantCulture) +
                   " reason=" + (info.Reason ?? string.Empty);
        }

        private static string BuildWallC2MGPObjAuditLineV40LikeOriginal(WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal c2m)
        {
            string model = desc != null ? (desc.ModelPath ?? string.Empty) : string.Empty;
            int id = desc != null ? desc.SpriteIndex : -1;
            if (c2m == null)
                return "id=" + id.ToString(CultureInfo.InvariantCulture) + " model='" + model + "' c2m=null";

            WallC2MGPObjInfoV40LikeOriginal info = c2m.GPObj;
            if (info == null)
                return "id=" + id.ToString(CultureInfo.InvariantCulture) + " model='" + model + "' gpobj=null";

            int triField = c2m.Triangles != null ? c2m.Triangles.Length / 3 : 0;
            int vertCount = c2m.Vertices != null ? c2m.Vertices.Length : 0;
            string chunks = FormatWallC2MGPObjChunksV40LikeOriginal(info);

            return "id=" + id.ToString(CultureInfo.InvariantCulture) +
                   " name=" + (desc != null ? desc.Name : string.Empty) +
                   " model='" + model + "'" +
                   " node=" + (c2m.NodeName ?? string.Empty) +
                   " gpName='" + (info.GPName ?? string.Empty) + "'" +
                   " frameIdx=" + info.FrameIdx.ToString(CultureInfo.InvariantCulture) +
                   " chunks=" + info.ChunkCount.ToString(CultureInfo.InvariantCulture) +
                   " sumTri=" + info.SumTri.ToString(CultureInfo.InvariantCulture) + "/" + triField.ToString(CultureInfo.InvariantCulture) +
                   " sumVert=" + info.SumVert.ToString(CultureInfo.InvariantCulture) + "/" + vertCount.ToString(CultureInfo.InvariantCulture) +
                   " matchTri=" + info.MatchTri +
                   " matchVert=" + info.MatchVert +
                   " tableOffset=" + info.ChunkTableOffset.ToString(CultureInfo.InvariantCulture) +
                   " reason=" + (info.Reason ?? string.Empty) +
                   " chunkPreview=[" + chunks + "]";
        }

        private static string FormatWallC2MGPObjChunksV40LikeOriginal(WallC2MGPObjInfoV40LikeOriginal info)
        {
            if (info == null || info.Chunks == null || info.Chunks.Count == 0)
                return string.Empty;

            int limit = Mathf.Min(C2WallObjectsV40GPObjChunkPreviewLimitLikeOriginal, info.Chunks.Count);
            var parts = new List<string>(limit + 1);
            for (int i = 0; i < limit; i++)
            {
                WallC2MGPObjChunkV40LikeOriginal ch = info.Chunks[i];
                parts.Add("#" + ch.Index.ToString(CultureInfo.InvariantCulture) +
                          ":nTri=" + ch.NTri.ToString(CultureInfo.InvariantCulture) +
                          ",nVert=" + ch.NVert.ToString(CultureInfo.InvariantCulture) +
                          ",flags=0x" + ch.Flags.ToString("X8", CultureInfo.InvariantCulture) +
                          ",triStart=" + ch.TriStart.ToString(CultureInfo.InvariantCulture) +
                          ",vertStart=" + ch.VertStart.ToString(CultureInfo.InvariantCulture));
            }
            if (info.Chunks.Count > limit)
                parts.Add("...+" + (info.Chunks.Count - limit).ToString(CultureInfo.InvariantCulture));
            return string.Join(";", parts.ToArray());
        }

        private Mesh TryBuildWallC2MGPObjDrawWChunkBakedMeshV50LikeOriginal(
            WallC2MParsedMeshV23LikeOriginal c2m,
            Vector3[] worldVertices,
            WallSpriteDescV1LikeOriginal desc,
            out string audit)
        {
            audit = "disabled";
            if (!C2WallObjectsV50BakeDrawWChunkUVIntoMeshLikeOriginal)
                return null;
            if (c2m == null || worldVertices == null || desc == null || !IsWallDambaC2MModelV33LikeOriginal(desc))
            {
                audit = "not_damba_or_missing_input";
                return null;
            }
            if (c2m.GPObj == null || !c2m.GPObj.Valid || c2m.GPObj.Chunks == null || c2m.GPObj.Chunks.Count == 0)
            {
                audit = "no_valid_gpobj_chunks";
                return null;
            }
            if (c2m.Triangles == null || c2m.Triangles.Length < 3 || c2m.UV == null || c2m.UV.Length != worldVertices.Length)
            {
                audit = "missing_triangles_or_uv";
                return null;
            }

            Texture2D gpTex = TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(c2m, out string texSource, out List<WallG16SquareV47LikeOriginal> squares);
            if (gpTex == null || squares == null || squares.Count == 0 || gpTex.width <= 0 || gpTex.height <= 0)
            {
                audit = "gp_texture_or_squares_missing source='" + (texSource ?? string.Empty) + "'";
                return null;
            }

            int chunkCount = Mathf.Min(c2m.GPObj.Chunks.Count, squares.Count);
            if (chunkCount <= 0)
            {
                audit = "no_chunk_square_pairs chunks=" + c2m.GPObj.Chunks.Count.ToString(CultureInfo.InvariantCulture) +
                        " squares=" + squares.Count.ToString(CultureInfo.InvariantCulture);
                return null;
            }

            var verts = new List<Vector3>(c2m.Triangles.Length);
            var uvs = new List<Vector2>(c2m.Triangles.Length);
            var cols = new List<Color32>(c2m.Triangles.Length);
            var tris = new List<int>(c2m.Triangles.Length);

            int emittedChunks = 0;
            int emittedTris = 0;
            int skippedChunks = 0;
            int clampedUv = 0;
            int badIndices = 0;
            int frameW = Mathf.Max(1, gpTex.width);
            int frameH = Mathf.Max(1, gpTex.height);

            for (int ci = 0; ci < chunkCount; ci++)
            {
                WallC2MGPObjChunkV40LikeOriginal ch = c2m.GPObj.Chunks[ci];
                WallG16SquareV47LikeOriginal sq = squares[ci];
                int triCount = Mathf.Max(0, ch.NTri);
                int triStart = Mathf.Max(0, ch.TriStart);
                int indexOffset = triStart * 3;
                int indexLength = triCount * 3;

                if (sq.Side <= 0 || indexLength <= 0 || indexOffset < 0 || indexOffset + indexLength > c2m.Triangles.Length)
                {
                    skippedChunks++;
                    continue;
                }

                bool emittedThisChunk = false;
                var remap = new Dictionary<int, int>();
                for (int k = 0; k < indexLength; k += 3)
                {
                    int old0 = c2m.Triangles[indexOffset + k + 0];
                    int old1 = c2m.Triangles[indexOffset + k + 1];
                    int old2 = c2m.Triangles[indexOffset + k + 2];
                    if (old0 < 0 || old0 >= worldVertices.Length || old1 < 0 || old1 >= worldVertices.Length || old2 < 0 || old2 >= worldVertices.Length)
                    {
                        badIndices++;
                        continue;
                    }

                    int new0 = GetOrAddWallC2MDrawWChunkVertexV50LikeOriginal(remap, worldVertices, c2m.UV, c2m.Colors, old0, sq, frameW, frameH, verts, uvs, cols, ref clampedUv);
                    int new1 = GetOrAddWallC2MDrawWChunkVertexV50LikeOriginal(remap, worldVertices, c2m.UV, c2m.Colors, old1, sq, frameW, frameH, verts, uvs, cols, ref clampedUv);
                    int new2 = GetOrAddWallC2MDrawWChunkVertexV50LikeOriginal(remap, worldVertices, c2m.UV, c2m.Colors, old2, sq, frameW, frameH, verts, uvs, cols, ref clampedUv);
                    tris.Add(new0);
                    tris.Add(new1);
                    tris.Add(new2);
                    emittedTris++;
                    emittedThisChunk = true;
                }

                if (emittedThisChunk)
                    emittedChunks++;
                else
                    skippedChunks++;
            }

            if (verts.Count == 0 || tris.Count < 3)
            {
                audit = "empty_after_bake chunks=" + chunkCount.ToString(CultureInfo.InvariantCulture) +
                        " skipped=" + skippedChunks.ToString(CultureInfo.InvariantCulture) +
                        " badIdx=" + badIndices.ToString(CultureInfo.InvariantCulture) +
                        " source='" + (texSource ?? string.Empty) + "'";
                c2m.DrawWChunkUvBakedV50 = false;
                c2m.DrawWChunkUvBakedAuditV50 = audit;
                return null;
            }

            Mesh mesh = new Mesh
            {
                name = "C2_SavedModelBackedWL_V50_DRAWWCHUNK_BAKED_UV_" + (desc != null ? desc.Name : "DAMBA") + "_" + Path.GetFileNameWithoutExtension(c2m.ModelPath ?? string.Empty)
            };
            if (verts.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();
            try { mesh.RecalculateNormals(); } catch { /* old/degenerate bridge chunks can contain zero-area triangles */ }

            audit = "ok contract=" + C2WallObjectsV50DrawWChunkContractLikeOriginal +
                    " v57='" + C2WallObjectsV57DrawWChunkContractLikeOriginal + "'" +
                    " rawTopLeft=" + C2WallObjectsV57DrawWChunkUseTemnyLessRawTopLeftUV +
                    " halfTexel=" + C2WallObjectsV57DrawWChunkUseHalfTexelCenters +
                    " flipLocalU=" + C2WallObjectsV57DrawWChunkFlipLocalU +
                    " flipLocalV=" + C2WallObjectsV57DrawWChunkFlipLocalV +
                    " swapLocalUV=" + C2WallObjectsV57DrawWChunkSwapLocalUV +
                    " gpName='" + (c2m.GPObj.GPName ?? string.Empty) + "'" +
                    " frameIdx=" + c2m.GPObj.FrameIdx.ToString(CultureInfo.InvariantCulture) +
                    " frame=" + frameW.ToString(CultureInfo.InvariantCulture) + "x" + frameH.ToString(CultureInfo.InvariantCulture) +
                    " chunks=" + chunkCount.ToString(CultureInfo.InvariantCulture) +
                    " emittedChunks=" + emittedChunks.ToString(CultureInfo.InvariantCulture) +
                    " skipped=" + skippedChunks.ToString(CultureInfo.InvariantCulture) +
                    " emittedTris=" + emittedTris.ToString(CultureInfo.InvariantCulture) +
                    " verts=" + verts.Count.ToString(CultureInfo.InvariantCulture) +
                    " clampedUv=" + clampedUv.ToString(CultureInfo.InvariantCulture) +
                    " badIdx=" + badIndices.ToString(CultureInfo.InvariantCulture) +
                    " source='" + (texSource ?? string.Empty) + "'";
            c2m.DrawWChunkUvBakedV50 = true;
            c2m.DrawWChunkUvBakedAuditV50 = audit;
            return mesh;
        }

        private static int GetOrAddWallC2MDrawWChunkVertexV50LikeOriginal(
            Dictionary<int, int> remap,
            Vector3[] sourceVerts,
            Vector2[] sourceUv,
            Color32[] sourceColors,
            int oldIndex,
            WallG16SquareV47LikeOriginal sq,
            int frameW,
            int frameH,
            List<Vector3> outVerts,
            List<Vector2> outUv,
            List<Color32> outColors,
            ref int clampedUv)
        {
            if (remap != null && remap.TryGetValue(oldIndex, out int existing))
                return existing;

            int newIndex = outVerts.Count;
            if (remap != null)
                remap[oldIndex] = newIndex;

            outVerts.Add(sourceVerts[oldIndex]);

            Vector2 local = sourceUv != null && oldIndex >= 0 && oldIndex < sourceUv.Length ? sourceUv[oldIndex] : Vector2.zero;
            float u = local.x;
            float v = local.y;
            if (float.IsNaN(u) || float.IsInfinity(u)) u = 0.0f;
            if (float.IsNaN(v) || float.IsInfinity(v)) v = 0.0f;

            // V57: exact TemnyLess DrawWChunk contract.  Source C2M UV is local chunk UV.
            // Keep the geometry untouched; only convert local UV into the packed G16 square rect.
            // Do not add Unity's V flip here: the Melinoja RGBA buffer is uploaded with the same
            // top-left row order that TemnyLess samples directly by ty.
            float lu = u;
            float lv = v;
            if (C2WallObjectsV57DrawWChunkSwapLocalUV)
            {
                float t = lu;
                lu = lv;
                lv = t;
            }
            if (C2WallObjectsV57DrawWChunkFlipLocalU) lu = 1.0f - lu;
            if (C2WallObjectsV57DrawWChunkFlipLocalV) lv = 1.0f - lv;

            float cu = Mathf.Clamp01(lu);
            float cv = Mathf.Clamp01(lv);
            if (Mathf.Abs(cu - lu) > 0.0001f || Mathf.Abs(cv - lv) > 0.0001f)
                clampedUv++;

            float sideForUv = Mathf.Max(1.0f, sq.Side);
            float tx = sq.X + cu * sideForUv;
            float ty = sq.Y + cv * sideForUv;
            if (C2WallObjectsV57DrawWChunkUseHalfTexelCenters)
            {
                // Prevent packed-square neighbour bleed under Unity sampling.
                tx = sq.X + 0.5f + cu * Mathf.Max(0.0f, sideForUv - 1.0f);
                ty = sq.Y + 0.5f + cv * Mathf.Max(0.0f, sideForUv - 1.0f);
            }

            float finalU = tx / Mathf.Max(1.0f, frameW);
            float rawTopLeftV = ty / Mathf.Max(1.0f, frameH);
            float finalV = C2WallObjectsV57DrawWChunkUseTemnyLessRawTopLeftUV
                ? rawTopLeftV
                : (C2WallObjectsV50DrawWChunkTopLeftToUnityVFlipLikeOriginal ? 1.0f - rawTopLeftV : rawTopLeftV);
            outUv.Add(new Vector2(finalU, finalV));

            if (sourceColors != null && oldIndex >= 0 && oldIndex < sourceColors.Length)
                outColors.Add(sourceColors[oldIndex]);
            else
                outColors.Add(new Color32(255, 255, 255, 255));

            return newIndex;
        }

        private static bool HasWallC2MGPObjChunkSubmeshesV41LikeOriginal(WallC2MParsedMeshV23LikeOriginal c2m)
        {
            if (!C2WallObjectsV41UseGPObjChunkSubmeshesLikeOriginal || c2m == null || c2m.GPObj == null || !c2m.GPObj.Valid)
                return false;
            return c2m.GPObj.Chunks != null && c2m.GPObj.Chunks.Count > 0 && c2m.Triangles != null && c2m.Triangles.Length >= 3;
        }

        private static bool ApplyWallC2MGPObjChunkSubmeshesV41LikeOriginal(Mesh mesh, WallC2MParsedMeshV23LikeOriginal c2m, out string audit)
        {
            audit = "disabled_or_invalid";
            if (mesh == null || !HasWallC2MGPObjChunkSubmeshesV41LikeOriginal(c2m))
                return false;

            try
            {
                WallC2MGPObjInfoV40LikeOriginal info = c2m.GPObj;
                mesh.subMeshCount = info.Chunks.Count;

                int applied = 0;
                int empty = 0;
                for (int i = 0; i < info.Chunks.Count; i++)
                {
                    WallC2MGPObjChunkV40LikeOriginal ch = info.Chunks[i];
                    int triCount = Mathf.Max(0, ch.NTri);
                    int triStart = Mathf.Max(0, ch.TriStart);
                    int indexOffset = triStart * 3;
                    int indexLength = triCount * 3;

                    if (indexLength <= 0 || indexOffset < 0 || indexOffset + indexLength > c2m.Triangles.Length)
                    {
                        mesh.SetTriangles(new int[0], i, false);
                        empty++;
                        continue;
                    }

                    int[] localTri = new int[indexLength];
                    Array.Copy(c2m.Triangles, indexOffset, localTri, 0, indexLength);
                    mesh.SetTriangles(localTri, i, false);
                    applied++;
                }

                audit = "submeshes=" + info.Chunks.Count.ToString(CultureInfo.InvariantCulture) +
                        " applied=" + applied.ToString(CultureInfo.InvariantCulture) +
                        " empty=" + empty.ToString(CultureInfo.InvariantCulture) +
                        " gpName='" + (info.GPName ?? string.Empty) + "'" +
                        " frameIdx=" + info.FrameIdx.ToString(CultureInfo.InvariantCulture) +
                        " contract=" + C2WallObjectsV41ChunkRenderContractLikeOriginal;
                return applied > 0;
            }
            catch (Exception ex)
            {
                audit = "failed " + ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private static Material[] BuildWallC2MGPObjChunkMaterialsV41LikeOriginal(Material baseMaterial, WallC2MParsedMeshV23LikeOriginal c2m)
        {
            int count = c2m != null && c2m.GPObj != null && c2m.GPObj.Chunks != null ? c2m.GPObj.Chunks.Count : 0;
            if (count <= 0)
                return baseMaterial != null ? new[] { baseMaterial } : new Material[0];

            Material[] mats = new Material[count];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = baseMaterial;
            return mats;
        }

        private static Material[] BuildWallC2MGPObjChunkMaterialsV47LikeOriginal(
            Material baseMaterial,
            WallC2MParsedMeshV23LikeOriginal c2m,
            List<WallG16SquareV47LikeOriginal> squares,
            int frameWidth,
            int frameHeight,
            bool damba)
        {
            int count = c2m != null && c2m.GPObj != null && c2m.GPObj.Chunks != null ? c2m.GPObj.Chunks.Count : 0;
            if (count <= 0)
                return baseMaterial != null ? new[] { baseMaterial } : new Material[0];

            if (!C2WallObjectsV47UseG16SquareRectsForGPObjChunksLikeOriginal ||
                baseMaterial == null ||
                !damba ||
                squares == null ||
                squares.Count < count ||
                frameWidth <= 0 ||
                frameHeight <= 0)
            {
                return BuildWallC2MGPObjChunkMaterialsV41LikeOriginal(baseMaterial, c2m);
            }

            Material[] mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                WallG16SquareV47LikeOriginal sq = squares[i];
                Material mat = new Material(baseMaterial)
                {
                    name = baseMaterial.name + "_sq" + i.ToString(CultureInfo.InvariantCulture)
                };

                if (mat.HasProperty("_MainTex"))
                {
                    float sx = sq.Side / (float)frameWidth;
                    float sy = sq.Side / (float)frameHeight;
                    float ox = sq.X / (float)frameWidth;
                    float oy = 1.0f - ((sq.Y + sq.Side) / (float)frameHeight);
                    mat.SetTextureScale("_MainTex", new Vector2(sx, sy));
                    mat.SetTextureOffset("_MainTex", new Vector2(ox, oy));
                }

                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", Color.white);
                if (mat.HasProperty("_UseVertexColor"))
                    mat.SetFloat("_UseVertexColor", 0.0f);

                mats[i] = mat;
            }

            return mats;
        }

        private static string BuildWallC2MGPObjChunkRenderAuditLineV41LikeOriginal(WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal c2m)
        {
            int id = desc != null ? desc.SpriteIndex : -1;
            string model = desc != null ? (desc.ModelPath ?? string.Empty) : string.Empty;
            if (!HasWallC2MGPObjChunkSubmeshesV41LikeOriginal(c2m))
                return "id=" + id.ToString(CultureInfo.InvariantCulture) + " model='" + model + "' active=False reason=no_valid_gpobj";

            WallC2MGPObjInfoV40LikeOriginal info = c2m.GPObj;
            int applied = 0;
            int empty = 0;
            for (int i = 0; i < info.Chunks.Count; i++)
            {
                if (info.Chunks[i].NTri > 0)
                    applied++;
                else
                    empty++;
            }

            return "id=" + id.ToString(CultureInfo.InvariantCulture) +
                   " name=" + (desc != null ? desc.Name : string.Empty) +
                   " model='" + model + "'" +
                   " active=True" +
                   " gpName='" + (info.GPName ?? string.Empty) + "'" +
                   " frameIdx=" + info.FrameIdx.ToString(CultureInfo.InvariantCulture) +
                   " submeshes=" + info.ChunkCount.ToString(CultureInfo.InvariantCulture) +
                   " expectedApplied=" + applied.ToString(CultureInfo.InvariantCulture) +
                   " expectedEmpty=" + empty.ToString(CultureInfo.InvariantCulture) +
                   " matchTri=" + info.MatchTri +
                   " matchVert=" + info.MatchVert;
        }

        private static int FindWallC2MVertexStartV23LikeOriginal(byte[] data, WallC2MNodeHeaderV23LikeOriginal h, int vertexCount, int indexCount, int vertexStride)
        {
            int bestStart = -1;
            int bestScore = -999999;
            int minStart = Mathf.Max(h.BodyOffset + 20, h.BodyOffset + 4);
            int maxStart = Mathf.Min(h.BodyOffset + 96, data.Length - 12);

            for (int start = minStart; start <= maxStart; start++)
            {
                long indexStart64 = (long)start + (long)vertexCount * vertexStride;
                if (indexStart64 < start || indexStart64 + (long)indexCount * 2L > data.Length)
                    continue;

                int indexStart = (int)indexStart64;
                int score = 0;
                int sampleVerts = Mathf.Min(vertexCount, 16);
                for (int i = 0; i < sampleVerts; i++)
                {
                    int off = start + i * vertexStride;
                    float x = ReadFloatLEWallV23LikeOriginal(data, off);
                    float y = ReadFloatLEWallV23LikeOriginal(data, off + 4);
                    float z = ReadFloatLEWallV23LikeOriginal(data, off + 8);
                    if (IsFiniteWallFloatV23LikeOriginal(x) && IsFiniteWallFloatV23LikeOriginal(y) && IsFiniteWallFloatV23LikeOriginal(z) &&
                        Mathf.Abs(x) < 100000.0f && Mathf.Abs(y) < 100000.0f && Mathf.Abs(z) < 100000.0f)
                        score += 6;
                    else
                        score -= 20;

                    float u = ReadFloatLEWallV23LikeOriginal(data, off + 16);
                    float v = ReadFloatLEWallV23LikeOriginal(data, off + 20);
                    if (IsFiniteWallFloatV23LikeOriginal(u) && IsFiniteWallFloatV23LikeOriginal(v) &&
                        Mathf.Abs(u) < 64.0f && Mathf.Abs(v) < 64.0f)
                        score += 2;
                }

                int sampleIndices = Mathf.Min(indexCount, 96);
                for (int i = 0; i < sampleIndices; i++)
                {
                    int idx = ReadUInt16LEWallV23LikeOriginal(data, indexStart + i * 2);
                    if (idx >= 0 && idx < vertexCount)
                        score += 2;
                    else
                        score -= 10;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestStart = start;
                }
            }

            return bestScore > 0 ? bestStart : -1;
        }

        private static Color32 DecodeC2MColorV23LikeOriginal(uint c)
        {
            // Stored as D3D-style 0xAARRGGBB in observed Carcass streams; white is 0xFFFFFFFF.
            byte a = (byte)((c >> 24) & 255);
            byte r = (byte)((c >> 16) & 255);
            byte g = (byte)((c >> 8) & 255);
            byte b = (byte)(c & 255);
            if (a == 0 && (r != 0 || g != 0 || b != 0))
                a = 255;
            return new Color32(r, g, b, a);
        }

        private static string ReadAscii4V23LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
                return string.Empty;
            return Encoding.ASCII.GetString(data, offset, 4);
        }

        private static int ReadInt32LEWallV23LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
                return 0;
            return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
        }

        private static uint ReadUInt32LEWallV23LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
                return 0;
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        private static int ReadUInt16LEWallV23LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 2 > data.Length)
                return 0;
            return data[offset] | (data[offset + 1] << 8);
        }

        private static float ReadFloatLEWallV23LikeOriginal(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
                return 0.0f;
            byte[] b = { data[offset], data[offset + 1], data[offset + 2], data[offset + 3] };
            return BitConverter.ToSingle(b, 0);
        }

        private static bool IsFiniteWallFloatV23LikeOriginal(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }


        // V167 COMPILE RESTORE: generic aligned mesh helper used by non-fence overlay fallback.
        private Mesh BuildSavedMapWallSpriteAlignedNoEmbedV20LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, float wPx, float hPx)
        {
            Mesh mesh = null;

            if (desc != null && (desc.AlignMode == 'V' || desc.AlignMode == 'S') && desc.AlignPoints.Count >= 2)
                mesh = BuildSavedMapWallSpriteVerticalAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

            if (mesh == null && desc != null && desc.AlignMode == 'H')
                mesh = BuildSavedMapWallSpriteGroundAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

            if (mesh == null && desc != null && desc.AlignMode == 'U' && desc.AlignPoints.Count >= 3)
                mesh = BuildSavedMapWallSpriteUniversalAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

            if (mesh == null)
                mesh = BuildSavedMapWallSpriteBillboardAdaptedMeshV10LikeOriginal(s, desc, wPx, hPx);

            return mesh;
        }

        private static List<WallG16SquareV47LikeOriginal> TryParseWallG16FrameSquaresV47LikeOriginal(string path, int frameIdx, out string audit)
        {
            audit = "disabled";
            if (!C2WallObjectsV47UseG16SquareRectsForGPObjChunksLikeOriginal)
                return null;

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    audit = "missing_file";
                    return null;
                }

                byte[] data = File.ReadAllBytes(path);
                if (data.Length < 64 || ReadAscii4V23LikeOriginal(data, 0) != "GU16")
                {
                    audit = "not_GU16_or_short";
                    return null;
                }

                int pos = 4;
                int blockSize = ReadInt32LEWallV23LikeOriginal(data, pos); pos += 4;
                int framesPerSegment = data[pos++];
                int spriteCount = ReadUInt16LEWallV23LikeOriginal(data, pos); pos += 2;
                int width = ReadUInt16LEWallV23LikeOriginal(data, pos); pos += 2;
                int height = ReadUInt16LEWallV23LikeOriginal(data, pos); pos += 2;
                int maxWorkbuf = ReadInt32LEWallV23LikeOriginal(data, pos); pos += 4;
                int packSegments = ReadUInt16LEWallV23LikeOriginal(data, pos); pos += 2;

                if (spriteCount <= 0 || spriteCount > 4096 || frameIdx < 0 || frameIdx >= spriteCount)
                {
                    audit = "bad_counts frame=" + frameIdx.ToString(CultureInfo.InvariantCulture) +
                            " sprites=" + spriteCount.ToString(CultureInfo.InvariantCulture);
                    return null;
                }

                var segmentOffsets = new int[packSegments];
                var segmentFlags = new int[packSegments];
                for (int i = 0; i < packSegments; i++)
                {
                    if (pos + 4 > data.Length)
                    {
                        audit = "short_pack_segment_table";
                        return null;
                    }

                    uint raw = ReadUInt32LEWallV23LikeOriginal(data, pos);
                    pos += 4;
                    segmentOffsets[i] = (int)(raw >> 4);
                    segmentFlags[i] = (int)(raw & 0x0F);
                }

                var squareCounts = new int[spriteCount];
                for (int i = 0; i < spriteCount; i++)
                {
                    if (pos + 2 > data.Length)
                    {
                        audit = "short_sprite_header_table";
                        return null;
                    }
                    squareCounts[i] = ReadUInt16LEWallV23LikeOriginal(data, pos);
                    pos += 2;
                }

                if (packSegments <= 0)
                {
                    audit = "unsupported_packSegments=" + packSegments.ToString(CultureInfo.InvariantCulture);
                    return null;
                }

                int segIdx = Mathf.Clamp(frameIdx / Mathf.Max(1, framesPerSegment), 0, packSegments - 1);
                int firstInSeg = segIdx * Mathf.Max(1, framesPerSegment);
                int frameInSeg = frameIdx - firstInSeg;
                int segment = segmentOffsets[segIdx];
                if (segment + 12 > data.Length)
                {
                    audit = "short_segment";
                    return null;
                }

                int outLen = ReadInt32LEWallV23LikeOriginal(data, segment);
                int work = segment + 4;
                int colorOffRel = ReadInt32LEWallV23LikeOriginal(data, work);
                int alphaOffRel = ReadInt32LEWallV23LikeOriginal(data, work + 4);
                int p = work + 8;

                int nFramesInSeg = segIdx == packSegments - 1
                    ? spriteCount - firstInSeg
                    : Mathf.Min(framesPerSegment, spriteCount - firstInSeg);

                for (int f = 0; f < nFramesInSeg; f++)
                {
                    if (p + 2 > data.Length)
                    {
                        audit = "short_frame_header f=" + f.ToString(CultureInfo.InvariantCulture);
                        return null;
                    }

                    int nSquares = ReadUInt16LEWallV23LikeOriginal(data, p);
                    p += 2;
                    if (nSquares < 0 || nSquares > 4096 || p + nSquares * 4 > data.Length)
                    {
                        audit = "bad_square_table f=" + f.ToString(CultureInfo.InvariantCulture) +
                                " n=" + nSquares.ToString(CultureInfo.InvariantCulture);
                        return null;
                    }

                    if (f == frameInSeg)
                    {
                        var result = new List<WallG16SquareV47LikeOriginal>(nSquares);
                        for (int i = 0; i < nSquares; i++)
                        {
                            uint header = ReadUInt32LEWallV23LikeOriginal(data, p + i * 4);
                            int sidePow = (int)((header >> 28) & 0x0F);
                            int side = 1 << sidePow;
                            int x = SignExtendWallG16ChunkCoord12V47LikeOriginal((int)((header >> 12) & 0x0FFF));
                            int y = SignExtendWallG16ChunkCoord12V47LikeOriginal((int)(header & 0x0FFF));
                            result.Add(new WallG16SquareV47LikeOriginal
                            {
                                Index = i,
                                X = x,
                                Y = y,
                                Side = side,
                                Header = header
                            });
                        }

                        audit = "ok frame=" + frameIdx.ToString(CultureInfo.InvariantCulture) +
                                " squares=" + result.Count.ToString(CultureInfo.InvariantCulture) +
                                " size=" + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                                " blockSize=" + blockSize.ToString(CultureInfo.InvariantCulture) +
                                " framesPerSeg=" + framesPerSegment.ToString(CultureInfo.InvariantCulture) +
                                " maxWorkbuf=" + maxWorkbuf.ToString(CultureInfo.InvariantCulture) +
                                " colorOffRel=" + colorOffRel.ToString(CultureInfo.InvariantCulture) +
                                " alphaOffRel=" + alphaOffRel.ToString(CultureInfo.InvariantCulture);
                        return result;
                    }

                    p += nSquares * 4;
                }

                audit = "frame_not_found";
                return null;
            }
            catch (Exception ex)
            {
                audit = "failed " + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static int SignExtendWallG16ChunkCoord12V47LikeOriginal(int value)
        {
            value &= 0x0FFF;
            return (value & 0x0800) != 0 ? value | unchecked((int)0xFFFFF000) : value;
        }

        private static List<string> BuildWallC2MGPObjG16CandidatePathsV42LikeOriginal(string gpName)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(gpName))
                return result;

            string clean = gpName.Trim().TrimEnd('\0', ' ');
            string[] names =
            {
                clean + ".g16",
                clean + ".G16",
                clean + ".g17",
                clean + ".G17"
            };

            string dataPath = Application.dataPath ?? string.Empty;
            string streaming = Application.streamingAssetsPath ?? string.Empty;
            string[] roots =
            {
                Path.Combine(dataPath, "Resources"),
                Path.Combine(dataPath, "Resources", "Cash"),
                Path.Combine(dataPath, "Resources", "Models"),
                Path.Combine(dataPath, "Resources", "WallObjects"),
                Path.Combine(streaming, "Cossacks2", "Data"),
                Path.Combine(streaming, "Cossacks2", "Data", "Cash"),
                Path.Combine(streaming, "Cossacks2", "Data1"),
                Path.Combine(streaming, "Cossacks2", "Data1", "Cash"),
                @"C:\GSC Game World\Cossacks II\Data",
                @"C:\GSC Game World\Cossacks II\Data\Cash",
                @"C:\GSC Game World\Cossacks II\Data1",
                @"C:\GSC Game World\Cossacks II\Data1\Cash"
            };

            for (int r = 0; r < roots.Length; r++)
            {
                for (int n = 0; n < names.Length; n++)
                    result.Add(Path.Combine(roots[r], names[n]));
            }

            return result;
        }

        private static Texture2D TryLoadG16FrameViaMelinojaV42LikeOriginal(string abs, int frameIndex, out string source)
        {
            source = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "path_not_found:" + (abs ?? string.Empty);
                    return null;
                }

                Type bridgeType = ResolveMelinojaBridgeTypeV2LikeOriginal();
                if (bridgeType == null)
                {
                    source = "Melinoja bridge type not found";
                    return null;
                }

                if (!C2WallObjectsLoadedG16V2LikeOriginal.Contains(abs))
                {
                    MethodInfo load = bridgeType.GetMethod("LoadG16ToMemory", BindingFlags.Public | BindingFlags.Static);
                    if (load != null)
                    {
                        object[] loadArgs = { abs, null, false };
                        object loadResult = load.Invoke(null, loadArgs);
                        bool loaded = loadResult is bool b && b;
                        string loadErr = loadArgs.Length > 1 ? loadArgs[1] as string : string.Empty;
                        if (!loaded)
                        {
                            source = "Melinoja LoadG16ToMemory failed: " + (loadErr ?? string.Empty);
                            return null;
                        }
                    }

                    C2WallObjectsLoadedG16V2LikeOriginal.Add(abs);
                }

                MethodInfo mi = bridgeType.GetMethod("TryGetG16FrameRGBA", BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                {
                    source = "Melinoja TryGetG16FrameRGBA not found";
                    return null;
                }

                object[] args = { abs, frameIndex, 0, 0, null, null };
                object result = mi.Invoke(null, args);
                if (!(result is bool ok) || !ok)
                {
                    string err = args.Length > 5 ? args[5] as string : string.Empty;
                    source = "Melinoja TryGetG16FrameRGBA failed: " + (err ?? string.Empty);
                    return null;
                }

                int w = args[2] is int iw ? iw : 0;
                int h = args[3] is int ih ? ih : 0;
                byte[] rgba = args[4] as byte[];
                if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                {
                    source = "invalid_frame path='" + abs + "' frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) +
                             " size=" + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture);
                    return null;
                }

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                tex.name = "C2_GPObj_" + Path.GetFileNameWithoutExtension(abs) + "_frame_" + frameIndex.ToString(CultureInfo.InvariantCulture);
                tex.LoadRawTextureData(rgba);
                tex.Apply(false, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                source = "MelinojaGP:" + abs + "#" + frameIndex.ToString(CultureInfo.InvariantCulture);
                return tex;
            }
            catch (Exception ex)
            {
                source = "MelinojaGP failed: " + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private Texture2D TryLoadWallSpriteTextureV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, out string source)
        {
            source = string.Empty;
            if (desc == null)
                return null;

            // V3 fix:
            // V2 allowed Border_frames/Resources fallback. That can silently bind W48MOST1
            // to main-menu border textures. WALLS.g16 itself does not contain menu frames,
            // so the correct path is strict:
            //   sprite name/index from walls.lst -> WALLS.g16 frame through Melinoja.
            Texture2D melinojaTex = TryLoadWallSpriteViaMelinojaV1LikeOriginal(desc.SpriteIndex, out source);
            if (melinojaTex != null)
            {
                source = "STRICT_WALLS_G16:" + desc.Name + "#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " " + source;
                return melinojaTex;
            }

            // Optional exact pre-extracted cache only. No Border_frames, no generic Resources,
            // no menu fallback.
            string frame = $"frame_{desc.SpriteIndex:0000}";
            string[] resourcePaths =
            {
                "WallObjects/WALLS_frames/" + frame,
                "WALLS_frames/" + frame
            };

            for (int i = 0; i < resourcePaths.Length; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(resourcePaths[i]);
                if (tex != null)
                {
                    source = "STRICT_WALLS_RESOURCES:" + desc.Name + "#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " Resources:" + resourcePaths[i];
                    return tex;
                }
            }

            if (string.IsNullOrWhiteSpace(source))
                source = "missing strict WALLS.g16 frame: " + desc.Name + "#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture);
            return null;
        }

        private static readonly HashSet<string> C2WallObjectsLoadedG16V2LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static Texture2D TryLoadWallSpriteViaMelinojaV1LikeOriginal(int frameIndex, out string source)
        {
            source = string.Empty;
            try
            {
                string abs = FindWallG16PathV2LikeOriginal();
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "WALLS.g16 not found. Expected Assets/Resources/WALLS.g16";
                    return null;
                }

                Type bridgeType = ResolveMelinojaBridgeTypeV2LikeOriginal();
                if (bridgeType == null)
                {
                    source = "Melinoja bridge type not found: TemnyLessCodec.MelinojaCodecBridge";
                    return null;
                }

                // Melinoja runtime requires the G16 to be loaded before frames are requested.
                if (!C2WallObjectsLoadedG16V2LikeOriginal.Contains(abs))
                {
                    MethodInfo load = bridgeType.GetMethod("LoadG16ToMemory", BindingFlags.Public | BindingFlags.Static);
                    if (load != null)
                    {
                        object[] loadArgs = { abs, null, false };
                        object loadResult = load.Invoke(null, loadArgs);
                        bool loaded = loadResult is bool b && b;
                        string loadErr = loadArgs.Length > 1 ? loadArgs[1] as string : string.Empty;
                        if (!loaded)
                        {
                            source = "Melinoja LoadG16ToMemory failed: " + (loadErr ?? string.Empty);
                            return null;
                        }
                    }

                    C2WallObjectsLoadedG16V2LikeOriginal.Add(abs);
                }

                MethodInfo mi = bridgeType.GetMethod("TryGetG16FrameRGBA", BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                {
                    source = "Melinoja TryGetG16FrameRGBA not found";
                    return null;
                }

                object[] args = { abs, frameIndex, 0, 0, null, null };
                object result = mi.Invoke(null, args);
                if (!(result is bool ok) || !ok)
                {
                    string err = args.Length > 5 ? args[5] as string : string.Empty;
                    source = "Melinoja TryGetG16FrameRGBA failed: " + (err ?? string.Empty);
                    return null;
                }

                int w = args[2] is int iw ? iw : 0;
                int h = args[3] is int ih ? ih : 0;
                byte[] rgba = args[4] as byte[];
                if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                {
                    source = $"Melinoja returned invalid frame {frameIndex}: {w}x{h} bytes={(rgba == null ? 0 : rgba.Length)}";
                    return null;
                }

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tex.name = $"WALLS_g16_frame_{frameIndex:0000}";
                tex.LoadRawTextureData(rgba);
                tex.Apply(false, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                source = "Melinoja:" + abs + "#" + frameIndex.ToString(CultureInfo.InvariantCulture);
                return tex;
            }
            catch (Exception ex)
            {
                source = "Melinoja failed: " + ex.Message;
                return null;
            }
        }

        private static string FindWallG16PathV2LikeOriginal()
        {
            string[] candidates =
            {
                Path.Combine(Application.dataPath, "Resources", "WALLS.g16"),
                Path.Combine(Application.dataPath, "Resources", "WallObjects", "WALLS.g16"),
                Path.Combine(Application.dataPath, "Resources", "Cash", "WALLS.g16"),
                @"C:\GSC Game World\Cossacks II\Data\Cash\WALLS.g16",
                @"C:\GSC Game World\Cossacks II\Data\WALLS.g16"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string p = candidates[i];
                if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                    return p;
            }

            return string.Empty;
        }

        private static Type ResolveMelinojaBridgeTypeV2LikeOriginal()
        {
            Type bridgeType = Type.GetType("TemnyLessCodec.MelinojaCodecBridge, TemnyLessCodec.Runtime", false)
                              ?? Type.GetType("TemnyLessCodec.MelinojaCodecBridge, Assembly-CSharp", false);
            if (bridgeType != null)
                return bridgeType;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                bridgeType = asm.GetType("TemnyLessCodec.MelinojaCodecBridge", false);
                if (bridgeType != null)
                    return bridgeType;
            }

            return null;
        }

        private static int GetDir256V1LikeOriginal(float dx, float dy)
        {
            if (Mathf.Abs(dx) < 0.001f && Mathf.Abs(dy) < 0.001f)
                return 0;
            float a = Mathf.Atan2(dy, dx);
            int v = Mathf.RoundToInt(a * 128.0f / Mathf.PI);
            return v & 255;
        }

        private static string[] SplitLinesV1LikeOriginal(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static string StripWallCommentV1LikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line))
                return string.Empty;
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("/", StringComparison.Ordinal))
                return string.Empty;
            int slash = line.IndexOf("//", StringComparison.Ordinal);
            return slash >= 0 ? line.Substring(0, slash) : line;
        }

        private static string[] SplitTokensV1LikeOriginal(string line)
        {
            return Regex.Split((line ?? string.Empty).Trim(), @"\s+");
        }

        private static int ParseIntV1LikeOriginal(string s)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                return Mathf.RoundToInt(f);
            return 0;
        }

        private static float ParseFloatV1LikeOriginal(string s)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;
            return 0.0f;
        }

        // V168 compile repair: neutral helpers kept only for non-fence diagnostics / non-WALS2D routes.
        // The old individual WALS2D fence routes remain absent from active routing.
        private const string C2WallObjectsV116FenceContractLikeOriginal = "V172_RESTORED_original_saved_WL_individual_OneSprite_path";
        private const string C2WallObjectsV117FenceContractLikeOriginal = "V172_RESTORED_original_saved_WL_individual_OneSprite_path";

        private static bool IsFiniteWallFloatV21LikeOriginal(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }

        private static bool IsFiniteWallM4V21LikeOriginal(Matrix4x4 m)
        {
            return IsFiniteWallFloatV21LikeOriginal(m.m00) && IsFiniteWallFloatV21LikeOriginal(m.m01) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m02) && IsFiniteWallFloatV21LikeOriginal(m.m03) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m10) && IsFiniteWallFloatV21LikeOriginal(m.m11) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m12) && IsFiniteWallFloatV21LikeOriginal(m.m13) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m20) && IsFiniteWallFloatV21LikeOriginal(m.m21) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m22) && IsFiniteWallFloatV21LikeOriginal(m.m23) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m30) && IsFiniteWallFloatV21LikeOriginal(m.m31) &&
                   IsFiniteWallFloatV21LikeOriginal(m.m32) && IsFiniteWallFloatV21LikeOriginal(m.m33);
        }

        private static bool HasOriginalSavedWallAligningV172LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return false;

            // Original LoadSprites2 uses OS->OC->HaveAligning, and if it is true the saved Matrix4D
            // read from 2ERT/TRE2 is deleted. In this port the catalog evidence for HaveAligning is
            // the parsed [ALIGNING] mode/points from walls.lst.
            char a = desc.AlignMode;
            return a == 'V' || a == 'S' || a == 'H' || a == 'U' || desc.AlignPoints.Count > 0;
        }

        private static bool UseSavedMatrixAfterLoadSprites2V172LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc)
        {
            if (s == null || !s.HasMatrix)
                return false;
            if (!IsFiniteWallM4V21LikeOriginal(s.Matrix))
                return false;

            // Original:
            // if(M4){
            //     if(OS->OC->HaveAligning) delete(M4);
            //     else OS->M4=M4;
            // }
            return !HasOriginalSavedWallAligningV172LikeOriginal(desc);
        }

        private static string FormatWallFloatV118LikeOriginal(float v)
        {
            if (!IsFiniteWallFloatV21LikeOriginal(v))
                return "nan";
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void IncrementWallIdCountV118LikeOriginal(Dictionary<int, int> ids, WallSpriteDescV1LikeOriginal desc)
        {
            if (ids == null || desc == null)
                return;
            int id = desc.SpriteIndex;
            ids.TryGetValue(id, out int count);
            ids[id] = count + 1;
        }

        private WallSavedWLRouteDecisionV20LikeOriginal SelectSavedWallRouteV20LikeOriginal(WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc)
        {
            var route = new WallSavedWLRouteDecisionV20LikeOriginal();
            route.Route = WallDrawRouteV20LikeOriginal.SavedAlignedSprite;
            route.Profile = WallSavedWLProfileV18LikeOriginal.BillboardFallback;
            route.ClassV118 = WallWL2DClassV118LikeOriginal.UnknownFallback;

            bool hasAligningV172 = HasOriginalSavedWallAligningV172LikeOriginal(desc);
            bool useSavedM4AfterLoadSprites2V172 = UseSavedMatrixAfterLoadSprites2V172LikeOriginal(s, desc);
            string loadSprites2RuleV172 = hasAligningV172
                ? "V172_LoadSprites2_HaveAligning_true_saved_M4_deleted_CreateMatrix_path"
                : (useSavedM4AfterLoadSprites2V172
                    ? "V172_LoadSprites2_HaveAligning_false_saved_M4_kept_DrawWSprite_path"
                    : "V172_LoadSprites2_no_saved_M4_AddWorldPoint_or_GetMatrix4D_path");

            route.Path = loadSprites2RuleV172;
            route.Reason =
                "V172 original saved-WL rule: sign=='WL' -> addSpriteAnyway(&WALLS); " +
                "if saved M4 exists and OC->HaveAligning then ignore/delete saved M4, else keep saved M4; " +
                "render via DrawWSprite when OS.M4 exists, otherwise AddWorldPoint/CreateMatrix.";

            if (desc != null && !string.IsNullOrWhiteSpace(desc.ModelPath))
            {
                route.Route = WallDrawRouteV20LikeOriginal.SavedModelC2M;
                route.Profile = WallSavedWLProfileV18LikeOriginal.ModelBackedC2M;
                route.ClassV118 = WallWL2DClassV118LikeOriginal.ModelBackedC2M;
                route.UseSavedM4 = useSavedM4AfterLoadSprites2V172;
                route.MatrixVerified = route.UseSavedM4;
                route.Path = useSavedM4AfterLoadSprites2V172
                    ? "V172_ModelBacked_LoadSprites2_no_HaveAligning_use_saved_M4_RenderModels"
                    : "V172_ModelBacked_LoadSprites2_HaveAligning_or_no_M4_use_GetMatrix4D";
                route.MatrixAudit = hasAligningV172
                    ? "V172_ignored_saved_M4_because_OC_HaveAligning_matches_LoadSprites2"
                    : (route.UseSavedM4 ? "V172_saved_M4_kept_because_no_HaveAligning" : "V172_no_saved_M4_after_LoadSprites2");
                return route;
            }

            char a = desc != null ? desc.AlignMode : '\0';
            if (a == 'H')
            {
                route.Profile = WallSavedWLProfileV18LikeOriginal.GroundAligned;
                route.ClassV118 = WallWL2DClassV118LikeOriginal.GroundAligned;
                route.Path = "V172_GroundAligned_LoadSprites2_HaveAligning_delete_saved_M4_CreateMatrix_atGround";
            }
            else if (a == 'V' || a == 'S' || a == 'U')
            {
                route.Profile = WallSavedWLProfileV18LikeOriginal.VerticalAligned;
                route.ClassV118 = WallWL2DClassV118LikeOriginal.VerticalAligned;
                route.Path = "V172_VerticalOrUniversal_LoadSprites2_HaveAligning_delete_saved_M4_CreateMatrix";
            }
            else
            {
                route.Profile = WallSavedWLProfileV18LikeOriginal.BillboardFallback;
                route.ClassV118 = WallWL2DClassV118LikeOriginal.Single2DProp;
                route.Path = loadSprites2RuleV172;
            }

            route.UseSavedM4 = useSavedM4AfterLoadSprites2V172;
            route.MatrixVerified = route.UseSavedM4;
            route.MatrixAudit = hasAligningV172
                ? "V172_ignored_saved_M4_because_OC_HaveAligning_matches_LoadSprites2"
                : (route.UseSavedM4 ? "V172_saved_M4_kept_because_no_HaveAligning" : "V172_no_saved_M4_after_LoadSprites2");
            return route;
        }

        private static void CountWallSavedRouteV20LikeOriginal(
            WallDrawRouteV20LikeOriginal route,
            ref int routeBridge,
            ref int routeFence,
            ref int routeLargeFence,
            ref int routeAligned,
            ref int routeModel,
            ref int routeFallback)
        {
            switch (route)
            {
                case WallDrawRouteV20LikeOriginal.SavedModelC2M:
                    routeModel++;
                    break;
                case WallDrawRouteV20LikeOriginal.SavedAlignedSprite:
                    routeAligned++;
                    break;
                default:
                    routeFallback++;
                    break;
            }
        }

        private static void CountWallSavedProfileV20LikeOriginal(
            WallSavedWLProfileV18LikeOriginal profile,
            ref int profileBridge,
            ref int profileFence,
            ref int profileModel,
            ref int profileGround,
            ref int profileVertical,
            ref int profileFallback)
        {
            switch (profile)
            {
                case WallSavedWLProfileV18LikeOriginal.ModelBackedC2M:
                    profileModel++;
                    break;
                case WallSavedWLProfileV18LikeOriginal.GroundAligned:
                    profileGround++;
                    break;
                case WallSavedWLProfileV18LikeOriginal.VerticalAligned:
                    profileVertical++;
                    break;
                default:
                    profileFallback++;
                    break;
            }
        }

        private string BuildWallWL2DAuditLineV118LikeOriginal(
            int order,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            Texture2D tex,
            Mesh mesh,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            string reason)
        {
            string name = desc != null ? desc.Name : "null";
            string id = desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-";
            string r = route != null ? route.Route.ToString() : "-";
            string cls = route != null ? route.ClassV118.ToString() : "-";
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + id +
                   " name=" + name +
                   " route=" + r +
                   " class=" + cls +
                   " path=V172_original_saved_WL_per_OneSprite_route" +
                   " xy=(" + (s != null ? s.X.ToString(CultureInfo.InvariantCulture) : "-") + "," +
                              (s != null ? s.Y.ToString(CultureInfo.InvariantCulture) : "-") + ")" +
                   " reason='" + (reason ?? string.Empty) + "'";
        }

        private string BuildWallMatrixAuditLineV21LikeOriginal(int order, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            Matrix4x4 m = s != null ? s.Matrix : Matrix4x4.identity;
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + (desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-") +
                   " name=" + (desc != null ? desc.Name : "null") +
                   " m4ok=" + (s != null && s.HasMatrix && IsFiniteWallM4V21LikeOriginal(m)).ToString(CultureInfo.InvariantCulture) +
                   " tr=(" + FormatWallFloatV118LikeOriginal(m.m03) + "," + FormatWallFloatV118LikeOriginal(m.m13) + "," + FormatWallFloatV118LikeOriginal(m.m23) + ")" +
                   " route=" + (route != null ? route.Route.ToString() : "-");
        }

        private string BuildWallModelAuditLineV22LikeOriginal(int order, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + (desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-") +
                   " name=" + (desc != null ? desc.Name : "null") +
                   " model=" + (desc != null ? (desc.ModelPath ?? string.Empty) : string.Empty) +
                   " route=" + (route != null ? route.Route.ToString() : "-") +
                   " note=V168_non_fence_model_audit";
        }

        private string BuildWallIMMRouteAuditLineV24LikeOriginal(int order, WallSavedMapSpriteV6LikeOriginal s, WallSpriteDescV1LikeOriginal desc, WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + (desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-") +
                   " route=" + (route != null ? route.Route.ToString() : "-") +
                   " imm=V168_neutral";
        }

        private string BuildWallRouteAuditLineV20LikeOriginal(
            int order,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            Texture2D tex,
            Mesh mesh)
        {
            return "order=" + order.ToString(CultureInfo.InvariantCulture) +
                   " id=" + (desc != null ? desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) : "-") +
                   " name=" + (desc != null ? desc.Name : "null") +
                   " route=" + (route != null ? route.Route.ToString() : "-") +
                   " profile=" + (route != null ? route.Profile.ToString() : "-") +
                   " path=" + (route != null ? route.Path : "-") +
                   " emitted=" + (mesh != null).ToString(CultureInfo.InvariantCulture) +
                   " tex=" + (tex != null ? tex.name : "null") +
                   " note=V172_original_saved_WL_per_OneSprite_restored";
        }

        private Mesh BuildSavedMapWallSpriteRouteMeshV20LikeOriginal(
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            Texture2D tex,
            WallSavedWLRouteDecisionV20LikeOriginal route)
        {
            float wPx = Mathf.Max(1.0f, tex != null ? tex.width : (desc != null ? desc.Width : 64));
            float hPx = Mathf.Max(1.0f, tex != null ? tex.height : (desc != null ? desc.Height : 64));
            if (route != null && route.UseSavedM4 && s != null && s.HasMatrix)
                return BuildSavedMapWallSpriteSavedM4MeshV21LikeOriginal(s, desc, wPx, hPx, route.FlipLocalY, route.Path);
            return BuildSavedMapWallSpriteAlignedNoEmbedV20LikeOriginal(s, desc, wPx, hPx);
        }

        private WallWL2DPlacementMetricV119LikeOriginal BuildWallWL2DPlacementMetricV119LikeOriginal(
            int order,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            Mesh mesh,
            Texture2D tex)
        {
            var m = new WallWL2DPlacementMetricV119LikeOriginal();
            m.Order = order;
            m.SpriteIndex = desc != null ? desc.SpriteIndex : -1;
            m.Name = desc != null ? desc.Name : string.Empty;
            m.X = s != null ? s.X : 0;
            m.Y = s != null ? s.Y : 0;
            m.Route = route != null ? route.Route : WallDrawRouteV20LikeOriginal.DebugFallback;
            m.Profile = route != null ? route.Profile : WallSavedWLProfileV18LikeOriginal.BillboardFallback;
            m.ClassV118 = route != null ? route.ClassV118 : WallWL2DClassV118LikeOriginal.UnknownFallback;
            m.HasMatrix = s != null && s.HasMatrix;
            m.UseMatrix = route != null && route.UseSavedM4;
            m.Path = route != null ? route.Path : string.Empty;
            m.TextureSource = tex != null ? tex.name : string.Empty;
            if (mesh != null)
            {
                Bounds b = mesh.bounds;
                m.BoundsDiagonal = b.size.magnitude;
                m.BoundsHeight = b.size.y;
                m.BoundsWidthXZ = Mathf.Sqrt(b.size.x * b.size.x + b.size.z * b.size.z);
                m.BoundsCenterY = b.center.y;
            }
            return m;
        }

        private int AddWallDambaSceneOnlyAnchorObjectsV84LikeOriginal(
            Transform parent,
            List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog)
        {
            return 0;
        }

        private string BuildWallWL2DIdSummaryV118LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog, Dictionary<int, int> ids)
        {
            if (ids == null || ids.Count == 0)
                return "0";
            var parts = new List<string>();
            foreach (var kv in ids)
            {
                string name = catalog != null && catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal d) && d != null ? d.Name : ("W" + kv.Key.ToString(CultureInfo.InvariantCulture));
                parts.Add(name + ":" + kv.Value.ToString(CultureInfo.InvariantCulture));
            }
            return string.Join(",", parts.ToArray());
        }

        private void LogWallWL2DTopOffendersV119LikeOriginal(List<WallWL2DPlacementMetricV119LikeOriginal> metrics)
        {
            if (metrics == null || metrics.Count == 0)
                return;
            Debug.Log("[C2:WALL WL2D TOP V119] disabled_by_V168 old individual WALS2D fence routes deleted; metrics=" + metrics.Count.ToString(CultureInfo.InvariantCulture));
        }

        private string BuildWallUsedIdCoverageLineV27LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog, Dictionary<int, int> wlIndexAudit)
        {
            return BuildWallWL2DIdSummaryV118LikeOriginal(catalog, wlIndexAudit);
        }

        private string BuildWallModelCoverageLineV27LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog, Dictionary<int, int> wlIndexAudit)
        {
            if (catalog == null || wlIndexAudit == null || wlIndexAudit.Count == 0)
                return "models=0";
            int modelCount = 0;
            foreach (var kv in wlIndexAudit)
            {
                if (catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal d) && d != null && !string.IsNullOrWhiteSpace(d.ModelPath))
                    modelCount += kv.Value;
            }
            return "models=" + modelCount.ToString(CultureInfo.InvariantCulture);
        }

        private string BuildWallUsedModelNameListV27LikeOriginal(WallSpriteCatalogV1LikeOriginal catalog, Dictionary<int, int> wlIndexAudit)
        {
            if (catalog == null || wlIndexAudit == null || wlIndexAudit.Count == 0)
                return "none";
            var names = new List<string>();
            foreach (var kv in wlIndexAudit)
            {
                if (catalog.ByIndex.TryGetValue(kv.Key, out WallSpriteDescV1LikeOriginal d) && d != null && !string.IsNullOrWhiteSpace(d.ModelPath))
                    names.Add(d.ModelPath);
            }
            return names.Count == 0 ? "none" : string.Join(",", names.ToArray());
        }

        private string ResolveWallC2MModelPathV161LikeOriginal(string modelPath, out string audit)
        {
            audit = string.Empty;
            string raw = (modelPath ?? string.Empty).Replace('/', '\\').Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(raw))
            {
                audit = "empty_model_path";
                return string.Empty;
            }

            if (_bootstrap == null || _bootstrap.Fs == null)
            {
                audit = "fs_not_ready";
                return raw;
            }

            var candidates = new List<string>();
            Action<string> add = c =>
            {
                c = (c ?? string.Empty).Replace('/', '\\').Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(c))
                    return;
                if (!candidates.Exists(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase)))
                    candidates.Add(c);
            };

            add(raw);
            if (!raw.EndsWith(".c2m", StringComparison.OrdinalIgnoreCase))
                add(raw + ".c2m");
            if (!raw.StartsWith("Models\\", StringComparison.OrdinalIgnoreCase))
            {
                add("Models\\" + raw);
                if (!raw.EndsWith(".c2m", StringComparison.OrdinalIgnoreCase))
                    add("Models\\" + raw + ".c2m");
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (_bootstrap.Fs.Exists(candidates[i]))
                {
                    audit = "resolved=" + candidates[i];
                    return candidates[i];
                }
            }

            audit = "not_found candidates=" + string.Join(",", candidates.ToArray());
            return raw;
        }

        // V169 compile compatibility wrappers after physical deletion of old WALS2D fence routes.
        // They keep non-fence audit/model code compiling while the old individual WALS2D fence builders remain physically removed.
        private string BuildWallWL2DAuditLineV118LikeOriginal(
            int order,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSavedMapSpriteV6LikeOriginal sourceSpriteForLog,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            object meshOrUnused,
            string reason)
        {
            return BuildWallWL2DAuditLineV118LikeOriginal(
                order,
                s,
                desc,
                null,
                meshOrUnused as Mesh,
                route,
                reason);
        }

        private string BuildWallRouteAuditLineV20LikeOriginal(
            int order,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSavedMapSpriteV6LikeOriginal sourceSpriteForLog,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            object meshOrSource)
        {
            return BuildWallRouteAuditLineV20LikeOriginal(
                order,
                s,
                desc,
                route,
                null,
                meshOrSource as Mesh);
        }

        private WallWL2DPlacementMetricV119LikeOriginal BuildWallWL2DPlacementMetricV119LikeOriginal(
            int order,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            Mesh mesh,
            string source)
        {
            return BuildWallWL2DPlacementMetricV119LikeOriginal(
                order,
                s,
                desc,
                route,
                mesh,
                (Texture2D)null);
        }

        private int AddWallDambaSceneOnlyAnchorObjectsV84LikeOriginal(
            GameObject go,
            WallSavedMapSpriteV6LikeOriginal s,
            WallSpriteDescV1LikeOriginal desc,
            WallSavedWLRouteDecisionV20LikeOriginal route,
            WallC2MParsedMeshV23LikeOriginal c2m,
            int order,
            List<string> audit)
        {
            return 0;
        }


    }
}
