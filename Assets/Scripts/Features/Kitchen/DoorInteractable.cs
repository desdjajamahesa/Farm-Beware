using UnityEngine;
using FeaturesInteraction;

/// <summary>
/// Pintu: menjembatani pemain antar area dalam scene yang sama (Opsi A saat ini).
/// Saat di-interact, player di-teleport ke spawnPoint tujuan.
/// Untuk transisi antar scene sungguhan, ganti implementasi dengan SceneManager + bootstrap.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Posisi spawn tujuan (mis. marker 'Spawn_Backyard').")]
    [SerializeField] private Transform spawnPoint;

    public void Interact(GameObject interactor)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("DoorInteractable: spawnPoint belum di-set.");
            return;
        }

        PlayerControl player = interactor != null ? interactor.GetComponent<PlayerControl>() : null;
        if (player == null)
            player = FindFirstObjectByType<PlayerControl>();

        if (player == null)
            return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = spawnPoint.position;
        }
        else
        {
            player.transform.position = spawnPoint.position;
        }
    }
}