// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.Default {
    using System;

    /// <summary>
    /// Event source adapter for subscriber
    /// </summary>
    public class EventSource<T> : IEventSource<T> {

        /// <inheritdoc/>
        public ICallbackRegistration Subscriber { get; set; }

        /// <inheritdoc/>
        public event Action<T> Events {
            add {
                lock (_lock) {
                    if (_events == null) {
                        Add?.Invoke();
                        _registration = Subscriber.Register<T>(Method, Invoke);
                    }
                    _events += value;
                }
            }
            remove {
                lock (_lock) {
                    if (_events == null) {
                        return;
                    }
                    _events -= value;
                    if (_events == null) {
                        _registration?.Dispose();
                        Remove?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Method
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Call before first event is added
        /// </summary>
        public Action Add { get; set; }

        /// <summary>
        /// Call when the last event is removed
        /// </summary>
        public Action Remove { get; set; }

        /// <summary>
        /// Deliver event to delegates
        /// </summary>
        /// <param name="eventData"></param>
        public void Invoke(T eventData) {
            _events?.Invoke(eventData);
        }

        private readonly object _lock = new object();
#pragma warning disable IDE1006 // Naming Styles
        private event Action<T> _events;
#pragma warning restore IDE1006 // Naming Styles
        private IDisposable _registration;
    }
}
