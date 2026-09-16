using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls the player Capsule character with smooth WASD movement,
/// jumping, gravity, sprint, and automatic wall collision handling via CharacterController.
/// Keeps capsule 100% full and elevated on top of the floor without sinking.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 9.0f;
    public float sprintSpeed = 15.0f;
    public float rotationSpeed = 14.0f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // Default floor Y level is 0.0f, capsule height is 2.0f (half height = 1.0f)
    private const float GroundY = 1.0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero; // Center aligned with capsule mesh origin
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45.0f;
            controller.skinWidth = 0.02f;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Ensure player starts 100% full above the floor surface
        if (transform.position.y < GroundY)
        {
            Vector3 pos = transform.position;
            pos.y = GroundY;
            transform.position = pos;
        }
    }

    void Update()
    {
        if (controller == null) return;

        isGrounded = controller.isGrounded || transform.position.y <= GroundY + 0.05f;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2.0f;

            // Clamp Y position so capsule NEVER sinks into the floor plane
            if (transform.position.y < GroundY)
            {
                Vector3 pos = transform.position;
                pos.y = GroundY;
                transform.position = pos;
            }
        }

        // Get Input
        float horizontal = 0f;
        float vertical = 0f;
        bool isSprinting = false;
        bool jumpPressed = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;

            isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (horizontal == 0f && vertical == 0f)
        {
            try
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
                if (!isSprinting) isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!jumpPressed) jumpPressed = Input.GetButtonDown("Jump");
            }
            catch
            {
                // Ignore legacy input exception
            }
        }
        #endif

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 moveDir;

            if (cameraTransform != null)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                moveDir = camForward * inputDir.z + camRight * inputDir.x;
            }
            else
            {
                moveDir = inputDir;
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            controller.Move(moveDir * currentSpeed * Time.deltaTime);
        }

        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Keep capsule full above ground
        if (transform.position.y < GroundY)
        {
            Vector3 clampedPos = transform.position;
            clampedPos.y = GroundY;
            transform.position = clampedPos;
        }
    }
}
