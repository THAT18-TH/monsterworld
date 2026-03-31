using System;
using MonsterWorldLike.Core;
using UnityEngine;

namespace MonsterWorldLike.Progression
{
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }
        public static event Action InstanceReady;

        [Header("Initial Progression")]
        [SerializeField, Min(1)] private int initialLevel = 1;
        [SerializeField, Min(0)] private int initialXp = 0;
        [SerializeField, Min(10)] private int baseXpPerLevel = 100;

        public int Level { get; private set; }
        public int Xp { get; private set; }

        public event Action OnProgressionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Level = Mathf.Max(1, initialLevel);
            Xp = Mathf.Max(0, initialXp);
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetProgress(int level, int xp)
        {
            Level = Mathf.Max(1, level);
            Xp = Mathf.Max(0, xp);
            OnProgressionChanged?.Invoke();
            SaveManager.MarkDirty();
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Xp += amount;

            while (Xp >= GetXpRequiredForNextLevel())
            {
                Xp -= GetXpRequiredForNextLevel();
                Level++;
            }

            OnProgressionChanged?.Invoke();
            SaveManager.MarkDirty();
        }

        public int GetXpRequiredForNextLevel()
        {
            return baseXpPerLevel + ((Level - 1) * 25);
        }
    }
}
