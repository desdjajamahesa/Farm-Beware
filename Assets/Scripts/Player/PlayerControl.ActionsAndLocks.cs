using System.Collections;
using FeaturesInteraction;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmBeware.Core.Runtime;

public partial class PlayerControl
{
    /// <summary>
    /// Membatalkan aksi menanam atau menyiram secara seketika saat pemain menekan tombol gerak.
    /// </summary>
    public void CancelFarmAction()
    {
        if (farmActionRoutine != null)
        {
            StopCoroutine(farmActionRoutine);
            farmActionRoutine = null;
        }
        isPlanting = false;
        if (animator != null)
        {
            animator.ResetTrigger("Watering");
            animator.ResetTrigger("PlantSeed");
            animator.SetBool("Idle", false);
        }
    }

    /// <summary>
    /// Memanggil animasi menanam secara kontekstual (mis. saat berinteraksi dengan FarmlandTile).
    /// </summary>
    public void TriggerPlantAnimation()
    {
        if (isPlanting || isAttacking) return;
        if (farmActionRoutine != null) StopCoroutine(farmActionRoutine);
        farmActionRoutine = StartCoroutine(RoutinePlantSeed());
    }

    /// <summary>
    /// Memanggil animasi memanen/mengambil tanaman dari tanah.
    /// </summary>
    public void TriggerHarvestAnimation()
    {
        if (isPlanting || isAttacking) return;
        if (farmActionRoutine != null) StopCoroutine(farmActionRoutine);
        farmActionRoutine = StartCoroutine(RoutinePlantSeed());
    }

    private IEnumerator RoutinePlantSeed()
    {
        isPlanting = true;
        inputVector = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetTrigger("PlantSeed");
            animator.SetFloat("Vel", 0f);
            animator.SetBool("Idle", true);
            animator.SetBool("Sprinting", false);
        }

        // Tunggu frame berikutnya agar transisi animator ke PlantSeed dimulai
        yield return null;

        float timer = 0f;
        while (timer < plantDuration)
        {
            timer += Time.deltaTime;

            // Early cancel jika pemain mulai bergerak
            if (timer > 0.15f && inputActions != null)
            {
                Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
                if (moveInput.sqrMagnitude > 0.04f)
                {
                    break;
                }
            }

            // Jika animasi sudah selesai dan bertransisi kembali ke Idle/Moving setelah minimal 0.25 detik (kecepatan 2x)
            if (timer > 0.25f && animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName("PlantSeed") && !animator.GetNextAnimatorStateInfo(0).IsName("PlantSeed"))
                {
                    break;
                }
            }

