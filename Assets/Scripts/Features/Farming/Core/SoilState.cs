namespace FeaturesFarming.Core
{
    /// <summary>
    /// Represents the discrete lifecycle state of a soil tile in the farm grid.
    /// </summary>
    public enum SoilState
    {
        Empty = 0,          // Raw, unworked grass or soil
        Tilled = 1,         // Tilled with a hoe, dry loose soil
        PlantedDry = 2,     // Seed planted, waiting for water
        PlantedWatered = 3, // Seed planted and watered (active growth state)
        ReadyToHarvest = 4  // Fully mature crop, ready to be harvested
    }
}
