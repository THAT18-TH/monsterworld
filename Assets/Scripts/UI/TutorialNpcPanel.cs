using MonsterWorldLike.NPC;
using MonsterWorldLike.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterWorldLike.UI
{
    public class TutorialNpcPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text tutorialStepText;
        [SerializeField] private TMP_Text npcDialogueText;
        [SerializeField] private Button skipButton;

        private void OnEnable()
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnStepChanged += OnStepChanged;
                TutorialManager.Instance.OnStatusChanged += OnStatusChanged;
                OnStepChanged(TutorialManager.Instance.CurrentStep, string.Empty);
                OnStatusChanged(TutorialManager.Instance.Status);
            }

            if (NpcManager.Instance != null)
            {
                NpcManager.Instance.OnDialogueLine += OnDialogue;
            }
        }

        private void OnDisable()
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnStepChanged -= OnStepChanged;
                TutorialManager.Instance.OnStatusChanged -= OnStatusChanged;
            }

            if (NpcManager.Instance != null)
            {
                NpcManager.Instance.OnDialogueLine -= OnDialogue;
            }
        }

        public void OnAdvanceTutorialPressed()
        {
            TutorialManager.Instance?.Advance();
        }

        public void OnStartNpcDialogue(string npcId)
        {
            NpcManager.Instance?.StartDialogue(npcId);
        }

        public void OnSkipTutorialPressed()
        {
            TutorialManager.Instance?.Skip();
        }

        private void OnStepChanged(int step, string text)
        {
            if (tutorialStepText != null)
            {
                tutorialStepText.text = $"Step {step + 1}: {text}";
            }
        }

        private void OnStatusChanged(TutorialStatus status)
        {
            if (skipButton != null)
            {
                skipButton.interactable = status == TutorialStatus.Active;
            }
        }

        private void OnDialogue(string npcName, string line)
        {
            if (npcDialogueText != null)
            {
                npcDialogueText.text = $"{npcName}: {line}";
            }
        }
    }
}
