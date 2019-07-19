// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Crypto;
    using System;

    /// <summary>
    /// Key extensions
    /// </summary>
    internal static class KeyEx {

        /// <summary>
        /// Convert to internal key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static AsymmetricKeyParameter ToAsymmetricKeyParameter(this Key key) {
            switch (key.Type) {
                case KeyType.RSA:
                    return (key.Parameters as RsaParams).ToRsaKeyParameters();
                case KeyType.ECC:
                    return (key.Parameters as EccParams).ToECKeyParameters();
                default:
                    throw new NotSupportedException("Key type not supported");
            }
        }
    }
}