using System;
using System.Collections.Generic;
using MonsterWorldLike.Areas;
using MonsterWorldLike.Core;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Progression;
using MonsterWorldLike.Quests;
using UnityEngine;

namespace MonsterWorldLike.Garden
{
    [Serializable]
    public class SeedInventoryEntry
    {
        public string plantId;
        public int amount;

        public SeedInventoryEntry(string id, int value)
        {
            plantId = id;
            amount = value;
        }
    }

    [Serializable]
    public class DecorationPlacement
    {
        public string decorationId;
        public int plotId;
        public string areaId = "main";
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
    }

    public class GardenManager : MonoBehaviour
    {
        public static GardenManager Instance { get; private set; }
        public static event Action InstanceReady;

        [Header("Garden")]
        [SerializeField, Min(1)] private int totalPlots = 6;
        [SerializeField, Min(1)] private int initiallyUnlockedPlots = 2;
        [SerializeField, Min(1)] private int unlockPlotCost = 200;
        [SerializeField, Min(1)] private int unlockPlotMinLevel = 1;
        [SerializeField, Min(1)] private int reviveFoodCost = 20;
        [SerializeField, Min(1)] private int witherGraceSeconds = 1800;
        [SerializeField] private GameObject plotVisualPrefab;
        [SerializeField] private Transform plotRoot;
        [SerializeField] private Vector3 plotStartPosition = Vector3.zero;
        [SerializeField, Min(1)] private int plotColumns = 3;
        [SerializeField, Min(1)] private int plotRows = 2;
        [SerializeField, Min(0.1f)] private float plotSpacingX = 1.1f;
        [SerializeField, Min(0.1f)] private float plotSpacingY = 0.8f;
        [SerializeField] private bool debugPlotVisualLogs;

        [Header("Plants")]
        [SerializeField] private List<PlantDefinition> plantCatalog = new();
        [SerializeField, Min(0)] private int starterSeedsPerPlant = 1;

        [Header("Starter Quests")]
        [SerializeField] private List<QuestState> starterQuests = new();

        private readonly List<PlotState> plots = new();
        private readonly List<GameObject> plotVisuals = new();
        private readonly Dictionary<int, PlotVisualController> plotVisualControllersById = new();
        private readonly Dictionary<string, int> seedInventory = new();
        private readonly List<DecorationPlacement> placedDecorations = new();
        private readonly List<QuestState> activeQuests = new();
        private readonly Dictionary<string, PlantDefinition> plantsById = new();

        public event Action OnGardenChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildPlantLookup();
            EnsureDefaultGarden();
            EnsureStarterQuests();
            InstanceReady?.Invoke();
        }

        private void Start()
        {
            RebuildPlotVisuals();
        }

