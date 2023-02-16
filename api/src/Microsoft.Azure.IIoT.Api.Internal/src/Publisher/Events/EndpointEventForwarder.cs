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
    /// Endpoint registry event publisher
    /// </summary>
    public class EndpointEventForwarder<THub> : IEventHandler<EndpointEventModel> {

        /// <inheritdoc/>
        public EndpointEventForwarder(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task HandleAsync(EndpointEventModel eventData) {
            var arguments = new object[] { eventData };
            return _callback.BroadcastAsync(
                EventTargets.EndpointEventTarget, arguments);
        }
        private readonly ICallbackInvoker _callback;
    }
}
