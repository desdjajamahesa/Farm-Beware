using System.Collections.Generic;
using UnityEngine;

public class InventoryComponent : MonoBehaviour
{
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int maxCapacity;

    // BLOCK TROPHY: bila true (mis. Inventory Player), item bertipe Trophy TIDAK PERNAH
    // bisa masuk ke inventory ini dari jalur mana pun (AddItem/MoveItemToSlot/TransferItemTo).
    [Tooltip("Tidak mengizinkan item tipe Trophy masuk ke inventory ini (Contoh: inventory Player).")]
    [SerializeField] private bool blockTrophyItems;

    // Pembatas kategori makanan yang boleh masuk (kosong = tanpa pembatasan).
    // Contoh: Kulkas = [Vegetable, Fruit]; Sink = hanya item yang bisa dicuci.
    [Tooltip("Pembatas kategori makanan (FoodCategory). Kosong = semua boleh masuk.")]
    [SerializeField] private List<ItemData.FoodCategory> allowedFoodCategories;

    // Akses baca publik untuk rule backend lain (mis. alur Trophy Cabinet / stasiun dapur).
    public bool BlocksTrophyItems { get { return blockTrophyItems; } }
    public List<ItemData.FoodCategory> AllowedFoodCategories { get { return allowedFoodCategories; } }

    /// <summary>
    /// Set pembatasan kategori makanan (meng-inisialisasi list bila masih null).
    /// Dipakai Kulkas untuk membatasi hanya sayur+buah.
    /// </summary>
    public void SetAllowedFoodCategories(List<ItemData.FoodCategory> categories)
    {
        if (allowedFoodCategories == null)
            allowedFoodCategories = new List<ItemData.FoodCategory>();
        allowedFoodCategories.Clear();
        if (categories != null)
            allowedFoodCategories.AddRange(categories);
    }

    /// <summary>
    /// Aturan backend tunggal: menentukan apakah sebuah item boleh MASUK ke inventory ini.
    /// Kombinasi: blockTrophyItems + pembatasan kategori makanan.
    /// </summary>
    public bool CanAcceptItem(ItemData item)
    {
        if (item == null)
            return false;

        // Trophy tidak boleh masuk ke inventory pemblokir (mis. Player).
        if (blockTrophyItems && item.type == ItemData.ItemType.Trophy)
            return false;

        // Pembatasan kategori makanan (bila daftar diisi).
        if (allowedFoodCategories != null && allowedFoodCategories.Count > 0)
        {
            bool allowed = false;
            for (int i = 0; i < allowedFoodCategories.Count; i++)
            {
                if (allowedFoodCategories[i] == item.foodCategory)
                {
                    allowed = true;
                    break;
                }
            }
            if (!allowed)
                return false;
        }

        return true;
    }

    // Listener hooking (tanpa mengubah struktur data) untuk memberi tahu UI ketika slot berubah.
    public System.Action OnInventoryChanged;

    // State & event Hotbar (4 slot pertama).
    public int selectedHotbarIndex = 0;
    public System.Action<int> OnHotbarSelected;

    public void SelectHotbarSlot(int index)
    {
        // Batasi indeks ke rentang hotbar 0..3.
        index = Mathf.Clamp(index, 0, 3);
        selectedHotbarIndex = index;
        OnHotbarSelected?.Invoke(selectedHotbarIndex);
    }

    public void ResetInventory(int capacity)
    {
        maxCapacity = capacity;
        slots.Clear();
        for (int i = 0; i < maxCapacity; i++)
            slots.Add(new InventorySlot());
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        // Aturan backend terpusat: trophy/ kategori terlarang tidak boleh masuk.
        if (!CanAcceptItem(item))
            return false;

        int remaining = amount;

        // 1. Tumpuk ke slot yang sudah punya item yang sama dan belum penuh
        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0) break;

