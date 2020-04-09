// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Default {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Event broker stubs
    /// </summary>
    public sealed class EventBrokerStubT<T> : IRegistryEventBroker<T>,
        IRegistryEvents<T> where T : class {

        /// <inheritdoc/>
        public Task NotifyAllAsync(Func<T, Task> evt) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Action Register(T listener) {
            return () => { };
        }
    }
}
