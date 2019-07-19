// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Signs digest
    /// </summary>
    public interface IDigestSigner {

        /// <summary>
        /// Sign a hash with the signing key using the specified algorithm
        /// </summary>
        /// <param name="signingKey"></param>
        /// <param name="digest"></param>
        /// <param name="algorithm"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<byte[]> SignAsync(KeyHandle signingKey, byte[] digest,
            SignatureType algorithm, CancellationToken ct = default);

        /// <summary>
        /// Verify a signature
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="digest"></param>
        /// <param name="algorithm"></param>
        /// <param name="signature"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> VerifyAsync(KeyHandle handle, byte[] digest,
            SignatureType algorithm, byte[] signature,
            CancellationToken ct = default);
    }
}