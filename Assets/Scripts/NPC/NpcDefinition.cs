using UnityEngine;

namespace MonsterWorldLike.NPC
{
    [CreateAssetMenu(menuName = "MonsterWorldLike/NPC/Npc Definition", fileName = "NpcDefinition")]
    public class NpcDefinition : ScriptableObject
    {
        public string npcId;
        public string displayName;
        [TextArea] public string[] dialogueLines;
    }
}
