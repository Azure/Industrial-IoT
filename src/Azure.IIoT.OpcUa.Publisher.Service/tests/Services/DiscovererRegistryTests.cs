// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services;
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Mock.Services;
    using Furly.Azure.IoT.Models;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class DiscovererRegistryTests
    {
        [Fact]
        public async Task GetDiscovererWithMalformedIdAsync()
        {
            CreateDiscovererFixtures(out _, out _, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await service.GetDiscovererAsync("test", default));
        }

        [Fact]
        public async Task GetDiscovererThatDoesNotExistAsync()
        {
            CreateDiscovererFixtures(out _, out _, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                async () => await service.GetDiscovererAsync(HubResource.Format(null, "test", "test"), default));
        }

        [Fact]
        public async Task GetDiscovererThatExistsAsync()
        {
            CreateDiscovererFixtures(out _, out var discoverers, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            var result = await service.GetDiscovererAsync(discoverers[0].Id, default);

            // Assert
            Assert.True(result.IsSameAs(discoverers[0]));
        }

        [Fact]
        public async Task ListAllDiscoverersAsync()
        {
            CreateDiscovererFixtures(out _, out var discoverers, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            var records = await service.ListDiscoverersAsync(null, null, default);

            // Assert
            Assert.True(discoverers.IsSameAs(records.Items));
        }

        [Fact]
        public async Task ListAllDiscoverersUsingQueryAsync()
        {
            CreateDiscovererFixtures(out _, out var discoverers, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            var records = await service.QueryDiscoverersAsync(null, null, default);

            // Assert
            Assert.True(discoverers.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QueryDiscoverersByDiscoveryModeReturnsNothingBecauseUnsupportedAsync()
        {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            var records = await service.QueryDiscoverersAsync(new DiscovererQueryModel
            {
                Discovery = DiscoveryMode.Network
            }, null, default);

            // Assert
            Assert.Empty(records.Items);
        }

        [Fact]
        public async Task QueryDiscoverersBySiteIdAsync()
        {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            var records = await service.QueryDiscoverersAsync(new DiscovererQueryModel
            {
                SiteId = site
            }, null, default);

            // Assert
            Assert.True(discoverers.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QueryDiscoverersByNoneExistantSiteIdAsync()
        {
            CreateDiscovererFixtures(out _, out _, out var modules, true);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<DiscovererRegistry>();

            // Run
            var records = await service.QueryDiscoverersAsync(new DiscovererQueryModel
            {
                SiteId = "test"
            }, null, default);

            // Assert
            Assert.Empty(records.Items);
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="discoverers"></param>
        /// <param name="modules"></param>
        /// <param name="noSite"></param>
        private static void CreateDiscovererFixtures(out string site,
            out List<DiscovererModel> discoverers, out List<DeviceTwinModel> modules,
            bool noSite = false)
        {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(VariantValue), typeof(VariantValue)));
            fix.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fix.Behaviors.Remove(b));
            fix.Behaviors.Add(new OmitOnRecursionBehavior());
            var sitex = site = noSite ? null : fix.Create<string>();
            discoverers = fix
                .Build<DiscovererModel>()
                .With(x => x.SiteId, sitex)
                .Without(x => x.Id)
                .Do(x => x.Id = HubResource.Format(null, fix.Create<string>(), fix.Create<string>()))
                .CreateMany(10)
                .ToList();

            modules = discoverers
                .Select(a => a.ToPublisherRegistration())
                .Select(a => a.ToDeviceTwin(TimeProvider.System))
                .Select(t =>
                {
                    t.Reported = new Dictionary<string, VariantValue>
                    {
                        [Constants.TwinPropertyTypeKey] = Constants.EntityTypePublisher
                    };
                    return t;
                })
                .ToList();
        }
        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
    }
}
