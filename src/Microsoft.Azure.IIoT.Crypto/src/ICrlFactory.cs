// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Crl factory
    /// </summary>
    public interface ICrlFactory {

        /// <summary>
        /// Create signed revocation list
        /// </summary>
        /// <param name="issuer"></param>
        /// <param name="signature"></param>
        /// <param name="revoked"></param>
        /// <param name="nextUpdate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Crl> CreateCrlAsync(Certificate issuer, SignatureType signature,
            IEnumerable<Certificate> revoked = null, DateTime? nextUpdate = null,
            CancellationToken ct = default);
    }
}