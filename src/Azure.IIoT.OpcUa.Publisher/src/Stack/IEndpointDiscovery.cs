// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint discovery services extensions
    /// </summary>
    public interface IEndpointDiscovery
    {
        /// <summary>
        /// Try get unique set of endpoints from all servers found on discovery
        /// server endpoint url, filtered by optional prioritized locale list.
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="locales"></param>
        /// <param name="findServersOnNetwork"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<DiscoveredEndpointModel>> FindEndpointsAsync(
            Uri discoveryUrl, IReadOnlyList<string>? locales = null,
            bool findServersOnNetwork = true, CancellationToken ct = default);
    }
}
