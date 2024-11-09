/* ========================================================================
 * Copyright (c) 2005-2023 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using Opc.Ua.Redaction;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Attempts to reconnect to the server.
    /// </summary>
    public class SessionReconnectHandler : IDisposable
    {
        /// <summary>
        /// The minimum reconnect period in ms.
        /// </summary>
        public const int MinReconnectPeriod = 500;

        /// <summary>
        /// The maximum reconnect period in ms.
        /// </summary>
        public const int MaxReconnectPeriod = 30000;

        /// <summary>
        /// The default reconnect period in ms.
        /// </summary>
        public const int DefaultReconnectPeriod = 1000;

        /// <summary>
        /// The internal state of the reconnect handler.
        /// </summary>
        [Flags]
        public enum ReconnectState
        {
            /// <summary>
            /// The reconnect handler is ready to start the reconnect timer.
            /// </summary>
            Ready = 0,

            /// <summary>
            /// The reconnect timer is triggered and waiting to reconnect.
            /// </summary>
            Triggered = 1,

            /// <summary>
            /// The reconnection is in progress.
            /// </summary>
            Reconnecting = 2,

            /// <summary>
            /// The reconnect handler is disposed and can not be used for
            /// further reconnect attempts.
            /// </summary>
            Disposed = 4
        }

        /// <summary>
        /// Create a reconnect handler.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="reconnectAbort">Set to <c>true</c> to allow reconnect abort if
        /// keep alive recovered.</param>
        /// <param name="maxReconnectPeriod">
        /// The upper limit for the reconnect period after exponential backoff.
        /// -1 (default) indicates that no exponential backoff should be used.
        /// </param>
        public SessionReconnectHandler(ILoggerFactory loggerFactory,
            bool reconnectAbort = false, int maxReconnectPeriod = -1)
        {
            _logger = loggerFactory.CreateLogger<SessionReconnectHandler>();
            _reconnectAbort = reconnectAbort;
            _reconnectTimer = new Timer(OnReconnectAsync, this, Timeout.Infinite, Timeout.Infinite);
            _state = ReconnectState.Ready;
            _cancelReconnect = false;
            _updateFromServer = false;
            _baseReconnectPeriod = DefaultReconnectPeriod;
            _maxReconnectPeriod = maxReconnectPeriod < 0 ? -1 :
                Math.Max(MinReconnectPeriod, Math.Min(maxReconnectPeriod, MaxReconnectPeriod));
            _random = new Random();
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    if (_state != ReconnectState.Disposed)
                    {
                        _reconnectTimer.Dispose();
                        _state = ReconnectState.Disposed;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the session managed by the handler.
        /// </summary>
        /// <value>The session.</value>
        public ISession? Session => _session;

        /// <summary>
        /// The internal state of the reconnect handler.
        /// </summary>
        public ReconnectState State
        {
            get
            {
                lock (_lock)
                {
                    if (_reconnectTimer == null)
                    {
                        return ReconnectState.Disposed;
                    }
                    return _state;
                }
            }
        }

        /// <summary>
        /// Cancel a reconnect in progress.
        /// </summary>
        public void CancelReconnect()
        {
            lock (_lock)
            {
                if (_reconnectTimer == null)
                {
                    return;
                }

                if (_state == ReconnectState.Triggered)
                {
                    _session = null;
                    EnterReadyState();
                    return;
                }

                _cancelReconnect = true;
            }
        }

        /// <summary>
        /// Begins the reconnect process using a reverse connection.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reverseConnectManager"></param>
        /// <param name="reconnectPeriod"></param>
        /// <param name="callback"></param>
        /// <exception cref="ServiceResultException"></exception>
        public ReconnectState BeginReconnect(ISession session,
            ReverseConnectManager? reverseConnectManager, int reconnectPeriod,
            EventHandler callback)
        {
            lock (_lock)
            {
                if (_reconnectTimer == null)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState);
                }

                // cancel reconnect requested, if possible
                if (session == null)
                {
                    if (_state == ReconnectState.Triggered)
                    {
                        _session = null;
                        EnterReadyState();
                        return _state;
                    }
                    // reconnect already in progress, schedule cancel
                    _cancelReconnect = true;
                    return _state;
                }

                // set reconnect period within boundaries
                reconnectPeriod = CheckedReconnectPeriod(reconnectPeriod);

                // ignore subsequent trigger requests
                if (_state == ReconnectState.Ready)
                {
                    _session = session;
                    _baseReconnectPeriod = reconnectPeriod;
                    _reconnectFailed = false;
                    _cancelReconnect = false;
                    _callback = callback;
                    _reverseConnectManager = reverseConnectManager;
                    _reconnectTimer.Change(JitteredReconnectPeriod(reconnectPeriod), Timeout.Infinite);
                    _reconnectPeriod = CheckedReconnectPeriod(reconnectPeriod, true);
                    _state = ReconnectState.Triggered;
                    return _state;
                }

                // if triggered, reset timer only if requested reconnect period is shorter
                if (_state == ReconnectState.Triggered && reconnectPeriod < _baseReconnectPeriod)
                {
                    _baseReconnectPeriod = reconnectPeriod;
                    _reconnectTimer.Change(JitteredReconnectPeriod(reconnectPeriod), Timeout.Infinite);
                    _reconnectPeriod = CheckedReconnectPeriod(reconnectPeriod, true);
                }

                return _state;
            }
        }

        /// <summary>
        /// Returns the reconnect period with a random jitter.
        /// </summary>
        /// <param name="reconnectPeriod"></param>
        public virtual int JitteredReconnectPeriod(int reconnectPeriod)
        {
            // The factors result in a jitter of 10%.
            const int jitterResolution = 1000;
            const int jitterFactor = 10;
#pragma warning disable CA5394 // Do not use insecure randomness
            var jitter = reconnectPeriod * _random.Next(-jitterResolution, jitterResolution) /
                (jitterResolution * jitterFactor);
#pragma warning restore CA5394 // Do not use insecure randomness
            return reconnectPeriod + jitter;
        }

        /// <summary>
        /// Returns the reconnect period within the min and max boundaries.
        /// </summary>
        /// <param name="reconnectPeriod"></param>
        /// <param name="exponentialBackoff"></param>
        public virtual int CheckedReconnectPeriod(int reconnectPeriod, bool exponentialBackoff = false)
        {
            // exponential backoff is controlled by _maxReconnectPeriod
            if (_maxReconnectPeriod > MinReconnectPeriod)
            {
                if (exponentialBackoff)
                {
                    reconnectPeriod *= 2;
                }
                return Math.Min(Math.Max(reconnectPeriod, MinReconnectPeriod), _maxReconnectPeriod);
            }

            return Math.Max(reconnectPeriod, MinReconnectPeriod);
        }

        /// <summary>
        /// Called when the reconnect timer expires.
        /// </summary>
        /// <param name="state"></param>
        private async void OnReconnectAsync(object? state)
        {
            var reconnectStart = HiResClock.TickCount;
            try
            {
                // check for exit.
                lock (_lock)
                {
                    if (_reconnectTimer == null || _session == null)
                    {
                        return;
                    }
                    if (_state != ReconnectState.Triggered)
                    {
                        return;
                    }
                    // enter reconnecting state
                    _state = ReconnectState.Reconnecting;
                }

                var keepaliveRecovered = false;

                // preserve legacy behavior if reconnectAbort is not set
                var session = _session;
                if (session != null && _reconnectAbort &&
                    session.Connected && !session.KeepAliveStopped)
                {
                    keepaliveRecovered = true;
                    // breaking change, the callback must only assign the new
                    // session if the property is != null
                    _logger.LogInformation("Reconnect {Session} aborted, KeepAlive recovered.", session.SessionId);
                    _session = null;
                }
                else
                {
                    _logger.LogInformation("Reconnect {Session}.", _session?.SessionId);
                }

                // do the reconnect or recover state.
                if (keepaliveRecovered ||
                    await DoReconnectAsync().ConfigureAwait(false))
                {
                    lock (_lock)
                    {
                        EnterReadyState();
                    }

                    // notify the caller.
                    _callback?.Invoke(this, EventArgs.Empty);

                    return;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError("Unexpected error during reconnect: {Message}", Redact.Create(exception));
            }

            // schedule the next reconnect.
            lock (_lock)
            {
                if (_state != ReconnectState.Disposed)
                {
                    if (_cancelReconnect)
                    {
                        EnterReadyState();
                    }
                    else
                    {
                        var elapsed = HiResClock.TickCount - reconnectStart;
                        _logger.LogInformation("Reconnect period is {Period} ms, {Elapsed} ms elapsed in reconnect.", _reconnectPeriod, elapsed);
                        var adjustedReconnectPeriod = CheckedReconnectPeriod(_reconnectPeriod - elapsed);
                        adjustedReconnectPeriod = JitteredReconnectPeriod(adjustedReconnectPeriod);
                        _reconnectTimer.Change(adjustedReconnectPeriod, Timeout.Infinite);
                        _logger.LogInformation("Next adjusted reconnect scheduled in {Period} ms.", adjustedReconnectPeriod);
                        _reconnectPeriod = CheckedReconnectPeriod(_reconnectPeriod, true);
                        _state = ReconnectState.Triggered;
                    }
                }
            }
        }

        /// <summary>
        /// Reconnects to the server.
        /// </summary>
        private async Task<bool> DoReconnectAsync()
        {
            // helper to override operation timeout
            ITransportChannel? transportChannel = null;
            Debug.Assert(_session != null);

            // try a reconnect.
            if (!_reconnectFailed)
            {
                try
                {
                    if (_reverseConnectManager != null)
                    {
                        var connection = await _reverseConnectManager.WaitForConnection(
                                new Uri(_session.Endpoint.EndpointUrl),
                                _session.Endpoint.Server.ApplicationUri
                            ).ConfigureAwait(false);

                        await _session.ReconnectAsync(connection).ConfigureAwait(false);
                    }
                    else
                    {
                        await _session.ReconnectAsync().ConfigureAwait(false);
                    }

                    // monitored items should start updating on their own.
                    return true;
                }
                catch (Exception exception)
                {
                    // recreate the session if it has been closed.
                    if (exception is ServiceResultException sre)
                    {
                        _logger.LogWarning("Reconnect failed. Reason={Reason}.", sre.Result);

                        // check if the server endpoint could not be reached.
                        if (sre.StatusCode == StatusCodes.BadTcpInternalError ||
                            sre.StatusCode == StatusCodes.BadCommunicationError ||
                            sre.StatusCode == StatusCodes.BadNotConnected ||
                            sre.StatusCode == StatusCodes.BadRequestTimeout ||
                            sre.StatusCode == StatusCodes.BadTimeout)
                        {
                            // check if reactivating is still an option.
                            var timeout = Convert.ToInt32(_session.SessionTimeout) -
                                (HiResClock.TickCount - _session.LastKeepAliveTickCount);
                            if (timeout > 0)
                            {
                                _logger.LogInformation(
                                    "Retry to reactivate, est. session timeout in {Timeout} ms.",
                                    timeout);
                                return false;
                            }
                        }

                        // check if the security configuration may have changed
                        if (sre.StatusCode == StatusCodes.BadSecurityChecksFailed ||
                            sre.StatusCode == StatusCodes.BadCertificateInvalid)
                        {
                            _updateFromServer = true;
                            _logger.LogInformation("Reconnect failed due to security check. " +
                                "Request endpoint update from server. {Message}", sre.Message);
                        }
                        // recreate session immediately, use existing channel
                        else if (sre.StatusCode == StatusCodes.BadSessionIdInvalid)
                        {
                            transportChannel = _session.NullableTransportChannel;
                            _session.DetachChannel();
                        }
                        else
                        {
                            // wait for next scheduled reconnect if connection failed,
                            // next attempt is to recreate session
                            _reconnectFailed = true;
                            return false;
                        }
                    }
                    else
                    {
                        _logger.LogError(exception, "Reconnect failed.");
                    }

                    _reconnectFailed = true;
                }
            }

            // re-create the session.
            try
            {
                ISession session;
                if (_reverseConnectManager != null)
                {
                    ITransportWaitingConnection? connection;
                    do
                    {
                        connection = await _reverseConnectManager.WaitForConnection(
                            new Uri(_session.Endpoint.EndpointUrl),
                            _session.Endpoint.Server.ApplicationUri).ConfigureAwait(false);

                        if (_updateFromServer)
                        {
                            var endpoint = _session.ConfiguredEndpoint;
                            await endpoint.UpdateFromServerAsync(
                                endpoint.EndpointUrl, connection,
                                endpoint.Description.SecurityMode,
                                endpoint.Description.SecurityPolicyUri).ConfigureAwait(false);
                            _updateFromServer = false;
                            connection = null;
                        }
                    } while (connection == null);

                    session = await _session.SessionFactory.RecreateAsync(
                        _session, connection).ConfigureAwait(false);
                }
                else
                {
                    if (_updateFromServer)
                    {
                        var endpoint = _session.ConfiguredEndpoint;
                        await endpoint.UpdateFromServerAsync(
                            endpoint.EndpointUrl,
                            endpoint.Description.SecurityMode,
                            endpoint.Description.SecurityPolicyUri).ConfigureAwait(false);
                        _updateFromServer = false;
                    }

                    session = await _session.SessionFactory.RecreateAsync(
                        _session, transportChannel).ConfigureAwait(false);
                }
                // note: the template session is not connected at this point
                //       and must be disposed by the owner
                _session = session;
                return true;
            }
            catch (ServiceResultException sre)
            {
                if (sre.InnerResult?.StatusCode == StatusCodes.BadSecurityChecksFailed ||
                    sre.InnerResult?.StatusCode == StatusCodes.BadCertificateInvalid)
                {
                    // schedule endpoint update and retry
                    _updateFromServer = true;
                    if (_maxReconnectPeriod > MinReconnectPeriod &&
                        _reconnectPeriod >= _maxReconnectPeriod)
                    {
                        _reconnectPeriod = _baseReconnectPeriod;
                    }
                    _logger.LogError("Could not reconnect due to failed security check. " +
                        "Request endpoint update from server. {Message}", Redact.Create(sre));
                }
                else
                {
                    _logger.LogError("Could not reconnect the Session. {Message}",
                        Redact.Create(sre));
                }
                return false;
            }
            catch (Exception exception)
            {
                _logger.LogError("Could not reconnect the Session. {Message}",
                    Redact.Create(exception));
                return false;
            }
        }

        /// <summary>
        /// Reset the timer and enter ready state.
        /// </summary>
        private void EnterReadyState()
        {
            _reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _state = ReconnectState.Ready;
            _cancelReconnect = false;
            _updateFromServer = false;
        }

        private readonly object _lock = new();
        private readonly int _maxReconnectPeriod;
        private readonly Random _random;
        private readonly ILogger _logger;
        private readonly bool _reconnectAbort;
        private readonly Timer _reconnectTimer;

        private ISession? _session;
        private ReconnectState _state;
        private bool _reconnectFailed;
        private bool _cancelReconnect;
        private bool _updateFromServer;
        private int _reconnectPeriod;
        private int _baseReconnectPeriod;
        private EventHandler? _callback;
        private ReverseConnectManager? _reverseConnectManager;
    }
}
