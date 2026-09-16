using UnityEngine;

/// <summary>
/// Feedback suara kecil untuk stasiun dapur (Sink / Kompor).
/// Murni visual/audio: subscribe OnProcessCompleted -> bunyi "pop" prosedural
/// (AudioClip.Create, tanpa aset). Cooldown mencegah bunyi dobel saat 2 slot
/// selesai hampir bersamaan. Layak juga digunakan oleh fitur lain di masa depan.
/// </summary>
public class KitchenStationSoundFx : MonoBehaviour
{
    [Tooltip("Stasiun yang dipantau (Sink / Kompor). Jika kosong, memakai komponen sendiri.")]
    [SerializeField] private KitchenStation station;

    [Tooltip("Volume suara selesai.")]
    [SerializeField] private float soundVolume = 0.8f;

    [Tooltip("Jeda minimum antar bunyi agar 2 slot yang selesai bersamaan tidak dobel.")]
    [SerializeField] private float soundCooldown = 0.5f;

    private AudioSource audioSource;
    private AudioClip generatedPop;
    private float lastSoundTime = -100f;

    private void OnEnable()
    {
        if (station == null)
            station = GetComponent<KitchenStation>();

        if (station == null)
            return;

        station.OnProcessCompleted += OnProcessCompleted;
    }

    private void OnDisable()
    {
        if (station == null)
            return;

        station.OnProcessCompleted -= OnProcessCompleted;
    }

    private void OnProcessCompleted(int slotIndex)
    {
        if (Time.time - lastSoundTime < soundCooldown || soundVolume <= 0f)
            return;

        lastSoundTime = Time.time;

        Debug.Log("[KitchenStationSoundFx] " + gameObject.name + ": slot " + slotIndex + " selesai, bunyi diputar.");

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (generatedPop == null)
            generatedPop = CreatePopClip();
        if (generatedPop != null)
            audioSource.PlayOneShot(generatedPop, soundVolume);
    }

    private static AudioClip CreatePopClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.14f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(1400f, 400f, t / duration);
            float envelope = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ProgressPop", samples, 1, sampleRate, false);
        if (clip != null)
            clip.SetData(data, 0);
        return clip;
    }
}