using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining inventory slot processing state and progress tracking.
    /// </summary>
    public interface ISlotProgressProvider
    {
        bool IsProcessing(int slotIndex);
        float GetSlotProgress(int slotIndex);
        event Action<int> OnProcessCompleted;
    }
}
