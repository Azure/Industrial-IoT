// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Endpoint registry event publisher
    /// </summary>
    public class DiscoveryProgressForwarder<THub> : IEventHandler<DiscoveryProgressModel> {

        /// <inheritdoc/>
        public DiscoveryProgressForwarder(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(DiscoveryProgressModel eventData) {
            if (eventData.TimeStamp + TimeSpan.FromSeconds(10) < DateTime.UtcNow) {
                // Do not forward stale events - todo make configurable / add metric
                return;
            }
            var requestId = eventData.Request?.Id;
            var arguments = new object[] { eventData.ToApiModel() };
            if (!string.IsNullOrEmpty(requestId)) {
                // Send to user
                await _callback.MulticastAsync(requestId,
                    EventTargets.DiscoveryProgressTarget, arguments);
            }
            if (!string.IsNullOrEmpty(eventData.DiscovererId)) {
                // Send to discovery listeners
                await _callback.MulticastAsync(eventData.DiscovererId,
                    EventTargets.DiscoveryProgressTarget, arguments);
            }
        }

        private readonly ICallbackInvoker _callback;
    }
}