            if (!slot.IsEmpty && slot.item == item)
            {
                int space = item.maxStack - slot.quantity;
                int toAdd = Mathf.Min(space, remaining);
                if (toAdd > 0)
                {
                    slot.quantity += toAdd;
                    remaining -= toAdd;
                }
            }
        }

        // 2. Isi ke slot kosong bila masih ada sisa.
        if (remaining > 0)
        {
            foreach (InventorySlot slot in slots)
            {
                if (remaining <= 0) break;

                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(item.maxStack, remaining);
                    slot.item = item;
                    slot.quantity = toAdd;
                    remaining -= toAdd;
                }
            }
        }

        if (remaining == 0)
            OnInventoryChanged?.Invoke();

        return remaining == 0;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (CountTotalQuantity(item) < amount)
            return false;

        int remaining = amount;

        // Kurangi dari slot yang memiliki item (dari belakang agar rapi).
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || slot.item != item)
                continue;

            int toRemove = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= toRemove;
            remaining -= toRemove;

            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }
        }

        if (remaining == 0)
            OnInventoryChanged?.Invoke();

        return remaining == 0;
    }

    public void TransferItemTo(InventoryComponent targetInventory, ItemData item, int amount)
    {
        if (targetInventory == null || item == null || amount <= 0)
            return;

        if (targetInventory == this)
            return;

        int available = CountTotalQuantity(item);
        int amountToMove = Mathf.Min(amount, available);
        if (amountToMove <= 0)
            return;

        // Coba tambah dulu ke inventori target. Jika gagal penuh, batalkan.
        if (!targetInventory.AddItem(item, amountToMove))
            return;

        RemoveItem(item, amountToMove);

        // Jamin kedua inventori (sumber & target) memberi tahu UI untuk refresh.
        targetInventory.OnInventoryChanged?.Invoke();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Overload berbasis INDEKS: pindahkan SELURUH isi slot [sourceIndex] pada inventory ini
    /// ke targetInventory. Dipakai sebagai backend command oleh TrophySystemManager
    /// (Rack -> Cabinet) dan alur data-driven lain. Bila target penuh/tidak muat,
    /// transaksi dibatalkan total (tidak ada isi slot yang dihapus).
    /// </summary>
    /// <param name="targetInventory">Inventori tujuan transfer.</param>
    /// <param name="sourceIndex">Indeks slot sumber pada inventory ini.</param>
    public void TransferItemTo(InventoryComponent targetInventory, int sourceIndex)
    {
        if (targetInventory == null || targetInventory == this)
            return;

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
            return;

        InventorySlot source = slots[sourceIndex];
        if (source == null || source.IsEmpty || source.item == null)
            return;

        // Coba tambahkan seluruh isi slot ke target dulu; bila gagal (penuh) batal total.
        if (!targetInventory.AddItem(source.item, source.quantity))
            return;

        // Transaksi berhasil: kosongkan slot sumber.
        source.item = null;
        source.quantity = 0;

        // AddItem sudah otomatis me-refresh target; sumber wajib di-refresh di sini.
        OnInventoryChanged?.Invoke();
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        ItemData item = slot.item;

        if (item.type == ItemData.ItemType.Consumable)
        {
            // Terapkan efek penyembuhan ke status pemain bila ada.
            PlayerStats playerStats = GetComponent<PlayerStats>();
            if (playerStats != null)
                playerStats.Heal(item.healAmount);

            Debug.Log("Mengonsumsi " + item.itemName + " memulihkan " + item.healAmount + " HP");

            // Kurangi quantity PADA SLOT yang diklik secara spesifik.
            slot.quantity -= 1;
            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }

            OnInventoryChanged?.Invoke();
        }
    }

    public void DropItem(int slotIndex)
    {
        // Validasi batas array dan pastikan slot tidak kosong.
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        ItemData item = slot.item;
        Debug.Log(string.Format("Dropping {0} to the ground!", item.itemName));

        // Kurangi quantity pada slot yang di-drop; kosongkan bila habis.
        slot.quantity -= 1;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();

        // Jika item yang di-drop berasal dari slot hotbar yang sedang active,
        // beri tahu agar sistem visual equipment me-refresh (mis. hancurkan model).
        if (slotIndex == selectedHotbarIndex)
            OnHotbarSelected?.Invoke(selectedHotbarIndex);
    }

    /// <summary>
    /// Ganti isi satu slot dengan item lain (transformasi state makanan:
    /// kotor -> bersih -> matang, dst.). Fires OnInventoryChanged.
    /// </summary>
    public void ReplaceItemAt(int index, ItemData item, int quantity)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (slot == null)
            return;

        slot.item = item;
        slot.quantity = quantity;
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Kurangi quantity pada satu slot secara spesifik (dipakai mengonsumsi bahan
    /// saat mulai memasak). Fires OnInventoryChanged.
    /// </summary>
    public void RemoveFromSlot(int index, int amount)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (slot == null || slot.IsEmpty || amount <= 0)
            return;

        slot.quantity -= amount;
        if (slot.quantity <= 0)
        {
            slot.item = null;
            slot.quantity = 0;
        }

        OnInventoryChanged?.Invoke();
    }

    // Perpindahan presisi slot-ke-slot antar inventory (atau dalam inventory yang sama).
    // Menangani tiga kasus: pindah ke slot kosong, penumpukan item sama, dan pertukaran item berbeda.
    public void MoveItemToSlot(int sourceIndex, InventoryComponent targetInventory, int targetIndex)
    {
        if (targetInventory == null)
            return;

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
            return;
        if (targetIndex < 0 || targetIndex >= targetInventory.slots.Count)
            return;

        InventorySlot sourceSlot = slots[sourceIndex];
        InventorySlot targetSlot = targetInventory.slots[targetIndex];
        if (sourceSlot == null || targetSlot == null)
            return;

        if (sourceSlot.IsEmpty)
            return;

        // Aturan backend terpusat: item yang tidak boleh masuk target (trophy/kategori) ditolak.
        if (sourceSlot.item != null && !targetInventory.CanAcceptItem(sourceSlot.item))
            return;

        if (targetSlot.IsEmpty)
        {
            // Pindahkan seluruh data dari source ke target, lalu kosongkan source.
            targetSlot.item = sourceSlot.item;
            targetSlot.quantity = sourceSlot.quantity;
            sourceSlot.item = null;
            sourceSlot.quantity = 0;
        }
        else if (targetSlot.item == sourceSlot.item)
        {
            // Penumpukan: isi ruang kosong target dengan item dari source.
            int space = targetSlot.item.maxStack - targetSlot.quantity;
            if (space > 0)
            {
                int toMove = Mathf.Min(space, sourceSlot.quantity);
                targetSlot.quantity += toMove;
                sourceSlot.quantity -= toMove;
                if (sourceSlot.quantity <= 0)
                {
                    sourceSlot.item = null;
                    sourceSlot.quantity = 0;
                }
            }
        }
        else
        {
            // Item berbeda: tukar data antar slot.
            InventorySlot temp = new InventorySlot();
            temp.item = targetSlot.item;
            temp.quantity = targetSlot.quantity;
            targetSlot.item = sourceSlot.item;
            targetSlot.quantity = sourceSlot.quantity;
            sourceSlot.item = temp.item;
            sourceSlot.quantity = temp.quantity;
        }

        OnInventoryChanged?.Invoke();
        if (this != targetInventory)
            targetInventory.OnInventoryChanged?.Invoke();

        if (selectedHotbarIndex == sourceIndex)
            OnHotbarSelected?.Invoke(selectedHotbarIndex);
        if (targetInventory.selectedHotbarIndex == targetIndex)
            targetInventory.OnHotbarSelected?.Invoke(targetInventory.selectedHotbarIndex);
    }

    public void SwapSlots(int indexA, int indexB)
    {        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
            return;

        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        // Beri tahu UI agar menggambar ulang seluruh slot (ikon + quantity).
        OnInventoryChanged?.Invoke();
    }

    public int CountItem(ItemData item)
    {
        return CountTotalQuantity(item);
    }

    private int CountTotalQuantity(ItemData item)
    {
        int total = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item)
                total += slot.quantity;
        }
        return total;
    }
}