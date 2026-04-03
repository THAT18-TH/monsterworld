using System;
using System.Collections.Generic;
using System.IO;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Areas;
using MonsterWorldLike.Buildings;
using MonsterWorldLike.Garden;
using MonsterWorldLike.Inventory;
using MonsterWorldLike.Monsters;
using MonsterWorldLike.Progression;
using MonsterWorldLike.Quests;
using MonsterWorldLike.Rewards;
using MonsterWorldLike.Tutorial;
using UnityEngine;

namespace MonsterWorldLike.Core
{
    public enum LoadDataResult
    {
        NoSaveFound,
        Loaded,
        LoadFailed
    }

    [Serializable]
    public class SaveData
    {
        public long gold;
        public long gems;
        public long food;
        public int level;
        public int xp;
        public List<MonsterInstance> monsters;
        public long totalProducedGold;
        public List<InventoryEntry> inventoryItems;
        public List<BuildingInstance> buildings;
        public List<AreaState> areas;
        public string currentAreaId;
        public long dailyRewardLastClaimUnix;
        public int dailyRewardStreak;
        public List<QuestRuntimeState> questRuntimeStates;
        public int tutorialStep;
        public int tutorialStatus;
        public List<PlotState> plots;
        public List<SeedInventoryEntry> seedInventory;
        public List<DecorationPlacement> placedDecorations;
        public List<QuestState> activeQuests;
        public string lastSaveUtc;
    }

    public static class SaveManager
    {
        private const string FileName = "savegame.json";
        private const float MinSaveIntervalSeconds = 0.75f;

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData LastLoadedData { get; private set; }
        public static bool IsDirty { get; private set; }
        public static bool HasUnsavedChanges => IsDirty;
        public static bool HasLoadedData => LastLoadedData != null;
        public static bool HasPendingLoadedData { get; private set; }

        private static bool isApplyingData;
        private static bool isSaving;
        private static float lastSaveRealtime;

        public static bool TrySave(bool force = false)
        {
            if (isSaving)
            {
                return false;
            }

            if (!force && !IsDirty)
            {
                return false;
            }

            var now = Time.realtimeSinceStartup;
            if (!force && now - lastSaveRealtime < MinSaveIntervalSeconds)
            {
                return false;
            }

            var data = BuildSaveDataFromScene();
            if (data == null)
            {
                return false;
            }

            isSaving = true;
            try
            {
                WriteDataToDisk(data);
                lastSaveRealtime = now;
                IsDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TrySave failed: {ex.Message}");
                return false;
            }
            finally
            {
                isSaving = false;
            }
        }

        public static void MarkDirty()
        {
            if (isApplyingData)
            {
                return;
            }

            IsDirty = true;
        }

        public static LoadDataResult LoadDataFromDisk()
        {
            if (!File.Exists(FilePath))
            {
                LastLoadedData = null;
                HasPendingLoadedData = false;
                return LoadDataResult.NoSaveFound;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    LastLoadedData = null;
                    HasPendingLoadedData = false;
                    return LoadDataResult.LoadFailed;
                }

                LastLoadedData = data;
                HasPendingLoadedData = true;
                return LoadDataResult.Loaded;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LoadDataFromDisk failed: {ex.Message}");
                LastLoadedData = null;
                HasPendingLoadedData = false;
                return LoadDataResult.LoadFailed;
            }
        }

        public static bool CanApplyLoadedDataToScene()
        {
            if (!HasLoadedData)
            {
                return false;
            }

            return EconomyManager.Instance != null &&
                   GardenManager.Instance != null &&
                   ProgressionManager.Instance != null &&
                   MonsterManager.Instance != null;
        }

        public static bool ApplyLoadedDataToScene()
        {
            if (!CanApplyLoadedDataToScene())
            {
                return false;
            }

            var applied = ApplyDataToScene(LastLoadedData);
            if (applied)
            {
                HasPendingLoadedData = false;
            }

            return applied;
        }

        public static bool TryApplyPendingLoadedData()
        {
            if (!HasPendingLoadedData)
            {
                return false;
            }

            return ApplyLoadedDataToScene();
        }

        private static SaveData BuildSaveDataFromScene()
        {
            var economy = EconomyManager.Instance;
            var garden = GardenManager.Instance;
            var progression = ProgressionManager.Instance;
            var monsters = MonsterManager.Instance;
            var inventory = InventoryManager.Instance;
            var buildings = BuildingManager.Instance;
            var areas = AreaManager.Instance;
            var daily = DailyRewardManager.Instance;
            var questManager = QuestManager.Instance;
            var tutorial = TutorialManager.Instance;

            if (economy == null || garden == null || progression == null || monsters == null)
            {
                return null;
            }

            return new SaveData
            {
                gold = economy.Gold,
                gems = economy.Gems,
                food = economy.Food,
                level = progression.Level,
                xp = progression.Xp,
                monsters = monsters.BuildSaveState(),
                totalProducedGold = monsters.TotalProducedGold,
                inventoryItems = inventory != null ? inventory.BuildSaveState() : null,
                buildings = buildings != null ? buildings.BuildSaveState() : null,
                areas = areas != null ? areas.BuildSaveState() : null,
                currentAreaId = areas != null ? areas.CurrentAreaId : "main",
                dailyRewardLastClaimUnix = daily != null ? daily.LastClaimUnix : 0,
                dailyRewardStreak = daily != null ? daily.Streak : 0,
                questRuntimeStates = questManager != null ? questManager.BuildSaveState() : null,
                tutorialStep = tutorial != null ? tutorial.CurrentStep : 0,
                tutorialStatus = tutorial != null ? (int)tutorial.Status : 0,
                plots = new List<PlotState>(garden.GetPlots()),
                seedInventory = garden.BuildSeedInventoryState(),
                placedDecorations = new List<DecorationPlacement>(garden.GetPlacedDecorations()),
                activeQuests = new List<QuestState>(garden.GetActiveQuests()),
                lastSaveUtc = DateTime.UtcNow.ToString("o")
            };
        }

        private static bool ApplyDataToScene(SaveData data)
        {
            var economy = EconomyManager.Instance;
            var garden = GardenManager.Instance;
            var progression = ProgressionManager.Instance;
            var monsters = MonsterManager.Instance;
            var inventory = InventoryManager.Instance;
            var buildings = BuildingManager.Instance;
            var areas = AreaManager.Instance;
            var daily = DailyRewardManager.Instance;
            var questManager = QuestManager.Instance;
            var tutorial = TutorialManager.Instance;

            if (economy == null || garden == null || progression == null || monsters == null)
            {
                return false;
            }

            isApplyingData = true;
            try
            {
                economy.SetBalances(data.gold, data.gems, data.food);
                progression.SetProgress(data.level, data.xp);
                monsters.ApplySaveState(data.monsters, data.totalProducedGold);
                garden.ApplySaveState(data.plots, data.seedInventory, data.placedDecorations, data.activeQuests);
                inventory?.ApplySaveState(data.inventoryItems);
                buildings?.ApplySaveState(data.buildings);
                areas?.ApplySaveState(data.areas, data.currentAreaId);
                daily?.ApplySaveState(data.dailyRewardLastClaimUnix, data.dailyRewardStreak);
                questManager?.ApplySaveState(data.questRuntimeStates);
                if (tutorial != null)
                {
                    tutorial.SetStateFromSave(data.tutorialStep, (TutorialStatus)Mathf.Clamp(data.tutorialStatus, 0, 3));
                }
                IsDirty = false;
                return true;
            }
            finally
            {
                isApplyingData = false;
            }
        }

        private static void WriteDataToDisk(SaveData data)
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }
    }
}
