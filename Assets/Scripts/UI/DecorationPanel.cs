using MonsterWorldLike.Garden;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class DecorationPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text decorationsCountText;
        [SerializeField] private TMP_Text infoText;
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
            if (manager == null)
            {
                if (!hasLoggedMissingManager)
                {
                    Debug.LogWarning("DecorationPanel: GardenManager no disponible.");
                    hasLoggedMissingManager = true;
                }

                if (decorationsCountText != null) decorationsCountText.text = "Decoraciones: 0";
                if (infoText != null) infoText.text = "GardenManager no disponible";
                return;
            }

            hasLoggedMissingManager = false;

            var count = manager.GetPlacedDecorations().Count;
            if (decorationsCountText != null) decorationsCountText.text = $"Decoraciones: {count}";
            if (infoText != null) infoText.text = "Panel base listo (colocación visual: TODO)";
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
            if (decorationsCountText == null || infoText == null)
            {
                Debug.LogWarning("DecorationPanel: faltan TMP_Text en inspector.");
            }
        }
    }
}
