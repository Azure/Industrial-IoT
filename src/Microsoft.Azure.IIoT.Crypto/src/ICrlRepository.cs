// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate revocation list repository
    /// </summary>
    public interface ICrlRepository {

        /// <summary>
        /// Invalidate entry in the repository
        /// </summary>
        /// <param name="serialNumber">Certificate serial number</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task InvalidateAsync(byte[] serialNumber,
            CancellationToken ct = default);
    }
}

