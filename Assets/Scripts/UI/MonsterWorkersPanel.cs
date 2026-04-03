using MonsterWorldLike.Monsters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterWorldLike.UI
{
    public class MonsterWorkersPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text workersCountText;
        [SerializeField] private TMP_Text producedGoldText;
        [SerializeField] private TMP_Text lastCollectText;
        [SerializeField] private Button collectAllButton;

        private void OnEnable()
        {
            MonsterManager.InstanceReady += OnMonsterManagerReady;
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            MonsterManager.InstanceReady -= OnMonsterManagerReady;
            Unbind();
        }

        public void OnCollectAllPressed()
        {
            var manager = MonsterManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("MonsterWorkersPanel: MonsterManager no disponible.");
                return;
            }

            var collected = manager.CollectAll();
            if (lastCollectText != null)
            {
                lastCollectText.text = $"CollectAll: +{collected} Gold";
            }

            Refresh();
        }

        public void Refresh()
        {
            var manager = MonsterManager.Instance;
            if (manager == null)
            {
                if (workersCountText != null) workersCountText.text = "Workers: 0";
                if (producedGoldText != null) producedGoldText.text = "Produced: 0";
                return;
            }

            if (workersCountText != null) workersCountText.text = $"Workers: {manager.GetMonsters().Count}";
            if (producedGoldText != null) producedGoldText.text = $"Produced: {manager.TotalProducedGold}";
        }

        private void OnMonsterManagerReady()
        {
            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnMonsterStateChanged -= Refresh;
                MonsterManager.Instance.OnMonsterStateChanged += Refresh;
            }
        }

        private void Unbind()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnMonsterStateChanged -= Refresh;
            }
        }

        private void OnValidate()
        {
            if (workersCountText == null || producedGoldText == null)
            {
                Debug.LogWarning("MonsterWorkersPanel: faltan TMP_Text en inspector.");
            }

            if (collectAllButton == null || collectAllButton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning("MonsterWorkersPanel: collectAllButton sin binding.");
            }
        }
    }
}
