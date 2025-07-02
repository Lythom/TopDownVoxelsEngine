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

2. **GameServer Extensions**:
   - Configures dependency injection
   - Handle blueprints related game events

## Data Flow

### Saving a Blueprint

1. Client creates a `SaveBlueprintEvent` with blueprint data
2. Server processes the event in HandleMessageAsync, `BlueprintService` serializes and saves the blueprint to database
3. Server broadcasts confirmation to sender about save state
4. Client display a short feedback to indicate save success or failure.

### Loading Blueprint List

1. Client creates a `LoadBlueprintListEvent` with pagination parameters
2. Server processes the event in HandleMessageAsync, `BlueprintService` retrieves and paginates blueprint metadata from db
3. Server sends `LoadBlueprintListResponseEvent` to requesting client
4. Client displays a selectable list

### Loading a Blueprint

1. Client creates a `LoadBlueprintEvent` with blueprint ID from select blueprint in the list
2. Server processes the event in HandleMessageAsync, `BlueprintService` retrieves the blueprint data
3. Server sends `LoadBlueprintResponseEvent` to requesting client
4. Client loads blueprint data in memory and merges blueprint area rendering with world rendering

### Placing a Blueprint

1. Client creates a `PlaceBlueprintEvent` with position and transformations
2. Server processes the event in HandleMessageAsync, `BlueprintService` loads the blueprint, applies transformations
3. Service modify the world accordingly
4. Server broadcasts BlueprintUpdateEvent to all clients with blockdata and positions required to update the world. 
Note: BlueprintUpdateEvent and BlueprintService should share the same code to update world from blueprint data.

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
