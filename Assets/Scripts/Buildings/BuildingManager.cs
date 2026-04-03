using System;
using System.Collections.Generic;
using MonsterWorldLike.Core;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Progression;
using UnityEngine;

namespace MonsterWorldLike.Buildings
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private List<BuildingDefinition> buildingCatalog = new();

        private readonly List<BuildingInstance> buildings = new();
        private readonly Dictionary<string, BuildingDefinition> defs = new();

        public event Action OnBuildingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildLookup();
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<BuildingDefinition> GetCatalog() => buildingCatalog;
        public IReadOnlyList<BuildingInstance> GetBuildings() => buildings;

        public bool BuyAndPlace(BuildingDefinition definition, Vector3 worldPosition, string areaId = "main")
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.buildingId))
            {
                return false;
            }

            if (!defs.ContainsKey(definition.buildingId))
            {
                return false;
            }

            var progression = ProgressionManager.Instance;
            if (progression != null && progression.Level < definition.unlockLevel)
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.SpendGold(definition.goldCost))
            {
                return false;
            }

            if (definition.gemsCost > 0 && economy.Gems < definition.gemsCost)
            {
                return false;
            }

            if (definition.gemsCost > 0)
            {
                economy.SetBalances(economy.Gold, economy.Gems - definition.gemsCost, economy.Food);
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            buildings.Add(new BuildingInstance
            {
                buildingId = definition.buildingId,
                areaId = string.IsNullOrWhiteSpace(areaId) ? "main" : areaId,
                posX = worldPosition.x,
                posY = worldPosition.y,
                posZ = worldPosition.z,
                rotY = 0f,
                placedUnix = now,
                readyUnix = now + definition.buildSeconds,
                completed = definition.buildSeconds <= 0
            });

            GameEvents.BuildingPlaced(definition.buildingId);
            OnBuildingsChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public void TickCompletion()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var changed = false;
            for (var i = 0; i < buildings.Count; i++)
            {
                var building = buildings[i];
                if (!building.completed && now >= building.readyUnix)
                {
                    building.completed = true;
                    changed = true;
                }
            }

            if (changed)
            {
                OnBuildingsChanged?.Invoke();
                SaveManager.MarkDirty();
            }
        }

        public List<BuildingInstance> BuildSaveState()
        {
            var save = new List<BuildingInstance>(buildings.Count);
            foreach (var b in buildings)
            {
                save.Add(new BuildingInstance
                {
                    buildingId = b.buildingId,
                    areaId = b.areaId,
                    posX = b.posX,
                    posY = b.posY,
                    posZ = b.posZ,
                    rotY = b.rotY,
                    placedUnix = b.placedUnix,
                    readyUnix = b.readyUnix,
                    completed = b.completed
                });
            }

            return save;
        }

        public void ApplySaveState(List<BuildingInstance> saved)
        {
            RebuildLookup();
            buildings.Clear();

            if (saved != null)
            {
                foreach (var b in saved)
                {
                    if (b == null || string.IsNullOrWhiteSpace(b.buildingId) || !defs.ContainsKey(b.buildingId))
                    {
                        continue;
                    }

                    buildings.Add(new BuildingInstance
                    {
                        buildingId = b.buildingId,
                        areaId = string.IsNullOrWhiteSpace(b.areaId) ? "main" : b.areaId,
                        posX = b.posX,
                        posY = b.posY,
                        posZ = b.posZ,
                        rotY = b.rotY,
                        placedUnix = b.placedUnix,
                        readyUnix = b.readyUnix,
                        completed = b.completed
                    });
                }
            }

            OnBuildingsChanged?.Invoke();
        }

        private void RebuildLookup()
        {
            defs.Clear();
            foreach (var item in buildingCatalog)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.buildingId))
                {
                    continue;
                }

                defs[item.buildingId] = item;
            }
        }
    }
}
