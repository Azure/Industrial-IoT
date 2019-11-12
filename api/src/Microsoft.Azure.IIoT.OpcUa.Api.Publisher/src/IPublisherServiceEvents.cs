// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;

    /// <summary>
    /// Publisher service events
    /// </summary>
    public interface IPublisherServiceEvents {

        /// <summary>
        /// Publisher samples from endpoint
        /// </summary>
        IEventSource<MonitoredItemMessageApiModel> Endpoint(
            string endpointId);
    }
}
