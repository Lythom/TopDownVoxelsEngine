using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace ChunkPositionBenchmark
{
    // Mock classes to simulate your environment
    public static class M
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(float value) => (int)MathF.Round(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorToInt(float value) => (int)MathF.Floor(value);
    }

    public static class Chunk
    {
        public const int Size = 16;
    }

    public struct Vector3
    {
        public float X;
        public float Z;
        
        public Vector3(float x, float z)
        {
            X = x;
            Z = z;
        }
    }

    // Implementation classes
    public static class TupleImplementation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int chX, int chZ) GetChunkPosition(Vector3 worldPosition)
        {
            return GetChunkPosition(worldPosition.X, worldPosition.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int chX, int chZ) GetChunkPosition(float wx, float wz)
        {
            int cX = M.RoundToInt(wx);
            int cZ = M.RoundToInt(wz);
            int chX = M.FloorToInt(cX / (float)Chunk.Size);
            int chZ = M.FloorToInt(cZ / (float)Chunk.Size);
            return (chX, chZ);
        }
    }

    public static class OutParameterImplementation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetChunkPosition(Vector3 worldPosition, out int chX, out int chZ)
        {
            GetChunkPosition(worldPosition.X, worldPosition.Z, out chX, out chZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetChunkPosition(float wx, float wz, out int chX, out int chZ)
        {
            int cX = M.RoundToInt(wx);
            int cZ = M.RoundToInt(wz);
            chX = M.FloorToInt(cX / (float)Chunk.Size);
            chZ = M.FloorToInt(cZ / (float)Chunk.Size);
        }
    }

    // Benchmark class
    public class ChunkPositionBenchmark
    {
        private const int WarmupIterations = 10_000;
        private const int BenchmarkIterations = 100_000_000;
        
        private static readonly Random Random = new(12345); // Fixed seed for consistency
        private static readonly float[] TestDataX;
        private static readonly float[] TestDataZ;
        
        static ChunkPositionBenchmark()
        {
            // Pre-generate test data to avoid allocation overhead during benchmark
            TestDataX = new float[BenchmarkIterations];
            TestDataZ = new float[BenchmarkIterations];
            
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                TestDataX[i] = (float)(Random.NextDouble() * 2000 - 1000); // Range: -1000 to 1000
                TestDataZ[i] = (float)(Random.NextDouble() * 2000 - 1000);
            }
        }

        public static void RunBenchmark()
        {
            Console.WriteLine("Chunk Position Performance Benchmark");
            Console.WriteLine("====================================");
            Console.WriteLine($"Warmup iterations: {WarmupIterations:N0}");
            Console.WriteLine($"Benchmark iterations: {BenchmarkIterations:N0}");
            Console.WriteLine();

            // Warmup both implementations
            Console.WriteLine("Warming up...");
            WarmupTuple();
            WarmupOutParameter();
            
            // Force garbage collection before benchmarks
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            Console.WriteLine("Running benchmarks...");
            Console.WriteLine();

            // Benchmark tuple implementation
            var tupleTime = BenchmarkTuple();
            
            // Small delay between benchmarks
            System.Threading.Thread.Sleep(100);
            
            // Benchmark out parameter implementation
            var outParamTime = BenchmarkOutParameter();
            
            // Display results
            DisplayResults(tupleTime, outParamTime);
        }

        private static void WarmupTuple()
        {
            for (int i = 0; i < WarmupIterations; i++)
            {
                var (chX, chZ) = TupleImplementation.GetChunkPosition(TestDataX[i], TestDataZ[i]);
                // Prevent optimization from removing the call
                if (chX == int.MaxValue) Console.WriteLine("Impossible");
            }
        }

        private static void WarmupOutParameter()
        {
            for (int i = 0; i < WarmupIterations; i++)
            {
                OutParameterImplementation.GetChunkPosition(TestDataX[i], TestDataZ[i], out var chX, out var chZ);
                // Prevent optimization from removing the call
                if (chX == int.MaxValue) Console.WriteLine("Impossible");
            }
        }

        private static TimeSpan BenchmarkTuple()
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var (chX, chZ) = TupleImplementation.GetChunkPosition(TestDataX[i], TestDataZ[i]);
                // Prevent dead code elimination
                if (chX == int.MaxValue && chZ == int.MaxValue) 
                    Console.WriteLine("Impossible");
            }
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static TimeSpan BenchmarkOutParameter()
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                OutParameterImplementation.GetChunkPosition(TestDataX[i], TestDataZ[i], out var chX, out var chZ);
                // Prevent dead code elimination
                if (chX == int.MaxValue && chZ == int.MaxValue) 
                    Console.WriteLine("Impossible");
            }
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static void DisplayResults(TimeSpan tupleTime, TimeSpan outParamTime)
        {
            Console.WriteLine("Results:");
            Console.WriteLine("========");
            Console.WriteLine($"Tuple approach:        {tupleTime.TotalMilliseconds:F2} ms");
            Console.WriteLine($"Out parameter approach: {outParamTime.TotalMilliseconds:F2} ms");
            Console.WriteLine();
            
            var difference = tupleTime.TotalMilliseconds - outParamTime.TotalMilliseconds;
            var percentDifference = (difference / tupleTime.TotalMilliseconds) * 100;
            
            Console.WriteLine($"Difference: {Math.Abs(difference):F2} ms ({Math.Abs(percentDifference):F2}%)");
            
            if (Math.Abs(percentDifference) < 1)
            {
                Console.WriteLine("Performance difference is negligible (< 1%)");
            }
            else if (tupleTime < outParamTime)
            {
                Console.WriteLine("Tuple approach is faster");
            }
            else
            {
                Console.WriteLine("Out parameter approach is faster");
            }
            
            // Calculate per-call overhead
            var tupleMsPerCall = (tupleTime.TotalMilliseconds / BenchmarkIterations);
            var outParamMsPerCall = (outParamTime.TotalMilliseconds / BenchmarkIterations);
            
            Console.WriteLine();
            Console.WriteLine("Per-call performance:");
            Console.WriteLine($"Tuple approach:        {tupleMsPerCall:F9} ms/call");
            Console.WriteLine($"Out parameter approach: {outParamMsPerCall:F9} ms/call");
            Console.WriteLine($"Difference per call:   {Math.Abs(tupleMsPerCall - outParamMsPerCall):F9} ms");
        }

        // Correctness test to ensure both implementations produce the same results
        public static void RunCorrectnessTest()
        {
            Console.WriteLine("Running correctness test...");
            
            var testCases = new[]
            {
                (0f, 0f),
                (15.9f, 15.9f),
                (16f, 16f),
                (32.1f, 32.1f),
                (-15.9f, -15.9f),
                (-16f, -16f),
                (-32.1f, -32.1f),
                (123.456f, -789.123f)
            };

            bool allCorrect = true;
            
            foreach (var (x, z) in testCases)
            {
                var (tupleChX, tupleChZ) = TupleImplementation.GetChunkPosition(x, z);
                OutParameterImplementation.GetChunkPosition(x, z, out var outChX, out var outChZ);
                
                if (tupleChX != outChX || tupleChZ != outChZ)
                {
                    Console.WriteLine($"MISMATCH at ({x}, {z}): Tuple=({tupleChX}, {tupleChZ}), Out=({outChX}, {outChZ})");
                    allCorrect = false;
                }
            }
            
            if (allCorrect)
            {
                Console.WriteLine("✓ All test cases passed - implementations are equivalent");
            }
            else
            {
                Console.WriteLine("✗ Some test cases failed - implementations differ");
            }
            
            Console.WriteLine();
        }
    
        [Test]
        public void BenchmarkChunkPositionPerformance() {
            Console.WriteLine(".NET Runtime: " + Environment.Version);
            Console.WriteLine("Architecture: " + (Environment.Is64BitProcess ? "x64" : "x86"));
            Console.WriteLine();
            
            // First verify correctness
            ChunkPositionBenchmark.RunCorrectnessTest();
            
            // Then run performance benchmark
            ChunkPositionBenchmark.RunBenchmark();
        }
    }
}