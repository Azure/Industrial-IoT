// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Registry
{
    using Azure.IIoT.OpcUa.Services.Registry.Models;
    using Azure.IIoT.OpcUa.Models;
    using Autofac;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Furly.Exceptions;

    public class DiscovererRegistryTests
    {
        [Fact]
        public void GetDiscovererThatDoesNotExist()
        {
            CreateDiscovererFixtures(out _, out _, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var t = service.GetDiscovererAsync("test");

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetDiscovererThatExists()
        {
            CreateDiscovererFixtures(out _, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var result = service.GetDiscovererAsync(discoverers[0].Id).Result;

                // Assert
                Assert.True(result.IsSameAs(discoverers[0]));
            }
        }

        [Fact]
        public void ListAllDiscoverers()
        {
            CreateDiscovererFixtures(out _, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.ListDiscoverersAsync(null, null).Result;

                // Assert
                Assert.True(discoverers.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllDiscoverersUsingQuery()
        {
            CreateDiscovererFixtures(out _, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(null, null).Result;

                // Assert
                Assert.True(discoverers.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryDiscoverersByDiscoveryModeReturnsNothingBecauseUnsupported()
        {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(new DiscovererQueryModel
                {
                    Discovery = DiscoveryMode.Network
                }, null).Result;

                // Assert
                Assert.Empty(records.Items);
            }
        }

        [Fact]
        public void QueryDiscoverersBySiteId()
        {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(new DiscovererQueryModel
                {
                    SiteId = site
                }, null).Result;

                // Assert
                Assert.True(discoverers.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryDiscoverersByNoneExistantSiteId()
        {
            CreateDiscovererFixtures(out _, out _, out var modules, true);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(new DiscovererQueryModel
                {
                    SiteId = "test"
                }, null).Result;

                // Assert
                Assert.True(records.Items.Count == 0);
            }
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="discoverers"></param>
        /// <param name="modules"></param>
        /// <param name="noSite"></param>
        private void CreateDiscovererFixtures(out string site,
            out List<DiscovererModel> discoverers, out List<(DeviceTwinModel, DeviceModel)> modules,
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
                .Do(x => x.Id = PublisherModelEx.CreatePublisherId(
                    fix.Create<string>(), fix.Create<string>()))
                .CreateMany(10)
                .ToList();

            modules = discoverers
                .Select(a => a.ToPublisherRegistration())
                .Select(a => a.ToDeviceTwin(_serializer))
                .Select(t =>
                {
                    t.Properties.Reported = new Dictionary<string, VariantValue>
                    {
                        [TwinProperty.Type] = IdentityType.Publisher
                    };
                    return t;
                })
                .Select(t => (t, new DeviceModel { Id = t.Id, ModuleId = t.ModuleId }))
                .ToList();
        }
        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
    }
}
