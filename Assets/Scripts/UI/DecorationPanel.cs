using MonsterWorldLike.Garden;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class DecorationPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text decorationsCountText;
        [SerializeField] private TMP_Text infoText;

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
            if (manager == null)
            {
                if (decorationsCountText != null) decorationsCountText.text = "Decoraciones: 0";
                if (infoText != null) infoText.text = "GardenManager no disponible";
                return;
            }

            var count = manager.GetPlacedDecorations().Count;
            if (decorationsCountText != null) decorationsCountText.text = $"Decoraciones: {count}";
            if (infoText != null) infoText.text = "Panel base listo (colocación visual: TODO)";
        }
    }
}
