namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining fluid management and water consumption/refill capabilities.
    /// </summary>
    public interface IWaterService
    {
        float CurrentWater { get; }
        float MaxWater { get; }
        bool HasWater(float amount);
        bool ConsumeWater(float amount);
        void RefillWater(float amount);
        void DrinkSip(float amount);
    }
}
