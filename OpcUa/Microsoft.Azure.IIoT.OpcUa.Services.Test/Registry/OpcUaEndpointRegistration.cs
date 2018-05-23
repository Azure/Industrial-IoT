// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry {
    using Microsoft.Azure.IIoT.Hub;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;

    public class OpcUaEndpointRegistrationTests {

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
            var r2 = OpcUaEndpointRegistration.FromServiceModel(m);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversionWhenDisabled() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = OpcUaEndpointRegistration.FromServiceModel(m, true);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversion() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            m.Registration.Endpoint.SecurityPolicy = "";
            var r2 = OpcUaEndpointRegistration.FromServiceModel(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = OpcUaEndpointRegistration.Patch(null, r1);
            m.Properties.Desired["Token"] = "password";
            var r2 = OpcUaTwinRegistration.ToRegistration(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }


        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = OpcUaEndpointRegistration.Patch(null, r1);
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
            var r2 = OpcUaEndpointRegistration.FromServiceModel(
                r1.ToServiceModel(), true);
            var m1 = OpcUaEndpointRegistration.Patch(r1, r2);
            var r3 = OpcUaEndpointRegistration.FromServiceModel(
                r2.ToServiceModel(), false);
            var m2 = OpcUaEndpointRegistration.Patch(r2, r3);

            Assert.True((bool?)m1.Tags[nameof(OpcUaTwinRegistration.IsDisabled)] ?? false);
            Assert.NotNull((DateTime?)m1.Tags[nameof(OpcUaTwinRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(OpcUaTwinRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(OpcUaTwinRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Helper to create registration
        /// </summary>
        /// <returns></returns>
        private static OpcUaEndpointRegistration CreateRegistration() {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));

            var cert = fix.CreateMany<byte>(1000).ToArray();
            var r1 = fix.Build<OpcUaEndpointRegistration>()
                .With(x => x.Certificate, cert.EncodeAsDictionary())
                .With(x => x.Thumbprint, cert.ToSha1Hash())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
            return r1;
        }
    }
}
