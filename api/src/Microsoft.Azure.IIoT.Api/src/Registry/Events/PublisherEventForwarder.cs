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
    /// Publisher registry event publisher
    /// </summary>
    public class PublisherEventForwarder<THub> :
        EventBusCallbackBridge<THub, PublisherEventModel> {

        /// <inheritdoc/>
        public PublisherEventForwarder(IEventBus bus, ICallbackInvokerT<THub> callback,
            ILogger logger) : base(bus, callback, logger) {
        }

        /// <inheritdoc/>
        public override Task HandleAsync(PublisherEventModel eventData) {
            var arguments = new object[] { eventData.ToApiModel() };
            return Callback.BroadcastAsync(
                EventTargets.PublisherEventTarget, arguments);
        }
    }
}
