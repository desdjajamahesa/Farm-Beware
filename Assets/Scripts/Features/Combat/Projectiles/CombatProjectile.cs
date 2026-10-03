using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FeaturesCombat.Projectiles
{
    /// <summary>
    /// Komponen entitas proyektil (sihir, peluru, panah, serangan energi) yang dilengkapi
    /// sumber cahaya dinamis Point Light beradius pendek untuk mode Deferred+.
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

        [Tooltip("Durasi hidup maksimum sebelum hancur otomatis.")]
        [SerializeField] private float lifetime = 4.0f;

        [Tooltip("Besar damage yang diberikan saat mengenai IDamageable.")]
        [SerializeField] private int damage = 25;

        [Header("Deferred+ Point Light Configuration")]
        [Tooltip("Komponen Point Light yang melekat pada proyektil.")]
        [SerializeField] private Light projectileLight;

        [Tooltip("Radius jangkauan pencahayaan proyektil (meter).")]
        [Range(1.0f, 10.0f)]
        [SerializeField] private float lightRadius = 4.0f;

        [Tooltip("Intensitas cahaya proyektil (lux / multiplier).")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float lightIntensity = 2.0f;

        [Tooltip("Warna pendaran cahaya proyektil.")]
        [SerializeField] private Color lightColor = new Color(0.2f, 0.8f, 1.0f); // Magic Cyan

        private Vector3 moveDirection = Vector3.forward;
        private float spawnTime = 0f;
        private GameObject shooterOwner;

        private void Awake()
        {
            EnsurePointLightConfiguration();
        }

        private void OnEnable()
        {
            spawnTime = Time.time;
            EnsurePointLightConfiguration();
        }

        private void Update()
        {
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
        /// Menginisialisasi proyektil saat ditembakkan.
        /// </summary>
        public void Launch(GameObject shooter, Vector3 direction, Color color, int damageAmount = 25, float projSpeed = 14f)
        {
            shooterOwner = shooter;
            moveDirection = direction.normalized;
            transform.forward = moveDirection;
            damage = damageAmount;
            speed = projSpeed;
            lightColor = color;
            spawnTime = Time.time;

            EnsurePointLightConfiguration();
        }

        /// <summary>
        /// Factory helper untuk membangkitkan entitas proyektil secara instan di dunia game.
        /// </summary>
        public static CombatProjectile Spawn(GameObject shooter, Vector3 position, Vector3 direction, int damageAmount, float projSpeed, Color projectileColor)
        {
            GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.name = "CombatProjectile_Kernel";
            projObj.transform.position = position;
            projObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            var col = projObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var rb = projObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var renderer = projObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(shader);
                mat.color = projectileColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", projectileColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", projectileColor * 1.5f);
                }
                renderer.material = mat;
            }

            var proj = projObj.AddComponent<CombatProjectile>();
            proj.Launch(shooter, direction, projectileColor, damageAmount, projSpeed);

            // Tambahkan Trail Renderer glowing untuk keterbacaan visual lintasan peluru
            var trail = projObj.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0.0f;
            Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var trailMat = new Material(trailShader);
            trailMat.color = projectileColor;
            if (trailMat.HasProperty("_BaseColor")) trailMat.SetColor("_BaseColor", projectileColor);
            trail.material = trailMat;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(projectileColor, 0f), new GradientColorKey(projectileColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.0f, 1f) }
            );
            trail.colorGradient = gradient;

            return proj;
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

        private void Despawn()
        {
            Destroy(gameObject);
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
