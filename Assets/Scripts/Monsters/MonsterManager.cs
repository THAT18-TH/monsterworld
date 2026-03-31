using System;
using System.Collections.Generic;
using MonsterWorldLike.Core;
using MonsterWorldLike.Economy;
using UnityEngine;

namespace MonsterWorldLike.Monsters
{
    public class MonsterManager : MonoBehaviour
    {
        public static MonsterManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private MonsterDefinition starterMonster;

        private readonly List<MonsterInstance> ownedMonsters = new();

        public long TotalProducedGold { get; private set; }

        public event Action OnMonsterStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (starterMonster != null &&
                !string.IsNullOrWhiteSpace(starterMonster.monsterId) &&
                ownedMonsters.Count == 0)
            {
                ownedMonsters.Add(new MonsterInstance(starterMonster.monsterId));
                OnMonsterStateChanged?.Invoke();
                SaveManager.MarkDirty();
            }
        }

        public IReadOnlyList<MonsterInstance> GetMonsters() => ownedMonsters;

        public List<MonsterInstance> BuildSaveState()
        {
            var snapshot = new List<MonsterInstance>(ownedMonsters.Count);
            foreach (var monster in ownedMonsters)
            {
                snapshot.Add(new MonsterInstance(monster.monsterId)
                {
                    nextReadyUnix = monster.nextReadyUnix
                });
            }

            return snapshot;
        }

        public void ApplySaveState(List<MonsterInstance> savedMonsters, long producedGold)
        {
            ownedMonsters.Clear();

            if (savedMonsters != null)
            {
                foreach (var monster in savedMonsters)
                {
                    if (monster == null || string.IsNullOrEmpty(monster.monsterId))
                    {
                        continue;
                    }

                    ownedMonsters.Add(new MonsterInstance(monster.monsterId)
                    {
                        nextReadyUnix = monster.nextReadyUnix
                    });
                }
            }

            TotalProducedGold = Math.Max(0, producedGold);
            OnMonsterStateChanged?.Invoke();
        }

        public bool BuyMonster(MonsterDefinition def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.monsterId))
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null || !economy.SpendGold(def.purchaseCost))
            {
                return false;
            }

            ownedMonsters.Add(new MonsterInstance(def.monsterId));
            OnMonsterStateChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool Collect(MonsterDefinition def, MonsterInstance instance)
        {
            if (def == null || instance == null)
            {
                return false;
            }

            if (!ownedMonsters.Contains(instance))
            {
                return false;
            }

            if (instance.monsterId != def.monsterId)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < instance.nextReadyUnix)
            {
                return false;
            }

            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                return false;
            }

            economy.AddGold(def.productionPerCycle);
            TotalProducedGold += def.productionPerCycle;
            instance.nextReadyUnix = now + def.cycleSeconds;
            OnMonsterStateChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public long SecondsUntilReady(MonsterInstance instance)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0L, instance.nextReadyUnix - now);
        }

        public void SetTotalProducedGold(long value)
        {
            TotalProducedGold = Math.Max(0, value);
            OnMonsterStateChanged?.Invoke();
            SaveManager.MarkDirty();
        }
    }
}
