using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // UNIT LOGIC REMOVED COMPATIBILITY ONLY.
    // These types keep building HUD/selection/construction code compiling without restoring unit runtime/spawn/rendering.
    internal static class C2NeutralPeasantUnitsLogGateV45LikeOriginal
    {
        public static bool Verbose = false;
    }

    public sealed class C2NeutralPeasantUnitFrameV2LikeOriginal
    {
        public Texture2D Texture;
        public Texture2D MaskTexture;
        public int FileRef;
        public int BaseSprite;
        public int ExactSprite;
        public bool MirrorX;
        public int PivotDx;
        public int PivotDy;
        public int Width;
        public int Height;
    }

    // Compatibility facade over the central C2 animation state.  The original
    // engine does not allocate an animator object in the scene for every unit.
    public sealed class C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal
    {
        private readonly C2NeutralPeasantUnitInfoV2LikeOriginal _unit;

        internal C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            _unit = unit;
        }

        private C2UnitOriginalRuntimeLinkLikeOriginal RuntimeLink
        {
            get
            {
                return _unit != null ? _unit.RuntimeLinkCachedLikeOriginal : null;
            }
        }

        public C2NeutralPeasantUnitFrameV2LikeOriginal CurrentFrame { get { return null; } }
        public int CurrentFrameIndex { get { return -1; } }
        public bool IsDyingLikeOriginal { get { return false; } }
        public bool IsDeathFinishedLikeOriginal { get { return false; } }

        public void SetRendererSortingOrderLikeOriginal(int order) { }
        public bool TryPixelHit(Camera cam, Vector3 screenPosition, out float alpha, out Vector2 uv)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null)
                return link.TryPixelHitLikeOriginal(cam, screenPosition, out alpha, out uv);
            alpha = 0.0f;
            uv = Vector2.zero;
            return false;
        }
        public bool TryGetScreenQuadDistanceLikeOriginal(Camera cam, Vector3 screenPosition, out float distancePx, out Vector2 anchor, out Vector4 rect)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            Rect r;
            if (link != null && link.TryGetScreenRectLikeOriginal(cam, out r, out anchor))
            {
                rect = new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
                Vector2 p = new Vector2(screenPosition.x, screenPosition.y);
                distancePx = r.Contains(p, true) ? 0.0f : Vector2.Distance(p, r.center);
                return true;
            }
            distancePx = float.MaxValue;
            anchor = Vector2.zero;
            rect = Vector4.zero;
            return false;
        }
        public bool TryGetSelectionFootScreenPointLikeOriginal(Camera cam, out Vector2 footScreen)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            Rect r;
            if (link != null && link.TryGetScreenRectLikeOriginal(cam, out r, out footScreen))
                return true;
            footScreen = Vector2.zero;
            return false;
        }
        public void SetSelectedVisualLikeOriginal(bool selected) { }
        public void SetForceStandLikeOriginal(bool forceStand) { }
        public void SetRealDirectionLikeOriginal(byte realDir)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.SetFacingDirectionLikeOriginal(realDir);
        }
        public void SetMotionStateLikeOriginal(byte realDir, bool backMotion)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.SetMotionStateLikeOriginal(realDir, backMotion);
        }
        public void SetMovingLikeOriginal(bool moving)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.SetMovingLikeOriginal(moving);
        }
        public int GetWorkFrameCountLikeOriginal(byte realDir)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            return link != null ? link.GetWorkFrameCountLikeOriginal(realDir) : 0;
        }
        public bool SetWorkFramePhaseLikeOriginal(byte realDir, float phase, bool force)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            return link != null && link.SetWorkFramePhaseLikeOriginal(realDir, phase, force);
        }
        public void StopWorkAnimationLikeOriginal()
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.StopWorkAnimationLikeOriginal();
        }
        public void SetDeathFramesByDirLikeOriginal(C2NeutralPeasantUnitFrameV2LikeOriginal[][] deathFramesByDir) { }
        public bool PlayDeathOneShotLikeOriginal(byte realDir) { return false; }
        public void SetWalkPathFrameLikeOriginal(float totalPathReal, float rInFrameReal)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.SetWalkPathFrameLikeOriginal(totalPathReal, rInFrameReal);
        }
    }

    public sealed class C2NeutralPeasantUnitInfoV2LikeOriginal
    {
        // COSSACKS2 keeps every live OneObject in the central Group[] table.
        // Unity's FindObjectsOfType walks the native object hierarchy and creates
        // a managed array on every call; doing that for thousands of units is not
        // part of the original engine.  Keep the compatibility components in one
        // registration-order table and expose one cached snapshot instead.
        private static readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> C2ActiveUnitsV359LikeOriginal =
            new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(16384);
        private static readonly Dictionary<int, int> C2ActiveUnitIndexByInstanceV359LikeOriginal =
            new Dictionary<int, int>(16384);
        private static readonly Dictionary<EntityId, C2NeutralPeasantUnitInfoV2LikeOriginal> C2ActiveUnitByProxyInstanceV366LikeOriginal =
            new Dictionary<EntityId, C2NeutralPeasantUnitInfoV2LikeOriginal>(256);
        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] C2ActiveUnitsSnapshotV359LikeOriginal =
            Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private static bool C2ActiveUnitsSnapshotDirtyV359LikeOriginal = true;
        internal static int C2ActiveUnitsRegistryRevisionV359LikeOriginal { get; private set; }
        // Integration storage for COSSACKS2 OneObject::Index.  The original Group[]
        // index is stable for an object lifetime and is paired with Serial.  Unity
        // objects do not expose that table, so assign one stable 16-bit slot when
        // the managed OneObject record first registers.  Combat code may use the
        // slot for the original 8192-byte BitMask without changing battle logic.
        // V408: faithful Group[]-style slot allocator. Slots are reused only after
        // release and every reuse receives a new Serial generation, so stale
        // EnemyID/EnemySN and ATTLIST references cannot bind to a new object.
        private static int C2NextObjectIndexV408LikeOriginal;
        private static readonly Queue<int> C2FreeObjectIndicesV408LikeOriginal = new Queue<int>();
        private static readonly ushort[] C2ObjectSerialGenerationV408LikeOriginal = new ushort[65536];
        private static readonly C2NeutralPeasantUnitInfoV2LikeOriginal[] C2ObjectByIndexV408LikeOriginal =
            new C2NeutralPeasantUnitInfoV2LikeOriginal[65536];

        // OneObject is plain engine data in COSSACKS2, not a Unity Component.
        // Keep the temporary GameObject only as the current visual proxy while
        // the simulation/gameplay record itself stays in managed engine data.
        [NonSerialized] private GameObject _rootGameObjectV365LikeOriginal;
        [NonSerialized] private int _instanceIdV365LikeOriginal;
        [NonSerialized] private bool _registeredV365LikeOriginal;
        [NonSerialized] internal int C2ObjectIndexV407LikeOriginal = -1;
        [NonSerialized] internal ushort C2ObjectSerialV408LikeOriginal;

        internal int C2ObjectIndexV408LikeOriginal { get { return C2ObjectIndexV407LikeOriginal; } }
        internal static int C2ObjectHighWaterMarkLikeOriginal { get { return C2NextObjectIndexV408LikeOriginal; } }

        internal static C2NeutralPeasantUnitInfoV2LikeOriginal C2GetByObjectIndexLikeOriginal(int index)
        {
            return index >= 0 && index < C2NextObjectIndexV408LikeOriginal
                ? C2ObjectByIndexV408LikeOriginal[index] : null;
        }
        internal ushort C2ObjectSerialLikeOriginal { get { return C2ObjectSerialV408LikeOriginal; } }

        public GameObject gameObject { get { return _rootGameObjectV365LikeOriginal; } }
        public Transform transform
        {
            get
            {
                GameObject root = EnsureUnityProxyLikeOriginal();
                return root != null ? root.transform : null;
            }
        }
        public bool isActiveAndEnabled
        {
            get
            {
                C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
                return link != null
                    ? link.IsReadyLikeOriginal
                    : _rootGameObjectV365LikeOriginal != null && _rootGameObjectV365LikeOriginal.activeInHierarchy;
            }
        }

        public Vector3 WorldPositionLikeOriginal
        {
            get
            {
                C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
                return link != null ? link.WorldPositionLikeOriginal :
                    (_rootGameObjectV365LikeOriginal != null ? _rootGameObjectV365LikeOriginal.transform.position : Vector3.zero);
            }
        }

        public GameObject EnsureUnityProxyLikeOriginal()
        {
            if (_rootGameObjectV365LikeOriginal != null) return _rootGameObjectV365LikeOriginal;
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            return link != null ? link.EnsureUnityProxyLikeOriginal() : null;
        }

        public void SetActiveLikeOriginal(bool active)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.SetActiveLikeOriginal(active);
            else if (_rootGameObjectV365LikeOriginal != null) _rootGameObjectV365LikeOriginal.SetActive(active);
        }

        public int GetInstanceID()
        {
            return _instanceIdV365LikeOriginal;
        }

        public T GetComponent<T>() where T : Component
        {
            return _rootGameObjectV365LikeOriginal != null
                ? _rootGameObjectV365LikeOriginal.GetComponent<T>()
                : null;
        }

        internal void C2BindRuntimeAndRegisterV366LikeOriginal(GameObject root, int stableInstanceId)
        {
            if (_registeredV365LikeOriginal && _instanceIdV365LikeOriginal == stableInstanceId)
            {
                C2AttachUnityProxyV366LikeOriginal(root);
                return;
            }
            C2ReleaseRegistrationV365LikeOriginal();
            _rootGameObjectV365LikeOriginal = root;
            _instanceIdV365LikeOriginal = stableInstanceId != 0
                ? stableInstanceId
                : (root != null ? root.GetEntityId().GetHashCode() : 0);
            if (_instanceIdV365LikeOriginal == 0) return;
            if (C2ObjectIndexV407LikeOriginal < 0)
            {
                int slot = -1;
                while (C2FreeObjectIndicesV408LikeOriginal.Count > 0 && slot < 0)
                {
                    int candidate = C2FreeObjectIndicesV408LikeOriginal.Dequeue();
                    if (candidate >= 0 && candidate < C2ObjectByIndexV408LikeOriginal.Length &&
                        C2ObjectByIndexV408LikeOriginal[candidate] == null)
                        slot = candidate;
                }
                if (slot < 0)
                {
                    while (C2NextObjectIndexV408LikeOriginal < C2ObjectByIndexV408LikeOriginal.Length &&
                           C2ObjectByIndexV408LikeOriginal[C2NextObjectIndexV408LikeOriginal] != null)
                        C2NextObjectIndexV408LikeOriginal++;
                    if (C2NextObjectIndexV408LikeOriginal < C2ObjectByIndexV408LikeOriginal.Length)
                        slot = C2NextObjectIndexV408LikeOriginal++;
                }
                if (slot < 0)
                    throw new InvalidOperationException("COSSACKS2 Group[] compatibility table exhausted (65536 live objects)");

                ushort serial = unchecked((ushort)(C2ObjectSerialGenerationV408LikeOriginal[slot] + 1));
                if (serial == 0 || serial == 0xFFFF) serial = 1;
                C2ObjectSerialGenerationV408LikeOriginal[slot] = serial;
                C2ObjectIndexV407LikeOriginal = slot;
                C2ObjectSerialV408LikeOriginal = serial;
                C2ObjectByIndexV408LikeOriginal[slot] = this;
            }

            if (!C2ActiveUnitIndexByInstanceV359LikeOriginal.ContainsKey(_instanceIdV365LikeOriginal))
            {
                C2ActiveUnitIndexByInstanceV359LikeOriginal.Add(
                    _instanceIdV365LikeOriginal, C2ActiveUnitsV359LikeOriginal.Count);
                C2ActiveUnitsV359LikeOriginal.Add(this);
                C2ActiveUnitsSnapshotDirtyV359LikeOriginal = true;
                C2ActiveUnitsRegistryRevisionV359LikeOriginal++;
            }
            _registeredV365LikeOriginal = true;
            C2LiveUnitCellIndex.PositionChanged(this);
            C2AttachUnityProxyV366LikeOriginal(root);
        }

        internal void C2AttachUnityProxyV366LikeOriginal(GameObject root)
        {
            if (_rootGameObjectV365LikeOriginal != null)
                C2ActiveUnitByProxyInstanceV366LikeOriginal.Remove(_rootGameObjectV365LikeOriginal.GetEntityId());
            _rootGameObjectV365LikeOriginal = root;
            if (root != null)
                C2ActiveUnitByProxyInstanceV366LikeOriginal[root.GetEntityId()] = this;
        }

        internal static C2NeutralPeasantUnitInfoV2LikeOriginal C2FindForGameObjectV365LikeOriginal(GameObject root)
        {
            if (root == null) return null;
            C2NeutralPeasantUnitInfoV2LikeOriginal info;
            return C2ActiveUnitByProxyInstanceV366LikeOriginal.TryGetValue(root.GetEntityId(), out info)
                ? info
                : null;
        }

        private void C2ReleaseRegistrationV365LikeOriginal()
        {
            if (!_registeredV365LikeOriginal && C2ObjectIndexV407LikeOriginal < 0) return;
            int instanceId = _instanceIdV365LikeOriginal;
            int index;
            if (_registeredV365LikeOriginal &&
                C2ActiveUnitIndexByInstanceV359LikeOriginal.TryGetValue(instanceId, out index))
            {
                int lastIndex = C2ActiveUnitsV359LikeOriginal.Count - 1;
                if (lastIndex >= 0 && index >= 0 && index <= lastIndex)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal last = C2ActiveUnitsV359LikeOriginal[lastIndex];
                    C2ActiveUnitsV359LikeOriginal[index] = last;
                    C2ActiveUnitsV359LikeOriginal.RemoveAt(lastIndex);
                    if (index != lastIndex && last != null)
                        C2ActiveUnitIndexByInstanceV359LikeOriginal[last.GetInstanceID()] = index;
                }
                C2ActiveUnitIndexByInstanceV359LikeOriginal.Remove(instanceId);
                C2ActiveUnitsSnapshotDirtyV359LikeOriginal = true;
                C2ActiveUnitsRegistryRevisionV359LikeOriginal++;
            }
            C2LiveUnitCellIndex.Remove(this);
            _registeredV365LikeOriginal = false;
            C2ReleaseObjectIdentityV408LikeOriginal();
        }

        private void C2ReleaseObjectIdentityV408LikeOriginal()
        {
            if (C2ObjectIndexV407LikeOriginal >= 0 &&
                C2ObjectIndexV407LikeOriginal < C2ObjectByIndexV408LikeOriginal.Length &&
                ReferenceEquals(C2ObjectByIndexV408LikeOriginal[C2ObjectIndexV407LikeOriginal], this))
            {
                int releasedSlot = C2ObjectIndexV407LikeOriginal;
                C2ObjectByIndexV408LikeOriginal[releasedSlot] = null;
                C2FreeObjectIndicesV408LikeOriginal.Enqueue(releasedSlot);
            }
            C2ObjectIndexV407LikeOriginal = -1;
            C2ObjectSerialV408LikeOriginal = 0;
        }

        internal static C2NeutralPeasantUnitInfoV2LikeOriginal C2GetByIndexSerialV408LikeOriginal(int index, ushort serial)
        {
            if (index < 0 || index >= C2ObjectByIndexV408LikeOriginal.Length) return null;
            C2NeutralPeasantUnitInfoV2LikeOriginal unit = C2ObjectByIndexV408LikeOriginal[index];
            return unit != null && unit.C2ObjectSerialV408LikeOriginal == serial ? unit : null;
        }

        internal void C2ReleaseV365LikeOriginal()
        {
            C2MoraleRuntimeV404LikeOriginal.ReleaseUnitLikeOriginal(this);
            C2OriginalOrderChainV352.ReleaseForUnitLikeOriginal(this);
            C2ReleaseRegistrationV365LikeOriginal();
            if (_rootGameObjectV365LikeOriginal != null)
                C2ActiveUnitByProxyInstanceV366LikeOriginal.Remove(_rootGameObjectV365LikeOriginal.GetEntityId());
            _rootGameObjectV365LikeOriginal = null;
        }

        internal static int C2ActiveUnitCountForDiagnostics => C2ActiveUnitsV359LikeOriginal.Count;

        public static C2NeutralPeasantUnitInfoV2LikeOriginal[] C2GetActiveUnitsSnapshotV359LikeOriginal()
        {
            if (!C2ActiveUnitsSnapshotDirtyV359LikeOriginal)
                return C2ActiveUnitsSnapshotV359LikeOriginal;

            C2ActiveUnitsSnapshotV359LikeOriginal = C2ActiveUnitsV359LikeOriginal.Count == 0
                ? Array.Empty<C2NeutralPeasantUnitInfoV2LikeOriginal>()
                : C2ActiveUnitsV359LikeOriginal.ToArray();
            C2ActiveUnitsSnapshotDirtyV359LikeOriginal = false;
            return C2ActiveUnitsSnapshotV359LikeOriginal;
        }

        internal static int C2SelectionRevisionV346LikeOriginal { get; private set; }
        // COSSACKS2 keeps these directly inside OneObject.  They are ordinary
        // runtime data, not Unity behaviours; keeping them here avoids one
        // native Component allocation per state block and per active order.
        [NonSerialized] internal C2UnitOrderRuntimeV325LikeOriginal C2OrderRuntimeStateV362LikeOriginal;
        [NonSerialized] internal C2OriginalOrderChainV352 C2MoveOrderChainV362LikeOriginal;
        public C2BattleTerrainMode OwnerMode;
        public C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal SpriteAnimator;
        public MeshRenderer SpriteMeshRenderer;

        public string SourceMonsterId = string.Empty;
        public string ResolvedMd = string.Empty;
        public int RecordIndex;
        private byte _NationV420;
        public byte Nation
        {
            get { return _NationV420; }
            set
            {
                if (_NationV420 == value) return;
                _NationV420 = value;
                C2LiveUnitCellIndex.AllegianceChanged(this);
            }
        }
        // CheckCapture changes Serial so old attack/projectile references expire.
        // Use the registry generation instead of toggling a bit: repeated captures
        // must not accidentally make a reference from two owners ago valid again.
        internal void C2RenewIdentityAfterCaptureV441()
        {
            int slot = C2ObjectIndexV408LikeOriginal;
            if (slot < 0 || slot >= C2ObjectSerialGenerationV408LikeOriginal.Length) return;
            ushort serial = unchecked((ushort)(C2ObjectSerialGenerationV408LikeOriginal[slot] + 1));
            if (serial == 0 || serial == 0xFFFF) serial = 1;
            C2ObjectSerialGenerationV408LikeOriginal[slot] = serial;
            C2ObjectSerialV408LikeOriginal = serial;
        }
        public ushort NIndex;
        public int RealX;
        public int RealY;
        public byte RealDir;
        public byte GraphDir;
        public byte OctantInfo = 0xFF;
        public float RealXFloat;
        public float RealYFloat;
        public int RealDirPrecise;
        public int SortKey;
        public int FrameCount;
        public int FirstFileRef;
        public int FirstExactSprite;
        public bool FirstMirrorX;
        public string DirectionAudit = string.Empty;
        public string VisualAudit = string.Empty;
        public string FramesAudit = string.Empty;
        public bool NotSelectable;
        public bool ControllableByPlayer = false;
        // DIP_SimpleBuilding gives villagers NMask=GetNatNMASK(Owner)|128:
        // settlement AI commands them, the human player does not.
        private bool _SettlementAiControlledLikeOriginalV420;
        public bool SettlementAiControlledLikeOriginal
        {
            get { return _SettlementAiControlledLikeOriginalV420; }
            set
            {
                if (_SettlementAiControlledLikeOriginalV420 == value) return;
                _SettlementAiControlledLikeOriginalV420 = value;
                C2LiveUnitCellIndex.AllegianceChanged(this);
            }
        }
        // Visual NNUM remains 7 for settlement population. NMask allegiance
        // follows the settlement Owner and is used by combat/AI only.
        private int _SettlementAllegianceNationLikeOriginalV420 = -1;
        public int SettlementAllegianceNationLikeOriginal
        {
            get { return _SettlementAllegianceNationLikeOriginalV420; }
            set
            {
                if (_SettlementAllegianceNationLikeOriginalV420 == value) return;
                _SettlementAllegianceNationLikeOriginalV420 = value;
                C2LiveUnitCellIndex.AllegianceChanged(this);
            }
        }
        public int UnitRadius = 16;
        // COSSACKS2 OneObject::ActivityState: 0 normal, 1 peaceful, 2 aggressive.
        // Kept as engine data so BrigadeOrder_Bitva/AttackObj can use the same gates.
        public byte ActivityStateV413LikeOriginal;
        // OneObject::FriendlyFire is the per-object toggle used only when the MD
        // declares FRIENDLYFIRE. Creation memset leaves it false in retail.
        public bool FriendlyFireV413LikeOriginal;
        public int MotionDist = 40;
        public int GeometryRadius2Real = 160;
        public bool CanBuildLikeOriginal;
        public bool PioneerLikeOriginal;
        public string UsageLikeOriginal = string.Empty;
        public int LifeLikeOriginal;
        public int MaxLifeLikeOriginal;
        internal int MoraleFixedLikeOriginal;
        internal int MaxMoraleFixedLikeOriginal;
        public float MoraleLikeOriginal
        {
            get { return MoraleFixedLikeOriginal / 10000.0f; }
            set { MoraleFixedLikeOriginal = Mathf.RoundToInt(value * 10000.0f); }
        }
        public float MaxMoraleLikeOriginal
        {
            get { return MaxMoraleFixedLikeOriginal / 10000.0f; }
            set { MaxMoraleFixedLikeOriginal = Mathf.RoundToInt(value * 10000.0f); }
        }
        [NonSerialized] internal bool MoraleInitializedLikeOriginal;
        // OneObject::GetTired is 100000 when fresh and falls toward zero.
        // V398 keeps the retail integer state as the authority; the float is only
        // a compatibility mirror for existing HUD/AI callers.
        public int GetTiredLikeOriginal = 100000;
        public float TiringRemainingPercentLikeOriginal = 100.0f;

        // V396: persistent OneObject combat fields.  In retail C2 these live on
        // OneObject from birth and are not inferred from distance at attack time.
        [NonSerialized] public bool CombatStateInitializedV396LikeOriginal;
        [NonSerialized] public bool ArmAttackCapableV396LikeOriginal;
        [NonSerialized] public bool ArmAttackV396LikeOriginal;
        [NonSerialized] public bool RifleAttackV396LikeOriginal;
        [NonSerialized] public int GroundStateV396LikeOriginal;
        [NonSerialized] public int NewStateV396LikeOriginal;
        // OneObject::SearchOnlyThisBrigadeToKill (0xFFFF in retail).  -1 is the
        // managed sentinel; SetEnemyForBrigade writes this on every member.
        [NonSerialized] public int SearchOnlyThisBrigadeToKillV407LikeOriginal = -1;

        public string SelectionTypeName = string.Empty;
        public float SelectionScaleX = 1.0f;
        public float SelectionScaleY = 1.0f;
        public int SelectionShift;
        public float MapPixelToWorld = 0.1f;
        public Vector3 SelectionLocalOffset;
        public float MarkerYOffset = -0.042f;
        public string SelectionAudit = string.Empty;

        private bool _selected;
        private bool _hasMoveTarget;
        private bool _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal;
        private int _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
        private bool _hasLastMoveRequestLikeOriginal;
        private float _lastMoveRequestRealXLikeOriginal;
        private float _lastMoveRequestRealYLikeOriginal;
        private float _lastMoveRequestSpeedLikeOriginal;
        private bool _lastMoveRequestHasFacingLikeOriginal;
        private byte _lastMoveRequestFacingLikeOriginal;
        private bool _lastMoveRequestPreciseLikeOriginal;
        private int _lineSortOverrideOrderLikeOriginal = int.MinValue;

        private C2UnitOriginalRuntimeLinkLikeOriginal _runtimeLinkCachedLikeOriginal;
        private C2UnitOriginalRuntimeLinkLikeOriginal RuntimeLink
        {
            get
            {
                return _runtimeLinkCachedLikeOriginal;
            }
        }

        internal C2UnitOriginalRuntimeLinkLikeOriginal RuntimeLinkCachedLikeOriginal
        {
            get { return RuntimeLink; }
        }

        internal void BindRuntimeLinkV364LikeOriginal(C2UnitOriginalRuntimeLinkLikeOriginal link)
        {
            _runtimeLinkCachedLikeOriginal = link;
        }

        public bool IsSelected
        {
            get
            {
                C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
                return link != null ? link.IsSelectedLikeOriginal : _selected;
            }
        }

        public bool IsDeadLikeOriginal
        {
            get
            {
                C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
                return link != null && link.Runtime != null && link.Runtime.State == C2UnitOriginalState.Death;
            }
        }

        public bool PlayDeathOneShotLikeOriginal(byte realDir)
        {
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                this, C2UnitOrderKindV325LikeOriginal.Death, "death", "one_shot");
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) return link.PlayDeathOneShotLikeOriginal(realDir);
            if (SpriteAnimator != null) return SpriteAnimator.PlayDeathOneShotLikeOriginal(realDir);
            return false;
        }

        public bool PlayAttackOneShotV325LikeOriginal(int attackState)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            return link != null && link.PlayAttackOneShotV325LikeOriginal(attackState);
        }

        public bool CanReceiveOrdersLikeOriginal()
        {
            if (NotSelectable) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            return link != null ? link.CanReceiveOrdersLikeOriginal() : ControllableByPlayer;
        }

        public bool CanReceivePlayerOrdersLikeOriginal()
        {
            return CanReceiveOrdersLikeOriginal() &&
                   !SettlementAiControlledLikeOriginal &&
                   ControllableByPlayer &&
                   C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(Nation);
        }

        public bool IsBusyWithBornExitOrMoveLikeOriginal()
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            return _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal ||
                   _hasMoveTarget ||
                   (link != null && link.Runtime != null &&
                    (link.Runtime.PreciseBornPathLikeOriginal || link.Runtime.HasMoveTargetLikeOriginal));
        }

        public int CombatNationLikeOriginal
        {
            get { return SettlementAllegianceNationLikeOriginal >= 0
                ? SettlementAllegianceNationLikeOriginal : Nation; }
        }

        public bool CanBuildOrRepairLikeOriginal()
        {
            return CanReceiveOrdersLikeOriginal() && (CanBuildLikeOriginal || PioneerLikeOriginal || IsPeasantLikeOriginal());
        }

        private string _peasantSourceV420, _peasantMdV420;
        private bool _peasantValueV420, _peasantBoundV420;

        public bool IsPeasantLikeOriginal()
        {
            if (_peasantBoundV420 && string.Equals(_peasantSourceV420, SourceMonsterId, StringComparison.Ordinal) &&
                string.Equals(_peasantMdV420, ResolvedMd, StringComparison.Ordinal)) return _peasantValueV420;
            _peasantSourceV420 = SourceMonsterId;
            _peasantMdV420 = ResolvedMd;
            _peasantBoundV420 = true;
            string id = ((SourceMonsterId ?? string.Empty) + " " + (ResolvedMd ?? string.Empty)).ToLowerInvariant();
            return _peasantValueV420 = id.IndexOf("kri", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("peasant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("worker", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool TryPixelHit(Camera cam, Vector3 screenPosition, out float alpha, out Vector2 uv)
        {
            if (SpriteAnimator != null)
                return SpriteAnimator.TryPixelHit(cam, screenPosition, out alpha, out uv);
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null)
                return link.TryPixelHitLikeOriginal(cam, screenPosition, out alpha, out uv);
            alpha = 0.0f;
            uv = Vector2.zero;
            return false;
        }

        public bool TryGetScreenQuadDistance(Camera cam, Vector3 screenPosition, out float distancePx, out Vector2 anchor, out Vector4 rect)
        {
            if (SpriteAnimator != null)
                return SpriteAnimator.TryGetScreenQuadDistanceLikeOriginal(cam, screenPosition, out distancePx, out anchor, out rect);
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            Rect r;
            if (link != null && link.TryGetScreenRectLikeOriginal(cam, out r, out anchor))
            {
                rect = new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
                Vector2 p = new Vector2(screenPosition.x, screenPosition.y);
                distancePx = r.Contains(p, true) ? 0.0f : Vector2.Distance(p, r.center);
                return true;
            }
            distancePx = float.MaxValue;
            anchor = Vector2.zero;
            rect = Vector4.zero;
            return false;
        }

        public void SetSelected(bool selected)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null)
            {
                link.SetSelectedLikeOriginal(selected);
                return;
            }
            bool next = selected && !NotSelectable;
            if (_selected != next) C2SelectionRevisionV346LikeOriginal++;
            _selected = next;
            if (SpriteAnimator != null) SpriteAnimator.SetSelectedVisualLikeOriginal(_selected);
        }

        internal void SetSelectedFromRuntimeLikeOriginal(bool selected)
        {
            bool next = selected && !NotSelectable;
            if (_selected != next) C2SelectionRevisionV346LikeOriginal++;
            _selected = next;
            if (SpriteAnimator != null) SpriteAnimator.SetSelectedVisualLikeOriginal(_selected);
        }

        public void ForceUpdateSelectionVisualsV42LikeOriginal()
        {
            if (SpriteAnimator != null) SpriteAnimator.SetSelectedVisualLikeOriginal(IsSelected);
        }

        public void SetMoveSpeedLikeOriginal(float speedOriginalPixelsPerSecond) { }

        public void SetMoveDestinationLikeOriginal(Vector3 target)
        {
            CancelBuildOrderForExternalMoveV258LikeOriginal("world_move");
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                this, C2UnitOrderKindV325LikeOriginal.Move, "world_move", "world_destination");

            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null)
            {
                _hasMoveTarget = true;
                _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = false;
                _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
                link.SetMoveDestinationWorldLikeOriginal(target, C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal);
                return;
            }

            if (OwnerMode != null)
            {
                float targetOriginalX;
                float targetOriginalY;
                if (OwnerMode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(target, out targetOriginalX, out targetOriginalY))
                {
                    SetMoveDestinationRealLikeOriginal(
                        targetOriginalX * 16.0f,
                        targetOriginalY * 16.0f,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        false,
                        0);
                    return;
                }
            }

            transform.position = target;
        }

        private void CancelBuildOrderForExternalMoveV258LikeOriginal(string reason)
        {
            if (C2BuildWorkerOrderV245LikeOriginal.C2BuildWorkerOrderInternalMoveV258LikeOriginal)
                return;

            C2BuildWorkerOrderV245LikeOriginal build = GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
            if (build != null && build.enabled)
                build.CancelFromExternalOrderLikeOriginal(reason ?? "external_move");
        }

        public void SetMoveDestinationRealLikeOriginal(float destRealX, float destRealY, float speedOriginalPixelsPerSecond)
        {
            SetMoveDestinationRealLikeOriginal(destRealX, destRealY, speedOriginalPixelsPerSecond, false, 0);
        }

        public void SetPreciseMoveDestinationRealLikeOriginal(float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir)
        {
            _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = true;
            SetMoveDestinationRealLikeOriginal(destRealX, destRealY, speedOriginalPixelsPerSecond, hasFinalFacingDir, finalFacingDir);
        }

        public void SetMoveDestinationRealLikeOriginal(float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir)
        {
            bool preciseBornPath = _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal;
            if (_hasLastMoveRequestLikeOriginal && IsBusyWithBornExitOrMoveLikeOriginal() &&
                Mathf.Abs(_lastMoveRequestRealXLikeOriginal - destRealX) < 1.0f &&
                Mathf.Abs(_lastMoveRequestRealYLikeOriginal - destRealY) < 1.0f &&
                Mathf.Abs(_lastMoveRequestSpeedLikeOriginal - speedOriginalPixelsPerSecond) < 0.01f &&
                _lastMoveRequestHasFacingLikeOriginal == hasFinalFacingDir &&
                (!hasFinalFacingDir || _lastMoveRequestFacingLikeOriginal == finalFacingDir) &&
                _lastMoveRequestPreciseLikeOriginal == preciseBornPath)
                return;

            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            Vector2[] path = null;
            bool useBuiltPath = false;
            bool directTravelClear = preciseBornPath;
            // The linked original runtime owns path planning.  Planning here and
            // again in SetRuntimeMoveDestinationRealInternal produced exactly two
            // searches per unit/order (unit_info_move + runtime:direct_order).
            // The runtime now performs the single authoritative three-way result:
            // clear direct segment / waypoint path / no passable route.

            _hasLastMoveRequestLikeOriginal = true;
            _lastMoveRequestRealXLikeOriginal = destRealX;
            _lastMoveRequestRealYLikeOriginal = destRealY;
            _lastMoveRequestSpeedLikeOriginal = speedOriginalPixelsPerSecond;
            _lastMoveRequestHasFacingLikeOriginal = hasFinalFacingDir;
            _lastMoveRequestFacingLikeOriginal = finalFacingDir;
            _lastMoveRequestPreciseLikeOriginal = preciseBornPath;

            CancelBuildOrderForExternalMoveV258LikeOriginal("real_move");
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                this, C2UnitOrderKindV325LikeOriginal.Move, "real_move",
                hasFinalFacingDir ? "destination_with_facing" : "destination");

            if (link != null)
            {
                _hasMoveTarget = true;
                if (!preciseBornPath)
                    _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;

                // Linked original runtime owns RealX/RealY and moves them continuously.
                // Do not pre-write destination into RealXFloat here, otherwise builder orders
                // think the peasant has already arrived at its BUILDPOINT and start #WORK too early.
                if (preciseBornPath)
                {
                    link.SetValidatedDirectMoveDestinationRealLikeOriginal(
                        destRealX,
                        destRealY,
                        speedOriginalPixelsPerSecond,
                        hasFinalFacingDir,
                        finalFacingDir,
                        preciseBornPath,
                        preciseBornPath ? "precise_born_order" : "unit_info_direct_validated");
                }
                else
                {
                    link.SetMoveDestinationRealLikeOriginal(
                        destRealX,
                        destRealY,
                        speedOriginalPixelsPerSecond,
                        hasFinalFacingDir,
                        finalFacingDir);
                }

                // Final facing is applied by runtime only after the last waypoint.
                // Applying it here made units slide backwards/sideways before motion corrected direction.
                return;
            }

            _hasMoveTarget = true;
            _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = false;
            _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
            C2NeutralPeasantFallbackMoveV311LikeOriginal mover = GetComponent<C2NeutralPeasantFallbackMoveV311LikeOriginal>();
            GameObject proxy = mover == null ? EnsureUnityProxyLikeOriginal() : null;
            if (mover == null && proxy != null) mover = proxy.AddComponent<C2NeutralPeasantFallbackMoveV311LikeOriginal>();
            if (mover == null) return;
            mover.Begin(this, destRealX, destRealY, speedOriginalPixelsPerSecond, hasFinalFacingDir, finalFacingDir);
        }

        public void SetFormationAssemblyDestinationRealLikeOriginal(float destRealX, float destRealY, float speedOriginalPixelsPerSecond)
        {
            SetFormationAssemblyDestinationRealLikeOriginal(
                destRealX, destRealY, speedOriginalPixelsPerSecond, RealDir);
        }

        public void SetFormationAssemblyDestinationRealLikeOriginal(
            float destRealX,
            float destRealY,
            float speedOriginalPixelsPerSecond,
            byte finalFacingDir)
        {
            // Formation assembly must use the same normal LOCKPOINTS path and animation setup
            // as any other order. The previous one-waypoint shortcut made blocked members
            // slide and converge instead of reaching their distinct orders.lst positions.
            SetMoveDestinationRealLikeOriginal(
                destRealX, destRealY, speedOriginalPixelsPerSecond, true, finalFacingDir);
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                this, C2UnitOrderKindV325LikeOriginal.FormationMove, "formation_move", "orders_lst_slot");
        }

        public void SetFacingDirectionLikeOriginal(byte realDir)
        {
            RealDir = realDir;
            GraphDir = realDir;
            RealDirPrecise = realDir;
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) link.SetFacingDirectionLikeOriginal(realDir);
            if (SpriteAnimator != null) SpriteAnimator.SetRealDirectionLikeOriginal(realDir);
        }

        public void SetCombatPostureV322LikeOriginal(int weaponType, bool active)
        {
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                this,
                active ? C2UnitOrderKindV325LikeOriginal.CombatPosture : C2UnitOrderKindV325LikeOriginal.Stand,
                "combat_posture",
                weaponType.ToString(System.Globalization.CultureInfo.InvariantCulture));
            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null)
                link.SetCombatPostureV322LikeOriginal(weaponType, active);
        }

        public bool AdvanceAttackFacingV410LikeOriginal(byte enemyDir)
        {
            // AttackObjLink has entered attack radius: retail clears the local
            // destination/path while it rotates. Cancel movement here WITHOUT
            // creating a generic Stand order (that order was the old source of
            // combat-order interference).
            _hasMoveTarget = false;
            _hasLastMoveRequestLikeOriginal = false;
            _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = false;
            _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
            if (SpriteAnimator != null) SpriteAnimator.SetMovingLikeOriginal(false);
            C2NeutralPeasantFallbackMoveV311LikeOriginal mover =
                GetComponent<C2NeutralPeasantFallbackMoveV311LikeOriginal>();
            if (mover != null) mover.CancelLikeOriginal();

            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) return link.AdvanceAttackFacingV410LikeOriginal(enemyDir);
            SetFacingDirectionLikeOriginal(enemyDir);
            return true;
        }

        public bool AdvanceAttackFacingV411LikeOriginal(byte enemyDir, int needState)
        {
            _hasMoveTarget = false;
            _hasLastMoveRequestLikeOriginal = false;
            _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = false;
            _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
            if (SpriteAnimator != null) SpriteAnimator.SetMovingLikeOriginal(false);
            C2NeutralPeasantFallbackMoveV311LikeOriginal mover =
                GetComponent<C2NeutralPeasantFallbackMoveV311LikeOriginal>();
            if (mover != null) mover.CancelLikeOriginal();

            C2UnitOriginalRuntimeLinkLikeOriginal link = RuntimeLink;
            if (link != null) return link.AdvanceAttackFacingV411LikeOriginal(enemyDir, needState);
            SetFacingDirectionLikeOriginal(enemyDir);
            return true;
        }

        public void StopMoveAndFaceDirectionLikeOriginal(byte realDir)
        {
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                this, C2UnitOrderKindV325LikeOriginal.Stand, "stop_move", "face_" + realDir);
            _hasMoveTarget = false;
            _hasLastMoveRequestLikeOriginal = false;
            _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = false;
            _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
            SetFacingDirectionLikeOriginal(realDir);
            if (SpriteAnimator != null) SpriteAnimator.SetMovingLikeOriginal(false);
            C2NeutralPeasantFallbackMoveV311LikeOriginal mover = GetComponent<C2NeutralPeasantFallbackMoveV311LikeOriginal>();
            if (mover != null) mover.CancelLikeOriginal();
        }

        public void C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(bool moving, bool preciseBornPath)
        {
            _hasMoveTarget = moving;
            if (!moving) _hasLastMoveRequestLikeOriginal = false;
            _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = moving && preciseBornPath;
            if (!moving || !preciseBornPath)
            {
                _v170PreciseBornpointMoveIgnoresBuildingBlockLikeOriginal = false;
                _v170PreciseBornpointSourceBuildingRecordLikeOriginal = int.MinValue;
            }
        }

        public void C2NeutralPeasantUnitsV15SetPreciseBornSourceBuildingRecordLikeOriginal(int recordIndex)
        {
            _v170PreciseBornpointSourceBuildingRecordLikeOriginal = recordIndex;
        }

        public int C2NeutralPeasantUnitsV15GetPreciseBornSourceBuildingRecordLikeOriginal()
        {
            return _v170PreciseBornpointSourceBuildingRecordLikeOriginal;
        }

        public bool C2NeutralPeasantUnitsV15HasLineSortOverrideLikeOriginal()
        {
            return _lineSortOverrideOrderLikeOriginal != int.MinValue;
        }

        public void C2NeutralPeasantUnitsV15ApplyLineSortOverrideLikeOriginal(int order)
        {
            _lineSortOverrideOrderLikeOriginal = order;
            SortKey = order;
            if (SpriteMeshRenderer != null)
                SpriteMeshRenderer.sortingOrder = order;
        }

        public void C2NeutralPeasantUnitsV15ClearLineSortOverrideLikeOriginal(int expectedOrder)
        {
            if (_lineSortOverrideOrderLikeOriginal != int.MinValue &&
                expectedOrder != int.MinValue &&
                _lineSortOverrideOrderLikeOriginal != expectedOrder)
                return;

            _lineSortOverrideOrderLikeOriginal = int.MinValue;
        }
    }

