using System;
using UnityEngine;
using MonsterWorldLike.Core;

namespace MonsterWorldLike.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }
        public static event Action InstanceReady;

        [Header("Initial Balances")]
        [SerializeField] private long initialGold = 500;
        [SerializeField] private long initialGems = 10;
        [SerializeField] private long initialFood = 200;

        public long Gold { get; private set; }
        public long Gems { get; private set; }
        public long Food { get; private set; }

        public event Action OnBalanceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Gold = initialGold;
            Gems = initialGems;
            Food = initialFood;
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetBalances(long gold, long gems, long food)
        {
            Gold = Math.Max(0, gold);
            Gems = Math.Max(0, gems);
            Food = Math.Max(0, food);
            OnBalanceChanged?.Invoke();
            SaveManager.MarkDirty();
        }

        public void AddGold(long amount)
        {
            Gold = Math.Max(0, Gold + amount);
            OnBalanceChanged?.Invoke();
            SaveManager.MarkDirty();
        }

        public bool SpendGold(long amount)
        {
            if (amount < 0 || Gold < amount)
            {
                return false;
            }

            Gold -= amount;
            OnBalanceChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }
    }
}
