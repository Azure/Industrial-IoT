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

    public class ApplicationRegistryTests
    {
        [Fact]
        public async Task GetApplicationThatDoesNotExistAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                async () => await service.GetApplicationAsync("test", false, default));
        }

        [Fact]
        public async Task GetApplicationThatExistsAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);
            var first = apps[0];

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var result = await service.GetApplicationAsync(
                ApplicationInfoModelEx.CreateApplicationId(site,
                first.ApplicationUri, first.ApplicationType), false, default);

            // Assert
            Assert.True(result.Application.IsSameAs(apps[0]));
            Assert.Empty(result.Endpoints);
        }

        [Fact]
        public async Task ListAllApplicationsAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.ListApplicationsAsync(null, null, default);

            // Assert
            Assert.True(apps.IsSameAs(records.Items));
        }

        [Fact]
        public async Task ListAllApplicationsUsingQueryAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(null, null, default);

            // Assert
            Assert.True(apps.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QueryApplicationsByClientAndServerApplicationTypeAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationType = ApplicationType.ClientAndServer
            }, null, default);

            // Assert
            Assert.Equal(apps.Count(x =>
                x.ApplicationType == ApplicationType.ClientAndServer), records.Items.Count);
        }

        [Fact]
        public async Task QueryApplicationsByServerApplicationTypeAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationType = ApplicationType.Server
            }, null, default);

            // Assert
            Assert.Equal(apps.Count(x => x.ApplicationType != ApplicationType.Client), records.Items.Count);
        }

        [Fact]
        public async Task QueryApplicationsByDiscoveryServerApplicationTypeAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationType = ApplicationType.DiscoveryServer
            }, null, default);

            // Assert
            Assert.Equal(apps.Count(x => x.ApplicationType == ApplicationType.DiscoveryServer), records.Items.Count);
        }

        [Fact]
        public async Task QueryApplicationsBySiteIdAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                SiteOrGatewayId = site
            }, null, default);

            // Assert
            Assert.True(apps.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QueryApplicationsBySupervisorIdAsync()
        {
            CreateAppFixtures(out var site, out var gateway, out var super, out var apps, out var devices, true);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                SiteOrGatewayId = gateway
            }, null, default);

            // Assert
            Assert.True(apps.IsSameAs(records.Items));
        }

        [Fact]
        public async Task QueryApplicationsByClientApplicationTypeAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationType = ApplicationType.Client
            }, null, default);

            // Assert
            Assert.Equal(apps.Count(x =>
                x.ApplicationType != ApplicationType.Server &&
                x.ApplicationType != ApplicationType.DiscoveryServer), records.Items.Count);
        }

        [Fact]
        public async Task QueryApplicationsByApplicationNameSameCaseAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationName = apps[0].ApplicationName
            }, null, default);

            // Assert
            Assert.True(records.Items.Count >= 1);
            Assert.True(records.Items[0].IsSameAs(apps[0]));
        }

        [Fact]
        public async Task QueryApplicationsByApplicationNameDifferentCaseAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationName = apps[0].ApplicationName.ToUpperInvariant()
            }, null, default);

            // Assert
            Assert.Empty(records.Items);
        }

        [Fact]
        public async Task QueryApplicationsByApplicationUriDifferentCaseAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            var records = await service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel
            {
                ApplicationUri = apps[0].ApplicationUri.ToUpperInvariant()
            }, null, default);

            // Assert
            Assert.True(records.Items.Count >= 1);
            Assert.True(records.Items[0].IsSameAs(apps[0]));
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [Fact]
        public async Task RegisterApplicationAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var hub = new IoTHubMock(_serializer);
            using var mock = AutoMock.GetLoose(builder =>
            {
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            foreach (var app in apps)
            {
                var record = await service.RegisterApplicationAsync(
                    app.ToRegistrationRequest(), default);
            }

            // Assert
            Assert.Equal(apps.Count, hub.Devices.Count());
            var records = await service.ListApplicationsAsync(null, null, default);

            // Assert
            Assert.True(apps.IsSameAs(records.Items));
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [Fact]
        public async Task UnregisterApplicationsAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var hub = IoTHubMock.Create(devices, _serializer);
            using var mock = AutoMock.GetLoose(builder =>
            {
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            // Run
            foreach (var app in apps)
            {
                await service.UnregisterApplicationAsync(app.ApplicationId, null, default);
            }

            // Assert
            Assert.Empty(hub.Devices);
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [Fact]
        public async Task BadArgShouldThrowExceptionsAsync()
        {
            using var mock = AutoMock.GetLoose(builder =>
            {
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterType<IoTHubMock>().As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.RegisterApplicationAsync(null, default));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetApplicationAsync(null, false, default));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetApplicationAsync("", false, default));
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                () => service.GetApplicationAsync("abc", false, default));
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                () => service.GetApplicationAsync(Guid.NewGuid().ToString(), false, default));
        }

        [Fact]
        public async Task DisableEnableApplicationAsync()
        {
            CreateAppFixtures(out var site, out _, out var super, out var apps, out var devices);

            using var mock = AutoMock.GetLoose(builder =>
            {
                var hub = IoTHubMock.Create(devices, _serializer);
                // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(hub).As<IIoTHubTwinServices>();
            });
            var service = mock.Create<ApplicationRegistry>();

            var app = apps[0];
            await service.DisableApplicationAsync(app.ApplicationId, null, default);
            var registration = await service.GetApplicationAsync(app.ApplicationId, false, default);
            Assert.NotNull(registration.Application.NotSeenSince);
            await service.EnableApplicationAsync(app.ApplicationId, null, default);
            registration = await service.GetApplicationAsync(app.ApplicationId, false, default);
            Assert.Null(registration.Application.NotSeenSince);
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="gateway"></param>
        /// <param name="super"></param>
        /// <param name="apps"></param>
        /// <param name="devices"></param>
        /// <param name="noSite"></param>
        private void CreateAppFixtures(out string site, out string gateway, out string super,
            out List<ApplicationInfoModel> apps, out List<DeviceTwinModel> devices,
            bool noSite = false)
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new TypeRelay(typeof(VariantValue), typeof(VariantValue)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlySet<>), typeof(HashSet<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyList<>), typeof(List<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyCollection<>), typeof(List<>)));
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            var sitex = site = noSite ? null : fixture.Create<string>();
            gateway = fixture.Create<string>();
            var superx = super = HubResource.Format(null, gateway, fixture.Create<string>());
            apps = fixture
                .Build<ApplicationInfoModel>()
                .Without(x => x.NotSeenSince)
                .With(x => x.SiteId, sitex)
                .With(x => x.DiscovererId, superx)
                .CreateMany(10)
                .ToList();
            apps.ForEach(x => x.ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                 sitex, x.ApplicationUri, x.ApplicationType));
            devices = apps
                .Select(a => a.ToApplicationRegistration())
                .Select(a => a.ToDeviceTwin(_serializer, TimeProvider.System))
                .ToList();
        }

        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
    }
}
