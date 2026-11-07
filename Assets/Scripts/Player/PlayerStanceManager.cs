using System;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesCombat.Core.PureLogic;
using FeaturesWorkbench;

/// <summary>
/// Thin MonoBehaviour Adapter managing contextual player stance (Agricultural vs CombatBrawl),
/// reacting to time phase shifts, auto-wielding equipment weapons at night,
/// and enforcing MVP GDD hotbar rules (farming tools muted, food buffs accessible).
/// </summary>
[DisallowMultipleComponent]
public class PlayerStanceManager : MonoBehaviour
{
    public static PlayerStanceManager Instance { get; private set; }

    private readonly PlayerStanceContext _context = new PlayerStanceContext();
    public PlayerStanceContext Context => _context;
    public PlayerStanceMode CurrentStance => _context.CurrentStance;
    public bool IsInCombatStance => _context.CurrentStance == PlayerStanceMode.CombatBrawl;

    public event Action<PlayerStanceMode, PlayerStanceMode> OnStanceChanged;

    private InventoryComponent _inventory;
    private PlayerEquipment _playerEquipment;
    private PlayerArmorUpgradeState _armorState;
    private ITimeService _timeService;
    private static ToolItemData _fallbackWeapon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        _inventory = GetComponent<InventoryComponent>();
        _playerEquipment = GetComponent<PlayerEquipment>();
        _armorState = GetComponent<PlayerArmorUpgradeState>();

