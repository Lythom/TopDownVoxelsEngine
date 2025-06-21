# Blueprints System

This is a system that allows players to load and save a collection of blocks in an area.
Blueprints are saved server-side and shared between all players, with creator information preserved.

## Data Structure

- `Guid Id`: Unique identifier for the blueprint
- `string Name`: User-provided name
- `string CreatorId`: ID of the player who created the blueprint
- `DateTime CreationDate`: When the blueprint was first created
- `DateTime LastModifiedDate`: When the blueprint was last modified
- `Vec3Int Size`: Dimensions (always odd in X and Z so that a "center" block can be designated)
- `CellArrayV0 Cells`: Block data (same format as chunks, but can be bigger)
- `BlockPathMapping BlockMapping`: Block type references (same as GameState, but only containing blocks used in the blueprint)
- `int FloorHeight`: Metadata that indicates how deep to place the blueprint in the ground when spawned for level generation
- `Symmetries PossibleSymmetries`: Supported symmetry operations (None, XAxis, ZAxis, Both)

## Implementation Details

### Server-Side Implementation

- `BlueprintService`: Manages blueprint storage, retrieval, and manipulation
  - Stores blueprints in the filesystem using MessagePack serialization
  - Provides caching for quick access
  - Handles transformation and placement logic

- `GameServer Extensions`: Process blueprint-related game events and side effects

### Shared Data Models

- `BlueprintV0`: Complete blueprint data including blocks
- `BlueprintMetadataV0`: Lightweight version for listing without full block data
- `CharacterV0`: Extended with blueprint anchor position and active blueprint reference

### Events and Side Effects

- `SaveBlueprintEvent`: Save a blueprint to the server
- `LoadBlueprintListEvent` / `LoadBlueprintListResponseEvent`: Get paginated list of available blueprints
- `LoadBlueprintEvent` / `LoadBlueprintResponseEvent`: Get a specific blueprint by ID
- `PlaceBlueprintEvent`: Place a blueprint in the world with transformations

## Use Cases

### Place Anchor

1. Select blueprint tool
2. Left click a block: place/move the anchor using PlacementMode.CollidingBlock on aimed block
3. Change tool: remove anchor
4. The blueprint area is visualized when the anchor is placed: the anchor is the block below and centered from the area = ((size.x-1)/2,-1,(size.z-1)/2).

Design notes: Anchor position and blueprint configured size are stored and shared between client and server. Stored on CharacterV0.

### Change Blueprint Size

1. Place anchor
2. The area is visualized as a blue&white box. Handles on the sides makes it possible to extend the area.
3. Player can drag handles using left click to push or pull the side by steps of 2 blocks. Each step extends the area by 2 and moves the anchor to preserve the other sides' positions.

### Save Blueprint

1. Select the blueprint tool, place anchor
2. Use the save controller button
3. A modal asks for the name. Last used name is prefilled if any.
4. The server saves the blueprint with that name and associates it with the player. It updates the save date.

### Load Blueprint

1. Select the blueprint tool, place anchor
2. Use the load controller button
3. A window appears with the list of the 20 most recent blueprints, paginated by save date
4. When selecting a blueprint, it's loaded and ready to be placed at the anchor

### Place Blueprint

1. After loading a blueprint, it's previewed at the anchor position
2. Optional: Rotate the blueprint (0°, 90°, 180°, 270°) or apply symmetry operations (flip X, flip Z)
3. Confirm placement to apply all blocks to the world

## Transformations

Blueprints support the following transformations:

1. Rotation: 0°, 90°, 180°, or 270° around the Y-axis
2. Flipping: Along X-axis, Z-axis, or both

These transformations are applied when placing a blueprint in the world, not when saving.
