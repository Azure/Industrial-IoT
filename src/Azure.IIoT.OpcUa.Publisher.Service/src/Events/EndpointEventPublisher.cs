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
    /// Endpoint registry event publisher
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class EndpointEventPublisher<THub> : IEndpointRegistryListener
    {
        /// <inheritdoc/>
        public EndpointEventPublisher(ICallbackInvoker<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnEndpointDeletedAsync(OperationContextModel? context,
            string endpointId, EndpointInfoModel endpoint)
        {
            return PublishAsync(EndpointEventType.Deleted, context,
                endpointId, endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointDisabledAsync(OperationContextModel? context,
            EndpointInfoModel endpoint)
        {
            return PublishAsync(EndpointEventType.Disabled, context,
                endpoint.Registration.Id, endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointEnabledAsync(OperationContextModel? context,
            EndpointInfoModel endpoint)
        {
            return PublishAsync(EndpointEventType.Enabled, context,
                endpoint.Registration.Id, endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointNewAsync(OperationContextModel? context,
            EndpointInfoModel endpoint)
        {
            return PublishAsync(EndpointEventType.New, context,
                endpoint.Registration.Id, endpoint);
        }

        /// <summary>
        /// Publish endpoint event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="endpointId"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task PublishAsync(EndpointEventType type,
            OperationContextModel? context, string? endpointId,
            EndpointInfoModel endpoint)
        {
            var arguments = new object[]
            {
                new EndpointEventModel
                {
                    EventType = type,
                    Context = context,
                    Id = endpointId,
                    Endpoint = endpoint
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.EndpointEventTarget, arguments);
        }

        private readonly ICallbackInvoker<THub> _callback;
    }
}
