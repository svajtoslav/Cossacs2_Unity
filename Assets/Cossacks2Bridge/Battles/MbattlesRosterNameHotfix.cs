
using TMPro;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Battles
{
    /// <summary>
    /// Runtime-only hotfix for the current-player name in MBattles roster.
    /// It avoids vertical wrapping when the chosen profile name is longer than one digit.
    /// This is intentionally isolated from the main renderer so it can be applied over v8 safely.
    /// </summary>
    internal sealed class MbattlesRosterNameHotfix : MonoBehaviour
    {
        private bool _logged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("MbattlesRosterNameHotfix");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<MbattlesRosterNameHotfix>();
        }

        private void LateUpdate()
        {
            if (!IsBattlesScreenVisible())
                return;

            var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allTexts == null || allTexts.Length == 0)
                return;

            string playerName = string.IsNullOrWhiteSpace(global::MenuActionSink.CurrentProfileName)
                ? "1"
                : global::MenuActionSink.CurrentProfileName.Trim();

            TextMeshProUGUI best = null;
            float bestScore = float.MaxValue;

            foreach (var tmp in allTexts)
            {
                if (tmp == null) continue;
                var rt = tmp.rectTransform;
                if (rt == null) continue;

                // Top-left roster name cell in MBattles.
                // Original XML: x=78 y=199 w=61 h=23.
                // We search by local anchored position, then widen the visual box for single-line rendering.
                float ax = rt.anchoredPosition.x;
                float ay = rt.anchoredPosition.y;

                if (ax < 60f || ax > 120f) continue;
                if (ay > -180f || ay < -245f) continue;

                var size = rt.sizeDelta;
                if (size.y < 16f || size.y > 40f) continue;

                float score = Mathf.Abs(ax - 78f) + Mathf.Abs(ay + 199f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = tmp;
                }
            }

            if (best == null)
                return;

            var bestRt = best.rectTransform;

            best.enableWordWrapping = false;
            best.textWrappingMode = TextWrappingModes.NoWrap;
            best.overflowMode = TextOverflowModes.Ellipsis;
            best.alignment = TextAlignmentOptions.Left;
            best.fontSize = 14f;
            best.text = playerName;

            // Make the visual field wider so names like "Влад" or "Вася" stay on one line,
            // while keeping the same top-left anchor as the original slot.
            bestRt.sizeDelta = new Vector2(118f, 23f);

            if (!_logged)
            {
                Debug.Log("[MBattlesNameFix] Applied single-line name hotfix: '" + playerName + "'");
                _logged = true;
            }
        }

        private static bool IsBattlesScreenVisible()
        {
            var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allTexts == null) return false;

            for (int i = 0; i < allTexts.Length; i++)
            {
                var t = allTexts[i];
                if (t == null) continue;
                if (t.text != null && t.text.Contains("СРАЖЕНИЯ И БАТАЛИИ"))
                    return true;
            }
            return false;
        }
    }
}
