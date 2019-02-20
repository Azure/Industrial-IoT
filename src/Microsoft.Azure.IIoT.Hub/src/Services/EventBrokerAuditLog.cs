// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Diagnostics.Models;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Log entry writer based on event broker
    /// </summary>
    public sealed class EventBrokerAuditLog : IAuditLog {

        /// <summary>
        /// Create event hub based audit log
        /// </summary>
        /// <param name="broker"></param>
        public EventBrokerAuditLog(IMessageBrokerClient broker) {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        /// <inheritdoc/>
        public async Task<IAuditLogWriter> OpenAsync(string log) {
            var client = await _broker.OpenAsync(log);
            return new EventBrokerWriter(client);
        }

        /// <summary>
        /// Client wrapper
        /// </summary>
        private class EventBrokerWriter : IAuditLogWriter {

            /// <summary>
            /// Create writer
            /// </summary>
            /// <param name="eventHub"></param>
            public EventBrokerWriter(IMessageClient eventHub) {
                _eventHub = eventHub;
            }

            /// <inheritdoc/>
            public Task WriteAsync(AuditLogEntryModel entry) =>
                _eventHub.SendAsync(JToken.FromObject(entry),
                    "application/x-audit-log-v1-json", entry.OperationId);

            private readonly IMessageClient _eventHub;
        }

        private readonly IMessageBrokerClient _broker;
    }
}
