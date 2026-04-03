using System.Collections.Generic;
using MonsterWorldLike.Areas;
using MonsterWorldLike.Decorations;
using MonsterWorldLike.Garden;
using MonsterWorldLike.Inventory;
using MonsterWorldLike.Progression;
using UnityEngine;

namespace MonsterWorldLike.Decorations
{
    public class DecorationManager : MonoBehaviour
    {
        public static DecorationManager Instance { get; private set; }

        [SerializeField] private List<DecorationDefinition> catalog = new();
        [SerializeField] private Material ghostMaterial;
        [SerializeField, Min(0.1f)] private float gridSize = 1f;
        [SerializeField, Min(0.1f)] private float maxPlacementDistance = 40f;
        [SerializeField] private LayerMask blockingLayers = ~0;
        [SerializeField] private Vector2 mapBoundsX = new(-25f, 25f);
        [SerializeField] private Vector2 mapBoundsZ = new(-25f, 25f);
        [SerializeField] private Color validGhostColor = new(0f, 1f, 0f, 0.35f);
        [SerializeField] private Color invalidGhostColor = new(1f, 0f, 0f, 0.35f);
        [SerializeField] private bool showValidationToasts = false;
        [SerializeField] private MonsterWorldLike.UI.ToastNotifier toastNotifier;

        private readonly Dictionary<string, DecorationDefinition> definitions = new();
        private readonly Dictionary<int, GameObject> visualsByIndex = new();

        private GameObject ghost;
        private DecorationDefinition selected;
        private bool lastCanPlace = true;
        private readonly Collider[] overlapBuffer = new Collider[16];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildLookup();
        }

