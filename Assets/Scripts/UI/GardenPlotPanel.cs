using MonsterWorldLike.Garden;
using MonsterWorldLike.Areas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterWorldLike.UI
{
    public class GardenPlotPanel : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.25f;

        [SerializeField] private int plotId;
        [SerializeField] private PlantDefinition defaultPlant;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Button primaryActionButton;
        [SerializeField] private Button buySeedButton;
        [SerializeField] private ToastNotifier toastNotifier;

        private float refreshTimer;
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

        public void Refresh()
        {
            var manager = GardenManager.Instance;
            var plot = GetPlot();

            if (manager == null || plot == null)
            {
                if (!hasLoggedMissingManager)
                {
                    Debug.LogWarning($"GardenPlotPanel({plotId}): GardenManager no disponible o plot inexistente.");
                    hasLoggedMissingManager = true;
                }

                if (stateText != null) stateText.text = "Parcela no disponible";
                if (timerText != null) timerText.text = "--";
                if (actionText != null) actionText.text = string.Empty;
                return;
            }

            hasLoggedMissingManager = false;

            if (!plot.unlocked)
            {
                if (stateText != null) stateText.text = "Bloqueada";
                if (timerText != null) timerText.text = "--";
                if (actionText != null) actionText.text = "Desbloquear";
                return;
            }

            if (plot.IsEmpty)
            {
                if (stateText != null) stateText.text = "Vacía";
                if (timerText != null) timerText.text = "--";
                if (actionText != null)
                {
                    if (defaultPlant == null)
                    {
                        actionText.text = "Sin semilla";
                    }
                    else
                    {
                        var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
                        var allowed = manager.IsPlantAllowedInCurrentArea(defaultPlant);
                        actionText.text = allowed ? "Plantar" : $"Bioma inválido ({currentArea})";
                    }
                }
                return;
            }

            if (plot.withered)
            {
                if (stateText != null) stateText.text = "Marchita";
                if (timerText != null) timerText.text = "--";
                if (actionText != null) actionText.text = "Revive desde UI/Economía";
                return;
            }

            if (plot.needsWater)
            {
                if (stateText != null) stateText.text = "Necesita agua";
                if (timerText != null) timerText.text = "--";
                if (actionText != null) actionText.text = "Regar";
                return;
            }

            var seconds = manager.SecondsUntilReady(plotId);
            if (seconds <= 0)
            {
                if (stateText != null) stateText.text = "Lista";
                if (timerText != null) timerText.text = "0s";
                if (actionText != null) actionText.text = "Cosechar";
            }
            else
            {
                if (stateText != null) stateText.text = "Creciendo";
                if (timerText != null) timerText.text = $"{seconds}s";
                if (actionText != null) actionText.text = "Esperando";
            }
        }

        private void Update()
        {
            var plot = GetPlot();
            if (plot == null || !plot.unlocked || plot.IsEmpty || plot.needsWater)
            {
                refreshTimer = 0f;
                return;
            }

            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer < RefreshIntervalSeconds)
            {
                return;
            }

            refreshTimer = 0f;
            Refresh();
        }

        public void OnPrimaryActionPressed()
        {
            var manager = GardenManager.Instance;
            var plot = GetPlot();
            if (manager == null || plot == null)
            {
                return;
            }

            if (!plot.unlocked)
            {
                if (!manager.UnlockPlot(plotId))
                {
                    ShowToast("No se pudo desbloquear parcela.");
                }
                return;
            }

            if (plot.needsWater)
            {
                if (!manager.Water(plotId))
                {
                    ShowToast("No se pudo regar.");
                }
                return;
            }

            if (plot.IsEmpty)
            {
                if (defaultPlant != null)
                {
                    if (!manager.IsPlantAllowedInCurrentArea(defaultPlant))
                    {
                        var currentArea = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";
                        ShowToast($"Semilla inválida para {currentArea}.");
                    }
                    else if (!manager.PlantSeed(plotId, defaultPlant))
                    {
                        ShowToast("No se pudo plantar.");
                    }
                }
                else
                {
                    ShowToast("No hay semilla configurada.");
                }

                return;
            }

            if (!manager.Harvest(plotId))
            {
                ShowToast("Aún no se puede cosechar.");
            }
        }

        public void OnBuyDefaultSeedPressed()
        {
            if (defaultPlant == null || GardenManager.Instance == null)
            {
                ShowToast("No hay semilla por defecto.");
                return;
            }

            if (!GardenManager.Instance.BuySeed(defaultPlant, 1))
            {
                ShowToast("No se pudo comprar semilla.");
            }
        }

        private PlotState GetPlot()
        {
            var manager = GardenManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var plots = manager.GetPlots();
            for (var i = 0; i < plots.Count; i++)
            {
                var plot = plots[i];
                if (plot != null && plot.plotId == plotId)
                {
                    return plot;
                }
            }

            return null;
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
            if (stateText == null || timerText == null || actionText == null)
            {
                Debug.LogWarning($"GardenPlotPanel({plotId}): faltan TMP_Text en inspector.");
            }

            if (defaultPlant == null)
            {
                Debug.LogWarning($"GardenPlotPanel({plotId}): PlantDefinition por defecto no asignada.");
            }

            if (primaryActionButton == null || primaryActionButton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning($"GardenPlotPanel({plotId}): primaryActionButton sin binding.");
            }

            if (buySeedButton == null || buySeedButton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning($"GardenPlotPanel({plotId}): buySeedButton sin binding.");
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
