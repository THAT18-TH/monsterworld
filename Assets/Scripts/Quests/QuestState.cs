using System;

namespace MonsterWorldLike.Quests
{
    [Serializable]
    public class QuestState
    {
        public string questId;
        public string requiredPlantId;
        public int requiredAmount;
        public int currentAmount;
        public int rewardGold;
        public int rewardXp;
        public bool completed;
    }
}
