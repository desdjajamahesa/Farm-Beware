using System;
using System.Collections.Generic;
using UnityEngine;
using FeaturesFarming.Data;

namespace FeaturesFarming.Core
{
    /// <summary>
    /// Pure C# container managing coordinate indexing and lifecycle evaluation for all farm tiles.
    /// Exposes discrete C# events to notify presentation adapters of state mutations.
    /// </summary>
    public class FarmGrid
    {
        private readonly Dictionary<Vector2Int, SoilTile> _tiles = new Dictionary<Vector2Int, SoilTile>();

        /// <summary>
        /// Triggered whenever an individual soil tile undergoes a state change (Till, Water, Plant, AdvanceDay, Harvest).
        /// </summary>
        public event Action<Vector2Int, SoilTile> OnTileUpdated;

        public IReadOnlyDictionary<Vector2Int, SoilTile> Tiles => _tiles;

        /// <summary>
        /// Registers a new tile coordinate into the grid.
        /// </summary>
        public bool RegisterTile(Vector2Int coord, SoilState initialState = SoilState.Empty)
        {
            if (_tiles.ContainsKey(coord))
                return false;

            var tile = new SoilTile(coord, initialState);
            _tiles[coord] = tile;
            OnTileUpdated?.Invoke(coord, tile);
            return true;
        }

        /// <summary>
        /// Checks if a tile exists at the given grid coordinate.
        /// </summary>
        public bool HasTile(Vector2Int coord) => _tiles.ContainsKey(coord);

        /// <summary>
        /// Retrieves the tile at the given coordinate, or null if unpopulated.
        /// </summary>
        public SoilTile GetTile(Vector2Int coord)
        {
            _tiles.TryGetValue(coord, out SoilTile tile);
            return tile;
        }

        /// <summary>
        /// Attempts to till the tile at the specified coordinate.
        /// </summary>
        public bool TryTill(Vector2Int coord)
        {
            if (!_tiles.TryGetValue(coord, out SoilTile tile))
                return false;

            if (tile.TryTill())
            {
                OnTileUpdated?.Invoke(coord, tile);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to water the tile at the specified coordinate.
        /// </summary>
        public bool TryWater(Vector2Int coord)
        {
            if (!_tiles.TryGetValue(coord, out SoilTile tile))
                return false;

            if (tile.TryWater())
            {
                OnTileUpdated?.Invoke(coord, tile);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to plant a crop at the specified coordinate.
        /// </summary>
        public bool TryPlant(Vector2Int coord, string cropId)
        {
            if (!_tiles.TryGetValue(coord, out SoilTile tile))
                return false;

            if (tile.TryPlant(cropId))
            {
                OnTileUpdated?.Invoke(coord, tile);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to harvest the crop at the specified coordinate.
        /// </summary>
        public bool TryHarvest(Vector2Int coord, out string harvestedCropId)
        {
            harvestedCropId = null;
            if (!_tiles.TryGetValue(coord, out SoilTile tile))
                return false;

            if (tile.TryHarvest(out harvestedCropId))
            {
                OnTileUpdated?.Invoke(coord, tile);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluates daily crop progression across all tiles upon day rollover.
        /// </summary>
        public void AdvanceDay(Func<string, CropData> cropLookup)
        {
            foreach (var kvp in _tiles)
            {
                Vector2Int coord = kvp.Key;
                SoilTile tile = kvp.Value;

                CropData cropData = null;
                if (!string.IsNullOrEmpty(tile.PlantedCropId) && cropLookup != null)
                {
                    cropData = cropLookup(tile.PlantedCropId);
                }

                tile.AdvanceDay(cropData);
                OnTileUpdated?.Invoke(coord, tile);
            }
        }

        /// <summary>
        /// Clears all registered tiles from the grid.
        /// </summary>
        public void Clear()
        {
            _tiles.Clear();
        }
    }
}
