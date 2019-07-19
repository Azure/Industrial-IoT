// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Create key params extensions
    /// </summary>
    public static class CreateKeyParamsEx {

        /// <summary>
        /// Create key using local csps
        /// </summary>
        /// <param name="keyParams"></param>
        /// <returns></returns>
        public static Key CreateKey(this CreateKeyParams keyParams) {
            switch (keyParams.Type) {
                case KeyType.RSA:
                    using (var rsa = RSA.Create((int)(keyParams.KeySize ?? 2048))) {
                        return rsa.ToKey();
                    }
                case KeyType.ECC:
                    using (var ecc = ECDsa.Create(keyParams.Curve?.ToECCurve()
                        ?? ECCurve.NamedCurves.nistP256)) {
                        return ecc.ToKey();
                    }
                case KeyType.AES:
                    using (var aes = Aes.Create()) {
                        aes.KeySize = (int)keyParams.KeySize;
                        aes.GenerateKey();
                        return aes.ToKey();
                    }
                default:
                    throw new NotSupportedException("Key type not supported");
            }
        }
    }
}

