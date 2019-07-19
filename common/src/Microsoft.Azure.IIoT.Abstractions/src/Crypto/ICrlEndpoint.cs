// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate revocation list endpoint
    /// </summary>
    public interface ICrlEndpoint {

        /// <summary>
        /// Get crl chain from cache using certificate serial number
        /// </summary>
        /// <param name="serialNumber">Certificate serial number</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Crl>> GetCrlChainAsync(byte[] serialNumber,
            CancellationToken ct = default);
    }
}

