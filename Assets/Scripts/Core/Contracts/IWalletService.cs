namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining player monetary balance, spending, and rewards.
    /// </summary>
    public interface IWalletService
    {
        int CurrentGold { get; }
        bool CanAfford(int amount);
        bool SpendGold(int amount);
        void AddGold(int amount);
    }
}
