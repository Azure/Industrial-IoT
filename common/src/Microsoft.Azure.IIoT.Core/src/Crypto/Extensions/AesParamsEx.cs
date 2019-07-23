// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Aes extensions
    /// </summary>
    public static class AesParamsEx {

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="aesProvider"></param>
        /// <returns></returns>
        public static Key ToKey(this Aes aesProvider) {
            return new Key {
                Type = KeyType.AES,
                Parameters = new AesParams {
                    K = aesProvider.Key
                }
            };
        }

        /// <summary>
        /// Convert to provider
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Aes ToAes(this Key key) {
            if (key.Type != KeyType.AES) {
                throw new ArgumentException("Not an aes key", nameof(key));
            }
            var aes = Aes.Create();
            aes.Key = (key.Parameters as AesParams).K;
            return aes;
        }

        /// <summary>
        /// Verifies whether this object has a private key
        /// </summary>
        /// <returns> True if the object has private key; false otherwise.</returns>
        public static bool HasPrivateKey(this AesParams key) {
            return key.K != null;
        }

        /// <summary>
        /// Clone params
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static AesParams Clone(this AesParams parameters) {
            if (parameters == null) {
                return null;
            }
            return new AesParams {
                K = parameters.K,
                T = parameters.T
            };
        }

        /// <summary>
        /// Compare params
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this AesParams parameters, AesParams other) {
            if (parameters == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.K, parameters.K)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.T, parameters.T)) {
                return false;
            }
            return true;
        }
    }
}

