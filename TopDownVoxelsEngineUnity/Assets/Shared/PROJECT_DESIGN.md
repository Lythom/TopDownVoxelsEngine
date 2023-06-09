# VoxelsEngine Project (codename)

Informations about the project:

- A game using unity engine, using voxels as world data and rendering
- The player can move around, jump, place block
- The project uses MessagePack for C# with AOT Code Generation for serialization / deserialization. Version 2.5.108
- The project uses UniTask for Task management and async operations. Version 2.3.3
- The game support networked multiplayer using the networking strategy described
  at https://gafferongames.com/post/state_synchronization/

## Code architecture

The project is split in 4 parts:

- The client (A Unity project, .net 2.1 Mono)
- The server (a .net core project)
- Shared code (a library project, compiled as .net core by the server and .net 2.1 Mono by the client)
- Test projects (for unit tests of the shared and server code).

Language version is C# 9 for the whole project.

### Multiplayer, State changes and networking

The networking strategy implies that both server and client can run the simulation in a similar way. This is what the
Shared project library is for.
The Shared project defines :

- GameState: The data model that can store a whole game.
- Actions (called GameEvent): The events can be applied to the game state to make it change. Only actions are allowed to
  write the state.

GameEvents works as follow :

```csharp
public abstract class GameEvent : IGameEvent, INetworkMessage
{
    public abstract int GetId();

    // public api cannot be overriden, it simply use the GameState API to apply itself
    public void Apply(GameState gameState, SideEffectManager? sideEffectManager) {
        gameState.ApplyEvent(DoApply, sideEffectManager);
    }

    // Method implemented by each event and only portion of the application to modify the GameState
    protected internal abstract void DoApply(GameState gameState, SideEffectManager? sideEffectManager);
    
    // Mostly for developper experience, express the assertions required for the event to succeed.
    // It helps the develop knows the presequites for this GameEvent to be applied;
    public abstract void AssertApplicationConditions(GameState gameState);
}
```

GameEvents are produced by the client when interacting with the game.
Any input that would change the GameState triggers a GameEvent that is :

- dispatched to server,
- applied immediately on the client (optimistic update).

In case the event is accepted by the server, the GameEvent is broadcast to all players (including the sender) using the
same id.
If the client is the sender (id in the sentbox), it will not re-apply the event.

The server will continuously provide state update of player / npc / elements positions and velocities, with a
prioritization effort to update
near data more frequently.

Prioritization is made using a priority accumulator for each state entry (using encaplusated data) EXCEPT for blocks.

Mostly, each client will run it's own simulation at it's own pace (as long as it is not too far away) and the server
will send both input updates (game event to apply) and state updated (values to override).
The client catches up out of sync content on the go.
Game ticks are run at 50 ticks/seconds = 20 ms per tick, are applied as a TickGameEvent, and are implicitly executed by
all clients and server.

Every 10 seconds, the server will try a full reconciliation. The idea is to bundle all the "near state" of each player, and send :
- id of the state (TickId)
- List of events to (re) apply on it
The client can then re apply sent event since this tick that the server did not ack.

#### Example: position synchronisation

https://sequencediagram.org/index.html#initialData=A4QwTgLglgxloDsIAIDCALcIYQKZgEEBzXJAKFElnhCTQBspSIBRBIqBXC8aORFAGV8AN1wBXMGQxYc+YswC0APmUyw2PIRJIAXMgDaAVWAATEHgC6yADJQU9AOSdg4lKfHIAVgHtxEqXVNeR0IFTVMDTltZn1jMwtcawBZXBQAA+8-MGQRe1wAZwLcZDTkWiJ6EtJkADMfBBwoBuRTQuQXNwLpSOCYpHDURmY2Di44k3MrZAAVDQQCgFsyvLwi6pQKqulhpFHOXEHd1nYD-VQQehhxKuR6EGQEPzF6W+AfAvtmhGRMmEwAI7+ZB8ADWOyYe1OXEGvWiCj0s3mSzK90ez1wrxK70+0AaPVkWgRYVUQXhoTiADEoAAPXCmSaJawEWq1WDoEpvfAFBoIEAkcqeABGLVIpjAPnsEJG0MOqmEYDEkn0bBEkpK4h+yyK-JKIDEMGQOK+DQANLl8utzVtcAAdBB-QHAsHIAAUIGAYAAFwVkDBLtd6ABKMgKpVgcJhgLnAM3TkPJ7iF5vD4mn6OkBAkpg0OiAJHSEnMa4fQAIQlIFM-oKDgexrxPzaIL8vqqvuA3I+v2b4lb7Rgx26Q0L+xhpOOo5LyFSGSykiNqYbpRQq0KxVa7Q7YB5vvtggIRkpC-nt18-nnNRg2V9rtMAEuvJlMmewCGgA

There is 3 levels of synchronisation. In the end what matters is that the client displays the object at the right
location :

- The server take the current velocity and applies it to the position. It sends the position and velocity to clients so
  that they improve their predictions and update their state.
- The client read from input, generate an event, send to server and applies to the state during the fixed update to
  anticipate the server latency.
- The client read from input and immediately updates the CharacterAgent. It keeps interpolating from the last fixedUpdate GameState value toward the expected position.

Other example: block placement

- The server take the changelist of blocks from all players input and the missing data of clients
    - if a client query a chunk, the whole chunk data is sent.
- The client read from input, generate an event, send to erver and applied to the state during fixed update to
  anticipate the server latency.
- There is no immediate update, an animated VFX is played immediatly to cover the latency

### Code safety

- Both client and servers are configured with null safety enabled by default
    - On the client, this is done by settings csc.rsp in the VoxelsEngineUnity/Assets/Shared and
      VoxelsEngineUnity/Assets/Scripts/VoxelsEngine folder:
      ``` 
      -nullable
      ```
    - On the server, this is done by configuring the csproject file:
      ```xml
      <PropertyGroup>
          …
          <Nullable>enable</Nullable>
      </PropertyGroup>
      ```
    - On the server, it's possible to finetune specific folders using "Directory.Build.props" in folders :
      ```xml
      <Project>
        <PropertyGroup>
          <Nullable>disabled</Nullable>
        </PropertyGroup>
      </Project>
      ```
    - For third party code that would get caught in the nullable enabled directive while no written this way, #nullable
      disabled is add as first line in all files.
    - Most third party code is
- The client uses Odin validator to enforce safety in the Scenes and gameobject.
- To allow shared and serve project to support odin attributes, following configuration was added in the project
    ```xml
    <Reference Include="Sirenix.OdinInspector.Attributes, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null">
        <HintPath>..\Plugins\Sirenix\Assemblies\Sirenix.OdinInspector.Attributes.dll</HintPath>
    </Reference>
    ```

### Files architecture

- VoxelsEngine
    - VoxelsEngineUnity (client project)
        - .config
        - .idea
        - Assets
            - Shared (shared library project used by both client and server)
            - Scripts
                - VoxelsEngine (most client script)
                - Tests (Unity test project)
            - Plugins (Third party code)
        - Documentation
        - Packages
        - ProjectSettings
        - UserSettings
    - VoxelsEngineServer (server project)

## Rendering (Client side)

## Networking (Server and client)

## Gameplay