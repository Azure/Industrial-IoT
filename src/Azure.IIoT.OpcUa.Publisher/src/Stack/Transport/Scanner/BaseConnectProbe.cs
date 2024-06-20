// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport.Scanner
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// A connect probe is a ip endpoint consumer that tries to connect
    /// to the consumed ip endpoint.  if successful it uses the probe
    /// implementation to interrogate the server.
    /// </summary>
    /// <returns></returns>
    internal abstract class BaseConnectProbe : IDisposable
    {
        /// <summary>
        /// Create connect probe
        /// </summary>
        /// <param name="index"></param>
        /// <param name="probe"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        protected BaseConnectProbe(int index, IAsyncProbe probe, ILogger logger,
            TimeProvider? timeProvider = null)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _index = index;
            _logger = logger;
            _lock = new SemaphoreSlim(1, 1);
            _arg = new AsyncConnect(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose probe
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _lock.Wait();
                    try
                    {
                        _cts.Cancel();
                        _probe?.Dispose();
                        DisposeArgsNoLock();
                    }
                    finally
                    {
                        _cts.Dispose();
                        _lock.Release();
                        _lock.Dispose();
                    }
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Start probe
        /// </summary>
        public void Start()
        {
            OnBegin();
        }

        /// <summary>
        /// Retrieve next endpoint
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        protected abstract bool GetNext(
            [NotNullWhen(true)] out IPEndPoint? ep, out int timeout);

        /// <summary>
        /// Whether the probe should give up or retry after
        /// a failure. Default is false.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldGiveUp()
        {
            return false;
        }

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
        /// Begin connecting to next endpoint
        /// </summary>
        private void OnBegin()
        {
            var exit = false;
            _lock.Wait();
            try
            {
                while (!_cts.IsCancellationRequested && !exit)
                {
                    IPEndPoint? ep = null;
                    var timeout = 3000;
                    try
                    {
                        if (!GetNext(out ep, out timeout))
                        {
                            exit = true;
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        exit = true;
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting endpoint for probe {Index}",
                            _index);
                        exit = true;
                        break;
                    }

                    if (_arg?.IsRunning == true)
                    {
                        // Reset args since it is in running state and cannot be used...
                        _logger.LogTrace("Disposing args in running state.");
                        DisposeArgsNoLock();
                    }

                    while (!_cts.IsCancellationRequested)
                    {
                        _arg ??= new AsyncConnect(this);
                        try
                        {
                            if (_arg.BeginConnect(ep, timeout))
                            {
                                // Completed synchronously - go to next candidate
                                break;
                            }
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Try again
                        }
                        catch (InvalidOperationException)
                        {
                            // Operation in progress - try again
                        }
                        catch (SocketException sex)
                        {
                            if (sex.SocketErrorCode == SocketError.NoBufferSpaceAvailable ||
                                sex.SocketErrorCode == SocketError.TooManyOpenSockets)
                            {
                                if (ShouldGiveUp())
                                {
                                    // Exit only until we hit (approx.) min probe count.
                                    exit = true;
                                    break;
                                }
                                // Otherwise retry...
                            }
                            else
                            {
                                _logger.LogError(sex, "{Code} in connect of probe {Index}...",
                                    sex.SocketErrorCode, _index);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Unexpected - shut probe down
                            _logger.LogError(ex,
                                "Probe {Index} has unexpected exception during connect.",
                                _index);
                            exit = true;
                            break;
                        }

                        // Retry same endpoint address with new args
                        DisposeArgsNoLock();
                    }

                    if (exit)
                    {
                        // We failed, requeue the endpoint and kill this probe
                        OnFail(ep);
                        break;
                    }
                }

                if (_cts.IsCancellationRequested)
                {
                    return;
                }
            }
            finally
            {
                _lock.Release();
            }

            if (exit)
            {
                //
                // We are here because we either...
                //
                // a) failed to dequeue
                // b) The producer has completed
                // c) Connect failed due to lack of ephimeral ports.
                // d) A non socket exception occurred.
                //
                // Notify of exit.
                //
                OnExit();
            }
        }

        /// <summary>
        /// Dispose of args and resources
        /// </summary>
        private void DisposeArgsNoLock()
        {
            _arg?.Dispose();
            _arg = null;
        }

        /// <summary>
        /// Internal arg state
        /// </summary>
        private enum State
        {
            Begin,
            Connect,
            Probe,
            Timeout,
            Error,
            Disposed
        }

        /// <summary>
        /// Wraps socket event arg and connection state
        /// </summary>
        private class AsyncConnect : IDisposable
        {
            /// <summary>
            /// Whether the connect is running
            /// </summary>
            internal bool IsRunning => _state != State.Begin;

            /// <summary>
            /// Create event arg wrapper
            /// </summary>
            /// <param name="outer"></param>
            public AsyncConnect(BaseConnectProbe outer)
            {
                _outer = outer;
                _timer = outer._timeProvider.CreateTimer(OnTimerTimeout,
                    null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _lock = new SemaphoreSlim(1, 1);
                _arg = new SocketAsyncEventArgs();
                _arg.Completed += OnComplete;
                _arg.UserToken = this;
            }

            /// <summary>
            /// Dispose probe
            /// </summary>
            public void Dispose()
            {
                _lock.Wait();
                try
                {
                    if (_state != State.Disposed)
                    {
                        Socket.CancelConnectAsync(_arg);
                        _arg.ConnectSocket?.Dispose();
                        _socket?.Dispose();
                        _arg.Dispose();
                        _timer.Dispose();
                        _state = State.Disposed;
                    }
                }
                finally
                {
                    _lock.Release();
                    _lock.Dispose();
                }
            }

            /// <summary>
            /// Begins a connect and returns whether it completed
            /// synchronously so outer can loop.
            /// </summary>
            /// <param name="ep"></param>
            /// <param name="timeout"></param>
            /// <returns></returns>
            internal bool BeginConnect(IPEndPoint ep, int timeout)
            {
                _lock.Wait();
                try
                {
                    _arg.RemoteEndPoint = ep;

                    // Reset arg, probe, and start connecting
                    _outer._probe.Reset();
                    _arg.ConnectSocket.SafeDispose();
                    _state = State.Connect;

                    // Open new socket
                    _socket?.Dispose();
                    _socket = new Socket(SocketType.Stream, ProtocolType.IP);
                    if (!_socket.ConnectAsync(_arg))
                    {
                        // Complete inline and pull next...
                        if (OnCompleteNoLock())
                        {
                            // Go to next candidate
                            return true;
                        }
                    }

                    // Wait for completion or timeout after x seconds
                    _timer.Change(TimeSpan.FromMilliseconds(timeout),
                        Timeout.InfiniteTimeSpan);
                    return false;
                }
                finally
                {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Complete is called from completion queue on cancel or connect/error
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="arg"></param>
            private void OnComplete(object? sender, SocketAsyncEventArgs arg)
            {
                _lock.Wait();
                try
                {
                    if (!OnCompleteNoLock())
                    {
                        return; // assume next OnComplete will occur or timeout...
                    }
                    // Cancel timer and start next
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
                catch (Exception ex)
                {
                    _outer._logger.LogDebug(ex, "Error during completion of probe {Index}",
                        _outer._index);
                }
                finally
                {
                    _lock.Release();
                }
                // We are now disposed, or at begin, go to next to cleanup or continue
                _outer.OnBegin();
            }

            /// <summary>
            /// No lock complete
            /// </summary>
            /// <exception cref="InvalidProgramException"></exception>
            private bool OnCompleteNoLock()
            {
                try
                {
                    switch (_state)
                    {
                        case State.Connect:
                            if (_arg.SocketError != SocketError.Success)
                            {
                                // Reset back to begin
                                _state = State.Begin;
                                return true;
                            }
                            // Start probe
                            _state = State.Probe;
                            return OnProbeNoLock();
                        case State.Probe:
                            // Continue probing until completed
                            return OnProbeNoLock();
                        case State.Timeout:
                            // Timeout caused cancel already - stay in timeout condition.
                            return true;
                        case State.Error:
                        case State.Disposed:
                            // Stay in error state or disposed - this should not happen.
                            return true;
                        // Unexpected.
                        // case State.Begin:
                        default:
                            throw new InvalidProgramException($"Unexpected state {_state}.");
                    }
                }
                catch
                {
                    // Go to error state
                    _state = State.Error;
                    return true;
                }
                finally
                {
                    if (_state != State.Connect && _state != State.Probe)
                    {
                        // Terminal - complete address now
                        if (_arg.RemoteEndPoint is IPEndPoint ep)
                        {
                            _outer.OnComplete(ep);
                        }
                        _arg.SocketError = SocketError.NotSocket;
                    }
                }
            }

            /// <summary>
            /// Perform probe
            /// </summary>
            /// <returns></returns>
            private bool OnProbeNoLock()
            {
                var completed = _outer._probe.OnComplete(_outer._index, _arg,
                    out var ok, out var timeout);
                if (completed)
                {
                    if (_arg.RemoteEndPoint is IPEndPoint ep)
                    {
                        if (ok)
                        {
                            _outer.OnSuccess(ep);
                        }
                        else
                        {
                            _outer.OnComplete(ep);
                        }
                    }
                    _state = State.Begin;
                }
                else
                {
                    // Wait for completion or timeout after x seconds
                    _timer.Change(TimeSpan.FromMilliseconds(timeout),
                        Timeout.InfiniteTimeSpan);
                }
                return completed;
            }

            /// <summary>
            /// Timeout current probe
            /// </summary>
            /// <param name="state"></param>
            private void OnTimerTimeout(object? state)
            {
                if (!_lock.Wait(0))
                {
                    // lock is taken either by having completed or having re-begun.
                    return;
                }
                try
                {
                    switch (_state)
                    {
                        case State.Timeout:
                            return;
                        case State.Connect:
                            //
                            // Cancel current arg and mark as timedout which will
                            // dispose the arg and open a new one.  Should the arg
                            // still complete later it will be in terminal state.
                            //
                            Socket.CancelConnectAsync(_arg);
                            _state = State.Timeout;
                            _arg.SocketError = SocketError.TimedOut;
                            return;
                        case State.Probe:
                            _outer._logger.LogDebug("Probe {Index} {RemoteEp} timed out...",
                                _outer._index, _arg.RemoteEndPoint);
                            _arg.SocketError = SocketError.TimedOut;
                            _state = State.Timeout;
                            if (_outer._probe.Reset())
                            {
                                // We will get a disconnect complete callback now.
                                return;
                            }
                            //
                            // There was nothing to cancel so start from beginning.
                            // Since connect socket is connected, go to begin state
                            // This will close the socket and reconnect a new one.
                            //
                            _outer._logger.LogInformation(
                                "Probe {Index} not cancelled - try restart...",
                                _outer._index);
                            _state = State.Begin;
                            _outer.OnBegin();
                            return;
                    }
                }
                catch (Exception ex)
                {
                    _outer._logger.LogDebug(ex,
                        "Error during timeout of probe {Index}", _outer._index);
                }
                finally
                {
                    _lock.Release();
                }
            }

            private readonly ITimer _timer;
            private readonly SocketAsyncEventArgs _arg;
            private readonly SemaphoreSlim _lock;
            private readonly BaseConnectProbe _outer;
            private State _state;
            private Socket? _socket;
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _lock;
        private readonly int _index;
        private readonly IAsyncProbe _probe;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private AsyncConnect? _arg;
        private bool _disposedValue;
    }
}
