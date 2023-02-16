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
    /// Discoverer registry event publisher
    /// </summary>
    public class DiscovererEventForwarder<THub> : IEventHandler<DiscovererEventModel> {

        /// <inheritdoc/>
        public DiscovererEventForwarder(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task HandleAsync(DiscovererEventModel eventData) {
            var arguments = new object[] { eventData };
            return _callback.BroadcastAsync(
                EventTargets.DiscovererEventTarget, arguments);
        }

        private readonly ICallbackInvoker _callback;
    }
}
