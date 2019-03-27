// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using Xunit;

    public class EndpointRegistrationTests {

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
            var r2 = EndpointRegistration.FromServiceModel(m);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversionWhenDisabled() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = EndpointRegistration.FromServiceModel(m, true);

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
            var r2 = EndpointRegistration.FromServiceModel(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = EndpointRegistration.Patch(null, r1);
            m.Properties.Desired["Credential"] = "password";
            var r2 = BaseRegistration.ToRegistration(m);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }


        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = EndpointRegistration.Patch(null, r1);
            var r2 = BaseRegistration.ToRegistration(m);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModelWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var r2 = EndpointRegistration.FromServiceModel(
                r1.ToServiceModel(), true);
            var m1 = EndpointRegistration.Patch(r1, r2);
            var r3 = EndpointRegistration.FromServiceModel(
                r2.ToServiceModel(), false);
            var m2 = EndpointRegistration.Patch(r2, r3);

            Assert.True((bool?)m1.Tags[nameof(BaseRegistration.IsDisabled)] ?? false);
            Assert.NotNull((DateTime?)m1.Tags[nameof(BaseRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(BaseRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(BaseRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Helper to create registration
        /// </summary>
        /// <returns></returns>
        private static EndpointRegistration CreateRegistration() {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));

            var cert = fix.CreateMany<byte>(1000).ToArray();
            var urls = fix.CreateMany<Uri>(4).ToList();
            var r1 = fix.Build<EndpointRegistration>()
                .With(x => x.Certificate, cert.EncodeAsDictionary())
                .With(x => x.Thumbprint, cert.ToSha1Hash())
                .With(x => x.ClientCertificate, cert.EncodeAsDictionary())
                .With(x => x.AlternativeUrls,
                    fix.CreateMany<Uri>(4)
                        .Select(u => u.ToString())
                        .ToList().EncodeAsDictionary())
                .With(x => x.AuthenticationMethods,
                    fix.CreateMany<AuthenticationMethodModel>()
                        .Select(JToken.FromObject).ToList().EncodeAsDictionary())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
            return r1;
        }
    }
}
