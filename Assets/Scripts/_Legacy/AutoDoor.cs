using UnityEngine;

/// <summary>
/// Hinge-based auto door: opens (rotates around local Y) when the player capsule
/// gets within triggerDistance, and closes after the player moves away.
/// Attach to a pivot GameObject; the door panel must be a child offset from the pivot
/// (the pivot acts as the door hinge).
/// </summary>
public class AutoDoor : MonoBehaviour
{
    [Header("Door Behavior")]
    public float openAngle = 90f;
    public float triggerDistance = 6f;
    public float openSpeed = 3f;
    public float closeSpeed = 4f;
    public float closeDelay = 1.5f;

    [Header("Optional")]
    public Transform playerTarget;

    private Transform _player;
    private Quaternion _closedRot;
    private bool _isOpen;
    private float _closeTimer;

    private void Start()
    {
        _closedRot = transform.localRotation;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (playerTarget != null)
        {
            _player = playerTarget;
            return;
        }
        GameObject capsule = GameObject.Find("PlayerCapsule");
        if (capsule != null)
        {
            _player = capsule.transform;
            return;
        }
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) _player = pc.transform;
    }

    private void Update()
    {
        if (_player == null) ResolvePlayer();
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist < triggerDistance)
        {
            _isOpen = true;
            _closeTimer = 0f;
        }
        else if (_isOpen)
        {
            _closeTimer += Time.deltaTime;
            if (_closeTimer >= closeDelay) _isOpen = false;
        }

        Quaternion target = _isOpen
            ? _closedRot * Quaternion.Euler(0f, openAngle, 0f)
            : _closedRot;

        float speed = _isOpen ? openSpeed : closeSpeed;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * speed);
    }
}
