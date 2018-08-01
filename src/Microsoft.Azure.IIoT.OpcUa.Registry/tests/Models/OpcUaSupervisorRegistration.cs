// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using AutoFixture;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Microsoft.Azure.IIoT.OpcUa.Models;

    public class OpcUaSupervisorRegistrationTests {

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
            var r2 = OpcUaSupervisorRegistration.FromServiceModel(m);

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
            var r2 = OpcUaSupervisorRegistration.FromServiceModel(m, true);

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
            var r2 = OpcUaSupervisorRegistration.FromServiceModel(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = OpcUaSupervisorRegistration.Patch(null, r1);
            m.Properties.Desired["AddressRangesToScan"] = null;
            var r2 = OpcUaTwinRegistration.ToRegistration(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = OpcUaSupervisorRegistration.Patch(null, r1);
            var r2 = OpcUaTwinRegistration.ToRegistration(m);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModelWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var r2 = OpcUaSupervisorRegistration.FromServiceModel(
                r1.ToServiceModel(), true);
            var m1 = OpcUaSupervisorRegistration.Patch(r1, r2);
            var r3 = OpcUaSupervisorRegistration.FromServiceModel(
                r2.ToServiceModel(), false);
            var m2 = OpcUaSupervisorRegistration.Patch(r2, r3);

            Assert.True((bool)m1.Tags[nameof(OpcUaTwinRegistration.IsDisabled)]);
            Assert.NotNull((DateTime?)m1.Tags[nameof(OpcUaTwinRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(OpcUaTwinRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(OpcUaTwinRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static OpcUaSupervisorRegistration CreateRegistration() {
            var fix = new Fixture();
            var cert = fix.CreateMany<byte>(1000).ToArray();
            var r = fix.Build<OpcUaSupervisorRegistration>()
                .FromFactory(() => new OpcUaSupervisorRegistration(
                    fix.Create<string>(), fix.Create<string>()))
                .With(x => x.Certificate, cert.EncodeAsDictionary())
                .With(x => x.DiscoveryCallbacks, fix.CreateMany<CallbackModel>()
                    .ToList().EncodeAsDictionary())
                .With(x => x.Thumbprint, cert.ToSha1Hash())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
            return r;
        }
    }
}
