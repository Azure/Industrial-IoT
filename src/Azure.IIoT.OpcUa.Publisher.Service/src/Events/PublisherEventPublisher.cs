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
    /// Publisher registry event publisher
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class PublisherEventPublisher<THub> : IPublisherRegistryListener
    {
        /// <inheritdoc/>
        public PublisherEventPublisher(ICallbackInvoker<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnPublisherDeletedAsync(OperationContextModel? context,
            string publisherId)
        {
            return PublishAsync(PublisherEventType.Deleted, context,
                publisherId, null);
        }

        /// <inheritdoc/>
        public Task OnPublisherNewAsync(OperationContextModel? context,
            PublisherModel publisher)
        {
            return PublishAsync(PublisherEventType.New, context,
                publisher.Id, publisher);
        }

        /// <inheritdoc/>
        public Task OnPublisherUpdatedAsync(OperationContextModel? context,
            PublisherModel publisher)
        {
            return PublishAsync(PublisherEventType.Updated, context,
                publisher.Id, publisher);
        }

        /// <summary>
        /// Publish publisher event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="publisherId"></param>
        /// <param name="publisher"></param>
        /// <returns></returns>
        public Task PublishAsync(PublisherEventType type,
            OperationContextModel? context, string? publisherId,
            PublisherModel? publisher)
        {
            var arguments = new object[] {
                new PublisherEventModel {
                    EventType = type,
                    Context = context,
                    Id = publisherId,
                    Publisher = publisher
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.PublisherEventTarget, arguments);
        }

        private readonly ICallbackInvoker<THub> _callback;
    }
}
