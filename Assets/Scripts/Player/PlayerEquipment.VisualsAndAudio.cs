using UnityEngine;

public partial class PlayerEquipment
{
    private void Awake()
    {
        inventory = GetComponent<InventoryComponent>();
        animator = GetComponentInChildren<Animator>();
        playerStats = GetComponent<PlayerStats>();
        buffManager = GetComponent<PlayerBuffManager>();
        FindHandSocketIfNeeded();
        EnsureBareHandsTrail();
        DestroyCurrentWeapon();
        EnsureRangeIndicator();
        if (combatStateMachine != null)
        {
            combatStateMachine.ComboResetWindow = comboResetWindow;
        }
    }

    private ItemData lastEquippedItem;

    private void Start()
    {
        var armorState = GetComponent<FeaturesWorkbench.PlayerArmorUpgradeState>() ?? FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
        if (armorState != null)
        {
            armorState.OnEquipmentChanged -= HandleArmorEquipmentChanged;
            armorState.OnEquipmentChanged += HandleArmorEquipmentChanged;
        }

        // Refresh visuals on game start
        lastEquippedItem = CurrentEquippedItem;
        RefreshEquipmentForStance();
        UpdateRangeIndicatorState();
    }

    private void Update()
    {
        combatStateMachine.Update(Time.time);
        ItemData current = CurrentEquippedItem;
        if (current != lastEquippedItem)
        {
            lastEquippedItem = current;
            RefreshEquipmentForStance();
        }
        UpdateRangeIndicatorState();
    }

