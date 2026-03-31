using System.Collections.Generic;
using MonsterWorldLike.Garden;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class SeedShopPanel : MonoBehaviour
    {
        [SerializeField] private int selectedPlantIndex;
        [SerializeField] private TMP_Text selectedPlantNameText;
        [SerializeField] private TMP_Text selectedPlantCostText;
        [SerializeField] private TMP_Text selectedPlantOwnedSeedsText;

        private void OnEnable()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged -= Refresh;
            }
        }

        public void SelectPlantByIndex(int index)
        {
            selectedPlantIndex = Mathf.Max(0, index);
            Refresh();
        }

        public void BuySelectedSeed()
        {
            var manager = GardenManager.Instance;
            var plant = GetSelectedPlant(manager);
            if (manager == null || plant == null)
            {
                return;
            }

            manager.BuySeed(plant, 1);
            Refresh();
        }

        public void Refresh()
        {
            var manager = GardenManager.Instance;
            var plant = GetSelectedPlant(manager);
            if (plant == null || manager == null)
            {
                if (selectedPlantNameText != null) selectedPlantNameText.text = "Sin planta";
                if (selectedPlantCostText != null) selectedPlantCostText.text = "Costo: --";
                if (selectedPlantOwnedSeedsText != null) selectedPlantOwnedSeedsText.text = "Semillas: 0";
                return;
            }

            if (selectedPlantNameText != null) selectedPlantNameText.text = plant.displayName;
            if (selectedPlantCostText != null) selectedPlantCostText.text = $"Costo: {plant.seedCost} Gold";
            if (selectedPlantOwnedSeedsText != null) selectedPlantOwnedSeedsText.text = $"Semillas: {manager.GetSeedCount(plant.plantId)}";
        }

        private PlantDefinition GetSelectedPlant(GardenManager manager)
        {
            if (manager == null)
            {
                return null;
            }

            IReadOnlyList<PlantDefinition> catalog = manager.GetPlantCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                return null;
            }

            selectedPlantIndex = Mathf.Clamp(selectedPlantIndex, 0, catalog.Count - 1);
            return catalog[selectedPlantIndex];
        }
    }
}
