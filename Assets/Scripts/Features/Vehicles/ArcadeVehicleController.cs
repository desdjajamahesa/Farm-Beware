using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesInteraction;
using FeaturesCamera;
using FarmBeware.Core.Runtime;
using FarmBeware.Features.Vehicles.PureLogic;
using FarmBeware.Features.Vehicles.UI;

namespace FarmBeware.Features.Vehicles
{
    /// <summary>
    /// Thin adapter MonoBehaviour controlling vehicle driving physics, input integration,
    /// enter/exit lifecycle, wheel steering/spinning, body suspension roll/pitch, and audio/particles.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class ArcadeVehicleController : MonoBehaviour, IInteractable, IObstructionExempt, IVehicleActor
    {
        [Header("Physics & Dynamics Core")]
        [SerializeField] private float vehicleMass = 1200f;
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.3f, 0.1f);
        [SerializeField] private float groundRayDistance = 1.0f;
        [SerializeField] private LayerMask groundLayer = ~0;

        [Header("Wheel Transforms")]
        [SerializeField] private Transform wheelFrontLeft;
        [SerializeField] private Transform wheelFrontRight;
        [SerializeField] private Transform wheelRearLeft;
        [SerializeField] private Transform wheelRearRight;
        [SerializeField] private float wheelRadius = 0.36f;

        [Header("Visual Suspensions & Body")]
        [Tooltip("Root of visual meshes that tilt on acceleration/braking and cornering.")]
        [SerializeField] private Transform visualBodyRoot;
        [SerializeField] private Transform steeringWheelTransform;

        [Header("Player Entry & Seating")]
        [SerializeField] private Transform driverSeatAnchor;
        [SerializeField] private Transform safeDismountAnchor;
        [SerializeField] private float interactionRadius = 5.5f;

        [Header("Audio & Juice")]
        [SerializeField] private AudioSource engineAudio;
        [SerializeField] private AudioSource hornAudio;
        [SerializeField] private ParticleSystem[] driftParticles;
        [SerializeField] private ParticleSystem exhaustParticles;

        private Rigidbody _rb;
        private VehicleDynamicsCore _dynamics;
        private UnityEngine.AI.NavMeshObstacle _navObstacle;
        private FeaturesEnvironment.FarmTruckController _truckLighting;

        // Player Stored State on Entry
        private Transform _playerOriginalParent;
        private Vector3 _playerOriginalScale = Vector3.one;

        // Runtime Driving State
        private bool _isDriverOccupied = false;
        private float _doorDebounceTimer = 0f;
        private GameObject _occupantPlayer;
        private PlayerControl _playerControl;
        private Rigidbody _occupantPlayerRb;
        private PlayerEquipment _playerEquipment;
        private PlayerInteractor _playerInteractor;
        private AnimatorCullingMode _originalAnimatorCullingMode = AnimatorCullingMode.CullUpdateTransforms;
        private RigidbodyInterpolation _originalPlayerInterpolation = RigidbodyInterpolation.Interpolate;
        private float _currentSpeedMps = 0f;
        private float _currentSteerAngle = 0f;
        private float _currentPitch = 0f;
        private float _currentRoll = 0f;
        private float _wheelSpinAngle = 0f;
        private float _lastForwardSpeed = 0f;
        private bool _isGrounded = true;

        private Transform _rollPivotFL;
        private Transform _rollPivotFR;
        private Transform _rollPivotRL;
        private Transform _rollPivotRR;

        public bool IsDriverOccupied => _isDriverOccupied;
        public float CurrentSpeedKmh => Mathf.Abs(_currentSpeedMps) * 3.6f;
        public VehicleDynamicsCore Dynamics => _dynamics;

        private void Reset()
        {
            EnsureSubAssemblies();
        }

        private void OnValidate()
        {
            EnsureSubAssemblies(allowCreate: false);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.mass = vehicleMass;
                _rb.centerOfMass = centerOfMassOffset;
                _rb.linearDamping = 0.5f;
                _rb.angularDamping = 1.0f;
                _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            _navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            _dynamics = new VehicleDynamicsCore();
            _truckLighting = GetComponent<FeaturesEnvironment.FarmTruckController>();

            EnsureSubAssemblies();
            SetupEngineAudio();
            SetupHornAudio();
        }

