// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Application registry event publisher
    /// </summary>
    public class ApplicationEventForwarder<THub> :
        EventBusCallbackBridge<THub, ApplicationEventModel> {

        /// <inheritdoc/>
        public ApplicationEventForwarder(IEventBus bus, ICallbackInvokerT<THub> callback,
            ILogger logger) : base(bus, callback, logger) {
        }

        /// <inheritdoc/>
        public override Task HandleAsync(ApplicationEventModel eventData) {
            var arguments = new object[] { eventData.ToApiModel() };
            return Callback.BroadcastAsync(
                EventTargets.ApplicationEventTarget, arguments);
        }
    }
}
