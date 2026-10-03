namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining daily economy accounting services (harvesting, trade).
    /// </summary>
    public interface IDailyEconomyService
    {
        void RecordCropHarvested(object item, int count);
        void RecordCombatGold(int goldAmount);
    }
}
