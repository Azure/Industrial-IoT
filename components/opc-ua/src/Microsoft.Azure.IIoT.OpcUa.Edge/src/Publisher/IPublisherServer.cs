// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using System.Threading.Tasks;

    /// <summary>
    /// Discover and connect to publisher server
    /// </summary>
    public interface IPublisherServer {

        /// <summary>
        /// Connect to publisher
        /// </summary>
        /// <returns></returns>
        Task<IPublisherClient> ConnectAsync();
    }
}
