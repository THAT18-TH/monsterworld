using UnityEngine;

namespace MonsterWorldLike.Quests
{
    public enum QuestObjectiveType
    {
        HarvestPlant,
        PlantSeed,
        WaterPlot,
        BuySeed,
        BuyMonster,
        CollectMonsterGold,
        BuildBuilding,
        PlaceDecoration,
        ClaimDailyReward,
        EarnGold
    }

    [CreateAssetMenu(menuName = "MonsterWorldLike/Quests/Quest Definition", fileName = "QuestDefinition")]
    public class QuestDefinition : ScriptableObject
    {
        public string questId;
        public string title;
        public string description;
        public QuestObjectiveType objectiveType = QuestObjectiveType.HarvestPlant;
        public string targetId;
        public int targetAmount = 1;
        public int rewardGold = 50;
        public int rewardXp = 20;
        public bool isDaily;
        public string nextQuestId;
    }
}
