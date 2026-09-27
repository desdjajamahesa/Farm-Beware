using UnityEngine;

namespace FeaturesEnvironment
{
    /// <summary>
    /// Animasi kedipan cahaya api unggun organik berbasis Perlin Noise.
    /// Mengatur intensitas dan sedikit pergeseran posisi untuk simulasi nyala api alami.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CampfireLightFlicker : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 8.5f;
        [SerializeField] private float flickerRange = 1.8f;
        [SerializeField] private float flickerSpeed = 7.0f;

        private Light fireLight;
        private Vector3 basePosition;
        private float noiseOffset;

        private void Awake()
        {
            fireLight = GetComponent<Light>();
            basePosition = transform.localPosition;
            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (fireLight == null) return;

            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
            fireLight.intensity = baseIntensity + (noise - 0.5f) * flickerRange;

            // Pergeseran mikro posisi lidah api
            float posX = (Mathf.PerlinNoise(Time.time * 4f, noiseOffset + 10f) - 0.5f) * 0.08f;
            float posZ = (Mathf.PerlinNoise(Time.time * 4f, noiseOffset + 20f) - 0.5f) * 0.08f;
            transform.localPosition = basePosition + new Vector3(posX, 0f, posZ);
        }
    }
}
