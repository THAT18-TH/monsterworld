using System;
using System.Collections.Generic;
using MonsterWorldLike.Decorations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterWorldLike.Garden
{
    [Serializable]
    public class PlantVisualMapping
    {
        public string plantId;
        public GameObject prefab;
    }

    public class PlotVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private int plotId;
        [SerializeField] private SpriteRenderer groundRenderer;
        [SerializeField] private Transform cropVisualRoot;
        [SerializeField] private Collider2D plotCollider2D;
        [SerializeField] private PlantDefinition defaultPlant;
        [SerializeField] private List<PlantVisualMapping> plantVisuals = new();
        [SerializeField] private GameObject fallbackCropPrefab;

        [Header("State Colors")]
        [SerializeField] private Color lockedColor = new(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color needsWaterColor = new(0.65f, 0.85f, 1f, 1f);
        [SerializeField] private Color growingColor = new(1f, 0.95f, 0.65f, 1f);
        [SerializeField] private Color readyColor = new(0.55f, 1f, 0.55f, 1f);
        [SerializeField] private Color witheredColor = new(0.55f, 0.3f, 0.25f, 1f);

        private readonly Dictionary<string, GameObject> plantPrefabsById = new();
        private GardenManager manager;
        private Camera cachedCamera;
        private GameObject activeCropVisual;

        public void Bind(GardenManager gardenManager, int assignedPlotId)
        {
            manager = gardenManager;
            plotId = assignedPlotId;
            RefreshVisual();
        }

        private void Awake()
        {
            cachedCamera = Camera.main;
            if (groundRenderer == null)
            {
                groundRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (plotCollider2D == null)
            {
                plotCollider2D = GetComponent<Collider2D>();
            }

            RebuildPlantVisualLookup();
        }

        private void OnEnable()
        {
            GardenManager.InstanceReady += OnGardenManagerReady;
            RebindManagerEvents();
            RefreshVisual();
        }

        private void OnDisable()
        {
            GardenManager.InstanceReady -= OnGardenManagerReady;
            if (manager != null)
            {
                manager.OnGardenChanged -= RefreshVisual;
            }
        }

        private void Update()
        {
            if (Input.touchCount != 1)
            {
                return;
            }

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            var cam = cachedCamera != null ? cachedCamera : Camera.main;
            if (cam == null || plotCollider2D == null)
            {
                return;
            }

            var worldPoint = cam.ScreenToWorldPoint(touch.position);
            var hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider == null || hit.collider != plotCollider2D)
            {
                return;
            }

            TryPrimaryAction();
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryPrimaryAction();
        }

        public void RefreshVisual()
        {
            if (manager == null)
            {
                manager = GardenManager.Instance;
            }

            if (manager == null || !manager.TryGetPlot(plotId, out var plot))
            {
                return;
            }

            if (groundRenderer != null)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                groundRenderer.color = !plot.unlocked
                    ? lockedColor
                    : plot.withered
                        ? witheredColor
                        : plot.IsEmpty
                            ? emptyColor
                            : plot.needsWater
                                ? needsWaterColor
                                : plot.IsReadyToHarvest(now)
                                    ? readyColor
                                    : growingColor;
            }

            RefreshCropVisual(plot);
        }

        private void TryPrimaryAction()
        {
            if (manager == null)
            {
                manager = GardenManager.Instance;
            }

            if (manager == null)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (DecorationManager.Instance != null && DecorationManager.Instance.IsPlacementActive)
            {
                return;
            }

            if (!manager.TryGetPlot(plotId, out var plot))
            {
                return;
            }

            bool success;
            if (!plot.unlocked)
            {
                success = manager.UnlockPlot(plotId);
            }
            else if (plot.withered)
            {
                success = manager.RevivePlot(plotId);
            }
            else if (plot.needsWater)
            {
                success = manager.Water(plotId);
            }
            else if (plot.IsEmpty)
            {
                success = defaultPlant != null && manager.PlantSeed(plotId, defaultPlant);
            }
            else
            {
                success = manager.Harvest(plotId);
            }

            if (success)
            {
                RefreshVisual();
            }
        }

        private void RefreshCropVisual(PlotState plot)
        {
            var show = plot != null && plot.IsPlanted && !plot.withered;
            if (!show)
            {
                if (activeCropVisual != null)
                {
                    activeCropVisual.SetActive(false);
                }

                return;
            }

            var prefab = ResolveCropPrefab(plot.plantedSeedId);
            if (prefab == null)
            {
                return;
            }

            if (activeCropVisual != null && activeCropVisual.name.StartsWith(prefab.name, StringComparison.Ordinal))
            {
                activeCropVisual.SetActive(true);
                return;
            }

            if (activeCropVisual != null)
            {
                Destroy(activeCropVisual);
            }

            var root = cropVisualRoot != null ? cropVisualRoot : transform;
            activeCropVisual = Instantiate(prefab, root);
            activeCropVisual.transform.localPosition = Vector3.zero;
            activeCropVisual.transform.localRotation = Quaternion.identity;
            activeCropVisual.SetActive(true);
        }

        private GameObject ResolveCropPrefab(string plantId)
        {
            if (!string.IsNullOrWhiteSpace(plantId) && plantPrefabsById.TryGetValue(plantId, out var prefab) && prefab != null)
            {
                return prefab;
            }

            return fallbackCropPrefab;
        }

        private void RebuildPlantVisualLookup()
        {
            plantPrefabsById.Clear();
            foreach (var entry in plantVisuals)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.plantId) || entry.prefab == null)
                {
                    continue;
                }

                plantPrefabsById[entry.plantId] = entry.prefab;
            }
        }

        private void OnGardenManagerReady()
        {
            RebindManagerEvents();
            RefreshVisual();
        }

        private void RebindManagerEvents()
        {
            if (manager == null)
            {
                manager = GardenManager.Instance;
            }

            if (manager != null)
            {
                manager.OnGardenChanged -= RefreshVisual;
                manager.OnGardenChanged += RefreshVisual;
            }
        }
    }
}
