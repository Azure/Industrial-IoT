// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Registry service events
    /// </summary>
    public interface IRegistryServiceEvents {

        /// <summary>
        /// Subscribe to application events
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeApplicationEventsAsync(
            string userId, Func<ApplicationEventApiModel, Task> callback);

        /// <summary>
        /// Subscribe to endpoint events
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeEndpointEventsAsync(
            string userId, Func<EndpointEventApiModel, Task> callback);

        /// <summary>
        /// Subscribe to gateway events
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeGatewayEventsAsync(
            string userId, Func<GatewayEventApiModel, Task> callback);

        /// <summary>
        /// Subscribe to supervisor events
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeSupervisorEventsAsync(
            string userId, Func<SupervisorEventApiModel, Task> callback);

        /// <summary>
        /// Subscribe to discoverer events
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeDiscovererEventsAsync(
            string userId, Func<DiscovererEventApiModel, Task> callback);

        /// <summary>
        /// Subscribe to publisher events
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribePublisherEventsAsync(
            string userId, Func<PublisherEventApiModel, Task> callback);

        /// <summary>
        /// Subscribe to supervisor discovery events
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeDiscoveryProgressByDiscovererIdAsync(
            string discovererId, string userId, Func<DiscoveryProgressApiModel, Task> callback);

        /// <summary>
        /// Subscribe to discovery events for a particular request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="userId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeDiscoveryProgressByRequestIdAsync(
            string requestId, string userId, Func<DiscoveryProgressApiModel, Task> callback);
    }
}
