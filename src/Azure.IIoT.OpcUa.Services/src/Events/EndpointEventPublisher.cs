// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Events {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry event publisher
    /// </summary>
    public class EndpointEventPublisher<THub> : IEndpointRegistryListener {

        /// <inheritdoc/>
        public EndpointEventPublisher(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnEndpointDeletedAsync(
            RegistryOperationContextModel context, string endpointId, EndpointInfoModel endpoint) {
            return PublishAsync(EndpointEventType.Deleted, context,
                endpointId, endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointDisabledAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return PublishAsync(EndpointEventType.Disabled, context,
                endpoint.Registration.Id, endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointEnabledAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return PublishAsync(EndpointEventType.Enabled, context,
                endpoint.Registration.Id, endpoint);
        }

        /// <inheritdoc/>
        public Task OnEndpointNewAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
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
            RegistryOperationContextModel context, string endpointId,
            EndpointInfoModel endpoint) {
            var arguments = new object[] {
                    new EndpointEventModel {
                    EventType = type,
                    Context = context,
                    Id = endpointId,
                    Endpoint = endpoint
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.EndpointEventTarget, arguments);
        }

        private readonly ICallbackInvoker _callback;
    }
}
