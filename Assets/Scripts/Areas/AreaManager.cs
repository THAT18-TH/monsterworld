using System;
using System.Collections.Generic;
using MonsterWorldLike.Core;
using UnityEngine;

namespace MonsterWorldLike.Areas
{
    [Serializable]
    public class AreaState
    {
        public string areaId;
        public bool unlocked;
    }

    [Serializable]
    public class AreaDefinition
    {
        public string areaId = "main";
        public string displayName = "Main";
        [TextArea] public string biomeDescription;
        [Range(0.5f, 3f)] public float cropSellMultiplier = 1f;
    }

    public class AreaManager : MonoBehaviour
    {
        public static AreaManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private List<AreaDefinition> areaDefinitions = new() { new AreaDefinition { areaId = "main", displayName = "Main", biomeDescription = "Bioma inicial", cropSellMultiplier = 1f } };

        private readonly List<AreaState> areas = new();
        private readonly Dictionary<string, AreaDefinition> defsById = new();

        public string CurrentAreaId { get; private set; } = "main";

        public event Action OnAreaChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildLookup();
            EnsureDefaultAreas();
            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<AreaState> GetAreas() => areas;
        public AreaDefinition CurrentAreaDefinition => GetAreaDefinition(CurrentAreaId);

        public AreaDefinition GetAreaDefinition(string areaId)
        {
            return !string.IsNullOrWhiteSpace(areaId) && defsById.TryGetValue(areaId, out var def) ? def : null;
        }

        public float GetCurrentCropSellMultiplier()
        {
            var def = CurrentAreaDefinition;
            return def != null ? Mathf.Max(0.1f, def.cropSellMultiplier) : 1f;
        }

        public bool UnlockArea(string areaId)
        {
            var area = FindArea(areaId);
            if (area == null || area.unlocked)
            {
                return false;
            }

            area.unlocked = true;
            OnAreaChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public bool SetCurrentArea(string areaId)
        {
            var area = FindArea(areaId);
            if (area == null || !area.unlocked)
            {
                return false;
            }

            CurrentAreaId = area.areaId;
            OnAreaChanged?.Invoke();
            SaveManager.MarkDirty();
            return true;
        }

        public void ApplySaveState(List<AreaState> savedAreas, string currentAreaId)
        {
            areas.Clear();
            RebuildLookup();

            if (savedAreas != null)
            {
                foreach (var area in savedAreas)
                {
                    if (area == null || string.IsNullOrWhiteSpace(area.areaId))
                    {
                        continue;
                    }

                    areas.Add(new AreaState { areaId = area.areaId, unlocked = area.unlocked });
                }
            }

            if (areas.Count == 0)
            {
                EnsureDefaultAreas();
            }

            if (!SetCurrentArea(currentAreaId))
            {
                CurrentAreaId = "main";
            }

            OnAreaChanged?.Invoke();
        }

        public List<AreaState> BuildSaveState()
        {
            var save = new List<AreaState>(areas.Count);
            foreach (var area in areas)
            {
                save.Add(new AreaState { areaId = area.areaId, unlocked = area.unlocked });
            }

            return save;
        }

        private void EnsureDefaultAreas()
        {
            areas.Clear();
            for (var i = 0; i < areaDefinitions.Count; i++)
            {
                var areaDef = areaDefinitions[i];
                var id = areaDef != null ? areaDef.areaId : string.Empty;
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                areas.Add(new AreaState { areaId = id, unlocked = i == 0 });
            }

            if (areas.Count == 0)
            {
                areas.Add(new AreaState { areaId = "main", unlocked = true });
            }
        }

        private void RebuildLookup()
        {
            defsById.Clear();
            foreach (var def in areaDefinitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.areaId))
                {
                    continue;
                }

                defsById[def.areaId] = def;
            }

            if (!defsById.ContainsKey("main"))
            {
                defsById["main"] = new AreaDefinition { areaId = "main", displayName = "Main", biomeDescription = "Bioma inicial", cropSellMultiplier = 1f };
            }
        }

        private AreaState FindArea(string areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
            {
                return null;
            }

            for (var i = 0; i < areas.Count; i++)
            {
                if (areas[i].areaId == areaId)
                {
                    return areas[i];
                }
            }

            return null;
        }
    }
}
