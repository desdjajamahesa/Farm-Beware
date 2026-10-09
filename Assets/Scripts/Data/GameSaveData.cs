using System;
using System.Collections.Generic;

namespace FeaturesSaveSystem
{
    /// <summary>
    /// Lightweight metadata stored in the manifest and at the top of each save file.
    /// Allows the UI to render the slot list instantly without reading heavy game state payloads.
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public string saveId;              // Unique identifier (UUID or clean key)
        public string displayName;         // Player-entered custom name (e.g. "Chapter 1 - Hutan")
        public string fileName;            // Disk file name (e.g. "save_20260930_215500.json")
        public string timestamp;           // Sortable ISO timestamp (yyyy-MM-dd HH:mm:ss)
        public string formattedDate;       // Pretty human-readable date
        public int dayNumber = 1;          // In-game day number
        public string phaseName = "Day";   // "Day" or "Night"
        public int gold = 0;               // Player currency
        public int currentHealth = 100;
        public int maxHealth = 100;
        public string locationName = "House"; // Room / area description
        public long fileSizeBytes = 0;
    }

    /// <summary>
    /// Fast-reading index manifest keeping track of all available save slots.
    /// </summary>
    [Serializable]
    public class SaveManifest
    {
        public int manifestVersion = 1;
        public List<SaveMetadata> slots = new List<SaveMetadata>();
        public string lastPlayedSaveId;
    }

    /// <summary>
    /// Complete game state payload serialized to disk for a single save file.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public int saveVersion = 1;
        public SaveMetadata metadata = new SaveMetadata();

        // Player Transform
        public float playerPosX;
        public float playerPosY;
        public float playerPosZ;
        public float playerRotY;

        // Player Stats
        public int health;
        public float stamina;
        public float hunger;
        public float thirst;

        // Economy & Daily Market
        public int gold;
        public int sweetPotatoPrice;
        public int taroPrice;
        public int cornPrice;
        public int dailyCropsHarvested;
        public int dailyCropsSold;
        public int dailyGoldEarnedTrading;
        public int dailyMonstersSlain;
        public int dailyGoldEarnedCombat;

        // Water Bottle & Plant Waterer
        public float waterBottleAmount;
        public float plantWatererAmount;

        // Time System
        public int currentDay;
        public int currentPhase; // 0 = Day, 1 = Night
        public float currentHour; // 0.0f - 24.0f continuous clock
        public bool isNightEncounterCleared;

        // Inventory
        public int selectedHotbarIndex = 0;
        public List<SavedInventorySlot> inventorySlots = new List<SavedInventorySlot>();
        public List<SavedContainerInventory> containerInventories = new List<SavedContainerInventory>();

        // Farmland Crops
        public List<SavedFarmlandTile> farmlandTiles = new List<SavedFarmlandTile>();

        // Wardrobe
        public string outfitName;
        public bool isHatEquipped = true;

        // Weapon Upgrades
        public int weaponLevel = 1;
        public bool sweetPotatoPathUnlocked;
        public bool taroPathUnlocked;
        public bool cornPathUnlocked;

        // Combat & Night Brawl
        public int nightCurrentWave;
        public int nightTotalWaves;
        public bool isNightBrawlActive;
        public List<SavedEnemyData> activeEnemies = new List<SavedEnemyData>();
    }

    [Serializable]
    public class SavedContainerInventory
    {
        public string containerId;
        public List<SavedInventorySlot> slots = new List<SavedInventorySlot>();
    }

    [Serializable]
    public class SavedEnemyData
    {
        public int enemyType;
        public int currentHealth;
        public int maxHealth;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
    }

    [Serializable]
    public class SavedInventorySlot
    {
        public int slotIndex;
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class SavedFarmlandTile
    {
        public string tileName;
        public int tileState;
        public string seedItemId;
        public float growthProgress;
        public float currentTimer;
        public float growthDuration;
    }
}
