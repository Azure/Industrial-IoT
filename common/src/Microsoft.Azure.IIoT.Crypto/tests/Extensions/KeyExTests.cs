// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Xunit;

    public static class KeyExTests {

        [Fact]
        public static void RSAConvertBackAndForth() {

            using (var rsa = RSA.Create()) {
                var key1 = rsa.ToPublicKey();
                var pk = key1.ToPublicKey();
                var key2 = pk.ToKey();

                Assert.True(key1.SameAs(key2));
            }
        }

        // [Fact]
        // public static void ECCConvertBackAndForth() {
        //
        //     using (var ecdsa = ECDsa.Create()) {
        //         var key1 = ecdsa.ToPublicKey();
        //         var pk = key1.ToPublicKey();
        //         var key2 = pk.ToKey();
        //
        //         Assert.True(key1.SameAs(key2));
        //     }
        // }
    }
}

