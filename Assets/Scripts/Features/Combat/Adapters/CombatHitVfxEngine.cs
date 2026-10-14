using UnityEngine;

namespace FeaturesCombat.Adapters
{
    /// <summary>
    /// Centralized Combat Hit & Impact Feedback Engine.
    /// Provides visceral game feel on weapon impact:
    /// 1. Procedural blade slice & impact thump sound effects (Zero external asset dependency).
    /// 2. Procedural hit spark & slash particle bursts.
    /// 3. Zero-GC Hitstop dual-freeze (attacker pause + victim freeze).
    /// 4. Dynamic trauma camera shake.
    /// 5. Hit flash coordination.
    /// </summary>
    public class CombatHitVfxEngine : MonoBehaviour
    {
        private static CombatHitVfxEngine _instance;
        public static CombatHitVfxEngine Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CombatHitVfxEngine>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CombatHitVfxEngine");
                        _instance = go.AddComponent<CombatHitVfxEngine>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private AudioSource _audioSource;
        private AudioClip _bladeHitClip;
        private AudioClip _monsterDefeatClip;
        private ParticleSystem _sparkParticleSystem;
        private ParticleSystem _defeatParticleSystem;
        private static Material _sharedSparkMaterial;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureAudioSource();
            EnsureParticleSystems();
        }

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0.4f; // 40% 3D spatial, 60% punchy 2D direct
            }

            if (_bladeHitClip == null)
            {
                _bladeHitClip = GenerateBladeHitClip();
            }

            if (_monsterDefeatClip == null)
            {
                _monsterDefeatClip = GenerateMonsterDefeatClip();
            }
        }

        private void EnsureParticleSystems()
        {
            if (_sparkParticleSystem == null)
            {
                var sparkGo = new GameObject("HitSparkEmitter");
                sparkGo.transform.SetParent(transform, false);
                _sparkParticleSystem = sparkGo.AddComponent<ParticleSystem>();
                _sparkParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = _sparkParticleSystem.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.3f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 13f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                main.startColor = new Color(1.0f, 0.9f, 0.35f, 1.0f); // Bright golden-white
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 3.5f;

                var emission = _sparkParticleSystem.emission;
                emission.rateOverTime = 0;

                var shape = _sparkParticleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
                shape.radius = 0.1f;

                var colorOverLifetime = _sparkParticleSystem.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.6f), new GradientColorKey(new Color(0.8f, 0.2f, 0f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                colorOverLifetime.color = grad;

                var renderer = sparkGo.GetComponent<ParticleSystemRenderer>();
                if (_sharedSparkMaterial == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                    if (s != null)
                    {
                        _sharedSparkMaterial = new Material(s);
                        _sharedSparkMaterial.color = new Color(1f, 0.95f, 0.7f, 1f);
                    }
                }
                if (_sharedSparkMaterial != null)
                {
                    renderer.material = _sharedSparkMaterial;
                }
            }

            if (_defeatParticleSystem == null)
            {
                var defeatGo = new GameObject("DefeatBurstEmitter");
                defeatGo.transform.SetParent(transform, false);
                _defeatParticleSystem = defeatGo.AddComponent<ParticleSystem>();
                _defeatParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = _defeatParticleSystem.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.4f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                main.startColor = new Color(0.7f, 0.2f, 0.9f, 0.9f); // Dark shadow/violet burst
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 1.0f;

                var emission = _defeatParticleSystem.emission;
                emission.rateOverTime = 0;

                var shape = _defeatParticleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;

                var renderer = defeatGo.GetComponent<ParticleSystemRenderer>();
                if (_sharedSparkMaterial != null)
                {
                    renderer.material = _sharedSparkMaterial;
                }
            }
        }

        /// <summary>
        /// Triggers full combat impact feedback when player weapon hits an enemy.
        /// </summary>
        public void TriggerHit(Vector3 hitPoint, Vector3 attackDir, GameObject victim, GameObject attacker = null, bool isCrit = false)
        {
            EnsureAudioSource();
            EnsureParticleSystems();

            // 1. Procedural Blade Impact Audio
            if (_audioSource != null && _bladeHitClip != null)
            {
                _audioSource.pitch = Random.Range(0.93f, 1.07f);
                _audioSource.transform.position = hitPoint;
                _audioSource.PlayOneShot(_bladeHitClip, isCrit ? 1.0f : 0.85f);
            }

            // 2. Visual Spark Particles
            if (_sparkParticleSystem != null)
            {
                _sparkParticleSystem.transform.position = hitPoint;
                Vector3 emitDir = attackDir.sqrMagnitude > 0.01f ? attackDir.normalized : Vector3.up;
                _sparkParticleSystem.transform.rotation = Quaternion.LookRotation(emitDir);
                _sparkParticleSystem.Emit(isCrit ? 22 : 14);
            }

            // 3. Hit Flash on Victim
            if (victim != null)
            {
                var feedback = victim.GetComponent<HitFeedbackRenderer>() ?? victim.GetComponentInChildren<HitFeedbackRenderer>();
                if (feedback != null)
                {
                    feedback.TriggerFlash(isCrit ? 0.14f : 0.10f);
                }
            }

            // 4. Dual Hitstop (Briefly freeze attacker swing & victim)
            if (HitstopCoordinator.Instance != null)
            {
                float attackerStop = isCrit ? 0.065f : 0.045f;
                float victimStop = isCrit ? 0.11f : 0.08f;
                HitstopCoordinator.Instance.RegisterDualHitstop(attacker, victim, attackerStop, victimStop);
            }

            // 5. Trauma Camera Shake
            if (TraumaCameraShake.Instance != null)
            {
                TraumaCameraShake.Instance.AddTrauma(isCrit ? 0.36f : 0.22f);
            }
        }

        /// <summary>
        /// Triggers defeat audio and visual feedback when a monster dies.
        /// </summary>
        public void TriggerMonsterDefeated(Vector3 position, Vector3 direction)
        {
            EnsureAudioSource();
            EnsureParticleSystems();

            // 1. Defeat Audio
            if (_audioSource != null && _monsterDefeatClip != null)
            {
                _audioSource.pitch = Random.Range(0.95f, 1.05f);
                _audioSource.transform.position = position;
                _audioSource.PlayOneShot(_monsterDefeatClip, 0.95f);
            }

            // 2. Defeat Burst Particles
            if (_defeatParticleSystem != null)
            {
                _defeatParticleSystem.transform.position = position + Vector3.up * 0.5f;
                _defeatParticleSystem.Emit(20);
            }

            // 3. Impact Camera Trauma
            if (TraumaCameraShake.Instance != null)
            {
                TraumaCameraShake.Instance.AddTrauma(0.26f);
            }
        }

        /// <summary>
        /// Synthesizes a crisp blade slice + low frequency impact punch sound clip.
        /// Zero external audio files required.
        /// </summary>
        private static AudioClip GenerateBladeHitClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.16f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // 1. High frequency blade metallic slice (2200Hz down to 500Hz)
                float sliceFreq = Mathf.Lerp(2200f, 500f, progress);
                float sliceWave = Mathf.Sin(2f * Mathf.PI * sliceFreq * t);
                float sliceEnv = Mathf.Exp(-progress * 22f);

                // Noise component for razor edge friction
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 35f) * 0.4f;

                // 2. Low frequency body impact thud (150Hz decaying fast to 45Hz)
                float thudFreq = Mathf.Lerp(150f, 45f, progress);
                float thudWave = Mathf.Sin(2f * Mathf.PI * thudFreq * t);
                float thudEnv = Mathf.Exp(-progress * 14f);

                // Combined composite sample with soft saturation clipping
                float combined = (sliceWave * 0.45f + noise) * sliceEnv + (thudWave * 0.75f) * thudEnv;
                data[i] = Mathf.Clamp(combined, -0.98f, 0.98f);
            }

            AudioClip clip = AudioClip.Create("ProceduralBladeHit", samples, 1, sampleRate, false);
            if (clip != null)
            {
                clip.SetData(data, 0);
            }
            return clip;
        }

        /// <summary>
        /// Synthesizes a satisfying monster defeat crunch/burst audio clip.
        /// </summary>
        private static AudioClip GenerateMonsterDefeatClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.24f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Heavy sub-bass crunch (90Hz -> 30Hz)
                float subFreq = Mathf.Lerp(110f, 32f, progress);
                float subWave = Mathf.Sin(2f * Mathf.PI * subFreq * t);
                float subEnv = Mathf.Exp(-progress * 9f);

                // Crushing squelch texture
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 16f) * 0.35f;

                float sample = (subWave * 0.7f + noise) * subEnv;
                data[i] = Mathf.Clamp(sample, -0.98f, 0.98f);
            }

            AudioClip clip = AudioClip.Create("ProceduralMonsterDefeat", samples, 1, sampleRate, false);
            if (clip != null)
            {
                clip.SetData(data, 0);
            }
            return clip;
        }
    }
}
