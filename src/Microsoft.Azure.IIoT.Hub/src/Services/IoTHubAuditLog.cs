// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Diagnostics.Models;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Audit log writer using IoT Hub telemetry events
    /// </summary>
    public sealed class IoTHubAuditLog : IAuditLog {

        /// <summary>
        /// Create event log
        /// </summary>
        /// <param name="events"></param>
        public IoTHubAuditLog(IIoTHubTelemetryServices events) {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc/>
        public Task<IAuditLogWriter> OpenAsync(string deviceId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Task.FromResult<IAuditLogWriter>(
                new DeviceLogWriter(this, deviceId));
        }

        /// <summary>
        /// Wraps message service and device identifier to write for
        /// </summary>
        private class DeviceLogWriter : IAuditLogWriter {

            /// <summary>
            /// Create writer
            /// </summary>
            /// <param name="deviceId"></param>
            /// <param name="outer"></param>
            public DeviceLogWriter(IoTHubAuditLog outer, string deviceId) {
                _deviceId = deviceId;
                _outer = outer;
            }

            /// <inheritdoc/>
            public Task WriteAsync(AuditLogEntryModel entry) {
                return _outer._events.SendAsync(_deviceId, new EventModel {
                    Payload = JToken.FromObject(entry)
                });
            }

            private readonly string _deviceId;
            private readonly IoTHubAuditLog _outer;
        }

        private readonly IIoTHubTelemetryServices _events;
    }
}
