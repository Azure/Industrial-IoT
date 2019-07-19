// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Default {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Application event broker stubs
    /// </summary>
    public sealed class ApplicationEventBrokerStub : IApplicationEventBroker,
        IApplicationRegistryEvents {

        /// <inheritdoc/>
        public Task NotifyAllAsync(Func<IApplicationRegistryListener, Task> evt) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Action Register(IApplicationRegistryListener listener) {
            return () => { };
        }
    }
}
