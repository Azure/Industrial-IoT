// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Threading.Tasks;

    /// <summary>
    /// Higher level event bus implementation
    /// </summary>
    public interface IEventBus {

        /// <summary>
        /// Publish message to subscribers
        /// </summary>
        /// <param name="message"></param>
        Task PublishAsync<T>(T message);

        /// <summary>
        /// Register handler
        /// </summary>
        /// <param name="handler"></param>
        Task<string> RegisterAsync<T>(IEventHandler<T> handler);

        /// <summary>
        /// Unregister
        /// </summary>
        /// <param name="token"></param>
        Task UnregisterAsync(string token);
    }
}
