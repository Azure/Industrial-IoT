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
    /// Get endpoint certificate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICertificateServices<T>
    {
        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            T endpoint, CancellationToken ct = default);
    }
}
