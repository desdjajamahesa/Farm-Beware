namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining drag-and-drop source behavior from sink stations into player inventory slots.
    /// </summary>
    public interface ISinkDropHandler
    {
        void HandleDropToPlayer(int targetSlotIndex);
    }
}
