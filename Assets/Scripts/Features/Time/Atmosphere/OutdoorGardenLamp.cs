using UnityEngine;

namespace FeaturesTime.Atmosphere
{
    /// <summary>
    /// Komponen lampu taman outdoor (Garden Lamp - Dual Source Architecture).
    /// Mengontrol sumber cahaya Point Light (radiosity sekeliling) + Spot Downlight (proyeksi tanah terang),
    /// serta kap bohlam kaca dan filamen pijar dengan dukungan MaterialPropertyBlock (Zero-GC)
    /// dan efek flickering organik.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class OutdoorGardenLamp : MonoBehaviour
    {
        [Header("Lamp References")]
        [Tooltip("Komponen Point Light untuk pencahayaan ambient sekeliling (karakter, monster, tiang).")]
        [SerializeField] private Light lampLight;

        [Tooltip("Komponen Spot Downlight untuk proyeksi cahaya kuat terfokus ke permukaan tanah.")]
        [SerializeField] private Light downLight;

        [Tooltip("Renderer mesh bohlam lentera luar.")]
        [SerializeField] private Renderer bulbRenderer;

        [Tooltip("Renderer inti filamen pijar di dalam bohlam.")]
        [SerializeField] private Renderer filamentRenderer;

        [Header("Lighting Calibration")]
        [Tooltip("Intensitas Point Light saat malam hari (lux).")]
        [SerializeField] [Min(0f)] private float nightPointIntensity = 25.0f;

        [Tooltip("Intensitas Spot Downlight saat malam hari (lux). Proyeksi tanah terang.")]
        [SerializeField] [Min(0f)] private float nightDownIntensity = 40.0f;

        [Tooltip("Intensitas cahaya saat siang hari (0 lux / mati).")]
        [SerializeField] [Min(0f)] private float dayIntensity = 0.0f;

        [Tooltip("Jangkauan Point Light sekeliling.")]
        [SerializeField] [Range(2f, 30f)] private float pointRange = 15.0f;

        [Tooltip("Jangkauan Spot Downlight ke tanah.")]
        [SerializeField] [Range(2f, 20f)] private float downRange = 9.0f;

        [Tooltip("Warna cahaya lampu taman (default: putih bersih / neutral white).")]
        [SerializeField] private Color lightColor = Color.white;

        [Header("Bulb Visuals (Day & Night)")]
        [Tooltip("Warna pendaran HDR bohlam saat malam hari (putih bersih / neutral white glow).")]
        [SerializeField] [ColorUsage(true, true)] private Color nightBulbColor = new Color(2.5f, 2.5f, 2.5f, 1.0f);

        [Tooltip("Warna kaca bohlam saat siang hari (frosted milky glass).")]
        [SerializeField] private Color dayBulbColor = new Color(0.96f, 0.93f, 0.85f, 1.0f);

        [Tooltip("Warna inti filamen pijar saat malam hari (putih pijar HDR).")]
        [SerializeField] [ColorUsage(true, true)] private Color nightFilamentColor = new Color(6.0f, 6.0f, 6.0f, 1.0f);

        [Tooltip("Warna inti filamen saat siang hari (kawat tungsten gelap).")]
        [SerializeField] private Color dayFilamentColor = new Color(0.35f, 0.28f, 0.18f, 1.0f);

        [Header("Micro Ambiance / Flicker")]
        [Tooltip("Aktifkan animasi kedip halus/organik saat malam hari.")]
        [SerializeField] private bool enableFlicker = true;

        [Tooltip("Amplitudo fluktuasi kedip (misal: 0.04 = fluktuasi 4%).")]
        [SerializeField] [Range(0.01f, 0.2f)] private float flickerAmount = 0.04f;

        [Tooltip("Kecepatan kedip Perlin noise.")]
        [SerializeField] [Range(0.5f, 10f)] private float flickerSpeed = 3.5f;

        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private float seed;

        public Light LampLight => lampLight;
        public Light DownLight => downLight;
        public Renderer BulbRenderer => bulbRenderer;
        public Renderer FilamentRenderer => filamentRenderer;
        public bool EnableFlicker => enableFlicker;
        public float FlickerAmount => flickerAmount;
        public float FlickerSpeed => flickerSpeed;

        private void Awake()
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            seed = (transform.position.x * 12.9898f + transform.position.z * 78.233f) % 1000f;
            ApplyLightSettings();
        }

