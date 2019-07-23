// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class X509AuthorityInformationAccessExtensionTests {

        [Fact]
        public void TestParseX509AuthorityInformationAccessExtension1() {

            // Setup
            var issuers = new List<string> {
                "http://test.org/test",
                "https://test/"
            };
            var ocspResponder = "http://responder";

            // Act
            var extension1 = new X509AuthorityInformationAccessExtension(
                issuers, ocspResponder, false);
            var sm1 = extension1.Format(true);
            var s1 = extension1.Format(false);

            var buffer = extension1.RawData;

            var extension2 = new X509AuthorityInformationAccessExtension(buffer);
            var sm2 = extension2.Format(true);
            var s2 = extension2.Format(false);

            // Assert

            Assert.Equal(extension1.OcspResponder, extension2.OcspResponder);
            Assert.Equal(ocspResponder, extension2.OcspResponder);
            Assert.Equal(extension1.IssuerUrls, extension2.IssuerUrls);
            Assert.Equal(issuers, extension2.IssuerUrls);
            Assert.Equal(Oids.AuthorityInformationAccess,
                extension1.Oid.Value);
            Assert.Equal(Oids.AuthorityInformationAccess,
                extension2.Oid.Value);
            Assert.Equal(sm1, sm2);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestParseX509AuthorityInformationAccessExtension2() {

            // Setup
            var issuer = "http://responder";
            var ocspResponder = "http://responder";

            // Act
            var extension1 = new X509AuthorityInformationAccessExtension(
                issuer, ocspResponder, false);
            var sm1 = extension1.Format(true);
            var s1 = extension1.Format(false);

            var buffer = extension1.RawData;

            var extension2 = new X509AuthorityInformationAccessExtension(buffer);
            var sm2 = extension2.Format(true);
            var s2 = extension2.Format(false);

            // Assert

            Assert.Equal(extension1.OcspResponder, extension2.OcspResponder);
            Assert.Equal(ocspResponder, extension2.OcspResponder);
            Assert.Equal(extension1.IssuerUrls, extension2.IssuerUrls);
            Assert.Single(extension2.IssuerUrls);
            Assert.Equal(Oids.AuthorityInformationAccess,
                extension1.Oid.Value);
            Assert.Equal(Oids.AuthorityInformationAccess,
                extension2.Oid.Value);
            Assert.Equal(sm1, sm2);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestParseX509AuthorityInformationAccessExtension3() {

            // Setup
            var issuer = "http://responder";

            // Act
            var extension1 = new X509AuthorityInformationAccessExtension(
                issuer);

            var buffer = extension1.RawData;

            var extension2 = new X509AuthorityInformationAccessExtension(buffer);
            var extension3 = new X509AuthorityInformationAccessExtension(extension2, false);

            // Assert

            Assert.Equal(extension1.OcspResponder, extension3.OcspResponder);
            Assert.Null(extension3.OcspResponder);
            Assert.Equal(extension1.IssuerUrls, extension2.IssuerUrls);
            Assert.Single(extension3.IssuerUrls);
            Assert.Equal(issuer, extension3.IssuerUrls.Single());
            Assert.Equal(Oids.AuthorityInformationAccess,
                extension3.Oid.Value);
        }
    }
}
