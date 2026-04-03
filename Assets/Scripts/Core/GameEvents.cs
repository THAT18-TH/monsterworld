using System;

namespace MonsterWorldLike.Core
{
    public static class GameEvents
    {
        public static event Action<string, int> OnSeedBought;
        public static event Action<int, string> OnSeedPlanted;
        public static event Action<int> OnPlotWatered;
        public static event Action<int, string, int> OnHarvested;
        public static event Action<string, int> OnMonsterBought;
        public static event Action<long> OnMonsterCollected;
        public static event Action<string> OnBuildingPlaced;
        public static event Action<string> OnDecorationPlaced;
        public static event Action<int, int> OnDailyRewardClaimed;

        public static void SeedBought(string plantId, int amount) => OnSeedBought?.Invoke(plantId, amount);
        public static void SeedPlanted(int plotId, string plantId) => OnSeedPlanted?.Invoke(plotId, plantId);
        public static void PlotWatered(int plotId) => OnPlotWatered?.Invoke(plotId);
        public static void Harvested(int plotId, string plantId, int amount) => OnHarvested?.Invoke(plotId, plantId, amount);
        public static void MonsterBought(string monsterId, int amount) => OnMonsterBought?.Invoke(monsterId, amount);
        public static void MonsterCollected(long gold) => OnMonsterCollected?.Invoke(gold);
        public static void BuildingPlaced(string buildingId) => OnBuildingPlaced?.Invoke(buildingId);
        public static void DecorationPlaced(string decorationId) => OnDecorationPlaced?.Invoke(decorationId);
        public static void DailyRewardClaimed(int rewardGold, int streak) => OnDailyRewardClaimed?.Invoke(rewardGold, streak);
    }
}
