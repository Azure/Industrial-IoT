// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using AutoFixture;
    using System;
    using System.Linq;
    using Xunit;

    public class SupervisorRegistrationTests {

        [Fact]
        public void TestEqualIsEqual() {
            var fix = new Fixture();

            var cert = fix.CreateMany<byte>(1000).ToArray();
            var r1 = CreateRegistration();
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqual() {
            var fix = new Fixture();

            var cert = fix.CreateMany<byte>(1000).ToArray();
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
            var r2 = m.ToSupervisorRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversionWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = m.ToSupervisorRegistration(true);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversion() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            m.DiscoveryConfig.AddressRangesToScan = "";
            var r2 = m.ToSupervisorRegistration();

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin();
            m.Properties.Desired["AddressRangesToScan"] = null;
            var r2 = m.ToRegistration();

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin();
            var r2 = m.ToRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModelWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var r2 = r1.ToServiceModel().ToSupervisorRegistration(true);
            var m1 = r1.Patch(r2);
            var r3 = r2.ToServiceModel().ToSupervisorRegistration(false);
            var m2 = r2.Patch(r3);

            Assert.True((bool)m1.Tags[nameof(BaseRegistration.IsDisabled)]);
            Assert.NotNull((DateTime?)m1.Tags[nameof(BaseRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(BaseRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(BaseRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static SupervisorRegistration CreateRegistration() {
            var fix = new Fixture();
            var cert = fix.CreateMany<byte>(1000).ToArray();
            var r = fix.Build<SupervisorRegistration>()
                .FromFactory(() => new SupervisorRegistration(
                    fix.Create<string>(), fix.Create<string>()))
                .With(x => x.Certificate, cert.EncodeAsDictionary())
                .With(x => x.Locales, fix.CreateMany<string>()
                    .ToList().EncodeAsDictionary())
                .With(x => x.SecurityPoliciesFilter, fix.CreateMany<string>()
                    .ToList().EncodeAsDictionary())
                .With(x => x.TrustListsFilter, fix.CreateMany<string>()
                    .ToList().EncodeAsDictionary())
                .With(x => x.DiscoveryUrls, fix.CreateMany<string>()
                    .ToList().EncodeAsDictionary())
                .With(x => x.Thumbprint, cert.ToSha1Hash())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
            return r;
        }
    }
}
