using System.Text;
using MonsterWorldLike.Areas;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class AreaPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text areaText;

        private void OnEnable()
        {
            AreaManager.InstanceReady += OnAreaReady;
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            AreaManager.InstanceReady -= OnAreaReady;
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.OnAreaChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            var manager = AreaManager.Instance;
            if (manager == null)
            {
                if (areaText != null) areaText.text = "Areas unavailable";
                return;
            }

            var sb = new StringBuilder();
            var current = manager.CurrentAreaDefinition;
            sb.AppendLine($"Current: {manager.CurrentAreaId}");
            if (current != null)
            {
                sb.AppendLine($"Biome: {current.displayName}");
                sb.AppendLine($"Crop bonus: x{manager.GetCurrentCropSellMultiplier():0.##}");
                if (!string.IsNullOrWhiteSpace(current.biomeDescription))
                {
                    sb.AppendLine(current.biomeDescription);
                }
            }
            foreach (var area in manager.GetAreas())
            {
                sb.AppendLine($"- {area.areaId}: {(area.unlocked ? "Unlocked" : "Locked")}");
            }

            if (areaText != null) areaText.text = sb.ToString();
        }

        public void OnSetArea(string areaId)
        {
            AreaManager.Instance?.SetCurrentArea(areaId);
        }

        private void OnAreaReady()
        {
            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.OnAreaChanged -= Refresh;
                AreaManager.Instance.OnAreaChanged += Refresh;
            }
        }
    }
}