    private void OnEnable()
    {
        if (inventory == null)
            inventory = GetComponent<InventoryComponent>();

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshCurrentEquipment;
            inventory.OnHotbarSelected += OnHotbarSlotChanged;
        }
        var armorState = GetComponent<FeaturesWorkbench.PlayerArmorUpgradeState>() ?? FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
        if (armorState != null)
        {
            armorState.OnEquipmentChanged -= HandleArmorEquipmentChanged;
            armorState.OnEquipmentChanged += HandleArmorEquipmentChanged;
        }
        UpdateRangeIndicatorState();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshCurrentEquipment;
            inventory.OnHotbarSelected -= OnHotbarSlotChanged;
        }
        var armorState = GetComponent<FeaturesWorkbench.PlayerArmorUpgradeState>() ?? FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
        if (armorState != null)
        {
            armorState.OnEquipmentChanged -= HandleArmorEquipmentChanged;
        }
        if (rangeIndicatorObj == null)
        {
            var found = transform.Find("AttackRangeIndicator");
            if (found != null) rangeIndicatorObj = found.gameObject;
        }
        if (rangeIndicatorObj != null)
        {
            rangeIndicatorObj.SetActive(false);
        }
    }

    /// <summary>
    /// Initializes and updates the LineRenderer component for the ground range indicator circle.
    /// </summary>
    private void EnsureRangeIndicator()
    {
        if (rangeIndicatorObj == null)
        {
            Transform existing = transform.Find("AttackRangeIndicator");
            if (existing != null)
            {
                rangeIndicatorObj = existing.gameObject;
            }
            else
            {
                rangeIndicatorObj = new GameObject("AttackRangeIndicator");
                rangeIndicatorObj.transform.SetParent(transform, false);
                rangeIndicatorObj.transform.localPosition = new Vector3(0f, groundYOffset, 0f);
                rangeIndicatorObj.transform.localRotation = Quaternion.identity;
            }
        }

        if (rangeLineRenderer == null && rangeIndicatorObj != null)
        {
            rangeLineRenderer = rangeIndicatorObj.GetComponent<LineRenderer>();
            if (rangeLineRenderer == null)
            {
                rangeLineRenderer = rangeIndicatorObj.AddComponent<LineRenderer>();
            }

            rangeLineRenderer.useWorldSpace = false;
            rangeLineRenderer.loop = true;
            rangeLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rangeLineRenderer.receiveShadows = false;
            rangeLineRenderer.alignment = LineAlignment.View;

            if (indicatorMaterial == null)
            {
                Shader shader = FarmBeware.Core.Runtime.ShaderFallbackResolver.ResolveSpritesDefaultShader();
                if (shader != null)
                {
                    indicatorMaterial = new Material(shader);
                }
            }
            if (indicatorMaterial != null)
            {
                rangeLineRenderer.material = indicatorMaterial;
            }
        }

        UpdateCircleGeometry();
        UpdateIndicatorVisual(isCurrentlyPulsing ? attackPulseColor : rangeIndicatorColor);
    }

    public void UpdateCircleGeometry()
    {
        if (rangeLineRenderer == null) return;
        if (rangeIndicatorObj != null)
        {
            rangeIndicatorObj.transform.localPosition = new Vector3(0f, groundYOffset, 0f);
        }

        int segments = 48;
        rangeLineRenderer.positionCount = segments;
        rangeLineRenderer.startWidth = indicatorLineWidth;
        rangeLineRenderer.endWidth = indicatorLineWidth;

        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Sin(rad) * attackHitRange;
            float z = Mathf.Cos(rad) * attackHitRange;
            rangeLineRenderer.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    private void UpdateIndicatorVisual(Color color)
    {
        if (rangeLineRenderer != null)
        {
            rangeLineRenderer.startColor = color;
            rangeLineRenderer.endColor = color;
        }
    }

    private void UpdateRangeIndicatorState()
    {
        EnsureRangeIndicator();
        if (rangeIndicatorObj == null) return;

        bool shouldShow = false;
        switch (rangeIndicatorMode)
        {
            case RangeIndicatorVisibility.Always:
                shouldShow = true;
                break;
            case RangeIndicatorVisibility.WhenHoldingWeapon:
                shouldShow = CanPerformAttackWithCurrentItem();
                break;
            case RangeIndicatorVisibility.OnlyDuringAttack:
                shouldShow = isCurrentlyPulsing;
                break;
            case RangeIndicatorVisibility.Disabled:
                shouldShow = false;
                break;
        }

        if (rangeIndicatorObj.activeSelf != shouldShow)
        {
            rangeIndicatorObj.SetActive(shouldShow);
        }
    }

    public bool CanPerformAttackWithCurrentItem()
    {
        ItemData item = CurrentEquippedItem;
        if (item == null) return allowBareHandsAttack;
        return (item is ToolItemData tool && tool.isWeapon) || allowBareHandsAttack;
    }

    private void TriggerRangeIndicatorPulse(float duration)
    {
        if (rangeIndicatorMode == RangeIndicatorVisibility.Disabled) return;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(RoutineRangeIndicatorPulse(duration));
    }

    private System.Collections.IEnumerator RoutineRangeIndicatorPulse(float duration)
    {
        isCurrentlyPulsing = true;
        EnsureRangeIndicator();

        if (rangeIndicatorObj != null)
        {
            rangeIndicatorObj.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color currentColor = Color.Lerp(attackPulseColor, rangeIndicatorColor, t);
            UpdateIndicatorVisual(currentColor);
            yield return null;
        }

        isCurrentlyPulsing = false;
        UpdateIndicatorVisual(rangeIndicatorColor);
        UpdateRangeIndicatorState();
        pulseCoroutine = null;
    }

    private void OnValidate()
    {
        if (rangeLineRenderer != null)
        {
            UpdateCircleGeometry();
            UpdateIndicatorVisual(isCurrentlyPulsing ? attackPulseColor : rangeIndicatorColor);
        }

#if UNITY_EDITOR
        AutoAssignCombatAudio();
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Vector3 center = transform.position + Vector3.up * groundYOffset;
        int segments = 40;
        Vector3 prev = center + new Vector3(attackHitRange, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * (360f / segments));
            Vector3 next = center + new Vector3(Mathf.Cos(rad) * attackHitRange, 0f, Mathf.Sin(rad) * attackHitRange);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Draw attack facing direction (forward ray)
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
        Vector3 forward = transform.forward * attackHitRange;
        Gizmos.DrawRay(center, forward);
    }

    /// <summary>
    /// Invoked when inventory contents change (chest transfers, bag moves, item pickup).
    /// </summary>
    public void RefreshCurrentEquipment()
    {
        if (inventory == null) return;

        ItemData currentItem = CurrentEquippedItem;

        UpdateEquipmentVisual(inventory.selectedHotbarIndex);

        // Equip/hold animation disabled

        lastEquippedItem = currentItem;
        UpdateRangeIndicatorState();
    }

    /// <summary>
    /// Invoked when the player switches hotbar slots (keys 1-4 or mouse scroll).
    /// Swaps the 3D model immediately WITHOUT triggering an equip animation to avoid interrupting locomotion.
    /// At night or during combat stance, held weapon is locked functionally and visually to the active combat weapon.
    /// </summary>
    public void OnHotbarSlotChanged(int hotbarIndex)
    {
        var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
        bool isNight = (timeService != null && timeService.IsNight) ||
                       (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                       (FeaturesTime.DayNightTimeManager.Instance != null && FeaturesTime.DayNightTimeManager.Instance.IsNight);

        var stance = GetComponent<PlayerStanceManager>() ?? PlayerStanceManager.Instance;
        if (isNight || (stance != null && stance.IsInCombatStance))
        {
            SpawnCombatWeaponVisual();
            lastEquippedItem = CurrentEquippedItem;
            UpdateRangeIndicatorState();
            return;
        }

        UpdateEquipmentVisual(hotbarIndex);
        if (inventory != null)
        {
            lastEquippedItem = CurrentEquippedItem;
        }
        UpdateRangeIndicatorState();
    }

    private void HandleArmorEquipmentChanged(FarmBeware.Core.Runtime.EquipmentSlotType slot, ItemData item)
    {
        if (slot == FarmBeware.Core.Runtime.EquipmentSlotType.MainWeapon)
        {
            lastEquippedItem = CurrentEquippedItem;
            RefreshEquipmentForStance();
        }
    }

    public void RefreshEquipmentForStance()
    {
        var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
        bool isNight = (timeService != null && timeService.IsNight) ||
                       (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                       (FeaturesTime.DayNightTimeManager.Instance != null && FeaturesTime.DayNightTimeManager.Instance.IsNight);

        var stance = GetComponent<PlayerStanceManager>() ?? PlayerStanceManager.Instance;
        if (isNight || (stance != null && stance.IsInCombatStance))
        {
            SpawnCombatWeaponVisual();
        }
        else
        {
            int slotIdx = inventory != null ? inventory.selectedHotbarIndex : 0;
            UpdateEquipmentVisual(slotIdx);
        }
        UpdateRangeIndicatorState();
    }

    public void SpawnCombatWeaponVisual()
    {
        DestroyCurrentWeapon();

        // 1. Direct priority: Equipped weapon in PlayerArmorUpgradeState MainWeapon slot
        ItemData weapon = null;
        var armorState = GetComponent<FeaturesWorkbench.PlayerArmorUpgradeState>() ?? FeaturesWorkbench.PlayerArmorUpgradeState.Instance;
        if (armorState != null)
        {
            var equippedWeapon = armorState.GetEquippedItem(FarmBeware.Core.Runtime.EquipmentSlotType.MainWeapon);
            if (equippedWeapon != null && FeaturesWorkbench.PlayerWeaponUpgradeState.IsValidCombatWeapon(equippedWeapon))
            {
                weapon = equippedWeapon;
            }
        }

        // 2. Second priority: Stance manager resolution (checks inventory for Root Cleaver / Slingshot)
        if (weapon == null)
        {
            var stance = GetComponent<PlayerStanceManager>() ?? PlayerStanceManager.Instance;
            weapon = stance != null ? stance.GetActiveCombatWeapon() : null;
        }

        if (weapon == null) return;

        FindHandSocketIfNeeded();
        if (handSocket == null) return;

        if (weapon is ToolItemData rangedTool && rangedTool.isRangedWeapon)
        {
            currentWeaponModel = FeaturesCombat.Adapters.SlingshotVisualFactory.CreateSlingshot3DModel(handSocket);
            return;
        }

        GameObject prefabToSpawn = null;
        if (weapon is ToolItemData tool && tool.equipPrefab != null)
        {
            prefabToSpawn = tool.equipPrefab;
        }

        if (prefabToSpawn == null && ItemDatabase.Instance != null)
        {
            var fallbackItem = ItemDatabase.Instance.GetItem("tool_weapon_root_cleaver") as ToolItemData;
            if (fallbackItem != null && fallbackItem.equipPrefab != null)
            {
                prefabToSpawn = fallbackItem.equipPrefab;
            }
        }

        if (prefabToSpawn == null)
        {
#if UNITY_EDITOR
            prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/DummySword.prefab");
#endif
        }

        if (prefabToSpawn != null)
        {
            SpawnWeaponPrefab(prefabToSpawn);
        }
    }

    public void SpawnWeaponPrefab(GameObject prefab)
    {
        FindHandSocketIfNeeded();
        if (handSocket == null || prefab == null) return;

        GameObject spawned = Instantiate(prefab, handSocket);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        currentWeaponModel = spawned;

        Vector3 prefabScale = prefab.transform.localScale;
        Vector3 parentLossy = handSocket.lossyScale;
        Vector3 resolvedScale = new Vector3(
            Mathf.Abs(parentLossy.x) > 0.0001f ? prefabScale.x / parentLossy.x : prefabScale.x,
            Mathf.Abs(parentLossy.y) > 0.0001f ? prefabScale.y / parentLossy.y : prefabScale.y,
            Mathf.Abs(parentLossy.z) > 0.0001f ? prefabScale.z / parentLossy.z : prefabScale.z
        );
        currentWeaponModel.transform.localScale = resolvedScale;

        var hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
        if (hitbox == null)
        {
            var col = currentWeaponModel.GetComponent<Collider>() ?? currentWeaponModel.AddComponent<BoxCollider>();
            col.isTrigger = true;
            hitbox = currentWeaponModel.AddComponent<FeaturesCombat.WeaponHitbox>();
        }

        if (hitbox != null)
        {
            hitbox.ConfigureHitRange(attackHitRange);
        }

        var rb = currentWeaponModel.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentWeaponModel.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        currentWeaponTrail = currentWeaponModel.GetComponentInChildren<FeaturesCombat.Adapters.WeaponTrailController>();
        if (currentWeaponTrail == null)
        {
            currentWeaponTrail = currentWeaponModel.AddComponent<FeaturesCombat.Adapters.WeaponTrailController>();
        }
        currentWeaponTrail.InitializeForWeapon(currentWeaponModel);
    }

    public void UpdateEquipmentVisual(int hotbarIndex)
    {
        DestroyCurrentWeapon();

        var timeService = FarmBeware.Core.Runtime.ServiceLocator.Resolve<FarmBeware.Core.Runtime.ITimeService>();
        bool isNight = (timeService != null && timeService.IsNight) ||
                       (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                       (FeaturesTime.DayNightTimeManager.Instance != null && FeaturesTime.DayNightTimeManager.Instance.IsNight);

        var stance = GetComponent<PlayerStanceManager>() ?? PlayerStanceManager.Instance;
        if (isNight || (stance != null && stance.IsInCombatStance))
        {
            SpawnCombatWeaponVisual();
            return;
        }

        if (inventory == null)
            inventory = GetComponent<InventoryComponent>();

        InventorySlot slot = (inventory != null && hotbarIndex >= 0 && hotbarIndex < inventory.slots.Count)
            ? inventory.slots[hotbarIndex]
            : null;

        ItemData itemToDisplay = (slot != null && !slot.IsEmpty) ? slot.item : CurrentEquippedItem;
        if (itemToDisplay == null)
            return;

        FindHandSocketIfNeeded();
        if (handSocket == null)
            return;

        if (itemToDisplay is ToolItemData rangedHotbarTool && rangedHotbarTool.isRangedWeapon)
        {
            currentWeaponModel = FeaturesCombat.Adapters.SlingshotVisualFactory.CreateSlingshot3DModel(handSocket);
            return;
        }

        // Skip spawning a 3D model if the item is not a Tool or has no equipPrefab
        if (itemToDisplay is not ToolItemData tool || tool.equipPrefab == null)
            return;

        GameObject spawned = Instantiate(tool.equipPrefab, handSocket);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        currentWeaponModel = spawned;

        // Compensate for bone hierarchy scale (e.g. 100x rig) so visual tool size in world matches equipPrefab
        Vector3 prefabScale = tool.equipPrefab.transform.localScale;
        Vector3 parentLossy = handSocket.lossyScale;
        Vector3 resolvedScale = new Vector3(
            Mathf.Abs(parentLossy.x) > 0.0001f ? prefabScale.x / parentLossy.x : prefabScale.x,
            Mathf.Abs(parentLossy.y) > 0.0001f ? prefabScale.y / parentLossy.y : prefabScale.y,
            Mathf.Abs(parentLossy.z) > 0.0001f ? prefabScale.z / parentLossy.z : prefabScale.z
        );
        currentWeaponModel.transform.localScale = resolvedScale;

        // Ensure the spawned weapon model has WeaponHitbox, trigger collider, and kinematic Rigidbody
        var hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
        if (hitbox == null)
        {
            var col = currentWeaponModel.GetComponent<Collider>() ?? currentWeaponModel.AddComponent<BoxCollider>();
            col.isTrigger = true;
            hitbox = currentWeaponModel.AddComponent<FeaturesCombat.WeaponHitbox>();
        }

        if (hitbox != null)
        {
            hitbox.ConfigureHitRange(attackHitRange);
        }

        var rb = currentWeaponModel.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentWeaponModel.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        currentWeaponTrail = currentWeaponModel.GetComponentInChildren<FeaturesCombat.Adapters.WeaponTrailController>();
        if (currentWeaponTrail == null)
        {
            currentWeaponTrail = currentWeaponModel.AddComponent<FeaturesCombat.Adapters.WeaponTrailController>();
        }
        currentWeaponTrail.InitializeForWeapon(currentWeaponModel);
    }

    private void FindHandSocketIfNeeded()
    {
        if (handSocket == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("HandSocket", System.StringComparison.OrdinalIgnoreCase))
                {
                    handSocket = t;
                    break;
                }
            }
            if (handSocket == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("hand_r", System.StringComparison.OrdinalIgnoreCase) ||
                        t.name.EndsWith("RightHand", System.StringComparison.OrdinalIgnoreCase))
                    {
                        handSocket = t;
                        break;
                    }
                }
            }
        }

        // Auto-normalize if HandSocket position was authored in meters on a 100x bone rig
        if (handSocket != null && handSocket.parent != null && handSocket.parent.lossyScale.x > 10f)
        {
            if (Mathf.Abs(handSocket.localPosition.y) > 0.01f)
            {
                handSocket.localPosition = new Vector3(
                    handSocket.localPosition.x * 0.01f,
                    handSocket.localPosition.y * 0.01f,
                    handSocket.localPosition.z * 0.01f
                );
            }
        }
    }

    private void EnsureBareHandsTrail()
    {
        FindHandSocketIfNeeded();
        if (handSocket != null && bareHandsTrail == null)
        {
            bareHandsTrail = handSocket.GetComponent<FeaturesCombat.Adapters.WeaponTrailController>();
            if (bareHandsTrail == null)
            {
                bareHandsTrail = handSocket.gameObject.AddComponent<FeaturesCombat.Adapters.WeaponTrailController>();
            }
            bareHandsTrail.InitializeForWeapon(handSocket.gameObject);
        }
    }

    private FeaturesCombat.Adapters.WeaponTrailController GetActiveTrail()
    {
        if (currentWeaponTrail != null) return currentWeaponTrail;
        EnsureBareHandsTrail();
        return bareHandsTrail;
    }

    public void DestroyCurrentWeapon()
    {
        if (currentWeaponModel != null)
        {
            var hitbox = currentWeaponModel.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
            if (hitbox != null)
            {
                hitbox.Deactivate();
            }

            if (Application.isPlaying)
            {
                Destroy(currentWeaponModel);
            }
            else
            {
                DestroyImmediate(currentWeaponModel);
            }
            currentWeaponModel = null;
        }

        if (currentWeaponTrail != null)
        {
            currentWeaponTrail.EndTrail();
            currentWeaponTrail = null;
        }

        // Safeguard: ensure ALL children under handSocket are cleared so no stray weapon models
        // (such as scene-saved prefabs) remain or stack as duplicates.
        FindHandSocketIfNeeded();
        if (handSocket != null)
        {
            for (int i = handSocket.childCount - 1; i >= 0; i--)
            {
                var child = handSocket.GetChild(i).gameObject;
                var hitbox = child.GetComponentInChildren<FeaturesCombat.WeaponHitbox>();
                if (hitbox != null)
                {
                    hitbox.Deactivate();
                }

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }

    #region Combat Audio Management

    private void EnsureWeaponAudioSource()
    {
        if (weaponAudioSource == null)
        {
            weaponAudioSource = GetComponent<AudioSource>();
            if (weaponAudioSource == null)
            {
                weaponAudioSource = gameObject.AddComponent<AudioSource>();
            }
            weaponAudioSource.playOnAwake = false;
            weaponAudioSource.spatialBlend = 0.35f;
        }

#if UNITY_EDITOR
        AutoAssignCombatAudio();
#endif
    }

    private void PlaySwingAudio(AudioClip clip)
    {
        if (clip == null) return;
        EnsureWeaponAudioSource();
        if (weaponAudioSource != null)
        {
            weaponAudioSource.pitch = Random.Range(0.94f, 1.06f);
            weaponAudioSource.PlayOneShot(clip, swingAudioVolume);
        }
    }

    private void PlayAttackSwingSound(bool is360, bool isFinisher, bool isHeavy, bool isDash)
    {
        if (!IsHoldingWeapon)
        {
            PlaySwingAudio(swingPunchClip);
            return;
        }

        if (isDash)
        {
            PlaySwingAudio(swingDashClip != null ? swingDashClip : swingLightClip);
        }
        else if (isHeavy)
        {
            PlaySwingAudio(swingHeavyClip);
        }
        else if (isFinisher || is360 || currentComboIndex == 2)
        {
            PlaySwingAudio(swingHeavyClip != null ? swingHeavyClip : swingMediumClip);
        }
        else if (currentComboIndex == 1)
        {
            PlaySwingAudio(swingMediumClip != null ? swingMediumClip : swingLightClip);
        }
        else
        {
            PlaySwingAudio(swingLightClip);
        }
    }

#if UNITY_EDITOR

    private void AutoAssignCombatAudio()
    {
        if (swingLightClip == null)
            swingLightClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Light.wav");
        if (swingMediumClip == null)
            swingMediumClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Medium.wav");
        if (swingHeavyClip == null)
            swingHeavyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Heavy.wav");
        if (swingDashClip == null)
            swingDashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Dash.wav");
        if (swingSkillClip == null)
            swingSkillClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Skill.wav");
        if (swingPunchClip == null)
            swingPunchClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Combat/SFX_Weapon_Swing_Punch.wav");
    }
#endif

    #endregion
}
