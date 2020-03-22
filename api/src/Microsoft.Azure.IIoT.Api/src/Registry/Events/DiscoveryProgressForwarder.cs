﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Endpoint registry event publisher
    /// </summary>
    public class DiscoveryProgressForwarder<THub> :
        EventBusCallbackBridge<THub, DiscoveryProgressModel> {

        /// <inheritdoc/>
        public DiscoveryProgressForwarder(IEventBus bus, ICallbackInvokerT<THub> callback,
            ILogger logger) : base(bus, callback, logger) {
        }

        /// <inheritdoc/>
        public async override Task HandleAsync(DiscoveryProgressModel eventData) {
            var requestId = eventData.Request?.Id;
            var arguments = new object[] { eventData.ToApiModel() };
            if (!string.IsNullOrEmpty(requestId)) {
                // Send to user
                await Callback.MulticastAsync(requestId,
                    EventTargets.DiscoveryProgressTarget, arguments);
            }
            if (!string.IsNullOrEmpty(eventData.DiscovererId)) {
                // Send to discovery listeners
                await Callback.MulticastAsync(eventData.DiscovererId,
                    EventTargets.DiscoveryProgressTarget, arguments);
            }
        }
    }
}
