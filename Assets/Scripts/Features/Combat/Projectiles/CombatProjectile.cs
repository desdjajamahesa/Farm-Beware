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
            // Pergerakan proyektil
            transform.position += moveDirection * (speed * Time.deltaTime);

            // Timeout lifetime
            if (Time.time - spawnTime >= lifetime)
            {
                Despawn();
            }
        }

        /// <summary>
        /// Menginisialisasi proyektil saat ditembakkan.
        /// </summary>
        public void Launch(Vector3 direction, Color color, int damageAmount = 25, float projSpeed = 14f)
        {
            moveDirection = direction.normalized;
            transform.forward = moveDirection;
            damage = damageAmount;
            speed = projSpeed;
            lightColor = color;
            spawnTime = Time.time;

            EnsurePointLightConfiguration();
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

        private void OnTriggerEnter(Collider other)
        {
            // Abaikan trigger lain
            if (other.isTrigger) return;

            // Cek apakah mengenai IDamageable
            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                damageable.TakeDamage(damage, hitPoint, moveDirection);
            }

            Despawn();
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
