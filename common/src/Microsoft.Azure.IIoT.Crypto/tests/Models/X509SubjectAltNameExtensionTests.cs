// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;


    public class X509SubjectAltNameExtensionTests {

        [Fact]
        public void TestParseX509SubjectAltNameExtension1() {

            // Setup
            var uris = new List<string> {
                "http://test.org/test",
                "https://test/"
            };
            var addresses = new List<string> {
                "1.2.3.4",
                "1.1.1.1"
            };

            // Act
            var extension1 = new X509SubjectAltNameExtension(
                uris, addresses, false);
            var sm1 = extension1.Format(true);
            var s1 = extension1.Format(false);

            var buffer = extension1.RawData;

            var extension2 = new X509SubjectAltNameExtension(buffer);
            var sm2 = extension2.Format(true);
            var s2 = extension2.Format(false);

            // Assert

            Assert.Equal(extension1.IPAddresses, extension2.IPAddresses);
            Assert.Equal(addresses, extension2.IPAddresses);
            Assert.Equal(extension1.Uris, extension2.Uris);
            Assert.Equal(uris, extension2.Uris);
            Assert.Equal(Oids.SubjectAltName2,
                extension1.Oid.Value);
            Assert.Equal(Oids.SubjectAltName2,
                extension2.Oid.Value);
            Assert.False(extension2.Critical);
            Assert.Equal(sm1, sm2);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestParseX509SubjectAltNameExtension2() {

            // Setup
            var uris = "http://test.org/test";
            var addresses = new List<string> {
                "1.2.3.4",
                "1.1.1.1"
            };

            // Act
            var extension1 = new X509SubjectAltNameExtension(
                uris, addresses, false);

            var buffer = extension1.RawData;

            var extension2 = new X509SubjectAltNameExtension(buffer, true);

            // Assert

            Assert.Equal(extension1.IPAddresses, extension2.IPAddresses);
            Assert.Equal(addresses, extension2.IPAddresses);
            Assert.Equal(extension1.Uris, extension2.Uris);
            Assert.Single(extension1.Uris);
            Assert.Equal(uris, extension2.Uris.Single());
            Assert.Equal(Oids.SubjectAltName2,
                extension1.Oid.Value);
            Assert.Equal(Oids.SubjectAltName2,
                extension2.Oid.Value);
            Assert.True(extension2.Critical);
        }

        [Fact]
        public void TestParseX509SubjectAltNameExtension3() {

            // Setup
            var uris = new List<string> {
                "http://test.org/test",
                "https://test/"
            };
            var addresses = new List<string> {
                "1.2.3.4",
                "1.1.1.1"
            };

            // Act
            var extension1 = new X509SubjectAltNameExtension(
                uris, addresses, false);

            var buffer = extension1.RawData;

            var extension = new X509SubjectAltNameExtension(buffer);
            var extension2 = new X509SubjectAltNameExtension(extension, true);

            // Assert


            Assert.Equal(extension1.IPAddresses, extension2.IPAddresses);
            Assert.Equal(addresses, extension2.IPAddresses);
            Assert.Equal(extension1.Uris, extension2.Uris);
            Assert.Equal(uris, extension2.Uris);
            Assert.Equal(Oids.SubjectAltName2,
                extension1.Oid.Value);
            Assert.Equal(Oids.SubjectAltName2,
                extension2.Oid.Value);
            Assert.False(extension1.Critical);
            Assert.True(extension2.Critical);
        }
    }
}
