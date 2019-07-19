// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Revocation info extensions
    /// </summary>
    public static class RevocationInfoEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="revoked"></param>
        /// <returns></returns>
        public static RevocationInfo Clone(this RevocationInfo revoked) {
            if (revoked == null) {
                return null;
            }
            return new RevocationInfo {
                Date = revoked.Date
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="revoked"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this RevocationInfo revoked, RevocationInfo other) {
            if (revoked == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            if (revoked.Date != other.Date) {
                return false;
            }
            return true;
        }
    }
}
