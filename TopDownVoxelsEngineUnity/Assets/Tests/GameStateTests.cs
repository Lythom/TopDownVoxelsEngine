using System.Collections.Generic;
using NUnit.Framework;
using Shared;

namespace Tests {
    public class GameStateTests {
        [Test]
        public void UpdateBlockMapping_WhenRegistryMatchesBlockPathById_ShouldMapCorrectly() {
            // Arrange
            var blockPathById = new string?[ushort.MaxValue];
            blockPathById[0] ="Air";
            blockPathById[1] ="Concrete.json";
            blockPathById[2] ="Grass.json";
            blockPathById[3] ="Ground.json";
            blockPathById[4] ="Sand.json";
            blockPathById[5] ="Stone.json";
            blockPathById[6] ="Stone2.json";
            blockPathById[7] ="Air";
            blockPathById[8] ="Wood.json";

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

            var gameState = new GameState(null, null, blockPathById);

            // Act
            gameState.UpdateBlockMapping(registry);

            // Assert
            Assert.AreEqual("Air", gameState.BlockPathById[0]);
            Assert.AreEqual("Stone", gameState.BlockPathById[1]);
            Assert.AreEqual("Dirt", gameState.BlockPathById[2]);

            Assert.AreEqual(0, gameState.BlockIdByPath["Air"]);
            Assert.AreEqual(1, gameState.BlockIdByPath["Stone"]);
            Assert.AreEqual(2, gameState.BlockIdByPath["Dirt"]);
        }

        [Test]
        public void UpdateBlockMapping_WhenRegistryIsMissingBlock_ShouldReplaceWithAir() {
            // Arrange
            var blockPathById = new string?[ushort.MaxValue];
            blockPathById[0] ="Air";
            blockPathById[1] ="Concrete.json";
            blockPathById[2] ="Grass.json";
            blockPathById[3] ="Ground.json";
            blockPathById[4] ="Sand.json";
            blockPathById[5] ="Stone.json";
            blockPathById[6] ="Stone2.json";
            blockPathById[7] ="Air";
            blockPathById[8] ="Wood.json";

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

            var gameState = new GameState(null, null, blockPathById);

            // Act
            gameState.UpdateBlockMapping(registry);

            // Assert
            Assert.AreEqual("Air", gameState.BlockPathById[0]);
            Assert.AreEqual("Air", gameState.BlockPathById[1]); // Stone should be replaced with Air
            Assert.AreEqual("Dirt", gameState.BlockPathById[2]);

            Assert.AreEqual(0, gameState.BlockIdByPath["Air"]);
            Assert.IsFalse(gameState.BlockIdByPath.ContainsKey("Stone"));
            Assert.AreEqual(2, gameState.BlockIdByPath["Dirt"]);
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

            var gameState = new GameState(null, null, blockPathById);

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

        public BlockConfigJson Get(string path) {
            if (_blocks.TryGetValue(path, out var block)) {
                return block;
            }

            return null;
        }
    }

}