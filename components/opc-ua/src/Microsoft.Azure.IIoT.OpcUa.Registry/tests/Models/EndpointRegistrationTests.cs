// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using AutoFixture;
    using AutoFixture.Kernel;
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
            var r2 = m.ToEndpointRegistration(_serializer);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversionWhenDisabled() {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = m.ToEndpointRegistration(_serializer, true);

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
            var r2 = m.ToEndpointRegistration(_serializer);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin(_serializer);
            m.Properties.Desired["SecurityPolicy"] = "babab";
            var r2 = m.ToEntityRegistration();

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }


        [Fact]
        public void TestEqualIsEqualWithDeviceModel() {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin(_serializer);
            var r2 = m.ToEntityRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModelWhenDisabled() {
            var fix = new Fixture();

            var r1 = CreateRegistration();
            var r2 = r1.ToServiceModel().ToEndpointRegistration(_serializer, true);
            var m1 = r1.Patch(r2, _serializer);
            var r3 = r2.ToServiceModel().ToEndpointRegistration(_serializer, false);
            var m2 = r2.Patch(r3, _serializer);

            Assert.True((bool?)m1.Tags[nameof(EntityRegistration.IsDisabled)] ?? false);
            Assert.NotNull((DateTime?)m1.Tags[nameof(EntityRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(EntityRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(EntityRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Helper to create registration
        /// </summary>
        /// <returns></returns>
        private EndpointRegistration CreateRegistration() {
            var fix = new Fixture();

            fix.Customizations.Add(new TypeRelay(typeof(VariantValue), typeof(VariantValue)));
            fix.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fix.Behaviors.Remove(b));
            fix.Behaviors.Add(new OmitOnRecursionBehavior());

            var cert = fix.CreateMany<byte>(1000).ToArray();
            var urls = fix.CreateMany<Uri>(4).ToList();
            var r1 = fix.Build<EndpointRegistration>()
                .With(x => x.Thumbprint, cert.ToThumbprint())
                .With(x => x.AlternativeUrls,
                    fix.CreateMany<Uri>(4)
                        .Select(u => u.ToString())
                        .ToList().EncodeAsDictionary())
                .With(x => x.AuthenticationMethods,
                    fix.CreateMany<AuthenticationMethodModel>()
                        .Select(_serializer.FromObject).ToList().EncodeAsDictionary())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
            return r1;
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
