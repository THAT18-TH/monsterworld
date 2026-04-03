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
        [SerializeField] private int plotId;
        [SerializeField] private PlantDefinition defaultPlant;
        [SerializeField] private SpriteRenderer groundRenderer;
        [SerializeField] private Transform cropVisualRoot;
        [SerializeField] private Collider2D plotCollider2D;
        [SerializeField] private List<PlantVisualMapping> plantVisuals = new();
        [SerializeField] private GameObject fallbackCropPrefab;

        [Header("State Colors")]
        [SerializeField] private Color lockedColor = new(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color needsWaterColor = new(0.65f, 0.85f, 1f, 1f);
        [SerializeField] private Color growingColor = new(1f, 0.95f, 0.65f, 1f);
        [SerializeField] private Color readyColor = new(0.55f, 1f, 0.55f, 1f);
        [SerializeField] private Color witheredColor = new(0.55f, 0.3f, 0.25f, 1f);

        private readonly Dictionary<string, GameObject> plantPrefabById = new();
        private GameObject currentCropVisual;
        private GardenManager manager;
        private Camera cachedCamera;

        public void Bind(GardenManager gardenManager, int assignedPlotId)
        {
            manager = gardenManager;
            plotId = assignedPlotId;
            RefreshVisual();
        }

        private void Awake()
        {
            cachedCamera = Camera.main;
            RebuildPlantVisualLookup();
            if (groundRenderer == null)
            {
                groundRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (plotCollider2D == null)
            {
                plotCollider2D = GetComponent<Collider2D>();
            }
        }

        private void OnEnable()
        {
            GardenManager.InstanceReady += OnGardenReady;
            BindManagerEvents();
            RefreshVisual();
        }

        private void OnDisable()
        {
            GardenManager.InstanceReady -= OnGardenReady;
            UnbindManagerEvents();
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
            if (cam == null)
            {
                return;
            }

            var world = cam.ScreenToWorldPoint(touch.position);
            var hit = Physics2D.Raycast(world, Vector2.zero);
            if (hit.collider == null || hit.collider != plotCollider2D)
            {
                return;
            }

            TryHandlePrimaryAction();
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryHandlePrimaryAction();
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

        private void RefreshCropVisual(PlotState plot)
        {
            var shouldShow = plot != null && plot.IsPlanted && !plot.withered;
            if (!shouldShow)
            {
                if (currentCropVisual != null)
                {
                    currentCropVisual.SetActive(false);
                }

                return;
            }

            var desiredPrefab = ResolveCropPrefab(plot.plantedSeedId);
            if (desiredPrefab == null)
            {
                return;
            }

            if (currentCropVisual != null && currentCropVisual.name.StartsWith(desiredPrefab.name, StringComparison.Ordinal))
            {
                currentCropVisual.SetActive(true);
                return;
            }

            if (currentCropVisual != null)
            {
                Destroy(currentCropVisual);
            }

            var root = cropVisualRoot != null ? cropVisualRoot : transform;
            currentCropVisual = Instantiate(desiredPrefab, root);
            currentCropVisual.transform.localPosition = Vector3.zero;
            currentCropVisual.transform.localRotation = Quaternion.identity;
            currentCropVisual.SetActive(true);
        }

        private GameObject ResolveCropPrefab(string plantId)
        {
            if (!string.IsNullOrWhiteSpace(plantId) && plantPrefabById.TryGetValue(plantId, out var prefab) && prefab != null)
            {
                return prefab;
            }

            return fallbackCropPrefab;
        }

        private void TryHandlePrimaryAction()
        {
            if (manager == null)
            {
                manager = GardenManager.Instance;
            }

            if (manager == null || IsPlacementInputBlocked())
            {
                return;
            }

            if (manager.TryPerformPrimaryAction(plotId, defaultPlant))
            {
                RefreshVisual();
            }
        }

        private static bool IsPlacementInputBlocked()
        {
            return DecorationManager.Instance != null && DecorationManager.Instance.IsPlacementActive;
        }

        private void OnGardenReady()
        {
            manager = GardenManager.Instance;
            BindManagerEvents();
            RefreshVisual();
        }

        private void BindManagerEvents()
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

        private void UnbindManagerEvents()
        {
            if (manager != null)
            {
                manager.OnGardenChanged -= RefreshVisual;
            }
        }

        private void RebuildPlantVisualLookup()
        {
            plantPrefabById.Clear();
            foreach (var entry in plantVisuals)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.plantId) || entry.prefab == null)
                {
                    continue;
                }

                plantPrefabById[entry.plantId] = entry.prefab;
            }
        }
    }
}
