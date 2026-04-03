using UnityEngine;

namespace MonsterWorldLike.Decorations
{
    [CreateAssetMenu(menuName = "MonsterWorldLike/Decorations/Decoration Definition", fileName = "DecorationDefinition")]
    public class DecorationDefinition : ScriptableObject
    {
        public string decorationId;
        public string displayName;
        public GameObject prefab;
        [Min(0)] public int goldCost = 25;
        [Min(0)] public int unlockLevel = 1;
    }
}
