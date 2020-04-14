// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher service events
    /// </summary>
    public interface IPublisherServiceEvents {

        /// <summary>
        /// Subscribe to monitored item messages
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task<IAsyncDisposable> NodePublishSubscribeByEndpointAsync(
            string endpointId, Func<MonitoredItemMessageApiModel, Task> callback);
    }
}
