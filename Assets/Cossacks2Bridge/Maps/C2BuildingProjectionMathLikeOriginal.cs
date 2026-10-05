using System;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        // COSSACKS2 Scape3D::GetPseudoProjectionTM. Work in camera space before
        // dividing by W; never clamp eye depth to Zn and then differentiate it.
        // A point behind the camera has no valid perspective sprite projection.
        internal struct C2BuildingProjectionCameraLikeOriginal
        {
            internal Vector3 Origin, AxisX, AxisY, AxisZ;
            internal Matrix4x4 Projection;
            internal Rect Viewport;
            internal bool Orthographic;

            internal bool TryProject(Vector3 point, out Vector3 screen)
            {
                double x = Origin.x + (double)point.x * AxisX.x + (double)point.y * AxisY.x + (double)point.z * AxisZ.x;
                double y = Origin.y + (double)point.x * AxisX.y + (double)point.y * AxisY.y + (double)point.z * AxisZ.y;
                double z = Origin.z + (double)point.x * AxisX.z + (double)point.y * AxisY.z + (double)point.z * AxisZ.z;
                double w = Projection.m30 * x + Projection.m31 * y + Projection.m32 * z + Projection.m33;
                screen = default;
                if (z >= -0.0001 || w <= 0.0001 || double.IsNaN(w)) return false;
                double sx = Projection.m00 * x + Projection.m01 * y + Projection.m02 * z + Projection.m03;
                double sy = Projection.m10 * x + Projection.m11 * y + Projection.m12 * z + Projection.m13;
                screen = new Vector3((float)(Viewport.x + (sx / w + 1.0) * Viewport.width * 0.5),
                    (float)(Viewport.y + (1.0 - sy / w) * Viewport.height * 0.5), (float)(Orthographic ? -z : -1.0 / z));
                return Finite(screen);
            }

            internal bool TryScreenBasis(out C2BuildingMatrix4LikeOriginal screen)
            {
                screen = C2BuildingMatrix4LikeOriginal.Identity();
                if (!TryProject(Vector3.zero, out Vector3 origin) ||
                    !TryDelta(AxisX, out Vector3 x) || !TryDelta(AxisY, out Vector3 y) || !TryDelta(AxisZ, out Vector3 z)) return false;
                screen.e00 = x.x; screen.e01 = x.y; screen.e02 = x.z;
                screen.e10 = y.x; screen.e11 = y.y; screen.e12 = y.z;
                screen.e20 = z.x; screen.e21 = z.y; screen.e22 = z.z;
                screen.e30 = origin.x; screen.e31 = origin.y; screen.e32 = origin.z;
                return true;
            }

            private bool TryDelta(Vector3 axis, out Vector3 delta)
            {
                // Difference of two projective samples, evaluated BEFORE rounding
                // to float. Subtracting large absolute screen positions loses the
                // basis on distant buildings and produces a singular 3-point fit.
                double x = Origin.x, y = Origin.y, z = Origin.z;
                double w = Projection.m30*x + Projection.m31*y + Projection.m32*z + Projection.m33;
                double dw = Projection.m30*axis.x + Projection.m31*axis.y + Projection.m32*axis.z;
                delta = default;
                if (z >= -0.0001 || z + axis.z >= -0.0001 || w <= 0.0001 || w + dw <= 0.0001) return false;
                double cx = Projection.m00*x + Projection.m01*y + Projection.m02*z + Projection.m03;
                double cy = Projection.m10*x + Projection.m11*y + Projection.m12*z + Projection.m13;
                double dx = Projection.m00*axis.x + Projection.m01*axis.y + Projection.m02*axis.z;
                double dy = Projection.m10*axis.x + Projection.m11*axis.y + Projection.m12*axis.z;
                double denominator = w * (w + dw);
                delta = new Vector3((float)((dx*w - cx*dw) / denominator * Viewport.width * 0.5),
                    (float)(-(dy*w - cy*dw) / denominator * Viewport.height * 0.5),
                    (float)(Orthographic ? -axis.z : axis.z / (z * (z + axis.z))));
                return Finite(delta);
            }

            internal bool IntersectsFrustum(float radius)
            {
                // C2 visits visible ZBuffer objects before DrawSpriteBuilding.
                // The Unity registry also contains offscreen/behind-camera parts.
                // Test the conservative building sphere before perspective divide.
                for (int axis = 0; axis < 3; axis++)
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    double a = Projection[3,0] + sign * Projection[axis,0];
                    double b = Projection[3,1] + sign * Projection[axis,1];
                    double c = Projection[3,2] + sign * Projection[axis,2];
                    double d = Projection[3,3] + sign * Projection[axis,3];
                    if (a*Origin.x + b*Origin.y + c*Origin.z + d < -radius*Math.Sqrt(a*a+b*b+c*c)) return false;
                }
                return true;
            }

            internal float ToEyeDepth(float projectedDepth) => Orthographic ? projectedDepth : 1.0f / projectedDepth;

            private static bool Finite(Vector3 p) => IsFiniteV292LikeOriginal(p.x) &&
                IsFiniteV292LikeOriginal(p.y) && IsFiniteV292LikeOriginal(p.z);
        }

        private static C2BuildingProjectionCameraLikeOriginal BuildingProjectionCameraLikeOriginal(Camera cam, Vector3 rootWorld, float scale)
        {
            Matrix4x4 view = cam.worldToCameraMatrix;
            return new C2BuildingProjectionCameraLikeOriginal
            {
                Origin = view.MultiplyPoint3x4(rootWorld),
                AxisX = view.MultiplyVector(new Vector3(scale, 0, 0)),
                AxisY = view.MultiplyVector(new Vector3(0, 0, -scale)),
                AxisZ = view.MultiplyVector(new Vector3(0, scale, 0)),
                Projection = cam.projectionMatrix, Viewport = cam.pixelRect, Orthographic = cam.orthographic
            };
        }

        private static float BuildingProjectionRadiusLikeOriginal(C2BuildingMdInfoLikeOriginal md, float scale)
        {
            float x = Mathf.Max(Mathf.Abs(md.PicDx), Mathf.Abs(md.PicDx + md.PicLx));
            float y = 2 * Mathf.Max(Mathf.Abs(md.PicDy), Mathf.Abs(md.PicDy + md.PicLy));
            return scale * (Mathf.Sqrt(x*x + y*y) + 128);
        }

        private static readonly System.Collections.Generic.HashSet<string> s_buildingProjectionFailures =
            new System.Collections.Generic.HashSet<string>();
        private static void LogBuildingProjectionFailureLikeOriginal(string reason, Camera cam, Transform root,
            C2BuildingMdInfoLikeOriginal md, int frame, Vector3 input, Vector3 screen)
        {
            string key = root.GetEntityId().ToString() + ":" + reason;
            if (!s_buildingProjectionFailures.Add(key)) return;
            var info = root.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();
            Debug.LogWarning("[C2:BUILDING PROJECTION] reason=" + reason + " building='" + root.name +
                "' md='" + md.MdName + "' record=" + (info != null ? info.RecordIndex.ToString() : "preview") +
                " frame=" + frame + " unityFrame=" + Time.frameCount + " root=" + root.position.ToString("F4") + " PicDxDy=(" + md.PicDx + "," + md.PicDy +
                ") PicLxLy=(" + md.PicLx + "," + md.PicLy + ") align1=(" + md.AlignPt1x + "," + md.AlignPt1y + "," + md.AlignPt1z +
                ") align2=(" + md.AlignPt2x + "," + md.AlignPt2y + "," + md.AlignPt2z + ") align3=(" + md.AlignPt3x + "," + md.AlignPt3y + "," + md.AlignPt3z +
                ") input=" + input.ToString("G9") + " screen=" + screen.ToString("G9") + " camera='" + cam.name +
                "' pos=" + cam.transform.position.ToString("F4") + " rotation=" + cam.transform.eulerAngles.ToString("F4") +
                " near=" + cam.nearClipPlane + " far=" + cam.farClipPlane + " rect=" + cam.pixelRect + "; first occurrence for this building/reason");
        }
    }
}
