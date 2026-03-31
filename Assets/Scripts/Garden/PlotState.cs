using System;

namespace MonsterWorldLike.Garden
{
    [Serializable]
    public class PlotState
    {
        public int plotId;
        public bool unlocked;
        public string plantedSeedId;
        public long plantedAtUnix;
        public long readyAtUnix;
        public bool needsWater;
        public bool withered;

        public PlotState(int id, bool isUnlocked)
        {
            plotId = id;
            unlocked = isUnlocked;
        }

        public bool IsPlanted => !string.IsNullOrEmpty(plantedSeedId) && !withered;

        public bool IsEmpty => unlocked && string.IsNullOrEmpty(plantedSeedId) && !withered;

        public bool IsReadyToHarvest(long nowUnix)
        {
            return IsPlanted && !needsWater && nowUnix >= readyAtUnix;
        }

        public void ClearPlant()
        {
            plantedSeedId = null;
            plantedAtUnix = 0;
            readyAtUnix = 0;
            needsWater = false;
            withered = false;
        }
    }
}
