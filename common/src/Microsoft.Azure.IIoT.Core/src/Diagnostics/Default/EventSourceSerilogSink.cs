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
    public class EventSourceSerilogSink : IEventSourceSubscriber {

        /// <summary>
        /// Level
        /// </summary>
        public EventLevel Level => ToEventLevel(LogControl.Level.MinimumLevel);

        /// <summary>
        /// Create bridge
        /// </summary>
        /// <param name="logger"></param>
        public EventSourceSerilogSink(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public virtual void OnEvent(EventWrittenEventArgs eventData) {
            WriteEvent(ToLogEventLevel(eventData.Level), eventData);
        }

        /// <summary>
        /// Write event to logger
        /// </summary>
        /// <param name="level"></param>
        /// <param name="eventData"></param>
        protected void WriteEvent(LogEventLevel level, EventWrittenEventArgs eventData) {
            var parameters = new object[eventData.Payload.Count + 4];
            parameters[0] = eventData.EventName;
            parameters[1] = eventData.Level;
            for (var i = 0; i < eventData.Payload.Count; i++) {
                parameters[2 + i] = eventData.Payload[i];
            }
            parameters[2 + eventData.Payload.Count + 0] = eventData.Message;
            parameters[2 + eventData.Payload.Count + 1] = eventData;
            var template = new StringBuilder();
            template.Append("[{event}] {level}: ");
            foreach (var name in eventData.PayloadNames) {
                template.Append("{");
                template.Append(name);
                template.Append("} ");
            }
            if (eventData.Message != null) {
                template.Append("{msg}");
            }
            _logger.Write(level, template.ToString(), parameters);
        }

        /// <summary>
        /// Convert to log event
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        protected static LogEventLevel ToLogEventLevel(EventLevel level) {
            switch (level) {
                case EventLevel.Critical:
                    return LogEventLevel.Fatal;
                case EventLevel.Warning:
                    return LogEventLevel.Warning;
                case EventLevel.Error:
                    return LogEventLevel.Error;
                case EventLevel.Informational:
                    return LogEventLevel.Information;
                case EventLevel.Verbose:
                    return LogEventLevel.Verbose;
                default:
                    return LogEventLevel.Debug;
            }
        }

        /// <summary>
        /// Convert to event level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        protected static EventLevel ToEventLevel(LogEventLevel level) {
            switch (level) {
                case LogEventLevel.Fatal:
                    return EventLevel.Critical;
                case LogEventLevel.Error:
                    return EventLevel.Error;
                case LogEventLevel.Warning:
                    return EventLevel.Warning;
                case LogEventLevel.Debug:
                case LogEventLevel.Information:
                    return EventLevel.Informational;
                case LogEventLevel.Verbose:
                    return EventLevel.Verbose;
                default:
                    return EventLevel.LogAlways;
            }
        }

        /// <summary> Logger </summary>
        protected readonly ILogger _logger;
    }
}