using System;
using System.Collections.Generic;
using MonsterWorldLike.Core;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Progression;
using UnityEngine;

namespace MonsterWorldLike.Monsters
{
    public class MonsterManager : MonoBehaviour
    {
        public static MonsterManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private List<MonsterDefinition> monsterCatalog = new();
        [SerializeField] private List<MonsterDefinition> starterMonsters = new();

        private readonly List<MonsterInstance> ownedMonsters = new();
        private readonly Dictionary<string, MonsterDefinition> definitionsById = new();

        public long TotalProducedGold { get; private set; }

        public event Action OnMonsterStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildLookup();
            EnsureStarterMonsters();
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<MonsterInstance> GetMonsters() => ownedMonsters;
        public IReadOnlyList<MonsterDefinition> GetCatalog() => monsterCatalog;

        public List<MonsterInstance> BuildSaveState()
        {
            var snapshot = new List<MonsterInstance>(ownedMonsters.Count);
            for (var i = 0; i < ownedMonsters.Count; i++)
            {
                var item = ownedMonsters[i];
                if (item == null || string.IsNullOrWhiteSpace(item.monsterId))
                {
                    continue;
                }

                snapshot.Add(new MonsterInstance(item.monsterId)
                {
                    assignedPlotId = item.assignedPlotId,
                    currentTask = item.currentTask,
                    energy = item.energy,
                    happiness = item.happiness,
                    level = item.level,
                    xp = item.xp,
                    lastFedUnix = item.lastFedUnix,
                    lastWorkUnix = item.lastWorkUnix
                });
            }

            return snapshot;
        }

        public void ApplySaveState(List<MonsterInstance> savedMonsters, long totalProducedGold)
        {
            RebuildLookup();
            ownedMonsters.Clear();

            if (savedMonsters != null)
            {
                foreach (var item in savedMonsters)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.monsterId))
                    {
                        continue;
                    }

                    if (!definitionsById.TryGetValue(item.monsterId, out var def) || def == null)
                    {
                        continue;
                    }

                    ownedMonsters.Add(new MonsterInstance(item.monsterId)
                    {
                        assignedPlotId = item.assignedPlotId,
                        currentTask = item.currentTask,
                        energy = Mathf.Clamp(item.energy, 0, def.maxEnergy),
                        happiness = Mathf.Clamp(item.happiness, 0, 100),
                        level = Mathf.Max(1, item.level),
                        xp = Mathf.Max(0, item.xp),
                        lastFedUnix = item.lastFedUnix,
                        lastWorkUnix = item.lastWorkUnix
                    });
                }
            }

            if (ownedMonsters.Count == 0)
            {
                EnsureStarterMonsters();
            }

            TotalProducedGold = Math.Max(0, totalProducedGold);
            OnMonsterStateChanged?.Invoke();
        }

        public bool BuyMonster(MonsterDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.monsterId))
            {
                return false;
            }

            if (!definitionsById.ContainsKey(definition.monsterId))
            {
                return false;
            }

            var progression = ProgressionManager.Instance;
            if (progression != null && progression.Level < definition.unlockLevel)
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.SpendGold(definition.purchaseGoldCost))
            {
                return false;
            }

            ownedMonsters.Add(new MonsterInstance(definition.monsterId));
            GameEvents.MonsterBought(definition.monsterId, 1);
            OnMonsterStateChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool AssignToPlot(MonsterInstance instance, int plotId, MonsterTask task)
        {
            if (instance == null || !ownedMonsters.Contains(instance))
            {
                return false;
            }

            instance.assignedPlotId = plotId;
            instance.currentTask = task;
            OnMonsterStateChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool FeedMonster(MonsterInstance instance)
        {
            if (instance == null || !ownedMonsters.Contains(instance))
            {
                return false;
            }

            if (!definitionsById.TryGetValue(instance.monsterId, out var definition))
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                return false;
            }

            var foodCost = Math.Max(0, definition.foodCost);
            if (economy.Food < foodCost)
            {
                return false;
            }

            economy.SetBalances(economy.Gold, economy.Gems, economy.Food - foodCost);
            instance.energy = definition.maxEnergy;
            instance.lastFedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            OnMonsterStateChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public long CollectAll()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var total = 0L;

            for (var i = 0; i < ownedMonsters.Count; i++)
            {
                var instance = ownedMonsters[i];
                if (instance == null || !definitionsById.TryGetValue(instance.monsterId, out var definition))
                {
                    continue;
                }

                var produced = CalculateOfflineProduction(instance, definition, now);
                if (produced <= 0)
                {
                    continue;
                }

                total += produced;
                instance.lastWorkUnix = now;
                instance.energy = Mathf.Max(0, instance.energy - Mathf.Clamp((int)(produced / 10), 1, 20));
                instance.xp += Mathf.Max(1, (int)(produced / 20));
                TryLevelUp(instance);
            }

            if (total > 0)
            {
                EconomyManager.Instance?.AddGold(total);
                TotalProducedGold += total;
                GameEvents.MonsterCollected(total);
                OnMonsterStateChanged?.Invoke();
                SaveManager.MarkDirty();
            }

            return total;
        }

        private static long CalculateOfflineProduction(MonsterInstance instance, MonsterDefinition definition, long now)
        {
            var elapsedSeconds = Math.Max(0L, now - instance.lastWorkUnix);
            if (elapsedSeconds <= 0)
            {
                return 0;
            }

            var elapsedHours = elapsedSeconds / 3600f;
            var roleMultiplier = definition.role switch
            {
                MonsterRole.Farmer => 1.25f,
                MonsterRole.Collector => 1.1f,
                MonsterRole.Explorer => 0.9f,
                MonsterRole.Builder => 0.8f,
                _ => 1f
            };

            var energyFactor = Mathf.Clamp01(instance.energy / (float)Mathf.Max(1, definition.maxEnergy));
            var levelFactor = 1f + ((Mathf.Max(1, instance.level) - 1) * 0.1f);
            var raw = definition.baseProductionPerHour * elapsedHours * roleMultiplier * Mathf.Max(0.25f, energyFactor) * levelFactor;
            return Math.Max(0L, (long)Mathf.Floor(raw));
        }

        private static void TryLevelUp(MonsterInstance instance)
        {
            var needed = instance.level * 100;
            while (instance.xp >= needed)
            {
                instance.xp -= needed;
                instance.level++;
                needed = instance.level * 100;
            }
        }

        private void EnsureStarterMonsters()
        {
            if (ownedMonsters.Count > 0)
            {
                return;
            }

            for (var i = 0; i < starterMonsters.Count; i++)
            {
                var definition = starterMonsters[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.monsterId))
                {
                    continue;
                }

                if (!definitionsById.ContainsKey(definition.monsterId))
                {
                    continue;
                }

                ownedMonsters.Add(new MonsterInstance(definition.monsterId));
            }

            if (ownedMonsters.Count > 0)
            {
                SaveManager.MarkDirty();
            }
        }

        private void RebuildLookup()
        {
            definitionsById.Clear();
            for (var i = 0; i < monsterCatalog.Count; i++)
            {
                var definition = monsterCatalog[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.monsterId))
                {
                    continue;
                }

                definitionsById[definition.monsterId] = definition;
            }
        }
    }
}
