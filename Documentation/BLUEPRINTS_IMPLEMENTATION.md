# Blueprint System Implementation Guide

This document provides technical details about the Blueprint system implementation.

## Architecture Overview

The Blueprint system is implemented across the shared and server components, with hooks for client-side integration:

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Client    │      │    Shared   │      │    Server   │
│             │◄────►│             │◄────►│             │
└─────────────┘      └─────────────┘      └─────────────┘
```

### Shared Components

1. **Data Models**:
   - `BlueprintV0`: Full blueprint data
   - `BlueprintMetadataV0`: Lightweight listing data
   - `Symmetries`: Enum for transformation options

2. **Game Events**:
   - `SaveBlueprintCommand`: Save blueprint to server
   - `LoadBlueprintListQuery`: Request blueprint list
   - `LoadBlueprintListResponse`: Server response with blueprint list
   - `LoadBlueprintQuery`: Request specific blueprint
   - `LoadBlueprintResponse`: Server response with blueprint data
   - `PlaceBlueprintCommand`: Place blueprint in world

3. **Character Extensions**:
   - Added blueprint anchor position
   - Added blueprint size
   - Added active blueprint reference

### Server Components

1. **Blueprint Service**:
   - Manages blueprint storage and retrieval
   - Handles blueprint transformations

2. **GameServer Extensions**:
   - Configures dependency injection
   - Handle blueprints related game events

## Data Flow

### Saving a Blueprint

1. Client creates a `SaveBlueprintCommand` with blueprint data
2. Server processes the event in HandleMessageAsync, `BlueprintService` serializes and saves the blueprint to database
3. Server broadcasts confirmation to sender about save state
4. Client display a short feedback to indicate save success or failure.

### Loading Blueprint List

1. Client creates a `LoadBlueprintListQuery` with pagination parameters
2. Server processes the event in HandleMessageAsync, `BlueprintService` retrieves and paginates blueprint metadata from db
3. Server sends `LoadBlueprintListResponse` to requesting client
4. Client displays a selectable list

### Loading a Blueprint

1. Client creates a `LoadBlueprintQuery` with blueprint ID from select blueprint in the list
2. Server processes the event in HandleMessageAsync, `BlueprintService` retrieves the blueprint data
3. Server sends `LoadBlueprintResponse` to requesting client
4. Client loads blueprint data in memory and merges blueprint area rendering with world rendering

### Placing a Blueprint

1. Client creates a `PlaceBlueprintCommand` with position and transformations
2. Server processes the event in HandleMessageAsync, `BlueprintService` loads the blueprint, applies transformations
3. Service modify the world accordingly
4. Server broadcasts ChunkUpdateEvent to all clients with updated chunks. 

## Storage

Blueprints are stored as MessagePack-serialized data in the server's database.

## Blueprint Transformation

When placing a blueprint, these transformations can be applied:

1. **Flipping**: Applied first, inverting X and/or Z coordinates
2. **Rotation**: Applied second, rotating around the Y-axis
3. **Position Adjustment**: The anchor position is adjusted based on the blueprint's center

## Performance Considerations

- Blueprints are cached in memory for quick access
- Only metadata is sent when listing blueprints
- Full data is only transferred when specifically requested


---

## Client Implementation Plan

This section defines the Unity client-side implementation for the Blueprint system, aligned with existing shared models and network events.

Goals (scope)
- Browse and search blueprints (paged) via server APIs/events.
- Download and locally cache selected blueprints (metadata + full data).
- Preview placement with transformations (rotation, flips) and anchor controls.
- Place blueprint via server-validated request; apply server-sent updates.
- Create and save blueprints from a selected world region.
- Provide solid UX, performance, and error handling; cover with tests.

Primary dependencies
- MessagePack for C# (serialization) and UniTask (async).
- Existing shared types: BlueprintV0, BlueprintMetadataV0, Symmetries, CellArrayV0, BlockPathMapping, Vector3Int.
- Network events (Shared.Net): SaveBlueprintCommand, LoadBlueprintListQuery/Response, LoadBlueprintQuery/Response, PlaceBlueprintCommand.
- CharacterV0 signals: BlueprintAnchorPosition, BlueprintSize, ActiveBlueprintId.

High-level components
1) BlueprintClientService (non-MonoBehaviour)
   - Responsibilities: request/response orchestration over the existing network layer; in-memory LRU cache; disk cache (optional) using Application.persistentDataPath + MessagePack.
   - API (proposed):
     - UniTask<(BlueprintMetadataV0[] items, int total)> GetListAsync(int page, int pageSize, CancellationToken ct)
     - UniTask<BlueprintV0> GetAsync(Guid id, CancellationToken ct)
     - UniTask SaveAsync(string name, Vector3Int anchor, Vector3Int size, CancellationToken ct) // extracts world data via IWorldReadService
     - UniTask PlaceAsync(Guid id, Vector3Int position, byte rotation, Symmetries flips, CancellationToken ct)
   - Internals: Correlate requests by Id; include CharacterShortId; de-dupe concurrent requests for same id.

2) BlueprintToolController (MonoBehaviour)
   - Manages the current blueprint tool state (active blueprint id, rotation, flips, anchor offset).
   - Reads/writes CharacterV0 signals (BlueprintAnchorPosition/Size, ActiveBlueprintId).
   - Integrates with input bindings: rotate (Q/E), flip X/Z (F/H), cycle blueprints, confirm place (LMB), cancel (ESC/RMB).

3) BlueprintCatalogUI (UI Toolkit or uGUI)
   - Paged list/grid of BlueprintMetadataV0 with search/sort (client-side initially; server-side filter later).
   - Actions: Select -> triggers download if needed; Set Active -> enters preview mode; Save New -> opens capture dialog.
   - Uses Kenney blueprint panel assets already in Sources/Kenney for visual theme.

4) BlueprintPreviewRenderer
   - Renders a ghosted preview of BlueprintV0 at the intended world position.
   - Rendering strategies (choose based on existing renderer):
     - Voxel overlay mesh built from Cells + BlockMapping with a transparent "ghost" material, or
     - Per-voxel GPU instancing if already supported, or
     - Fallback: simple gizmo cubes for early milestone.
   - Applies transformations in order: flips (Symmetries), then rotation (Y 0/90/180/270), then translation to anchor.
   - Snaps Y using FloorHeight when anchor is unset; supports manual elevation.

5) ClientEngine implementation
    - Make it work when in LocalEngine mode

6) LocalCaching
   - In-memory LRU by Guid (e.g., capacity 16; configurable).
   - Optional disk cache keyed by blueprint Guid + pack version: MessagePack-serialize blueprint data to persistent path; validate by LastModifiedDate from metadata.

7) Error handling & telemetry
   - Surface network errors and validation messages in UI (toast/status bar).
   - Timeouts and retries with backoff for list/get.
   - Clear messages for size limit exceeded, unknown blueprint id, permission errors.

Networking flow (client perspective)
- List: send LoadBlueprintListQuery(page, pageSize) -> await LoadBlueprintListResponse -> update UI.
- Get: check cache; if miss, send LoadBlueprintQuery(id) -> await LoadBlueprintResponse -> cache + render preview.
- Place: PlaceBlueprintCommand(id, position, rotation, flips); server updates world and emits ChunkUpdateEvent -> apply to world; preview exits.
- Save: SaveBlueprintCommand(name, anchor, size); show success/failure feedback.

Transformation math (core logic)
- Maintain a TransformBlueprint utility:
  - Vector3Int Apply(Vector3Int p, Vector3Int size, byte rotation, Symmetries flips):
    - if flips has X: p.x = (size.x - 1) - p.x; if Z: p.z = (size.z - 1) - p.z
    - rotate around Y: 0: (x,z), 90: (z, size.x - 1 - x), 180: (size.x - 1 - x, size.z - 1 - z), 270: (size.z - 1 - z, x)
  - Iterate Cells and emit transformed positions for preview/placement visualization.
- Anchor: use CharacterV0.BlueprintAnchorPosition if set; otherwise compute center or honor FloorHeight to snap to ground.

UX details
- Catalog panel: pagination controls; hover shows size/floor height; double-click sets active + enters preview.
- Preview HUD: rotation/flips indicators; estimated block count; placement validity (red if colliding or out of bounds).
- Save dialog: pick size (defaults from CharacterV0.BlueprintSize), anchor choice (use current cursor block as anchor), name field; pre-validate size <= server MaxBlueprintSize (64).

Performance guidelines
- Cap preview voxel count (e.g., 64^3 max) and decimate visual if over budget.
- Build preview mesh asynchronously (UniTask) to avoid frame spikes; cache mesh per (id, rotation, flips) where feasible.

Testing strategy
- Unit tests (pure C#) for TransformBlueprint.Apply (all rotations/flips), anchor math, and size validations.
- PlayMode tests: preview rendering toggles, input rotation/flip, placement request emission.
- Integration tests (optional): mock network layer to feed Response/Update events and assert world changes.

Milestones & acceptance criteria
- M1 Catalog: list + select; downloads cache; shows metadata. (AC: paging and selection works; errors surfaced)
- M2 Preview: preview renderer with rotation/flip; anchor; valid/invalid state. (AC: visual matches transforms)
- M3 Place: send PlaceBlueprintCommand; apply ChunkUpdateEvent to world. (AC: world matches server update)
- M4 Save: capture region and SaveBlueprintCommand with pre-validation. (AC: save success/failure feedback)
- M5 Caching & polish: mesh caching, disk cache, UX refinements. (AC: reduced network and smooth UX)
- M6 Tests: unit + play mode coverage for core logic and flows. (AC: green tests in CI)

Notes & alignment
- Use existing CharacterShortId in all events; use request Id correlation consistent with other client messages.
- Keep code in Assets/VoxelsEngine/* (scripts) and Assets/Shared for shared models only.
- Respect C# 9, nullable enabled; document public APIs with XML comments; prefer immutable data where reasonable.
