// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Scanner {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A connect probe is a ip endpoint consumer that tries to connect
    /// to the consumed ip endpoint.  if successful it uses the probe
    /// implementation to interrogate the server.
    /// </summary>
    /// <returns></returns>
    public abstract class BaseConnectProbe : IDisposable {

        /// <summary>
        /// Internal probe state
        /// </summary>
        private enum State {
            Begin,
            Connect,
            Probe,
            Timeout,
            Disposed
        }

        /// <summary>
        /// Create connect probe
        /// </summary>
        /// <param name="index"></param>
        /// <param name="probe"></param>
        /// <param name="logger"></param>
        protected BaseConnectProbe(int index, IAsyncProbe probe, ILogger logger) {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _index = index;
            _logger = logger;
            _timer = new Timer(OnTimerTimeout);
            _lock = new SemaphoreSlim(1);
            _arg = new SocketAsyncEventArgs();
            _arg.Completed += OnComplete;
        }

        /// <summary>
        /// Dispose probe
        /// </summary>
        public void Dispose() {
            if (_state == State.Disposed) {
                return;
            }
            _lock.Wait();
            try {
                if (_state != State.Disposed) {
                    _state = State.Disposed;
                    Socket.CancelConnectAsync(_arg);
                    _probe.Dispose();
                    _arg.Dispose();
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Start probe
        /// </summary>
        public void Start() {
            if (_state != State.Disposed) {
#if FALSE
                Task.Delay(_scanner._rand.Next(0, 500)).ContinueWith(_ =>
                    Task.Run(() => OnBeginAsync(_arg)));
#else
                Task.Run(() => OnBeginAsync(_arg));
#endif
            }
        }

        /// <summary>
        /// Retrieve next endpoint
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        protected abstract bool Next(out IPEndPoint ep, out int timeout);

        /// <summary>
        /// Whether the probe should give up or retry after
        /// a failure. Default is false.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldGiveUp() => false;

        /// <summary>
        /// Called on individual failure
        /// </summary>
        /// <param name="ep"></param>
        protected abstract void OnFail(IPEndPoint ep);

        /// <summary>
        /// Called on success
        /// </summary>
        /// <param name="ep"></param>
        protected abstract void OnSuccess(IPEndPoint ep);

        /// <summary>
        /// Called on timeout
        /// </summary>
        /// <param name="ep"></param>
        protected virtual void OnComplete(IPEndPoint ep) { }

        /// <summary>
        /// Called on exit
        /// </summary>
        protected virtual void OnExit() { }

        /// <summary>
        /// Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void OnComplete(object sender, SocketAsyncEventArgs arg) {
            _lock.Wait();
            try {
                if (!OnCompleteNoLock(arg)) {
                    return; // assume next OnComplete will occur or timeout...
                }

                // Cancel timer and start next
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception ex) {
                _logger.Debug($"Error during completion of probe {_index}",
                    () => ex);
            }
            finally {
                _lock.Release();
            }
            // We are now disposed, or at begin, go to next to cleanup or continue
            OnBeginAsync(arg);
        }

        /// <summary>
        /// Start connect
        /// </summary>
        /// <param name="arg"></param>
        private async void OnBeginAsync(SocketAsyncEventArgs arg) {
            await _lock.WaitAsync();
            try {
                if (_state == State.Disposed) {
                    return;
                }

                var exit = false;
                while (_state != State.Disposed && !exit) {
                    IPEndPoint ep = null;
                    var timeout = 3000;
                    try {
                        if (!Next(out ep, out timeout)) {
                            exit = true;
                            break;
                        }
                    }
                    catch (OperationCanceledException) {
                        exit = true;
                        break;
                    }
                    catch (InvalidOperationException) {
                        continue;
                    }
                    catch (Exception ex) {
                        _logger.Error($"Error getting endpoint for probe {_index}",
                            () => ex);
                        exit = true;
                        break;
                    }

                    // Now try to connect
                    arg.RemoteEndPoint = ep;
                    while (_state != State.Disposed) {
                        try {
                            // Reset probe and start connect
                            _probe.Reset();
                            _state = State.Connect;
                            if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.IP, arg)) {
                                // Complete inline and pull next...
                                if (OnCompleteNoLock(arg)) {
                                    // Go to next candidate
                                    break;
                                }
                            }
                            // Wait for completion or timeout after x seconds
                            _timer.Change(timeout, Timeout.Infinite);
                            return;
                        }
                        catch (ObjectDisposedException) {
                            continue; // Try again
                        }
                        catch (InvalidOperationException) {
                            continue; // Operation in progress - try again
                        }
                        catch (SocketException sex) {
                            if (sex.SocketErrorCode == SocketError.NoBufferSpaceAvailable ||
                                sex.SocketErrorCode == SocketError.TooManyOpenSockets) {
                                if (ShouldGiveUp()) {
                                    // Exit only until we hit (approx.) min probe count.
                                    exit = true;
                                    break;
                                }
                                // Otherwise retry...
                                continue;
                            }
                            _logger.Error(
                                $"{sex.SocketErrorCode} in connect of probe {_index}...",
                                    () => sex);
                            continue;
                        }
                        catch (Exception ex) {
                            // Unexpected - shut probe down
                            _logger.Error(
                                $"Probe {_index} has unexpected exception during connect.",
                                () => ex);
                            exit = true;
                            break;
                        }
                    }

                    if (exit) {
                        // We failed, requeue the endpoint and kill this probe
                        OnFail(ep);
                        break;
                    }
                }

                //
                // We are here because we either...
                //
                // a) failed to dequeue
                // b) The producer has completed
                // c) Connect failed due to lack of ephimeral ports.
                // d) A non socket exception occurred.
                //
                // and we need to kill this probe and dispose of the underlying argument.
                //
                _probe.Dispose();
                _arg.Dispose();
                _state = State.Disposed;
                OnExit();
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// No lock complete
        /// </summary>
        /// <param name="arg"></param>
        private bool OnCompleteNoLock(SocketAsyncEventArgs arg) {
            try {
                switch (_state) {
                    case State.Connect:
                        if (arg.SocketError != SocketError.Success) {
                            // Reset back to begin
                            _state = State.Begin;
                            return true;
                        }
                        // Start probe
                        _state = State.Probe;
                        return OnProbeNoLock(arg);
                    case State.Probe:
                        // Continue probing until completed
                        return OnProbeNoLock(arg);
                    case State.Timeout:
                        // Timeout caused cancel, go back to begin
                        _state = State.Begin;
                        return true;
                    case State.Disposed:
                        // Stay disposed
                        return true;
                    default:
                        throw new SystemException("Unexpected");
                }
            }
            catch {
                // Error, continue at beginning
                _state = State.Begin;
                return true;
            }
            finally {
                if (_state == State.Begin || _state == State.Disposed) {
                    OnComplete((IPEndPoint)arg.RemoteEndPoint);
                    arg.ConnectSocket.SafeDispose();
                    arg.SocketError = SocketError.NotSocket;
                }
            }
        }

        /// <summary>
        /// Perform probe
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private bool OnProbeNoLock(SocketAsyncEventArgs arg) {
            var completed = _probe.CompleteAsync(_index, arg,
                out var ok, out var timeout);
            if (completed) {
                if (ok) {
                    OnSuccess((IPEndPoint)arg.RemoteEndPoint);
                }
                else {
                    OnComplete((IPEndPoint)arg.RemoteEndPoint);
                }
                _state = State.Begin;
            }
            else {
                // Wait for completion or timeout after x seconds
                _timer.Change(timeout, Timeout.Infinite);
            }
            return completed;
        }

        /// <summary>
        /// Timeout current probe
        /// </summary>
        /// <param name="state"></param>
        private void OnTimerTimeout(object state) {
            if (!_lock.Wait(0)) {
                // lock is taken either by having completed or having re-begun.
                return;
            }
            try {
                switch (_state) {
                    case State.Timeout:
                        return;
                    case State.Connect:
                        // Cancel current arg and mark as timedout then recycle
                        Socket.CancelConnectAsync(_arg);
                        _arg.SocketError = SocketError.TimedOut;
                        return;
                    case State.Probe:
                        _logger.Debug(
                            $"Probe {_index} {_arg.RemoteEndPoint} timed out...");
                        _arg.SocketError = SocketError.TimedOut;
                        if (_probe.Reset()) {
                            return;
                        }
                        // Has not cancelled the operation in progress.
                        _logger.Info(
                            $"Probe {_index} not cancelled - try restart...");
                        OnBeginAsync(_arg);
                        return;
                }
            }
            catch (Exception ex) {
                _logger.Debug($"Error during timeout of probe {_index}",
                    () => ex);
            }
            finally {
                _state = State.Timeout;
                _lock.Release();
            }
        }

        private readonly ILogger _logger;
        private readonly SocketAsyncEventArgs _arg;
        private readonly SemaphoreSlim _lock;
        private readonly int _index;
        private readonly Timer _timer;
        private readonly IAsyncProbe _probe;
        private State _state;
    }
}
