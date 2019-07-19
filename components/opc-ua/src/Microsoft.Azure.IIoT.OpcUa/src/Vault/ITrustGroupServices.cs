// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Trust group services
    /// </summary>
    public interface ITrustGroupServices {

        /// <summary>
        /// Renews a trust group certificate.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="ct"></param>
        /// <returns>The new Issuer CA cert</returns>
        Task RenewCertificateAsync(string groupId,
            CancellationToken ct = default);
    }
}