        private void OnValidate()
        {
            ApplyLightSettings();
        }

        public void ApplyLightSettings()
        {
            if (lampLight != null)
            {
                lampLight.type = LightType.Point;
                lampLight.range = pointRange;
                lampLight.color = lightColor;
                lampLight.shadows = LightShadows.None;
            }

            if (downLight != null)
            {
                downLight.type = LightType.Spot;
                downLight.range = downRange;
                downLight.spotAngle = 135f;
                downLight.innerSpotAngle = 80f;
                downLight.color = lightColor;
                downLight.shadows = LightShadows.Soft;
                downLight.shadowStrength = 0.6f;
                downLight.shadowBias = 0.05f;
                downLight.shadowNormalBias = 0.35f;
                downLight.shadowNearPlane = 0.2f;
            }
        }

        /// <summary>
        /// Mengatur intensitas pencahayaan berdasarkan interpolasi Day-Night (0.0 = Siang murni, 1.0 = Malam murni).
        /// </summary>
        public void SetNormalizedNight(float t, float flickerFactor = 1.0f)
        {
            if (lampLight != null)
            {
                float targetPoint = Mathf.Lerp(dayIntensity, nightPointIntensity, t);
                float finalPoint = targetPoint * flickerFactor;
                lampLight.intensity = finalPoint;
                lampLight.enabled = finalPoint > 0.001f;
            }

            if (downLight != null)
            {
                float targetDown = Mathf.Lerp(dayIntensity, nightDownIntensity, t);
                float finalDown = targetDown * flickerFactor;
                downLight.intensity = finalDown;
                downLight.enabled = finalDown > 0.001f;
            }

            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            // Atur visual bohlam luar
            if (bulbRenderer != null)
            {
                bulbRenderer.GetPropertyBlock(propBlock);
                Color targetBulb = Color.Lerp(dayBulbColor, nightBulbColor * flickerFactor, t);
                propBlock.SetColor(BaseColorID, targetBulb);
                Color targetBulbEmission = Color.Lerp(Color.black, nightBulbColor * flickerFactor, t);
                propBlock.SetColor(EmissionColorID, targetBulbEmission);
                bulbRenderer.SetPropertyBlock(propBlock);
            }

            // Atur visual filamen pijar dalam
            if (filamentRenderer != null)
            {
                filamentRenderer.GetPropertyBlock(propBlock);
                Color targetFil = Color.Lerp(dayFilamentColor, nightFilamentColor * flickerFactor, t);
                propBlock.SetColor(BaseColorID, targetFil);
                Color targetFilEmission = Color.Lerp(Color.black, nightFilamentColor * flickerFactor, t);
                propBlock.SetColor(EmissionColorID, targetFilEmission);
                filamentRenderer.SetPropertyBlock(propBlock);
            }
        }

        /// <summary>
        /// Menghitung faktor kedip Perlin noise pada waktu tertentu.
        /// </summary>
        public float EvaluateFlicker(float time)
        {
            if (!enableFlicker) return 1.0f;
            float noise = Mathf.PerlinNoise(time * flickerSpeed + seed, 0f);
            return 1.0f + (noise - 0.5f) * 2.0f * flickerAmount;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.58f, 0.18f, 0.25f);
            Vector3 lightPos = lampLight != null ? lampLight.transform.position : transform.position + Vector3.up * 2.2f;
            Gizmos.DrawWireSphere(lightPos, pointRange);
        }
    }
}
