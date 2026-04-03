using System.Collections.Generic;
using MonsterWorldLike.Areas;
using MonsterWorldLike.Garden;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterWorldLike.UI
{
    public class SeedShopPanel : MonoBehaviour
    {
        [SerializeField] private int selectedPlantIndex;
        [SerializeField] private TMP_Text selectedPlantNameText;
        [SerializeField] private TMP_Text selectedPlantCostText;
        [SerializeField] private TMP_Text selectedPlantOwnedSeedsText;
        [SerializeField] private Button buyButton;
        [SerializeField] private ToastNotifier toastNotifier;

        private bool hasLoggedMissingManager;

        private void OnEnable()
        {
            GardenManager.InstanceReady += OnGardenManagerReady;

            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            GardenManager.InstanceReady -= OnGardenManagerReady;

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
                ShowToast("No hay semillas disponibles.");
                return;
            }

            if (!manager.IsPlantAllowedInCurrentArea(plant))
            {
                var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
                ShowToast($"No se puede plantar en {currentArea}. Requiere {plant.allowedAreaId}.");
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
                if (!hasLoggedMissingManager)
                {
                    Debug.LogWarning("SeedShopPanel: GardenManager no disponible o catálogo vacío.");
                    hasLoggedMissingManager = true;
                }

                if (selectedPlantNameText != null) selectedPlantNameText.text = "Sin planta";
                if (selectedPlantCostText != null) selectedPlantCostText.text = "Costo: --";
                if (selectedPlantOwnedSeedsText != null) selectedPlantOwnedSeedsText.text = "Semillas: 0";
                return;
            }

            hasLoggedMissingManager = false;

            var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
            var allowed = string.IsNullOrWhiteSpace(plant.allowedAreaId) || plant.allowedAreaId == currentArea;
            if (selectedPlantNameText != null) selectedPlantNameText.text = plant.displayName;
            if (selectedPlantCostText != null)
            {
                selectedPlantCostText.text = allowed
                    ? $"Costo: {plant.seedCost} Gold"
                    : $"No plantable en {currentArea}. Bioma requerido: {plant.allowedAreaId}";
            }
            if (selectedPlantOwnedSeedsText != null) selectedPlantOwnedSeedsText.text = $"Semillas: {manager.GetSeedCount(plant.plantId)}";
            if (buyButton != null)
            {
                buyButton.interactable = allowed;
            }
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

        private void OnGardenManagerReady()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged -= Refresh;
                GardenManager.Instance.OnGardenChanged += Refresh;
            }

            Refresh();
        }

        private void OnValidate()
        {
            if (selectedPlantNameText == null || selectedPlantCostText == null || selectedPlantOwnedSeedsText == null)
            {
                Debug.LogWarning("SeedShopPanel: faltan TMP_Text en inspector.");
            }

            if (buyButton == null || buyButton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning("SeedShopPanel: buyButton sin binding.");
            }
        }

        private void ShowToast(string message)
        {
            if (toastNotifier != null && !string.IsNullOrWhiteSpace(message))
            {
                toastNotifier.Show(message);
            }
        }
    }
}
