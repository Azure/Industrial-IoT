// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry service events
    /// </summary>
    public interface IRegistryServiceEvents
    {
        /// <summary>
        /// Subscribe to application events
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeApplicationEventsAsync(
            Func<ApplicationEventModel?, Task> callback);

        /// <summary>
        /// Subscribe to endpoint events
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeEndpointEventsAsync(
            Func<EndpointEventModel?, Task> callback);

        /// <summary>
        /// Subscribe to gateway events
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeGatewayEventsAsync(
            Func<GatewayEventModel?, Task> callback);

        /// <summary>
        /// Subscribe to supervisor events
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeSupervisorEventsAsync(
            Func<SupervisorEventModel?, Task> callback);

        /// <summary>
        /// Subscribe to discoverer events
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeDiscovererEventsAsync(
            Func<DiscovererEventModel?, Task> callback);

        /// <summary>
        /// Subscribe to publisher events
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribePublisherEventsAsync(
            Func<PublisherEventModel?, Task> callback);

        /// <summary>
        /// Subscribe to supervisor discovery events
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeDiscoveryProgressByDiscovererIdAsync(
            string discovererId, Func<DiscoveryProgressModel?, Task> callback);

        /// <summary>
        /// Subscribe to discovery events for a particular request
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> SubscribeDiscoveryProgressByRequestIdAsync(
            string requestId, Func<DiscoveryProgressModel?, Task> callback);
    }
}
