// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Registry.Events {
    using Microsoft.Azure.IIoT.Api;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Gateway registry event publisher
    /// </summary>
    public class GatewayEventForwarder<THub> : IEventHandler<GatewayEventModel> {

        /// <inheritdoc/>
        public GatewayEventForwarder(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task HandleAsync(GatewayEventModel eventData) {
            var arguments = new object[] { eventData };
            return _callback.BroadcastAsync(
                EventTargets.GatewayEventTarget, arguments);
        }
        private readonly ICallbackInvoker _callback;
    }
}
