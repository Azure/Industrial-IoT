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
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class SupervisorRegistryTests
    {
        [Fact]
        public void GetSupervisorThatDoesNotExist()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var t = service.GetSupervisorAsync("test", false);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetSupervisorThatExists()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var result = service.GetSupervisorAsync(supervisors[0].Id, false).Result;

                // Assert
                Assert.True(result.IsSameAs(supervisors[0]));
            }
        }

        [Fact]
        public void ListAllSupervisors()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.ListSupervisorsAsync(null, false, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllSupervisorsUsingQuery()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(null, false, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QuerySupervisorsBySiteId()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel
                {
                    SiteId = site
                }, false, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QuerySupervisorsByNoneExistantSiteId()
        {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules, true);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubServices.Create(modules);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            }))
            {
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel
                {
                    SiteId = "test"
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count == 0);
            }
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="supervisors"></param>
        /// <param name="modules"></param>
        /// <param name="noSite"></param>
        private void CreateSupervisorFixtures(out string site,
            out List<SupervisorModel> supervisors, out List<(DeviceTwinModel, DeviceModel)> modules,
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
                .Do(x => x.Id = PublisherModelEx.CreatePublisherId(
                    fix.Create<string>(), fix.Create<string>()))
                .CreateMany(10)
                .ToList();

            modules = supervisors
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
