using UnityEngine;

namespace MonsterWorldLike.Buildings
{
    [CreateAssetMenu(menuName = "MonsterWorldLike/Buildings/Building Definition", fileName = "BuildingDefinition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string buildingId;
        public string displayName;
        public Sprite icon;
        [Min(0)] public int goldCost = 100;
        [Min(0)] public int gemsCost;
        [Min(1)] public int unlockLevel = 1;
        [Min(0)] public int buildSeconds;
    }
}
