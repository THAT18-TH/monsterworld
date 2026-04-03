using System;

namespace MonsterWorldLike.Monsters
{
    public enum MonsterTask
    {
        Idle,
        Farming,
        Collecting,
        Exploring,
        Building
    }

    [Serializable]
    public class MonsterInstance
    {
        public string monsterId;
        public int assignedPlotId = -1;
        public MonsterTask currentTask = MonsterTask.Idle;
        public int energy = 100;
        public int happiness = 50;
        public int level = 1;
        public int xp;
        public long lastFedUnix;
        public long lastWorkUnix;

        public MonsterInstance(string id)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            monsterId = id;
            lastFedUnix = now;
            lastWorkUnix = now;
        }
    }
}
