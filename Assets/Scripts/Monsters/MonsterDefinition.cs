using UnityEngine;

namespace MonsterWorldLike.Monsters
{
    public enum MonsterRole
    {
        Farmer,
        Collector,
        Explorer,
        Builder
    }

    [CreateAssetMenu(menuName = "MonsterWorldLike/Monsters/Monster Definition", fileName = "MonsterDefinition")]
    public class MonsterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string monsterId;
        public string displayName;
        public Sprite icon;
        public MonsterRole role = MonsterRole.Farmer;

        [Header("Progression")]
        [Min(1)] public int unlockLevel = 1;
        [Min(0)] public int purchaseGoldCost = 150;

        [Header("Needs")]
        [Min(0)] public int foodCost = 5;
        [Min(1)] public int maxEnergy = 100;

        [Header("Production")]
        [Min(0f)] public float baseProductionPerHour = 40f;
    }
}
