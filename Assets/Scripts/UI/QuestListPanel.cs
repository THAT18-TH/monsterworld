using MonsterWorldLike.Garden;
using MonsterWorldLike.Quests;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class QuestListPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text quest1Text;
        [SerializeField] private TMP_Text quest2Text;
        private bool hasLoggedMissingManager;

        private void OnEnable()
        {
            GardenManager.InstanceReady += OnGardenManagerReady;

            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            GardenManager.InstanceReady -= OnGardenManagerReady;

            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            var manager = GardenManager.Instance;
            if (manager == null)
            {
                if (!hasLoggedMissingManager)
                {
                    Debug.LogWarning("QuestListPanel: GardenManager no disponible.");
                    hasLoggedMissingManager = true;
                }

                SetQuestText(quest1Text, null, 1);
                SetQuestText(quest2Text, null, 2);
                return;
            }

            hasLoggedMissingManager = false;

            var quests = manager.GetActiveQuests();
            SetQuestText(quest1Text, quests.Count > 0 ? quests[0] : null, 1);
            SetQuestText(quest2Text, quests.Count > 1 ? quests[1] : null, 2);
        }

        private static void SetQuestText(TMP_Text target, QuestState quest, int slot)
        {
            if (target == null)
            {
                return;
            }

            if (quest == null)
            {
                target.text = $"Quest {slot}: --";
                return;
            }

            var status = quest.completed
                ? "Completada"
                : $"{quest.currentAmount}/{quest.requiredAmount}";

            target.text = $"{quest.questId} ({quest.requiredPlantId}) {status}";
        }

        private void OnGardenManagerReady()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged -= Refresh;
                GardenManager.Instance.OnGardenChanged += Refresh;
            }

            Refresh();
        }

        private void OnValidate()
        {
            if (quest1Text == null || quest2Text == null)
            {
                Debug.LogWarning("QuestListPanel: faltan TMP_Text en inspector.");
            }
        }
    }
}
