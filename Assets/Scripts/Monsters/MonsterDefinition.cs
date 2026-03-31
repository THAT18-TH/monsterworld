using UnityEngine;

namespace MonsterWorldLike.Monsters
{
    [CreateAssetMenu(menuName = "MonsterWorldLike/Monster Definition", fileName = "MonsterDefinition")]
    public class MonsterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string monsterId;
        public string displayName;
        public Sprite icon;

        [Header("Economy")]
        [Min(1)] public int purchaseCost = 250;
        [Min(1)] public int productionPerCycle = 40;
        [Min(2)] public int cycleSeconds = 10;
    }
}
