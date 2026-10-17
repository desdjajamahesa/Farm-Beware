using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FarmBeware.Editor.Audio
{
    /// <summary>
    /// Utility generator for high-definition 16-bit 44.1kHz PCM WAV combat audio assets.
    /// Synthesizes procedural combat sound effects directly into Assets/Audio/SFX/Combat/
    /// without any third-party asset store or external recording dependencies.
    /// </summary>
    public static class CombatAudioClipGenerator
    {
        private const int SampleRate = 44100;
        private const string OutputFolder = "Assets/Audio/SFX/Combat";

        [MenuItem("Tools/Farm-Beware/Audio/Generate Combat Audio Clips (WAV)")]
        public static void GenerateAllCombatAudioClips()
        {
            string absoluteDirPath = Path.Combine(Application.dataPath, "Audio/SFX/Combat");
            if (!Directory.Exists(absoluteDirPath))
            {
                Directory.CreateDirectory(absoluteDirPath);
            }

            Debug.Log("[CombatAudioClipGenerator] Starting synthesis of combat audio WAV assets...");

            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Swing_Light.wav"), GenerateSwingLight());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Swing_Medium.wav"), GenerateSwingMedium());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Swing_Heavy.wav"), GenerateSwingHeavy());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Swing_Dash.wav"), GenerateSwingDash());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Swing_Skill.wav"), GenerateSwingSkill());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Swing_Punch.wav"), GenerateSwingPunch());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Hit_Slice.wav"), GenerateHitSlice());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Hit_Heavy.wav"), GenerateHitHeavy());
            SaveWavFile(Path.Combine(absoluteDirPath, "SFX_Weapon_Hit_Crit.wav"), GenerateHitCrit());

            AssetDatabase.Refresh();

            // Set optimal audio import settings for low-latency SFX
            ConfigureImportSettings(OutputFolder);

            Debug.Log($"[CombatAudioClipGenerator] Successfully generated 9 combat audio clips in '{OutputFolder}'.");
        }

        #region Synthesis Procedures

        /// <summary>
        /// Light sword swing whoosh: fast, sharp blade cutting air (0.16s).
        /// </summary>
        private static float[] GenerateSwingLight()
        {
            const float duration = 0.16f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                // Asymmetric attack/decay bell curve (peaking at 35% of duration)
                float envelope = Mathf.Pow(Mathf.Sin(norm * Mathf.PI), 1.6f);

                // Pitch sweep: 1500Hz down to 280Hz
                float freq = Mathf.Lerp(1500f, 280f, Mathf.Pow(norm, 0.7f));
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Band-passed white noise for air friction
                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                filterState += (rawNoise - filterState) * Mathf.Clamp01(freq / (SampleRate * 0.5f));

                float sample = (tone * 0.45f + filterState * 0.55f) * envelope;
                samples[i] = SoftClip(sample * 1.15f);
            }
            return samples;
        }

        /// <summary>
        /// Medium sword swing whoosh: fuller body with resonant blade sweep (0.22s).
        /// </summary>
        private static float[] GenerateSwingMedium()
        {
            const float duration = 0.22f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                float envelope = Mathf.Pow(Mathf.Sin(norm * Mathf.PI), 1.5f);

                // Pitch sweep: 1150Hz down to 180Hz
                float freq = Mathf.Lerp(1150f, 180f, Mathf.Pow(norm, 0.8f));
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float toneHarmonic = Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f;

                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                filterState += (rawNoise - filterState) * Mathf.Clamp01(freq / (SampleRate * 0.5f));

                float sample = (tone * 0.5f + toneHarmonic + filterState * 0.5f) * envelope;
                samples[i] = SoftClip(sample * 1.2f);
            }
            return samples;
        }

        /// <summary>
        /// Heavy sword swing whoosh: deep air displacement with low-frequency rumble (0.28s).
        /// </summary>
        private static float[] GenerateSwingHeavy()
        {
            const float duration = 0.28f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                float envelope = Mathf.Pow(Mathf.Sin(norm * Mathf.PI), 1.4f);

                // Low bass whoosh (550Hz down to 80Hz)
                float freq = Mathf.Lerp(650f, 75f, Mathf.Pow(norm, 0.6f));
                float subBass = Mathf.Sin(2f * Mathf.PI * freq * t);

                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                filterState += (rawNoise - filterState) * Mathf.Clamp01((freq * 1.5f) / (SampleRate * 0.5f));

                // Layered low thud
                float thud = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-norm * 8f) * 0.4f;

                float sample = (subBass * 0.6f + thud + filterState * 0.45f) * envelope;
                samples[i] = SoftClip(sample * 1.25f);
            }
            return samples;
        }

        /// <summary>
        /// Dash attack whoosh: sharp aerodynamic piercing thrust (0.17s).
        /// </summary>
        private static float[] GenerateSwingDash()
        {
            const float duration = 0.17f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                // Sharp forward peak at 20% duration
                float attack = Mathf.Clamp01(norm / 0.2f);
                float decay = Mathf.Exp(-(norm - 0.2f) * 9f);
                float envelope = (norm < 0.2f) ? attack : decay;

                // High-velocity piercing whistle (1800Hz down to 350Hz)
                float freq = Mathf.Lerp(1800f, 350f, norm);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);

                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                filterState += (rawNoise - filterState) * Mathf.Clamp01((freq * 0.8f) / (SampleRate * 0.5f));

                float sample = (tone * 0.4f + filterState * 0.6f) * envelope;
                samples[i] = SoftClip(sample * 1.2f);
            }
            return samples;
        }

        /// <summary>
        /// Skill whirlwind slash: modulated resonant energy spin with dual-blade sweep (0.38s).
        /// </summary>
        private static float[] GenerateSwingSkill()
        {
            const float duration = 0.38f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                float envelope = Mathf.Pow(Mathf.Sin(norm * Mathf.PI), 1.3f);

                // Double spin modulation (whirlwind oscillating frequency)
                float mod = Mathf.Sin(2f * Mathf.PI * 8.5f * t) * 220f;
                float freq = Mathf.Lerp(1300f, 220f, norm) + mod;
                float tone1 = Mathf.Sin(2f * Mathf.PI * Mathf.Max(60f, freq) * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * Mathf.Max(120f, freq * 1.5f) * t) * 0.3f;

                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                filterState += (rawNoise - filterState) * Mathf.Clamp01(freq / (SampleRate * 0.5f));

                float sample = (tone1 * 0.5f + tone2 + filterState * 0.45f) * envelope;
                samples[i] = SoftClip(sample * 1.25f);
            }
            return samples;
        }

        /// <summary>
        /// Punch whoosh: bare hands air swish (0.14s).
        /// </summary>
        private static float[] GenerateSwingPunch()
        {
            const float duration = 0.14f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float filterState = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                float envelope = Mathf.Pow(Mathf.Sin(norm * Mathf.PI), 1.6f);

                // Low blunt air displacement (320Hz down to 90Hz)
                float freq = Mathf.Lerp(320f, 90f, norm);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);

                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                filterState += (rawNoise - filterState) * Mathf.Clamp01(freq / (SampleRate * 0.5f));

                float sample = (tone * 0.55f + filterState * 0.45f) * envelope;
                samples[i] = SoftClip(sample * 1.15f);
            }
            return samples;
        }

        /// <summary>
        /// Blade hit slice: razor metallic edge impact followed by organic flesh impact (0.19s).
        /// </summary>
        private static float[] GenerateHitSlice()
        {
            const float duration = 0.19f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                // 1. Blade slice scrape transient (2200Hz down to 450Hz)
                float sliceFreq = Mathf.Lerp(2200f, 450f, norm);
                float sliceTone = Mathf.Sin(2f * Mathf.PI * sliceFreq * t);
                float sliceEnv = Mathf.Exp(-norm * 20f);
                float sliceNoise = UnityEngine.Random.Range(-1f, 1f) * Mathf.Exp(-norm * 28f) * 0.4f;

                // 2. Flesh impact thud (160Hz down to 50Hz)
                float thudFreq = Mathf.Lerp(160f, 48f, norm);
                float thudTone = Mathf.Sin(2f * Mathf.PI * thudFreq * t);
                float thudEnv = Mathf.Exp(-norm * 12f);

                float combined = (sliceTone * 0.45f + sliceNoise) * sliceEnv + (thudTone * 0.75f) * thudEnv;
                samples[i] = SoftClip(combined * 1.1f);
            }
            return samples;
        }

        /// <summary>
        /// Heavy hit impact: deep sub-bass crunch with bone-cracking transient (0.25s).
        /// </summary>
        private static float[] GenerateHitHeavy()
        {
            const float duration = 0.25f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                // Deep sub thump (110Hz -> 35Hz)
                float subFreq = Mathf.Lerp(110f, 35f, norm);
                float subTone = Mathf.Sin(2f * Mathf.PI * subFreq * t);
                float subEnv = Mathf.Exp(-norm * 9f);

                // Crack texture
                float crack = UnityEngine.Random.Range(-1f, 1f) * Mathf.Exp(-norm * 32f) * 0.5f;

                float sample = (subTone * 0.8f + crack) * subEnv;
                samples[i] = SoftClip(sample * 1.2f);
            }
            return samples;
        }

        /// <summary>
        /// Critical hit impact: sharp blade bell ring layered with visceral explosion (0.32s).
        /// </summary>
        private static float[] GenerateHitCrit()
        {
            const float duration = 0.32f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                // High metallic bell chime (2800Hz & 3500Hz)
                float chime1 = Mathf.Sin(2f * Mathf.PI * 2800f * t);
                float chime2 = Mathf.Sin(2f * Mathf.PI * 3500f * t) * 0.5f;
                float chimeEnv = Mathf.Exp(-norm * 14f);

                // Heavy sub thump
                float thudFreq = Mathf.Lerp(180f, 40f, norm);
                float thudTone = Mathf.Sin(2f * Mathf.PI * thudFreq * t);
                float thudEnv = Mathf.Exp(-norm * 8f);

                float crack = UnityEngine.Random.Range(-1f, 1f) * Mathf.Exp(-norm * 25f) * 0.4f;

                float sample = (chime1 + chime2) * 0.35f * chimeEnv + (thudTone * 0.7f + crack) * thudEnv;
                samples[i] = SoftClip(sample * 1.25f);
            }
            return samples;
        }

        private static float SoftClip(float x)
        {
            // Cubic soft-knee saturation
            if (x > 1.0f) return 1.0f;
            if (x < -1.0f) return -1.0f;
            return x - (x * x * x) / 3f;
        }

        #endregion

        #region WAV Encoder & File Serialization

        /// <summary>
        /// Writes an IEEE Float array to standard 16-bit PCM RIFF WAV format.
        /// </summary>
        private static void SaveWavFile(string filepath, float[] samples)
        {
            using (var stream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int channels = 1; // Mono
                int bytesPerSample = 2; // 16-bit
                int dataSize = samples.Length * bytesPerSample;

                // 1. RIFF Header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // 2. 'fmt ' Subchunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk size (16 for PCM)
                writer.Write((short)1); // AudioFormat: 1 = PCM
                writer.Write((short)channels);
                writer.Write(SampleRate);
                writer.Write(SampleRate * channels * bytesPerSample); // ByteRate
                writer.Write((short)(channels * bytesPerSample)); // BlockAlign
                writer.Write((short)(bytesPerSample * 8)); // BitsPerSample: 16

                // 3. 'data' Subchunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                // Write 16-bit PCM samples
                for (int i = 0; i < samples.Length; i++)
                {
                    short pcmSample = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    writer.Write(pcmSample);
                }
            }
        }

        private static void ConfigureImportSettings(string folderPath)
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    importer.forceToMono = true;
                    importer.loadInBackground = false;

                    AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                    settings.preloadAudioData = true;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                    importer.defaultSampleSettings = settings;

                    importer.SaveAndReimport();
                }
            }
        }

        #endregion
    }
}
