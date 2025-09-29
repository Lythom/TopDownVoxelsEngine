using System.Collections.Generic;
using NUnit.Framework;
using Shared;
using MessagePack;
using VoxelsEngine;

namespace Tests {
    public class GameStateTests {
        [Test]
        public void UpdateBlockMapping_WhenRegistryMatchesBlockPathById_ShouldMapCorrectly() {
            // Arrange
            var blockPathById = new string?[ushort.MaxValue];
            blockPathById[0] = "Air";
            blockPathById[1] = "Concrete.json";
            blockPathById[2] = "Grass.json";
            blockPathById[3] = "Ground.json";
            blockPathById[4] = "Sand.json";
            blockPathById[5] = "Stone.json";
            blockPathById[6] = "Stone2.json";
            blockPathById[7] = "Air";
            blockPathById[8] = "Wood.json";

            var registry = new MockBlockRegistry();
            var d = registry.Get();
            d.Add("Air", new BlockConfigJson());
            d.Add("Concrete.json", new BlockConfigJson());
            d.Add("Grass.json", new BlockConfigJson());
            d.Add("Ground.json", new BlockConfigJson());
            d.Add("Sand.json", new BlockConfigJson());
            d.Add("Stone.json", new BlockConfigJson());
            d.Add("Stone2.json", new BlockConfigJson());
            d.Add("WoodMissing.json", new BlockConfigJson());

            var gameState = new GameState(new(null, null, blockPathById));

            // Act
            gameState.UpdateBlockMapping(registry);

            // Assert
            Assert.AreEqual("Air", gameState.BlockPathById[0]);
            Assert.AreEqual("Concrete.json", gameState.BlockPathById[1]);
            Assert.AreEqual("Grass.json", gameState.BlockPathById[2]);

            Assert.AreEqual(0, gameState.BlockIdByPath["Air"]);
            Assert.AreEqual(1, gameState.BlockIdByPath["Concrete.json"]);
            Assert.AreEqual(2, gameState.BlockIdByPath["Grass.json"]);
        }

        [Test]
        public void UpdateBlockMapping_WhenRegistryIsMissingBlock_ShouldReplaceWithFirstBlock() {
            // Arrange
            var blockPathById = new string?[ushort.MaxValue];
            blockPathById[0] = "Air";
            blockPathById[1] = "Concrete.json";
            blockPathById[2] = "Grass.json";
            blockPathById[3] = "Ground.json";
            blockPathById[4] = "Sand.json";
            blockPathById[5] = "Stone.json";
            blockPathById[6] = "Stone2.json";
            blockPathById[7] = "Air";
            blockPathById[8] = "Wood.json";

            var registry = new MockBlockRegistry();
            var d = registry.Get();
            d.Add("Air", new BlockConfigJson());
            d.Add("Concrete.json", new BlockConfigJson());
            d.Add("Grass.json", new BlockConfigJson());
            d.Add("Ground.json", new BlockConfigJson());
            d.Add("Sand.json", new BlockConfigJson());
            d.Add("Stone.json", new BlockConfigJson());
            d.Add("Stone2.json", new BlockConfigJson());
            d.Add("WoodMissing.json", new BlockConfigJson());

            var gameState = new GameState(new(null, null, blockPathById));

            // Act
            gameState.UpdateBlockMapping(registry);

            // Assert
            Assert.AreEqual("Air", gameState.BlockPathById[0]);
            Assert.AreEqual("Concrete.json", gameState.BlockPathById[1]); // Stone should be replaced with Air
            Assert.AreEqual("Grass.json", gameState.BlockPathById[2]);

            Assert.AreEqual(0, gameState.BlockIdByPath["Air"]);
            Assert.IsFalse(gameState.BlockIdByPath.ContainsKey("Stone"));
            Assert.AreEqual(2, gameState.BlockIdByPath["Grass.json"]);
        }

        [Test]
        public void UpdateBlockMapping_WhenBlockPathByIdIsMissingBlock_ShouldAddNewBlock() {
            // Arrange
            var blockPathById = new string?[ushort.MaxValue];
            blockPathById[0] = "Air";
            blockPathById[1] = "Stone";
            // Dirt is missing from blockPathById

            var registry = new MockBlockRegistry();
            registry.Get().Add("Air", new BlockConfigJson());
            registry.Get().Add("Stone", new BlockConfigJson());
            registry.Get().Add("Dirt", new BlockConfigJson());

            var gameState = new GameState(new(null, null, blockPathById));

            // Act
            gameState.UpdateBlockMapping(registry);

            // Assert
            Assert.AreEqual("Air", gameState.BlockPathById[0]);
            Assert.AreEqual("Stone", gameState.BlockPathById[1]);
            Assert.AreEqual("Dirt", gameState.BlockPathById[2]); // Dirt should be added at next available index

            Assert.AreEqual(0, gameState.BlockIdByPath["Air"]);
            Assert.AreEqual(1, gameState.BlockIdByPath["Stone"]);
            Assert.AreEqual(2, gameState.BlockIdByPath["Dirt"]);
        }

