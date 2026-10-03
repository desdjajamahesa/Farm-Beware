namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining player monetary balance, spending, and rewards.
    /// </summary>
    public interface IWalletService
    {
        int CurrentGold { get; }
        event System.Action<int> OnGoldChanged;
        bool CanAfford(int amount);
        bool SpendGold(int amount);
        void AddGold(int amount);
    }
}
