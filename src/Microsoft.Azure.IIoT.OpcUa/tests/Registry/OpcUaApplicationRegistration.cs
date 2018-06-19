// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Microsoft.Azure.IIoT.Hub;
    using Moq;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class OpcUaApplicationRegistrationTests {

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
            var r2 = OpcUaApplicationRegistration.FromServiceModel(m);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithServiceModelConversionWhenDisabled() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = OpcUaApplicationRegistration.FromServiceModel(m, true);

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
            var r2 = OpcUaApplicationRegistration.FromServiceModel(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = OpcUaApplicationRegistration.Patch(null, r1);
            m.Tags["DiscoveryProfileUri"] = null;
            var r2 = OpcUaTwinRegistration.ToRegistration(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = OpcUaApplicationRegistration.Patch(null, r1);
            var r2 = OpcUaTwinRegistration.ToRegistration(m);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModelWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var r2 = OpcUaApplicationRegistration.FromServiceModel(
                r1.ToServiceModel(), true);
            var m1 = OpcUaApplicationRegistration.Patch(r1, r2);
            var r3 = OpcUaApplicationRegistration.FromServiceModel(
                r2.ToServiceModel(), false);
            var m2 = OpcUaApplicationRegistration.Patch(r2, r3);

            Assert.True((bool?)m1.Tags[nameof(OpcUaTwinRegistration.IsDisabled)] ?? false);
            Assert.NotNull((DateTime?)m1.Tags[nameof(OpcUaTwinRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(OpcUaTwinRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(OpcUaTwinRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static OpcUaApplicationRegistration CreateRegistration() {
            var fix = new Fixture();
            var cert = fix.CreateMany<byte>(1000).ToArray();
            var r1 = fix.Build<OpcUaApplicationRegistration>()
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
