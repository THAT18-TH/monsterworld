using System;
using System.Collections.Generic;
using MonsterWorldLike.Core;
using UnityEngine;

namespace MonsterWorldLike.Inventory
{
    public enum InventoryCategory
    {
        Seed,
        Resource,
        Monster,
        Decoration,
        Building,
        Material
    }

    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public InventoryCategory category;
        public int amount;
    }

    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private List<InventoryEntry> starterItems = new();

        private readonly Dictionary<string, InventoryEntry> items = new();

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureStarterInventory();
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyCollection<InventoryEntry> GetEntries() => items.Values;

        public int GetAmount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return items.TryGetValue(itemId, out var entry) ? entry.amount : 0;
        }

        public void Add(string itemId, InventoryCategory category, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return;
            }

            if (!items.TryGetValue(itemId, out var entry))
            {
                entry = new InventoryEntry { itemId = itemId, category = category, amount = 0 };
                items[itemId] = entry;
            }

            entry.amount += amount;
            OnInventoryChanged?.Invoke();
            SaveManager.MarkDirty();
        }

        public bool Consume(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            if (!items.TryGetValue(itemId, out var entry) || entry.amount < amount)
            {
                return false;
            }

            entry.amount -= amount;
            if (entry.amount <= 0)
            {
                items.Remove(itemId);
            }

            OnInventoryChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public List<InventoryEntry> BuildSaveState()
        {
            var result = new List<InventoryEntry>(items.Count);
            foreach (var pair in items)
            {
                result.Add(new InventoryEntry
                {
                    itemId = pair.Value.itemId,
                    category = pair.Value.category,
                    amount = pair.Value.amount
                });
            }

            return result;
        }

        public void ApplySaveState(List<InventoryEntry> saved)
        {
            items.Clear();

            if (saved != null)
            {
                foreach (var entry in saved)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.amount <= 0)
                    {
                        continue;
                    }

                    if (items.TryGetValue(entry.itemId, out var existing))
                    {
                        existing.amount += entry.amount;
                    }
                    else
                    {
                        items[entry.itemId] = new InventoryEntry
                        {
                            itemId = entry.itemId,
                            category = entry.category,
                            amount = entry.amount
                        };
                    }
                }
            }

            if (items.Count == 0)
            {
                EnsureStarterInventory();
            }

            OnInventoryChanged?.Invoke();
        }

        private void EnsureStarterInventory()
        {
            if (items.Count > 0)
            {
                return;
            }

            foreach (var starter in starterItems)
            {
                if (starter == null || string.IsNullOrWhiteSpace(starter.itemId) || starter.amount <= 0)
                {
                    continue;
                }

                items[starter.itemId] = new InventoryEntry
                {
                    itemId = starter.itemId,
                    category = starter.category,
                    amount = starter.amount
                };
            }
        }
    }
}
