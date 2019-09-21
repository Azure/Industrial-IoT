// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;

    /// <summary>
    /// Event listener bridge
    /// </summary>
    public sealed class EventSourceBroker : EventListener, IEventSourceBroker {

        /// <inheritdoc/>
        public IEventSourceSubscription Subscribe(string eventSource,
            IEventSourceSubscriber subscriber) {
            if (subscriber == null) {
                throw new ArgumentNullException(nameof(subscriber));
            }
            if (string.IsNullOrEmpty(eventSource)) {
                throw new ArgumentNullException(nameof(eventSource));
            }
            var subscription = new EventSourceSubscription(eventSource, subscriber, this);
            lock (_subscribers) {
                if (!_subscribers.TryGetValue(eventSource, out var subscriptions)) {
                    subscriptions = new List<EventSourceSubscription>();
                }
                subscriptions.Add(subscription);
            }
            return subscription;
        }

        /// <inheritdoc/>
        protected override void OnEventWritten(EventWrittenEventArgs eventData) {
            lock (_subscribers) {
                if (_subscribers.TryGetValue(eventData.EventSource.Name, out var subscriptions)) {
                    subscriptions.ForEach(s => s.OnEvent(eventData));
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnEventSourceCreated(EventSource eventSource) {
            lock (_subscribers) {
                if (_subscribers.TryGetValue(eventSource.Name, out var subscriptions)) {
                    subscriptions.ForEach(s => s.EventSource = eventSource);
                    EnableEvents(eventSource, subscriptions.Max(s => s.Level));
                }
            }
            base.OnEventSourceCreated(eventSource);
        }

        /// <summary>
        /// Update log level
        /// </summary>
        /// <param name="eventSource"></param>
        private void OnLogLevelChanged(EventSource eventSource) {
            lock (_subscribers) {
                if (_subscribers.TryGetValue(eventSource.Name, out var subscriptions)) {
                    EnableEvents(eventSource, subscriptions.Max(s => s.Level));
                }
            }
        }

        /// <summary>
        /// Update log level
        /// </summary>
        /// <param name="eventSource"></param>
        /// <param name="subscription"></param>
        private void Unsubscribe(string eventSource, EventSourceSubscription subscription) {
            lock (_subscribers) {
                if (_subscribers.TryGetValue(eventSource, out var subscriptions)) {
                    subscriptions.Remove(subscription);
                }
            }
        }

        /// <summary>
        /// Connects listener to event source
        /// </summary>
        class EventSourceSubscription : IEventSourceSubscription {

            /// <summary>
            /// Event source
            /// </summary>
            internal EventSource EventSource { get; set; }

            /// <inheritdoc/>
            public EventLevel Level {
                get => _level;
                set {
                    _level = value;
                    if (EventSource != null) {
                        _listener.OnLogLevelChanged(EventSource);
                    }
                }
            }

            /// <summary>
            /// Create listener
            /// </summary>
            /// <param name="source"></param>
            /// <param name="subscriber"></param>
            /// <param name="listener"></param>
            internal EventSourceSubscription(string source,
                IEventSourceSubscriber subscriber, EventSourceBroker listener) {
                _sourceName = source;
                _subscriber = subscriber;
                _listener = listener;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _listener.Unsubscribe(_sourceName, this);
            }

            /// <summary>
            /// Log event
            /// </summary>
            /// <param name="eventData"></param>
            internal void OnEvent(EventWrittenEventArgs eventData) {
                if (eventData.Level <= _level) {
                    _subscriber.OnEvent(eventData);
                }
            }

            private readonly EventSourceBroker _listener;
            private readonly IEventSourceSubscriber _subscriber;
            private readonly string _sourceName;
            private EventLevel _level;
        }

        private readonly Dictionary<string, List<EventSourceSubscription>> _subscribers =
            new Dictionary<string, List<EventSourceSubscription>>();
    }
}