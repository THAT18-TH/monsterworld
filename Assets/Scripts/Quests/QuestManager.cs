using System;
using System.Collections.Generic;
using MonsterWorldLike.Core;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Progression;
using MonsterWorldLike.Areas;
using UnityEngine;

namespace MonsterWorldLike.Quests
{
    [Serializable]
    public class QuestRuntimeState
    {
        public string questId;
        public int progress;
        public bool completed;
        public bool claimed;
        public long dailyResetUnix;
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private List<QuestDefinition> questCatalog = new();
        [SerializeField] private List<string> starterQuestIds = new();
        [SerializeField, Min(1)] private int dailyQuestCount = 3;

        private readonly Dictionary<string, QuestDefinition> defs = new();
        private readonly List<QuestRuntimeState> active = new();

        public event Action OnQuestChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildLookup();
            EnsureStarterQuests();
            RefreshDailiesIfNeeded();
            BindEvents();
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            UnbindEvents();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<QuestRuntimeState> GetActiveQuests() => active;
        public QuestDefinition FindDefinition(string questId) => defs.TryGetValue(questId, out var def) ? def : null;

        public void AddProgress(QuestObjectiveType type, string targetId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            RefreshDailiesIfNeeded();
            var changed = false;
            for (var i = 0; i < active.Count; i++)
            {
                var state = active[i];
                if (!defs.TryGetValue(state.questId, out var def) || def == null || state.claimed)
                {
                    continue;
                }

                if (def.objectiveType != type)
                {
                    continue;
                }

                if (!IsTargetMatch(def, targetId))
                {
                    continue;
                }

                if (def.isDaily && !IsStateForToday(state))
                {
                    continue;
                }

                state.progress = Mathf.Min(def.targetAmount, state.progress + Mathf.Max(0, amount));
                state.completed = state.progress >= def.targetAmount;
                changed = true;
            }

            if (changed)
            {
                OnQuestChanged?.Invoke();
                SaveManager.MarkDirty();
            }
        }

        public bool Claim(string questId)
        {
            RefreshDailiesIfNeeded();
            var state = FindState(questId);
            if (state == null || !state.completed || state.claimed)
            {
                return false;
            }

            if (!defs.TryGetValue(state.questId, out var def) || def == null)
            {
                return false;
            }

            EconomyManager.Instance?.AddGold(def.rewardGold);
            ProgressionManager.Instance?.AddXp(def.rewardXp);
            state.claimed = true;

            if (!string.IsNullOrWhiteSpace(def.nextQuestId))
            {
                ActivateIfMissing(def.nextQuestId);
            }

            OnQuestChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public List<QuestRuntimeState> BuildSaveState()
        {
            var save = new List<QuestRuntimeState>(active.Count);
            foreach (var q in active)
            {
                save.Add(new QuestRuntimeState
                {
                    questId = q.questId,
                    progress = q.progress,
                    completed = q.completed,
                    claimed = q.claimed,
                    dailyResetUnix = q.dailyResetUnix
                });
            }

            return save;
        }

        public void ApplySaveState(List<QuestRuntimeState> saved)
        {
            RebuildLookup();
            active.Clear();

            if (saved != null)
            {
                foreach (var q in saved)
                {
                    if (q == null || string.IsNullOrWhiteSpace(q.questId) || !defs.ContainsKey(q.questId))
                    {
                        continue;
                    }

                    active.Add(new QuestRuntimeState
                    {
                        questId = q.questId,
                        progress = Mathf.Max(0, q.progress),
                        completed = q.completed,
                        claimed = q.claimed,
                        dailyResetUnix = q.dailyResetUnix
                    });
                }
            }

            EnsureStarterQuests();
            RefreshDailiesIfNeeded();

            OnQuestChanged?.Invoke();
        }

        private void EnsureStarterQuests()
        {
            foreach (var questId in starterQuestIds)
            {
                ActivateIfMissing(questId);
            }
        }

        private void ActivateIfMissing(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) || !defs.ContainsKey(questId))
            {
                return;
            }

            if (FindState(questId) != null)
            {
                return;
            }

            active.Add(new QuestRuntimeState
            {
                questId = questId,
                progress = 0,
                completed = false,
                claimed = false,
                dailyResetUnix = defs[questId].isDaily ? StartOfCurrentUtcDayUnix() : 0
            });
        }

