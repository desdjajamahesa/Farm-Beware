using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FeaturesCombat.Projectiles
{
    /// <summary>
    /// Komponen entitas proyektil (sihir, peluru, panah, serangan energi) yang terintegrasi
    /// dengan CombatProjectilePool untuk alokasi memori zero-GC.
    /// Dilengkapi sumber cahaya dinamis Point Light beradius pendek untuk mode Deferred+.
    ///
    /// ATURAN KRITIS (GPU PERFORMANCE GUARD):
    /// Fitur Shadow Caster pada Point Light proyektil ini SECARA ABSOLUT DINONAKTIFKAN (shadows = None).
    /// Pada arsitektur Deferred+ (clustered shading), ratusan proyektil bercahaya tanpa bayangan
    /// dapat dirender secara simultan dengan cost ALU minimal. Namun jika bayangan diaktifkan,
    /// GPU akan dipaksa merender ratusan pass shadow map per frame yang akan memacetkan rasterizer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class CombatProjectile : MonoBehaviour
    {
        [Header("Projectile Flight Dynamics")]
        [Tooltip("Kecepatan terbang proyektil (unit per detik).")]
        [SerializeField] private float speed = 14f;

        [Tooltip("Durasi hidup maksimum sebelum didaur ulang otomatis.")]
        [SerializeField] private float lifetime = 4.0f;

        [Tooltip("Besar damage yang diberikan saat mengenai IDamageable.")]
        [SerializeField] private int damage = 25;

        [Header("Deferred+ Point Light Configuration")]
        [Tooltip("Komponen Point Light yang melekat pada proyektil.")]
        [SerializeField] private Light projectileLight;

        [Tooltip("Radius jangkauan pencahayaan proyektil (meter).")]
        [Range(1.0f, 10.0f)]
        [SerializeField] private float lightRadius = 3.5f;

        [Tooltip("Intensitas cahaya proyektil (lux / multiplier).")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float lightIntensity = 1.8f;

        [Tooltip("Warna pendaran cahaya proyektil.")]
        [SerializeField] private Color lightColor = new Color(1f, 0.75f, 0.2f); // Amber glow

        [Header("Visual Components (Cached)")]
        [SerializeField] private MeshRenderer projectileRenderer;
        [SerializeField] private TrailRenderer projectileTrail;

        private Vector3 moveDirection = Vector3.forward;
        private float spawnTime = 0f;
        private GameObject shooterOwner;
        private CombatProjectilePool poolRef;
        private bool isDespawning = false;

        private static MaterialPropertyBlock s_propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Gradient _cachedGradient;
        private GradientColorKey[] _cachedColorKeys;
        private GradientAlphaKey[] _cachedAlphaKeys;

        private void Awake()
        {
            if (projectileRenderer == null)
            {
                projectileRenderer = GetComponent<MeshRenderer>();
            }

            if (projectileTrail == null)
            {
                projectileTrail = GetComponent<TrailRenderer>();
            }

            EnsurePointLightConfiguration();
            InitializeGradientCache();
        }

        private void InitializeGradientCache()
        {
            if (_cachedGradient == null)
            {
                _cachedGradient = new Gradient();
                _cachedColorKeys = new GradientColorKey[2];
                _cachedAlphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0.0f, 1f)
                };
            }
        }

        private void OnEnable()
        {
            isDespawning = false;
            spawnTime = Time.time;
            EnsurePointLightConfiguration();
        }

        private void Update()
        {
            if (isDespawning) return;

            float stepDist = speed * Time.deltaTime;
            Vector3 step = moveDirection * stepDist;

            // Continuous sphere cast sweep agar peluru tidak pernah menembus (tunneling) target
            if (Physics.SphereCast(transform.position, 0.25f, moveDirection, out RaycastHit hit, stepDist, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore))
            {
                if (IsValidTarget(hit.collider))
                {
                    ApplyHit(hit.collider, hit.point);
                    return;
                }
            }

            transform.position += step;

            // Timeout lifetime
            if (Time.time - spawnTime >= lifetime)
            {
                Despawn();
            }
        }

        /// <summary>
        /// Menginisialisasi proyektil saat ditembakkan dari pool atau secara manual.
        /// Zero heap allocation: menggunakan MaterialPropertyBlock dan cached gradients.
        /// </summary>
        public void Launch(GameObject shooter, Vector3 direction, Color color, int damageAmount = 25, float projSpeed = 14f, CombatProjectilePool pool = null)
        {
            shooterOwner = shooter;
            moveDirection = direction.normalized;
            transform.forward = moveDirection;
            damage = damageAmount;
            speed = projSpeed;
            lightColor = color;
            spawnTime = Time.time;
            poolRef = pool;
            isDespawning = false;

            EnsurePointLightConfiguration();

            // Zero-GC Material Property Block configuration
            if (projectileRenderer != null)
            {
                if (s_propBlock == null) s_propBlock = new MaterialPropertyBlock();
                projectileRenderer.GetPropertyBlock(s_propBlock);
                Color hdrColor = color * 1.5f;
                s_propBlock.SetColor(BaseColorId, hdrColor);
                s_propBlock.SetColor(ColorId, hdrColor);
                projectileRenderer.SetPropertyBlock(s_propBlock);
            }

            // Zero-GC Trail configuration
            if (projectileTrail != null)
            {
                projectileTrail.Clear();
                InitializeGradientCache();
                _cachedColorKeys[0] = new GradientColorKey(color, 0f);
                _cachedColorKeys[1] = new GradientColorKey(color, 1f);
                _cachedGradient.SetKeys(_cachedColorKeys, _cachedAlphaKeys);
                projectileTrail.colorGradient = _cachedGradient;
            }
        }

        /// <summary>
        /// Factory helper untuk membangkitkan entitas proyektil.
        /// Menggunakan CombatProjectilePool untuk menjamin 0 byte runtime GC allocation.
        /// </summary>
        public static CombatProjectile Spawn(GameObject shooter, Vector3 position, Vector3 direction, int damageAmount, float projSpeed, Color projectileColor)
        {
            var pool = CombatProjectilePool.EnsureInstanceExists();
            return pool.Spawn(shooter, position, direction, damageAmount, projSpeed, projectileColor);
        }

        /// <summary>
        /// Mengonfigurasi dan memvalidasi properti Point Light:
        /// Tipe = Point, Radius pendek, dan SHADOWS ABSOLUT NONE.
        /// </summary>
        public void EnsurePointLightConfiguration()
        {
            if (projectileLight == null)
            {
                projectileLight = GetComponentInChildren<Light>();
                if (projectileLight == null)
                {
                    var lightGo = new GameObject("Projectile_PointLight");
                    lightGo.transform.SetParent(transform, false);
                    projectileLight = lightGo.AddComponent<Light>();
                }
            }

            projectileLight.type = LightType.Point;
            projectileLight.range = lightRadius;
            projectileLight.intensity = lightIntensity;
            projectileLight.color = lightColor;

            // ATURAN KRITIS DEFERRED+:
            // Shadows WAJIB None untuk mencegah overhead shadow map pass di GPU!
            projectileLight.shadows = LightShadows.None;

            var additionalData = projectileLight.GetComponent<UniversalAdditionalLightData>();
            if (additionalData != null)
            {
                additionalData.shadowRenderingLayers = 0;
            }
        }

        private bool IsValidTarget(Collider other)
        {
            if (other.isTrigger) return false;

            if (shooterOwner != null)
            {
                if (other.gameObject == shooterOwner || other.transform.IsChildOf(shooterOwner.transform))
                    return false;

                // Cegah friendly fire antar sesama monster
                if (shooterOwner.GetComponent<EnemyBase>() != null && (other.GetComponent<EnemyBase>() != null || other.GetComponentInParent<EnemyBase>() != null))
                    return false;
            }

            return true;
        }

        private void ApplyHit(Collider other, Vector3 hitPoint)
        {
            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                int effectiveDamage = damage;
                if (shooterOwner != null)
                {
                    var pc = shooterOwner.GetComponent<FarmBeware.Core.Runtime.IPlayerContext>() ?? shooterOwner.GetComponentInParent<FarmBeware.Core.Runtime.IPlayerContext>();
                    if (pc != null && pc.IsGodMode)
                    {
                        effectiveDamage = 99999;
                    }
                }
                damageable.TakeDamage(effectiveDamage, hitPoint, moveDirection);
            }

            Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsValidTarget(other))
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                ApplyHit(other, hitPoint);
            }
        }

        public void Despawn()
        {
            if (isDespawning) return;
            isDespawning = true;

            if (projectileTrail != null)
            {
                projectileTrail.Clear();
            }

            if (poolRef != null)
            {
                poolRef.ReturnToPool(this);
            }
            else if (CombatProjectilePool.Instance != null)
            {
                CombatProjectilePool.Instance.ReturnToPool(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
            if (projectileLight != null)
            {
                // Jaga agar inspector tidak sengaja menyalakan shadows
                if (projectileLight.shadows != LightShadows.None)
                {
                    Debug.LogWarning("[CombatProjectile] Shadows pada proyektil dilarang diaktifkan. Dikembalikan ke LightShadows.None demi stabilitas Deferred+.");
                    projectileLight.shadows = LightShadows.None;
                }
            }
        }
    }
}
