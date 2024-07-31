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
    /// Endpoint manager extensions
    /// </summary>
    internal static class EndpointManagerEx
    {
        /// <summary>
        /// Register endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manager"></param>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<string> RegisterEndpointAsync<T>(this IEndpointManager<T> manager,
            EndpointModel endpoint, T context = null, CancellationToken ct = default) where T : class
        {
            return manager.RegisterEndpointAsync(new ServerEndpointQueryModel
            {
                DiscoveryUrl = endpoint.Url,
                SecurityPolicy = endpoint.SecurityPolicy,
                SecurityMode = endpoint.SecurityMode,
                Certificate = endpoint.Certificate
            }, context, ct);
        }
    }
}
