using MonsterWorldLike.Garden;
using TMPro;
using UnityEngine;

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

        private float refreshTimer;

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

        public void Refresh()
        {
            var manager = GardenManager.Instance;
            var plot = GetPlot();

            if (manager == null || plot == null)
            {
                if (stateText != null) stateText.text = "Parcela no disponible";
                if (timerText != null) timerText.text = "--";
                if (actionText != null) actionText.text = string.Empty;
                return;
            }

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
                if (actionText != null) actionText.text = defaultPlant != null ? "Plantar" : "Sin semilla";
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
                manager.UnlockPlot(plotId);
                return;
            }

            if (plot.needsWater)
            {
                manager.Water(plotId);
                return;
            }

            if (plot.IsEmpty)
            {
                if (defaultPlant != null)
                {
                    manager.PlantSeed(plotId, defaultPlant);
                }

                return;
            }

            manager.Harvest(plotId);
        }

        public void OnBuyDefaultSeedPressed()
        {
            if (defaultPlant == null || GardenManager.Instance == null)
            {
                return;
            }

            GardenManager.Instance.BuySeed(defaultPlant, 1);
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
    }
}
