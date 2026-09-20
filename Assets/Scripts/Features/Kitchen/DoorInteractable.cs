using UnityEngine;
using FeaturesInteraction;
using FeaturesCommon;

/// <summary>
/// Pintu: menjembatani pemain antar area dalam scene yang sama (Opsi A saat ini).
/// Saat di-interact, player di-teleport ke spawnPoint tujuan.
/// Untuk transisi antar scene sungguhan, ganti implementasi dengan SceneManager + bootstrap.
/// </summary>
public enum ThresholdAxis
{
    X,
    Y,
    Z
}

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Spawn Points (Two-Way)")]
    [Tooltip("Spawn point ketika player berada di DALAM (keluar ke luar)")]
    [SerializeField] private Transform spawnPointInside;
    
    [Tooltip("Spawn point ketika player berada di LUAR (masuk ke dalam)")]
    [SerializeField] private Transform spawnPointOutside;

    [Header("Detection")]
    [Tooltip("Axis to use for inside/outside detection")]
    [SerializeField] private ThresholdAxis thresholdAxis = ThresholdAxis.Z;
    
    [Tooltip("Threshold value on selected axis: player coordinate > threshold = inside (for X/Z), player coordinate < threshold = inside (for Y)")]
    [SerializeField] private float insideThreshold = 14.0f;

    [Header("Fade Effect")]
    [Tooltip("Durasi fade in/out (detik)")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Tooltip("Apakah menggunakan efek fade saat teleport")]
    [SerializeField] private bool useFadeEffect = true;

    public void Interact(GameObject interactor)
    {
        if (spawnPointInside == null || spawnPointOutside == null)
        {
            Debug.LogWarning("DoorInteractable: spawnPointInside atau spawnPointOutside belum di-set.");
            return;
        }

        PlayerControl player = interactor != null ? interactor.GetComponent<PlayerControl>() : null;
        if (player == null)
            player = FindFirstObjectByType<PlayerControl>();

        if (player == null)
            return;

        // Kunci gerakan pemain selama proses teleport
        player.StopMovement();
        player.isInputLocked = true;

        // Deteksi posisi player: bandingkan jarak ke spawnPointInside vs spawnPointOutside
        // Jika lebih dekat ke luar -> target adalah ke dalam (spawnPointInside)
        // Jika lebih dekat ke dalam -> target adalah ke luar (spawnPointOutside)
        float distToInside = Vector3.Distance(player.transform.position, spawnPointInside.position);
        float distToOutside = Vector3.Distance(player.transform.position, spawnPointOutside.position);
        bool isInside = distToInside < distToOutside;

        Transform targetSpawn = isInside ? spawnPointOutside : spawnPointInside;

        if (useFadeEffect && FadeManager.Instance != null)
        {
            StartCoroutine(TeleportWithFade(player, targetSpawn.position, isInside));
        }
        else
        {
            TeleportPlayer(player, targetSpawn.position);
            player.isInputLocked = false; // Langsung buka kunci jika tanpa fade
        }

        Debug.Log($"[DoorInteractable] Player teleported to " + (isInside ? "outside" : "inside") + " via " + gameObject.name);
    }

    private System.Collections.IEnumerator TeleportWithFade(PlayerControl player, Vector3 targetPosition, bool isInside)
    {
        // Fade to black
        yield return FadeManager.Instance.FadeIn(fadeDuration);

        // Teleport player
        TeleportPlayer(player, targetPosition);

        // Small delay to ensure position is set
        yield return null;

        // Fade back to clear
        yield return FadeManager.Instance.FadeOut(fadeDuration);

        // Buka kunci gerakan setelah fade selesai
        if (player != null)
            player.isInputLocked = false;
    }

    private void TeleportPlayer(PlayerControl player, Vector3 targetPosition)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = targetPosition;
        }
        player.transform.position = targetPosition;
        Physics.SyncTransforms();
    }

    private float GetCoordinate(Vector3 pos, ThresholdAxis axis)
    {
        return axis switch
        {
            ThresholdAxis.X => pos.x,
            ThresholdAxis.Y => pos.y,
            ThresholdAxis.Z => pos.z,
            _ => pos.z
        };
    }
}