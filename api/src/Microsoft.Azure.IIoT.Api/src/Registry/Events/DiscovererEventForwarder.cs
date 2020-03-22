// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Discoverer registry event publisher
    /// </summary>
    public class DiscovererEventForwarder<THub> :
        EventBusCallbackBridge<THub, DiscovererEventModel> {

        /// <inheritdoc/>
        public DiscovererEventForwarder(IEventBus bus, ICallbackInvokerT<THub> callback,
            ILogger logger) : base(bus, callback, logger) {
        }

        /// <inheritdoc/>
        public override Task HandleAsync(DiscovererEventModel eventData) {
            var arguments = new object[] { eventData.ToApiModel() };
            return Callback.BroadcastAsync(
                EventTargets.DiscovererEventTarget, arguments);
        }
    }
}
