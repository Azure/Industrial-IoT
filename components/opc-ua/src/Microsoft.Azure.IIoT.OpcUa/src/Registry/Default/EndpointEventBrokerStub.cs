// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Default {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint event broker stubs
    /// </summary>
    public sealed class EndpointEventBrokerStub : IEndpointEventBroker,
        IEndpointRegistryEvents {

        /// <inheritdoc/>
        public Task NotifyAllAsync(Func<IEndpointRegistryListener, Task> evt) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Action Register(IEndpointRegistryListener listener) {
            return () => { };
        }
    }
}
