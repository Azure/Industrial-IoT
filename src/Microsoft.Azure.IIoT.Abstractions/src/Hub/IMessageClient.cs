// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Messaging client
    /// </summary>
    public interface IMessageClient : IEventClient, IDisposable {

        /// <summary>
        /// Send the provided message to the topic
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="properties"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        Task SendAsync(byte[] payload, IDictionary<string, string> properties,
            string partitionKey = null);

        /// <summary>
        /// Close topic
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
