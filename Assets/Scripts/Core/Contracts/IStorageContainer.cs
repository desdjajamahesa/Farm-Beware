namespace FarmBeware.Core.Runtime
{
    public interface IInventorySlot
    {
        bool IsEmpty { get; }
        int Quantity { get; }
    }

    /// <summary>
    /// Contract defining a storage container capable of item deposit and withdrawal transactions.
    /// </summary>
    public interface IStorageContainer
    {
        bool TryDeposit(object item, int quantity);
        bool TryWithdraw(int slotIndex, int quantity, out object result);
    }
}
