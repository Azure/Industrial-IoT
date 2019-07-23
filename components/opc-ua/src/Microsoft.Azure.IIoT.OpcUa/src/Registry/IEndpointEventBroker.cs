// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry event broker
    /// </summary>
    public interface IEndpointEventBroker {

        /// <summary>
        /// Notify all listeners
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        Task NotifyAllAsync(Func<IEndpointRegistryListener, Task> evt);
    }
}
