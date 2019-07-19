// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Revocation services
    /// </summary>
    public interface ICertificateRevoker {

        /// <summary>
        /// Revoke the certificate with the given serial
        /// number.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RevokeCertificateAsync(byte[] serialNumber,
            CancellationToken ct = default);
    }
}