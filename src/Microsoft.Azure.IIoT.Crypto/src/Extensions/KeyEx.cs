// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Key extensions
    /// </summary>
    public static class KeyEx {

        /// <summary>
        /// Convert to public key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static PublicKey ToPublicKey(this Key key) {
            switch (key.Type) {
                case KeyType.RSA:
                    return (key.Parameters as RsaParams).ToPublicKey();
                case KeyType.ECC:
                    return (key.Parameters as EccParams).ToPublicKey();
                default:
                    throw new NotSupportedException("Key type not supported");
            }
        }

        /// <summary>
        /// Convert public key to key
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static Key ToKey(this PublicKey publicKey) {
            if (publicKey == null) {
                return null;
            }
            if (publicKey.Oid.Value == Oids.Rsa) {
                return publicKey.Key.ToPublicKey();
            }
            if (publicKey.Oid.Value == Oids.EcPublicKey) {
                // TODO
            }
            throw new NotSupportedException("Key type not supported");
        }
    }
}