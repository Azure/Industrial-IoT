// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Security.Cryptography;
    using Xunit;

    /// <summary>
    /// Rsa param test
    /// </summary>
    public static class RsaParamsTests {

        [Fact]
        public static void ConvertToAndFromTest() {
            using (var ecdsa = RSA.Create()) {
                var key = ecdsa.ToKey();

                var rsaparams1 = key.Parameters as RsaParams;
                var rsaparamsb = rsaparams1.ToRsaKeyParameters();
                var rsaparams2 = rsaparamsb.ToRsaParams();

                Assert.True(rsaparams1.SameAs(rsaparams2));
            }
        }
    }
}
