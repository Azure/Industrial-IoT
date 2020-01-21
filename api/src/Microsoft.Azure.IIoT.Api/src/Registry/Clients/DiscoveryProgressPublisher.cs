// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress publishing
    /// </summary>
    public sealed class DiscoveryProgressPublisher : IDiscoveryProgressProcessor,
        IDisposable {

        /// <summary>
        /// Create publisher
        /// </summary>
        /// <param name="callback"></param>
        public DiscoveryProgressPublisher(ICallbackInvoker callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task OnDiscoveryProgressAsync(DiscoveryProgressModel message) {
            var requestId = message.Request.Id;
            var arguments = new object[] { message.ToApiModel() };
            if (!string.IsNullOrEmpty(requestId)) {
                // Send to user
                await _callback.MulticastAsync(requestId,
                    EventTargets.DiscoveryProgressTarget, arguments);
            }
            if (!string.IsNullOrEmpty(message.DiscovererId)) {
                // Send to discovery listeners
                await _callback.MulticastAsync(message.DiscovererId,
                    EventTargets.DiscoveryProgressTarget, arguments);
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _callback.Dispose();
        }

        private readonly ICallbackInvoker _callback;
    }
}
