using System;
using UnityEngine;

namespace FeaturesEconomy
{
    /// <summary>
    /// Manages the player's currency (Gold).
    /// Provides methods for spending and earning gold, firing events for UI updates.
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PlayerWallet>();
                    if (instance == null)
                    {
                        var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            instance = player.GetComponent<PlayerWallet>() ?? player.AddComponent<PlayerWallet>();
                        }
                    }
                }
                return instance;
            }
            private set => instance = value;
        }
        private static PlayerWallet instance;

        [Header("Starting Balance")]
        [Tooltip("Initial amount of gold the player starts with.")]
        [SerializeField] private int startingGold = 500;

        [Header("Runtime Balance")]
        [SerializeField] private int currentGold;

        public int CurrentGold => currentGold;

        public event Action<int> OnGoldChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;
            if (currentGold <= 0)
                currentGold = startingGold;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Start()
        {
            OnGoldChanged?.Invoke(currentGold);
        }

        public bool CanAfford(int amount)
        {
            return currentGold >= amount;
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0) return false;
            if (currentGold < amount) return false;

            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }
    }
}
