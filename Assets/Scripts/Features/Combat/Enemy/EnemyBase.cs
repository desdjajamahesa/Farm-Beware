using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Base controller for all enemy types in Farm-Beware.
    /// Manages HP, armor, navigation movement, AI state machine, contact attacks, unique skills, and loot drops.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public partial class EnemyBase : MonoBehaviour, IDamageable
    {
        public enum EnemyActionState
        {
            Locomotion,
            ExecutingSkill,
            RetreatingAndHealing,
            Burrowed,
            RootGuarded,
            Dead
        }

        public static event Action<EnemyBase> OnAnyEnemyDied;
        public event Action<int, int> OnHealthChanged;
        public event Action OnActionInterrupted;

        [Header("Enemy Identity")]
        public EnemyType enemyType = EnemyType.TuberMaw;
        [SerializeField] private FeaturesCombat.Data.EnemyData enemyData;
        public FeaturesCombat.Data.EnemyData EnemyData => enemyData;
        public string displayName = "Tuber Maw";
        public bool isBoss = false;
        public bool isSummonedMinion = false;

        public void SetEnemyData(FeaturesCombat.Data.EnemyData data)
        {
            enemyData = data;
            if (enemyData != null)
            {
                enemyType = enemyData.enemyType;
                InitializeStatsByType();
                InitializePoise();
            }
        }

        [Header("Stats")]
        public int maxHealth = 100;
        public int currentHealth;
        public int armor = 20;
        public float moveSpeed = 3f;
        public int contactDamage = 15;
        public float attackRate = 0.4f; // Attacks per second
        public float attackRange = 1.5f;
        public float aggroRange = 6f;
        [Range(0f, 1f)] public float knockbackResistance = 0.2f;

        [Header("Drop Loot Table")]
        public ItemData dropMaterial;
        public int minDropCount = 1;
        public int maxDropCount = 3;
        [Range(0f, 1f)] public float dropChance = 0.5f;
        public int minGold = 200;
        public int maxGold = 500;
        [Range(0f, 1f)] public float goldChance = 0.5f;

        // Internal state
        public bool IsDead => currentHealth <= 0;
        public bool IsPerformingSkill { get => isPerformingSkill; set => isPerformingSkill = value; }
        public bool IsBurrowed => isBurrowed;
        public bool IsRetreatingAndHealing => isRetreatingAndHealing;
        public EnemyActionState CurrentActionState
        {
            get
            {
                if (IsDead) return EnemyActionState.Dead;
                if (isBurrowed) return EnemyActionState.Burrowed;
                if (isRootGuarded) return EnemyActionState.RootGuarded;
                if (isRetreatingAndHealing) return EnemyActionState.RetreatingAndHealing;
                if (isPerformingSkill) return EnemyActionState.ExecutingSkill;
                return EnemyActionState.Locomotion;
            }
        }
        public Transform PlayerTarget => playerTarget;
        public bool IsUnstaggerable { get; set; } = false;
        public bool IsRootGuarded => isRootGuarded;
        public bool IsTrackingLocked { get; private set; }
        public Vector3 LockedStrikeDirection { get; private set; } = Vector3.forward;
        public float CurrentWindupProgress { get; private set; }
        public bool IsInRecoveryPunishWindow { get; private set; }
        private bool _wasStaggerTinted = false;
        private Transform playerTarget;
        private Rigidbody rb;
        private Renderer meshRenderer;
        private Color originalColor;
        private MaterialPropertyBlock mpb;

        private void SetLinearVelocity(Vector3 velocity)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = velocity;
            }
        }

        private void SetAngularVelocity(Vector3 velocity)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.angularVelocity = velocity;
            }
        }

        private float GetLinearVelocityY()
        {
            return (rb != null && !rb.isKinematic) ? rb.linearVelocity.y : 0f;
        }
        private float lastAttackTime = 0f;
        private float skillCooldownTimer = 0f;
        private bool isPerformingSkill = false;
        private bool isBurrowed = false;
        private bool isRootGuarded = false;
        private int originalArmor;

        private FeaturesCombat.Core.PureLogic.PoiseTracker poiseTracker;
        public FeaturesCombat.Core.PureLogic.PoiseTracker PoiseTracker
        {
            get
            {
                if (poiseTracker == null)
                {
                    InitializePoise();
                }
                return poiseTracker;
            }
        }
        private FeaturesCombat.Adapters.HitFeedbackRenderer hitFeedback;
        private FeaturesCombat.Core.Physics.AirborneHazardEntity airborneHazard;

        public void InitializePoise()
        {
            var profile = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.GetProfile(enemyType);
            float maxP = enemyData != null ? enemyData.maxPoise : profile.MaxPoise;
            float regenR = enemyData != null ? enemyData.poiseRegenRate : profile.PoiseRegenRate;
            float regenD = enemyData != null ? enemyData.poiseRegenDelay : profile.PoiseRegenDelay;
            bool hasSA = enemyData != null ? enemyData.hasSuperArmor : profile.InnateSuperArmor;

            if (poiseTracker == null)
            {
                poiseTracker = new FeaturesCombat.Core.PureLogic.PoiseTracker(maxP, regenR, regenD, hasSA);
            }
            else
            {
                poiseTracker.Initialize(maxP, regenR, regenD, hasSA);
            }
        }

        [Header("Low HP Retreat & Heal Mechanic")]
        [Tooltip("Jarak aman mundur saat HP sekarat sebelum berhenti untuk memulihkan diri.")]
        [SerializeField] private float retreatSafeDistance = 7.0f;
        [Tooltip("Batas durasi maksimal mundur sebelum memaksakan heal (mencegah kabur tanpa henti).")]
        [SerializeField] private float maxRetreatDuration = 1.8f;
        [Tooltip("Cooldown antar eksekusi retreat & heal.")]
        [SerializeField] private float retreatHealCooldown = 15f;
        [Tooltip("Jumlah target current health yang dipulihkan saat heal (default: 50).")]
        [SerializeField] private int retreatHealTargetHealth = 50;
        private bool isRetreatingAndHealing = false;
        private float lastRetreatHealTime = -99f;

        [Header("NavMesh & Obstacle Avoidance")]
        [SerializeField] private float repathInterval = 0.25f;
        [SerializeField] private float cornerReachThreshold = 0.8f;
        [SerializeField] private float obstacleAvoidanceDistance = 1.35f;
        [SerializeField] private float whiskerAngle = 35f;

        private NavMeshPath navMeshPath;
        private readonly Vector3[] _pathCornersBuffer = new Vector3[16];
        private int _pathCornerCount = 0;
        private static readonly RaycastHit[] _beamHitsBuffer = new RaycastHit[16];
        private int currentPathIndex = 1;
        private float nextRepathTime = 0f;
        private bool hasValidNavPath = false;
        private Vector3 smoothedAvoidanceDir = Vector3.forward;
        private Vector3 lastUnstuckPosition;
        private float stuckDuration = 0f;
        private Vector3 unstuckEvasionVector = Vector3.zero;

        [Header("Tactical AI & Pack Dynamics")]
        private FeaturesCombat.Core.PureLogic.MonsterTacticalEvaluator tacticalEvaluator;
        public FeaturesCombat.Core.PureLogic.MonsterTacticalEvaluator TacticalEvaluator => tacticalEvaluator;
        private FeaturesCombat.Core.PureLogic.MonsterTacticalState currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.IdlePatrol;
        public FeaturesCombat.Core.PureLogic.MonsterTacticalState CurrentTacticalState => currentTacticalState;
        private float moraleSurgeTimer = 0f;
        private float moraleSurgeSpeedMult = 1.0f;
        private Vector3 spawnAnchorPosition;
        private float nextAmbientWanderTime = 0f;
        private Vector3 currentAmbientWanderTarget;

        [Header("Telegraph & Attack Visuals")]
        private CombatTelegraphDecal combatTelegraph;
        private LineRenderer laserSightLine;
        private static Material _sharedLaserSightMaterial;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            meshRenderer = GetComponentInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                originalColor = meshRenderer.sharedMaterial.HasProperty("_BaseColor")
                    ? meshRenderer.sharedMaterial.GetColor("_BaseColor")
                    : meshRenderer.sharedMaterial.color;
            }

            originalArmor = armor;
            InitializeStatsByType();
            InitializePoise();
            currentHealth = maxHealth;

            hitFeedback = GetComponent<FeaturesCombat.Adapters.HitFeedbackRenderer>() ?? gameObject.AddComponent<FeaturesCombat.Adapters.HitFeedbackRenderer>();
            airborneHazard = GetComponent<FeaturesCombat.Core.Physics.AirborneHazardEntity>() ?? gameObject.AddComponent<FeaturesCombat.Core.Physics.AirborneHazardEntity>();

            tacticalEvaluator = new FeaturesCombat.Core.PureLogic.MonsterTacticalEvaluator();
            spawnAnchorPosition = transform.position;
            currentAmbientWanderTarget = transform.position;

            navMeshPath = new NavMeshPath();
            nextRepathTime = 0f;
        }

        private void Start()
        {
            FindPlayerTarget();
            EnsureTelegraphElements();
        }

        private void OnEnable()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = transform.position;
                rb.rotation = transform.rotation;
                SetLinearVelocity(Vector3.zero);
                SetAngularVelocity(Vector3.zero);
            }
            Physics.SyncTransforms();

            spawnAnchorPosition = transform.position;
            currentAmbientWanderTarget = transform.position;
            moraleSurgeTimer = 0f;
            moraleSurgeSpeedMult = 1.0f;
            OnAnyEnemyDied += HandlePackMemberDied;
        }

        private void OnDisable()
        {
            OnAnyEnemyDied -= HandlePackMemberDied;
            if (combatTelegraph != null) combatTelegraph.Hide();
            if (laserSightLine != null) laserSightLine.enabled = false;
            isRetreatingAndHealing = false;
            moraleSurgeTimer = 0f;
            moraleSurgeSpeedMult = 1.0f;

            // Audit Hardening: Release from HitstopCoordinator to prevent dangling slot references in object pools
            FeaturesCombat.Adapters.HitstopCoordinator.Instance?.Unregister(gameObject, false);

            // Audit Hardening: Release all tokens held by this entity across melee and ranged pools
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseAllForEntity(gameObject.GetInstanceID());

            // Audit Hardening: Deactivate airborne hazard state if disabled/pooled mid-air
            if (airborneHazard != null)
            {
                airborneHazard.DeactivateHazard();
            }
        }

        private void EnsureTelegraphElements()
        {
            if (combatTelegraph == null)
            {
                var decalObj = new GameObject("CombatTelegraph");
                decalObj.transform.SetParent(transform, false);
                combatTelegraph = decalObj.AddComponent<CombatTelegraphDecal>();
                combatTelegraph.Initialize(Mathf.Max(1.2f, attackRange));
            }

            if ((enemyType == EnemyType.CornMusketeer || enemyType == EnemyType.TheRanger || enemyType == EnemyType.CyclopsTuberMaw) && laserSightLine == null)
            {
                var laserObj = new GameObject("LaserSight");
                laserObj.transform.SetParent(transform, false);
                laserSightLine = laserObj.AddComponent<LineRenderer>();
                laserSightLine.useWorldSpace = true;
                laserSightLine.startWidth = 0.04f;
                laserSightLine.endWidth = 0.02f;
                laserSightLine.positionCount = 2;

                if (_sharedLaserSightMaterial == null)
                {
                    Shader shader = ShaderFallbackResolver.ResolveUnlitShader();
                    _sharedLaserSightMaterial = new Material(shader);
                    Color laserColor = new Color(1f, 0.85f, 0.1f, 0.85f);
                    _sharedLaserSightMaterial.color = laserColor;
                    if (_sharedLaserSightMaterial.HasProperty("_BaseColor")) _sharedLaserSightMaterial.SetColor("_BaseColor", laserColor);
                }
                laserSightLine.sharedMaterial = _sharedLaserSightMaterial;
                laserSightLine.enabled = false;
            }
        }

        private void FindPlayerTarget()
        {
            if (ServiceLocator.TryResolve<IPlayerContext>(out var playerCtx) && playerCtx != null && playerCtx.Transform != null)
            {
                playerTarget = playerCtx.Transform;
                return;
            }

            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (p != null) playerTarget = p.transform;
        }

        public void InitializeStatsByType()
        {
            if (enemyData != null)
            {
                enemyType = enemyData.enemyType;
                displayName = enemyData.displayName;
                isBoss = enemyData.isBoss;
                maxHealth = enemyData.maxHealth;
                armor = enemyData.armor;
                moveSpeed = enemyData.moveSpeed;
                contactDamage = enemyData.contactDamage;
                attackRate = enemyData.attackRate;
                attackRange = enemyData.attackRange;
                aggroRange = enemyData.aggroRange;
                knockbackResistance = enemyData.knockbackResistance;
                minGold = enemyData.minGold;
                maxGold = enemyData.maxGold;
                goldChance = enemyData.goldChance;

                // Sanitize attack range for melee vs ranged
                if (enemyType == EnemyType.TheRanger) attackRange = Mathf.Max(10f, attackRange);
                else if (enemyType == EnemyType.CyclopsTuberMaw) attackRange = Mathf.Min(2.4f, attackRange);
                else if (enemyType == EnemyType.TaroBrute) attackRange = Mathf.Min(1.8f, attackRange);
                else if (enemyType == EnemyType.TaroColossus) attackRange = Mathf.Min(2.4f, attackRange);
                else if (enemyType == EnemyType.TuberMaw) attackRange = Mathf.Min(1.5f, attackRange);

                originalArmor = armor;
                currentHealth = maxHealth;
                if (tacticalEvaluator != null)
                {
                    tacticalEvaluator.OrbitMinRadius = Mathf.Max(1.4f, attackRange + 0.4f);
                    tacticalEvaluator.OrbitMaxRadius = Mathf.Max(2.4f, attackRange + 1.4f);
                }
                return;
            }

            var db = ItemDatabase.Instance;

            switch (enemyType)
            {
                case EnemyType.TuberMaw:
                    displayName = "Tuber Maw";
                    maxHealth = 100;
                    armor = 20;
                    moveSpeed = 3f;
                    contactDamage = 15;
                    attackRate = 0.4f;
                    attackRange = 1.4f;
                    aggroRange = 6f;
                    knockbackResistance = 0.20f;
                    isBoss = false;
                    dropMaterial = db?.GetItem("mat_mutated_root");
                    minDropCount = 1; maxDropCount = 3; dropChance = 0.5f;
                    minGold = 200; maxGold = 500; goldChance = 0.5f;
                    break;

                case EnemyType.CyclopsTuberMaw:
                    displayName = "Cyclops Tuber Maw";
                    maxHealth = 600;
                    armor = 40;
                    moveSpeed = 3f;
                    contactDamage = 30;
                    attackRate = 0.4f;
                    attackRange = 2.4f;
                    aggroRange = 12f;
                    knockbackResistance = 0.35f;
                    isBoss = true;
                    dropMaterial = db?.GetItem("mat_cyclops_eye");
                    minDropCount = 1; maxDropCount = 2; dropChance = 0.30f;
                    minGold = 700; maxGold = 1000; goldChance = 0.70f;
                    break;

                case EnemyType.TaroBrute:
                    displayName = "Taro Brute";
                    maxHealth = 150;
                    armor = 15;
                    moveSpeed = 1.5f;
                    contactDamage = 25;
                    attackRate = 0.5f;
                    attackRange = 1.8f;
                    aggroRange = 6f;
                    knockbackResistance = 0.80f;
                    isBoss = false;
                    dropMaterial = db?.GetItem("mat_hardened_root");
                    minDropCount = 1; maxDropCount = 3; dropChance = 0.5f;
                    minGold = 200; maxGold = 500; goldChance = 0.5f;
                    break;

                case EnemyType.TaroColossus:
                    displayName = "Taro Colossus";
                    maxHealth = 900;
                    armor = 55;
                    moveSpeed = 2f;
                    contactDamage = 45;
                    attackRate = 0.5f;
                    attackRange = 2.4f;
                    aggroRange = 10f;
                    knockbackResistance = 1.0f; // Immune to knockback
                    isBoss = true;
                    dropMaterial = db?.GetItem("mat_colossus_core");
                    minDropCount = 1; maxDropCount = 1; dropChance = 0.20f;
                    minGold = 700; maxGold = 1000; goldChance = 0.80f;
                    break;

                case EnemyType.CornMusketeer:
                    displayName = "Corn Musketeer";
                    maxHealth = 85;
                    armor = 0;
                    moveSpeed = 2.8f;
                    contactDamage = 15; // MVP projectile shot damage (15 DMG)
                    attackRate = 0.5f; // 1 shot every 2 seconds
                    attackRange = 12f; // Ranged attack range
                    aggroRange = 16f;
                    knockbackResistance = 0.15f;
                    isBoss = false;
                    dropMaterial = db?.GetItem("mat_kernel_shrapnel");
                    minDropCount = 1; maxDropCount = 3; dropChance = 0.5f;
                    minGold = 200; maxGold = 500; goldChance = 0.5f;
                    break;

                case EnemyType.TheRanger:
                    displayName = "The Ranger";
                    maxHealth = 750;
                    armor = 30;
                    moveSpeed = 0f; // Stationary turret
                    contactDamage = 60;
                    attackRate = 0.5f;
                    attackRange = 10f;
                    aggroRange = 15f;
                    knockbackResistance = 1.0f; // Immune
                    isBoss = true;
                    dropMaterial = db?.GetItem("mat_cob_core");
                    minDropCount = 1; maxDropCount = 2; dropChance = 0.15f;
                    minGold = 700; maxGold = 1000; goldChance = 0.85f;
                    break;
            }

            originalArmor = armor;
            currentHealth = maxHealth;
            if (tacticalEvaluator != null)
            {
                tacticalEvaluator.OrbitMinRadius = Mathf.Max(1.4f, attackRange + 0.4f);
                tacticalEvaluator.OrbitMaxRadius = Mathf.Max(2.4f, attackRange + 1.4f);
            }
        }

        private void Update()
        {
            if (IsDead) return;

            if (poiseTracker != null)
            {
                poiseTracker.Tick(Time.deltaTime);
                if (poiseTracker.IsStaggered)
                {
                    if (!_wasStaggerTinted && meshRenderer != null)
                    {
                        SetRendererColor(new Color(1f, 0.78f, 0.22f));
                        _wasStaggerTinted = true;
                    }
                    return;
                }
                else if (_wasStaggerTinted)
                {
                    _wasStaggerTinted = false;
                    if (!isPerformingSkill && !isRetreatingAndHealing && meshRenderer != null)
                    {
                        SetRendererColor(originalColor);
                    }
                }
            }

            if (isPerformingSkill) return;

            // 1. Safe Zone Anti-Penetration Bounds: Prevent monsters from entering or clipping inside the modular house interior
            if (NightBrawlManager.IsInsideHouse(transform.position))
            {
                Vector3 safeOutdoor = NightBrawlManager.GetNearestOutdoorPosition(transform.position, 1.5f);
                if (rb != null)
                {
                    rb.position = safeOutdoor;
                    SetLinearVelocity(Vector3.zero);
                }
                transform.position = safeOutdoor;
            }

            if (playerTarget == null)
            {
                FindPlayerTarget();
                return;
            }

            skillCooldownTimer += Time.deltaTime;

            if (moraleSurgeTimer > 0f)
            {
                moraleSurgeTimer -= Time.deltaTime;
                if (moraleSurgeTimer <= 0f)
                {
                    moraleSurgeSpeedMult = 1.0f;
                }
            }

            bool isPlayerInsideHouse = NightBrawlManager.IsInsideHouse(playerTarget.position);
            Vector3 effectiveTargetPos = isPlayerInsideHouse 
                ? NightBrawlManager.GetNearestOutdoorPosition(playerTarget.position, 2.5f) 
                : playerTarget.position;

            // Compute planar horizontal distance (Delta XZ) so large-scale bosses aren't skewed by pivot height differences (Delta Y)
            Vector3 diffTarget = effectiveTargetPos - transform.position;
            diffTarget.y = 0f;
            float distToTarget = diffTarget.magnitude;

            Vector3 diffPlayer = playerTarget.position - transform.position;
            diffPlayer.y = 0f;
            float distToPlayer = diffPlayer.magnitude;

            // During Night Brawl, monsters are always aggroed and actively pursue the player regardless of aggro range
            bool isNightActive = (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                                 (NightBrawlManager.Instance != null && NightBrawlManager.Instance.IsNightBrawlActive);

            bool isAggroed = isNightActive || (distToPlayer <= aggroRange && !isPlayerInsideHouse);

            // Evaluate AI behavior
            if (isAggroed)
            {
                // Check low-HP retreat and heal behavior for TuberMaw
                if (enemyType == EnemyType.TuberMaw && currentHealth <= Mathf.RoundToInt(maxHealth * 0.25f) && !isRetreatingAndHealing && Time.time >= lastRetreatHealTime + retreatHealCooldown)
                {
                    currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.RetreatHeal;
                    StartCoroutine(RoutineRetreatAndHeal());
                    return;
                }

                bool isDirectPathBlocked = !isPlayerInsideHouse && NavMesh.Raycast(transform.position, effectiveTargetPos, out NavMeshHit navHit, NavMesh.AllAreas);
                if (!isDirectPathBlocked)
                {
                    LookAtTarget(effectiveTargetPos);
                }

                // Check special skill execution when cooldown is ready (only if player is not safely indoors)
                bool canExecuteSkill = false;
                switch (enemyType)
                {
                    case EnemyType.TuberMaw:
                        canExecuteSkill = distToPlayer >= 2.5f && distToPlayer <= 10.0f; // Gap closer
                        break;
                    case EnemyType.CyclopsTuberMaw:
                        canExecuteSkill = distToPlayer <= 14.0f; // Tracking beam
                        break;
                    case EnemyType.TaroBrute:
                        canExecuteSkill = distToPlayer <= 5.0f; // Root guard defensive
                        break;
                    case EnemyType.TaroColossus:
                        canExecuteSkill = distToPlayer <= 7.0f; // Airborne slam
                        break;
                    case EnemyType.CornMusketeer:
                        canExecuteSkill = distToPlayer >= 2.5f && distToPlayer <= 11.5f; // Kernel shot (on-screen frustum range)
                        break;
                    case EnemyType.TheRanger:
                        canExecuteSkill = distToPlayer <= 16.0f; // Heavens fall
                        break;
                    default:
                        canExecuteSkill = distToPlayer <= 10.0f;
                        break;
                }

                if (canExecuteSkill && skillCooldownTimer >= GetSkillCooldown())
                {
                    currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.TelegraphAttack;
                    TryExecuteSkill(distToPlayer);
                }
                else
                {
                    bool isRanged = (enemyType == EnemyType.CornMusketeer || enemyType == EnemyType.TheRanger);

                    // Check if entity already holds an active unexpired attack token
                    bool hasToken = (NightBrawlManager.Instance != null && NightBrawlManager.Instance.TokenDispatcher != null)
                        ? NightBrawlManager.Instance.TokenDispatcher.HasToken(gameObject.GetInstanceID(), isRanged, Time.time)
                        : true;

                    bool isAttackReady = (Time.time - lastAttackTime >= (1f / Mathf.Max(0.1f, attackRate))) && !isPerformingSkill && !isPlayerInsideHouse;

                    // Anticipatory Token Acquisition:
                    // If monster does not hold a token, its attack cooldown is ready, and it is within immediate combat approach (attackRange + 1.4m),
                    // attempt to acquire an attack token so it charges directly into melee range rather than freezing in orbit!
                    float approachThreshold = Mathf.Max(2.4f, attackRange + 1.4f);
                    if (!hasToken && isAttackReady)
                    {
                        if (distToPlayer <= approachThreshold && NightBrawlManager.Instance?.TokenDispatcher != null)
                        {
                            var priority = isBoss ? FeaturesCombat.Core.PureLogic.AttackTokenPriority.Boss : FeaturesCombat.Core.PureLogic.AttackTokenPriority.Minion;
                            hasToken = NightBrawlManager.Instance.TokenDispatcher.TryAcquireToken(
                                gameObject.GetInstanceID(), isRanged, Time.time, 1.8f, priority, distToPlayer, isCommitted: false);
                        }
                    }

                    if (tacticalEvaluator == null)
                    {
                        tacticalEvaluator = new FeaturesCombat.Core.PureLogic.MonsterTacticalEvaluator();
                    }

                    Vector3 tacticalDir = tacticalEvaluator.EvaluateMovement(
                        transform.position,
                        effectiveTargetPos,
                        distToPlayer,
                        hasToken,
                        isRanged,
                        gameObject.GetInstanceID(),
                        attackRange,
                        isAttackReady,
                        out currentTacticalState);

                    // Obstacle Gating: If an obstacle (fence, wall, closed door) blocks the direct line
                    // to the target on the NavMesh, do not orbit against the barrier. Pathfind directly around it!
                    if (isDirectPathBlocked && currentTacticalState == FeaturesCombat.Core.PureLogic.MonsterTacticalState.FlankOrbit)
                    {
                        currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.EngageDirect;
                    }

                    if (currentTacticalState == FeaturesCombat.Core.PureLogic.MonsterTacticalState.TangentialKite)
                    {
                        // Ranged unit evasive kiting: move along tangential evasive vector
                        Vector3 retreatPos = transform.position + tacticalDir * 4.2f;
                        ChaseTarget(retreatPos, 1.15f * moraleSurgeSpeedMult);
                    }
                    else if (!isPlayerInsideHouse && !isDirectPathBlocked && distToPlayer <= attackRange)
                    {
                        TryPerformAttack(distToPlayer);
                        if (currentTacticalState == FeaturesCombat.Core.PureLogic.MonsterTacticalState.FlankOrbit && moveSpeed > 0f && !isBurrowed)
                        {
                            // Awaiting attack token in combat ring: actively orbit and flank instead of standing still
                            Vector3 orbitTarget = transform.position + tacticalDir * 3.0f;
                            ChaseTarget(orbitTarget, tacticalEvaluator.DefaultOrbitSpeedFactor * moraleSurgeSpeedMult);
                        }
                    }
                    else if (currentTacticalState == FeaturesCombat.Core.PureLogic.MonsterTacticalState.FlankOrbit && moveSpeed > 0f && !isBurrowed)
                    {
                        // Flank and circle around player in combat ring awaiting attack opportunity
                        Vector3 orbitTarget = transform.position + tacticalDir * 3.0f;
                        ChaseTarget(orbitTarget, tacticalEvaluator.DefaultOrbitSpeedFactor * moraleSurgeSpeedMult);
                    }
                    else if (moveSpeed > 0f && !isBurrowed)
                    {
                        ChaseTarget(effectiveTargetPos, moraleSurgeSpeedMult);
                    }
                }
            }
            else
            {
                // Ambient Idle Wander: patrol within anchor radius instead of freezing
                currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.IdlePatrol;
                if (moveSpeed > 0f)
                {
                    if (Time.time >= nextAmbientWanderTime)
                    {
                        nextAmbientWanderTime = Time.time + UnityEngine.Random.Range(4f, 7.5f);
                        float randomAngle = UnityEngine.Random.Range(0f, 360f);
                        float randomDist = UnityEngine.Random.Range(0.2f, 1f);
                        if (spawnAnchorPosition == Vector3.zero) spawnAnchorPosition = transform.position;
                        currentAmbientWanderTarget = FeaturesCombat.Core.PureLogic.MonsterTacticalEvaluator.CalculateAmbientWanderTarget(
                            spawnAnchorPosition, 3.5f, randomAngle, randomDist);
                    }

                    float distToWander = (currentAmbientWanderTarget - transform.position).magnitude;
                    if (distToWander > 0.6f)
                    {
                        ChaseTarget(currentAmbientWanderTarget, 0.35f);
                    }
                    else if (rb != null)
                    {
                        SetLinearVelocity(new Vector3(0f, GetLinearVelocityY(), 0f));
                    }
                }
                else if (rb != null)
                {
                    SetLinearVelocity(new Vector3(0f, GetLinearVelocityY(), 0f));
                }
            }
        }

        private void ChaseTarget(Vector3 targetPos, float speedMultiplier = 1.0f)
        {
            if (rb == null || rb.isKinematic) return;

            Vector3 desiredDir;

            // 1. Periodically compute smart NavMesh path (repath interval)
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + repathInterval;

                Vector3 sampleStart = transform.position;
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hitStart, 2.0f, NavMesh.AllAreas))
                {
                    sampleStart = hitStart.position;
                }

                Vector3 sampleTarget = targetPos;
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hitTarget, 3.0f, NavMesh.AllAreas))
                {
                    sampleTarget = hitTarget.position;
                }

                hasValidNavPath = NavMesh.CalculatePath(sampleStart, sampleTarget, NavMesh.AllAreas, navMeshPath);
                _pathCornerCount = (hasValidNavPath && navMeshPath != null) ? navMeshPath.GetCornersNonAlloc(_pathCornersBuffer) : 0;
                currentPathIndex = 1; // Index 0 is the monster's current start position
            }

            // 2. Traverse path corner waypoints if valid (Zero-GC non-alloc buffer)
            if (hasValidNavPath && navMeshPath != null && _pathCornerCount > 1 && navMeshPath.status != NavMeshPathStatus.PathInvalid)
            {
                while (currentPathIndex < _pathCornerCount - 1)
                {
                    Vector3 toCorner = _pathCornersBuffer[currentPathIndex] - transform.position;
                    toCorner.y = 0f;
                    if (toCorner.sqrMagnitude <= cornerReachThreshold * cornerReachThreshold)
                    {
                        currentPathIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                int targetIndex = Mathf.Min(currentPathIndex, _pathCornerCount - 1);
                Vector3 nextCorner = _pathCornersBuffer[targetIndex];
                desiredDir = (nextCorner - transform.position);
                desiredDir.y = 0f;
                if (desiredDir.sqrMagnitude > 0.01f)
                {
                    desiredDir.Normalize();
                }
                else
                {
                    desiredDir = (targetPos - transform.position).normalized;
                }
            }
            else
            {
                // Fallback: direct direction if NavMesh is unavailable or recalculating
                desiredDir = (targetPos - transform.position).normalized;
            }

            desiredDir.y = 0f;

            // 3. Apply Dynamic Obstacle Avoidance (Whisker Sensors) to steer around obstacles and other monsters
            Vector3 finalMoveDir = ApplyObstacleAvoidance(desiredDir);

            // 4. Anti-Stuck & Physical Collider Obstacle Auto-Recovery
            float effectiveSpeed = moveSpeed * speedMultiplier;
            float deltaPos = Vector3.Distance(transform.position, lastUnstuckPosition);
            if (deltaPos < 0.08f && effectiveSpeed > 0.5f)
            {
                stuckDuration += Time.deltaTime;
                if (stuckDuration > 0.35f)
                {
                    if (unstuckEvasionVector == Vector3.zero)
                    {
                        Vector3 lateral = Vector3.Cross(Vector3.up, desiredDir);
                        unstuckEvasionVector = ((gameObject.GetInstanceID() % 2 == 0) ? lateral : -lateral).normalized;
                    }
                    finalMoveDir = (desiredDir * 0.2f + unstuckEvasionVector * 0.8f).normalized;

                    if (stuckDuration > 1.2f)
                    {
                        if (NavMesh.SamplePosition(transform.position + unstuckEvasionVector * 1.5f, out NavMeshHit unHit, 3.0f, NavMesh.AllAreas))
                        {
                            transform.position = unHit.position;
                            if (rb != null) rb.position = unHit.position;
                        }
                        stuckDuration = 0f;
                        unstuckEvasionVector = Vector3.zero;
                    }
                }
            }
            else
            {
                stuckDuration = Mathf.Max(0f, stuckDuration - Time.deltaTime * 2f);
                if (stuckDuration <= 0f) unstuckEvasionVector = Vector3.zero;
                lastUnstuckPosition = transform.position;
            }

            var profile = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.GetProfile(enemyType);
            Vector3 targetHorizVel = new Vector3(finalMoveDir.x * effectiveSpeed, 0f, finalMoveDir.z * effectiveSpeed);
            Vector3 currentHorizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float accel = profile.MoveAcceleration > 0.1f ? profile.MoveAcceleration : 16f;
            Vector3 smoothedHorizVel = Vector3.MoveTowards(currentHorizVel, targetHorizVel, accel * Time.deltaTime);
            SetLinearVelocity(new Vector3(smoothedHorizVel.x, GetLinearVelocityY(), smoothedHorizVel.z));

            // Smoothly rotate monster to face its navigation movement direction at archetype turn rate
            if (finalMoveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(finalMoveDir);
                float maxStepDeg = (profile.TurnSpeedDegPerSec > 0f ? profile.TurnSpeedDegPerSec : 240f) * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxStepDeg);
            }
        }

        /// <summary>
        /// Dynamic obstacle avoidance sensor (Whisker Raycast Avoidance).
        /// Detects solid obstacles or nearby monsters ahead and steers movement direction
        /// so pack movement feels organic and avoids getting stuck in tight corners.
        /// </summary>
        private Vector3 ApplyObstacleAvoidance(Vector3 forwardMoveDir)
        {
            if (forwardMoveDir.sqrMagnitude < 0.001f) return forwardMoveDir;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            float checkDist = obstacleAvoidanceDistance;

            // Check if a solid obstacle is directly ahead (ignore the target player character)
            bool blockedForward = false;
            if (Physics.SphereCast(origin, 0.35f, forwardMoveDir, out RaycastHit forwardHit, checkDist, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            {
                if (playerTarget == null || (forwardHit.collider.gameObject != playerTarget.gameObject && !forwardHit.collider.transform.IsChildOf(playerTarget)))
                {
                    blockedForward = true;
                }
            }

            if (!blockedForward)
            {
                smoothedAvoidanceDir = forwardMoveDir;
                return forwardMoveDir;
            }

            // Left & right whisker sensors
            Vector3 leftWhisker = Quaternion.Euler(0f, -whiskerAngle, 0f) * forwardMoveDir;
            Vector3 rightWhisker = Quaternion.Euler(0f, whiskerAngle, 0f) * forwardMoveDir;

            bool leftBlocked = Physics.Raycast(origin, leftWhisker, checkDist, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
            bool rightBlocked = Physics.Raycast(origin, rightWhisker, checkDist, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);

            Vector3 steerDir = forwardMoveDir;
            if (!leftBlocked && rightBlocked)
            {
                steerDir = leftWhisker;
            }
            else if (leftBlocked && !rightBlocked)
            {
                steerDir = rightWhisker;
            }
            else if (!leftBlocked && !rightBlocked)
            {
                // Deterministic tie-breaker per instance eliminates frame-to-frame whisker zig-zagging
                steerDir = ((gameObject.GetInstanceID() & 1) == 0) ? leftWhisker : rightWhisker;
            }
            else
            {
                // Both sensors blocked: turn more sharply (70 degrees)
                steerDir = Quaternion.Euler(0f, 70f, 0f) * forwardMoveDir;
            }

            steerDir.y = 0f;
            steerDir.Normalize();

            smoothedAvoidanceDir = Vector3.Slerp(smoothedAvoidanceDir, steerDir, Time.deltaTime * 12f);
            return smoothedAvoidanceDir;
        }

        private void LookAtTarget(Vector3 targetPos)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
            }
        }

        // --- IDAMAGEABLE IMPLEMENTATION ---

        public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            TakeDamageInternal(damage, hitPoint, hitDirection, -1f, -1f, -1f);
        }

        public void TakeDamage(
            int damage,
            Vector3 hitPoint,
            Vector3 hitDirection,
            float explicitPoiseDamage,
            float explicitKnockbackForce,
            float explicitImpulse)
        {
            TakeDamageInternal(damage, hitPoint, hitDirection, explicitPoiseDamage, explicitKnockbackForce, explicitImpulse);
        }

        private void TakeDamageInternal(
            int damage,
            Vector3 hitPoint,
            Vector3 hitDirection,
            float explicitPoiseDamage,
            float explicitKnockbackForce,
            float explicitImpulse)
        {
            if (IsDead) return;

            // GodMode one-hit kill bypass
            if (damage >= 9999)
            {
                currentHealth = 0;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Die();
                return;
            }

            // Armor-based damage reduction
            int effectiveDmg = Mathf.Max(1, damage - Mathf.RoundToInt(armor * 0.25f));
            currentHealth -= effectiveDmg;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Visual hit flash (Zero-GC via MaterialPropertyBlock)
            if (hitFeedback != null) hitFeedback.TriggerFlash();

            Vector3 pushDir = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : -transform.forward;
            var profile = FeaturesCombat.Core.PureLogic.EnemyCombatProfile.GetProfile(enemyType);
            float kbRes = Mathf.Max(knockbackResistance, profile.KnockbackResistance * 0.5f);
            if (knockbackResistance >= 0.99f) kbRes = 1.0f;

            // Apply tiered poise, stagger interruption, token eviction, and single-source knockback
            if (poiseTracker != null)
            {
                float poiseDmg = explicitPoiseDamage >= 0f ? explicitPoiseDamage : Mathf.Max(10f, damage * 0.75f);
                float derivedImpulse = effectiveDmg >= 45 ? 2.1f : (effectiveDmg >= 30 ? 1.4f : 1.0f);
                float impulse = explicitImpulse >= 0f
                    ? Mathf.Max(explicitImpulse, derivedImpulse)
                    : Mathf.Max(hitDirection.magnitude, derivedImpulse);
                var staggerTier = poiseTracker.ApplyPoiseDamage(poiseDmg, impulse, out bool poiseBroken);

                if ((poiseBroken || staggerTier != FeaturesCombat.Core.PureLogic.StaggerTier.None) && !IsUnstaggerable)
                {
                    if (profile.EvictTokenOnStagger)
                    {
                        NightBrawlManager.Instance?.TokenDispatcher?.ReleaseAllForEntity(gameObject.GetInstanceID());
                    }

                    if (staggerTier >= FeaturesCombat.Core.PureLogic.StaggerTier.HeavyStagger && isPerformingSkill && !isBurrowed)
                    {
                        StopAllCoroutines();
                        isPerformingSkill = false;
                        isRetreatingAndHealing = false;
                        IsTrackingLocked = false;
                        CurrentWindupProgress = 0f;
                        IsInRecoveryPunishWindow = false;
                        if (isRootGuarded)
                        {
                            armor = originalArmor;
                            isRootGuarded = false;
                        }
                        if (meshRenderer != null) SetRendererColor(originalColor);
                        if (combatTelegraph != null) combatTelegraph.Hide();
                        if (laserSightLine != null) laserSightLine.enabled = false;
                        OnActionInterrupted?.Invoke();
                    }
                }

                if (rb != null && !IsUnstaggerable && kbRes < 1f)
                {
                    if (staggerTier == FeaturesCombat.Core.PureLogic.StaggerTier.Knockdown)
                    {
                        float launchMag = (explicitKnockbackForce > 0f ? Mathf.Clamp(explicitKnockbackForce * 1.8f, 6f, 12f) : 10f) * (1f - kbRes);
                        if (launchMag > 0.25f)
                        {
                            if (airborneHazard != null)
                            {
                                airborneHazard.LaunchAsHazard(pushDir * launchMag + Vector3.up * 3.5f);
                            }
                            else
                            {
                                rb.AddForce(pushDir * launchMag + Vector3.up * 3f, ForceMode.Impulse);
                            }
                        }
                    }
                    else
                    {
                        float baseKb = explicitKnockbackForce >= 0f
                            ? explicitKnockbackForce
                            : (staggerTier == FeaturesCombat.Core.PureLogic.StaggerTier.HeavyStagger ? 6f :
                               (staggerTier == FeaturesCombat.Core.PureLogic.StaggerTier.MicroStagger ? 2.5f : 0f));
                        float finalKb = baseKb * (1f - kbRes);
                        if (finalKb > 0.05f)
                        {
                            Vector3 kbImpulse = (pushDir + Vector3.up * 0.15f).normalized * finalKb;
                            if (FeaturesCombat.Adapters.HitstopCoordinator.Instance != null)
                            {
                                FeaturesCombat.Adapters.HitstopCoordinator.Instance.QueueKnockbackOrApply(gameObject, kbImpulse);
                            }
                            else if (!rb.isKinematic)
                            {
                                rb.AddForce(kbImpulse, ForceMode.Impulse);
                            }
                        }
                    }
                }
            }
            else if (rb != null && kbRes < 1f && !IsUnstaggerable)
            {
                float actualKb = (explicitKnockbackForce >= 0f ? explicitKnockbackForce : 6f) * (1f - kbRes);
                if (actualKb > 0.05f)
                {
                    Vector3 kbImpulse = (pushDir + Vector3.up * 0.15f).normalized * actualKb;
                    if (FeaturesCombat.Adapters.HitstopCoordinator.Instance != null)
                    {
                        FeaturesCombat.Adapters.HitstopCoordinator.Instance.QueueKnockbackOrApply(gameObject, kbImpulse);
                    }
                    else if (!rb.isKinematic)
                    {
                        rb.AddForce(kbImpulse, ForceMode.Impulse);
                    }
                }
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }



        private void Die()
        {
            currentHealth = 0;

            StopAllCoroutines();
            OnActionInterrupted?.Invoke();

            // Immediately disable collider so dead monster doesn't block player or absorb subsequent attacks
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (rb != null)
            {
                SetLinearVelocity(Vector3.zero);
                SetAngularVelocity(Vector3.zero);
                rb.isKinematic = true;
            }

            if (combatTelegraph != null) combatTelegraph.Hide();
            if (laserSightLine != null) laserSightLine.enabled = false;

            // Trigger visceral monster defeat sound & burst VFX
            FeaturesCombat.Adapters.CombatHitVfxEngine.Instance?.TriggerMonsterDefeated(transform.position, Vector3.up);

            FeaturesCombat.Adapters.HitstopCoordinator.Instance?.Unregister(gameObject);
            if (airborneHazard != null) airborneHazard.DeactivateHazard();
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseAllForEntity(gameObject.GetInstanceID());

            // Process Loot Drops (Task 4.1 Refactor: Delegated to modular handler)
            EnemyLootDropHandler.ProcessDeathDrops(this);

            OnAnyEnemyDied?.Invoke(this);
            ServiceLocator.Resolve<IDailyEconomyService>()?.RecordMonsterSlain();

            if (Application.isPlaying)
            {
                if (EnemyObjectPool.Instance != null)
                {
                    StartCoroutine(RoutineReturnToPool(0.04f));
                }
                else
                {
                    Destroy(gameObject, 0.04f);
                }
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private IEnumerator RoutineReturnToPool(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (EnemyObjectPool.Instance != null)
            {
                EnemyObjectPool.Instance.ReturnToPool(this);
            }
            else
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>
        /// Resets monster state to combat-ready when retrieved from the Object Pool.
        /// </summary>
        public void ResetEnemyState()
        {
            currentHealth = maxHealth;
            isPerformingSkill = false;
            IsUnstaggerable = false;
            isBurrowed = false;
            isRootGuarded = false;
            isSummonedMinion = false;
            armor = originalArmor;
            skillCooldownTimer = 0f;
            lastAttackTime = 0f;
            isRetreatingAndHealing = false;
            lastRetreatHealTime = -99f;
            IsTrackingLocked = false;
            LockedStrikeDirection = transform.forward;
            CurrentWindupProgress = 0f;
            IsInRecoveryPunishWindow = false;
            _wasStaggerTinted = false;
            currentPathIndex = 1;
            hasValidNavPath = false;
            nextRepathTime = 0f;
            smoothedAvoidanceDir = transform.forward;
            spawnAnchorPosition = transform.position;
            currentAmbientWanderTarget = transform.position;
            moraleSurgeTimer = 0f;
            moraleSurgeSpeedMult = 1.0f;
            currentTacticalState = FeaturesCombat.Core.PureLogic.MonsterTacticalState.IdlePatrol;
            if (navMeshPath != null)
            {
                navMeshPath.ClearCorners();
            }
            StopAllCoroutines();
            OnActionInterrupted?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.position = transform.position;
                rb.rotation = transform.rotation;
                SetLinearVelocity(Vector3.zero);
                SetAngularVelocity(Vector3.zero);
            }
            Physics.SyncTransforms();

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            EnsureTelegraphElements();
            if (combatTelegraph != null) combatTelegraph.Hide();
            if (laserSightLine != null) laserSightLine.enabled = false;

            transform.localScale = enemyType switch
            {
                EnemyType.TuberMaw => new Vector3(0.9f, 0.9f, 0.9f),
                EnemyType.CyclopsTuberMaw => new Vector3(2.2f, 2.4f, 2.2f),
                EnemyType.TaroBrute => new Vector3(1.2f, 1.5f, 1.2f),
                EnemyType.TaroColossus => new Vector3(2.6f, 3.2f, 2.6f),
                EnemyType.CornMusketeer => new Vector3(0.7f, 1.4f, 0.7f),
                EnemyType.TheRanger => new Vector3(1.8f, 3.5f, 1.8f),
                _ => Vector3.one
            };
        }

        /// <summary>
        /// Updates renderer color using MaterialPropertyBlock (without material duplication).
        /// Preserves SRP Batcher compatibility and prevents pink/magenta rendering.
        /// </summary>
        private void SetRendererColor(Color color)
        {
            if (meshRenderer == null || mpb == null) return;
            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color); // Fallback for standard shaders
            meshRenderer.SetPropertyBlock(mpb);
        }

        private void OnDestroy()
        {
            OnAnyEnemyDied -= HandlePackMemberDied;
            if (FeaturesCombat.UI.EnemyHealthBarManager.Instance != null)
            {
                FeaturesCombat.UI.EnemyHealthBarManager.Instance.HandleEnemyDied(this);
            }
            if (isBoss && FeaturesCombat.UI.BossHealthBarManager.Instance != null)
            {
                FeaturesCombat.UI.BossHealthBarManager.Instance.HandleEnemyDied(this);
            }
        }

        /// <summary>
        /// Responds to the demise of a nearby pack member, triggering a brief adrenaline surge.
        /// </summary>
        private void HandlePackMemberDied(EnemyBase fallen)
        {
            if (fallen == null || fallen == this || IsDead) return;

            float distSq = (fallen.transform.position - transform.position).sqrMagnitude;
            if (distSq <= 64f) // Within 8.0m radius
            {
                TriggerMoraleSurge(2.5f, 1.15f);
            }
        }

        /// <summary>
        /// Activates a pack morale surge granting a temporary movement and reaction boost.
        /// </summary>
        public void TriggerMoraleSurge(float duration = 2.5f, float speedMultiplier = 1.15f)
        {
            if (IsDead) return;
            moraleSurgeTimer = duration;
            moraleSurgeSpeedMult = Mathf.Max(moraleSurgeSpeedMult, speedMultiplier);
            nextRepathTime = 0f; // Immediate repath to re-engage aggressively

            if (FloatingCombatTextManager.Instance != null && !isBoss)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    "⚡ Surge!",
                    new Color(1f, 0.45f, 0.15f, 0.9f));
            }
        }
    }
}
