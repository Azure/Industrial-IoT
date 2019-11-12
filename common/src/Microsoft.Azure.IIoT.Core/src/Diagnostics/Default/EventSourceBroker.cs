// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Event listener that brokers event notifications to subscribers
    /// </summary>
    public sealed class EventSourceBroker : EventListener, IEventSourceBroker {

        /// <inheritdoc/>
        public EventSourceBroker() {
            // Check log level changes every 30 seconds
            _timer = new Timer(_ => SyncEventLevels(), null,
                TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        /// <inheritdoc/>
        public override void Dispose() {
            _timer.Dispose();
            base.Dispose();
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(string eventSource, IEventSourceSubscriber subscriber) {
            if (subscriber == null) {
                throw new ArgumentNullException(nameof(subscriber));
            }
            if (string.IsNullOrEmpty(eventSource)) {
                throw new ArgumentNullException(nameof(eventSource));
            }
            var source = _subscribers.GetOrAdd(eventSource, 
                name => new EventSourceWrapper(this));
            return source.Add(subscriber);
        }

        /// <inheritdoc/>
        protected override void OnEventWritten(EventWrittenEventArgs eventData) {
            if (_subscribers.TryGetValue(eventData.EventSource.Name, out var eventSource)) {
                eventSource.OnEvent(eventData);
            }
        }

        /// <inheritdoc/>
        protected override void OnEventSourceCreated(EventSource eventSource) {
            _subscribers.AddOrUpdate(eventSource.Name,
                name => new EventSourceWrapper(this, eventSource),
                (name, source) => source.Connect(eventSource));
            base.OnEventSourceCreated(eventSource);
        }

        /// <summary>
        /// Update log level
        /// </summary>
        private void SyncEventLevels() {
            foreach (var eventSource in _subscribers.Values.ToList()) {
                eventSource.UpdateEventLevel();
            }
        }

        /// <summary>
        /// Event Source with subscriptions
        /// </summary>
        class EventSourceWrapper {

            /// <summary>
            /// Event source
            /// </summary>
            internal EventSource EventSource { get; set; }

            /// <summary>
            /// Create listener
            /// </summary>
            /// <param name="listener"></param>
            /// <param name="eventSource"></param>
            internal EventSourceWrapper(EventSourceBroker listener, 
                EventSource eventSource = null) {
                EventSource = eventSource;
                _listener = listener;
            }

            /// <summary>
            /// Log event
            /// </summary>
            /// <param name="eventData"></param>
            internal void OnEvent(EventWrittenEventArgs eventData) {
                lock (_subscriptions) {
                    _subscriptions.ForEach(s => s.OnEvent(eventData));
                }
            }

            /// <summary>
            /// Enable events
            /// </summary>
            internal void UpdateEventLevel() {
                if (EventSource != null) {
                    lock (_subscriptions) {
                        var level = _subscriptions.Any() ? 
                            _subscriptions.Max(s => s.Level) : EventLevel.LogAlways;
                        if (level != _enabledLevel) {
                            _enabledLevel = level;
                            _listener.EnableEvents(EventSource, level);
                        }
                    }
                }
            }

            /// <summary>
            /// Connect event source
            /// </summary>
            /// <param name="eventSource"></param>
            /// <returns></returns>
            internal EventSourceWrapper Connect(EventSource eventSource) {
                EventSource = eventSource;
                UpdateEventLevel();
                return this;
            }

            /// <summary>
            /// Add subscriber
            /// </summary>
            /// <param name="subscriber"></param>
            /// <returns></returns>
            internal EventSourceSubscriber Add(IEventSourceSubscriber subscriber) {
                var subscription = new EventSourceSubscriber(subscriber, this);
                lock(_subscriptions) {
                    _subscriptions.Add(subscription);
                }
                UpdateEventLevel();
                return subscription;
            }

            /// <summary>
            /// Remove subscriber
            /// </summary>
            /// <param name="eventSourceSubscriber"></param>
            internal void Remove(EventSourceSubscriber eventSourceSubscriber) {
                lock (_subscriptions) {
                    _subscriptions.Remove(eventSourceSubscriber);
                }
                UpdateEventLevel();
            }

            private EventLevel _enabledLevel = EventLevel.LogAlways;
            private readonly EventSourceBroker _listener;
            private readonly List<EventSourceSubscriber> _subscriptions = 
                new List<EventSourceSubscriber>();
        }

        /// <summary>
        /// Wraps event source subscriber
        /// </summary>
        class EventSourceSubscriber : IDisposable, IEventSourceSubscriber {

            /// <inheritdoc/>
            public EventLevel Level => _subscriber.Level;

            /// <summary>
            /// Create listener
            /// </summary>
            /// <param name="subscriber"></param>
            /// <param name="outer"></param>
            internal EventSourceSubscriber(
                IEventSourceSubscriber subscriber, EventSourceWrapper outer) {
                _subscriber = subscriber;
                _outer = outer;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _outer.Remove(this);
            }

            /// <inheritdoc/>
            public void OnEvent(EventWrittenEventArgs eventData) {
                if (eventData.Level <= Level) {
                    _subscriber.OnEvent(eventData);
                }
            }

            private readonly EventSourceWrapper _outer;
            private readonly IEventSourceSubscriber _subscriber;
        }

        private readonly ConcurrentDictionary<string, EventSourceWrapper> _subscribers =
            new ConcurrentDictionary<string, EventSourceWrapper>();
        private readonly Timer _timer;
    }
}