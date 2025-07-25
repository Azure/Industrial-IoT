// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server discovery interface
    /// </summary>
    public interface IServerDiscovery
    {
        /// <summary>
        /// Find a server using the endpoint url in the query
        /// object. Returns a application registration object only
        /// if the endpoint is part of the application's endpoint
        /// list.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> FindServerAsync(
            ServerEndpointQueryModel query,
            CancellationToken ct = default);
    }
}