// V432 integration probe: declaration supplied by consolidated file.


    public sealed partial class C2BattleTerrainMode
    {
        public const float C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal = 42.0f;

        public bool C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(Vector3 world, out float x, out float y)
        {
            return C2NoUnitWorldToOriginalPixelLikeOriginal(world, out x, out y);
        }

        public Vector3 C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(float x, float y)
        {
            return C2NoUnitOriginalPixelToWorldLikeOriginal(x, y);
        }
    }

    internal sealed class C2NeutralPeasantFallbackMoveV311LikeOriginal : MonoBehaviour
    {
        private static int s_logs;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private float _targetRealX;
        private float _targetRealY;
        private float _speedOriginalPixelsPerSecond;
        private bool _hasFinalFacingDir;
        private byte _finalFacingDir;
        private bool _active;
        private float _pathReal;

        internal void Begin(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float targetRealX,
            float targetRealY,
            float speedOriginalPixelsPerSecond,
            bool hasFinalFacingDir,
            byte finalFacingDir)
        {
            _unit = unit;
            _targetRealX = targetRealX;
            _targetRealY = targetRealY;
            _speedOriginalPixelsPerSecond = speedOriginalPixelsPerSecond > 0.001f
                ? speedOriginalPixelsPerSecond
                : C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal;
            _hasFinalFacingDir = hasFinalFacingDir;
            _finalFacingDir = finalFacingDir;
            _active = _unit != null;
            _pathReal = 0.0f;
            enabled = _active;
            if (_unit != null)
                _unit.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, false);

            if (_unit != null && s_logs < 16)
            {
                s_logs++;
                Debug.Log("[C2:UNIT FALLBACK MOVE V311] no_runtime_link_step_move unit='" + (_unit.SourceMonsterId ?? string.Empty) +
                          "' targetReal=(" + targetRealX.ToString("0", CultureInfo.InvariantCulture) +
                          "," + targetRealY.ToString("0", CultureInfo.InvariantCulture) + ")" +
                          " reason=prevent_build_order_teleport_without_animation");
            }
        }

        private void Update()
        {
            if (!_active || _unit == null)
            {
                enabled = false;
                return;
            }

            float ux = _unit.RealXFloat != 0.0f ? _unit.RealXFloat : _unit.RealX;
            float uy = _unit.RealYFloat != 0.0f ? _unit.RealYFloat : _unit.RealY;
            float dx = _targetRealX - ux;
            float dy = _targetRealY - uy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= 16.0f)
            {
                ApplyRealPosition(_targetRealX, _targetRealY);
                if (_hasFinalFacingDir) _unit.SetFacingDirectionLikeOriginal(_finalFacingDir);
                if (_unit.SpriteAnimator != null) _unit.SpriteAnimator.SetMovingLikeOriginal(false);
                _unit.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false, false);
                _active = false;
                enabled = false;
                return;
            }

            float step = Mathf.Min(dist, Mathf.Max(1.0f, _speedOriginalPixelsPerSecond * 16.0f) * Mathf.Max(0.0f, Time.deltaTime));
            float nx = dx / Mathf.Max(0.0001f, dist);
            float ny = dy / Mathf.Max(0.0001f, dist);
            float nextX = ux + nx * step;
            float nextY = uy + ny * step;
            _pathReal += step;

            byte dir = C2RuntimeDirectionFromRealDeltaV311LikeOriginal(nx, ny);
            _unit.SetFacingDirectionLikeOriginal(dir);
            if (_unit.SpriteAnimator != null)
            {
                _unit.SpriteAnimator.SetMovingLikeOriginal(true);
                _unit.SpriteAnimator.SetMotionStateLikeOriginal(_unit.GraphDir, false);
                _unit.SpriteAnimator.SetWalkPathFrameLikeOriginal(_pathReal, Mathf.Max(1.0f, _unit.MotionDist * 16.0f));
            }
            ApplyRealPosition(nextX, nextY);
        }

        private void ApplyRealPosition(float realX, float realY)
        {
            if (_unit == null) return;
            _unit.RealXFloat = realX;
            _unit.RealYFloat = realY;
            _unit.RealX = Mathf.RoundToInt(realX);
            _unit.RealY = Mathf.RoundToInt(realY);
            C2LiveUnitCellIndex.PositionChanged(_unit);

            if (_unit.OwnerMode != null)
            {
                Vector3 world = _unit.OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(realX / 16.0f, realY / 16.0f);
                _unit.transform.position = world;
            }
        }

        private static byte C2RuntimeDirectionFromRealDeltaV311LikeOriginal(float dx, float dy)
        {
            if ((dx * dx + dy * dy) < 0.000001f) return 0;
            return C2OriginalMovementMathV352.Quantize16(
                C2OriginalMovementMathV352.GetDir(Mathf.RoundToInt(dx), Mathf.RoundToInt(dy)));
        }

        internal void CancelLikeOriginal()
        {
            if (_unit != null)
                _unit.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false, false);
            _active = false;
            enabled = false;
        }
    }
}
