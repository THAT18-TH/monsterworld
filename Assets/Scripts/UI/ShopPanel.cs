using System.Collections.Generic;
using MonsterWorldLike.Buildings;
using MonsterWorldLike.Decorations;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Areas;
using MonsterWorldLike.Garden;
using MonsterWorldLike.Monsters;
using MonsterWorldLike.Progression;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public enum ShopTab
    {
        Seeds,
        Monsters,
        Decorations,
        Buildings
    }

    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private ShopTab currentTab;
        [SerializeField] private TMP_Text tabTitleText;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemCostText;
        [SerializeField] private TMP_Text itemRequirementText;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private ToastNotifier toastNotifier;

        [SerializeField] private List<DecorationDefinition> decorationCatalog = new();

        private int selectedIndex;
        private int buyAmount = 1;

        public void SetTab(int tab)
        {
            currentTab = (ShopTab)Mathf.Clamp(tab, 0, 3);
            selectedIndex = 0;
            Refresh();
        }

        public void SetBuyAmount(int amount)
        {
            buyAmount = Mathf.Clamp(amount, 1, 10);
            Refresh();
        }

        public void SelectItem(int index)
        {
            selectedIndex = Mathf.Max(0, index);
            Refresh();
        }

        public void BuySelected()
        {
            switch (currentTab)
            {
                case ShopTab.Seeds:
                    TryBuySeed();
                    break;
                case ShopTab.Monsters:
                    TryBuyMonster();
                    break;
                case ShopTab.Buildings:
                    TryBuyBuilding();
                    break;
                case ShopTab.Decorations:
                    TryBuyDecoration();
                    break;
            }

            Refresh();
        }

        public void Refresh()
        {
            var level = ProgressionManager.Instance != null ? ProgressionManager.Instance.Level : 1;
            if (tabTitleText != null) tabTitleText.text = currentTab.ToString();

            switch (currentTab)
            {
                case ShopTab.Seeds:
                    var plant = GetSelectedPlant();
                    if (plant == null)
                    {
                        SetEmpty();
                        return;
                    }

                    itemNameText.text = plant.displayName;
                    itemCostText.text = $"Cost: {plant.seedCost * buyAmount} Gold";
                    var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
                    var allowed = string.IsNullOrWhiteSpace(plant.allowedAreaId) || plant.allowedAreaId == currentArea;
                    itemRequirementText.text = allowed
                        ? "Req: biome met"
                        : $"Req: biome {plant.allowedAreaId}";
                    var sellMultiplier = AreaManager.Instance != null ? AreaManager.Instance.GetCurrentCropSellMultiplier() : 1f;
                    tooltipText.text = $"Buy seeds and plant in unlocked plots. Current biome sell x{sellMultiplier:0.##}.";
                    break;

                case ShopTab.Monsters:
                    var monster = GetSelectedMonster();
                    if (monster == null)
                    {
                        SetEmpty();
                        return;
                    }

                    itemNameText.text = monster.displayName;
                    itemCostText.text = $"Cost: {monster.purchaseGoldCost * buyAmount} Gold";
                    itemRequirementText.text = level >= monster.unlockLevel
                        ? "Req: met"
                        : $"Req: Level {monster.unlockLevel}";
                    tooltipText.text = $"Role: {monster.role} | Food: {monster.foodCost}";
                    break;

                case ShopTab.Buildings:
                    var building = GetSelectedBuilding();
                    if (building == null)
                    {
                        SetEmpty();
                        return;
                    }

                    itemNameText.text = building.displayName;
                    itemCostText.text = $"Cost: {building.goldCost} Gold + {building.gemsCost} Gems";
                    itemRequirementText.text = level >= building.unlockLevel
                        ? "Req: met"
                        : $"Req: Level {building.unlockLevel}";
                    tooltipText.text = $"Build time: {building.buildSeconds}s";
                    break;

                case ShopTab.Decorations:
                    var decoration = GetSelectedDecoration();
                    if (decoration == null)
                    {
                        SetEmpty();
                        return;
                    }

                    itemNameText.text = decoration.displayName;
                    itemCostText.text = $"Cost: {decoration.goldCost} Gold";
                    itemRequirementText.text = level >= decoration.unlockLevel
                        ? "Req: met"
                        : $"Req: Level {decoration.unlockLevel}";
                    tooltipText.text = "Use DecorationManager placement mode to place.";
                    break;
            }
        }

        private void TryBuySeed()
        {
            var plant = GetSelectedPlant();
            if (plant == null || GardenManager.Instance == null)
            {
                ShowToast("No hay semillas disponibles.");
                return;
            }

            var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
            if (!string.IsNullOrWhiteSpace(plant.allowedAreaId) && plant.allowedAreaId != currentArea)
            {
                ShowToast($"Esta planta se cultiva en bioma {plant.allowedAreaId}.");
            }

            for (var i = 0; i < buyAmount; i++)
            {
                if (!GardenManager.Instance.BuySeed(plant, 1))
                {
                    ShowToast("Compra detenida: oro insuficiente.");
                    break;
                }
            }
        }

        private void TryBuyMonster()
        {
            var monster = GetSelectedMonster();
            if (monster == null || MonsterManager.Instance == null)
            {
                ShowToast("No hay workers disponibles.");
                return;
            }

            for (var i = 0; i < buyAmount; i++)
            {
                if (!MonsterManager.Instance.BuyMonster(monster))
                {
                    ShowToast("No se pudo comprar worker (requisitos/recursos).");
                    break;
                }
            }
        }

        private void TryBuyBuilding()
        {
            var building = GetSelectedBuilding();
            if (building == null || BuildingManager.Instance == null)
            {
                ShowToast("No hay edificios disponibles.");
                return;
            }

            if (!BuildingManager.Instance.BuyAndPlace(building, Vector3.zero))
            {
                ShowToast("No se pudo comprar/colocar edificio.");
            }
        }

        private void TryBuyDecoration()
        {
            var def = GetSelectedDecoration();
            if (def == null)
            {
                ShowToast("No hay decoraciones disponibles.");
                return;
            }

            var progression = ProgressionManager.Instance;
            if (progression != null && progression.Level < def.unlockLevel)
            {
                ShowToast($"Nivel {def.unlockLevel} requerido.");
                return;
            }

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.SpendGold(def.goldCost * buyAmount))
            {
                ShowToast("Oro insuficiente para decoración.");
                return;
            }

            var inventory = Inventory.InventoryManager.Instance;
            if (inventory == null)
            {
                ShowToast("Inventario no disponible.");
                return;
            }

            inventory.Add(def.decorationId, Inventory.InventoryCategory.Decoration, buyAmount);
            ShowToast("Decoración comprada.");
        }

        private PlantDefinition GetSelectedPlant()
        {
            var manager = GardenManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var catalog = manager.GetPlantCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                return null;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, catalog.Count - 1);
            return catalog[selectedIndex];
        }

        private MonsterDefinition GetSelectedMonster()
        {
            var manager = MonsterManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var catalog = manager.GetCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                return null;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, catalog.Count - 1);
            return catalog[selectedIndex];
        }

        private BuildingDefinition GetSelectedBuilding()
        {
            var manager = BuildingManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var catalog = manager.GetCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                return null;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, catalog.Count - 1);
            return catalog[selectedIndex];
        }

        private DecorationDefinition GetSelectedDecoration()
        {
            if (decorationCatalog == null || decorationCatalog.Count == 0)
            {
                return null;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, decorationCatalog.Count - 1);
            return decorationCatalog[selectedIndex];
        }

        private void SetEmpty()
        {
            if (itemNameText != null) itemNameText.text = "No items";
            if (itemCostText != null) itemCostText.text = "Cost: --";
            if (itemRequirementText != null) itemRequirementText.text = "Req: --";
            if (tooltipText != null) tooltipText.text = "No data available.";
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
