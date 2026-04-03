using UnityEngine;

namespace MonsterWorldLike.Garden
{
    [CreateAssetMenu(menuName = "MonsterWorldLike/Garden/Plant Definition", fileName = "PlantDefinition")]
    public class PlantDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string plantId;
        public string displayName;
        public Sprite icon;

        [Header("Economy")]
        [Min(1)] public int seedCost = 10;
        [Min(1)] public int sellValue = 15;

        [Header("Growth")]
        [Min(1)] public int growSeconds = 20;
        [Min(1)] public int xpReward = 5;
    }
}
