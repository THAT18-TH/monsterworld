using MonsterWorldLike.Economy;
using MonsterWorldLike.Progression;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private TMP_Text foodText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;

        private bool hasLoggedMissingManager;

        private void OnEnable()
        {
            EconomyManager.InstanceReady += OnManagerReady;
            ProgressionManager.InstanceReady += OnManagerReady;

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBalanceChanged += Refresh;
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            EconomyManager.InstanceReady -= OnManagerReady;
            ProgressionManager.InstanceReady -= OnManagerReady;

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBalanceChanged -= Refresh;
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            var economy = EconomyManager.Instance;
            var progression = ProgressionManager.Instance;

            if (economy == null || progression == null)
            {
                if (!hasLoggedMissingManager)
                {
                    Debug.LogWarning("HudController: EconomyManager o ProgressionManager no disponible.");
                    hasLoggedMissingManager = true;
                }
                return;
            }

            hasLoggedMissingManager = false;

            if (goldText != null) goldText.text = $"Gold: {economy.Gold}";
            if (gemsText != null) gemsText.text = $"Gems: {economy.Gems}";
            if (foodText != null) foodText.text = $"Food: {economy.Food}";
            if (levelText != null) levelText.text = $"Level: {progression.Level}";
            if (xpText != null) xpText.text = $"XP: {progression.Xp}/{progression.GetXpRequiredForNextLevel()}";
        }

        private void OnManagerReady()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBalanceChanged -= Refresh;
                EconomyManager.Instance.OnBalanceChanged += Refresh;
            }

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnProgressionChanged -= Refresh;
                ProgressionManager.Instance.OnProgressionChanged += Refresh;
            }

            Refresh();
        }

        private void OnValidate()
        {
            if (goldText == null || gemsText == null || foodText == null || levelText == null || xpText == null)
            {
                Debug.LogWarning("HudController: faltan referencias TMP_Text en inspector.");
            }
        }
    }
}
