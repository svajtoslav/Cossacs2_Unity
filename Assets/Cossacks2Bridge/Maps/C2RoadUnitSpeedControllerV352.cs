using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SetUnitsSpeed.
    // RoadSpeed=(64+32)=96, MaxSpdLimit=240, minimum=32.
    internal sealed class C2RoadUnitSpeedControllerV352 : MonoBehaviour
    {
        private static readonly Dictionary<int, List<C2RoadUnitSpeedControllerV352>> Groups =
            new Dictionary<int, List<C2RoadUnitSpeedControllerV352>>();
        private static int _lastProcessedFrame = -1;

        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private int _groupId = -1;

        internal static void AttachLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int groupId)
        {
            if (unit == null) return;
            C2RoadUnitSpeedControllerV352 c = unit.GetComponent<C2RoadUnitSpeedControllerV352>();
            GameObject unitProxy = c == null ? unit.EnsureUnityProxyLikeOriginal() : null;
            if (c == null && unitProxy != null) c = unitProxy.AddComponent<C2RoadUnitSpeedControllerV352>();
            if (c == null) return;
            c.Bind(unit, groupId);
            c.enabled = true;
            C2UnitOriginalRuntime rt = c.Runtime;
            if (rt != null) rt.OriginalGoOnRoadLikeOriginal = true;
        }

        internal static void DetachLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            C2RoadUnitSpeedControllerV352 c = unit.GetComponent<C2RoadUnitSpeedControllerV352>();
            if (c == null) return;
            C2UnitOriginalRuntime rt = c.Runtime;
            if (rt != null)
            {
                rt.OriginalUnitSpeedLikeOriginal = 64;
                rt.OriginalGoOnRoadLikeOriginal = false;
            }
            c.enabled = false;
        }

        internal static bool IsGoOnRoadLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return false;
            C2RoadUnitSpeedControllerV352 c = unit.GetComponent<C2RoadUnitSpeedControllerV352>();
            return c != null && c.enabled && c._groupId >= 0;
        }

        private void Bind(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int groupId)
        {
            if (_groupId != groupId) RemoveFromGroup();
            _unit = unit;
            _groupId = groupId;
            List<C2RoadUnitSpeedControllerV352> list;
            if (!Groups.TryGetValue(groupId, out list))
            {
                list = new List<C2RoadUnitSpeedControllerV352>(64);
                Groups[groupId] = list;
            }
            if (!list.Contains(this)) list.Add(this);
        }

        private void Update()
        {
            if (_groupId < 0) return;
            if (_lastProcessedFrame == Time.frameCount) return;
            _lastProcessedFrame = Time.frameCount;
            ProcessAllGroupsLikeOriginal();
        }

        private static void ProcessAllGroupsLikeOriginal()
        {
            foreach (KeyValuePair<int, List<C2RoadUnitSpeedControllerV352>> pair in Groups)
            {
                List<C2RoadUnitSpeedControllerV352> list = pair.Value;
                if (list == null || list.Count == 0) continue;

                long sum = 0;
                int n = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    C2RoadUnitSpeedControllerV352 c = list[i];
                    C2UnitOriginalRuntime rt = c != null ? c.Runtime : null;
                    if (c == null || rt == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                    if (!rt.HasMoveTargetLikeOriginal)
                    {
                        rt.OriginalUnitSpeedLikeOriginal = 64;
                        continue;
                    }
                    int ds = DistanceToDestPixelsLikeOriginal(rt);
                    if (ds < 200)
                    {
                        sum += ds;
                        n++;
                    }
                }

                if (n == 0) continue;
                int avrDst = (int)(sum / n);
                for (int i = 0; i < list.Count; i++)
                {
                    C2RoadUnitSpeedControllerV352 c = list[i];
                    C2UnitOriginalRuntime rt = c != null ? c.Runtime : null;
                    if (rt == null || !rt.HasMoveTargetLikeOriginal) continue;
                    int ds = DistanceToDestPixelsLikeOriginal(rt) + 1;
                    int spd = (96 * ds) / (avrDst + 1);
                    if (spd < 32) spd = 32;
                    if (spd > 240) spd = 240;
                    rt.OriginalUnitSpeedLikeOriginal = spd;
                }
            }
        }

        private static int DistanceToDestPixelsLikeOriginal(C2UnitOriginalRuntime rt)
        {
            int dx = Mathf.RoundToInt((rt.RuntimeRealXLikeOriginal - rt.MoveTargetRealXLikeOriginal) / 16.0f);
            int dy = Mathf.RoundToInt((rt.RuntimeRealYLikeOriginal - rt.MoveTargetRealYLikeOriginal) / 16.0f);
            return C2OriginalMovementMathV352.Norma(dx, dy);
        }

        private C2UnitOriginalRuntime Runtime
        {
            get
            {
                if (_unit == null)
                    _unit = C2NeutralPeasantUnitInfoV2LikeOriginal.C2FindForGameObjectV365LikeOriginal(gameObject);
                C2UnitOriginalRuntimeLinkLikeOriginal link =
                    _unit != null ? _unit.RuntimeLinkCachedLikeOriginal : null;
                return link != null ? link.Runtime : null;
            }
        }

        private void OnDisable()
        {
            C2UnitOriginalRuntime rt = Runtime;
            if (rt != null)
            {
                rt.OriginalUnitSpeedLikeOriginal = 64;
                rt.OriginalGoOnRoadLikeOriginal = false;
            }
            RemoveFromGroup();
        }

        private void OnDestroy()
        {
            RemoveFromGroup();
        }

        private void RemoveFromGroup()
        {
            if (_groupId < 0) return;
            List<C2RoadUnitSpeedControllerV352> list;
            if (Groups.TryGetValue(_groupId, out list) && list != null)
            {
                list.Remove(this);
                if (list.Count == 0) Groups.Remove(_groupId);
            }
            _groupId = -1;
        }
    }
}
