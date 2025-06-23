using System;
using NUnit.Framework;
using Server.Services;
using Shared;

namespace Server.Tests {
    public class BlueprintServiceTests {
        private static readonly object[] FlipTestCases = {
            // Format: x, z, sizeX, sizeZ, flipOperation, expectedX, expectedZ
            new object[] {0, 0, 3, 3, Symmetries.None, 0, 0},
            new object[] {0, 0, 3, 3, Symmetries.XAxis, 2, 0},
            new object[] {0, 0, 3, 3, Symmetries.ZAxis, 0, 2},
            new object[] {2, 2, 3, 3, Symmetries.XAxis, 0, 2},
            new object[] {2, 2, 3, 3, Symmetries.ZAxis, 2, 0},
        };

        private static readonly object[] RotationTestCases = {
            // Format: x, z, sizeX, sizeZ, rotation, expectedX, expectedZ
            // Test with 3x3 blueprint
            new object[] {0, 0, 3, 3, (byte) 0, 0, 0}, // No rotation
            new object[] {0, 0, 3, 3, (byte) 1, 2, 0}, // 90° clockwise
            new object[] {0, 0, 3, 3, (byte) 2, 2, 2}, // 180°
            new object[] {0, 0, 3, 3, (byte) 3, 0, 2}, // 270° clockwise
            new object[] {2, 2, 3, 3, (byte) 0, 2, 2}, // Corner point, no rotation
            new object[] {2, 2, 3, 3, (byte) 1, 0, 2}, // Corner point, 90° clockwise
            new object[] {2, 2, 3, 3, (byte) 2, 0, 0}, // Corner point, 180°
            new object[] {2, 2, 3, 3, (byte) 3, 2, 0}, // Corner point, 270° clockwise
            // Test with 5x5 blueprint to verify center calculation
            new object[] {0, 0, 5, 5, (byte) 1, 4, 0}, // 90° clockwise
            new object[] {4, 4, 5, 5, (byte) 1, 0, 4}, // Corner point, 90° clockwise
        };

        private static readonly object[] CombinedTransformationTestCases = {
            // Format: x, z, sizeX, sizeZ, rotation, flipOperation, expectedX, expectedZ
            new object[] {0, 0, 3, 3, (byte) 1, Symmetries.XAxis, 0, 0}, // 90° + flip X
            new object[] {0, 0, 3, 3, (byte) 1, Symmetries.ZAxis, 2, 2}, // 90° + flip Z
            new object[] {2, 2, 3, 3, (byte) 2, Symmetries.XAxis, 2, 0}, // 180° + flip X
            new object[] {2, 2, 3, 3, (byte) 3, Symmetries.ZAxis, 2, 2}, // 270° + flip Z
        };

        [Test]
        [TestCaseSource(nameof(FlipTestCases))]
        public void ApplyTransformations_Flip_TransformsCorrectly(
            int x,
            int z,
            int sizeX,
            int sizeZ,
            Symmetries flipOperation,
            int expectedX,
            int expectedZ
        ) {
            // Act
            var (actualX, actualZ) = BlueprintService.ApplyTransformations(
                x, z, sizeX, sizeZ, 0, flipOperation);

            // Assert
            Assert.That(actualX, Is.EqualTo(expectedX), "X coordinate was not transformed correctly");
            Assert.That(actualZ, Is.EqualTo(expectedZ), "Z coordinate was not transformed correctly");
        }

        [Test]
        [TestCaseSource(nameof(RotationTestCases))]
        public void ApplyTransformations_Rotate_TransformsCorrectly(
            int x,
            int z,
            int sizeX,
            int sizeZ,
            byte rotation,
            int expectedX,
            int expectedZ
        ) {
            // Act
            var (actualX, actualZ) = BlueprintService.ApplyTransformations(
                x, z, sizeX, sizeZ, rotation, Symmetries.None);

            // Assert
            Assert.That(actualX, Is.EqualTo(expectedX), "X coordinate was not transformed correctly");
            Assert.That(actualZ, Is.EqualTo(expectedZ), "Z coordinate was not transformed correctly");
        }

        [Test]
        [TestCaseSource(nameof(CombinedTransformationTestCases))]
        public void ApplyTransformations_Combined_TransformsCorrectly(
            int x,
            int z,
            int sizeX,
            int sizeZ,
            byte rotation,
            Symmetries flipOperation,
            int expectedX,
            int expectedZ
        ) {
            // Act
            var (actualX, actualZ) = BlueprintService.ApplyTransformations(
                x, z, sizeX, sizeZ, rotation, flipOperation);

            // Assert
            Assert.That(actualX, Is.EqualTo(expectedX), "X coordinate was not transformed correctly");
            Assert.That(actualZ, Is.EqualTo(expectedZ), "Z coordinate was not transformed correctly");
        }

        [Test]
        public void ApplyTransformations_InvalidRotation_HandlesModulo() {
            // Arrange
            const int x = 0;
            const int z = 0;
            const int size = 3;
            const byte rotation = 5; // Should be equivalent to rotation = 1

            // Act
            var (actualX, actualZ) = BlueprintService.ApplyTransformations(
                x, z, size, size, rotation, Symmetries.None);
            var (expectedX, expectedZ) = BlueprintService.ApplyTransformations(
                x, z, size, size, 1, Symmetries.None);

            // Assert
            Assert.That(actualX, Is.EqualTo(expectedX), "X coordinate was not transformed correctly");
            Assert.That(actualZ, Is.EqualTo(expectedZ), "Z coordinate was not transformed correctly");
        }

        [Test]
        public void ApplyTransformations_EvenSize_ThrowsArgumentException() {
            // Arrange
            const int x = 0;
            const int z = 0;
            const int sizeX = 4; // Even size
            const int sizeZ = 3;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                BlueprintService.ApplyTransformations(x, z, sizeX, sizeZ, 0, Symmetries.None));
        }

        [Test]
        public void ApplyTransformations_PreservesCenter() {
            // Arrange
            const int size = 5;
            var center = (size - 1) / 2;

            // Act
            var (actualX, actualZ) = BlueprintService.ApplyTransformations(
                center, center, size, size, 1, Symmetries.XAxis);

            // Assert
            Assert.That(actualX, Is.EqualTo(center), "Center X coordinate was not preserved");
            Assert.That(actualZ, Is.EqualTo(center), "Center Z coordinate was not preserved");
        }

        [Test]
        public void ApplyTransformations_PreservesCenterZ() {
            // Arrange
            const int size = 5;
            var center = (size - 1) / 2;

            // Act
            var (actualX, actualZ) = BlueprintService.ApplyTransformations(
                center, center, size, size, 1, Symmetries.ZAxis);

            // Assert
            Assert.That(actualX, Is.EqualTo(center), "Center X coordinate was not preserved");
            Assert.That(actualZ, Is.EqualTo(center), "Center Z coordinate was not preserved");
        }
    }
}