        private QuestRuntimeState FindState(string questId)
        {
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i].questId == questId)
                {
                    return active[i];
                }
            }

            return null;
        }

        private void RebuildLookup()
        {
            defs.Clear();
            foreach (var q in questCatalog)
            {
                if (q == null || string.IsNullOrWhiteSpace(q.questId))
                {
                    continue;
                }

                defs[q.questId] = q;
            }
        }

        private static long StartOfCurrentUtcDayUnix()
        {
            var now = DateTimeOffset.UtcNow;
            var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            return start.ToUnixTimeSeconds();
        }

        private static bool IsSameUtcDay(long unixA, long unixB)
        {
            if (unixA <= 0 || unixB <= 0)
            {
                return false;
            }

            var a = DateTimeOffset.FromUnixTimeSeconds(unixA).UtcDateTime.Date;
            var b = DateTimeOffset.FromUnixTimeSeconds(unixB).UtcDateTime.Date;
            return a == b;
        }

        private bool IsStateForToday(QuestRuntimeState state)
        {
            return state != null && IsSameUtcDay(state.dailyResetUnix, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        private void RefreshDailiesIfNeeded()
        {
            var todayStart = StartOfCurrentUtcDayUnix();
            var changed = RemoveExpiredDailies(todayStart);
            changed |= EnsureDailyQuestSet(todayStart);
            if (changed)
            {
                OnQuestChanged?.Invoke();
                SaveManager.MarkDirty();
            }
        }

        private bool RemoveExpiredDailies(long todayStart)
        {
            var changed = false;
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var state = active[i];
                if (state == null || !defs.TryGetValue(state.questId, out var def) || def == null || !def.isDaily)
                {
                    continue;
                }

                if (!IsSameUtcDay(state.dailyResetUnix, todayStart))
                {
                    active.RemoveAt(i);
                    changed = true;
                }
            }

            return changed;
        }

        private bool EnsureDailyQuestSet(long todayStart)
        {
            var pool = new List<QuestDefinition>();
            foreach (var def in questCatalog)
            {
                if (def != null && def.isDaily && !string.IsNullOrWhiteSpace(def.questId))
                {
                    pool.Add(def);
                }
            }

            if (pool.Count == 0)
            {
                return false;
            }

            var activeDailyCount = 0;
            for (var i = 0; i < active.Count; i++)
            {
                var state = active[i];
                if (state != null && defs.TryGetValue(state.questId, out var def) && def != null && def.isDaily && IsSameUtcDay(state.dailyResetUnix, todayStart))
                {
                    activeDailyCount++;
                }
            }

            var needed = Mathf.Clamp(dailyQuestCount - activeDailyCount, 0, dailyQuestCount);
            if (needed <= 0)
            {
                return false;
            }

            var changed = false;
            var startIndex = Mathf.Abs((int)(todayStart % pool.Count));
            for (var i = 0; i < pool.Count && needed > 0; i++)
            {
                var candidate = pool[(startIndex + i) % pool.Count];
                if (FindState(candidate.questId) != null)
                {
                    continue;
                }

                active.Add(new QuestRuntimeState
                {
                    questId = candidate.questId,
                    progress = 0,
                    completed = false,
                    claimed = false,
                    dailyResetUnix = todayStart
                });
                needed--;
                changed = true;
            }

            return changed;
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

        private void OnSeedBought(string plantId, int amount) => AddProgress(QuestObjectiveType.BuySeed, plantId, amount);
        private void OnSeedPlanted(int plotId, string plantId) => AddProgress(QuestObjectiveType.PlantSeed, plantId, 1);
        private void OnPlotWatered(int plotId) => AddProgress(QuestObjectiveType.WaterPlot, string.Empty, 1);
        private void OnHarvested(int plotId, string plantId, int amount) => AddProgress(QuestObjectiveType.HarvestPlant, plantId, amount);
        private void OnMonsterBought(string monsterId, int amount) => AddProgress(QuestObjectiveType.BuyMonster, monsterId, amount);
        private void OnMonsterCollected(long gold)
        {
            var amount = (int)Mathf.Min(int.MaxValue, Mathf.Max(1f, gold));
            AddProgress(QuestObjectiveType.CollectMonsterGold, string.Empty, amount);
            AddProgress(QuestObjectiveType.EarnGold, string.Empty, amount);
        }
        private void OnBuildingPlaced(string buildingId) => AddProgress(QuestObjectiveType.BuildBuilding, buildingId, 1);
        private void OnDecorationPlaced(string decorationId) => AddProgress(QuestObjectiveType.PlaceDecoration, decorationId, 1);
        private void OnDailyRewardClaimed(int rewardGold, int streak) => AddProgress(QuestObjectiveType.ClaimDailyReward, string.Empty, 1);

        private static bool IsTargetMatch(QuestDefinition def, string targetId)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.targetId))
            {
                return true;
            }

            const string areaPrefix = "area:";
            if (def.targetId.StartsWith(areaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var requiredArea = def.targetId.Substring(areaPrefix.Length);
                var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
                return string.Equals(requiredArea, currentArea, StringComparison.OrdinalIgnoreCase);
            }

            return def.targetId == targetId;
        }
    }
}
