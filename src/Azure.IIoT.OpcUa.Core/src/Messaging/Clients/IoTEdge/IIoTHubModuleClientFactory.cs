// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using global::IoTHubby;
    using System;

    /// <summary>
    /// Creates module clients for the IoT Edge messaging stack.
    /// </summary>
    public interface IIoTHubModuleClientFactory
    {
        /// <summary>
        /// Create module client.
        /// </summary>
        IIoTHubModuleClient Create(IoTEdgeClientOptions options,
            Action<IoTHubClientOptions> configure);
    }
}
