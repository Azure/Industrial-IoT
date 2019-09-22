// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Diagnostics.Tracing;
    using System.Text;

    /// <summary>
    /// Event source logger
    /// </summary>
    public sealed class EventSourceLogging : IEventSourceSubscriber {

        /// <summary>
        /// Level
        /// </summary>
        public EventLevel Level => ToEventLevel(LogEx.Level.MinimumLevel);

        /// <summary>
        /// Create bridge
        /// </summary>
        /// <param name="logger"></param>
        public EventSourceLogging(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void OnEvent(EventWrittenEventArgs eventData) {
            var level = ToLogEventLevel(eventData.Level);
            var parameters = new object[eventData.Payload.Count + 4];
            parameters[0] = eventData.Level;
            parameters[1] = eventData.EventName;
            for (var i = 0; i < eventData.Payload.Count; i++){
                parameters[2 + i] = eventData.Payload[i];
            }
            parameters[2 + eventData.Payload.Count + 0] = eventData.Message;
            parameters[2 + eventData.Payload.Count + 1] = eventData;
            var template = new StringBuilder();
            template.Append("[{level}] {event}:");
            foreach (var name in eventData.PayloadNames) {
                template.Append("{");
                template.Append(name);
                template.Append("}, ");
            }
            if (eventData.Message != null) {
                template.Append("message={message}");
            }
            else {
                template.Append("<No message>");
            }
            _logger.Write(level, template.ToString(), parameters);
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
    }
}