        [Test]
        public void SerializeDeserialize_GameState_ShouldPreserveAllProperties() {
            // Arrange - Create a game state with test data
            var blockPathById = new string?[ushort.MaxValue];
            blockPathById[0] = "Air";
            blockPathById[1] = "Stone";
            blockPathById[2] = "Dirt";

            var blockIdByPath = new Dictionary<string, ushort> {
                ["Air"] = 0,
                ["Stone"] = 1,
                ["Dirt"] = 2
            };

            // Create a level map
            var levelMap = new LevelMap("TestWorld", new Vector3(100, 10, 100));

            // Add a chunk to the level
            var chunk = new Chunk {IsGenerated = true};
            chunk.Cells = new CellV0[Chunk.Size, Chunk.Height, Chunk.Size];

            // Set some test blocks in the chunk
            for (int x = 0; x < Chunk.Size; x++) {
                for (int y = 0; y < Chunk.Height; y++) {
                    for (int z = 0; z < Chunk.Size; z++) {
                        chunk.Cells[x, y, z] = new CellV0 {Block = (ushort) ((y < 5) ? 1 : 0)}; // Stone below y=5, Air above
                    }
                }
            }

            levelMap.Chunks[10, 10] = chunk;

            // Create a character
            var character = new CharacterV0(
                "TestPlayer",
                new Vector3(100, 15, 100),
                Vector3.zero,
                1,
                new("TestWorld"),
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null
            );

            // Create the game state
            var originalState = new GameState(new(null, null, blockPathById));
            originalState.Levels.Add("TestWorld", levelMap);
            originalState.Characters.Add(1, character);

            // Act - Serialize and deserialize
            var serializedData = MessagePackSerializer.Serialize((IGameData) originalState.GameData, Configurator.MessagePackOptions);
            var deserializedState = new GameState(IGameData.DeserializedUpdated(serializedData, Configurator.MessagePackOptions));

            // Assert - Verify all properties were correctly preserved

            // Check block mappings
            Assert.AreEqual(originalState.BlockPathById[0], deserializedState.BlockPathById[0]);
            Assert.AreEqual(originalState.BlockPathById[1], deserializedState.BlockPathById[1]);
            Assert.AreEqual(originalState.BlockPathById[2], deserializedState.BlockPathById[2]);

            Assert.AreEqual(originalState.BlockIdByPath["Air"], deserializedState.BlockIdByPath["Air"]);
            Assert.AreEqual(originalState.BlockIdByPath["Stone"], deserializedState.BlockIdByPath["Stone"]);
            Assert.AreEqual(originalState.BlockIdByPath["Dirt"], deserializedState.BlockIdByPath["Dirt"]);

            // Check levels
            Assert.IsTrue(deserializedState.Levels.ContainsKey("TestWorld"));
            var deserializedLevel = deserializedState.Levels["TestWorld"];
            Assert.AreEqual(levelMap.LevelId, deserializedLevel.LevelId);
            Assert.AreEqual(levelMap.SpawnPosition, deserializedLevel.SpawnPosition);

            // Check chunk data
            var deserializedChunk = deserializedLevel.Chunks[10, 10];
            Assert.IsTrue(originalState.Levels["TestWorld"].Chunks[10, 10].IsGenerated);
            Assert.IsTrue(deserializedChunk.IsGenerated);
            Assert.IsNotNull(deserializedChunk.Cells);

            // Sample check a few blocks
            Assert.AreEqual(1, deserializedChunk.Cells![5, 3, 5].Block); // Should be Stone (1)
            Assert.AreEqual(0, deserializedChunk.Cells[5, 10, 5].Block); // Should be Air (0)

            // Check character data
            Assert.IsTrue(deserializedState.Characters.ContainsKey(1));
            var deserializedCharacter = deserializedState.Characters[1];
            Assert.AreEqual("TestPlayer", deserializedCharacter.Name);
            Assert.AreEqual(new Vector3(100, 15, 100), deserializedCharacter.Position);
            Assert.AreEqual("TestWorld", deserializedCharacter.Level.Value);
        }
    }

    // Mock implementation of the registry
    public class MockBlockRegistry : IRegistry<BlockConfigJson> {
        private readonly Dictionary<string, BlockConfigJson> _blocks = new();

        public MockBlockRegistry(params string[] blockPaths) {
            foreach (var path in blockPaths) {
                _blocks[path] = new BlockConfigJson();
            }
        }

        public Dictionary<string, BlockConfigJson> Get() {
            return _blocks;
        }

        public BlockConfigJson? Get(string path) {
            if (_blocks.TryGetValue(path, out var block)) {
                return block;
            }

            return null;
        }
    }
}