// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using System.Threading;

    /// <summary>
    /// Implementation of a client factory that is a slightly less fancy version
    /// of the the one implemented in asp.net core and uses a handler builder
    /// instead of the asp.net builder.
    /// </summary>
    public sealed class HttpClientFactory : IHttpClientFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public HttpClientFactory(IHttpHandlerFactory factory, ILogger logger) {

            _factory = factory ?? new HttpHandlerFactory(logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _activeHandlers = new ConcurrentDictionary<string, ActiveHandlerEntry>(
                StringComparer.Ordinal);
            _expiredHandlers = new ConcurrentQueue<ExpiredHandlerEntry>();
        }

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="logger"></param>
        public HttpClientFactory(ILogger logger) : this(null, logger) { }

        /// <inheritdoc/>
        public System.Net.Http.HttpClient CreateClient(string resourceId) {
            if (resourceId == null) {
                throw new ArgumentNullException(nameof(resourceId));
            }
            // Get handler entry for client
            var entry = _activeHandlers.GetOrAdd(resourceId,
                key => ActiveHandlerEntry.Create(_factory, key, OnHandlerExpired));

            return new System.Net.Http.HttpClient(entry, false);
        }

        /// <summary>
        /// Immutable entry in expired queue
        /// </summary>
        private class ExpiredHandlerEntry : IDisposable {

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="other"></param>
            public ExpiredHandlerEntry(ActiveHandlerEntry other) {
                Name = other.Name;
                _livenessTracker = new WeakReference(other);
                _innerHandler = other.InnerHandler;
            }

            /// <summary>
            /// Check disposable
            /// </summary>
            public bool CanDispose => !_livenessTracker.IsAlive;

            /// <summary>
            /// Name of entry for logging
            /// </summary>
            public string Name { get; }

            /// <inheritdoc/>
            public void Dispose() {
                _innerHandler.Dispose();
            }

            private readonly WeakReference _livenessTracker;
            private readonly HttpMessageHandler _innerHandler;
        }

        /// <summary>
        /// Immutable entry in active handler queue
        /// </summary>
        private class ActiveHandlerEntry : DelegatingHandler {

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="name"></param>
            /// <param name="handler"></param>
            /// <param name="lifetime"></param>
            /// <param name="expirationCallback"></param>
            private ActiveHandlerEntry(string name, HttpMessageHandler handler,
                TimeSpan lifetime, Action<ActiveHandlerEntry> expirationCallback) :
                base(handler) {
                Name = name;
                _expired = expirationCallback;
                if (lifetime != Timeout.InfiniteTimeSpan) {
                    _timer = new Timer(OnTimer, null, lifetime, Timeout.InfiniteTimeSpan);
                }
            }

            /// <summary>
            /// Create handler entry
            /// </summary>
            /// <param name="factory"></param>
            /// <param name="name"></param>
            /// <param name="expirationCallback"></param>
            /// <returns></returns>
            public static ActiveHandlerEntry Create(IHttpHandlerFactory factory,
                string name, Action<ActiveHandlerEntry> expirationCallback) {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                var lifetime = factory.Create(name, out var handler);
#pragma warning restore IDE0067 // Dispose objects before losing scope
                return new ActiveHandlerEntry(name, handler, lifetime, expirationCallback);
            }

            /// <summary>
            /// Name of the handler
            /// </summary>
            public string Name { get; }

            private void OnTimer(object state) {
                _timer.Dispose();
                _timer = null;
                _expired(this);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                base.Dispose(disposing);
                if (disposing) {
                    _timer?.Dispose();
                }
            }

            private Timer _timer;
            private readonly Action<ActiveHandlerEntry> _expired;
        }


        /// <summary>
        /// Called when handler entry expired.  Removes from active list
        /// and adds weak reference to expiry queue.  When garbage collected
        /// expired handlers are cleaned up.
        /// </summary>
        /// <param name="active"></param>
        private void OnHandlerExpired(ActiveHandlerEntry active) {
#pragma warning disable IDE0067 // Dispose objects before losing scope
            _activeHandlers.TryRemove(active.Name, out _);
            _expiredHandlers.Enqueue(new ExpiredHandlerEntry(active));
#pragma warning restore IDE0067 // Dispose objects before losing scope
            StartCleanupTimer();
        }

        /// <summary>
        /// Cleanup every 10 seconds
        /// </summary>
        private void StartCleanupTimer() {
            lock (_cleanupTimerLock) {
                if (_cleanupTimer != null) {
                    return;
                }
                _cleanupTimer = new Timer(OnCleanup, null,
                    TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Stop cleanup
        /// </summary>
        private void StopCleanupTimer() {
            lock (_cleanupTimerLock) {
                _cleanupTimer.Dispose();
                _cleanupTimer = null;
            }
        }

        /// <summary>
        /// Clean up expired handlers
        /// </summary>
        /// <param name="state"></param>
        private void OnCleanup(object state) {
            StopCleanupTimer();
            if (Monitor.TryEnter(_cleanupActiveLock)) {
                try {
                    var initialCount = _expiredHandlers.Count;
                    for (var i = 0; i < initialCount; i++) {
#pragma warning disable IDE0068 // Use recommended dispose pattern
                        _expiredHandlers.TryDequeue(out var entry);
#pragma warning restore IDE0068 // Use recommended dispose pattern
                        if (entry.CanDispose) {
                            try {
                                entry.Dispose();
                            }
                            catch (Exception ex) {
                                _logger.Error(ex, "Failed to cleanup handler {name}",
                                    entry.Name);
                            }
                        }
                        else {
                            // If the entry is still live, put it back in the
                            // queue so we can process it during the next cycle.
                            _expiredHandlers.Enqueue(entry);
                        }
                    }
                }
                finally {
                    Monitor.Exit(_cleanupActiveLock);
                }
            }
            if (_expiredHandlers.Count > 0) {
                StartCleanupTimer();
            }
        }

        private Timer _cleanupTimer;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _cleanupTimerLock = new SemaphoreSlim(1, 1);
        private readonly object _cleanupActiveLock = new object();
        private readonly ConcurrentDictionary<string, ActiveHandlerEntry> _activeHandlers;
        private readonly ConcurrentQueue<ExpiredHandlerEntry> _expiredHandlers;
        private readonly IHttpHandlerFactory _factory;
    }
}
