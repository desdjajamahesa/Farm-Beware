using System;

/// <summary>
/// Interface untuk stasiun dapur yang memancarkan event proses.
/// Digunakan oleh KitchenStationSoundFx dan KitchenStationProgressOverlay
/// agar bisa bekerja dengan stasiun apapun (KitchenStation, GenshinStove, dll).
/// </summary>
public interface IKitchenStationEvents
{
    // Events
    event Action<int, float> OnProcessStarted;   // (slotIndex, duration)
    event Action<int, float> OnProcessProgress;  // (slotIndex, 0..1)
    event Action<int> OnProcessCompleted;        // (slotIndex)
    event Action<int> OnProcessCancelled;        // (slotIndex)

    // Read-only state (untuk polling di Update)
    bool IsProcessing(int slotIndex);
    float GetSlotProgress(int slotIndex);
}