        private void OnEnable()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged += RebuildVisualsFromState;
            }

            RebuildVisualsFromState();
        }

        private void OnDisable()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.OnGardenChanged -= RebuildVisualsFromState;
            }

            DestroyGhost();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<DecorationDefinition> GetCatalog() => catalog;
        public bool IsPlacementActive => selected != null && ghost != null;

        public void BeginPlacement(DecorationDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.decorationId))
            {
                return;
            }

            if (!definitions.TryGetValue(definition.decorationId, out var def))
            {
                return;
            }

            var progression = ProgressionManager.Instance;
            if (progression != null && progression.Level < def.unlockLevel)
            {
                return;
            }

            selected = def;
            CreateGhost(def);
        }

        public bool PlaceAt(Vector3 worldPos)
        {
            if (selected == null || GardenManager.Instance == null)
            {
                return false;
            }

            var snapped = Snap(worldPos);
            if (!CanPlace(snapped, out var reason))
            {
                ShowToast(reason);
                return false;
            }

            var inventory = InventoryManager.Instance;
            var area = AreaManager.Instance != null ? AreaManager.Instance.CurrentAreaId : "main";

            if (inventory != null && !inventory.Consume(selected.decorationId, 1))
            {
                return false;
            }

            var placed = GardenManager.Instance.PlaceDecoration(new DecorationPlacement
            {
                decorationId = selected.decorationId,
                areaId = area,
                posX = snapped.x,
                posY = snapped.y,
                posZ = snapped.z,
                rotY = 0f,
                plotId = -1
            });

            if (!placed && inventory != null)
            {
                inventory.Add(selected.decorationId, InventoryCategory.Decoration, 1);
            }
            else if (placed)
            {
                GameEvents.DecorationPlaced(selected.decorationId);
                ShowToast("Decoración colocada.");
            }

            return placed;
        }

        public bool Move(int index, Vector3 worldPos)
        {
            return GardenManager.Instance != null && GardenManager.Instance.MoveDecoration(index, worldPos);
        }

        public bool Rotate(int index, float yRot)
        {
            return GardenManager.Instance != null && GardenManager.Instance.RotateDecoration(index, yRot);
        }

        public bool Sell(int index, int refundGold)
        {
            var ok = GardenManager.Instance != null && GardenManager.Instance.RemoveDecoration(index);
            if (ok)
            {
                MonsterWorldLike.Economy.EconomyManager.Instance?.AddGold(refundGold);
            }

            return ok;
        }

        public bool Delete(int index)
        {
            return GardenManager.Instance != null && GardenManager.Instance.RemoveDecoration(index);
        }

        public void UpdateGhostPosition(Vector3 worldPos, bool canPlace)
        {
            if (ghost == null)
            {
                return;
            }

            var snapped = Snap(worldPos);
            var valid = canPlace && CanPlace(snapped, out _);
            ghost.transform.position = snapped;
            if (ghostMaterial != null)
            {
                ghostMaterial.color = valid ? validGhostColor : invalidGhostColor;
            }

            var changed = valid != lastCanPlace;
            lastCanPlace = valid;
            if (showValidationToasts && changed)
            {
                ShowToast(valid ? "Posición válida" : "Posición inválida");
            }
        }

        private void RebuildVisualsFromState()
        {
            foreach (var pair in visualsByIndex)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            visualsByIndex.Clear();

            if (GardenManager.Instance == null)
            {
                return;
            }

            var placements = GardenManager.Instance.GetPlacedDecorations();
            for (var i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (p == null || string.IsNullOrWhiteSpace(p.decorationId))
                {
                    continue;
                }

                if (!definitions.TryGetValue(p.decorationId, out var def) || def.prefab == null)
                {
                    continue;
                }

                var go = Instantiate(def.prefab, new Vector3(p.posX, p.posY, p.posZ), Quaternion.Euler(0f, p.rotY, 0f), transform);
                visualsByIndex[i] = go;
            }
        }

        private void RebuildLookup()
        {
            definitions.Clear();
            foreach (var item in catalog)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.decorationId))
                {
                    continue;
                }

                definitions[item.decorationId] = item;
            }
        }

        private void CreateGhost(DecorationDefinition def)
        {
            DestroyGhost();
            if (def.prefab == null)
            {
                return;
            }

            ghost = Instantiate(def.prefab, Vector3.zero, Quaternion.identity, transform);
            var renderers = ghost.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (ghostMaterial != null)
                {
                    r.sharedMaterial = ghostMaterial;
                }
            }
        }

        private void DestroyGhost()
        {
            if (ghost != null)
            {
                Destroy(ghost);
                ghost = null;
            }
        }

        private Vector3 Snap(Vector3 worldPos)
        {
            return new Vector3(
                Mathf.Round(worldPos.x / gridSize) * gridSize,
                Mathf.Round(worldPos.y / gridSize) * gridSize,
                Mathf.Round(worldPos.z / gridSize) * gridSize);
        }

        private bool CanPlace(Vector3 worldPos, out string reason)
        {
            if (worldPos.magnitude > maxPlacementDistance)
            {
                reason = "Fuera de rango de colocación.";
                return false;
            }

            if (worldPos.x < Mathf.Min(mapBoundsX.x, mapBoundsX.y) || worldPos.x > Mathf.Max(mapBoundsX.x, mapBoundsX.y) ||
                worldPos.z < Mathf.Min(mapBoundsZ.x, mapBoundsZ.y) || worldPos.z > Mathf.Max(mapBoundsZ.x, mapBoundsZ.y))
            {
                reason = "Fuera de los límites del mapa.";
                return false;
            }

            var overlapCount = Physics.OverlapBoxNonAlloc(worldPos, Vector3.one * 0.45f, overlapBuffer, Quaternion.identity, blockingLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < overlapCount; i++)
            {
                var hit = overlapBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                if (ghost != null && (hit.transform == ghost.transform || hit.transform.IsChildOf(ghost.transform)))
                {
                    continue;
                }

                reason = "Espacio ocupado.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private void ShowToast(string message)
        {
            if (toastNotifier != null && !string.IsNullOrWhiteSpace(message))
            {
                toastNotifier.Show(message);
            }
        }
    }
}
