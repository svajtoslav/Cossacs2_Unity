using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2SpriteDepthPrepassLikeOriginal
    {
        internal const int DepthRenderQueue = 3650;
        private static readonly CameraEvent[] ClearDepthEvents =
        {
            CameraEvent.AfterForwardOpaque,
            CameraEvent.BeforeForwardAlpha
        };

        private static bool s_installerCreated;
        private static readonly System.Collections.Generic.HashSet<EntityId> s_loggedCameraIds =
            new System.Collections.Generic.HashSet<EntityId>();

        internal static Material CreateDepthMaterialLikeOriginal(string name, Texture2D texture, float cutoff)
        {
            EnsureCameraDepthClearInstalledLikeOriginal();

            Shader shader = Shader.Find("Cossacks2Bridge/C2SpriteDepthCutoutV1LikeOriginal");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent Cutout");
            if (shader == null)
                return null;

            Material mat = new Material(shader);
            mat.name = string.IsNullOrEmpty(name) ? "C2SpriteDepthCutoutV1LikeOriginal" : name;
            ConfigureDepthMaterialLikeOriginal(mat, texture, cutoff);
            return mat;
        }

        internal static void ConfigureDepthMaterialLikeOriginal(Material mat, Texture2D texture, float cutoff)
        {
            if (mat == null)
                return;

            Texture2D main = texture != null ? texture : Texture2D.whiteTexture;
            mat.mainTexture = main;
            mat.renderQueue = DepthRenderQueue;
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", main);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", main);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", cutoff);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_OffsetFactor")) mat.SetFloat("_OffsetFactor", 0.0f);
            if (mat.HasProperty("_OffsetUnits")) mat.SetFloat("_OffsetUnits", 0.0f);
        }

        internal static MeshRenderer AddDepthRendererLikeOriginal(
            GameObject colorObject,
            Mesh mesh,
            Material depthMaterial,
            int sortingOrder,
            string childName)
        {
            EnsureCameraDepthClearInstalledLikeOriginal();

            if (colorObject == null || mesh == null || depthMaterial == null)
                return null;

            GameObject depthObject = new GameObject(string.IsNullOrEmpty(childName) ? "depth_cutout_prepass" : childName);
            depthObject.layer = colorObject.layer;
            depthObject.transform.SetParent(colorObject.transform, false);
            depthObject.transform.localPosition = Vector3.zero;
            depthObject.transform.localRotation = Quaternion.identity;
            depthObject.transform.localScale = Vector3.one;

            MeshFilter mf = depthObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = depthObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = depthMaterial;
            mr.sortingOrder = sortingOrder;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return mr;
        }

        internal static void ApplyTextureLikeOriginal(MeshRenderer renderer, Texture2D texture, float cutoff)
        {
            if (renderer == null || renderer.sharedMaterial == null)
                return;

            ConfigureDepthMaterialLikeOriginal(renderer.sharedMaterial, texture, cutoff);
        }

        private static void EnsureCameraDepthClearInstalledLikeOriginal()
        {
            if (C2SpriteDepthLayerLikeOriginal.UseSeparateSpriteDepthCamera)
                return;

            if (!s_installerCreated)
            {
                s_installerCreated = true;
                GameObject go = new GameObject("C2_SpriteDepthPrepassInstaller_V1");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<C2SpriteDepthPrepassInstallerLikeOriginal>();
            }

            C2SpriteDepthPrepassInstallerLikeOriginal.InstallOnCurrentCamerasLikeOriginal();
        }

        private sealed class C2SpriteDepthPrepassInstallerLikeOriginal : MonoBehaviour
        {
            private float _nextScanTime;

            internal static void InstallOnCurrentCamerasLikeOriginal()
            {
                Camera[] cameras = Camera.allCameras;
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera cam = cameras[i];
                    if (!IsBattleSpriteCameraLikeOriginal(cam))
                        continue;

                    C2SpriteDepthPrepassCameraClearLikeOriginal clear = cam.GetComponent<C2SpriteDepthPrepassCameraClearLikeOriginal>();
                    if (clear == null)
                        clear = cam.gameObject.AddComponent<C2SpriteDepthPrepassCameraClearLikeOriginal>();
                    clear.EnsureInstalledLikeOriginal();
                }
            }

            private void Update()
            {
                if (Time.unscaledTime < _nextScanTime)
                    return;

                _nextScanTime = Time.unscaledTime + 0.5f;
                InstallOnCurrentCamerasLikeOriginal();
            }

            private static bool IsBattleSpriteCameraLikeOriginal(Camera cam)
            {
                if (cam == null || !cam.isActiveAndEnabled)
                    return false;

                string n = cam.name ?? string.Empty;
                if (n.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("HUD", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Preview", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;

                return n.IndexOf("C2_BattleTerrainCamera_Iso", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("BattleTerrainCamera", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("Battle", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("Terrain", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("Iso", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       cam == Camera.main;
            }
        }

        private sealed class C2SpriteDepthPrepassCameraClearLikeOriginal : MonoBehaviour
        {
            private Camera _camera;
            private CommandBuffer _commandBuffer;

            internal void EnsureInstalledLikeOriginal()
            {
                Camera cam = GetComponent<Camera>();
                if (cam == null)
                    return;

                if (_camera == cam && _commandBuffer != null)
                    return;

                RemoveLikeOriginal();
                _camera = cam;
                _commandBuffer = new CommandBuffer { name = "C2 Sprite Depth Clear Before Alpha" };
                _commandBuffer.ClearRenderTarget(true, false, Color.clear);
                for (int i = 0; i < ClearDepthEvents.Length; i++)
                    _camera.AddCommandBuffer(ClearDepthEvents[i], _commandBuffer);

                EntityId id = _camera.GetEntityId();
                if (!s_loggedCameraIds.Contains(id))
                {
                    s_loggedCameraIds.Add(id);
                    Debug.Log("[C2:SPRITE DEPTH CLEAR V270] installed camera='" + _camera.name +
                              "' events=AfterForwardOpaque+BeforeForwardAlpha queue=" +
                              DepthRenderQueue.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                              " contract=separate_sprite_depth_before_units_buildings");
                }
            }

            private void OnEnable()
            {
                EnsureInstalledLikeOriginal();
            }

            private void OnDisable()
            {
                RemoveLikeOriginal();
            }

            private void OnDestroy()
            {
                RemoveLikeOriginal();
            }

            private void RemoveLikeOriginal()
            {
                if (_camera != null && _commandBuffer != null)
                {
                    for (int i = 0; i < ClearDepthEvents.Length; i++)
                        _camera.RemoveCommandBuffer(ClearDepthEvents[i], _commandBuffer);
                }

                if (_commandBuffer != null)
                    _commandBuffer.Release();

                _commandBuffer = null;
                _camera = null;
            }
        }
    }
}
