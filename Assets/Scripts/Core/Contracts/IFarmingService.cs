namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Service contract allowing decoupled features to query farming state,
    /// such as crops about to ripen prior to early sleep or day advancement,
    /// and managing farmland expansion capacity.
    /// </summary>
    public interface IFarmingService
    {
        bool HasCropsAboutToRipen(float thresholdSeconds, out string cropSummary);
        int UnlockedPlotCount { get; }
        int MaxPlotCount { get; }
        bool IsFullyExpanded { get; }
        bool TryExpandPlots(IWalletService wallet);
    }
}
