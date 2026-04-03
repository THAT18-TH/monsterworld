using MonsterWorldLike.Core;
using MonsterWorldLike.Garden;
using MonsterWorldLike.Monsters;
using UnityEngine;

namespace MonsterWorldLike.Debugging
{
    public class GardenLoopSmokeTest : MonoBehaviour
    {
        [SerializeField] private PlantDefinition plant;
        [SerializeField] private int plotId;

        [ContextMenu("Smoke/Buy Seed")]
        public void SmokeBuySeed()
        {
            if (!TryValidate(out var garden))
            {
                return;
            }

            var ok = garden.BuySeed(plant, 1);
            Debug.Log($"SmokeBuySeed => {ok}");
        }

        [ContextMenu("Smoke/Plant")]
        public void SmokePlant()
        {
            if (!TryValidate(out var garden))
            {
                return;
            }

            var ok = garden.PlantSeed(plotId, plant);
            Debug.Log($"SmokePlant => {ok}");
        }

        [ContextMenu("Smoke/Water")]
        public void SmokeWater()
        {
            var garden = GardenManager.Instance;
            if (garden == null)
            {
                Debug.LogWarning("SmokeWater failed: GardenManager no disponible.");
                return;
            }

            var ok = garden.Water(plotId);
            Debug.Log($"SmokeWater => {ok}");
        }

        [ContextMenu("Smoke/Harvest")]
        public void SmokeHarvest()
        {
            var garden = GardenManager.Instance;
            if (garden == null)
            {
                Debug.LogWarning("SmokeHarvest failed: GardenManager no disponible.");
                return;
            }

            var ok = garden.Harvest(plotId);
            Debug.Log($"SmokeHarvest => {ok}");
        }

        [ContextMenu("Smoke/Save")]
        public void SmokeSave()
        {
            var ok = SaveManager.TrySave(force: true);
            Debug.Log($"SmokeSave => {ok}");
        }

        [ContextMenu("Smoke/CollectAll Monsters")]
        public void SmokeCollectAllMonsters()
        {
            var manager = MonsterManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("SmokeCollectAllMonsters failed: MonsterManager no disponible.");
                return;
            }

            var collected = manager.CollectAll();
            Debug.Log($"SmokeCollectAllMonsters => +{collected} Gold");
        }

        [ContextMenu("Smoke/Reload")]
        public void SmokeReload()
        {
            var load = SaveManager.LoadDataFromDisk();
            var applied = SaveManager.TryApplyPendingLoadedData();
            Debug.Log($"SmokeReload => load:{load} apply:{applied}");
        }

        [ContextMenu("Smoke/SaveLoad Stress x20")]
        public void SmokeSaveLoadStress()
        {
            var saveOk = 0;
            var loadOk = 0;
            for (var i = 0; i < 20; i++)
            {
                if (SaveManager.TrySave(force: true))
                {
                    saveOk++;
                }

                var load = SaveManager.LoadDataFromDisk();
                var applied = SaveManager.TryApplyPendingLoadedData();
                if (load == LoadDataResult.Loaded && applied)
                {
                    loadOk++;
                }
            }

            Debug.Log($"SmokeSaveLoadStress => saves:{saveOk}/20 loads:{loadOk}/20");
        }

        private bool TryValidate(out GardenManager garden)
        {
            garden = GardenManager.Instance;
            if (garden == null)
            {
                Debug.LogWarning("Smoke test failed: GardenManager no disponible.");
                return false;
            }

            if (plant == null)
            {
                Debug.LogWarning("Smoke test failed: PlantDefinition no asignada.");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            if (plant == null)
            {
                Debug.LogWarning("GardenLoopSmokeTest: asigna PlantDefinition en inspector.");
            }
        }
    }
}