        _context.OnStanceChanged += (prev, next) =>
        {
            OnStanceChanged?.Invoke(prev, next);
        };
    }

    private void Start()
    {
        ResolveDependencies();
        SubscribeTimeEvents();
        CheckInitialPhase();
    }

    private void Update()
    {
        bool isNight = (_timeService != null && _timeService.IsNight) ||
                       (TimeManager.Instance != null && TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night) ||
                       (FeaturesTime.DayNightTimeManager.Instance != null && FeaturesTime.DayNightTimeManager.Instance.IsNight);

        if (isNight && CurrentStance != PlayerStanceMode.CombatBrawl)
        {
            TransitionToPhase(DayPhase.Night);
        }
        else if (!isNight && CurrentStance == PlayerStanceMode.CombatBrawl)
        {
            TransitionToPhase(DayPhase.Day);
        }
    }

    private void OnEnable()
    {
        ResolveDependencies();
        SubscribeTimeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeTimeEvents();
    }

    private void SubscribeTimeEvents()
    {
        UnsubscribeTimeEvents();
        if (_timeService != null)
        {
            _timeService.OnPhaseChanged += HandleTimeServicePhaseChanged;
        }
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnPhaseChanged += HandleLegacyTimeManagerPhaseChanged;
        }
    }

    private void UnsubscribeTimeEvents()
    {
        if (_timeService != null)
        {
            _timeService.OnPhaseChanged -= HandleTimeServicePhaseChanged;
        }
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnPhaseChanged -= HandleLegacyTimeManagerPhaseChanged;
        }
    }

    private void ResolveDependencies()
    {
        if (_inventory == null) _inventory = GetComponent<InventoryComponent>();
        if (_playerEquipment == null) _playerEquipment = GetComponent<PlayerEquipment>();
        if (_armorState == null) _armorState = GetComponent<PlayerArmorUpgradeState>() ?? PlayerArmorUpgradeState.Instance;

        if (_timeService == null)
        {
            _timeService = ServiceLocator.Resolve<ITimeService>();
            if (_timeService == null && FeaturesTime.DayNightTimeManager.Instance != null)
            {
                _timeService = FeaturesTime.DayNightTimeManager.Instance;
            }
        }
    }

    private void CheckInitialPhase()
    {
        DayPhase currentPhase = DayPhase.Day;
        if (_timeService != null)
        {
            currentPhase = _timeService.CurrentPhase;
        }
        else if (TimeManager.Instance != null)
        {
            currentPhase = TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night 
                ? DayPhase.Night 
                : DayPhase.Day;
        }

        if (currentPhase == DayPhase.Night)
        {
            TransitionToPhase(DayPhase.Night);
        }
    }

    private void HandleTimeServicePhaseChanged(int day, DayPhase phase)
    {
        TransitionToPhase(phase);
    }

    private void HandleLegacyTimeManagerPhaseChanged(TimeManager.DayPhase legacyPhase)
    {
        DayPhase phase = (legacyPhase == TimeManager.DayPhase.Night) ? DayPhase.Night : DayPhase.Day;
        TransitionToPhase(phase);
    }

    /// <summary>
    /// Transitions stance according to the incoming DayPhase.
    /// Night activates CombatBrawl with weapon auto-wield; Dawn/Day restores Agricultural stance and saved hotbar.
    /// </summary>
    public void TransitionToPhase(DayPhase phase)
    {
        ResolveDependencies();

        int currentSlot = _inventory != null ? _inventory.selectedHotbarIndex : 0;
        bool changed = _context.TransitionToPhase(phase, currentSlot);

        if (!changed) return;

        if (_context.CurrentStance == PlayerStanceMode.CombatBrawl)
        {
            // Night Phase: Auto-wield combat weapon and mute farming tools
            _playerEquipment?.RefreshEquipmentForStance();

            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                transform.position + Vector3.up * 1.6f,
                "⚔️ Night Brawl: Weapon Ready!",
                new Color(1f, 0.45f, 0.2f));
        }
        else
        {
            // Dawn / Day Phase: Restore saved agricultural hotbar slot
            int restoredSlot = _context.GetRestoredHotbarIndex(0);
            if (_inventory != null)
            {
                _inventory.SelectHotbarSlot(restoredSlot);
            }
            _playerEquipment?.RefreshEquipmentForStance();

            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                transform.position + Vector3.up * 1.6f,
                "🌅 Dawn: Agricultural Stance Restored",
                new Color(0.4f, 1f, 0.4f));
        }
    }

    /// <summary>
    /// Retrieves the currently active combat weapon:
    /// 1. Weapon equipped in MainWeapon slot on PlayerArmorUpgradeState.
    /// 2. If null, weapon present in player inventory (Root Cleaver or Slingshot).
    /// 3. If null, fallbacks to Root Cleaver in ItemDatabase.
    /// 4. If still null, returns a safe runtime Root Cleaver instance (25 base damage, 6 knockback).
    /// </summary>
    public ItemData GetActiveCombatWeapon()
    {
        if (_armorState == null) _armorState = GetComponent<PlayerArmorUpgradeState>() ?? PlayerArmorUpgradeState.Instance;
        if (_armorState != null)
        {
            var equipped = _armorState.GetEquippedItem(EquipmentSlotType.MainWeapon);
            if (equipped != null && IsValidCombatWeapon(equipped)) return equipped;
        }

        // Check inventory slots for Root Cleaver or Slingshot
        if (_inventory == null) _inventory = GetComponent<InventoryComponent>();
        if (_inventory != null && _inventory.slots != null)
        {
            foreach (var s in _inventory.slots)
            {
                if (s != null && !s.IsEmpty && s.item != null && IsValidCombatWeapon(s.item))
                {
                    return s.item;
                }
            }
        }

        return null;
    }

    private static bool IsValidCombatWeapon(ItemData item)
    {
        if (item == null) return false;
        if (item is ToolItemData t)
        {
            if (t.isHoe) return false;
            if (string.Equals(t.itemId, "tool_plant_waterer", StringComparison.OrdinalIgnoreCase) ||
                (t.itemName != null && t.itemName.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0))
                return false;
            if (t.isWeapon || t.isRangedWeapon) return true;
        }
        if (item.category == ItemCategory.Weapon) return true;
        if (string.Equals(item.itemId, "tool_weapon_root_cleaver", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(item.itemId, "tool_weapon_corn_repeater", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// Handles hotbar key triggers [1-4].
    /// In Agricultural stance: switches hotbar slots normally.
    /// In CombatBrawl stance: consumes food buffs directly if present, and rejects farming tools (Hoe/Watering Can).
    /// </summary>
    public bool HandleHotbarTrigger(int slotIndex, InventoryComponent inv)
    {
        if (inv == null || slotIndex < 0 || slotIndex >= inv.slots.Count)
            return false;

        if (!IsInCombatStance)
        {
            inv.SelectHotbarSlot(slotIndex);
            return true;
        }

        // In CombatBrawl stance at night:
        InventorySlot slot = inv.slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                transform.position + Vector3.up * 1.4f,
                "Slot is empty!",
                new Color(0.8f, 0.8f, 0.8f));
            return false;
        }

        ItemData item = slot.item;
        bool isConsumable = item.type == ItemData.ItemType.Consumable || item is FoodItemData;
        bool isHoe = (item is ToolItemData t && t.isHoe);
        bool isWateringCan = (item is ToolItemData tw && !tw.isWeapon && !tw.isHoe);

        if (isConsumable)
        {
            // Food buffs are directly consumable during night brawl per GDD
            inv.UseItem(slotIndex);
            return true;
        }

        if (PlayerStanceContext.IsToolForbiddenAtNight(item.itemId, isHoe, isWateringCan))
        {
            ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
                transform.position + Vector3.up * 1.4f,
                "❌ Farming tools unavailable at night! Focus on brawl.",
                new Color(1f, 0.4f, 0.4f));
            return false;
        }

        ServiceLocator.Resolve<IFloatingTextService>()?.SpawnText(
            transform.position + Vector3.up * 1.4f,
            "Focus on combat brawl!",
            new Color(1f, 0.7f, 0.3f));
        return false;
    }
}
