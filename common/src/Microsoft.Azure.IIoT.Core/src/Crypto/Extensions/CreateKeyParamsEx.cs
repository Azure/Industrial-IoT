// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Create key parameters extensions
    /// </summary>
    public static class CreateKeyParamsEx {

        /// <summary>
        /// Clone create key params
        /// </summary>
        /// <param name="keyParams"></param>
        /// <returns></returns>
        public static CreateKeyParams Clone(this CreateKeyParams keyParams) {
            return new CreateKeyParams {
                Curve = keyParams.Curve,
                KeySize = keyParams.KeySize,
                Type = keyParams.Type
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="keyParams"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this CreateKeyParams keyParams, CreateKeyParams other) {

            if (keyParams == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            if (keyParams.Type != other.Type) {
                return false;
            }
            if (keyParams.Curve != other.Curve) {
                return false;
            }
            if (keyParams.KeySize != other.KeySize) {
                return false;
            }
            return true;
        }
    }
}

