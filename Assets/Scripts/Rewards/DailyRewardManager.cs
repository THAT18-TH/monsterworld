using System;
using MonsterWorldLike.Core;
using MonsterWorldLike.Economy;
using UnityEngine;

namespace MonsterWorldLike.Rewards
{
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField, Min(1)] private int baseRewardGold = 50;

        public long LastClaimUnix { get; private set; }
        public int Streak { get; private set; }

        public event Action OnDailyRewardChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool CanClaim()
        {
            if (LastClaimUnix <= 0)
            {
                return true;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now - LastClaimUnix >= 60L * 60L * 24L;
        }

        public bool Claim()
        {
            if (!CanClaim())
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var wasConsecutive = LastClaimUnix > 0 && now - LastClaimUnix <= 60L * 60L * 48L;
            Streak = wasConsecutive ? Streak + 1 : 1;
            LastClaimUnix = now;

            var reward = baseRewardGold * Mathf.Clamp(Streak, 1, 7);
            economy.AddGold(reward);
            GameEvents.DailyRewardClaimed(reward, Streak);
            OnDailyRewardChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public void ApplySaveState(long lastClaimUnix, int streak)
        {
            LastClaimUnix = Math.Max(0, lastClaimUnix);
            Streak = Math.Max(0, streak);
            OnDailyRewardChanged?.Invoke();
        }
    }
}
