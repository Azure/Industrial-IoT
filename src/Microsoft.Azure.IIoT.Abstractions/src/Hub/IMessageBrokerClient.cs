// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using System.Threading.Tasks;

    /// <summary>
    /// Event broker
    /// </summary>
    public interface IMessageBrokerClient {

        /// <summary>
        /// Create client to event broker namespace/topic path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<IMessageClient> OpenAsync(string path);
    }
}
