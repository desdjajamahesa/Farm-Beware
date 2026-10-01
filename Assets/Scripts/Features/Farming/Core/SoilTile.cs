using System;
using UnityEngine;
using FeaturesFarming.Data;

namespace FeaturesFarming.Core
{
    /// <summary>
    /// Pure C# POCO representing an individual soil tile's lifecycle and state.
    /// Strictly decoupled from MonoBehaviour to allow headless testing and zero-overhead simulation.
    /// </summary>
    [Serializable]
    public class SoilTile
    {
        public Vector2Int Coordinates { get; private set; }
        public SoilState State { get; private set; }
        public bool IsWatered { get; private set; }
        public string PlantedCropId { get; private set; }
        public int DaysGrown { get; private set; }

        public SoilTile(Vector2Int coordinates, SoilState initialState = SoilState.Empty)
        {
            Coordinates = coordinates;
            State = initialState;
            IsWatered = false;
            PlantedCropId = null;
            DaysGrown = 0;
        }

        /// <summary>
        /// Attempts to till the soil with a hoe.
        /// </summary>
        public bool TryTill()
        {
            if (State != SoilState.Empty)
                return false;

            State = SoilState.Tilled;
            return true;
        }

        /// <summary>
        /// Attempts to apply water to the tile.
        /// </summary>
        public bool TryWater()
        {
            if (IsWatered)
                return false;

            if (State == SoilState.Tilled)
            {
                IsWatered = true;
                return true;
            }

            if (State == SoilState.PlantedDry)
            {
                IsWatered = true;
                State = SoilState.PlantedWatered;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to plant a crop seed into tilled soil.
        /// </summary>
        public bool TryPlant(string cropId)
        {
            if (string.IsNullOrEmpty(cropId))
                return false;

            if (State != SoilState.Tilled)
                return false;

            PlantedCropId = cropId;
            DaysGrown = 0;

            if (IsWatered)
            {
                State = SoilState.PlantedWatered;
            }
            else
            {
                State = SoilState.PlantedDry;
            }

            return true;
        }

        /// <summary>
        /// Attempts to harvest a fully mature crop. Reverts tile back to Tilled soil upon success.
        /// </summary>
        public bool TryHarvest(out string harvestedCropId)
        {
            if (State != SoilState.ReadyToHarvest)
            {
                harvestedCropId = null;
                return false;
            }

            harvestedCropId = PlantedCropId;
            PlantedCropId = null;
            DaysGrown = 0;
            IsWatered = false;
            State = SoilState.Tilled;
            return true;
        }

        /// <summary>
        /// Advances tile state when the day rolls over.
        /// Evaluates active crop growth only if the tile was watered. Resets soil moisture to dry overnight.
        /// </summary>
        public void AdvanceDay(CropData cropData)
        {
            if (State == SoilState.PlantedWatered)
            {
                DaysGrown++;

                if (cropData != null && cropData.IsMature(DaysGrown))
                {
                    State = SoilState.ReadyToHarvest;
                }
                else
                {
                    // Soil moisture dries up, requiring watering on the new day to continue growth
                    State = SoilState.PlantedDry;
                }

                IsWatered = false;
            }
            else if (State == SoilState.PlantedDry)
            {
                // Unwatered crop does not progress in growth
                IsWatered = false;
            }
            else if (State == SoilState.Tilled)
            {
                // Tilled soil dries up overnight
                IsWatered = false;
            }
        }

        /// <summary>
        /// Direct state mutator for unit testing and save/load hydration.
        /// </summary>
        public void SetStateDirect(SoilState state, bool isWatered, string cropId, int daysGrown)
        {
            State = state;
            IsWatered = isWatered;
            PlantedCropId = cropId;
            DaysGrown = daysGrown;
        }
    }
}