            yield return null;
        }

        if (animator != null)
        {
            animator.ResetTrigger("PlantSeed");
        }
        isPlanting = false;
        farmActionRoutine = null;
    }

    /// <summary>
    /// Memanggil animasi menyiram tanaman secara kontekstual (mis. saat menyiram FarmlandTile).
    /// </summary>
    public void TriggerWaterAnimation()
    {
        if (isPlanting || isAttacking) return;
        if (FeaturesFarming.PlantWaterer.Instance != null && FeaturesFarming.PlantWaterer.Instance.CurrentWater < 10f)
        {
            return;
        }
        if (farmActionRoutine != null) StopCoroutine(farmActionRoutine);
        farmActionRoutine = StartCoroutine(RoutineWaterCrop());
    }

    private IEnumerator RoutineWaterCrop()
    {
        isPlanting = true;
        inputVector = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetTrigger("Watering");
            animator.SetFloat("Vel", 0f);
            animator.SetBool("Idle", true);
            animator.SetBool("Sprinting", false);
        }

        yield return null;

        float timer = 0f;
        while (timer < 0.7f)
        {
            timer += Time.deltaTime;

            // Early cancel jika pemain mulai bergerak
            if (timer > 0.12f && inputActions != null)
            {
                Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
                if (moveInput.sqrMagnitude > 0.04f)
                {
                    break;
                }
            }

            if (timer > 0.2f && animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName("Watering") && !animator.GetNextAnimatorStateInfo(0).IsName("Watering"))
                {
                    break;
                }
            }

            yield return null;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Watering");
        }
        isPlanting = false;
        farmActionRoutine = null;
    }

    // Tombol Tab / I membuka-menutup panel pemain. Jika storage terbuka, tutup semua.
    private void HandleInventoryInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Guard: jangan buka inventory jika sedang Trophy Mode
        if (TrophySystemManager.Instance != null && TrophySystemManager.Instance.IsInTrophyMode)
            return;

        if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
        {
            if (Time.frameCount == FarmBeware.Core.Runtime.UIModalHelper.LastFrameUIPanelClosed)
                return;

            if (InventoryManagerUI.Instance != null)
            {
                if (InventoryManagerUI.Instance.IsAnyInventoryUIRelatedOpen())
                    InventoryManagerUI.Instance.CloseAllUI();
                else
                    InventoryManagerUI.Instance.TogglePlayerInventory();
            }
        }
    }

    // Seleksi Hotbar: angka 1-4 + scroll mouse (wrapping).
    private void HandleHotbarInput()
    {
        if (playerInventory == null) return;

        var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
        bool isNight = (timeService != null && timeService.IsNight) ||
                       (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                       (FeaturesTime.DayNightTimeManager.Instance != null && FeaturesTime.DayNightTimeManager.Instance.IsNight);

        if (isNight)
        {
            // At night, held item is locked to active equipped combat weapon (Root Cleaver / Slingshot).
            // Consumables can still be used via double-click directly in hotbar or inventory.
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) playerInventory.SelectHotbarSlot(3);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f)
            {
                int idx = playerInventory.selectedHotbarIndex - 1;
                if (idx < 0) idx = 3;
                playerInventory.SelectHotbarSlot(idx);
            }
            else if (scroll < 0f)
            {
                int idx = playerInventory.selectedHotbarIndex + 1;
                if (idx > 3) idx = 0;
                playerInventory.SelectHotbarSlot(idx);
            }
        }
    }

    private void ExecuteJump()
    {
        if (isInputLocked || isPlanting || isAttacking) return;

        // Hanya bisa lompat jika menginjak tanah
        if (isGrounded)
        {
            isGrounded = false;
            isJumping = true;
            groundBufferTimer = 0f;
            if (animator != null)
            {
                animator.SetBool("Grounded", false);
                animator.Play("AirBorn", 0, 0f);
            }

            // Reset kecepatan Y agar lompatan konsisten, lalu dorong ke atas
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Translates raw 2D input (X = horizontal, Z = vertical) into isometric camera-relative world direction.
    /// Preserves analog stick magnitude instead of blanket normalization.
    /// </summary>
    public static Vector3 GetCameraRelativeDirection(Vector3 rawInput)
    {
        float inputMag = Mathf.Clamp01(rawInput.magnitude);
        if (inputMag < 0.001f) return Vector3.zero;

        Vector3 rawDir = rawInput / inputMag;

        if (Camera.main != null)
        {
            Vector3 camFwd = Camera.main.transform.forward;
            camFwd.y = 0f;
            camFwd.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 worldDir = camFwd * rawDir.z + camRight * rawDir.x;
            if (worldDir.sqrMagnitude > 0.001f)
            {
                return worldDir.normalized * inputMag;
            }
            return Vector3.zero;
        }

        // Standard 45-degree isometric projection fallback
        return (Quaternion.Euler(0f, 45f, 0f) * rawDir).normalized * inputMag;
    }

    private void HandleDefensiveInput()
    {
        if (isInputLocked || isPlanting || isDodging) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // 1. Dodge Roll: Left Alt, C key
        bool dodgeTriggered = kb.leftAltKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame;
        if (dodgeTriggered)
        {
            PerformDodgeRoll();
            return;
        }

        // 2. Precision Deflect / Parry: V key or Left Ctrl key
        bool parryTriggered = kb.vKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame;
        if (parryTriggered)
        {
            PerformParry();
            return;
        }
    }

    public void PerformDodgeRoll()
    {
        if (isDodging || isPlanting || isInputLocked) return;

        // Allow dodge-canceling if attacking and reached cancelable window (normalizedTime >= 0.25f)
        if (isAttacking)
        {
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.normalizedTime >= 0.25f)
                {
                    CancelActiveCombatAction();
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (playerStats != null && !playerStats.UseStamina(dodgeStaminaCost))
        {
            return;
        }

        // Read live movement input
        Vector2 move = inputActions != null ? inputActions.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        Vector3 rawMove = move.sqrMagnitude > 0.01f ? new Vector3(move.x, 0f, move.y).normalized : inputVector;

        // Resolve true camera-relative direction in world space
        Vector3 rollDir;
        if (rawMove.sqrMagnitude > 0.01f)
        {
            rollDir = GetCameraRelativeDirection(rawMove);
        }
        else
        {
            rollDir = transform.forward;
        }

        rollDir.y = 0f;
        if (rollDir.sqrMagnitude < 0.001f)
        {
            rollDir = transform.forward;
        }
        else
        {
            rollDir.Normalize();
        }

        if (dodgeCoroutine != null)
        {
            StopCoroutine(dodgeCoroutine);
            dodgeCoroutine = null;
        }
        dodgeCoroutine = StartCoroutine(RoutineDodgeRoll(rollDir));
    }

    private IEnumerator RoutineDodgeRoll(Vector3 rollDir)
    {
        isDodging = true;
        dodgeRollDirection = rollDir;

        // Instantly align character rotation with roll direction
        Quaternion targetRot = Quaternion.LookRotation(rollDir, Vector3.up);
        transform.rotation = targetRot;
        if (rb != null)
        {
            rb.rotation = targetRot;
            rb.linearVelocity = new Vector3(rollDir.x * dodgeSpeed, rb.linearVelocity.y, rollDir.z * dodgeSpeed);
        }

        // Trigger i-Frames on DefenseEvaluator
        var mods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        if (playerStats != null)
        {
            playerStats.DefenseEvaluator.DodgeIFrameDuration = dodgeIFrameDuration;
            playerStats.DefenseEvaluator.TriggerDodge(mods.ExtraDodgeDuration);
        }

        if (animator != null)
        {
            animator.SetBool("Sliding", true);
        }

        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            animator.SetBool("Sliding", false);
        }

        isDodging = false;
        dodgeCoroutine = null;
    }

    public void PerformParry()
    {
        if (isDodging || isPlanting || isInputLocked) return;

        // Allow parry-canceling if attacking and reached cancelable window
        if (isAttacking)
        {
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.normalizedTime >= 0.20f)
                {
                    CancelActiveCombatAction();
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        // Reject parry if already parrying or currently in whiff lockout
        if (playerStats != null && !playerStats.DefenseEvaluator.CanInitiateParry)
        {
            return;
        }

        float effectiveStaminaCost = parryStaminaCost;
        if (playerStats != null && playerStats.currentStamina < 20f)
        {
            // Low stamina penalty: costs 1.5x to prevent low-stamina parry cheese
            effectiveStaminaCost *= 1.5f;
        }

        if (playerStats != null && !playerStats.UseStamina(effectiveStaminaCost))
        {
            return;
        }

        var mods = playerStats != null ? playerStats.GetCombatStatModifiers() : FeaturesCombat.Core.PureLogic.CombatStatModifiers.Default;
        if (playerStats != null)
        {
            playerStats.DefenseEvaluator.ParryWindow = parryWindow;
            playerStats.DefenseEvaluator.TriggerParry(mods.ExtraParryWindow);
        }

        if (animator != null)
        {
            animator.ResetTrigger("Parry");
            animator.SetTrigger("Parry");
        }

        if (PlayerUI.FloatingCombatTextManager.Instance != null)
        {
            PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                transform.position + Vector3.up * 1.5f,
                "GUARD",
                new Color(0.8f, 0.8f, 1f));
        }
    }

    private int lastInteractFrame = -1;

    /// <summary>
    /// Menghentikan gerakan pemain secara paksa.
    /// Dipanggil saat memulai interaksi agar pemain tidak sliding/bergerak.
    /// </summary>
    public void StopMovement()
    {
        inputVector = Vector3.zero;
        isRunning = false;
        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (animator != null && !animator.GetBool("Driving"))
        {
            animator.SetFloat("Vel", 0f);
            animator.SetBool("Idle", true);
            animator.SetBool("Sprinting", false);
        }
    }

    public void TriggerInteract()
    {
        if (Time.frameCount == lastInteractFrame || isPlanting || isAttacking) return;
        lastInteractFrame = Time.frameCount;

        if (isInputLocked)
        {
            Debug.LogWarning("[PlayerControl] Tombol E ditekan tetapi isInputLocked = true!");
            return;
        }

        // Pastikan skrip interactor tidak hilang/error
        if (interactor == null)
            interactor = GetComponent<PlayerInteractor>();

        if (interactor != null)
        {
            // Hentikan gerakan pemain sebelum memulai interaksi
            StopMovement();
            // Perintahkan "Tangan" untuk menjalankan logikanya
            interactor.OnInteractInput();
        }
        else
        {
            Debug.LogError("[PlayerControl] PlayerInteractor tidak ditemukan pada Player!");
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        TriggerInteract();
    }


    private void CheckGrounded()
    {
        // Jika sedang fase awal lompat (bergerak ke atas karena dorongan lompat), abaikan ground check
        if (isJumping)
        {
            if (rb != null && rb.linearVelocity.y <= 0f)
            {
                // Sudah mencapai puncak loncatan dan mulai jatuh
                isJumping = false;
            }
            else
            {
                isGrounded = false;
                groundBufferTimer = 0f;
                return;
            }
        }

        // SphereCast ke bawah untuk mendeteksi tanah secara akurat di tanjakan/tangga
        // Mulai sedikit di atas kaki karakter (y = 0.25f), radius 0.2f, jarak cast 0.25f
        float sphereRadius = 0.2f;
        Vector3 origin = transform.position + Vector3.up * (sphereRadius + 0.05f);
        float castDistance = 0.25f;

        bool hitGround = Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            ~LayerMask.GetMask("Ignore Raycast"),
            QueryTriggerInteraction.Ignore
        );

        // Abaikan jika collider yang terkena adalah collider diri sendiri
        if (hitGround && playerCollider != null && hit.collider == playerCollider)
        {
            hitGround = false;
        }

        if (hitGround)
        {
            isGrounded = true;
            groundBufferTimer = GroundBufferDuration;
        }
        else
        {
            if (groundBufferTimer > 0f)
            {
                groundBufferTimer -= Time.deltaTime;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
    }

    #region Boss Attack Impact Helpers (Stun & Control Lock)

    private Coroutine stunCoroutine;

    /// <summary>
    /// Memberikan efek stun sementara pada pemain (misal: akibat bantingan grapple bos).
    /// Dilengkapi safety timeout coroutine agar pemain tidak pernah terkunci selamanya.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }
        stunCoroutine = StartCoroutine(RoutineStun(duration));
    }

    private System.Collections.IEnumerator RoutineStun(float duration)
    {
        AddInputLock("Stun");

        if (PlayerUI.FloatingCombatTextManager.Instance != null)
        {
            PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                transform.position + Vector3.up * 1.8f,
                "⚡ STUNNED!",
                new Color(1f, 0.85f, 0.2f));
        }

        yield return new WaitForSeconds(duration);

        RemoveInputLock("Stun");
        stunCoroutine = null;
    }

    /// <summary>
    /// Menerapkan dorongan knockback fisika pada pemain.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (rb != null)
        {
            direction.y = 0.2f;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Mengunci / membuka input kontrol pemain secara eksplisit (misal: saat diangkat bos).
    /// </summary>
    public void SetControlLock(bool locked)
    {
        if (locked)
        {
            if (!_inputLockSources.Contains("LegacyGeneric"))
                _inputLockSources.Add("LegacyGeneric");
            ApplyLockState(true);
        }
        else
        {
            _inputLockSources.Remove("LegacyGeneric");
            ApplyLockState(_inputLockSources.Count > 0);
            if (_inputLockSources.Count == 0 && stunCoroutine != null)
            {
                StopCoroutine(stunCoroutine);
                stunCoroutine = null;
            }
        }
    }

    public void AddInputLock(string source)
    {
        if (string.IsNullOrEmpty(source)) source = "Generic";
        _inputLockSources.Add(source);
        ApplyLockState(true);
    }

    public void RemoveInputLock(string source)
    {
        if (string.IsNullOrEmpty(source)) source = "Generic";
        _inputLockSources.Remove(source);
        ApplyLockState(_inputLockSources.Count > 0);
    }

    public void ClearAllInputLocks()
    {
        _inputLockSources.Clear();
        ApplyLockState(false);
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
    }

    public bool HasInputLock(string source)
    {
        if (string.IsNullOrEmpty(source)) return _inputLockSources.Count > 0;
        return _inputLockSources.Contains(source);
    }

    public bool HasLockOtherThan(string excludedSource)
    {
        foreach (var src in _inputLockSources)
        {
            if (src != excludedSource) return true;
        }
        return false;
    }

    private void ApplyLockState(bool locked)
    {
        _isInputLocked = locked;
        if (locked)
        {
            StopMovement();
            CancelActiveCombatAction();
            if (dodgeCoroutine != null)
            {
                StopCoroutine(dodgeCoroutine);
                dodgeCoroutine = null;
            }
            isDodging = false;
            CheckGrounded();
            if (animator != null && !animator.GetBool("Driving"))
            {
                animator.SetBool("Grounded", isGrounded);
                animator.SetFloat("Vel", 0f);
                animator.SetBool("Idle", true);
                animator.SetBool("Sprinting", false);
                animator.SetBool("Sliding", false);
                animator.SetBool("IsAttacking", false);
            }
        }
    }

    /// <summary>
    /// Helper eksplisit untuk mengaktifkan / menonaktifkan input kontrol pemain.
    /// </summary>
    public void SetInputActive(bool active) => SetControlLock(!active);

    #endregion

    /// <summary>
    /// Teleports the player to a target position and rotation, resetting physics velocity and syncing transforms.
    /// Used by the Save/Load system and scene transitions.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        StopMovement();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.position = position;
            rb.rotation = rotation;
        }
        transform.position = position;
        transform.rotation = rotation;
        Physics.SyncTransforms();
    }

    int IPlayerContext.Health => playerStats != null ? playerStats.currentHealth : 100;
    float IPlayerContext.Stamina => playerStats != null ? playerStats.currentStamina : 100f;
    float IPlayerContext.Hunger => playerStats != null ? playerStats.currentHunger : 100f;
    float IPlayerContext.Thirst => playerStats != null ? playerStats.currentThirst : 100f;

    void IPlayerContext.RestoreStats(int health, float stamina, float hunger, float thirst)
    {
        if (playerStats != null)
        {
            playerStats.RestoreStats(health, stamina, hunger, thirst);
        }
    }

    void IPlayerContext.UpdateEquipmentVisual(int slotIndex)
    {
        var equip = GetComponent<PlayerEquipment>();
        if (equip != null)
        {
            equip.UpdateEquipmentVisual(slotIndex);
        }
    }
}
