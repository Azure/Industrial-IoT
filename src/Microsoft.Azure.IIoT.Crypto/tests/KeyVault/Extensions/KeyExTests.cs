// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.KeyVault.WebKey {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Security.Cryptography;
    using Xunit;

    public class KeyExTests {

        [Fact]
        public void ToKeyAndBackRsaTests() {

            using (var rsa1 = RSA.Create()) {
                var key1 = rsa1.ToKey();
                var key2 = key1.ToJsonWebKey().ToKey();
                var rsa2 = key2.ToRSA();

                Assert.True(key1.Parameters.SameAs(key2.Parameters));
                Assert.Equal(rsa1.KeySize, rsa2.KeySize);
            }
        }

        [Fact]
        public void ToKeyAndBackEccTests() {

            using (var ecc1 = ECDsa.Create()) {
                var key1 = ecc1.ToKey();
                var key2 = key1.ToJsonWebKey().ToKey();
                var ecc2 = key2.ToECDsa();

                Assert.True(key1.Parameters.SameAs(key2.Parameters));
                Assert.Equal(ecc1.KeySize, ecc2.KeySize);
            }
        }

        [Fact]
        public void ToKeyAndBackAesTests() {

            using (var aes1 = Aes.Create()) {
                var key1 = aes1.ToKey();
                var key2 = key1.ToJsonWebKey().ToKey();
                var aes2 = key2.ToAes();

                Assert.True(key1.Parameters.SameAs(key2.Parameters));
                Assert.Equal(aes1.Key, aes2.Key);
            }
        }

        [Fact]
        public void ArgumentThrowTests1() {

            using (var aes1 = Aes.Create()) {
                var key1 = aes1.ToKey();
                var keybad1 = key1.Clone();
                keybad1.Type = (KeyType)555;
                var keybad2 = key1.ToJsonWebKey();
                keybad2.Kty = "bad";

                Assert.Throws<NotSupportedException>(() => keybad1.ToJsonWebKey());
                Assert.Throws<NotSupportedException>(() => keybad2.ToKey());
            }
        }

        [Fact]
        public void ArgumentThrowTests2() {

            Assert.Throws<NotSupportedException>(() => CurveType.Brainpool_P160r1.ToJsonWebKeyCurveName());
            Assert.Throws<NotSupportedException>(() => CurveType.Brainpool_P224r1.ToJsonWebKeyCurveName());
            Assert.Throws<ArgumentOutOfRangeException>(() => ((CurveType)2342).ToJsonWebKeyCurveName());
            Assert.Throws<ArgumentException>(() => KeyEx.FromJsonWebKeyCurveName("53234"));
            Assert.Throws<ArgumentNullException>(() => KeyEx.FromJsonWebKeyCurveName(null));
        }
    }
}
