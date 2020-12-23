// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Get endpoint certificate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICertificateServices<T> {

        /// <summary>
        /// Activate endpoint
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte[]> GetEndpointCertificateAsync(T id,
            CancellationToken ct = default);
    }
}
