using UnityEngine;
using FeaturesInteraction;
using FarmBeware.Core.Runtime;

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

public class DoorInteractable : MonoBehaviour, IInteractable, IDynamicLabelProvider, IObstructionExempt
{
    [Header("Spawn Points (Two-Way)")]
    [Tooltip("Spawn point when player is inside (teleporting outside).")]
    [SerializeField] private Transform spawnPointInside;
    
    [Tooltip("Spawn point when player is outside (teleporting inside).")]
    [SerializeField] private Transform spawnPointOutside;

#pragma warning disable 0414
    [Header("Detection")]
    [Tooltip("Axis to use for inside/outside detection.")]
    [SerializeField] private ThresholdAxis thresholdAxis = ThresholdAxis.Z;
    
    [Tooltip("Threshold value on selected axis: player coordinate > threshold = inside (for X/Z), player coordinate < threshold = inside (for Y).")]
    [SerializeField] private float insideThreshold = 14.0f;
#pragma warning restore 0414

    [Header("Fade Effect")]
    [Tooltip("Fade in/out duration in seconds.")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Tooltip("Whether to use screen fade effect during teleport.")]
    [SerializeField] private bool useFadeEffect = true;

    [Header("Dynamic Destination Labels")]
    [Tooltip("Label displayed when player is outside approaching to enter (e.g. 'Bedroom', 'House')")]
    [SerializeField] private string enterLabel = "Bedroom";

    [Tooltip("Label displayed when player is inside approaching to exit (e.g. 'Living Room', 'Outside')")]
    [SerializeField] private string exitLabel = "Living Room";

    private WorldLabel worldLabel;
    private IPlayerContext cachedPlayer;

    private void Awake()
    {
        worldLabel = GetComponent<WorldLabel>();
        if (worldLabel == null)
            worldLabel = GetComponentInChildren<WorldLabel>();
        if (worldLabel == null)
            worldLabel = gameObject.AddComponent<WorldLabel>();
    }

    private void Start()
    {
        if (cachedPlayer == null)
            cachedPlayer = ServiceLocator.Resolve<IPlayerContext>();
        UpdateDynamicLabel();
    }

    private void Update()
    {
        if (cachedPlayer == null)
            cachedPlayer = ServiceLocator.Resolve<IPlayerContext>();

        UpdateDynamicLabel();
    }

    void IDynamicLabelProvider.UpdateDynamicLabel(GameObject interactor) => UpdateDynamicLabel();

    public void UpdateDynamicLabel()
    {
        if (worldLabel == null)
            worldLabel = GetComponent<WorldLabel>() ?? GetComponentInChildren<WorldLabel>();

        if (cachedPlayer == null)
            cachedPlayer = ServiceLocator.Resolve<IPlayerContext>();

        if (worldLabel == null || spawnPointInside == null || spawnPointOutside == null || cachedPlayer == null) return;

        float distToInside = Vector3.Distance(cachedPlayer.Transform.position, spawnPointInside.position);
        float distToOutside = Vector3.Distance(cachedPlayer.Transform.position, spawnPointOutside.position);
        bool isInside = distToInside < distToOutside;

        worldLabel.displayName = isInside ? exitLabel : enterLabel;
    }

    public void Interact(GameObject interactor)
    {
        if (spawnPointInside == null || spawnPointOutside == null)
        {
            Debug.LogWarning("[DoorInteractable] spawnPointInside or spawnPointOutside is not assigned!");
            return;
        }

        IPlayerContext player = interactor != null ? interactor.GetComponent<IPlayerContext>() : ServiceLocator.Resolve<IPlayerContext>();

        if (player == null)
            return;

        // Kunci gerakan pemain selama proses teleport
        player.IsInputLocked = true;

        // Deteksi posisi player: bandingkan jarak ke spawnPointInside vs spawnPointOutside
        // Jika lebih dekat ke luar -> target adalah ke dalam (spawnPointInside)
        // Jika lebih dekat ke dalam -> target adalah ke luar (spawnPointOutside)
        float distToInside = Vector3.Distance(player.Transform.position, spawnPointInside.position);
        float distToOutside = Vector3.Distance(player.Transform.position, spawnPointOutside.position);
        bool isInside = distToInside < distToOutside;

        Transform targetSpawn = isInside ? spawnPointOutside : spawnPointInside;

        var fade = ServiceLocator.Resolve<IFadeService>();
        if (useFadeEffect && fade != null)
        {
            StartCoroutine(TeleportWithFade(player, targetSpawn.position, isInside, fade));
        }
        else
        {
            TeleportPlayer(player, targetSpawn.position);
            player.IsInputLocked = false; // Langsung buka kunci jika tanpa fade
        }

        Debug.Log($"[DoorInteractable] Player teleported to " + (isInside ? "outside" : "inside") + " via " + gameObject.name);
    }

    private System.Collections.IEnumerator TeleportWithFade(IPlayerContext player, Vector3 targetPosition, bool isInside, IFadeService fade)
    {
        // Fade to black
        fade.FadeIn(fadeDuration);
        yield return new WaitForSeconds(fadeDuration);

        // Teleport player
        TeleportPlayer(player, targetPosition);

        // Small delay to ensure position is set
        yield return null;

        // Fade back to clear
        fade.FadeOut(fadeDuration);
        yield return new WaitForSeconds(fadeDuration);

        // Buka kunci gerakan setelah fade selesai
        if (player != null)
            player.IsInputLocked = false;
    }

    private void TeleportPlayer(IPlayerContext player, Vector3 targetPosition)
    {
        Rigidbody rb = player.Transform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = targetPosition;
        }
        player.Transform.position = targetPosition;
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