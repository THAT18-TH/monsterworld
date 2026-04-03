# MonsterWorldLike (Unity Starter)

Base de **garden sim + workers** para Android/Unity, con persistencia local y arquitectura modular.

## Estado actual

### Terminado y funcional
- Loop jardín estable: buy seed -> plant -> water -> wait -> harvest -> gold/xp.
- Workers/monstruos integrados con `CollectAll` offline.
- Persistencia unificada (`SaveManager`) y ciclo de vida (`GameManager`).
- Shop base con tabs + compra x1/x5/x10.
- Inventario general, edificios, áreas y daily rewards (base jugable).
- Placement visual de decoraciones con manager dedicado + preview fantasma, límites de mapa y validación de colisiones.
- Quest manager separado con claim y soporte cadena básica (`nextQuestId`).
- Sistema base de NPC + tutorial event-driven por pasos.
- Biomas/áreas con metadata integrada en gameplay y UI (descripción + multiplicador de cosecha).

### Pendiente (siguiente iteración)
- Pulir highlights visuales avanzados de tutorial/objetivos (actualmente feedback textual).
- Quest chains más profundas, dailies avanzadas y tracking UI completo por categoría.
- Pulido visual de tutorial (highlights/UI contextuales avanzados).
- Integración de biomas en más pantallas secundarias y tutorial contextual.

## Arquitectura principal

- `GameManager`: ciclo de vida/autosave/apply diferido.
- `SaveManager`: `savegame.json` + compatibilidad por campos opcionales.
- `EconomyManager`: `Gold`, `Gems`, `Food`.
- `ProgressionManager`: `Level`, `XP`.
- `GardenManager`: parcelas, semillas, plantar/regar/cosechar, regrow/multi-harvest base, validación de plantas por bioma y bonus de venta por área, quests legacy, decoraciones persistidas.
- `GardenManager`: layout visual de parcelas configurable (grid compacto) + soporte de interacción directa por `PlotVisualController`.
- `MonsterManager`: workers, roles, energía/felicidad, offline production.
- `InventoryManager`: inventario transversal.
- `BuildingManager`: compra/colocación/construcción.
- `AreaManager`: áreas/biomas desbloqueables, metadata por área (`displayName`, descripción, `cropSellMultiplier`) y área activa.
- `DailyRewardManager`: claim diario con streak.
- `QuestManager`: quests por definición/estado con claim, chains, dailies y tracking reactivo por `GameEvents`.
- `DecorationManager`: placement mode, preview, snapping, validación de rango/límites/mapa/collisions, move/rotate/sell/delete + reconstrucción visual desde save.
- `NpcManager` + `TutorialManager`: diálogo simple y tutorial automático reactivo a `GameEvents`.
- `GameEvents`: bus de eventos gameplay (seed buy/plant/water/harvest, monster buy/collect, building/deco placed, daily claim).
- UI principal (`ShopPanel`, `SeedShopPanel`, `GardenPlotPanel`) con toasts de error/estado para feedback consistente.

## Datos persistidos (`SaveData`)

- `gold`, `gems`, `food`
- `level`, `xp`
- `monsters`, `totalProducedGold`
- `inventoryItems`
- `buildings`
- `areas`, `currentAreaId`
- `dailyRewardLastClaimUnix`, `dailyRewardStreak`
- `questRuntimeStates`, `tutorialStep`, `tutorialStatus`
- `plots`, `seedInventory`, `placedDecorations`, `activeQuests`
- `lastSaveUtc`

Compatibilidad: saves viejos sin campos nuevos cargan con defaults (listas null/valores 0), manteniendo loop jugable.

Corrección relevante: decoraciones guardadas con `plotId = -1` ahora se restauran correctamente (antes podían omitirse por validación excesiva al cargar).

## Montaje exacto de escena `Main`

### GameObjects requeridos

1. `GameManager` + `GameManager`
2. `Systems`
   - `EconomyManager`
   - `ProgressionManager`
   - `GardenManager`
   - `MonsterManager`
   - `InventoryManager`
   - `BuildingManager`
   - `AreaManager`
   - `DailyRewardManager`
   - `QuestManager`
   - `DecorationManager`
   - `NpcManager`
   - `TutorialManager`
