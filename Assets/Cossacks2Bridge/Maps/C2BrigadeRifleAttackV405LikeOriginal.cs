using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V405 is a direct managed port of COSSACKS2/BrigadeOrders.cpp
    // BrigadeOrder_RifleAttack + Brigade.cpp::BrigadeRifleAttack.
    //
    // IMPORTANT: the HUD does not assign targets here. Multi.cpp::SetArmAttackState
    // only changes OneObject::RifleAttack. The brigade loop creates this order only
    // after at least one RifleAttack member has delay==0.
    public sealed class C2BrigadeRifleAttackV405LikeOriginal : MonoBehaviour
    {
        private const int BrigadeOrderRifleAttackIdLikeOriginal = 1;
        private const int MaxEnemyLikeOriginal = 240;
        private const int EnemySearchRadiusLikeOriginal = 1200;
        private const int RefillTicksLikeOriginal = 42 + 29 + 21;
        private const int OriginalTicksPerSecondLikeOriginal = 25;

        private sealed class EnemyEntryLikeOriginal
        {
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public int PredictedDamage;
            public int FireCount;
        }

        private sealed class OrderLikeOriginal
        {
            public int GroupId = -1;
            public C2NeutralPeasantUnitInfoV2LikeOriginal Representative;
            public bool FirstProcess;
            public long FillTimeAnimLikeOriginal;
            public int OnlyOneBrig = -1;
            public int EnemyCenterX;
            public int EnemyCenterY;
            public readonly List<EnemyEntryLikeOriginal> Enemies = new List<EnemyEntryLikeOriginal>(MaxEnemyLikeOriginal);
            public readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> FireOrder = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(256);
        }

        private sealed class MdRifleProfileLikeOriginal
        {
            public int SearchEnemyRadius = 0;
            public int SearchEnemyRadiusShiftPercent = 0;
            public bool ShotAlwaysOn;
            public int GoldPrice;
        }

        private static readonly Dictionary<int, C2NeutralPeasantUnitInfoV2LikeOriginal> ArmedGroupsLikeOriginal =
            new Dictionary<int, C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private static readonly Dictionary<int, OrderLikeOriginal> OrdersLikeOriginal =
            new Dictionary<int, OrderLikeOriginal>();
        private static readonly Dictionary<string, MdRifleProfileLikeOriginal> MdProfilesLikeOriginal =
            new Dictionary<string, MdRifleProfileLikeOriginal>(StringComparer.OrdinalIgnoreCase);

        private static C2BrigadeRifleAttackV405LikeOriginal _instance;
        private static bool _insideOrderDestructorLikeOriginal;
        private long _lastProcessTickLikeOriginal = -1;

        public static void OnRifleStateEnabledLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            if (representative == null) return;
            int groupId;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                    representative, out groupId))
                return;

            EnsureHostLikeOriginal();
            ArmedGroupsLikeOriginal[groupId] = representative;
            int readyV405, delayedV405;
            CountRifleReadinessLikeOriginal(representative, out readyV405, out delayedV405);
            Debug.Log("[C2:RIFLE V405 ARMED] group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                      " ready=" + readyV405.ToString(CultureInfo.InvariantCulture) +
                      " delayed=" + delayedV405.ToString(CultureInfo.InvariantCulture) +
                      " source=Multi.cpp::SetArmAttackState_129");
            TryStartFromBrigadeLoopLikeOriginal(groupId, representative, "SetArmAttackState_129");
        }

        // Explicit rifle-off / melee replacement. ClearFormationRifleAttackState
        // calls this before changing the flags so the active brigade order gets the
        // same destructor path as BR->DeleteNewBOrder().
        public static void OnFormationRifleStateClearedLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            string reason)
        {
            if (_insideOrderDestructorLikeOriginal || representative == null) return;
            int groupId;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                    representative, out groupId))
                return;
            ArmedGroupsLikeOriginal.Remove(groupId);
            DeleteOrderLikeOriginal(groupId, reason ?? "rifle_state_clear_v405", true);
        }

        // A replacing brigade order destroys BrigadeOrder_RifleAttack. Movement then
        // interrupts ATTACK3 separately via the existing runtime delay adapter.
        public static void OnExternalBrigadeOrderReplacementLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            string reason)
        {
            if (_insideOrderDestructorLikeOriginal || unit == null) return;
            int groupId;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                    unit, out groupId))
                return;
            ArmedGroupsLikeOriginal.Remove(groupId);
            DeleteOrderLikeOriginal(groupId, reason ?? "external_brigade_order_v405", true);
        }

        public static bool IsRifleOrderActiveLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return false;
            int groupId;
            return C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                       unit, out groupId) && OrdersLikeOriginal.ContainsKey(groupId);
        }

        private static void EnsureHostLikeOriginal()
        {
            if (_instance != null) return;
            GameObject go = GameObject.Find("C2_BrigadeOrder_RifleAttack_V405");
            if (go == null)
            {
                go = new GameObject("C2_BrigadeOrder_RifleAttack_V405");
                DontDestroyOnLoad(go);
            }
            _instance = go.GetComponent<C2BrigadeRifleAttackV405LikeOriginal>();
            if (_instance == null)
                _instance = go.AddComponent<C2BrigadeRifleAttackV405LikeOriginal>();
            Debug.Log("[C2:RIFLE V405] installed source=COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_RifleAttack" +
                      "+Brigade.cpp::BrigadeRifleAttack trigger=Bitva/KeepPositions_delay0" +
                      " FillTime=(42+29+21)*256 enemyRadius=1200 maxEnemy=240");
        }

        private void Update()
        {
            long tick = C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal;
            if (tick == _lastProcessTickLikeOriginal) return;
            _lastProcessTickLikeOriginal = tick;

            // Brigade::Bitva / BrigadeOrder_Bitva / KeepPositions: when the rifle
            // flag is armed but no rifle order exists, wait for delay==0, then create it.
            if (ArmedGroupsLikeOriginal.Count > 0)
            {
                int[] armed = new int[ArmedGroupsLikeOriginal.Count];
                ArmedGroupsLikeOriginal.Keys.CopyTo(armed, 0);
                for (int i = 0; i < armed.Length; i++)
                {
                    int groupId = armed[i];
                    C2NeutralPeasantUnitInfoV2LikeOriginal representative;
                    if (!ArmedGroupsLikeOriginal.TryGetValue(groupId, out representative)) continue;
                    if (!GroupHasAnyRifleFlagLikeOriginal(representative))
                    {
                        ArmedGroupsLikeOriginal.Remove(groupId);
                        continue;
                    }
                    if (!OrdersLikeOriginal.ContainsKey(groupId))
                        TryStartFromBrigadeLoopLikeOriginal(groupId, representative, "Brigade::Bitva_delay0");
                }
            }

            if (OrdersLikeOriginal.Count == 0) return;
            int[] ids = new int[OrdersLikeOriginal.Count];
            OrdersLikeOriginal.Keys.CopyTo(ids, 0);
            long animTime = tick * 256L;
            for (int i = 0; i < ids.Length; i++)
            {
                OrderLikeOriginal order;
                if (!OrdersLikeOriginal.TryGetValue(ids[i], out order) || order == null) continue;
                if (!ProcessLikeOriginal(order, animTime))
                    DeleteOrderLikeOriginal(order.GroupId, "ProcessPre_false", true);
            }
        }

        private static void TryStartFromBrigadeLoopLikeOriginal(
            int groupId,
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            string source)
        {
            if (representative == null || OrdersLikeOriginal.ContainsKey(groupId)) return;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
            int resolvedGroup;
            string shape;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    representative, out members, out resolvedGroup, out shape) ||
                members == null || resolvedGroup != groupId)
                return;

            C2NeutralPeasantUnitInfoV2LikeOriginal trigger = null;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                if (ob == null || ob.IsDeadLikeOriginal || !ob.isActiveAndEnabled) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                if (!ob.RifleAttackV396LikeOriginal) continue;
                int delay, maxDelay;
                C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(
                    ob, 1, out delay, out maxDelay);
                if (delay == 0)
                {
                    trigger = ob;
                    break;
                }
            }
            if (trigger == null) return;

            // Brigade.cpp::BrigadeRifleAttack creates only if the current NewBOrder
            // is absent or is not BRIGADEORDER_RIFLEATTACK. The V405 dictionary is
            // the managed NewBOrder slot for this order type.
            OrderLikeOriginal ra = new OrderLikeOriginal();
            ra.GroupId = groupId;
            ra.Representative = representative;
            ra.OnlyOneBrig = trigger.SearchOnlyThisBrigadeToKillV407LikeOriginal;
            OrdersLikeOriginal[groupId] = ra;

            Debug.Log("[C2:RIFLE V405 CREATE] group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                      " orderId=" + BrigadeOrderRifleAttackIdLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " trigger='" + (trigger.SourceMonsterId ?? string.Empty) + "'" +
                      " source='" + (source ?? string.Empty) + "'");

            long tick = C2FormationRuntimeV167LikeOriginal.CurrentSimulationTickV403ELikeOriginal;
            if (!ProcessLikeOriginal(ra, tick * 256L))
                DeleteOrderLikeOriginal(groupId, "Init_Process_false", true);
        }

        // BrigadeOrder_RifleAttack::Process.
        private static bool ProcessLikeOriginal(OrderLikeOriginal order, long animTime)
        {
            if (order == null || order.Representative == null) return false;

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
            int groupId;
            string shape;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    order.Representative, out members, out groupId, out shape) ||
                members == null || members.Count == 0 || groupId != order.GroupId)
                return false;

            if (!order.FirstProcess)
            {
                ClearTargetObjLikeOriginal(members, "BrigadeOrder_RifleAttack_first_process");
                ClearLastOrdersLikeOriginal(members);
                order.FirstProcess = true;
                order.FillTimeAnimLikeOriginal = 0;
                FillEnemyListLikeOriginal(order, members);
            }

            return ProcessPreLikeOriginal(order, members, animTime);
        }

        // BrigadeOrder_RifleAttack::ProcessPre, statement-for-statement structure.
        private static bool ProcessPreLikeOriginal(
            OrderLikeOriginal order,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members,
            long animTime)
        {
            bool rez = true;
            if (order.FillTimeAnimLikeOriginal < animTime)
            {
                bool rec = true;
                bool rtf = false;
                MdRifleProfileLikeOriginal mon = null;
                for (int i = 0; i < members.Count && rec; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                    if (ob == null || ob.IsDeadLikeOriginal) continue;
                    C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                    if (!ob.RifleAttackV396LikeOriginal) continue;

                    rtf = true;
                    if (mon == null && i > 3)
                        mon = LoadMdProfileLikeOriginal(ob);

                    int delay, maxDelay;
                    C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(
                        ob, 1, out delay, out maxDelay);
                    if (delay == 0)
                    {
                        C2CombatRuntimeV334LikeOriginal combat =
                            ob.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                        if (combat != null && combat.HasLiveRifleAttackObjV405LikeOriginal)
                            rec = false;
                    }
                }

                if (!rtf) return false;
                if (rec)
                {
                    FillEnemyListLikeOriginal(order, members);
                    order.FillTimeAnimLikeOriginal = animTime + RefillTicksLikeOriginal * 256L;
                    rez = false;
                    if (mon != null && mon.ShotAlwaysOn)
                        rez = true;
                }
            }

            int nEnemyLife = order.Enemies.Count;
            for (int i = 0; i < order.FireOrder.Count && nEnemyLife > 0; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = order.FireOrder[i];
                if (ob != null && !ob.IsDeadLikeOriginal)
                {
                    C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                    int delay, maxDelay;
                    C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(
                        ob, 1, out delay, out maxDelay);
                    if (ob.RifleAttackV396LikeOriginal && delay == 0)
                    {
                        C2CombatRuntimeV334LikeOriginal combat =
                            ob.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                        bool needsTarget = combat == null || !combat.HasLiveRifleAttackObjV405LikeOriginal;
                        if (needsTarget && SetTargetObjLikeOriginal(order, ob))
                            rez = true;
                    }
                    else if (IsCommandSlotLikeOriginal(ob, members))
                    {
                        // Original compares OB->Index with BR->Memb[0..2], even
                        // after FireOrder was qsorted. Keep that exact identity test.
                        ClearOneMemberOrdersLikeOriginal(ob, "BrigadeOrder_RifleAttack_command_slot");
                    }
                }
                nEnemyLife = order.Enemies.Count;
            }
            return rez;
        }

        // BrigadeOrder_RifleAttack::SetTargetObj.
        private static bool SetTargetObjLikeOriginal(
            OrderLikeOriginal order,
            C2NeutralPeasantUnitInfoV2LikeOriginal shooter)
        {
            if (order == null || shooter == null || order.Enemies.Count == 0) return false;

            int dist = 9999999;
            int targetIndex = -1;
            int ni = order.Enemies.Count / 8 + 5;
            bool eger = string.Equals(
                (shooter.UsageLikeOriginal ?? string.Empty).Trim(), "EGER",
                StringComparison.OrdinalIgnoreCase);
            if (eger) ni = order.Enemies.Count / 2;
            if (order.Enemies.Count < 16) ni = order.Enemies.Count;

            MdRifleProfileLikeOriginal shooterProfile = LoadMdProfileLikeOriginal(shooter);
            int seaBase = shooterProfile.SearchEnemyRadius;
            if (seaBase <= 0)
            {
                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(shooter);
                seaBase = Mathf.Max(0, md.AttackRadius1);
            }

            for (int i = 0; i < ni; i++)
            {
                int rn = PositiveModLikeOriginal(NextRandoLikeOriginal(shooter), order.Enemies.Count);
                EnemyEntryLikeOriginal enemy = order.Enemies[rn];
                C2NeutralPeasantUnitInfoV2LikeOriginal tob = enemy != null ? enemy.Unit : null;
                if (tob == null || tob.IsDeadLikeOriginal || !tob.isActiveAndEnabled) continue;

                C2CombatRuntimeV334LikeOriginal.EnsureUnitLifeV405LikeOriginal(tob);
                int life = Mathf.Max(0, tob.LifeLikeOriginal);
                if (!(life > enemy.PredictedDamage - 40 &&
                      ((enemy.FireCount < 5 && life < 101) || life > 100)))
                    continue;

                int nd = NormaLikeOriginal(
                    OriginalPixelXLikeOriginal(shooter) - OriginalPixelXLikeOriginal(tob),
                    OriginalPixelYLikeOriginal(shooter) - OriginalPixelYLikeOriginal(tob));
                if (eger)
                    nd = nd / (LoadMdProfileLikeOriginal(tob).GoldPrice + 1);

                int shooterZ = TerrainHeightLikeOriginal(shooter);
                int targetZ = TerrainHeightLikeOriginal(tob);
                int dz = shooterZ - targetZ;
                if (dz < 0) dz = 0;
                if (dz > 150) dz = 150;
                dz <<= 1;
                int sea = seaBase + dz;

                if (nd < dist && nd <= sea)
                {
                    dist = nd;
                    targetIndex = rn;
                }
            }

            if (targetIndex == -1) return false;
            EnemyEntryLikeOriginal selected = order.Enemies[targetIndex];
            if (selected == null || selected.Unit == null || selected.Unit.IsDeadLikeOriginal)
                return false;

            C2CombatRuntimeV334LikeOriginal combat =
                shooter.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat == null)
            {
                GameObject proxy = shooter.EnsureUnityProxyLikeOriginal();
                if (proxy != null)
                    combat = proxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
            }
            if (combat == null) return false;

            bool preciseAttack;
            if (!C2CombatCoreV408LikeOriginal.TryAttackObjV408LikeOriginal(
                    shooter, selected.Unit, 128 + 15, out preciseAttack))
                return false;
            if (preciseAttack)
                return true;

            C2NeutralPeasantUnitInfoV2LikeOriginal authoritativeTarget;
            bool hasAuthoritative = C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(
                shooter, out authoritativeTarget);
            if (hasAuthoritative && authoritativeTarget == selected.Unit)
            {
                combat.BeginRifleAttackObjFromBrigadeOrderV405LikeOriginal(
                    shooter, selected.Unit, selected.Unit.WorldPositionLikeOriginal);
            }
            // NoSearchVictim may make AttackObj return true without replacing the
            // old LocalOrder.  Retail still accounts EnemyLife/EnemyFireCount for
            // this true result, but it does not retarget the shooter.
            if (hasAuthoritative && authoritativeTarget == selected.Unit)
            {
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    shooter,
                    C2UnitOrderKindV325LikeOriginal.RangedAttack,
                    "BrigadeOrder_RifleAttack_V408",
                    "AttackObj_prio_143_slot_1");
            }

            // BrigadeOrders.cpp: only after AttackObj returned true.
            selected.PredictedDamage = (selected.PredictedDamage +
                C2CombatCoreV408LikeOriginal.GetDamageV408LikeOriginal(
                    shooter, selected.Unit, 1)) & 0xFFFF;
            selected.FireCount = (selected.FireCount + 1) & 0xFFFF;
            return true;
        }

        // BrigadeOrder_RifleAttack::FillEnemyList + AddEnemy.
        private static void FillEnemyListLikeOriginal(
            OrderLikeOriginal order,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members)
        {
            order.Enemies.Clear();
            order.FireOrder.Clear();
            order.EnemyCenterX = 0;
            order.EnemyCenterY = 0;
            if (members == null || members.Count == 0) return;

            int centerX = 0;
            int centerY = 0;
            int centerN = 0;
            int nation = -1;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                if (ob == null || ob.IsDeadLikeOriginal || !ob.isActiveAndEnabled) continue;
                centerX += OriginalPixelXLikeOriginal(ob);
                centerY += OriginalPixelYLikeOriginal(ob);
                centerN++;
                if (nation < 0) nation = ob.CombatNationLikeOriginal;
            }
            if (centerN == 0) return;
            centerX /= centerN;
            centerY /= centerN;

            bool eger = false;
            if (members.Count > 3 && members[3] != null)
                eger = string.Equals(
                    (members[3].UsageLikeOriginal ?? string.Empty).Trim(), "EGER",
                    StringComparison.OrdinalIgnoreCase);

            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            SortUnitsByObjectIndexV408LikeOriginal(all);
            int cou = 0;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal enemy = all[i];
                if (enemy == null || enemy.IsDeadLikeOriginal || !enemy.isActiveAndEnabled) continue;
                if (order.Representative != null &&
                    !C2CombatCoreV408LikeOriginal.CanAttackRelationV408LikeOriginal(order.Representative, enemy)) continue;

                if (order.OnlyOneBrig >= 0)
                {
                    int enemyGroup;
                    if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(
                            enemy, out enemyGroup) || enemyGroup != order.OnlyOneBrig)
                        continue;
                }

                int dx = OriginalPixelXLikeOriginal(enemy) - centerX;
                int dy = OriginalPixelYLikeOriginal(enemy) - centerY;
                // ActiveScenary.cpp::PerformActionOverUnitsInRadius uses strict
                // Euclidean dx*dx+dy*dy < R*R here (not Norma).
                long d2 = (long)dx * dx + (long)dy * dy;
                if (d2 >= (long)EnemySearchRadiusLikeOriginal * EnemySearchRadiusLikeOriginal) continue;
                if (order.Enemies.Count >= MaxEnemyLikeOriginal) break;

                bool accept = !eger;
                if (eger && order.Enemies.Count < 30)
                    accept = LoadMdProfileLikeOriginal(enemy).GoldPrice != 0 || (cou % 5) == 0;
                if (accept)
                {
                    EnemyEntryLikeOriginal entry = new EnemyEntryLikeOriginal();
                    entry.Unit = enemy;
                    order.Enemies.Add(entry);
                    order.EnemyCenterX += OriginalPixelXLikeOriginal(enemy);
                    order.EnemyCenterY += OriginalPixelYLikeOriginal(enemy);
                }
                cou++;
            }

            if (order.Enemies.Count > 0)
            {
                order.EnemyCenterX /= order.Enemies.Count;
                order.EnemyCenterY /= order.Enemies.Count;
            }

            for (int i = 0; i < members.Count; i++)
                order.FireOrder.Add(members[i]);

            if (order.Enemies.Count < 40)
            {
                order.FireOrder.Sort(delegate(
                    C2NeutralPeasantUnitInfoV2LikeOriginal a,
                    C2NeutralPeasantUnitInfoV2LikeOriginal b)
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    int d1 = NormaLikeOriginal(
                        OriginalPixelXLikeOriginal(a) - order.EnemyCenterX,
                        OriginalPixelYLikeOriginal(a) - order.EnemyCenterY);
                    int d2 = NormaLikeOriginal(
                        OriginalPixelXLikeOriginal(b) - order.EnemyCenterX,
                        OriginalPixelYLikeOriginal(b) - order.EnemyCenterY);
                    return d1 - d2;
                });
            }
        }

        // ClearTargetObj from ctor/destructor paths.
        private static void ClearTargetObjLikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members,
            string reason)
        {
            if (members == null) return;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                if (ob == null) continue;
                C2CombatRuntimeV334LikeOriginal combat =
                    ob.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(ob);
                if (combat == null) continue;
                if (combat.HasAnyRifleAttackObjV405LikeOriginal)
                    combat.ClearRifleAttackObjFromBrigadeOrderV405LikeOriginal(
                        true, reason ?? "ClearTargetObj_v405");
                else if (combat.HasAnyAttackObjForBrigadeRifleOrderV405LikeOriginal)
                    combat.ClearPreExistingAttackObjForBrigadeRifleOrderV405LikeOriginal(
                        true, reason ?? "ClearTargetObj_preexisting_v405");
            }
        }

        // ClearLastOrders deletes non-AttackObj/non-NewAttackPoint local orders.
        private static void ClearLastOrdersLikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members)
        {
            if (members == null) return;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                if (ob == null || ob.IsDeadLikeOriginal) continue;
                C2CombatRuntimeV334LikeOriginal combat =
                    ob.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                // AttackObjLink is explicitly exempt from ClearLastOrders in C2.
                if (combat != null && combat.HasAnyAttackObjForBrigadeRifleOrderV405LikeOriginal)
                    continue;
                ClearOneMemberOrdersLikeOriginal(ob, "BrigadeOrder_RifleAttack_ClearLastOrders");
            }
        }

        private static void ClearOneMemberOrdersLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal ob,
            string reason)
        {
            if (ob == null || ob.IsDeadLikeOriginal) return;
            C2CombatRuntimeV334LikeOriginal combat =
                ob.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat != null && combat.IsActiveOrderV350LikeOriginal)
                combat.CancelForExternalOrderLikeOriginal(reason ?? "ClearOrders_v405");
            C2CombatCoreV408LikeOriginal.DeleteAttackObjV408LikeOriginal(ob);
            ob.StopMoveAndFaceDirectionLikeOriginal(ob.RealDir);
        }

        private static void DeleteOrderLikeOriginal(int groupId, string reason, bool clearRifleFlags)
        {
            OrderLikeOriginal order;
            if (!OrdersLikeOriginal.TryGetValue(groupId, out order)) return;
            OrdersLikeOriginal.Remove(groupId);
            ArmedGroupsLikeOriginal.Remove(groupId);

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members = null;
            if (order != null && order.Representative != null)
            {
                int gid;
                string shape;
                C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    order.Representative, out members, out gid, out shape);
            }

            // ~BrigadeOrder_RifleAttack: RifleAttack=false for every member, then
            // ClearTargetObj(BR). The helper preserves a currently executing ATTACK
            // for one final frame/order but never clears delay/MaxDelay.
            _insideOrderDestructorLikeOriginal = true;
            try
            {
                if (clearRifleFlags && order != null && order.Representative != null)
                    C2CombatRuntimeV334LikeOriginal.ClearFormationRifleFlagsFromBrigadeOrderV405LikeOriginal(
                        order.Representative, reason ?? "BrigadeOrder_RifleAttack_destructor_v405");
                if (members != null)
                    ClearTargetObjLikeOriginal(members, reason ?? "BrigadeOrder_RifleAttack_destructor_v405");
            }
            finally
            {
                _insideOrderDestructorLikeOriginal = false;
            }

            Debug.Log("[C2:RIFLE V405 DELETE] group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                      " reason='" + (reason ?? string.Empty) + "'");
        }

        private static bool IsCommandSlotLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal ob,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members)
        {
            if (ob == null || members == null) return false;
            int n = Mathf.Min(3, members.Count);
            for (int i = 0; i < n; i++)
                if (ReferenceEquals(ob, members[i])) return true;
            return false;
        }

        private static void CountRifleReadinessLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative,
            out int ready,
            out int delayed)
        {
            ready = 0;
            delayed = 0;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
            int gid;
            string shape;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    representative, out members, out gid, out shape) || members == null) return;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                if (ob == null || ob.IsDeadLikeOriginal) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                if (!ob.RifleAttackV396LikeOriginal) continue;
                int dt, mt;
                C2CombatRuntimeV334LikeOriginal.TryGetWeaponDelayTicksV395LikeOriginal(ob, 1, out dt, out mt);
                if (dt == 0) ready++; else delayed++;
            }
        }

        private static bool GroupHasAnyRifleFlagLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal representative)
        {
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
            int gid;
            string shape;
            if (!C2FormationRuntimeV167LikeOriginal.TryGetGroupUnitsV172LikeOriginal(
                    representative, out members, out gid, out shape) || members == null)
                return false;
            for (int i = 0; i < members.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal ob = members[i];
                if (ob == null || ob.IsDeadLikeOriginal) continue;
                C2CombatRuntimeV334LikeOriginal.EnsureUnitCombatStateV396LikeOriginal(ob);
                if (ob.RifleAttackV396LikeOriginal) return true;
            }
            return false;
        }

        private static void SortUnitsByObjectIndexV408LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all)
        {
            if (all == null || all.Length < 2) return;
            Array.Sort(all, delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a,
                                     C2NeutralPeasantUnitInfoV2LikeOriginal b)
            {
                int ai = a != null ? a.C2ObjectIndexV408LikeOriginal : int.MaxValue;
                int bi = b != null ? b.C2ObjectIndexV408LikeOriginal : int.MaxValue;
                return ai.CompareTo(bi);
            });
        }

        private static int OriginalPixelXLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0;
            float real = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            return Mathf.RoundToInt(real) >> 4;
        }

        private static int OriginalPixelYLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 0;
            float real = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            return Mathf.RoundToInt(real) >> 4;
        }

        private static int TerrainHeightLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.OwnerMode == null) return 0;
            return unit.OwnerMode.C2OriginalFogTerrainHeightV1LikeOriginal(
                OriginalPixelXLikeOriginal(unit), OriginalPixelYLikeOriginal(unit));
        }

        private static int NormaLikeOriginal(int dx, int dy)
        {
            // COSSACKS2/NewMon.h::Norma exactly:
            // (max(abs(x),abs(y)) + abs(x) + abs(y)) >> 1.
            int ax = Math.Abs(dx);
            int ay = Math.Abs(dy);
            int m = ax > ay ? ax : ay;
            return (m + ax + ay) >> 1;
        }

        private static MdRifleProfileLikeOriginal LoadMdProfileLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string path = md.Path ?? string.Empty;
            MdRifleProfileLikeOriginal cached;
            if (MdProfilesLikeOriginal.TryGetValue(path, out cached)) return cached;

            MdRifleProfileLikeOriginal result = new MdRifleProfileLikeOriginal();
            result.SearchEnemyRadius = Mathf.Max(0, md.AttackRadius1);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = StripCommentLikeOriginal(lines[i]);
                        if (line.Length == 0) continue;
                        string[] t = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (t.Length == 0) continue;
                        string cmd = t[0].Trim().ToUpperInvariant();
                        if (cmd == "SEARCH_ENEMY_RADIUS" && t.Length >= 2)
                        {
                            int v;
                            if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                                result.SearchEnemyRadius = Mathf.Max(0, v);
                        }
                        else if (cmd == "SEARCH_ENEMY_RADIUS_SHIFT" && t.Length >= 2)
                        {
                            int v;
                            if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                                result.SearchEnemyRadiusShiftPercent = v;
                        }
                        else if (cmd == "SHOTALWAYSON")
                        {
                            result.ShotAlwaysOn = true;
                        }
                        else if (cmd == "PRICE" && t.Length >= 4)
                        {
                            for (int k = 2; k + 1 < t.Length; k += 2)
                            {
                                if (!string.Equals(t[k], "GOLD", StringComparison.OrdinalIgnoreCase)) continue;
                                int amount;
                                if (int.TryParse(t[k + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out amount))
                                    result.GoldPrice = Mathf.Max(0, amount);
                            }
                        }
                    }
                }
                catch
                {
                    // MD data are already parsed by the project. Failure here only
                    // removes EGER price weighting/ShotAlwaysOn extras; base radius remains.
                }
            }
            if (result.SearchEnemyRadiusShiftPercent != 0)
            {
                int den = 100 + result.SearchEnemyRadiusShiftPercent;
                if (den != 0)
                    result.SearchEnemyRadius = (result.SearchEnemyRadius * 100) / den;
            }
            MdProfilesLikeOriginal[path] = result;
            return result;
        }

        private static string StripCommentLikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            int p = line.IndexOf("//", StringComparison.Ordinal);
            if (p >= 0) line = line.Substring(0, p);
            return line.Trim();
        }

        // UnSyncro.cpp::RandNew: every subsystem shares the same randoma[8192]/rpos.
        // Keeping a rifle-local stream changes every later combat random decision.
        private static int NextRandoLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal seedUnit)
        {
            return C2RetailRandomV407LikeOriginal.Rando(seedUnit);
        }

        private static int PositiveModLikeOriginal(int value, int divisor)
        {
            if (divisor <= 0) return 0;
            int r = value % divisor;
            return r < 0 ? r + divisor : r;
        }
    }
}
