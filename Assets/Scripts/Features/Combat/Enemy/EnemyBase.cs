using System;
using System.Collections;
using UnityEngine;
using PlayerUI;
using FeaturesEconomy;

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

        [Header("Enemy Identity")]
        public EnemyType enemyType = EnemyType.TuberMaw;
        public string displayName = "Tuber Maw";
        public bool isBoss = false;

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
        private Transform playerTarget;
        private Rigidbody rb;
        private Renderer meshRenderer;
        private Color originalColor;
        private float lastAttackTime = 0f;
        private float skillCooldownTimer = 0f;
        private bool isPerformingSkill = false;
        private bool isBurrowed = false;
        private bool isRootGuarded = false;
        private int originalArmor;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            meshRenderer = GetComponentInChildren<Renderer>();
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                originalColor = meshRenderer.sharedMaterial.color;
            }

            originalArmor = armor;
            InitializeStatsByType();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            FindPlayerTarget();
        }

        private void FindPlayerTarget()
        {
            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (p != null) playerTarget = p.transform;
        }

        public void InitializeStatsByType()
        {
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
                    attackRange = 2.0f;
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
                    moveSpeed = 3f;
                    contactDamage = 15;
                    attackRate = 0.8f;
                    attackRange = 8f;
                    aggroRange = 15f;
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

            if (playerTarget == null)
            {
                FindPlayerTarget();
                return;
            }

            skillCooldownTimer += Time.deltaTime;

            float dist = Vector3.Distance(transform.position, playerTarget.position);

            // Cek AI behaviour
            if (dist <= aggroRange)
            {
                LookAtTarget(playerTarget.position);

                // Cek eksekusi skill khusus jika cooldown siap
                if (skillCooldownTimer >= GetSkillCooldown())
                {
                    TryExecuteSkill(dist);
                }
                else if (dist <= attackRange)
                {
                    TryPerformAttack();
                }
                else if (moveSpeed > 0f && !isBurrowed)
                {
                    ChasePlayer();
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

        private void ChasePlayer()
        {
            if (rb == null || playerTarget == null) return;

            // Jika HP sangat sekarat dan tipe TuberMaw, jalankan Tunnel Rush (lari menjauh)
            bool isLowHp = currentHealth <= maxHealth * 0.25f;
            Vector3 moveDir;

            if (isLowHp && enemyType == EnemyType.TuberMaw)
            {
                moveDir = (transform.position - playerTarget.position).normalized;
            }
            else
            {
                moveDir = (playerTarget.position - transform.position).normalized;
            }

            moveDir.y = 0f;
            rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);
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

            lastAttackTime = Time.time;

            // Berikan contact damage pada pemain
            if (playerTarget != null)
            {
                IDamageable playerDamageable = playerTarget.GetComponent<IDamageable>();
                if (playerDamageable != null && !playerDamageable.IsDead)
                {
                    Vector3 hitDir = (playerTarget.position - transform.position).normalized;
                    playerDamageable.TakeDamage(contactDamage, playerTarget.position + Vector3.up * 1f, hitDir);
                }
            }
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
                    StartCoroutine(RoutineAirborneSlam());
                    break;

                case EnemyType.CornMusketeer:
                    StartCoroutine(RoutineKernelShot());
                    break;

                case EnemyType.TheRanger:
                    StartCoroutine(RoutineCornHeavensFall());
                    break;
            }
        }

        // --- SKILL ROUTINES ---

        private IEnumerator RoutineBurrowStrike()
        {
            isPerformingSkill = true;
            isBurrowed = true;

            // Sembunyi / masuk tanah
            Vector3 startScale = transform.localScale;
            transform.localScale = new Vector3(startScale.x, 0.1f, startScale.z);

            // Track posisi pemain selama 1.2 detik
            float timer = 0f;
            while (timer < 1.2f)
            {
                timer += Time.deltaTime;
                if (playerTarget != null)
                {
                    transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, (moveSpeed * 1.5f) * Time.deltaTime);
                }
                yield return null;
            }

            // Muncul mendadak dan hantam pemain
            transform.localScale = startScale;
            isBurrowed = false;

            if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= 2.2f)
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

            // Tembakkan laser ke garis lurus depan
            Vector3 shootDir = transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, shootDir, out hit, 15f))
            {
                var target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && hit.collider.CompareTag("Player"))
                {
                    target.TakeDamage(25, hit.point, shootDir);
                }
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

            // Turun menghantam tanah di dekat pemain
            Vector3 slamTarget = playerTarget != null ? playerTarget.position : groundPos;
            transform.position = slamTarget;

            if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= 4f)
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

            // Charge shot 1 detik
            yield return new WaitForSeconds(0.8f);

            if (playerTarget != null)
            {
                Vector3 shotDir = (playerTarget.position - transform.position).normalized;
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * 1.2f, shotDir, out hit, 15f))
                {
                    var target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
                    if (target != null && hit.collider.CompareTag("Player"))
                    {
                        target.TakeDamage(45, hit.point, shotDir);
                    }
                }
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

            if (playerTarget != null)
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

            // Reduksi damage berdasarkan armor
            int effectiveDmg = Mathf.Max(1, damage - Mathf.RoundToInt(armor * 0.25f));
            currentHealth -= effectiveDmg;

            // Flash visual
            StartCoroutine(RoutineHitFlash());

            // Terapkan knockback
            if (rb != null && knockbackResistance < 1f)
            {
                float actualKb = 6f * (1f - knockbackResistance);
                rb.AddForce(hitDirection * actualKb, ForceMode.Impulse);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator RoutineHitFlash()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.white;
                yield return new WaitForSeconds(0.08f);
                meshRenderer.material.color = originalColor;
            }
        }

        private void Die()
        {
            currentHealth = 0;

            // 1. Drop Gold
            if (UnityEngine.Random.value <= goldChance)
            {
                int goldAmount = UnityEngine.Random.Range(minGold, maxGold + 1);
                if (PlayerWallet.Instance != null)
                {
                    PlayerWallet.Instance.AddGold(goldAmount);
                    if (FeaturesEconomy.DailyEconomyManager.Instance != null)
                    {
                        FeaturesEconomy.DailyEconomyManager.Instance.RecordCombatGold(goldAmount);
                    }

                    if (FloatingCombatTextManager.Instance != null)
                    {
                        FloatingCombatTextManager.Instance.SpawnText(
                            transform.position + Vector3.up * 1.5f,
                            $"+{goldAmount} Gold",
                            new Color(1f, 0.85f, 0.2f));
                    }
                }
            }

            // 2. Drop Monster Material
            if (dropMaterial != null && UnityEngine.Random.value <= dropChance)
            {
                int dropCount = UnityEngine.Random.Range(minDropCount, maxDropCount + 1);
                WorldItemPickup.Spawn(transform.position, dropMaterial, dropCount);
            }

            OnAnyEnemyDied?.Invoke(this);

            Destroy(gameObject, 0.1f);
        }
    }
}
