// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using AutoFixture;
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Linq;
    using Xunit;

    public class ApplicationRegistrationTests {

        [Fact]
        public void TestEqualIsEqual() {
            var r1 = CreateRegistration();
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqual() {
            var r1 = CreateRegistration();
            var r2 = CreateRegistration();

            Assert.NotEqual(r1, r2);
            Assert.False(r1.Equals(null));
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithServiceModelConversion() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = m.ToApplicationRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithServiceModelConversionWhenDisabled() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = m.ToApplicationRegistration(true);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversion() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            m.DiscoveryProfileUri = "";
            var r2 = m.ToApplicationRegistration();

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin();
            m.Tags["DiscoveryProfileUri"] = null;
            var r2 = m.ToEntityRegistration();

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin();
            var r2 = m.ToEntityRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModelWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var r2 = r1.ToServiceModel().ToApplicationRegistration(true);
            var m1 = r1.Patch(r2);
            var r3 = r2.ToServiceModel().ToApplicationRegistration(false);
            var m2 = r2.Patch(r3);

            Assert.True((bool?)m1.Tags[nameof(EntityRegistration.IsDisabled)] ?? false);
            Assert.NotNull((DateTime?)m1.Tags[nameof(EntityRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(EntityRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(EntityRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static ApplicationRegistration CreateRegistration() {
            var fix = new Fixture();
            var cert = fix.CreateMany<byte>(1000).ToArray();
            var r1 = fix.Build<ApplicationRegistration>()
                .With(x => x.Certificate, cert.EncodeAsDictionary())
                .With(x => x.Thumbprint, cert.ToSha1Hash())
                .With(x => x.Capabilities, fix.CreateMany<string>().ToHashSet()
                    .EncodeAsDictionary(true))
                .With(x => x.DiscoveryUrls, fix.CreateMany<string>().ToList()
                    .EncodeAsDictionary())
                .With(x => x.HostAddresses, fix.CreateMany<string>().ToList()
                    .EncodeAsDictionary())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
            return r1;
        }
    }
}
