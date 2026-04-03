using System.Text;
using MonsterWorldLike.Inventory;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class InventoryPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text inventoryText;

        private void OnEnable()
        {
            InventoryManager.InstanceReady += OnInventoryReady;
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            InventoryManager.InstanceReady -= OnInventoryReady;
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            var manager = InventoryManager.Instance;
            if (manager == null)
            {
                if (inventoryText != null) inventoryText.text = "Inventory unavailable";
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in manager.GetEntries())
            {
                sb.AppendLine($"[{item.category}] {item.itemId}: {item.amount}");
            }

            if (inventoryText != null) inventoryText.text = sb.Length > 0 ? sb.ToString() : "Inventory empty";
        }

        private void OnInventoryReady()
        {
            Bind();
            Refresh();
        }

        private void Bind()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
                InventoryManager.Instance.OnInventoryChanged += Refresh;
            }
        }
    }
}
