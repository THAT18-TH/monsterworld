using System.Text;
using MonsterWorldLike.Buildings;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class BuildingPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text buildingListText;

        private void OnEnable()
        {
            BuildingManager.InstanceReady += OnBuildingsReady;
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            BuildingManager.InstanceReady -= OnBuildingsReady;
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingsChanged -= Refresh;
            }
        }

        private void Update()
        {
            BuildingManager.Instance?.TickCompletion();
        }

        public void Refresh()
        {
            var manager = BuildingManager.Instance;
            if (manager == null)
            {
                if (buildingListText != null) buildingListText.text = "Buildings unavailable";
                return;
            }

            var sb = new StringBuilder();
            var list = manager.GetBuildings();
            for (var i = 0; i < list.Count; i++)
            {
                var b = list[i];
                sb.AppendLine($"{b.buildingId} | {b.areaId} | {(b.completed ? "Ready" : "Building")}");
            }

            if (buildingListText != null) buildingListText.text = sb.Length > 0 ? sb.ToString() : "No buildings";
        }

        private void OnBuildingsReady()
        {
            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingsChanged -= Refresh;
                BuildingManager.Instance.OnBuildingsChanged += Refresh;
            }
        }
    }
}
