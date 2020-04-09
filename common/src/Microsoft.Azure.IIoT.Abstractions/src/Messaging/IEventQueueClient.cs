// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Messaging client
    /// </summary>
    public interface IEventQueueClient : IEventClient, IDisposable {

        /// <summary>
        /// Send the provided message
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="properties"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        Task SendAsync(byte[] payload, IDictionary<string, string> properties = null,
            string partitionKey = null);

        /// <summary>
        /// Close client
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
