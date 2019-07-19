// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Xunit;

    public class X509AuthorityKeyIdentifierExtensionTests {

        [Fact]
        public void TestParseX509AuthorityKeyIdentifierExtension1() {

            // Setup
            var authority = "CN=TestAuthority";
            var keyId = "32362340932423";

            var serialNumber = new SerialNumber(4);

            // Act
            var extension1 = new X509AuthorityKeyIdentifierExtension(
                authority, serialNumber, keyId, false);
            var sm1 = extension1.Format(true);
            var s1 = extension1.Format(false);

            var buffer = extension1.RawData;

            var extension2 = new X509AuthorityKeyIdentifierExtension(buffer);
            var sm2 = extension2.Format(true);
            var s2 = extension2.Format(false);

            // Assert

            Assert.Equal(extension1.AuthorityNames, extension2.AuthorityNames);
            Assert.Single(extension2.AuthorityNames);
            Assert.Equal(authority, extension1.AuthorityNames.Single());
            Assert.Equal(extension1.SerialNumber, extension2.SerialNumber);
            Assert.Equal(serialNumber, extension2.SerialNumber);
            Assert.Equal(extension1.KeyId, extension2.KeyId);
            Assert.Equal(keyId, extension2.KeyId);
            Assert.Equal(Oids.AuthorityKeyIdentifier,
                extension1.Oid.Value);
            Assert.Equal(Oids.AuthorityKeyIdentifier,
                extension2.Oid.Value);
            Assert.Equal(sm1, sm2);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestParseX509AuthorityKeyIdentifierExtension2() {

            // Setup
            var authorities = new List<string> {
                "CN=TestAuthority", "CN=test", "CN=ttttttt"
            };
            var keyId = "32362340932423";
            var serialNumber = new SerialNumber();

            // Act
            var extension1 = new X509AuthorityKeyIdentifierExtension(
                authorities, serialNumber, keyId, false);
            var sm1 = extension1.Format(true);
            var s1 = extension1.Format(false);

            var buffer = extension1.RawData;

            var extension2 = new X509AuthorityKeyIdentifierExtension(buffer, true);
            var sm2 = extension2.Format(true);
            var s2 = extension2.Format(false);

            // Assert

            Assert.Equal(extension1.AuthorityNames, extension2.AuthorityNames);
            Assert.Equal(authorities, extension1.AuthorityNames);
            Assert.Equal(extension1.SerialNumber, extension2.SerialNumber);
            Assert.Equal(serialNumber, extension2.SerialNumber);
            Assert.Equal(extension1.KeyId, extension2.KeyId);
            Assert.Equal(keyId, extension2.KeyId);
            Assert.Equal(Oids.AuthorityKeyIdentifier,
                extension1.Oid.Value);
            Assert.Equal(Oids.AuthorityKeyIdentifier,
                extension2.Oid.Value);
            Assert.True(extension2.Critical);
            Assert.Equal(sm1, sm2);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestParseX509AuthorityKeyIdentifierExtension3() {

            // Setup
            var authority = "CN=TestAuthority";
            var keyId = "32362340932423";
            var serialNumber = new SerialNumber(40);

            // Act
            var extension1 = new X509AuthorityKeyIdentifierExtension(
                authority, serialNumber, keyId, false);

            var buffer = extension1.RawData;

            var extension2 = new X509AuthorityKeyIdentifierExtension(buffer);
            var extension3 = new X509AuthorityKeyIdentifierExtension(extension2, true);

            // Assert

            Assert.Equal(extension1.AuthorityNames, extension3.AuthorityNames);
            Assert.Single(extension3.AuthorityNames);
            Assert.Equal(authority, extension1.AuthorityNames.Single());
            Assert.Equal(extension1.SerialNumber, extension3.SerialNumber);
            Assert.Equal(serialNumber, extension3.SerialNumber);
            Assert.Equal(extension1.KeyId, extension3.KeyId);
            Assert.Equal(keyId, extension3.KeyId);
            Assert.Equal(Oids.AuthorityKeyIdentifier,
                extension1.Oid.Value);
            Assert.Equal(Oids.AuthorityKeyIdentifier,
                extension3.Oid.Value);
            Assert.True(extension3.Critical);
        }

        [Fact]
        public void TestCreate509AuthorityKeyIdentifierExtension1() {
            var authority = "CN=TestAuthority";
            var keyId = "32362340932423";
            var serial = new byte[20];
            var rand = new Random();
            rand.NextBytes(serial);
            serial[0] = 0x80;
            serial[1] = 0x80;
            var serialNumber = new SerialNumber(serial);

            using (var rsa = RSA.Create()) {
                var request = rsa.ToKey().CreateCertificateRequest(
                    new X500DistinguishedName("CN=test"),
                    SignatureType.PS256,
                    new X509AuthorityKeyIdentifierExtension(authority, serialNumber, keyId)
                        .YieldReturn());
                var cert = request.Create(new X500DistinguishedName("CN=test"),
                    X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                    DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(1), serialNumber.Value);

                var aki = cert.GetAuthorityKeyIdentifierExtension();

                Assert.Equal(serialNumber.ToString(), cert.SerialNumber);
                Assert.Equal(serialNumber, aki.SerialNumber);
                Assert.Equal(aki.SerialNumber.ToString(), cert.SerialNumber);
            }
        }

        [Fact]
        public void TestCreate509AuthorityKeyIdentifierExtension2() {
            var authority = "CN=TestAuthority";
            var keyId = "32362340932423";

            var serial = new byte[20];
            var rand = new Random();
            rand.NextBytes(serial);
            serial[0] = 0;
            serial[1] = 0;
            serial[2] = 0;
            var serialNumber = new SerialNumber(serial);

            using (var rsa = RSA.Create()) {
                var request = rsa.ToKey().CreateCertificateRequest(
                    new X500DistinguishedName("CN=test"),
                    SignatureType.PS256,
                    new X509AuthorityKeyIdentifierExtension(authority, serialNumber, keyId)
                        .YieldReturn());
                var cert = request.Create(new X500DistinguishedName("CN=test"),
                    X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                    DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(1), serialNumber.Value);

                var aki = cert.GetAuthorityKeyIdentifierExtension();

                Assert.Equal(serialNumber, aki.SerialNumber);
                Assert.Equal(serialNumber.ToString(), cert.SerialNumber);
                Assert.Equal(aki.SerialNumber, SerialNumber.Parse(cert.SerialNumber));
            }
        }
    }
}