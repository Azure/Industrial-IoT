// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Events
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer progress processor
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class DiscoveryProgressPublisher<THub> : IDiscoveryProgressProcessor
    {
        /// <inheritdoc/>
        public DiscoveryProgressPublisher(ICallbackInvoker<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task OnDiscoveryProgressAsync(DiscoveryProgressModel message)
        {
            if (message.TimeStamp + TimeSpan.FromSeconds(10) < DateTime.UtcNow)
            {
                // Do not forward stale events - todo make configurable / add metric
                return;
            }
            var requestId = message.Request?.Id;
            var arguments = new object[] { message };
            if (!string.IsNullOrEmpty(requestId))
            {
                // Send to user
                await _callback.MulticastAsync(requestId,
                    EventTargets.DiscoveryProgressTarget, arguments).ConfigureAwait(false);
            }
            if (!string.IsNullOrEmpty(message.DiscovererId))
            {
                // Send to discovery listeners
                await _callback.MulticastAsync(message.DiscovererId,
                    EventTargets.DiscoveryProgressTarget, arguments).ConfigureAwait(false);
            }
        }

        private readonly ICallbackInvoker<THub> _callback;
    }
}
