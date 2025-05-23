﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared;
using Shared.Net;

namespace Server {
    public class ServerClock {
        private readonly VoxelsEngineServer _voxelsEngineServer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

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

        public ServerClock(VoxelsEngineServer voxelsEngineServer) {
            _voxelsEngineServer = voxelsEngineServer;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }


        public void Stop() {
            _cancellationTokenSource.Cancel(false);
        }

        public async UniTaskVoid StartFixedUpdateAsync(ConcurrentQueue<InputMessage> inbox) {
            SideEffectManager sideEffectManager = new SideEffectManager();
            sideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
            try {
                while (!_cancellationToken.IsCancellationRequested) {
                    await TickAsync(_cancellationToken, sideEffectManager, inbox);
                }
            } catch (Exception e) {
                Logr.LogException(e, "Error while self applying. Fatal.");
            }

            sideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
        }

        private void UpdatePriorityLevel(PriorityLevel level) {
            _minimumPriority = level;
        }

        public async UniTask TickAsync(CancellationToken cancellationToken, SideEffectManager sideEffectManager, ConcurrentQueue<InputMessage> inbox) {
            if (!_voxelsEngineServer.IsReady) {
                await Task.Delay(2000, cancellationToken);
                return;
            }

            // read and apply message as part of the tick
            bool hasInput;
            InputMessage m;
            do {
                hasInput = inbox.TryDequeue(out m);
                if (hasInput) await _voxelsEngineServer.HandleMessageAsync(m);
            } while (hasInput);

            _frameStopwatch.Restart();

            _tick.MinPriority = _minimumPriority;
            _tick.Apply(_voxelsEngineServer.State, sideEffectManager);
            
            // TODO: Should be part of the tickGameEvent ?
            _voxelsEngineServer.TryGenerateChunks(_minimumPriority);
            _voxelsEngineServer.SendScheduledChunks();

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
    }
}