// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server endpoint manager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEndpointManager<T> where T : class
    {
        /// <summary>
        /// Find the endpoint and server application information that
        /// matches the endpoint query and register it in the registry.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns>The endpoint id if succussful</returns>
        Task<string> RegisterEndpointAsync(ServerEndpointQueryModel query,
            T? context = null, CancellationToken ct = default);
    }
}
