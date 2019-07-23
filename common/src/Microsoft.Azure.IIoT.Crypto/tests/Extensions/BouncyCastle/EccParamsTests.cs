// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Security.Cryptography;
    using Xunit;

    /// <summary>
    /// Ecc param test
    /// </summary>
    public static class EccParamsTests {

        [Fact]
        public static void ConvertToAndFromTest() {
            using (var ecdsa = ECDsa.Create()) {
                var key = ecdsa.ToKey();

                var eccparams1 = key.Parameters as EccParams;
                var eccparamsbpriv = eccparams1.ToECPrivateKeyParameters();
                var eccparamsbpub = eccparams1.ToECPublicKeyParameters();
                var eccparams2 = eccparamsbpub.ToEccParams(eccparamsbpriv);

                Assert.True(eccparams1.SameAs(eccparams2));
            }
        }
    }
}
