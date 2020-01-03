// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;

    /// <summary>
    /// Key parameter extensions
    /// </summary>
    public static class KeyParamsEx {

        /// <summary>
        /// Clone params
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static KeyParams Clone(this KeyParams parameters) {
            if (parameters == null) {
                return null;
            }
            switch (parameters) {
                case RsaParams rsa:
                    return rsa.Clone();
                case AesParams aes:
                    return aes.Clone();
                case EccParams ecc:
                    return ecc.Clone();
                default:
                    throw new NotSupportedException("Key type not supported");
            }
        }

        /// <summary>
        /// Compare parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this KeyParams parameters, KeyParams other) {
            if (parameters == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            switch (parameters) {
                case RsaParams rsa:
                    return rsa.SameAs(other as RsaParams);
                case AesParams aes:
                    return aes.SameAs(other as AesParams);
                case EccParams ecc:
                    return ecc.SameAs(other as EccParams);
                default:
                    throw new NotSupportedException("Key type not supported");
            }
        }
    }
}