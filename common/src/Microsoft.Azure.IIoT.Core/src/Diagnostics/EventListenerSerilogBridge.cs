// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Diagnostics.Tracing;
    using System.Threading;

    /// <summary>
    /// Event listener bridge
    /// </summary>
    public sealed class EventListenerSerilogBridge : EventListener, IEventSourceBroker,
        IDisposable {

        /// <summary>
        /// Create bridge
        /// </summary>
        /// <param name="logger"></param>
        public EventListenerSerilogBridge(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lastLevel = LogEx.Level.MinimumLevel;

            // Check log level changes every 30 seconds
            _timer = new Timer(_ => EnableEvents(), null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <inheritdoc/>
        public void Subscribe(Guid eventSource) {
        }

        /// <inheritdoc/>
        public void Disable(Guid eventSource) {
        }

        /// <inheritdoc/>
        protected override void OnEventWritten(EventWrittenEventArgs eventData) {
            var level = ToLogEventLevel(eventData.Level);
            //if (_logger.IsEnabled(level)) {
            //    _logger.Write(level, "[{event}] {message} ({@data})",
            //        eventData.EventName, eventData.Message, eventData);
            //}
        }

        /// <inheritdoc/>
        public override void Dispose() {
            _timer.Dispose();
            base.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnEventSourceCreated(EventSource eventSource) {
            // Enable events for current log level
            EnableEvents(eventSource);
            base.OnEventSourceCreated(eventSource);
        }

        /// <summary>
        /// Enable events for event source
        /// </summary>
        /// <param name="eventSource"></param>
        private void EnableEvents(EventSource eventSource) {
            EnableEvents(eventSource, ToEventLevel(LogEx.Level.MinimumLevel));
        }

        /// <summary>
        /// Enable events
        /// </summary>
        private void EnableEvents() {
            if (_lastLevel != LogEx.Level.MinimumLevel) {
                foreach (var source in EventSource.GetSources()) {
                    EnableEvents(source);
                }
                _lastLevel = LogEx.Level.MinimumLevel;
            }
        }

        /// <summary>
        /// Convert to log event
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static LogEventLevel ToLogEventLevel(EventLevel level) {
            switch (level) {
                case EventLevel.Critical:
                    return LogEventLevel.Fatal;
                case EventLevel.Error:
                    return LogEventLevel.Error;
                case EventLevel.Informational:
                    return LogEventLevel.Debug;
                case EventLevel.Verbose:
                    return LogEventLevel.Verbose;
                case EventLevel.Warning:
                    return LogEventLevel.Warning;
                default:
                    return LogEventLevel.Debug;
            }
        }

        /// <summary>
        /// Convert to event level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static EventLevel ToEventLevel(LogEventLevel level) {
            switch (level) {
                case LogEventLevel.Fatal:
                    return EventLevel.Critical;
                case LogEventLevel.Error:
                    return EventLevel.Error;
                case LogEventLevel.Debug:
                case LogEventLevel.Information:
                    return EventLevel.Informational;
                case LogEventLevel.Verbose:
                    return EventLevel.Verbose;
                case LogEventLevel.Warning:
                    return EventLevel.Warning;
                default:
                    return EventLevel.LogAlways;
            }
        }

        private readonly ILogger _logger;
        private readonly Timer _timer;
        private volatile LogEventLevel _lastLevel;
    }
}