using MonsterWorldLike.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterWorldLike.UI
{
    public class QuestBoardPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text questTitleText;
        [SerializeField] private TMP_Text questProgressText;
        [SerializeField] private TMP_Text questRewardText;
        [SerializeField] private Button claimButton;

        private int selectedIndex;

        private void OnEnable()
        {
            QuestManager.InstanceReady += OnQuestReady;
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            QuestManager.InstanceReady -= OnQuestReady;
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestChanged -= Refresh;
            }
        }

        public void SelectQuest(int index)
        {
            selectedIndex = Mathf.Max(0, index);
            Refresh();
        }

        public void ClaimSelected()
        {
            var manager = QuestManager.Instance;
            if (manager == null)
            {
                return;
            }

            var quests = manager.GetActiveQuests();
            if (selectedIndex < 0 || selectedIndex >= quests.Count)
            {
                return;
            }

            manager.Claim(quests[selectedIndex].questId);
            Refresh();
        }

        public void Refresh()
        {
            var manager = QuestManager.Instance;
            if (manager == null)
            {
                questTitleText.text = "No quests";
                questProgressText.text = string.Empty;
                questRewardText.text = string.Empty;
                return;
            }

            var quests = manager.GetActiveQuests();
            if (quests.Count == 0)
            {
                questTitleText.text = "No quests";
                questProgressText.text = string.Empty;
                questRewardText.text = string.Empty;
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, quests.Count - 1);
            var q = quests[selectedIndex];
            var definition = manager.FindDefinition(q.questId);
            var title = definition != null && !string.IsNullOrWhiteSpace(definition.title) ? definition.title : q.questId;
            var description = definition != null ? definition.description : string.Empty;
            var targetAmount = definition != null ? Mathf.Max(1, definition.targetAmount) : 1;
            questTitleText.text = string.IsNullOrWhiteSpace(description) ? title : $"{title}\n{description}";
            questProgressText.text = q.claimed
                ? "Estado: Reclamada"
                : q.completed
                    ? "Estado: Completada (reclamar)"
                    : $"Progreso: {q.progress}/{targetAmount}";
            var rewardGold = definition != null ? definition.rewardGold : 0;
            var rewardXp = definition != null ? definition.rewardXp : 0;
            questRewardText.text = $"Recompensa: {rewardGold} oro, {rewardXp} XP";
            if (claimButton != null)
            {
                claimButton.interactable = q.completed && !q.claimed;
            }
        }

        private void Bind()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestChanged -= Refresh;
                QuestManager.Instance.OnQuestChanged += Refresh;
            }
        }

        private void OnQuestReady()
        {
            Bind();
            Refresh();
        }

        private void OnValidate()
        {
            if (claimButton == null || claimButton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning("QuestBoardPanel: claimButton sin binding.");
            }
        }
    }
}
