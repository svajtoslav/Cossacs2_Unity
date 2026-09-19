using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2MoraleWorldUiV404LikeOriginal : MonoBehaviour
    {
        private static C2MoraleWorldUiV404LikeOriginal _instance;
        private readonly List<C2MoraleRuntimeV404LikeOriginal.WorldUiSnapshot> _snapshots =
            new List<C2MoraleRuntimeV404LikeOriginal.WorldUiSnapshot>(32);
        private C2BattleTerrainMode _mode;
        private Camera _camera;
        private GUIStyle _deltaStyle;
        private GUIStyle _deltaShadowStyle;
        private static readonly Sprite[] _originalMoraleFrameV404C = new Sprite[5];
        private static bool _originalMoraleFrameAuditLoggedV404C;

        internal static void EnsureInstalledV404LikeOriginal()
        {
            if (_instance != null) return;
            C2MoraleWorldUiV404LikeOriginal existing =
                UnityEngine.Object.FindObjectOfType<C2MoraleWorldUiV404LikeOriginal>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject go = new GameObject("C2_MoraleWorldUi_V404A");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<C2MoraleWorldUiV404LikeOriginal>();
        }

        private void Awake()
        {
            _instance = this;
        }

        private void BuildStylesV404LikeOriginal()
        {
            _deltaStyle = new GUIStyle(GUI.skin.label);
            _deltaStyle.alignment = TextAnchor.MiddleCenter;
            _deltaStyle.fontSize = 12;
            _deltaStyle.fontStyle = FontStyle.Bold;
            _deltaStyle.normal.textColor = new Color32(0xFD, 0xE8, 0x1F, 0xFF);

            _deltaShadowStyle = new GUIStyle(_deltaStyle);
            _deltaShadowStyle.normal.textColor = new Color(0.08f, 0.04f, 0.0f, 0.90f);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            C2MoraleRuntimeV404LikeOriginal.FillWorldUiSnapshotsV404LikeOriginal(_snapshots);
            if (_snapshots.Count == 0) return;

            ResolveCameraV404ALikeOriginal();
            if (_camera == null || _mode == null) return;
            if (_deltaStyle == null) BuildStylesV404LikeOriginal();

            // Lower IMGUI depth is drawn on top. This must sit above the camera-stacked HUD.
            GUI.depth = -32000;

            for (int i = 0; i < _snapshots.Count; i++)
            {
                C2MoraleRuntimeV404LikeOriginal.WorldUiSnapshot s = _snapshots[i];

                C2NeutralPeasantUnitInfoV2LikeOriginal rep;
                float ox, oy;
                if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationCenterForUiV404LikeOriginal(
                        s.GroupId, out rep, out ox, out oy) || rep == null || rep.Nation == 7)
                    continue;

                Vector3 world = _mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(ox, oy);
                Vector3 screen = _camera.WorldToScreenPoint(world);
                if (screen.z <= 0.0f) continue;

                float sx = screen.x;
                // Retail DrawSomethingOverBrigade uses brigade center at H+80.
                // The Unity bridge projects the ground center, so keep the equivalent
                // screen-space lift that already matches the unit sprite scale.
                float sy = Screen.height - screen.y - 54.0f;

                if (s.Alpha > 0.0f)
                    DrawMoraleDeskV404ALikeOriginal(sx - 75.0f, sy, s.DisplayMorale, s.MaxMorale, s.Alpha);

                if (s.DeltaAlpha > 0.0f && !string.IsNullOrEmpty(s.DeltaText))
                {
                    Rect r = new Rect(sx - 55.0f, sy - 19.0f, 110.0f, 18.0f);
                    Color old = GUI.color;
                    GUI.color = new Color(1, 1, 1, s.DeltaAlpha);
                    GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s.DeltaText, _deltaShadowStyle);
                    GUI.Label(r, s.DeltaText, _deltaStyle);
                    GUI.color = old;
                }
            }
        }

        private void ResolveCameraV404ALikeOriginal()
        {
            if (_mode == null)
                _mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();

            if (IsUsableBattleCameraV404ALikeOriginal(_camera))
                return;

            _camera = null;
            Camera[] cams = Camera.allCameras;
            Camera best = null;
            int bestScore = int.MinValue;

            for (int i = 0; cams != null && i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (!IsUsableBattleCameraV404ALikeOriginal(c)) continue;

                string n = c.name ?? string.Empty;
                int score = Mathf.RoundToInt(c.depth * 100.0f);
                score += Mathf.RoundToInt(c.pixelRect.width * c.pixelRect.height / 100000.0f);

                if (n.IndexOf("C2_BattleTerrainCamera_Free", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 60;
                if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 50;
                else if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 40;
                else if (n.IndexOf("C2_Battle", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 30;
                else if (n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 20;

                if (best == null || score > bestScore)
                {
                    best = c;
                    bestScore = score;
                }
            }

            _camera = best;
        }

        private static bool IsUsableBattleCameraV404ALikeOriginal(Camera c)
        {
            if (c == null || !c.isActiveAndEnabled || !c.gameObject.activeInHierarchy) return false;
            if (c.targetTexture != null) return false;

            string n = c.name ?? string.Empty;
            if (n.IndexOf("GameplayHud_OverlayCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 &&
                n.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("C2_BattleTerrainCamera_Free", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("C2_Battle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawMoraleDeskV404ALikeOriginal(
            float x, float y, float moraleF, float maxMoraleF, float alpha)
        {
            // Exact retail composition:
            // BRIG_BONUS_L -> VitButton Back, GP_File=Interf3\morale_line, SpritePassive0=6.
            // VitButton::_Draw does NOT stretch sprite 6. It calls
            // DrawHeaderEx2(x,y,150,file,6,7,8,9,10,Nation):
            // 6 = left cap, 7 = right cap, 8/9/10 = repeating centre.
            // Canvas is exactly (17,5), 116x6.
            Color old = GUI.color;
            EnsureOriginalMoraleFrameV404C();
            GUI.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha));
            DrawHeaderEx2LikeOriginalV404C(x, y, 150.0f, _originalMoraleFrameV404C);

            DrawMoraleCanvasV404ALikeOriginal(x + 17.0f, y + 5.0f, 116.0f, 6.0f,
                moraleF, maxMoraleF, alpha);

            GUI.color = old;
        }

        private static void EnsureOriginalMoraleFrameV404C()
        {
            for (int i = 0; i < 5; i++)
            {
                if (_originalMoraleFrameV404C[i] == null)
                    _originalMoraleFrameV404C[i] =
                        C2GameplayOriginalSpriteCacheV1.LoadSprite(
                            "Interf3\\morale_line", 6 + i,
                            "world_morale_frame_v404c_" + (6 + i).ToString());
            }

            if (_originalMoraleFrameAuditLoggedV404C) return;
            _originalMoraleFrameAuditLoggedV404C = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            sb.Append("[C2:MORALE V404C FRAME] exact=DrawHeaderEx2 sprites=6,7,8,9,10 ");
            for (int i = 0; i < 5; i++)
            {
                if (i != 0) sb.Append(" | ");
                sb.Append(6 + i).Append(":")
                  .Append(C2GameplayOriginalSpriteCacheV1.GetSourceAudit(
                      "Interf3\\morale_line", 6 + i));
            }
            Debug.Log(sb.ToString());
        }

        private static float NativeSpriteWidthV404C(Sprite sp)
        {
            if (sp == null) return 0.0f;
            if (sp.rect.width > 0.0f) return sp.rect.width;
            return sp.texture != null ? sp.texture.width : 0.0f;
        }

        private static float NativeSpriteHeightV404C(Sprite sp)
        {
            if (sp == null) return 0.0f;
            if (sp.rect.height > 0.0f) return sp.rect.height;
            return sp.texture != null ? sp.texture.height : 0.0f;
        }

        private static void DrawNativeSpriteClippedV404C(
            Sprite sp, float x, float y, float clipX0, float clipX1)
        {
            if (sp == null || sp.texture == null) return;
            float sw = NativeSpriteWidthV404C(sp);
            float sh = NativeSpriteHeightV404C(sp);
            if (sw <= 0.0f || sh <= 0.0f) return;

            float dx0 = Mathf.Max(x, clipX0);
            float dx1 = Mathf.Min(x + sw, clipX1);
            if (dx1 <= dx0) return;

            Rect tr = sp.textureRect;
            Texture2D tex = sp.texture;
            float src01a = (dx0 - x) / sw;
            float src01b = (dx1 - x) / sw;
            Rect uv = new Rect(
                (tr.x + tr.width * src01a) / tex.width,
                tr.y / tex.height,
                tr.width * (src01b - src01a) / tex.width,
                tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(
                new Rect(dx0, y, dx1 - dx0, sh), tex, uv, true);
        }

        private static void DrawHeaderEx2LikeOriginalV404C(
            float x0, float y0, float width, Sprite[] frames)
        {
            if (frames == null || frames.Length < 5) return;
            Sprite left = frames[0];
            Sprite right = frames[1];
            float leftW = NativeSpriteWidthV404C(left);
            float rightW = NativeSpriteWidthV404C(right);
            if (leftW > 2048.0f) leftW = 0.0f;
            if (rightW > 2048.0f) rightW = 0.0f;

            // DrawHeaderEx2 clips the repeating centre between the two end caps.
            float clip0 = x0 + leftW;
            float clip1 = x0 + width - rightW;
            float i = 0.0f;
            int guard = 0;
            while (i < width && guard++ < 300)
            {
                // Retail switches by integer i%3, not by cycle index.
                int imod = ((Mathf.RoundToInt(i) % 3) + 3) % 3;
                Sprite center = frames[2 + imod];
                float cw = NativeSpriteWidthV404C(center);
                if (cw <= 0.0f) break;
                DrawNativeSpriteClippedV404C(center, x0 + i, y0, clip0, clip1);
                i += cw;
            }

            DrawNativeSpriteClippedV404C(left, x0, y0, -1000000.0f, 1000000.0f);
            DrawNativeSpriteClippedV404C(
                right, x0 + width - rightW, y0,
                -1000000.0f, 1000000.0f);
        }

        private static void DrawMoraleCanvasV404ALikeOriginal(
            float x, float y, float w, float h, float moraleF, float maxMoraleF, float alpha)
        {
            int morale = Mathf.Max(0, Mathf.FloorToInt(moraleF));
            int maxMorale = Mathf.Max(1, Mathf.FloorToInt(maxMoraleF));
            int n = morale / 100;
            int m = morale % 100;
            int M = Mathf.Clamp(maxMorale - n * 100, 0, 100);
            if (n + M <= 0 || n >= 10) return;

            float lx = m * w / 100.0f;
            float lmax = M * w / 100.0f;
            float lr = n == 0 ? Mathf.Min(m, 32) * w / 100.0f : 0.0f;

            Color old = GUI.color;
            // Retail SetMorale pulses below 45. Preserve that behaviour, while
            // deliberately making the missing/max segment much darker so the loss
            // is obvious at a glance.
            float pulse = morale < 45
                ? Mathf.Clamp01(0.78f + 0.22f * (0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 5.0f)))
                : 1.0f;

            if (lr > 0.0f)
            {
                Color c = new Color32(0xFF, 0x25, 0x18, 0xFF);
                c.a *= alpha * pulse;
                GUI.color = c;
                GUI.DrawTexture(new Rect(x, y, lr, h), Texture2D.whiteTexture);
            }

            if (lx > lr)
            {
                Color c = new Color32(0xFF, 0xD4, 0x12, 0xFF);
                c.a *= alpha * pulse;
                GUI.color = c;
                GUI.DrawTexture(new Rect(x + lr, y, lx - lr, h), Texture2D.whiteTexture);
            }

            if (lmax > lx)
            {
                Color c = new Color32(0x6E, 0x45, 0x08, 0xFF);
                c.a *= alpha;
                GUI.color = c;
                GUI.DrawTexture(new Rect(x + lx, y, lmax - lx, h), Texture2D.whiteTexture);
            }

            if (n > 0)
            {
                float tick = Mathf.Max(1.0f, h - 1.0f);
                float start = (w - (n + n - 1) * tick) * 0.5f;
                Color c = new Color32(0xAF, 0x00, 0x00, 0xFF);
                c.a *= alpha;
                GUI.color = c;
                for (int i = 0; i < n; i++)
                    GUI.DrawTexture(new Rect(x + start + i * 2.0f * tick, y, tick, h),
                        Texture2D.whiteTexture);
            }

            GUI.color = old;
        }
    }
}
