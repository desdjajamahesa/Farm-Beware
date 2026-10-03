using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using FarmBeware.Core.Runtime;

namespace FeaturesCombat
{
    /// <summary>
    /// Komponen modular untuk kemampuan Boss memanggil minion (Task 2.1).
    /// Menggunakan EnemyObjectPool terpusat (Zero-GC), validasi NavMesh & batas outdoor,
    /// serta pembatasan kuota minion aktif agar tidak membebani performa dan gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public class BossSummonAbility : MonoBehaviour
    {
        [Header("Summon Configuration")]
        [SerializeField] private EnemyType minionType = EnemyType.TuberMaw;
        [SerializeField] private int minionCount = 3;
        [SerializeField] private int maxActiveMinions = 6;
        [SerializeField] private float summonCooldown = 45f;
        [SerializeField] private float initialDelay = 4f;
        [SerializeField] private float channelDuration = 1.8f;
        [SerializeField] private bool makeUnstaggerableDuringSummon = true;
        [Tooltip("If true, minions spawn through the front entrance gate instead of appearing randomly around the boss inside the compound.")]
        [SerializeField] private bool summonFromFrontGate = true;

        private EnemyBase bossEnemy;
        private float lastSummonTime = 0f;
        private bool isSummoning = false;
        private readonly List<EnemyBase> activeMinions = new List<EnemyBase>();
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            bossEnemy = GetComponent<EnemyBase>();
        }

        private void Start()
        {
            lastSummonTime = Time.time - summonCooldown + initialDelay;
        }

        public void Configure(EnemyType type, int count, float cooldown, int maxMinions, bool unstaggerable, float channelTime = 1.8f)
        {
            minionType = type;
            minionCount = count;
            summonCooldown = cooldown;
            maxActiveMinions = maxMinions;
            makeUnstaggerableDuringSummon = unstaggerable;
            channelDuration = channelTime;
            lastSummonTime = Time.time - summonCooldown + initialDelay;
        }

        private void Update()
        {
            if (bossEnemy == null || bossEnemy.IsDead || isSummoning) return;
            if (bossEnemy.IsPerformingSkill) return;
            if (Time.time - lastSummonTime < summonCooldown) return;
            if (bossEnemy.PlayerTarget == null) return;

            // Bersihkan referensi minion yang sudah mati / kembali ke pool
            activeMinions.RemoveAll(m => m == null || m.IsDead || !m.gameObject.activeInHierarchy);

            if (activeMinions.Count >= maxActiveMinions) return;

            StartCoroutine(RoutineSummonMinions());
        }

        private IEnumerator RoutineSummonMinions()
        {
            isSummoning = true;
            bossEnemy.IsPerformingSkill = true;

            if (makeUnstaggerableDuringSummon)
            {
                bossEnemy.IsUnstaggerable = true;
            }

            // Hentikan pergerakan saat channeling
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            // Notifikasi visual di atas bos
            if (FloatingCombatTextManager.Instance != null)
            {
                string bossName = bossEnemy.displayName;
                FloatingCombatTextManager.Instance.SpawnText(
                    transform.position + Vector3.up * 2.8f,
                    $"📢 {bossName} Summons Reinforcements!",
                    new Color(0.9f, 0.2f, 0.4f));
            }

            // Efek visual / wind-up channeling via MaterialPropertyBlock
            var rend = GetComponent<Renderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color originalCol = Color.white;
            if (rend != null && rend.sharedMaterial != null)
            {
                originalCol = rend.sharedMaterial.HasProperty("_BaseColor") 
                    ? rend.sharedMaterial.GetColor("_BaseColor") 
                    : (rend.sharedMaterial.HasProperty("_Color") ? rend.sharedMaterial.GetColor("_Color") : Color.white);
            }
            float timer = 0f;
            while (timer < channelDuration)
            {
                timer += Time.deltaTime;
                if (rend != null)
                {
                    float pulse = Mathf.PingPong(timer * 6f, 1f);
                    Color pulseCol = Color.Lerp(originalCol, new Color(1f, 0.1f, 0.3f), pulse);
                    rend.GetPropertyBlock(_mpb);
                    _mpb.SetColor("_BaseColor", pulseCol);
                    _mpb.SetColor("_Color", pulseCol);
                    rend.SetPropertyBlock(_mpb);
                }
                yield return null;
            }

            if (rend != null)
            {
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", originalCol);
                _mpb.SetColor("_Color", originalCol);
                rend.SetPropertyBlock(_mpb);
            }

            // Eksekusi pemanggilan minion
            int toSpawn = Mathf.Min(minionCount, maxActiveMinions - activeMinions.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 spawnPos = CalculateValidSpawnPos(i, toSpawn);

                if (EnemyObjectPool.Instance != null)
                {
                    EnemyBase minion = EnemyObjectPool.Instance.Spawn(minionType, spawnPos);
                    if (minion != null)
                    {
                        activeMinions.Add(minion);
                        if (NightBrawlManager.Instance != null)
                        {
                            NightBrawlManager.Instance.RegisterDynamicEnemy(minion);
                        }
                    }
                }
                else
                {
                    GameObject minionObj = EnemyPrefabFactory.CreateEnemy(minionType, spawnPos);
                    var minion = minionObj.GetComponent<EnemyBase>();
                    if (minion != null)
                    {
                        activeMinions.Add(minion);
                        if (NightBrawlManager.Instance != null)
                        {
                            NightBrawlManager.Instance.RegisterDynamicEnemy(minion);
                        }
                    }
                }
            }

            if (makeUnstaggerableDuringSummon)
            {
                bossEnemy.IsUnstaggerable = false;
            }

            bossEnemy.IsPerformingSkill = false;
            isSummoning = false;
            lastSummonTime = Time.time;
        }

        private Vector3 CalculateValidSpawnPos(int index, int total)
        {
            if (summonFromFrontGate && NightBrawlManager.Instance != null)
            {
                return NightBrawlManager.Instance.CalculateRandomSpawnPoint();
            }

            float angle = (index * (360f / Mathf.Max(1, total))) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(2.5f, 4.0f);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Vector3 candidate = transform.position + offset;

            // Validasi batas outdoor (dilarang spawn di dalam rumah)
            if (NightBrawlManager.IsInsideHouse(candidate))
            {
                candidate = NightBrawlManager.GetNearestOutdoorPosition(candidate, 2.0f);
            }

            // Snap ke NavMesh
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            candidate.y = Mathf.Max(0.5f, candidate.y);
            return candidate;
        }

        private void OnDisable()
        {
            if (isSummoning && bossEnemy != null)
            {
                bossEnemy.IsPerformingSkill = false;
                bossEnemy.IsUnstaggerable = false;
                isSummoning = false;
            }
        }
    }
}
