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

        private void OnEnable()
        {
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
                return;
            }

            if (goldText != null) goldText.text = $"Gold: {economy.Gold}";
            if (gemsText != null) gemsText.text = $"Gems: {economy.Gems}";
            if (foodText != null) foodText.text = $"Food: {economy.Food}";
            if (levelText != null) levelText.text = $"Level: {progression.Level}";
            if (xpText != null) xpText.text = $"XP: {progression.Xp}/{progression.GetXpRequiredForNextLevel()}";
        }
    }
}