3. `MainCamera` + `IsometricCameraController`
4. `Canvas` con paneles UI

### UI/Canvas y wiring de Inspector

- `HudPanel` + `HudController`
  - `goldText`, `gemsText`, `foodText`, `levelText`, `xpText` (`TMP_Text`)

- `GardenPlotPanel` (uno por parcela)
  - `plotId`
  - `defaultPlant` (`PlantDefinition`)
  - `stateText`, `timerText`, `actionText` (`TMP_Text`)
  - `primaryActionButton` -> `OnPrimaryActionPressed`
  - `buySeedButton` -> `OnBuyDefaultSeedPressed`

- `SeedShopPanel`
  - `selectedPlantNameText`, `selectedPlantCostText`, `selectedPlantOwnedSeedsText` (`TMP_Text`)
  - `buyButton` -> `BuySelectedSeed`

- `ShopPanel`
  - `tabTitleText`, `itemNameText`, `itemCostText`, `itemRequirementText`, `tooltipText` (`TMP_Text`)
  - botones tabs -> `SetTab(int)` (0 Seeds, 1 Monsters, 2 Decorations, 3 Buildings)
  - botones cantidades -> `SetBuyAmount(int)` (1/5/10)
  - botón comprar -> `BuySelected`

- `QuestListPanel`
  - `quest1Text`, `quest2Text` (`TMP_Text`)

- `QuestBoardPanel`
  - `questTitleText`, `questProgressText`, `questRewardText` (`TMP_Text`)
  - `claimButton` -> `ClaimSelected`

- `DecorationPanel`
  - `decorationsCountText`, `infoText` (`TMP_Text`)

- `MonsterWorkersPanel`
  - `workersCountText`, `producedGoldText`, `lastCollectText` (`TMP_Text`)
  - `collectAllButton` -> `OnCollectAllPressed`

- `InventoryPanel`
  - `inventoryText` (`TMP_Text`)

- `BuildingPanel`
  - `buildingListText` (`TMP_Text`)

- `AreaPanel`
  - `areaText` (`TMP_Text`)
  - botones área -> `OnSetArea(string areaId)`

- `TutorialNpcPanel`
  - `tutorialStepText`, `npcDialogueText` (`TMP_Text`)
  - botón avanzar tutorial -> `OnAdvanceTutorialPressed`
  - botón saltar tutorial (opcional) -> `OnSkipTutorialPressed`
  - botón hablar NPC -> `OnStartNpcDialogue(string npcId)`

- `ToastNotifier`
  - `toastText` (`TMP_Text`)

### Prefab visual de parcela (nuevo flujo recomendado)

1. Crear prefab `PlotVisual` con:
   - `SpriteRenderer` para suelo.
   - `Collider2D` (ej. `BoxCollider2D`) para click/tap.
   - `PlotVisualController`.
   - (Opcional) child vacío `CropVisualRoot`.
2. En `GardenManager` configurar:
   - `plotVisualPrefab` -> prefab `PlotVisual`.
   - `plotRoot` -> transform contenedor (opcional).
   - `plotStartPosition` -> esquina superior izquierda del bloque.
   - `plotColumns`, `plotRows` -> por ejemplo 3x2.
   - `plotSpacingX`, `plotSpacingY` -> separación horizontal/vertical.
3. En `PlotVisualController` del prefab:
   - `groundRenderer` -> sprite del suelo.
   - `cropVisualRoot` -> root de cultivo (opcional).
   - `plotCollider2D` -> collider del tile.
   - `defaultPlant` -> planta por defecto para acción contextual de plantar.
   - `plantVisuals` -> mapeo `plantId -> prefab` (opcional).
   - `fallbackCropPrefab` -> visual genérico si no hay mapeo específico.

> Este repositorio no incluye escenas/prefabs versionados; el wiring de inspector se hace manualmente en Unity.

## Smoke test manual

Agregar `GardenLoopSmokeTest` a un GameObject y ejecutar:
1. `Smoke/Buy Seed`
2. `Smoke/Plant`
3. `Smoke/Water`
4. `Smoke/Harvest`
5. `Smoke/CollectAll Monsters`
6. `Smoke/Save`
7. `Smoke/Reload`
8. `Smoke/SaveLoad Stress x20`
