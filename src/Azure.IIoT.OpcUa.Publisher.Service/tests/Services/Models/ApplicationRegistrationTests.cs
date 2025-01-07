// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Tests.Services.Models
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using AutoFixture;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class ApplicationRegistrationTests
    {
        [Fact]
        public void TestEqualIsEqual()
        {
            var r1 = CreateRegistration();
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqual()
        {
            var r1 = CreateRegistration();
            var r2 = CreateRegistration();

            Assert.NotEqual(r1, r2);
            Assert.False(r1.Equals(null));
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithServiceModelConversion()
        {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = m.ToApplicationRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithServiceModelConversionWhenDisabled()
        {
            var r1 = CreateRegistration();
            var m = r1.ToServiceModel();
            var r2 = m.ToApplicationRegistration(true);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversion()
        {
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
        public void TestEqualIsNotEqualWithDeviceModel()
        {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin(_serializer, TimeProvider.System);
            m.Tags = new Dictionary<string, VariantValue>(m.Tags)
            {
                ["DiscoveryProfileUri"] = null
            };
            var r2 = m.ToEntityRegistration();

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModel()
        {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin(_serializer, TimeProvider.System);
            var r2 = m.ToEntityRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDeviceModelWhenDisabled()
        {
            var r1 = CreateRegistration();
            var r2 = r1.ToServiceModel().ToApplicationRegistration(true);
            var m1 = r1.Patch(r2, _serializer, TimeProvider.System);
            var r3 = r2.ToServiceModel().ToApplicationRegistration(false);
            var m2 = r2.Patch(r3, _serializer, TimeProvider.System);

            Assert.True((bool?)m1.Tags[nameof(EntityRegistration.IsDisabled)] ?? false);
            Assert.NotNull((DateTime?)m1.Tags[nameof(EntityRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(EntityRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(EntityRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static ApplicationRegistration CreateRegistration()
        {
            var fix = new Fixture();
            return fix.Build<ApplicationRegistration>()
                .With(x => x.Capabilities, fix.CreateMany<string>().ToHashSet()
                    .EncodeAsDictionary(true))
                .With(x => x.DiscoveryUrls, fix.CreateMany<string>().ToList()
                    .EncodeAsDictionary())
                .With(x => x.HostAddresses, fix.CreateMany<string>().ToList()
                    .EncodeAsDictionary())
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
        }

        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
    }
}
