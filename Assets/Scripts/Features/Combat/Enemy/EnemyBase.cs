using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Base controller untuk semua jenis musuh di Farm-Beware.
    /// Mengelola HP, armor, pergerakan navigasi, AI state machine, serangan kontak, skill unik, dan drop loot.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        public static event Action<EnemyBase> OnAnyEnemyDied;
        public event Action<int, int> OnHealthChanged;

        [Header("Enemy Identity")]
        public EnemyType enemyType = EnemyType.TuberMaw;
        [SerializeField] private FeaturesCombat.Data.EnemyData enemyData;
        public FeaturesCombat.Data.EnemyData EnemyData => enemyData;
        public string displayName = "Tuber Maw";
        public bool isBoss = false;

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
        public float attackRate = 0.4f; // Serangan per detik
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

        // Status internal
        public bool IsDead => currentHealth <= 0;
        public bool IsPerformingSkill { get => isPerformingSkill; set => isPerformingSkill = value; }
        public Transform PlayerTarget => playerTarget;
        public bool IsUnstaggerable { get; set; } = false;
        private Transform playerTarget;
        private Rigidbody rb;
        private Renderer meshRenderer;
        private Color originalColor;
        private MaterialPropertyBlock mpb;
        private float lastAttackTime = 0f;
        private float skillCooldownTimer = 0f;
        private bool isPerformingSkill = false;
        private bool isBurrowed = false;
        private bool isRootGuarded = false;
        private int originalArmor;

        private FeaturesCombat.Core.PureLogic.PoiseTracker poiseTracker;
        public FeaturesCombat.Core.PureLogic.PoiseTracker PoiseTracker => poiseTracker;
        private FeaturesCombat.Adapters.HitFeedbackRenderer hitFeedback;
        private FeaturesCombat.Core.Physics.AirborneHazardEntity airborneHazard;

        public void InitializePoise()
        {
            float maxP = enemyData != null ? enemyData.maxPoise : 100f;
            float regenR = enemyData != null ? enemyData.poiseRegenRate : 25f;
            float regenD = enemyData != null ? enemyData.poiseRegenDelay : 2.5f;
            if (poiseTracker == null)
            {
                poiseTracker = new FeaturesCombat.Core.PureLogic.PoiseTracker(maxP, regenR, regenD);
            }
            else
            {
                bool hasSA = (enemyData != null) && enemyData.hasSuperArmor;
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
        private int currentPathIndex = 1;
        private float nextRepathTime = 0f;
        private bool hasValidNavPath = false;
        private Vector3 smoothedAvoidanceDir = Vector3.forward;

        [Header("Telegraph & Attack Visuals")]
        private GameObject telegraphDecal;
        private LineRenderer telegraphLine;
        private LineRenderer laserSightLine;

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
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
        }

        private void OnDisable()
        {
            if (telegraphDecal != null) telegraphDecal.SetActive(false);
            if (laserSightLine != null) laserSightLine.enabled = false;
            isRetreatingAndHealing = false;

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
            if (telegraphDecal == null)
            {
                telegraphDecal = new GameObject("TelegraphDecal");
                telegraphDecal.transform.SetParent(transform, false);
                telegraphLine = telegraphDecal.AddComponent<LineRenderer>();
                telegraphLine.useWorldSpace = false;
                telegraphLine.loop = true;
                telegraphLine.startWidth = 0.08f;
                telegraphLine.endWidth = 0.08f;
                telegraphLine.positionCount = 24;

                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Material mat = new Material(shader);
                Color warningColor = new Color(1f, 0.22f, 0.22f, 0.85f);
                mat.color = warningColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", warningColor);
                telegraphLine.material = mat;

                float radius = Mathf.Max(1.2f, attackRange);
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2f / 24f;
                    telegraphLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
                }

                telegraphDecal.SetActive(false);
            }

            if (enemyType == EnemyType.CornMusketeer && laserSightLine == null)
            {
                var laserObj = new GameObject("LaserSight");
                laserObj.transform.SetParent(transform, false);
                laserSightLine = laserObj.AddComponent<LineRenderer>();
                laserSightLine.useWorldSpace = true;
                laserSightLine.startWidth = 0.04f;
                laserSightLine.endWidth = 0.02f;
                laserSightLine.positionCount = 2;

                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Material mat = new Material(shader);
                Color laserColor = new Color(1f, 0.85f, 0.1f, 0.85f);
                mat.color = laserColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
                laserSightLine.material = mat;
                laserSightLine.enabled = false;
            }
        }

        private void FindPlayerTarget()
        {
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
                originalArmor = armor;
                currentHealth = maxHealth;
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
                    attackRange = 1.5f;
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
                    attackRange = 3.6f;
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
                    attackRange = 2.5f;
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
                    attackRange = 3.0f;
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
                    contactDamage = 0; // Tidak ada melee contact damage; menyerang dengan proyektil murni!
                    attackRate = 0.5f; // 1 tembakan tiap 2 detik
                    attackRange = 12f; // Jarak tembak ranged
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
                    attackRange = 6f;
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
        }

        private void Update()
        {
            if (IsDead || isPerformingSkill) return;

            if (poiseTracker != null)
            {
                poiseTracker.Tick(Time.deltaTime);
                if (poiseTracker.IsStaggered) return;
            }

            // 1. Batas Anti-Penetrasi Safe Zone: Cegah monster masuk/terselip ke dalam interior rumah modular
            if (NightBrawlManager.IsInsideHouse(transform.position))
            {
                Vector3 safeOutdoor = NightBrawlManager.GetNearestOutdoorPosition(transform.position, 1.5f);
                if (rb != null)
                {
                    rb.position = safeOutdoor;
                    rb.linearVelocity = Vector3.zero;
                }
                transform.position = safeOutdoor;
            }

            if (playerTarget == null)
            {
                FindPlayerTarget();
                return;
            }

            skillCooldownTimer += Time.deltaTime;

            bool isPlayerInsideHouse = NightBrawlManager.IsInsideHouse(playerTarget.position);
            Vector3 effectiveTargetPos = isPlayerInsideHouse 
                ? NightBrawlManager.GetNearestOutdoorPosition(playerTarget.position, 2.5f) 
                : playerTarget.position;

            // Hitung jarak planar / horizontal (Delta XZ) agar musuh raksasa berskala besar tidak terdistorsi perbedaan tinggi pivot (Delta Y)
            Vector3 diffTarget = effectiveTargetPos - transform.position;
            diffTarget.y = 0f;
            float distToTarget = diffTarget.magnitude;

            Vector3 diffPlayer = playerTarget.position - transform.position;
            diffPlayer.y = 0f;
            float distToPlayer = diffPlayer.magnitude;

            // Pada mode malam (Night Brawl), monster selalu agresif langsung mengejar dan memburu pemain tanpa batas jarak aggro
            bool isNightActive = (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                                 (NightBrawlManager.Instance != null && NightBrawlManager.Instance.IsNightBrawlActive);

            bool isAggroed = isNightActive || (distToPlayer <= aggroRange && !isPlayerInsideHouse);

            // Cek AI behaviour
            if (isAggroed)
            {
                // Cek behaviour kabur sejenak & heal saat HP sekarat untuk TuberMaw
                if (enemyType == EnemyType.TuberMaw && currentHealth <= Mathf.RoundToInt(maxHealth * 0.25f) && !isRetreatingAndHealing && Time.time >= lastRetreatHealTime + retreatHealCooldown)
                {
                    StartCoroutine(RoutineRetreatAndHeal());
                    return;
                }

                LookAtTarget(effectiveTargetPos);

                // Cek eksekusi skill khusus jika cooldown siap (hanya jika pemain tidak berada aman di dalam rumah)
                if (skillCooldownTimer >= GetSkillCooldown())
                {
                    TryExecuteSkill(distToPlayer);
                }
                else if (enemyType == EnemyType.CornMusketeer)
                {
                    // Taktik Kiting & Ranged untuk Corn Musketeer:
                    if (!isPlayerInsideHouse && distToPlayer <= 3.8f)
                    {
                        // Pemain terlalu dekat: mundur menjaga jarak tembak ideal (kiting)
                        Vector3 retreatPos = transform.position + (transform.position - effectiveTargetPos).normalized * 4.5f;
                        ChaseTarget(retreatPos);
                    }
                    else if (!isPlayerInsideHouse && distToPlayer <= attackRange)
                    {
                        // Berada di jarak tembak: berhenti dan luncurkan tembakan terarah
                        TryPerformAttack();
                    }
                    else if (moveSpeed > 0f && !isBurrowed)
                    {
                        ChaseTarget(effectiveTargetPos);
                    }
                }
                else if (!isPlayerInsideHouse && distToPlayer <= attackRange)
                {
                    TryPerformAttack();
                }
                else if (moveSpeed > 0f && !isBurrowed)
                {
                    ChaseTarget(effectiveTargetPos);
                }
            }
            else
            {
                // Diam atau patroli santai
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
            }
        }

        private void ChaseTarget(Vector3 targetPos)
        {
            if (rb == null) return;

            Vector3 desiredDir;

            // 1. Hitung jalur NavMesh cerdas secara periodik (repath interval)
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + repathInterval;
                hasValidNavPath = NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, navMeshPath);
                currentPathIndex = 1; // Indeks 0 adalah posisi awal monster saat ini
            }

            // 2. Telusuri waypoint sudut (corners) jika jalur valid
            if (hasValidNavPath && navMeshPath != null && navMeshPath.corners.Length > 1)
            {
                while (currentPathIndex < navMeshPath.corners.Length - 1)
                {
                    Vector3 toCorner = navMeshPath.corners[currentPathIndex] - transform.position;
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

                int targetIndex = Mathf.Min(currentPathIndex, navMeshPath.corners.Length - 1);
                Vector3 nextCorner = navMeshPath.corners[targetIndex];
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
                // Fallback: arah langsung jika NavMesh belum terpasang atau sedang recalculate
                desiredDir = (targetPos - transform.position).normalized;
            }

            desiredDir.y = 0f;

            // 3. Terapkan Dynamic Obstacle Avoidance (Whisker Sensors) agar monster tidak menabrak rintangan atau kawanan lain
            Vector3 finalMoveDir = ApplyObstacleAvoidance(desiredDir);

            rb.linearVelocity = new Vector3(finalMoveDir.x * moveSpeed, rb.linearVelocity.y, finalMoveDir.z * moveSpeed);

            // Putar hadap monster mengikuti arah gerak navigasi secara halus
            if (finalMoveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalMoveDir), Time.deltaTime * 10f);
            }
        }

        /// <summary>
        /// Sensor penghindar rintangan dinamis (Whisker Raycast Avoidance).
        /// Mendeteksi rintangan solid atau sesama monster di depan dan membelokkan arah gerak
        /// sehingga pergerakan kawanan monster terasa organik dan tidak macet di sudut-sudut sempit.
        /// </summary>
        private Vector3 ApplyObstacleAvoidance(Vector3 forwardMoveDir)
        {
            if (forwardMoveDir.sqrMagnitude < 0.001f) return forwardMoveDir;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            float checkDist = obstacleAvoidanceDistance;

            // Cek apakah ada rintangan solid tepat di depan (abaikan karakter pemain target)
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

            // Sensor Whisker kiri & kanan
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
                steerDir = (UnityEngine.Random.value > 0.5f) ? leftWhisker : rightWhisker;
            }
            else
            {
                // Kedua sensor terhalang: belok lebih tajam (70 derajat)
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

        private void TryPerformAttack()
        {
            if (Time.time - lastAttackTime < (1f / Mathf.Max(0.1f, attackRate))) return;
            if (isPerformingSkill) return;

            // Attack token check: prevent uncoordinated horde dogpiling
            bool isRanged = (enemyType == EnemyType.CornMusketeer || enemyType == EnemyType.TheRanger);
            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.TokenDispatcher != null)
            {
                var priority = isBoss ? FeaturesCombat.Core.PureLogic.AttackTokenPriority.Boss : FeaturesCombat.Core.PureLogic.AttackTokenPriority.Minion;
                if (!NightBrawlManager.Instance.TokenDispatcher.TryAcquireToken(gameObject.GetInstanceID(), isRanged, Time.time, 2.8f, priority))
                {
                    return; // Denied, wait for token
                }
            }

            lastAttackTime = Time.time;

            if (enemyType == EnemyType.CornMusketeer)
            {
                StartCoroutine(RoutineCornMusketeerAttack());
            }
            else
            {
                StartCoroutine(RoutineMeleeTelegraphedAttack());
            }
        }

        /// <summary>
        /// Serangan melee bertelegraf.
        /// Monster berhenti sejenak, memunculkan indikator lingkaran merah di tanah (wind-up 0.35s).
        /// Jika pemain menghindar keluar dari lingkaran, serangan akan luput ("Miss!").
        /// </summary>
        private IEnumerator RoutineMeleeTelegraphedAttack()
        {
            isPerformingSkill = true;
            EnsureTelegraphElements();

            // 1. Wind-up Phase (0.35s): Berhenti sejenak, aktifkan indikator telegraf tanah, kedip oranye
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            if (telegraphDecal != null)
            {
                telegraphDecal.SetActive(true);
            }

            if (meshRenderer != null)
            {
                SetRendererColor(new Color(1f, 0.45f, 0.15f)); // Wind-up warning color
            }

            float windupTimer = 0f;
            while (windupTimer < 0.35f)
            {
                windupTimer += Time.deltaTime;
                if (playerTarget != null)
                {
                    LookAtTarget(playerTarget.position);
                }
                yield return null;
            }

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            if (telegraphDecal != null)
            {
                telegraphDecal.SetActive(false);
            }

            // 2. Active Strike Phase: Cek apakah pemain masih berada di dalam area serang (planar horizontal XZ)
            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
            {
                Vector3 toPlayer = playerTarget.position - transform.position;
                toPlayer.y = 0f;
                float currentDist = toPlayer.magnitude;
                Vector3 forwardFlat = transform.forward;
                forwardFlat.y = 0f;
                forwardFlat.Normalize();
                float dot = (currentDist > 0.01f) ? Vector3.Dot(forwardFlat, toPlayer.normalized) : 1f;

                // Pemain terkena serangan jika berada di dalam jangkauan melee dan berada di depan atau menempel dekat monster
                if (currentDist <= attackRange + 0.8f && (dot >= 0.10f || currentDist <= 2.2f))
                {
                    IDamageable playerDamageable = playerTarget.GetComponent<IDamageable>() ?? playerTarget.GetComponentInParent<IDamageable>();
                    if (playerDamageable != null && !playerDamageable.IsDead)
                    {
                        playerDamageable.TakeDamage(contactDamage, playerTarget.position + Vector3.up * 1f, transform.forward);
                    }
                }
                else
                {
                    // Pemain berhasil menghindar tepat waktu!
                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            transform.position + Vector3.up * 1.6f,
                            "Miss!",
                            new Color(0.85f, 0.85f, 0.85f, 0.8f));
                    }
                }
            }

            // 3. Recovery Phase (0.18s)
            yield return new WaitForSeconds(0.18f);
            isPerformingSkill = false;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), false);
        }

        /// <summary>
        /// Serangan tembakan terarah Corn Musketeer.
        /// Membidik pemain dengan laser sight selama 0.55 detik (0.40s tracking dinamis, 0.15s Aim Lock).
        /// Elevasi tembakan diselaraskan datar (Delta Y = 0) agar kecepatan proyektil di layar simetris 100%.
        /// </summary>
        private IEnumerator RoutineCornMusketeerAttack()
        {
            isPerformingSkill = true;
            EnsureTelegraphElements();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            // 1. Aiming Telegraph (0.55s total)
            if (laserSightLine != null)
            {
                laserSightLine.enabled = true;
                Color trackingColor = new Color(1f, 0.85f, 0.1f, 0.75f);
                if (laserSightLine.material != null)
                {
                    laserSightLine.material.color = trackingColor;
                    if (laserSightLine.material.HasProperty("_BaseColor"))
                        laserSightLine.material.SetColor("_BaseColor", trackingColor);
                }
            }

            // Ambil referensi collider untuk mengukur tinggi dasar kaki di atas tanah (terlepas dari posisi pivot 3D mesh)
            Collider myCol = GetComponent<Collider>();
            float feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
            Vector3 muzzlePos = transform.position + transform.forward * 0.6f;
            muzzlePos.y = feetY + 1.0f; // Tepat setinggi dada (1.0m di atas tanah)

            Vector3 aimDir = transform.forward;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector3.forward;
            aimDir.Normalize();

            float aimTimer = 0f;
            while (aimTimer < 0.55f)
            {
                aimTimer += Time.deltaTime;

                // Tracking Phase (0 s.d. 0.40s): Membidik dan mengikuti posisi pemain
                if (aimTimer <= 0.40f)
                {
                    if (playerTarget != null)
                    {
                        LookAtTarget(playerTarget.position);

                        feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
                        muzzlePos = transform.position + transform.forward * 0.6f;
                        muzzlePos.y = feetY + 1.0f;

                        Collider playerCol = playerTarget.GetComponent<Collider>();
                        Vector3 targetPos = playerCol != null ? playerCol.bounds.center : (playerTarget.position + Vector3.up * 1.0f);

                        Vector3 toTarget = targetPos - muzzlePos;
                        if (toTarget.sqrMagnitude > 0.001f)
                        {
                            aimDir = toTarget.normalized;
                        }

                        if (laserSightLine != null)
                        {
                            laserSightLine.SetPosition(0, muzzlePos);
                            laserSightLine.SetPosition(1, targetPos);
                        }
                    }
                }
                // Aim Freeze Phase (0.40s s.d. 0.55s): Arah tembakan terkunci! Berikan sinyal merah peringatan
                else
                {
                    if (laserSightLine != null)
                    {
                        Color lockColor = new Color(1f, 0.25f, 0.1f, 0.95f);
                        if (laserSightLine.material != null)
                        {
                            laserSightLine.material.color = lockColor;
                            if (laserSightLine.material.HasProperty("_BaseColor"))
                                laserSightLine.material.SetColor("_BaseColor", lockColor);
                        }
                        laserSightLine.SetPosition(0, muzzlePos);
                        laserSightLine.SetPosition(1, muzzlePos + aimDir * attackRange);
                    }
                }

                yield return null;
            }

            if (laserSightLine != null)
            {
                laserSightLine.enabled = false;
            }

            // 2. Firing: Tembakkan proyektil fisik tepat dari ketinggian dada Corn ke dada pemain
            FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                gameObject,
                muzzlePos,
                aimDir,
                damageAmount: 22,
                projSpeed: 13.5f,
                projectileColor: new Color(1f, 0.85f, 0.15f));

            // Recoil kick mundur sejenak
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(-aimDir * 2.5f, ForceMode.Impulse);
            }

            // 3. Recovery (0.2s)
            yield return new WaitForSeconds(0.2f);
            isPerformingSkill = false;
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseToken(gameObject.GetInstanceID(), true);
        }

        private float GetSkillCooldown()
        {
            switch (enemyType)
            {
                case EnemyType.TuberMaw: return 5f;
                case EnemyType.CyclopsTuberMaw: return 15f;
                case EnemyType.TaroBrute: return 12f;
                case EnemyType.TaroColossus: return 18f;
                case EnemyType.CornMusketeer: return 10f;
                case EnemyType.TheRanger: return 14f;
                default: return 10f;
            }
        }

        private void TryExecuteSkill(float distToPlayer)
        {
            skillCooldownTimer = 0f;

            switch (enemyType)
            {
                case EnemyType.TuberMaw:
                    StartCoroutine(RoutineBurrowStrike());
                    break;

                case EnemyType.CyclopsTuberMaw:
                    StartCoroutine(RoutineTrackingBeam());
                    break;

                case EnemyType.TaroBrute:
                    StartCoroutine(RoutineRootGuard());
                    break;

                case EnemyType.TaroColossus:
                    if (GetComponent<ColossusAirborneAbility>() == null)
                    {
                        StartCoroutine(RoutineAirborneSlam());
                    }
                    break;

                case EnemyType.CornMusketeer:
                    StartCoroutine(RoutineKernelShot());
                    break;

                case EnemyType.TheRanger:
                    StartCoroutine(RoutineCornHeavensFall());
                    break;
            }
        }

        // --- LOW HP RETREAT & HEAL ROUTINE ---

        private IEnumerator RoutineRetreatAndHeal()
        {
            isRetreatingAndHealing = true;
            isPerformingSkill = true;
            lastRetreatHealTime = Time.time;

            float retreatTimer = 0f;

            // 1. Sedikit menjauh dari pemain (lari mundur teratur sampai jarak aman ~7m atau maksimal 1.8s)
            while (retreatTimer < maxRetreatDuration && !IsDead)
            {
                retreatTimer += Time.deltaTime;

                if (playerTarget == null)
                {
                    FindPlayerTarget();
                }

                if (playerTarget != null)
                {
                    Vector3 awayFromPlayer = transform.position - playerTarget.position;
                    awayFromPlayer.y = 0f;
                    float dist = awayFromPlayer.magnitude;

                    // Jika sudah sedikit menjauh dan berada di jarak aman, hentikan lari mundur dan mulai proses pemulihan
                    if (dist >= retreatSafeDistance)
                    {
                        break;
                    }

                    Vector3 fleeDir = awayFromPlayer.normalized;
                    if (fleeDir.sqrMagnitude < 0.001f)
                    {
                        fleeDir = -transform.forward;
                    }

                    // Terapkan Dynamic Obstacle Avoidance agar tidak menabrak rintangan atau terjebak di sudut
                    Vector3 moveDir = ApplyObstacleAvoidance(fleeDir);

                    // Cegah mundur menembus ke dalam rumah modular
                    if (NightBrawlManager.IsInsideHouse(transform.position + moveDir * 0.5f))
                    {
                        Vector3 safeOutdoor = NightBrawlManager.GetNearestOutdoorPosition(transform.position, 1.5f);
                        moveDir = (safeOutdoor - transform.position).normalized;
                    }

                    if (rb != null)
                    {
                        rb.linearVelocity = new Vector3(moveDir.x * (moveSpeed * 1.35f), rb.linearVelocity.y, moveDir.z * (moveSpeed * 1.35f));
                    }

                    if (moveDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 12f);
                    }
                }

                yield return null;
            }

            if (IsDead)
            {
                isRetreatingAndHealing = false;
                isPerformingSkill = false;
                yield break;
            }

            // Hentikan gerak mundur
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            // 2. Heal diri sendiri sampai 50 current health
            if (FloatingCombatTextManager.Instance != null)
            {
                int healRecovered = Mathf.Max(0, retreatHealTargetHealth - currentHealth);
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.5f,
                    $"💚 +{healRecovered} HP (Recovered)",
                    new Color(0.2f, 1f, 0.4f)
                );
            }

            // Visual flash warna hijau pemulihan
            if (meshRenderer != null)
            {
                SetRendererColor(new Color(0.25f, 1f, 0.45f));
            }

            yield return new WaitForSeconds(0.7f);

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            if (!IsDead)
            {
                currentHealth = Mathf.Min(maxHealth, retreatHealTargetHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            yield return new WaitForSeconds(0.2f);

            // 3. Selesai heal, langsung kembali agresif mengejar dan menyerang player lagi
            isRetreatingAndHealing = false;
            isPerformingSkill = false;
        }

        // --- SKILL ROUTINES ---

        private IEnumerator RoutineBurrowStrike()
        {
            isPerformingSkill = true;
            isBurrowed = true;

            // Sembunyi / masuk tanah
            Vector3 startScale = transform.localScale;
            transform.localScale = new Vector3(startScale.x, 0.1f, startScale.z);

            // Track posisi pemain selama 1.2 detik (dibatasi di luar batas rumah jika pemain di dalam rumah)
            float timer = 0f;
            while (timer < 1.2f)
            {
                timer += Time.deltaTime;
                if (playerTarget != null)
                {
                    Vector3 burrowDest = playerTarget.position;
                    if (NightBrawlManager.IsInsideHouse(burrowDest))
                    {
                        burrowDest = NightBrawlManager.GetNearestOutdoorPosition(burrowDest, 2.0f);
                    }
                    transform.position = Vector3.MoveTowards(transform.position, burrowDest, (moveSpeed * 1.5f) * Time.deltaTime);
                }
                yield return null;
            }

            // Muncul mendadak dan hantam pemain jika pemain berada di luar rumah
            transform.localScale = startScale;
            isBurrowed = false;

            if (rb != null)
            {
                rb.position = transform.position;
                rb.linearVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();

            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position) && Vector3.Distance(transform.position, playerTarget.position) <= 2.2f)
            {
                var target = playerTarget.GetComponent<IDamageable>();
                target?.TakeDamage(contactDamage + 10, playerTarget.position, Vector3.up);
            }

            yield return new WaitForSeconds(0.4f);
            isPerformingSkill = false;
        }

        private IEnumerator RoutineTrackingBeam()
        {
            isPerformingSkill = true;

            // Charge beam selama 1 detik
            float charge = 0f;
            while (charge < 1.0f)
            {
                charge += Time.deltaTime;
                if (playerTarget != null) LookAtTarget(playerTarget.position);
                yield return null;
            }

            // Tembakkan laser membidik ke arah dada/tengah tubuh pemain
            Vector3 eyeOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 targetChest = (playerTarget != null) ? playerTarget.position + Vector3.up * 1.0f : (eyeOrigin + transform.forward * 10f);
            Vector3 shootDir = (targetChest - eyeOrigin).normalized;

            RaycastHit[] hits = Physics.RaycastAll(eyeOrigin, shootDir, 20f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
                var target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && hit.collider.CompareTag("Player") && !NightBrawlManager.IsInsideHouse(hit.point))
                {
                    target.TakeDamage(25, hit.point, shootDir);
                }
                break;
            }

            yield return new WaitForSeconds(0.5f);
            isPerformingSkill = false;
        }

        private IEnumerator RoutineRootGuard()
        {
            isPerformingSkill = true;
            isRootGuarded = true;
            armor = Mathf.RoundToInt(originalArmor * 1.5f);

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 1.8f,
                    "🛡️ Root Guard (+50% Armor)",
                    new Color(0.4f, 0.9f, 0.4f));
            }

            yield return new WaitForSeconds(6f);

            armor = originalArmor;
            isRootGuarded = false;
            isPerformingSkill = false;
        }

        private IEnumerator RoutineAirborneSlam()
        {
            isPerformingSkill = true;

            // Loncat tinggi ke udara
            Vector3 groundPos = transform.position;
            transform.position = groundPos + Vector3.up * 12f;

            yield return new WaitForSeconds(1.5f);

            // Turun menghantam tanah di dekat pemain (atau di batas luar jika pemain di dalam rumah)
            Vector3 slamTarget = playerTarget != null ? playerTarget.position : groundPos;
            if (NightBrawlManager.IsInsideHouse(slamTarget))
            {
                slamTarget = NightBrawlManager.GetNearestOutdoorPosition(slamTarget, 2.5f);
            }
            transform.position = slamTarget;
            if (rb != null)
            {
                rb.position = slamTarget;
                rb.linearVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();

            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position) && Vector3.Distance(transform.position, playerTarget.position) <= 4f)
            {
                var target = playerTarget.GetComponent<IDamageable>();
                target?.TakeDamage(35, slamTarget, Vector3.up);
            }

            yield return new WaitForSeconds(0.6f);
            isPerformingSkill = false;
        }

        private IEnumerator RoutineKernelShot()
        {
            isPerformingSkill = true;
            EnsureTelegraphElements();

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2f,
                    "🌽 Kernel Burst!",
                    new Color(1f, 0.8f, 0.2f));
            }

            // Charge sejenak (0.4s)
            yield return new WaitForSeconds(0.4f);

            Collider myCol = GetComponent<Collider>();

            // Tembakkan 3 butir proyektil jagung beruntun tepat setinggi dada
            for (int i = 0; i < 3; i++)
            {
                if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
                {
                    LookAtTarget(playerTarget.position);

                    float feetY = myCol != null ? myCol.bounds.min.y : (transform.position.y - 1.4f);
                    Vector3 muzzlePos = transform.position + transform.forward * 0.6f;
                    muzzlePos.y = feetY + 1.0f;

                    Collider playerCol = playerTarget.GetComponent<Collider>();
                    Vector3 targetPos = playerCol != null ? playerCol.bounds.center : (playerTarget.position + Vector3.up * 1.0f);

                    Vector3 toTarget = targetPos - muzzlePos;
                    Vector3 aimDir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;

                    FeaturesCombat.Projectiles.CombatProjectile.Spawn(
                        gameObject,
                        muzzlePos,
                        aimDir,
                        damageAmount: 18,
                        projSpeed: 14.5f,
                        projectileColor: new Color(1f, 0.65f, 0.1f));

                    if (rb != null && !rb.isKinematic)
                    {
                        rb.AddForce(-aimDir * 1.8f, ForceMode.Impulse);
                    }
                }
                yield return new WaitForSeconds(0.14f);
            }

            yield return new WaitForSeconds(0.3f);
            isPerformingSkill = false;
        }

        private IEnumerator RoutineCornHeavensFall()
        {
            isPerformingSkill = true;

            if (FloatingCombatTextManager.Instance != null)
            {
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2f,
                    "🌽 Heavens Fall!",
                    new Color(1f, 0.6f, 0.2f));
            }

            yield return new WaitForSeconds(1.2f);

            // Jika pemain berada di dalam rumah, atap melindungi sepenuhnya (0 damage)
            if (playerTarget != null && !NightBrawlManager.IsInsideHouse(playerTarget.position))
            {
                var target = playerTarget.GetComponent<IDamageable>();
                target?.TakeDamage(30, playerTarget.position, Vector3.down);
            }

            yield return new WaitForSeconds(0.5f);
            isPerformingSkill = false;
        }

        // --- IDAMAGEABLE IMPLEMENTATION ---

        public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
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

            // Reduksi damage berdasarkan armor
            int effectiveDmg = Mathf.Max(1, damage - Mathf.RoundToInt(armor * 0.25f));
            currentHealth -= effectiveDmg;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Flash visual (Zero-GC via MaterialPropertyBlock)
            if (hitFeedback != null) hitFeedback.TriggerFlash();

            // Terapkan knockback & stagger berjenjang (diabaikan jika un-staggerable)
            if (poiseTracker != null)
            {
                float poiseDmg = Mathf.Max(10f, damage * 0.75f);
                float impulse = Mathf.Max(1.0f, hitDirection.magnitude);
                var staggerTier = poiseTracker.ApplyPoiseDamage(poiseDmg, impulse, out bool poiseBroken);

                if (rb != null && !IsUnstaggerable)
                {
                    if (staggerTier == FeaturesCombat.Core.PureLogic.StaggerTier.Knockdown)
                    {
                        if (airborneHazard != null)
                        {
                            airborneHazard.LaunchAsHazard(hitDirection * 12f + Vector3.up * 4f);
                        }
                        else
                        {
                            rb.AddForce(hitDirection * 10f + Vector3.up * 3f, ForceMode.Impulse);
                        }
                    }
                    else if (staggerTier == FeaturesCombat.Core.PureLogic.StaggerTier.HeavyStagger)
                    {
                        rb.AddForce(hitDirection * 6f, ForceMode.Impulse);
                    }
                    else if (staggerTier == FeaturesCombat.Core.PureLogic.StaggerTier.MicroStagger)
                    {
                        rb.AddForce(hitDirection * 2.5f, ForceMode.Impulse);
                    }
                }
            }
            else if (rb != null && knockbackResistance < 1f && !IsUnstaggerable)
            {
                float actualKb = 6f * (1f - knockbackResistance);
                rb.AddForce(hitDirection * actualKb, ForceMode.Impulse);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }



        private void Die()
        {
            currentHealth = 0;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            FeaturesCombat.Adapters.HitstopCoordinator.Instance?.Unregister(gameObject);
            if (airborneHazard != null) airborneHazard.DeactivateHazard();
            NightBrawlManager.Instance?.TokenDispatcher?.ReleaseAllForEntity(gameObject.GetInstanceID());

            // Process Loot Drops (Task 4.1 Refactor: Delegated to modular handler)
            EnemyLootDropHandler.ProcessDeathDrops(this);

            OnAnyEnemyDied?.Invoke(this);
            ServiceLocator.Resolve<IDailyEconomyService>()?.RecordMonsterSlain();

            if (EnemyObjectPool.Instance != null)
            {
                StartCoroutine(RoutineReturnToPool(0.12f));
            }
            else
            {
                Destroy(gameObject, 0.12f);
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
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Mengembalikan status monster ke kondisi siap bertarung saat dipanggil dari Object Pool.
        /// </summary>
        public void ResetEnemyState()
        {
            currentHealth = maxHealth;
            isPerformingSkill = false;
            IsUnstaggerable = false;
            isBurrowed = false;
            isRootGuarded = false;
            armor = originalArmor;
            skillCooldownTimer = 0f;
            lastAttackTime = 0f;
            isRetreatingAndHealing = false;
            lastRetreatHealTime = -99f;
            currentPathIndex = 1;
            hasValidNavPath = false;
            nextRepathTime = 0f;
            smoothedAvoidanceDir = transform.forward;
            if (navMeshPath != null)
            {
                navMeshPath.ClearCorners();
            }
            StopAllCoroutines();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (rb != null)
            {
                rb.position = transform.position;
                rb.rotation = transform.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();

            if (meshRenderer != null)
            {
                SetRendererColor(originalColor);
            }

            EnsureTelegraphElements();
            if (telegraphDecal != null) telegraphDecal.SetActive(false);
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
        /// Mengubah warna renderer menggunakan MaterialPropertyBlock (tanpa duplikasi material).
        /// Menjaga kompatibilitas SRP Batcher dan mencegah pink/magenta rendering.
        /// </summary>
        private void SetRendererColor(Color color)
        {
            if (meshRenderer == null || mpb == null) return;
            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color); // Fallback for standard shaders
            meshRenderer.SetPropertyBlock(mpb);
        }
    }
}
