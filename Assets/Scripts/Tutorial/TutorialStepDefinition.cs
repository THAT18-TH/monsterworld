using MonsterWorldLike.Quests;
using UnityEngine;

namespace MonsterWorldLike.Tutorial
{
    [CreateAssetMenu(menuName = "MonsterWorldLike/Tutorial/Step Definition", fileName = "TutorialStepDefinition")]
    public class TutorialStepDefinition : ScriptableObject
    {
        public string stepId;
        public string title;
        [TextArea] public string instruction;
        public QuestObjectiveType objectiveType;
        public string targetId;
        [Min(1)] public int targetAmount = 1;
    }
}
