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
    /// Publisher service events
    /// </summary>
    public interface IPublisherServiceEvents
    {
        /// <summary>
        /// Subscribe to monitored item messages
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> NodePublishSubscribeByEndpointAsync(
            string endpointId, Func<MonitoredItemMessageModel?, Task> callback);
    }
}
