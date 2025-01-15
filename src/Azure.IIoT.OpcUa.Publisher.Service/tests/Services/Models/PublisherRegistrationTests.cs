// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Tests.Services.Models
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using AutoFixture;
    using System;
    using System.Linq;
    using Xunit;

    public class PublisherRegistrationTests
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
            var m = r1.ToPublisherModel();
            var r2 = m.ToPublisherRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithServiceModelConversionWhenDisabled()
        {
            var r1 = CreateRegistration();
            var m = r1.ToPublisherModel();
            var r2 = m.ToPublisherRegistration(true);

            Assert.NotEqual(r1, r2);
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModel()
        {
            var r1 = CreateRegistration();
            var m = r1.ToDeviceTwin(TimeProvider.System);
            var r2 = m.ToEntityRegistration();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqualWithDeviceModelWhenDisabled()
        {
            var r1 = CreateRegistration();
            var r2 = r1.ToPublisherModel().ToPublisherRegistration(true);
            var m1 = r1.Patch(r2, TimeProvider.System);
            var r3 = r2.ToPublisherModel().ToPublisherRegistration(false);
            var m2 = r2.Patch(r3, TimeProvider.System);

            Assert.True((bool)m1.Tags[nameof(EntityRegistration.IsDisabled)]);
            Assert.NotNull((DateTime?)m1.Tags[nameof(EntityRegistration.NotSeenSince)]);
            Assert.Null((bool?)m2.Tags[nameof(EntityRegistration.IsDisabled)]);
            Assert.Null((DateTime?)m2.Tags[nameof(EntityRegistration.NotSeenSince)]);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static PublisherRegistration CreateRegistration()
        {
            var fix = new Fixture();
            var cert = fix.CreateMany<byte>(1000).ToArray();
            return fix.Build<PublisherRegistration>()
                .FromFactory(() => new PublisherRegistration(
                    fix.Create<string>(), fix.Create<string>()))
                .Without(x => x.IsDisabled)
                .Without(x => x.NotSeenSince)
                .Create();
        }
    }
}
