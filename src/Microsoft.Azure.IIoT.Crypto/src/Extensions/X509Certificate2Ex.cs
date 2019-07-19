// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.BouncyCastle;
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Certificate extensions
    /// </summary>
    public static class X509CertificateEx {

        /// <summary>
        /// Convert to pfx
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="privateKey"></param>
        /// <param name="password"></param>
        public static byte[] ToPfx(this X509Certificate2 certificate,
            Key privateKey, string password = null) {
            var cert = certificate.ToX509Certificate();
            return cert.ToPfx(privateKey.ToAsymmetricKeyParameter(), password);
        }
    }
}
