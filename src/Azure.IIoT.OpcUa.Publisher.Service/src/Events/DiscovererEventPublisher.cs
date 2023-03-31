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
    /// Discoverer event publisher
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class DiscovererEventPublisher<THub> : IDiscovererRegistryListener
    {
        /// <inheritdoc/>
        public DiscovererEventPublisher(ICallbackInvoker<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnDiscovererDeletedAsync(OperationContextModel? context,
            string discovererId)
        {
            return PublishAsync(DiscovererEventType.Deleted, context,
                discovererId, null);
        }

        /// <inheritdoc/>
        public Task OnDiscovererNewAsync(OperationContextModel? context,
            DiscovererModel discoverer)
        {
            return PublishAsync(DiscovererEventType.New, context,
                discoverer.Id, discoverer);
        }

        /// <inheritdoc/>
        public Task OnDiscovererUpdatedAsync(OperationContextModel? context,
            DiscovererModel discoverer)
        {
            return PublishAsync(DiscovererEventType.Updated, context,
                discoverer.Id, discoverer);
        }

        /// <summary>
        /// Publish
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="discovererId"></param>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        public Task PublishAsync(DiscovererEventType type,
            OperationContextModel? context, string? discovererId,
            DiscovererModel? discoverer)
        {
            var arguments = new[] {
                new DiscovererEventModel {
                    EventType = type,
                    Context = context,
                    Id = discovererId,
                    Discoverer = discoverer
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.DiscovererEventTarget, arguments);
        }

        private readonly ICallbackInvoker<THub> _callback;
    }
}
