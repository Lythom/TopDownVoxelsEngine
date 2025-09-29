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

- `GameServer Extensions`: Process blueprint-related game events

### Shared Data Models

- `BlueprintV0`: Complete blueprint data including blocks
- `BlueprintMetadataV0`: Lightweight version for listing without full block data
- `CharacterV0`: Extended with blueprint anchor position and active blueprint reference

### Events

- `SaveBlueprintCommand`: Save a blueprint to the server
- `LoadBlueprintListQuery` / `LoadBlueprintListResponse`: Get paginated list of available blueprints
- `LoadBlueprintQuery` / `LoadBlueprintResponse`: Get a specific blueprint by ID
- `PlaceBlueprintCommand`: Place a blueprint in the world with transformations

## Tool Modes and Controls

The Blueprint tool has two modes. The player experience is built around a single main action and one mode switch:

- Main action: UseTool (typically left-click / press; supports press, press-and-hold, and drag)
- Mode switch: ChangeItem (key at the E layout position) toggles between modes

Modes:
1) Blueprint Save mode
2) Blueprint Place mode

On-screen gizmos and diegetic UI appear contextually and are operated exclusively with UseTool.

Notes:
- A short press is referred to as “UseTool press”.
- A press-and-hold followed by movement is referred to as “UseTool drag”.

## Use Cases

### Blueprint Save mode (ChangeItem to enter/exit)

Goal: Capture a region of the world and save it as a shared blueprint.

1) Place or move anchor
   - UX: Aim at any world block and UseTool press.
   - Result: An anchor is placed at the aimed block using PlacementMode.CollidingBlock. The configurable area is visualized as a box centered so that the anchor is the block at offset ((size.x-1)/2, -1, (size.z-1)/2) from the area.
   - Persistence: Anchor position and current size are stored on the character and synchronized.

2) Adjust capture size
   - UX: UseTool press on any visible resize handle to select it, then UseTool drag to push/pull.
   - Behavior: Size changes in steps of 2 blocks. The anchor auto-shifts to preserve the opposite sides’ positions.
   - Feedback: Live update of the box with dimensions;

3) Save the blueprint
   - UX: UseTool press on the floating “Save” control near the anchor (or inside the area HUD).
   - Flow:
     - A modal appears with a Name input field (prefilled with last used name). Confirm and cancel are UseTool press on their respective buttons.
     - On confirm, a Save action is sent to the server. A toast shows success or error feedback.
   - Result: The server saves and timestamps the blueprint, associated with the creator.

4) Exit Save mode
   - UX: ChangeItem to switch to Blueprint Place mode or another tool. When the tool is inactive, any temporary gizmos are hidden.

Design notes:
- The anchor position and blueprint size are stored and shared between client and server (on CharacterV0).
- The area is always centered to maintain odd X/Z so a center block exists.

### Blueprint Brush mode (ChangeItem to enter/exit)

Goal: Browse/select a saved blueprint, preview it at the anchor, transform, and place it.

1) Place or move anchor for placement
   - UX: Aim a world block and UseTool press to set/move the anchor. The preview will align to this anchor when a blueprint is selected.

2) Open catalog and select a blueprint
   - UX: UseTool press on the floating “Catalog” control near the anchor (or a dedicated HUD button).
   - Flow:
     - A panel opens listing the 20 most recent blueprints by save date, with pagination controls operated by UseTool press.
     - UseTool press on an item selects and downloads it if needed, then closes the panel and enters preview.
   - Result: The chosen blueprint is set as active and shown as a ghosted preview at the anchor.

3) Adjust transformations (preview)
   - Rotate: UseTool press on on-screen Rotate controls to cycle rotation (0°, 90°, 180°, 270°).
   - Flip: UseTool press on Flip X / Flip Z controls to toggle symmetry operations.
   - Elevation snap: If applicable, UseTool press on Up/Down nudge controls to raise/lower the preview; default snaps using FloorHeight when relevant.
   - Validity: The preview indicates validity (green) or conflicts (red) based on placement rules.

4) Place the blueprint
   - UX: UseTool press on the “Place” control (appears only when valid).
   - Result: A place command is sent to the server; the world updates upon server confirmation, and the preview exits.

5) Change or clear the active blueprint
   - UX: UseTool press on “Change” to reopen the catalog; UseTool press on “Clear” to remove active selection and hide the preview.

6) Exit Place mode
   - UX: ChangeItem to switch to Blueprint Save mode or another tool.

## Transformations

Blueprints support the following transformations:

1) Rotation: 0°, 90°, 180°, or 270° around the Y-axis
2) Flipping: Along X-axis, Z-axis, or both

These transformations are applied when placing a blueprint in the world, not when saving.
