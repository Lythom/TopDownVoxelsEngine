using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Shared {
    public enum Priority
    
    public class GameServer : IDisposable {
        public readonly GameState State = new();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        public int Frames { get; private set; } = 0;
        public int SimulatedFrameTime { get; set; } = 0; // For testing

        private const int TargetFrameTime = 20; // 20ms per frame = 50 FPS
        private const int MaxCatchUpFrames = 15; // up to 300ms delay is acceptable. Beyond that we throttle.

        private int catchUpFrames = 0;
        private Stopwatch frameStopwatch = new Stopwatch();

        public GameServer() {
            Character playerCharacter = new Character();
            playerCharacter.Position = new Vector3(1028f, 4.5f, 1028f);
            State.Characters.Add(playerCharacter);

            LevelMap level = new LevelMap("server", "default");

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            StartFixedUpdate();
        }

        private async void StartFixedUpdate() {
            while (!_cancellationToken.IsCancellationRequested) {
                await FixedUpdate(_cancellationToken);
            }
        }

        public async Task FixedUpdate(CancellationToken cancellationToken) {
            frameStopwatch.Restart();

            await DoTick(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            Frames++;

            var elapsed = frameStopwatch.ElapsedMilliseconds;
            if (elapsed < TargetFrameTime && catchUpFrames == 0) {
                // We finished early, so wait the remaining time
                await Task.Delay(TargetFrameTime - (int) elapsed);
            } else if (elapsed > TargetFrameTime && catchUpFrames < MaxCatchUpFrames) {
                // We're behind, so skip waiting to catch up
                catchUpFrames++;
            } else {
                // Either we're caught up or we've hit the max catch-up frames
                catchUpFrames = 0;
            }
        }

        private async Task DoTick(CancellationToken cancellationToken) {
            try {
                // Generate missing chunks
                foreach (var c in State.Characters) {
                    var playerPos = c.Position;
                    if (string.IsNullOrEmpty(c.Level)) continue; // player not yet into a world
                    var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);

                    var range = 2;
                    for (int x = -range; x <= range; x++) {
                        for (int z = -range; z <= range; z++) {
                            var key = Chunk.GetFlatIndex(chX + x, chZ + z);
                            if (State.Levels[0].) {
                                if (chX + x < 0 || chX + x >= _level.Chunks.GetLength(0) || chZ + z < 0 || chZ + z >= _level.Chunks.GetLength(1)) continue;
                                RendererChunks.Add(key);
                                ToBeRendererQueue.Enqueue(key);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Logr.LogException(e);
            }

            // Simulate work being done for a frame for test purpose
            if (SimulatedFrameTime > 0) await Task.Delay(SimulatedFrameTime, cancellationToken: cancellationToken);
        }

        public void Stop() {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose() {
            _cancellationTokenSource.Cancel(false);
            _cancellationTokenSource.Dispose();
        }
    }
}