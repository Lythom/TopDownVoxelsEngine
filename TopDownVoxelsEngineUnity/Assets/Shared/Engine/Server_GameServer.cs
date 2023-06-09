﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared.Net;

namespace Shared {
    public class Server_GameServer : IDisposable {
        public readonly GameState State = new(null, null, null);
        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        public int Frames { get; private set; } = 0;
        public int SimulatedFrameTime { get; set; } = 0; // For testing

        private const int TargetFrameTime = 20; // 20ms per frame = 50 FPS
        private const int MaxCatchUpTime = 15; // up to 300ms delay is acceptable. Beyond that we throttle.

        private int _catchUpTime = 0;
        private PriorityLevel _minimumPriority = PriorityLevel.All;
        private readonly Stopwatch _frameStopwatch = new();

        private readonly TickGameEvent _tick = new() {
            Id = -1,
            MinPriority = PriorityLevel.All
        };


        public Server_GameServer() {
            InitState();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            StartFixedUpdate();
        }

        private void InitState() {
            LevelMap level = new LevelMap("lobby", new Vector3(64 + 8, 4, 64 + 8));
            State.Levels.Add("lobby", level);
        }

        private async void StartFixedUpdate() {
            SideEffectManager sideEffectManager = new SideEffectManager();
            sideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
            while (!_cancellationToken.IsCancellationRequested) {
                await Tick(_cancellationToken, sideEffectManager);
            }

            sideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
        }

        private void UpdatePriorityLevel(PriorityLevel level) {
            _minimumPriority = level;
        }

        public async Task Tick(CancellationToken cancellationToken, SideEffectManager sideEffectManager) {
            _frameStopwatch.Restart();

            _tick.MinPriority = _minimumPriority;
            _tick.Apply(State, sideEffectManager);

            // Simulate work being done for a frame for test purpose
            if (SimulatedFrameTime > 0) await Task.Delay(SimulatedFrameTime, cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            Frames++;

            var elapsed = _frameStopwatch.ElapsedMilliseconds;
            var remaining = TargetFrameTime - (int) elapsed;
            if (remaining > 0) {
                // We finished early, wait the remaining time or try to catch up
                if (_catchUpTime > 0) {
                    // We have time to catch up !
                    if (remaining > _catchUpTime) {
                        // Cover the catchUp immediately and wait the remaining of that
                        await Task.Delay(remaining - _catchUpTime, cancellationToken);
                        _catchUpTime = 0;
                        // we can cover all processing needs
                        _minimumPriority = PriorityLevel.All;
                    } else {
                        // can't full cover the catch up, deduce the time no waited and keep going immediatly
                        _catchUpTime -= remaining;

                        if (_catchUpTime > MaxCatchUpTime) {
                            // we are very late, try to catch up doing only essential processing
                            _minimumPriority = PriorityLevel.Must;
                        } else {
                            // we are somewhat late, try to avoid less important processing
                            _minimumPriority = PriorityLevel.Should;
                        }
                    }
                } else {
                    // we are in time, wait for the next tick
                    await Task.Delay(remaining, cancellationToken);
                }
            } else {
                // We're behind schedule (this tick took too long), so skip waiting to catch up
                // and register how late we are
                _catchUpTime -= remaining;
            }
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