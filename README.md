# MonsterWorldLike (Unity Starter)

Starter 2D isométrico enfocado en **garden sim** (sembrar -> crecer -> cosechar -> vender/progreso), inspirado en la estructura jugable general de juegos de jardín casual, sin reutilizar IP/arte/textos de terceros.

## Núcleo actual

- `GameManager`: ciclo de vida global, autosave, save en pause/focus/quit y apply diferido de datos cargados.
- `SaveManager`: persistencia JSON local en `Application.persistentDataPath/savegame.json` con dirty-flag y dos fases (leer disco / aplicar a escena).
- `EconomyManager`: monedas (`Gold`, `Gems`, `Food`).
- `GardenManager`: parcelas, semillas, plantado, riego, cosecha, desbloqueo de parcelas, inventario de semillas, decoraciones básicas y quests activas.
- `ProgressionManager`: `Level` y `XP`.
- `HudController`: HUD de `Gold/Gems/Food/Level/XP`.
- `GardenPlotPanel`: UI por parcela (estado, tiempo restante, acción principal).
- `SeedShopPanel`: tienda mínima para comprar semillas desde UI.
- `QuestListPanel`: muestra 1-2 pedidos/quests activos y progreso.
- `IsometricCameraController`: cámara táctil isométrica (pan/zoom).

## Datos persistidos

`SaveData` guarda:
- `gold`, `gems`, `food`
- `level`, `xp`
- `plots`
- `seedInventory`
- `placedDecorations`
- `activeQuests`
- `lastSaveUtc`

## Flujo principal

1. Comprar semilla (`GardenManager.BuySeed`).
2. Plantar en parcela desbloqueada (`PlantSeed`).
3. Regar (`Water`) para habilitar la cosecha del cultivo plantado.
4. Esperar `growSeconds` (timestamps Unix para progreso offline).
5. Cosechar (`Harvest`) para ganar `sellValue` en oro y `xpReward` en XP.
6. Completar pedidos/quests simples (base incluida) y desbloquear más progreso.

## Escena mínima recomendada

- GameObject `GameManager` + `GameManager`
- GameObject `EconomyManager` + `EconomyManager`
- GameObject `ProgressionManager` + `ProgressionManager`
- GameObject `GardenManager` + `GardenManager`
- Canvas con `HudController`
- Uno o más `GardenPlotPanel` (uno por `plotId`)
- Panel `SeedShopPanel` enlazado a un catálogo de plantas
- Panel `QuestListPanel` con 2 `TMP_Text` para quests activas
- Cámara con `IsometricCameraController`

## TODOs próximos

- UI de tienda de semillas completa (grid de catálogo).
- Sistema visual de decoraciones colocables.
- Balanceo de curvas de XP/desbloqueos por nivel.
- Quests por cadena con refresh diario.
- Balanceo de starter seeds y nivel mínimo de desbloqueo de parcelas.
