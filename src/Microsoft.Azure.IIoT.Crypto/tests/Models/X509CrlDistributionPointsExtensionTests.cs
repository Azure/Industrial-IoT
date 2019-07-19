// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using Xunit;


    public class X509CrlDistributionPointsExtensionTests {

        [Fact]
        public void TestParseX509CrlDistributionPointsExtension1() {

            // Setup
            var distributionPoint = "http://responder";

            // Act
            var extension1 = new X509CrlDistributionPointsExtension(
                distributionPoint, false);
            var sm1 = extension1.Format(true);
            var s1 = extension1.Format(false);

            var buffer = extension1.RawData;

            var extension2 = new X509CrlDistributionPointsExtension(buffer);
            var sm2 = extension2.Format(true);
            var s2 = extension2.Format(false);

            // Assert

            Assert.Equal(extension1.DistributionPoint, extension2.DistributionPoint);
            Assert.Equal(distributionPoint, extension2.DistributionPoint);
            Assert.Equal(Oids.CrlDistributionPoints,
                extension1.Oid.Value);
            Assert.Equal(Oids.CrlDistributionPoints,
                extension2.Oid.Value);
            Assert.Equal(sm1, sm2);
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void TestParseX509CrlDistributionPointsExtension2() {

            // Setup
            var distributionPoint = "http://responder";

            // Act
            var extension1 = new X509CrlDistributionPointsExtension(
                distributionPoint);

            var buffer = extension1.RawData;

            var extension2 = new X509CrlDistributionPointsExtension(buffer, true);
            var extension3 = new X509CrlDistributionPointsExtension(extension2, false);

            // Assert

            Assert.Equal(extension1.DistributionPoint, extension3.DistributionPoint);
            Assert.Equal(distributionPoint, extension3.DistributionPoint);
            Assert.Equal(Oids.CrlDistributionPoints,
                extension3.Oid.Value);
            Assert.True(extension2.Critical);
            Assert.False(extension3.Critical);
        }
    }
}
