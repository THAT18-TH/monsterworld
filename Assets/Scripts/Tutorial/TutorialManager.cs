using System.Collections.Generic;
using UnityEngine;
using MonsterWorldLike.Core;
using MonsterWorldLike.Quests;

namespace MonsterWorldLike.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private List<TutorialStepDefinition> tutorialSteps = new();
        [SerializeField] private bool allowSkip = true;

        public int CurrentStep { get; private set; } = -1;
        public TutorialStatus Status { get; private set; } = TutorialStatus.NotStarted;

        public event System.Action<int, string> OnStepChanged;
        public event System.Action<TutorialStatus> OnStatusChanged;

        private readonly Dictionary<string, int> progressByStepId = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BindEvents();
            StartTutorialIfNeeded();
        }

        private void OnDestroy()
        {
            UnbindEvents();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public string CurrentInstruction
        {
            get
            {
                var step = GetCurrentStep();
                return step != null ? step.instruction : "Tutorial completo";
            }
        }

        public void StartTutorialIfNeeded()
        {
            if (Status != TutorialStatus.NotStarted || tutorialSteps.Count == 0)
            {
                EmitStep();
                return;
            }

            Status = TutorialStatus.Active;
            CurrentStep = 0;
            OnStatusChanged?.Invoke(Status);
            EmitStep();
        }

        public void Advance()
        {
            if (Status != TutorialStatus.Active)
            {
                return;
            }

            if (CurrentStep >= tutorialSteps.Count - 1)
            {
                CompleteTutorial();
                return;
            }

            CurrentStep++;
            EmitStep();
            SaveManager.MarkDirty();
        }

        public void Skip()
        {
            if (!allowSkip || Status == TutorialStatus.Completed || Status == TutorialStatus.Skipped)
            {
                return;
            }

            Status = TutorialStatus.Skipped;
            CurrentStep = tutorialSteps.Count - 1;
            OnStatusChanged?.Invoke(Status);
            EmitStep();
            SaveManager.MarkDirty();
        }

        public void SetStateFromSave(int currentStep, TutorialStatus status)
        {
            Status = status;
            if (Status == TutorialStatus.NotStarted)
            {
                StartTutorialIfNeeded();
                return;
            }

            CurrentStep = Mathf.Clamp(currentStep, 0, Mathf.Max(0, tutorialSteps.Count - 1));
            OnStatusChanged?.Invoke(Status);
            EmitStep();
        }

        private void EmitStep()
        {
            var text = CurrentStep >= 0 && CurrentStep < tutorialSteps.Count && tutorialSteps[CurrentStep] != null
                ? tutorialSteps[CurrentStep].instruction
                : "Tutorial completo";

            OnStepChanged?.Invoke(CurrentStep, text);
        }

        private TutorialStepDefinition GetCurrentStep()
        {
            return CurrentStep >= 0 && CurrentStep < tutorialSteps.Count ? tutorialSteps[CurrentStep] : null;
        }

        private void BindEvents()
        {
            GameEvents.OnSeedBought += OnSeedBought;
            GameEvents.OnSeedPlanted += OnSeedPlanted;
            GameEvents.OnPlotWatered += OnPlotWatered;
            GameEvents.OnHarvested += OnHarvested;
            GameEvents.OnMonsterBought += OnMonsterBought;
            GameEvents.OnMonsterCollected += OnMonsterCollected;
            GameEvents.OnBuildingPlaced += OnBuildingPlaced;
            GameEvents.OnDecorationPlaced += OnDecorationPlaced;
            GameEvents.OnDailyRewardClaimed += OnDailyRewardClaimed;
        }

        private void UnbindEvents()
        {
            GameEvents.OnSeedBought -= OnSeedBought;
            GameEvents.OnSeedPlanted -= OnSeedPlanted;
            GameEvents.OnPlotWatered -= OnPlotWatered;
            GameEvents.OnHarvested -= OnHarvested;
            GameEvents.OnMonsterBought -= OnMonsterBought;
            GameEvents.OnMonsterCollected -= OnMonsterCollected;
            GameEvents.OnBuildingPlaced -= OnBuildingPlaced;
            GameEvents.OnDecorationPlaced -= OnDecorationPlaced;
            GameEvents.OnDailyRewardClaimed -= OnDailyRewardClaimed;
        }

        private void OnSeedBought(string plantId, int amount) => TryProgress(QuestObjectiveType.BuySeed, plantId, amount);
        private void OnSeedPlanted(int plotId, string plantId) => TryProgress(QuestObjectiveType.PlantSeed, plantId, 1);
        private void OnPlotWatered(int plotId) => TryProgress(QuestObjectiveType.WaterPlot, string.Empty, 1);
        private void OnHarvested(int plotId, string plantId, int amount) => TryProgress(QuestObjectiveType.HarvestPlant, plantId, amount);
        private void OnMonsterBought(string monsterId, int amount) => TryProgress(QuestObjectiveType.BuyMonster, monsterId, amount);
        private void OnMonsterCollected(long gold) => TryProgress(QuestObjectiveType.CollectMonsterGold, string.Empty, (int)Mathf.Min(int.MaxValue, Mathf.Max(1f, gold)));
        private void OnBuildingPlaced(string buildingId) => TryProgress(QuestObjectiveType.BuildBuilding, buildingId, 1);
        private void OnDecorationPlaced(string decorationId) => TryProgress(QuestObjectiveType.PlaceDecoration, decorationId, 1);
        private void OnDailyRewardClaimed(int reward, int streak) => TryProgress(QuestObjectiveType.ClaimDailyReward, string.Empty, 1);

        private void TryProgress(QuestObjectiveType type, string targetId, int amount)
        {
            if (Status != TutorialStatus.Active || amount <= 0)
            {
                return;
            }

            for (var i = CurrentStep; i < tutorialSteps.Count; i++)
            {
                var step = tutorialSteps[i];
                if (step == null || step.objectiveType != type)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(step.targetId) && step.targetId != targetId)
                {
                    continue;
                }

                var stepKey = GetStepKey(step, i);
                progressByStepId.TryGetValue(stepKey, out var current);
                current += amount;
                progressByStepId[stepKey] = current;
                if (current >= Mathf.Max(1, step.targetAmount))
                {
                    if (i == CurrentStep)
                    {
                        Advance();
                    }
                }

                SaveManager.MarkDirty();
                break;
            }
        }

        private void CompleteTutorial()
        {
            Status = TutorialStatus.Completed;
            CurrentStep = tutorialSteps.Count - 1;
            OnStatusChanged?.Invoke(Status);
            EmitStep();
            SaveManager.MarkDirty();
        }

        private static string GetStepKey(TutorialStepDefinition step, int index)
        {
            return !string.IsNullOrWhiteSpace(step.stepId) ? step.stepId : $"step_{index}";
        }
    }
}
