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

    public class SupervisorRegistryTests
    {
        [Fact]
        public async Task GetSupervisorWithmalformedIdAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await service.GetSupervisorAsync("test", false, default));
        }
        [Fact]
        public async Task GetSupervisorThatDoesNotExistAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                async () => await service.GetSupervisorAsync(HubResource.Format(null, "test", "test"), false, default));
        }

        [Fact]
        public async Task GetSupervisorThatExistsAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            var result = await service.GetSupervisorAsync(supervisors[0].Id, false, default);

            // Assert
            Assert.True(result.IsSameAs(supervisors[0]));
        }

        [Fact]
        public async Task ListAllSupervisorsAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            var records = await service.ListSupervisorsAsync(null, false, null, default);

            // Assert
            Assert.True(supervisors.IsSameAs(records.Items));
        }

        [Fact]
        public async Task ListAllSupervisorsUsingQueryAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            var records = await service.QuerySupervisorsAsync(null, false, null, default);

            // Assert
            Assert.True(supervisors.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QuerySupervisorsBySiteIdAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            var records = await service.QuerySupervisorsAsync(new SupervisorQueryModel
            {
                SiteId = site
            }, false, null, default);

            // Assert
            Assert.True(supervisors.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QuerySupervisorsByNoneExistantSiteIdAsync()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules, true);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(modules, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<SupervisorRegistry>();

            // Run
            var records = await service.QuerySupervisorsAsync(new SupervisorQueryModel
            {
                SiteId = "test"
            }, false, null, default);

            // Assert
            Assert.Empty(records.Items);
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="supervisors"></param>
        /// <param name="modules"></param>
        /// <param name="noSite"></param>
        private static void CreateSupervisorFixtures(out string site,
            out List<SupervisorModel> supervisors, out List<DeviceTwinModel> modules,
            bool noSite = false)
        {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(VariantValue), typeof(VariantValue)));
            fix.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fix.Behaviors.Remove(b));
            fix.Behaviors.Add(new OmitOnRecursionBehavior());
            var sitex = site = noSite ? null : fix.Create<string>();
            supervisors = fix
                .Build<SupervisorModel>()
                .With(x => x.SiteId, sitex)
                .Without(x => x.Id)
                .Do(x => x.Id = HubResource.Format(null, fix.Create<string>(), fix.Create<string>()))
                .CreateMany(10)
                .ToList();

            modules = supervisors
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