        public void RebuildPlotVisuals()
        {
            for (var i = 0; i < plotVisuals.Count; i++)
            {
                if (plotVisuals[i] != null)
                {
                    Destroy(plotVisuals[i]);
                }
            }

            plotVisuals.Clear();
            plotVisualControllersById.Clear();

            if (plotVisualPrefab == null)
            {
                return;
            }

            var root = plotRoot != null ? plotRoot : transform;
            var count = totalPlots;
            var seenPlotIds = new HashSet<int>();
            for (var i = 0; i < count; i++)
            {
                var col = i % Mathf.Max(1, plotColumns);
                var row = i / Mathf.Max(1, plotColumns);
                if (row >= Mathf.Max(1, plotRows) && i == Mathf.Max(1, plotColumns) * Mathf.Max(1, plotRows))
                {
                    Debug.LogWarning("GardenManager: totalPlots excede plotColumns*plotRows; se continuará en filas adicionales.");
                }
                var pos = plotStartPosition + new Vector3(col * plotSpacingX, -row * plotSpacingY, 0f);
                var plotVisual = Instantiate(plotVisualPrefab, pos, Quaternion.identity, root);
                plotVisuals.Add(plotVisual);

                var plotId = i;
                if (!seenPlotIds.Add(plotId))
                {
                    Debug.LogWarning($"GardenManager.RebuildPlotVisuals duplicate plotId detectado: {plotId}");
                }

                var controller = plotVisual.GetComponent<PlotVisualController>();
                if (controller == null)
                {
                    controller = plotVisual.AddComponent<PlotVisualController>();
                }

                controller.Bind(this, plotId);
                plotVisualControllersById[plotId] = controller;
                if (debugPlotVisualLogs)
                {
                    Debug.Log($"[GardenVisual] Bind visual '{plotVisual.name}' -> plotId={plotId} row={row} col={col}");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<PlotState> GetPlots() => plots;
        public IReadOnlyList<PlantDefinition> GetPlantCatalog() => plantCatalog;

        public IReadOnlyList<QuestState> GetActiveQuests() => activeQuests;

        public IReadOnlyList<DecorationPlacement> GetPlacedDecorations() => placedDecorations;

        public bool TryGetPlot(int plotId, out PlotState plot)
        {
            plot = GetPlotById(plotId);
            return plot != null;
        }

        public bool IsPlantAllowedInCurrentArea(PlantDefinition plant)
        {
            if (plant == null)
            {
                return false;
            }

            var areaId = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
            return string.IsNullOrWhiteSpace(plant.allowedAreaId) || plant.allowedAreaId == areaId;
        }

        public bool PlaceDecoration(DecorationPlacement placement)
        {
            if (placement == null || string.IsNullOrWhiteSpace(placement.decorationId))
            {
                return false;
            }

            placedDecorations.Add(new DecorationPlacement
            {
                decorationId = placement.decorationId,
                plotId = placement.plotId,
                areaId = string.IsNullOrWhiteSpace(placement.areaId) ? "main" : placement.areaId,
                posX = placement.posX,
                posY = placement.posY,
                posZ = placement.posZ,
                rotY = placement.rotY
            });
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool MoveDecoration(int index, Vector3 position)
        {
            if (index < 0 || index >= placedDecorations.Count)
            {
                return false;
            }

            var item = placedDecorations[index];
            item.posX = position.x;
            item.posY = position.y;
            item.posZ = position.z;
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool RotateDecoration(int index, float yRotation)
        {
            if (index < 0 || index >= placedDecorations.Count)
            {
                return false;
            }

            placedDecorations[index].rotY = yRotation;
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool RemoveDecoration(int index)
        {
            if (index < 0 || index >= placedDecorations.Count)
            {
                return false;
            }

            placedDecorations.RemoveAt(index);
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public List<SeedInventoryEntry> BuildSeedInventoryState()
        {
            var result = new List<SeedInventoryEntry>(seedInventory.Count);
            foreach (var pair in seedInventory)
            {
                result.Add(new SeedInventoryEntry(pair.Key, pair.Value));
            }

            return result;
        }

        public int GetSeedCount(string plantId)
        {
            if (string.IsNullOrWhiteSpace(plantId))
            {
                return 0;
            }

            return seedInventory.TryGetValue(plantId, out var count) ? count : 0;
        }

        public bool BuySeed(PlantDefinition plant, int amount = 1)
        {
            if (plant == null || string.IsNullOrWhiteSpace(plant.plantId) || amount <= 0)
            {
                return false;
            }

            if (!plantsById.ContainsKey(plant.plantId))
            {
                Debug.LogWarning($"BuySeed rejected. Unknown plantId '{plant.plantId}'.");
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                return false;
            }

            var totalCost = (long)plant.seedCost * amount;
            if (!economy.SpendGold(totalCost))
            {
                return false;
            }

            seedInventory.TryGetValue(plant.plantId, out var current);
            seedInventory[plant.plantId] = current + amount;
            GameEvents.SeedBought(plant.plantId, amount);
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool PlantSeed(int plotId, PlantDefinition plant)
        {
            if (plant == null || string.IsNullOrWhiteSpace(plant.plantId))
            {
                return false;
            }

            if (!plantsById.ContainsKey(plant.plantId))
            {
                Debug.LogWarning($"PlantSeed rejected. Unknown plantId '{plant.plantId}'.");
                return false;
            }

            if (!IsPlantAllowedInCurrentArea(plant))
            {
                return false;
            }

            var plot = GetPlotById(plotId);
            if (plot == null || !plot.unlocked || !plot.IsEmpty)
            {
                return false;
            }

            if (!ConsumeSeed(plant.plantId, 1))
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            plot.plantedSeedId = plant.plantId;
            plot.plantedAtUnix = now;
            plot.readyAtUnix = now + plant.growSeconds;
            plot.needsWater = true;
            plot.withered = false;
            plot.harvestsRemaining = Mathf.Max(1, plant.harvestCount);

            GameEvents.SeedPlanted(plotId, plant.plantId);
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool Harvest(int plotId)
        {
            var plot = GetPlotById(plotId);
            if (plot == null || !plot.unlocked || string.IsNullOrWhiteSpace(plot.plantedSeedId))
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateWitheredState(plot, now);
            if (!plot.IsReadyToHarvest(now))
            {
                return false;
            }

            if (!plantsById.TryGetValue(plot.plantedSeedId, out var plant) || plant == null)
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                return false;
            }

            var areaSellMultiplier = AreaManager.Instance != null ? AreaManager.Instance.GetCurrentCropSellMultiplier() : 1f;
            var sellValue = Mathf.Max(1, Mathf.RoundToInt(plant.sellValue * areaSellMultiplier));
            economy.AddGold(sellValue);
            ProgressionManager.Instance?.AddXp(plant.xpReward);
            ApplyQuestProgress(plant.plantId, 1);
            plot.harvestsRemaining = Mathf.Max(0, plot.harvestsRemaining - 1);
            GameEvents.Harvested(plotId, plant.plantId, 1);
            if (plot.harvestsRemaining <= 0 || plant.regrowSeconds <= 0)
            {
                plot.ClearPlant();
            }
            else
            {
                plot.plantedAtUnix = now;
                plot.readyAtUnix = now + plant.regrowSeconds;
                plot.needsWater = true;
            }

            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool Water(int plotId)
        {
            var plot = GetPlotById(plotId);
            if (plot == null || !plot.unlocked || !plot.IsPlanted || !plot.needsWater)
            {
                return false;
            }

            plot.needsWater = false;
            GameEvents.PlotWatered(plotId);
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool RevivePlot(int plotId)
        {
            var plot = GetPlotById(plotId);
            if (plot == null || !plot.unlocked || !plot.withered)
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null || economy.Food < reviveFoodCost)
            {
                return false;
            }

            economy.SetBalances(economy.Gold, economy.Gems, economy.Food - reviveFoodCost);
            plot.withered = false;
            plot.needsWater = true;
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool UnlockPlot(int plotId)
        {
            var plot = GetPlotById(plotId);
            if (plot == null || plot.unlocked)
            {
                return false;
            }

            var progression = ProgressionManager.Instance;
            if (progression != null && progression.Level < unlockPlotMinLevel)
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.SpendGold(unlockPlotCost))
            {
                return false;
            }

            plot.unlocked = true;
            plot.ClearPlant();
            OnGardenChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public long SecondsUntilReady(int plotId)
        {
            var plot = GetPlotById(plotId);
            if (plot == null || !plot.IsPlanted)
            {
                return 0;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0L, plot.readyAtUnix - now);
        }

        public bool IsReadyToHarvest(int plotId)
        {
            var plot = GetPlotById(plotId);
            if (plot == null)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return plot.IsReadyToHarvest(now);
        }

        public bool TryGetPlantDefinition(string plantId, out PlantDefinition plant)
        {
            if (string.IsNullOrWhiteSpace(plantId))
            {
                plant = null;
                return false;
            }

            return plantsById.TryGetValue(plantId, out plant) && plant != null;
        }

        public void ApplySaveState(
            List<PlotState> savedPlots,
            List<SeedInventoryEntry> savedInventory,
            List<DecorationPlacement> savedDecorations,
            List<QuestState> savedQuests)
        {
            RebuildPlantLookup();
            plots.Clear();
            seedInventory.Clear();
            placedDecorations.Clear();
            activeQuests.Clear();
            EnsureDefaultGarden();

            if (savedPlots != null)
            {
                foreach (var item in savedPlots)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    var safePlot = GetPlotById(item.plotId);
                    if (safePlot == null)
                    {
                        continue;
                    }

                    safePlot.unlocked = item.unlocked;
                    safePlot.plantedSeedId = item.plantedSeedId;
                    safePlot.plantedAtUnix = item.plantedAtUnix;
                    safePlot.readyAtUnix = item.readyAtUnix;
                    safePlot.needsWater = item.needsWater;
                    safePlot.withered = item.withered;
                    safePlot.harvestsRemaining = Mathf.Max(0, item.harvestsRemaining);

                    if (safePlot.readyAtUnix < safePlot.plantedAtUnix)
                    {
                        safePlot.readyAtUnix = safePlot.plantedAtUnix;
                    }
                }
            }

            for (var i = 0; i < plots.Count; i++)
            {
                var safePlot = plots[i];
                if (!safePlot.unlocked)
                {
                    safePlot.ClearPlant();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(safePlot.plantedSeedId))
                {
                    safePlot.ClearPlant();
                    continue;
                }

                if (!plantsById.ContainsKey(safePlot.plantedSeedId))
                {
                    safePlot.ClearPlant();
                    continue;
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var maxFutureSeconds = 60L * 60L * 24L * 365L;
                if (safePlot.plantedAtUnix > now + maxFutureSeconds ||
                    safePlot.readyAtUnix > now + maxFutureSeconds)
                {
                    safePlot.ClearPlant();
                }

                if (safePlot.harvestsRemaining <= 0)
                {
                    safePlot.harvestsRemaining = 1;
                }
            }

            if (savedInventory != null)
            {
                foreach (var entry in savedInventory)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.plantId) || entry.amount <= 0)
                    {
                        continue;
                    }

                    if (!plantsById.ContainsKey(entry.plantId))
                    {
                        continue;
                    }

                    seedInventory.TryGetValue(entry.plantId, out var existingAmount);
                    seedInventory[entry.plantId] = existingAmount + entry.amount;
                }
            }

            if (savedDecorations != null)
            {
                var decorationKeys = new HashSet<string>();
                foreach (var decoration in savedDecorations)
                {
                    if (decoration == null || string.IsNullOrWhiteSpace(decoration.decorationId))
                    {
                        continue;
                    }

                    if (decoration.plotId >= 0 && GetPlotById(decoration.plotId) == null)
                    {
                        continue;
                    }

                    var areaId = string.IsNullOrWhiteSpace(decoration.areaId) ? "main" : decoration.areaId;
                    var dedupeKey = $"{decoration.decorationId}|{areaId}|{decoration.plotId}|{decoration.posX:0.###}|{decoration.posY:0.###}|{decoration.posZ:0.###}|{decoration.rotY:0.###}";
                    if (!decorationKeys.Add(dedupeKey))
                    {
                        continue;
                    }

                    placedDecorations.Add(new DecorationPlacement
                    {
                        decorationId = decoration.decorationId,
                        plotId = decoration.plotId,
                        areaId = areaId,
                        posX = decoration.posX,
                        posY = decoration.posY,
                        posZ = decoration.posZ,
                        rotY = decoration.rotY
                    });
                }
            }

            if (savedQuests != null)
            {
                var questIds = new HashSet<string>();
                foreach (var quest in savedQuests)
                {
                    if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
                    {
                        continue;
                    }

                    if (!questIds.Add(quest.questId))
                    {
                        continue;
                    }

                    activeQuests.Add(new QuestState
                    {
                        questId = quest.questId,
                        requiredPlantId = quest.requiredPlantId,
                        requiredAmount = Mathf.Max(1, quest.requiredAmount),
                        currentAmount = Mathf.Max(0, quest.currentAmount),
                        rewardGold = Mathf.Max(0, quest.rewardGold),
                        rewardXp = Mathf.Max(0, quest.rewardXp),
                        completed = quest.completed
                    });
                }
            }

            if (activeQuests.Count == 0)
            {
                EnsureStarterQuests();
            }

            RebuildPlotVisuals();
            OnGardenChanged?.Invoke();
        }

        public bool TryPerformPrimaryAction(int plotId, PlantDefinition defaultPlant = null)
        {
            var plot = GetPlotById(plotId);
            if (plot == null)
            {
                return false;
            }

            if (!plot.unlocked)
            {
                return UnlockPlot(plotId);
            }

            if (plot.withered)
            {
                return RevivePlot(plotId);
            }

            if (plot.needsWater)
            {
                return Water(plotId);
            }

            if (plot.IsEmpty)
            {
                return defaultPlant != null && PlantSeed(plotId, defaultPlant);
            }

            return Harvest(plotId);
        }

        private PlotState GetPlotById(int plotId)
        {
            for (var i = 0; i < plots.Count; i++)
            {
                if (plots[i].plotId == plotId)
                {
                    return plots[i];
                }
            }

            return null;
        }

        private bool ConsumeSeed(string plantId, int amount)
        {
            if (!seedInventory.TryGetValue(plantId, out var current) || current < amount)
            {
                return false;
            }

            current -= amount;
            if (current <= 0)
            {
                seedInventory.Remove(plantId);
            }
            else
            {
                seedInventory[plantId] = current;
            }

            return true;
        }

        private void ApplyQuestProgress(string harvestedPlantId, int amount)
        {
            for (var i = 0; i < activeQuests.Count; i++)
            {
                var quest = activeQuests[i];
                if (quest.completed || quest.requiredPlantId != harvestedPlantId)
                {
                    continue;
                }

                quest.currentAmount = Mathf.Min(quest.requiredAmount, quest.currentAmount + amount);
                if (quest.currentAmount >= quest.requiredAmount)
                {
                    quest.completed = true;
                    EconomyManager.Instance?.AddGold(quest.rewardGold);
                    ProgressionManager.Instance?.AddXp(quest.rewardXp);
                }
            }
        }

        private void UpdateWitheredState(PlotState plot, long now)
        {
            if (plot == null || plot.withered || !plot.IsPlanted)
            {
                return;
            }

            if (plot.needsWater && now > plot.readyAtUnix + witherGraceSeconds)
            {
                plot.withered = true;
            }
        }

        private void EnsureDefaultGarden()
        {
            plots.Clear();
            var unlockedCount = Mathf.Clamp(initiallyUnlockedPlots, 1, totalPlots);
            for (var i = 0; i < totalPlots; i++)
            {
                plots.Add(new PlotState(i, i < unlockedCount));
            }

            GrantStarterSeedsIfNeeded();
        }

        private void EnsureStarterQuests()
        {
            if (starterQuests != null && starterQuests.Count > 0)
            {
                foreach (var quest in starterQuests)
                {
                    if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
                    {
                        continue;
                    }

                    AddQuestIfMissing(new QuestState
                    {
                        questId = quest.questId,
                        requiredPlantId = quest.requiredPlantId,
                        requiredAmount = Mathf.Max(1, quest.requiredAmount),
                        currentAmount = 0,
                        rewardGold = Mathf.Max(0, quest.rewardGold),
                        rewardXp = Mathf.Max(0, quest.rewardXp),
                        completed = false
                    });
                }

                return;
            }

            if (plantCatalog == null || plantCatalog.Count == 0 || plantCatalog[0] == null)
            {
                return;
            }

            var firstPlant = plantCatalog[0];
            if (string.IsNullOrWhiteSpace(firstPlant.plantId))
            {
                return;
            }

            AddQuestIfMissing(new QuestState
            {
                questId = "starter_harvest_1",
                requiredPlantId = firstPlant.plantId,
                requiredAmount = 2,
                currentAmount = 0,
                rewardGold = 50,
                rewardXp = 20,
                completed = false
            });
        }

        private void RebuildPlantLookup()
        {
            plantsById.Clear();
            foreach (var plant in plantCatalog)
            {
                if (plant == null || string.IsNullOrWhiteSpace(plant.plantId))
                {
                    continue;
                }

                if (plantsById.ContainsKey(plant.plantId))
                {
                    Debug.LogWarning($"Duplicate plantId '{plant.plantId}' found in plantCatalog. Latest entry will be used.");
                }

                plantsById[plant.plantId] = plant;
            }
        }

        private void GrantStarterSeedsIfNeeded()
        {
            if (starterSeedsPerPlant <= 0 || seedInventory.Count > 0)
            {
                return;
            }

            foreach (var plant in plantCatalog)
            {
                if (plant == null || string.IsNullOrWhiteSpace(plant.plantId))
                {
                    continue;
                }

                seedInventory[plant.plantId] = starterSeedsPerPlant;
            }
        }

        private void AddQuestIfMissing(QuestState quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
            {
                return;
            }

            for (var i = 0; i < activeQuests.Count; i++)
            {
                if (activeQuests[i].questId == quest.questId)
                {
                    return;
                }
            }

            activeQuests.Add(quest);
        }
    }
}
