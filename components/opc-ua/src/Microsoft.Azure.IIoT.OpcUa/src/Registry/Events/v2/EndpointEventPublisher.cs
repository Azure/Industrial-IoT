// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry event publisher
    /// </summary>
    public class EndpointEventPublisher : IEndpointRegistryListener {

        /// <summary>
        /// Create event publisher
        /// </summary>
        /// <param name="bus"></param>
        public EndpointEventPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnEndpointActivatedAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.Activated, context, endpoint));
        }

        /// <inheritdoc/>
        public Task OnEndpointDeactivatedAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.Deactivated, context, endpoint));
        }

        /// <inheritdoc/>
        public Task OnEndpointDeletedAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.Deleted, context, endpoint));
        }

        /// <inheritdoc/>
        public Task OnEndpointDisabledAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.Disabled, context, endpoint));
        }

        /// <inheritdoc/>
        public Task OnEndpointEnabledAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.Enabled, context, endpoint));
        }

        /// <inheritdoc/>
        public Task OnEndpointNewAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.New, context, endpoint));
        }

        /// <inheritdoc/>
        public Task OnEndpointUpdatedAsync(
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return _bus.PublishAsync(Wrap(EndpointEventType.Updated, context, endpoint));
        }

        /// <summary>
        /// Create endpoint event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static EndpointEventModel Wrap(EndpointEventType type,
            RegistryOperationContextModel context, EndpointInfoModel endpoint) {
            return new EndpointEventModel {
                EventType = type,
                Context = context,
                Endpoint = endpoint
            };
        }

        private readonly IEventBus _bus;
    }
}
