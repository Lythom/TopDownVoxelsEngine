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
   - `SaveBlueprintEvent`: Save blueprint to server
   - `LoadBlueprintListEvent`: Request blueprint list
   - `LoadBlueprintListResponseEvent`: Server response with blueprint list
   - `LoadBlueprintEvent`: Request specific blueprint
   - `LoadBlueprintResponseEvent`: Server response with blueprint data
   - `PlaceBlueprintEvent`: Place blueprint in world

3. **Character Extensions**:
   - Added blueprint anchor position
   - Added blueprint size
   - Added active blueprint reference

### Server Components

1. **Blueprint Service**:
   - Manages blueprint storage and retrieval
   - Handles blueprint transformations
   - Processes blueprint-related side effects

2. **GameServer Extensions**:
   - Registers blueprint side effect handlers
   - Configures dependency injection

## Data Flow

### Saving a Blueprint

1. Client creates a `SaveBlueprintEvent` with blueprint data
2. Event is applied optimistically on client and sent to server
3. Server processes the event in HandleMessageAsync
4. `BlueprintService` serializes and saves the blueprint to database
5. Server broadcasts confirmation to all clients

### Loading Blueprint List

1. Client creates a `LoadBlueprintListEvent` with pagination parameters
2. Event is sent to server
3. Server processes the event, triggering `LoadBlueprintListSideEffect`
4. `BlueprintService` retrieves and paginates blueprint metadata from db
5. Server sends `LoadBlueprintListResponseEvent` to requesting client

### Loading a Blueprint

1. Client creates a `LoadBlueprintEvent` with blueprint ID
2. Event is sent to server
3. Server processes the event, triggering `LoadBlueprintSideEffect`
4. `BlueprintService` retrieves the blueprint data
5. Server sends `LoadBlueprintResponseEvent` to requesting client

### Placing a Blueprint

1. Client creates a `PlaceBlueprintEvent` with position and transformations
2. Event is sent to server
3. Server processes the event, triggering `PlaceBlueprintSideEffect`
4. `BlueprintService` loads the blueprint, applies transformations
5. Service creates individual `PlaceBlockEvent`s for each block
6. Server broadcasts these events to all clients

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
- Placement generates individual block events rather than bulk operations

