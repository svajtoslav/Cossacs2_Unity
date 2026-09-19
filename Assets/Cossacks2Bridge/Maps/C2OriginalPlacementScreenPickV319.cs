using System;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const float C2OriginalPlacementCos30V319 = 0.8660254037844386f;
        // V322: residual found by V320/V321 diagnostic for the original camera-pick chain.
        // It is applied to the mouse/terrain pick only; BORN/CONC/BUILD/CHECK/LOCK and create-chain stay raw.
        private const float C2OriginalPlacementResidualXPxV322 = 11.0f;
        private const float C2OriginalPlacementResidualYPxV322 = 160.0f;

        internal bool C2TryGameplayGroundScreenPickLikeOriginal(
            Vector2 unityMouseBottomLeft,
            out Vector3 world,
            out float originalX,
            out float originalY,
            out string audit)
        {
            if (_freeCameraMode)
            {
                world = Vector3.zero;
                originalX = 0.0f;
                originalY = 0.0f;
                audit = "free_camera_uses_unity_plane_fallback";
                return false;
            }
            return C2TryOriginalScreenPickV346LikeOriginal(
                unityMouseBottomLeft, false, out world, out originalX, out originalY, out audit);
        }

        public bool C2TryOriginalPlacementScreenPickV319LikeOriginal(
            Vector2 unityMouseBottomLeft,
            out Vector3 world,
            out float originalX,
            out float originalY,
            out string audit)
        {
            return C2TryOriginalScreenPickV346LikeOriginal(
                unityMouseBottomLeft, true, out world, out originalX, out originalY, out audit);
        }

        private bool C2TryOriginalScreenPickV346LikeOriginal(
            Vector2 unityMouseBottomLeft,
            bool applyPlacementResidualLikeOriginal,
            out Vector3 world,
            out float originalX,
            out float originalY,
            out string audit)
        {
            world = Vector3.zero;
            originalX = 0.0f;
            originalY = 0.0f;
            audit = "original_camera_pick_v322_not_started";

            if (_map == null)
            {
                audit = "original_camera_pick_v322_no_map";
                return false;
            }

            Camera cam = GetActiveBattleCameraLikeOriginal();
            if (cam == null)
            {
                audit = "original_camera_pick_v322_no_camera";
                return false;
            }

            // V356: the strict Unity gameplay camera intentionally renders the battle
            // orthographically (C2BattleTerrainMode.ApplyStrictIsoCameraTransform).
            // A mouse target must therefore be obtained by inverting THAT rendered
            // projection first.  Feeding the same screen point into C2's perspective
            // GetPickRay math produces a valid map point, but not the point visible
            // under the Unity cursor; the error grows with distance from screen center
            // and makes units stop several body lengths before/after the cursor.
            //
            // The optional comparison mode must use its rendered ray for placement
            // as well as movement, so ghosts and MD exits share the terrain origin.
            // Original perspective placement continues through the C2 path below.
            if (cam.orthographic && (!applyPlacementResidualLikeOriginal ||
                C2ProjectionComparisonModeLikeOriginal.UseStableProjectionForComparison))
            {
                return C2TryRenderedOrthographicGameplayGroundPickV356(
                    unityMouseBottomLeft, cam, out world, out originalX, out originalY, out audit);
            }

            Rect vp = cam.pixelRect;
            float w = Mathf.Max(1.0f, vp.width);
            float h = Mathf.Max(1.0f, vp.height);

            // Original engine mouse coordinates are Win32/DirectX screen coordinates: origin at top-left.
            // Unity input is bottom-left. V318 applied ScreenToProjectionSpace to bottom-left Y directly,
            // which double-inverted the ray. Here we first convert to original screen Y, then apply the exact
            // BaseCamera::ScreenToProjectionSpace formula from sgCamera.inl.
            float sxPixel = unityMouseBottomLeft.x;
            float syUnityBottom = unityMouseBottomLeft.y;
            float syOriginalTop = vp.y + (h - (syUnityBottom - vp.y));

            float px = sxPixel;
            float py = syOriginalTop;
            float projX = px;
            float projY = py;
            projX -= vp.x;
            projX /= w * 0.5f;
            projX -= 1.0f;
            projY -= vp.y;
            projY /= h * 0.5f;
            projY = 1.0f - projY;

            float scale = GetStrictScaleLikeOriginal();
            float viewVol = GetStrictViewVolLikeOriginal(scale);
            float realLy = GetStrictRealLyLikeOriginal(scale);
            ClampStrictIsoMapStateLikeOriginal(viewVol, realLy, scale);

            float yaw = _strictYawLikeOriginal;
            float roll = _strictRollLikeOriginal;
            float fovX = Mathf.Clamp(StrictIsoBaseFovXDegrees, 1.0f, 179.0f) * Mathf.Deg2Rad;
            float aspect = Mathf.Max(0.0001f, w / h);

            Vector3 posO = new Vector3(
                _strictMapX * 32.0f + viewVol * 0.5f,
                _strictMapY * 32.0f + realLy,
                0.0f);

            Vector3 dirO = new Vector3(0.0f, -Mathf.Cos(yaw), -Mathf.Sin(yaw));
            if (Mathf.Abs(roll) > 0.000001f)
            {
                float cr = Mathf.Cos(roll);
                float sr = Mathf.Sin(roll);
                float rx = dirO.x * cr - dirO.y * sr;
                float ry = dirO.x * sr + dirO.y * cr;
                dirO.x = rx;
                dirO.y = ry;
            }
            dirO.Normalize();

            float ydist = realLy * scale * C2OriginalPlacementCos30V319 * 2.0f;
            float cameraDistance = viewVol * StrictIsoCameraFactor + _strictZoom;
            float camZn = cameraDistance - ydist - 4.0f * 256.0f / C2OriginalPlacementCos30V319;
            if (camZn < 50.0f) camZn = 50.0f;
            float camZf = cameraDistance + ydist * 2.0f + ydist * 10.0f;
            if (camZn < 10.0f) camZn = 10.0f;
            if (camZf <= camZn + 1.0f) camZf = camZn + 1.0f;

            posO += -dirO * cameraDistance;

            Vector3 originalUp = Vector3.forward;
            Vector3 rightO = Vector3.Cross(originalUp, dirO);
            if (rightO.sqrMagnitude < 0.0000001f)
                rightO = Vector3.right;
            rightO.Normalize();
            Vector3 upO = Vector3.Cross(dirO, rightO);
            if (upO.sqrMagnitude < 0.0000001f)
                upO = originalUp;
            upO.Normalize();

            float tanX = Mathf.Tan(fovX * 0.5f);
            float tanY = tanX / aspect;
            // V355 SOURCE FIX: BaseCamera::GetPickRay takes screen-space projection X/Y,
            // applies inverse ViewProj and therefore positive projection X/Y remain
            // positive camera Right/Up.  The old V321 bridge used -Right/-Up and
            // mirrored the gameplay pick around the screen center:
            // right -> left and top -> bottom.  This is the exact reason a click
            // in the upper-right could produce a destination in the lower-left.
            Vector3 rayDirO = dirO + rightO * (projX * tanX) + upO * (projY * tanY);
            if (rayDirO.sqrMagnitude < 0.0000001f)
            {
                audit = "original_camera_pick_v322_zero_ray";
                return false;
            }
            rayDirO.Normalize();

            float terrainZ = 0.0f;
            Vector3 hitO = posO;
            int iterations = 0;
            bool hitOk = false;
            for (int iter = 0; iter < 8; iter++)
            {
                iterations = iter + 1;
                if (Mathf.Abs(rayDirO.z) < 0.000001f)
                    break;

                float t = (terrainZ - posO.z) / rayDirO.z;
                if (t < 0.0f)
                    break;

                hitO = posO + rayDirO * t;
                if (float.IsNaN(hitO.x) || float.IsNaN(hitO.y) || float.IsInfinity(hitO.x) || float.IsInfinity(hitO.y))
                    break;

                float hOriginal = SampleWallHeightOriginalXYV1LikeOriginal(hitO.x, hitO.y);
                OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel ? _lastBuiltTerrainKernel : CreateOriginalTerrainKernelConfigLikeOriginal(_map);
                float nextTerrainZ = hOriginal * kernel.HeightScale;
                hitOk = true;
                if (Mathf.Abs(nextTerrainZ - terrainZ) <= 0.01f)
                {
                    terrainZ = nextTerrainZ;
                    break;
                }
                terrainZ = nextTerrainZ;
            }

            if (!hitOk)
            {
                audit = "original_camera_pick_v322_no_intersection" +
                        " mouseBL=" + unityMouseBottomLeft.x.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                      unityMouseBottomLeft.y.ToString("0.###", CultureInfo.InvariantCulture) +
                        " mouseTopY=" + syOriginalTop.ToString("0.###", CultureInfo.InvariantCulture) +
                        " proj=" + projX.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                                   projY.ToString("0.######", CultureInfo.InvariantCulture) +
                        " rayDirZ=" + rayDirO.z.ToString("0.######", CultureInfo.InvariantCulture);
                return false;
            }

            float rawOriginalX = hitO.x;
            float rawOriginalY = hitO.y;

            // V322: do not touch service-point math. Only compensate the remaining pick residual exposed by V321:
            // EngKaz confirm was corner 820/455 while the original target row needs 821/465.
            // In engine pixels that is +11 px X and +160 px Y before conversion to RealX/RealY.
            if (applyPlacementResidualLikeOriginal)
            {
                hitO.x += C2OriginalPlacementResidualXPxV322;
                hitO.y += C2OriginalPlacementResidualYPxV322;
            }

            originalX = hitO.x;
            originalY = hitO.y;
            world = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(originalX, originalY);

            audit =
                "original_camera_pick_v322" +
                " contract=Scape3D_SetupCamera_BaseCamera_GetPickRay_inverseViewProj_signs_V355_PLUS_RIGHT_PLUS_UP_ITerraPick" +
                " input=unity_bottom_left_to_original_top_left matrixSigns=plusRight_plusUp_V355 residualPickPxV322=" +
                (applyPlacementResidualLikeOriginal ? "11/160" : "disabled_for_gameplay") +
                " unityMouseBL=" + unityMouseBottomLeft.x.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                    unityMouseBottomLeft.y.ToString("0.###", CultureInfo.InvariantCulture) +
                " originalMouse=" + px.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                    py.ToString("0.###", CultureInfo.InvariantCulture) +
                " viewport=" + vp.x.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                              vp.y.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                              w.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                              h.ToString("0.###", CultureInfo.InvariantCulture) +
                " projXY=" + projX.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                             projY.ToString("0.######", CultureInfo.InvariantCulture) +
                " strictMap=" + _strictMapX.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                               _strictMapY.ToString("0.###", CultureInfo.InvariantCulture) +
                " viewVolRealLy=" + viewVol.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                    realLy.ToString("0.###", CultureInfo.InvariantCulture) +
                " yawRoll=" + yaw.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                              roll.ToString("0.######", CultureInfo.InvariantCulture) +
                " fovX=" + StrictIsoBaseFovXDegrees.ToString("0.###", CultureInfo.InvariantCulture) +
                " aspect=" + aspect.ToString("0.######", CultureInfo.InvariantCulture) +
                " dist=" + cameraDistance.ToString("0.###", CultureInfo.InvariantCulture) +
                " znzf=" + camZn.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                            camZf.ToString("0.###", CultureInfo.InvariantCulture) +
                " camO=" + posO.x.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                            posO.y.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                            posO.z.ToString("0.###", CultureInfo.InvariantCulture) +
                " dirO=" + dirO.x.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                            dirO.y.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                            dirO.z.ToString("0.######", CultureInfo.InvariantCulture) +
                " rayO=" + rayDirO.x.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                            rayDirO.y.ToString("0.######", CultureInfo.InvariantCulture) + "/" +
                            rayDirO.z.ToString("0.######", CultureInfo.InvariantCulture) +
                " hitOriginalRawV322=" + rawOriginalX.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                        rawOriginalY.ToString("0.###", CultureInfo.InvariantCulture) +
                " residualAppliedPxV322=" + C2OriginalPlacementResidualXPxV322.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                           C2OriginalPlacementResidualYPxV322.ToString("0.###", CultureInfo.InvariantCulture) +
                " hitOriginal=" + originalX.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                  originalY.ToString("0.###", CultureInfo.InvariantCulture) +
                " terrainZ=" + terrainZ.ToString("0.###", CultureInfo.InvariantCulture) +
                " iter=" + iterations.ToString(CultureInfo.InvariantCulture) +
                " camUnity='" + cam.name + "'" +
                " camUnityOrtho=" + cam.orthographic;

            return true;
        }

        private bool C2TryRenderedOrthographicGameplayGroundPickV356(
            Vector2 unityMouseBottomLeft,
            Camera cam,
            out Vector3 world,
            out float originalX,
            out float originalY,
            out string audit)
        {
            world = Vector3.zero;
            originalX = 0.0f;
            originalY = 0.0f;
            audit = "gameplay_ground_pick_v356_not_started";

            if (cam == null || _map == null)
            {
                audit = "gameplay_ground_pick_v356_missing_camera_or_map";
                return false;
            }

            Ray ray = cam.ScreenPointToRay(new Vector3(
                unityMouseBottomLeft.x, unityMouseBottomLeft.y, 0.0f));
            if (Mathf.Abs(ray.direction.y) < 0.000001f)
            {
                audit = "gameplay_ground_pick_v356_parallel_ray";
                return false;
            }

            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);

            float terrainWorldY = _terrainBuilt ? _terrainBounds.center.y : 0.0f;
            Vector3 hitWorld = Vector3.zero;
            int iterations = 0;
            bool haveHit = false;

            for (int iter = 0; iter < 8; iter++)
            {
                iterations = iter + 1;
                float t = (terrainWorldY - ray.origin.y) / ray.direction.y;
                if (t < 0.0f || float.IsNaN(t) || float.IsInfinity(t))
                    break;

                hitWorld = ray.GetPoint(t);
                if (!C2NoUnitWorldToOriginalPixelLikeOriginal(hitWorld, out originalX, out originalY))
                    break;

                float nextTerrainWorldY =
                    SampleWallHeightOriginalXYV1LikeOriginal(originalX, originalY) * kernel.HeightScale +
                    C2WallObjectsV1YOffsetLikeOriginal;

                haveHit = true;
                if (Mathf.Abs(nextTerrainWorldY - terrainWorldY) <= 0.01f)
                {
                    terrainWorldY = nextTerrainWorldY;
                    break;
                }
                terrainWorldY = nextTerrainWorldY;
            }

            if (!haveHit)
            {
                audit = "gameplay_ground_pick_v356_no_intersection";
                return false;
            }

            // Rebuild the exact terrain point from the canonical C2 X/Y -> Unity bridge,
            // so the point used by movement and the point rendered under the cursor use
            // one and the same axis/sign/scale contract.
            world = WallOriginalXYToWorldV1LikeOriginal(originalX, originalY, 0.0f);
            audit =
                "gameplay_ground_pick_v356" +
                " contract=UnityOrthographicScreenPointToRay_to_C2XY_to_WallWorld" +
                " mouseBL=" + unityMouseBottomLeft.x.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                               unityMouseBottomLeft.y.ToString("0.###", CultureInfo.InvariantCulture) +
                " originalXY=" + originalX.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                                  originalY.ToString("0.###", CultureInfo.InvariantCulture) +
                " world=" + world.x.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                             world.y.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                             world.z.ToString("0.###", CultureInfo.InvariantCulture) +
                " iterations=" + iterations.ToString(CultureInfo.InvariantCulture);
            return true;
        }
    }
}