        private void Start()
        {
            // Ensure Driving HUD is registered
            if (VehicleDrivingHUD.Instance == null)
            {
                var hudGo = new GameObject("VehicleDrivingHUD_Host");
                hudGo.AddComponent<VehicleDrivingHUD>();
            }
        }

        #region IInteractable Implementation
        public bool CanInteract(GameObject interactor)
        {
            if (_isDriverOccupied) return false;
            if (interactor == null) return false;

            // 1. Check distance to vehicle transform root
            float dist = Vector3.Distance(transform.position, interactor.transform.position);
            if (dist <= interactionRadius) return true;

            // 2. Check distance to driver seat anchor
            if (driverSeatAnchor != null)
            {
                float distSeat = Vector3.Distance(driverSeatAnchor.position, interactor.transform.position);
                if (distSeat <= interactionRadius) return true;
            }

            // 3. Check distance to safe dismount anchor
            if (safeDismountAnchor != null)
            {
                float distDismount = Vector3.Distance(safeDismountAnchor.position, interactor.transform.position);
                if (distDismount <= interactionRadius) return true;
            }

            // 4. Check distance to any child collider or trigger volume
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col.isTrigger && col.bounds.Contains(interactor.transform.position))
                    return true;

                Vector3 closest = col.ClosestPoint(interactor.transform.position);
                if (Vector3.Distance(closest, interactor.transform.position) <= 4.0f)
                    return true;
            }
            return false;
        }

        public void Interact(GameObject interactor)
        {
            if (_doorDebounceTimer > 0f) return;

            if (!_isDriverOccupied)
            {
                EnterVehicle(interactor);
            }
        }
        #endregion

        #region Enter / Exit Lifecycle
        public void EnterVehicle(GameObject player)
        {
            if (_isDriverOccupied || player == null) return;

            _doorDebounceTimer = 0.4f;
            _occupantPlayer = player;
            _playerControl = player.GetComponent<PlayerControl>();
            _occupantPlayerRb = player.GetComponent<Rigidbody>();
            _playerEquipment = player.GetComponent<PlayerEquipment>();
            _playerInteractor = player.GetComponent<PlayerInteractor>();
            _playerOriginalParent = player.transform.parent;
            if (_playerOriginalParent == null || _playerOriginalParent == transform || _playerOriginalParent.IsChildOf(transform))
            {
                var entities = GameObject.Find("_ENTITIES");
                _playerOriginalParent = entities != null ? entities.transform : null;
            }
            _playerOriginalScale = player.transform.localScale;

            // Lock player control & disable component updates while driving
            if (_playerControl != null)
            {
                _playerControl.AddInputLock("VehicleDriving");
                _playerControl.enabled = false;
            }

            // Disable player equipment & interaction updates so indicators and prompts don't tick
            if (_playerEquipment != null)
            {
                _playerEquipment.enabled = false;
            }
            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = false;
            }

            // Configure player Rigidbody physics to prevent PhysX drift/lag
            if (_occupantPlayerRb != null)
            {
                _originalPlayerInterpolation = _occupantPlayerRb.interpolation;
                _occupantPlayerRb.linearVelocity = Vector3.zero;
                _occupantPlayerRb.angularVelocity = Vector3.zero;
                _occupantPlayerRb.isKinematic = true;
                _occupantPlayerRb.interpolation = RigidbodyInterpolation.None;
                _occupantPlayerRb.detectCollisions = false;
            }

            // Disable player collisions & hide non-driver accessories while keeping character meshes visible
            var colliders = player.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            SetNonCharacterAccessoriesActive(player, false);

            // Trigger Driving seated animation state with persistent culling mode
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                _originalAnimatorCullingMode = anim.cullingMode;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.SetBool("Driving", true);
                anim.SetBool("Grounded", true);
                anim.SetFloat("Vel", 0f);
                anim.SetBool("Idle", true);
                anim.SetBool("Sprinting", false);
                anim.ResetTrigger("Attack");
                anim.ResetTrigger("SkillAttack");
                anim.ResetTrigger("PlantSeed");
                anim.ResetTrigger("Parry");
                anim.ResetTrigger("HitReact");
                anim.ResetTrigger("LookAround");
                anim.ResetTrigger("HeavyAttack");
                anim.Play("Driving", 0, 0f);
            }

            // Dock player safely: parent to driverSeatAnchor or transform root, guaranteeing clean scale 1,1,1
            Transform dockTarget = driverSeatAnchor != null ? driverSeatAnchor : transform;
            player.transform.SetParent(dockTarget, false);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            if (_occupantPlayerRb != null)
            {
                _occupantPlayerRb.position = dockTarget.position;
                _occupantPlayerRb.rotation = dockTarget.rotation;
            }
            Physics.SyncTransforms();

            // Adjust Camera target & orthographic size
            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.target = transform;
                IsometricCameraController.Instance.smoothSpeed = 8f;
                IsometricCameraController.Instance.orthographicSize = 7.5f;
            }

            // Switch NavMeshObstacle carving off so the truck can move
            if (_navObstacle != null)
            {
                _navObstacle.carving = false;
            }

            _isDriverOccupied = true;

            // Start Audio & HUD
            if (engineAudio != null && !engineAudio.isPlaying)
            {
                engineAudio.Play();
            }

            if (VehicleDrivingHUD.Instance != null)
            {
                VehicleDrivingHUD.Instance.Show();
            }

            Debug.Log("[ArcadeVehicleController] Player entered vehicle. Driving mode active.");
        }

        public void ExitVehicle()
        {
            if (!_isDriverOccupied || _occupantPlayer == null) return;

            _doorDebounceTimer = 0.4f;

            // 0. Immediately bring vehicle to a complete, safe halt (Parking Brake)
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _currentSpeedMps = 0f;
            _currentSteerAngle = 0f;

            // 1. Calculate safe dismount position on terrain to the left of the driver door
            Vector3 exitPos = safeDismountAnchor != null 
                ? safeDismountAnchor.position 
                : transform.position + transform.TransformDirection(new Vector3(-2.6f, 0.2f, 0.2f));

            Ray ray = new Ray(exitPos + Vector3.up * 4f, Vector3.down);
            var hits = Physics.RaycastAll(ray, 10f, groundLayer, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    exitPos = hit.point + Vector3.up * 0.05f;
                    break;
                }
            }

            // 2. Return to original parent with worldPositionStays: true (strictly preserving _ENTITIES root)
            if (_playerOriginalParent == null || _playerOriginalParent == transform || _playerOriginalParent.IsChildOf(transform))
            {
                var entities = GameObject.Find("_ENTITIES");
                _playerOriginalParent = entities != null ? entities.transform : null;
            }
            _occupantPlayer.transform.SetParent(_playerOriginalParent, true);

            // 3. Strictly restore original clean scale and restore player Rigidbody physics
            _occupantPlayer.transform.localScale = (_playerOriginalScale != Vector3.zero) ? _playerOriginalScale : Vector3.one;
            Quaternion exitRot = Quaternion.Euler(0f, transform.eulerAngles.y - 90f, 0f);

            if (_occupantPlayerRb != null)
            {
                _occupantPlayerRb.isKinematic = false;
                _occupantPlayerRb.interpolation = _originalPlayerInterpolation;
                _occupantPlayerRb.detectCollisions = true;
                _occupantPlayerRb.linearVelocity = Vector3.zero;
                _occupantPlayerRb.angularVelocity = Vector3.zero;
            }

            if (_playerControl != null)
            {
                _playerControl.enabled = true;
                _playerControl.RemoveInputLock("VehicleDriving");
                _playerControl.Teleport(exitPos, exitRot);
            }
            else
            {
                _occupantPlayer.transform.position = exitPos;
                _occupantPlayer.transform.rotation = exitRot;
            }

            if (_playerEquipment != null)
            {
                _playerEquipment.enabled = true;
            }

            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = true;
            }

            // 4. Force-reset child indicators (e.g. AttackRangeIndicator) to clean unit scale
            var indicator = _occupantPlayer.transform.Find("AttackRangeIndicator");
            if (indicator != null)
            {
                indicator.localPosition = Vector3.zero;
                indicator.localScale = Vector3.one;
            }
            Physics.SyncTransforms();

            // 6. Re-enable player colliders, character animation, and accessories
            var anim = _occupantPlayer.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.cullingMode = _originalAnimatorCullingMode;
                anim.SetBool("Driving", false);
                anim.SetBool("Grounded", true);
                anim.SetFloat("Vel", 0f);
                anim.SetBool("Idle", true);
                anim.SetBool("Sprinting", false);
                anim.Play("Idle", 0, 0f);
            }

            SetNonCharacterAccessoriesActive(_occupantPlayer, true);

            var colliders = _occupantPlayer.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }

            var renderers = _occupantPlayer.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                rend.enabled = true;
            }

            // 7. Restore camera target to player
            if (IsometricCameraController.Instance != null)
            {
                IsometricCameraController.Instance.target = _occupantPlayer.transform;
                IsometricCameraController.Instance.smoothSpeed = 5f;
                IsometricCameraController.Instance.orthographicSize = 6.0f;
                IsometricCameraController.Instance.SnapToTarget();
            }

            // 9. Re-engage NavMeshObstacle carving when parked
            if (_navObstacle != null)
            {
                _navObstacle.carving = true;
            }

            // 10. Hide HUD & stop engine audio
            if (VehicleDrivingHUD.Instance != null)
            {
                VehicleDrivingHUD.Instance.Hide();
            }

            if (engineAudio != null)
            {
                engineAudio.Stop();
            }

            _isDriverOccupied = false;
            _occupantPlayer = null;
            _playerControl = null;
            _occupantPlayerRb = null;
            _playerEquipment = null;
            _playerInteractor = null;

            Debug.Log("[ArcadeVehicleController] Player exited vehicle. Restored gameplay state cleanly.");
        }

        /// <summary>
        /// Instantly teleports the vehicle to target position and rotation, resetting all PhysX velocities.
        /// Also repositions seated occupant Rigidbody if occupied.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.position = position;
                _rb.rotation = rotation;
            }
            transform.position = position;
            transform.rotation = rotation;
            _currentSpeedMps = 0f;
            _currentSteerAngle = 0f;

            if (_isDriverOccupied && _occupantPlayer != null)
            {
                Transform dockTarget = driverSeatAnchor != null ? driverSeatAnchor : transform;
                if (_occupantPlayerRb != null)
                {
                    _occupantPlayerRb.position = dockTarget.position;
                    _occupantPlayerRb.rotation = dockTarget.rotation;
                }
                _occupantPlayer.transform.position = dockTarget.position;
                _occupantPlayer.transform.rotation = dockTarget.rotation;
            }

            Physics.SyncTransforms();
        }

        public void SetHeadlights(bool enable)
        {
            if (_truckLighting == null) _truckLighting = GetComponent<FeaturesEnvironment.FarmTruckController>();
            if (_truckLighting != null)
            {
                _truckLighting.SetHeadlights(enable);
            }
        }
        #endregion

        #region Physics Execution (FixedUpdate)
        private void FixedUpdate()
        {
            if (_dynamics == null) _dynamics = new VehicleDynamicsCore();

            float throttleInput = 0f;
            float steerInput = 0f;
            bool handbrake = false;

            if (_isDriverOccupied && Keyboard.current != null)
            {
                // Read Input System
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) throttleInput += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) throttleInput -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput -= 1f;
                handbrake = Keyboard.current.spaceKey.isPressed;
            }

            float dt = Time.fixedDeltaTime;

            // Ground Check
            _isGrounded = CheckGroundContact();

            // 1. Current Velocity Breakdown in Local Space
            Vector3 currentVel = _rb.linearVelocity;
            Vector3 localVel = transform.InverseTransformDirection(currentVel);
            _currentSpeedMps = localVel.z;
            float lateralSpeed = localVel.x;

            // 2. Pure Kinematics Target Speed Calculation
            float targetSpeed = _dynamics.CalculateForwardSpeed(throttleInput, _currentSpeedMps, handbrake, dt);
            _currentSteerAngle = _dynamics.CalculateSteeringAngle(steerInput, _currentSteerAngle, _currentSpeedMps, dt);

            if (_isGrounded)
            {
                // Smooth acceleration / braking towards target forward speed
                float accelRate = (Mathf.Abs(throttleInput) > 0.05f) ? _dynamics.AccelerationRate : _dynamics.BrakingRate;
                if (handbrake) accelRate = _dynamics.HandbrakeBrakingRate;
                float newForwardSpeed = Mathf.MoveTowards(localVel.z, targetSpeed, accelRate * dt);

                // Lateral grip / drift slip calculation
                float grip = _dynamics.CalculateLateralGrip(handbrake, lateralSpeed);
                float lateralDampRate = handbrake ? 3.5f : (grip * 18.0f);
                float newLateralSpeed = Mathf.MoveTowards(localVel.x, 0f, lateralDampRate * dt);

                // Retain vertical gravity velocity from PhysX
                Vector3 newLocalVel = new Vector3(newLateralSpeed, localVel.y, newForwardSpeed);
                _rb.linearVelocity = transform.TransformDirection(newLocalVel);

                // 3. Steering & Yaw Rotation (Ackermann Kinematics: strictly 0 when stationary)
                float yawRateDeg = _dynamics.CalculateAckermannYawRate(_currentSpeedMps, _currentSteerAngle, handbrake, lateralSpeed, dt);
                float yawDegrees = yawRateDeg * dt;

                if (Mathf.Abs(yawDegrees) > 0.0005f)
                {
                    Quaternion deltaRot = Quaternion.Euler(0f, yawDegrees, 0f);
                    _rb.MoveRotation(_rb.rotation * deltaRot);
                }
            }
            else
            {
                // In air, apply slight stabilizing downward pull
                _rb.AddForce(Physics.gravity * 0.5f, ForceMode.Acceleration);
            }

            // Reset angular velocity to avoid unphysical physics spinning
            _rb.angularVelocity = Vector3.zero;

            // 4. Compute accelerations for visual suspension body pitch/roll
            float forwardAccel = (_currentSpeedMps - _lastForwardSpeed) / dt;
            _lastForwardSpeed = _currentSpeedMps;
            float lateralAccel = lateralSpeed / dt;

            _currentPitch = _dynamics.CalculateSuspensionPitch(forwardAccel, _currentPitch, dt);
            _currentRoll = _dynamics.CalculateSuspensionRoll(lateralAccel, _currentRoll, dt);

            // Synchronize occupant player Rigidbody in PhysX to strictly match driver seat
            if (_isDriverOccupied && _occupantPlayer != null)
            {
                Transform dockTarget = driverSeatAnchor != null ? driverSeatAnchor : transform;
                if (_occupantPlayerRb != null)
                {
                    _occupantPlayerRb.MovePosition(dockTarget.position);
                    _occupantPlayerRb.MoveRotation(dockTarget.rotation);
                }
            }
        }
        #endregion

        #region Visual & Audio Update (Update)
        private void Update()
        {
            var cutscene = ServiceLocator.Resolve<ICutsceneService>();
            bool isCutscenePlaying = cutscene != null && cutscene.IsCutscenePlaying;

            if (_doorDebounceTimer > 0f)
            {
                _doorDebounceTimer -= Time.deltaTime;
            }

            // Direct Entry Fallback: If player is near the truck and presses E (blocked during cutscenes)
            if (!isCutscenePlaying && !_isDriverOccupied && _doorDebounceTimer <= 0f && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null && CanInteract(player))
                {
                    EnterVehicle(player);
                    return;
                }
            }

            // Enter / Exit Trigger on E (blocked during cutscenes)
            if (!isCutscenePlaying && _isDriverOccupied && _doorDebounceTimer <= 0f && Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    ExitVehicle();
                    return;
                }

                // Horn Trigger on H
                if (Keyboard.current.hKey.wasPressedThisFrame)
                {
                    PlayHorn();
                }

                // Headlight toggle on L or F
                if ((Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame) && _truckLighting != null)
                {
                    _truckLighting.ToggleHeadlights();
                }
            }

            float dt = Time.deltaTime;

            // 1. Steering Wheel Rotation (Front Wheels pivot around local Y)
            if (wheelFrontLeft != null)
            {
                wheelFrontLeft.localRotation = Quaternion.Euler(0f, _currentSteerAngle, 0f);
            }
            if (wheelFrontRight != null)
            {
                wheelFrontRight.localRotation = Quaternion.Euler(0f, _currentSteerAngle, 0f);
            }

            // 2. Wheel Spin Rotation (All 4 wheels roll around pitch X-axis)
            _wheelSpinAngle = _dynamics.CalculateWheelSpinAngle(_currentSpeedMps, wheelRadius, _wheelSpinAngle, dt);
            RotateWheelMesh(ref _rollPivotFL, wheelFrontLeft);
            RotateWheelMesh(ref _rollPivotFR, wheelFrontRight);
            RotateWheelMesh(ref _rollPivotRL, wheelRearLeft);
            RotateWheelMesh(ref _rollPivotRR, wheelRearRight);

            // 3. Body Suspension Tilt (Micro-animations: pitch on accel/brake, roll on turns)
            if (visualBodyRoot != null)
            {
                visualBodyRoot.localRotation = Quaternion.Euler(_currentPitch, 0f, _currentRoll);
            }

            // 4. Steering Wheel Dynamic Cockpit Spin
            if (steeringWheelTransform != null)
            {
                steeringWheelTransform.localRotation = Quaternion.Euler(32f, 0f, -_currentSteerAngle * 2.5f);
            }

            // 5. Engine Audio Modulation
            if (engineAudio != null && _isDriverOccupied)
            {
                float speedRatio = Mathf.Clamp01(Mathf.Abs(_currentSpeedMps) / _dynamics.MaxForwardSpeed);
                engineAudio.pitch = Mathf.Lerp(0.85f, 2.1f, speedRatio);
                engineAudio.volume = Mathf.Lerp(0.6f, 1.0f, speedRatio);
            }

            // 5. Drift Particles
            bool isDrifting = _isDriverOccupied && Keyboard.current != null && Keyboard.current.spaceKey.isPressed && Mathf.Abs(_currentSpeedMps) > 2.5f;
            if (driftParticles != null)
            {
                foreach (var ps in driftParticles)
                {
                    if (ps == null) continue;
                    if (isDrifting && !ps.isPlaying) ps.Play();
                    else if (!isDrifting && ps.isPlaying) ps.Stop();
                }
            }

            // 6. Update HUD Telemetry
            if (_isDriverOccupied && VehicleDrivingHUD.Instance != null)
            {
                float throttle = 0f;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed) throttle = 1f;
                    else if (Keyboard.current.sKey.isPressed) throttle = -1f;
                }
                var gear = _dynamics.DetermineGear(_currentSpeedMps, throttle);
                VehicleDrivingHUD.Instance.UpdateTelemetry(_currentSpeedMps, gear);
            }
        }

        private void LateUpdate()
        {
            // Strictly pin occupant player transform to driver seat anchor after all physics & animations
            if (_isDriverOccupied && _occupantPlayer != null)
            {
                Transform dockTarget = driverSeatAnchor != null ? driverSeatAnchor : transform;
                _occupantPlayer.transform.position = dockTarget.position;
                _occupantPlayer.transform.rotation = dockTarget.rotation;
            }
        }

        private void RotateWheelMesh(ref Transform rollPivot, Transform wheelHub)
        {
            if (rollPivot == null && wheelHub != null)
            {
                rollPivot = EnsureWheelRollPivot(wheelHub);
            }
            if (rollPivot != null)
            {
                rollPivot.localRotation = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);
            }
        }
        #endregion

        #region Helpers & Ground Detection
        private bool CheckGroundContact()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            return Physics.Raycast(origin, -Vector3.up, groundRayDistance + 0.5f, groundLayer, QueryTriggerInteraction.Ignore);
        }

        public void EnsureSubAssemblies(bool allowCreate = true)
        {
            if (visualBodyRoot == null)
            {
                visualBodyRoot = transform.Find("Cabin_And_Greenhouse") ?? transform;
            }

            var wheelsGroup = transform.Find("Wheels_Group");
            if (wheelsGroup != null)
            {
                if (wheelFrontLeft == null) wheelFrontLeft = wheelsGroup.Find("Wheel_Front_Left");
                if (wheelFrontRight == null) wheelFrontRight = wheelsGroup.Find("Wheel_Front_Right");
                if (wheelRearLeft == null) wheelRearLeft = wheelsGroup.Find("Wheel_Rear_Left");
                if (wheelRearRight == null) wheelRearRight = wheelsGroup.Find("Wheel_Rear_Right");
            }

            _rollPivotFL = EnsureWheelRollPivot(wheelFrontLeft, allowCreate);
            _rollPivotFR = EnsureWheelRollPivot(wheelFrontRight, allowCreate);
            _rollPivotRL = EnsureWheelRollPivot(wheelRearLeft, allowCreate);
            _rollPivotRR = EnsureWheelRollPivot(wheelRearRight, allowCreate);

            if (wheelFrontLeft != null && wheelRearLeft != null && _dynamics != null)
            {
                float wb = Mathf.Abs(wheelFrontLeft.localPosition.z - wheelRearLeft.localPosition.z);
                if (wb > 0.5f) _dynamics.Wheelbase = wb;
            }

            SetupHornAudio(allowCreate);

            if (steeringWheelTransform == null)
            {
                steeringWheelTransform = transform.Find("Interior_Cockpit/SteeringWheel_Assembly");
                if (steeringWheelTransform == null)
                {
                    steeringWheelTransform = transform.Find("Interior_Cockpit/SteeringWheel_Rim");
                }
            }

            if (driverSeatAnchor == null)
            {
                var seatGo = transform.Find("DriverSeatAnchor");
                if (seatGo != null) driverSeatAnchor = seatGo;
                else
                {
                    var newSeat = new GameObject("DriverSeatAnchor");
                    newSeat.transform.SetParent(transform, false);
                    driverSeatAnchor = newSeat.transform;
                }
            }

            if (driverSeatAnchor != null)
            {
                driverSeatAnchor.localPosition = new Vector3(-0.45f, 0.10f, -0.12f);
                driverSeatAnchor.localRotation = Quaternion.identity;
            }

            if (safeDismountAnchor == null)
            {
                safeDismountAnchor = transform.Find("DismountPoint");
                if (safeDismountAnchor == null && allowCreate)
                {
                    var dGo = new GameObject("DismountPoint");
                    dGo.transform.SetParent(transform, false);
                    dGo.transform.localPosition = new Vector3(-2.6f, 0.1f, 0.2f);
                    safeDismountAnchor = dGo.transform;
                }
            }

            // Ensure dedicated InteractionTrigger volume exists
            var triggerTransform = transform.Find("InteractionTrigger");
            BoxCollider box = null;
            if (triggerTransform == null)
            {
                if (allowCreate)
                {
                    var triggerGo = new GameObject("InteractionTrigger");
                    triggerGo.transform.SetParent(transform, false);
                    triggerGo.transform.localPosition = new Vector3(-0.4f, 0.75f, 0.2f);
                    box = triggerGo.AddComponent<BoxCollider>();
                }
            }
            else
            {
                box = triggerTransform.GetComponent<BoxCollider>();
            }
            if (box != null)
            {
                box.isTrigger = true;
                box.size = new Vector3(6.5f, 3.0f, 7.0f);
            }
        }

        private Transform EnsureWheelRollPivot(Transform wheelHub, bool allowCreate = true)
        {
            if (wheelHub == null) return null;
            var pivot = wheelHub.Find("Roll_Pivot");
            if (pivot == null && allowCreate)
            {
                var go = new GameObject("Roll_Pivot");
                go.transform.SetParent(wheelHub, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                var children = new System.Collections.Generic.List<Transform>();
                for (int i = 0; i < wheelHub.childCount; i++)
                {
                    var child = wheelHub.GetChild(i);
                    if (child != go.transform) children.Add(child);
                }
                foreach (var child in children)
                {
                    child.SetParent(go.transform, true);
                }
                pivot = go.transform;
            }
            return pivot;
        }

        private void SetupEngineAudio()
        {
            if (engineAudio == null)
            {
                engineAudio = GetComponent<AudioSource>();
                if (engineAudio == null) engineAudio = gameObject.AddComponent<AudioSource>();
                engineAudio.loop = true;
                engineAudio.playOnAwake = false;
                engineAudio.spatialBlend = 0.8f;
                engineAudio.minDistance = 2f;
                engineAudio.maxDistance = 35f;
            }
        }

        public void PlayHorn()
        {
            if (hornAudio == null || hornAudio.clip == null)
            {
                SetupHornAudio(allowCreate: true);
            }
            if (hornAudio != null && hornAudio.clip != null)
            {
                hornAudio.PlayOneShot(hornAudio.clip);
            }
        }

        private void SetupHornAudio(bool allowCreate = true)
        {
            if (hornAudio == null)
            {
                var hornGo = transform.Find("HornAudio");
                if (hornGo == null)
                {
                    if (allowCreate)
                    {
                        var go = new GameObject("HornAudio");
                        go.transform.SetParent(transform, false);
                        go.transform.localPosition = new Vector3(0f, 0.8f, 2.0f);
                        hornAudio = go.AddComponent<AudioSource>();
                    }
                }
                else
                {
                    hornAudio = hornGo.GetComponent<AudioSource>();
                    if (hornAudio == null && allowCreate)
                    {
                        hornAudio = hornGo.gameObject.AddComponent<AudioSource>();
                    }
                }

                if (hornAudio != null)
                {
                    hornAudio.loop = false;
                    hornAudio.playOnAwake = false;
                    hornAudio.spatialBlend = 0.8f;
                    hornAudio.minDistance = 3f;
                    hornAudio.maxDistance = 45f;
                    hornAudio.volume = 0.95f;
                }
            }

            if (hornAudio != null && hornAudio.clip == null)
            {
                hornAudio.clip = Resources.Load<AudioClip>("Audio/SFX_Truck_Horn");
            }
        }

        public void SetupWheelReferences(Transform fl, Transform fr, Transform rl, Transform rr, Transform bodyRoot)
        {
            wheelFrontLeft = fl;
            wheelFrontRight = fr;
            wheelRearLeft = rl;
            wheelRearRight = rr;
            visualBodyRoot = bodyRoot;
        }

        private void SetNonCharacterAccessoriesActive(GameObject player, bool active)
        {
            if (player == null) return;

            var indicator = player.transform.Find("AttackRangeIndicator");
            if (indicator != null) indicator.gameObject.SetActive(active);

            var dust = player.transform.Find("NightAtmosphere_Dust");
            if (dust != null) dust.gameObject.SetActive(active);

            var lantern = player.transform.Find("PlayerLantern");
            if (lantern != null) lantern.gameObject.SetActive(active);

            var trails = player.GetComponentsInChildren<TrailRenderer>(true);
            foreach (var tr in trails) tr.enabled = active;

            // Handle held weapon model in HandSocket
            var handSocket = player.transform.Find("character/Armature./mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/HandSocket");
            if (handSocket != null)
            {
                for (int i = 0; i < handSocket.childCount; i++)
                {
                    var child = handSocket.GetChild(i);
                    if (child.name != "WeaponSlashTrail")
                    {
                        child.gameObject.SetActive(active);
                    }
                }
            }
        }
        #endregion
    }
}
