// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Service;
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
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class DiscoveryProcessorTests
    {
        [Fact]
        public async Task ProcessDiscoveryWithNoResultsAndNoExistingApplicationsAsync()
        {
            var found = new List<DiscoveryEventModel>();
            var fix = new Fixture();

            var gateway = fix.Create<string>();
            var Gateway = new GatewayModel
            {
                Id = gateway
            }.ToGatewayRegistration().ToDeviceTwin(TimeProvider.System);
            var module = fix.Create<string>();
            var discoverer = HubResource.Format(null, gateway, module);
            var Discoverer = new DiscovererModel
            {
                Id = discoverer
            }.ToPublisherRegistration().ToDeviceTwin(TimeProvider.System);
            module = fix.Create<string>();
            var supervisor = HubResource.Format(null, gateway, module);
            var Supervisor = new SupervisorModel
            {
                Id = supervisor
            }.ToPublisherRegistration().ToDeviceTwin(TimeProvider.System);
            module = fix.Create<string>();
            var publisher = HubResource.Format(null, gateway, module);
            var Publisher = new PublisherModel
            {
                Id = publisher
            }.ToPublisherRegistration().ToDeviceTwin(TimeProvider.System);

            using var registry = IoTHubMock.Create(Gateway.YieldReturn() // Single device
                .Append(Discoverer)
                .Append(Supervisor)
                .Append(Publisher), _serializer);

            using (var mock = AutoMock.GetLoose(builder =>
            {
                // Setup
                builder.RegisterInstance(registry).As<IIoTHubTwinServices>();
                builder.RegisterType<DiscovererRegistry>().As<IDiscovererRegistry>();
                builder.RegisterType<SupervisorRegistry>().As<ISupervisorRegistry>();
                builder.RegisterType<PublisherRegistry>().As<IPublisherRegistry>();
                builder.RegisterType<GatewayRegistry>().As<IGatewayRegistry>();
                builder.RegisterType<ApplicationRegistry>().As<IApplicationBulkProcessor>();
            }))
            {
                var service = mock.Create<DiscoveryProcessor>();

                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.Single(registry.Devices);
                Assert.Equal(gateway, registry.Devices.First().Id);
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithAlreadyExistingApplications()
        {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);

            using (registry)
            using (var mock = AutoMock.GetLoose(builder =>
            {
                // Setup
                builder.RegisterInstance(registry).As<IIoTHubTwinServices>();
                builder.RegisterType<DiscovererRegistry>().As<IDiscovererRegistry>();
                builder.RegisterType<SupervisorRegistry>().As<ISupervisorRegistry>();
                builder.RegisterType<PublisherRegistry>().As<IPublisherRegistry>();
                builder.RegisterType<GatewayRegistry>().As<IGatewayRegistry>();
                builder.RegisterType<ApplicationRegistry>().As<IApplicationBulkProcessor>();
            }))
            {
                var service = mock.Create<DiscoveryProcessor>();

                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithNoExistingApplications()
        {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var created,
                out var found, out var registry, 0);

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(created));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryThrowsWithMultipleSites()
        {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);
            found[found.Count / 2].Application.SiteId = "aaaaaaaaaaaa";

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await Assert.ThrowsAsync<ArgumentException>(
                    async () => await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithOneExistingApplication()
        {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var created,
                out var found, out var registry, 1);

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(created));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithDifferentDiscoverersSameSiteApplications()
        {
            var fix = new Fixture();
            var discoverer2 = HubResource.Format(null, fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x =>
                {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ToList().ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                });

            // Assert no changes

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public async Task ProcessOneDiscoveryWithDifferentDiscoverersFromExisting()
        {
            var fix = new Fixture();
            var discoverer2 = HubResource.Format(null, fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x =>
                {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ToList().ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                });

            // Found one item
            found = new List<DiscoveryEventModel> { found[0] };
            // Assert there is still the same content as originally

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithDifferentDiscoverersFromExistingWhenExistingDisabled()
        {
            var fix = new Fixture();
            var discoverer2 = HubResource.Format(null, fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x =>
                {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ToList().ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                }, true);

            // Assert disabled items are now enabled
            var count = registry.Devices.Count();

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.False(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count());
                Assert.All(inreg, a => Assert.Equal(discoverer, a.Application.DiscovererId));
                Assert.All(inreg, a => Assert.Null(a.Application.NotSeenSince));
                Assert.All(inreg, a => Assert.All(a.Endpoints, e => Assert.Equal(discoverer, e.DiscovererId)));
            }
        }

        [Fact]
        public async Task ProcessOneDiscoveryWithDifferentDiscoverersFromExistingWhenExistingDisabled()
        {
            var fix = new Fixture();
            var discoverer2 = HubResource.Format(null, fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x =>
                {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ToList().ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                }, true);

            // Found one app and endpoint
            found = new List<DiscoveryEventModel> { found[0] };
            var count = registry.Devices.Count();
            // Assert disabled items are now enabled

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.Equal(count, registry.Devices.Count());
                Assert.False(inreg.IsSameAs(existing));
                Assert.Equal(discoverer, inreg[0].Application.DiscovererId);
                Assert.Null(inreg[0].Application.NotSeenSince);
                Assert.Equal(discoverer, inreg[0].Endpoints[0].DiscovererId);
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithNoResultsWithDifferentDiscoverersFromExisting()
        {
            var fix = new Fixture();
            var discoverer2 = HubResource.Format(null, fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x =>
                {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ToList().ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                });

            // Found nothing
            found = new List<DiscoveryEventModel>();
            // Assert there is still the same content as originally

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert

                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithNoResultsAndExisting()
        {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);

            // Found nothing
            found = new List<DiscoveryEventModel>();
            var count = registry.Devices.Count();
            // Assert there is still the same content as originally but now disabled

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.True(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count());
                Assert.All(inreg, a => Assert.NotNull(a.Application.NotSeenSince));
            }
        }

        [Fact]
        public async Task ProcessDiscoveryWithOneEndpointResultsAndExisting()
        {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);

            // Found single endpoints
            found = found
                .GroupBy(a => a.Application.ApplicationId)
                .Select(x => x.First()).ToList();
            var count = registry.Devices.Count();

            // All applications, but only one endpoint each is enabled

            using (registry)
            using (var mock = Setup(registry, out var service))
            {
                // Run
                await service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.True(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count());
                var disabled = registry.Devices.Count(d =>
                {
                    if (!d.Tags.ContainsKey("IsDisabled"))
                    {
                        return false;
                    }
                    return (bool?)d.Tags["IsDisabled"] == true;
                });
                Assert.Equal(count - (inreg.Count * 2) - 1, disabled);
            }
        }

        /// <summary>
        /// Setup all services used in processing
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="processor"></param>
        private static AutoMock Setup(IoTHubMock registry, out IDiscoveryResultProcessor processor)
        {
            var mock = AutoMock.GetLoose(builder =>
            {
                //   // builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonsoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(registry).As<IIoTHubTwinServices>();
                builder.RegisterType<DiscovererRegistry>().As<IDiscovererRegistry>();
                builder.RegisterType<SupervisorRegistry>().As<ISupervisorRegistry>();
                builder.RegisterType<PublisherRegistry>().As<IPublisherRegistry>();
                builder.RegisterType<GatewayRegistry>().As<IGatewayRegistry>();
                builder.RegisterType<ApplicationRegistry>().As<IApplicationBulkProcessor>();
            });
            processor = mock.Create<DiscoveryProcessor>();
            return mock;
        }

        /// <summary>
        /// Extract application registrations from registry
        /// </summary>
        /// <param name="registry"></param>
        /// <returns></returns>
        private static List<ApplicationRegistrationModel> ApplicationsIn(IoTHubMock registry)
        {
            var registrations = registry.Devices
                .Select(t => t.ToEntityRegistration())
                .ToList();
            var endpoints = registrations
                .OfType<EndpointRegistration>()
                .GroupBy(d => d.ApplicationId)
                .ToDictionary(
                    k => k.Key,
                    v => v.Select(e => e.ToServiceModel().Registration).ToList());
            return registrations
                .OfType<ApplicationRegistration>()
                .Select(a => a.ToServiceModel())
                .Select(a => new ApplicationRegistrationModel
                {
                    Application = a,
                    Endpoints = endpoints[a.ApplicationId]
                })
                .ToList();
        }

        /// <summary>
        /// Helper to create fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="discoverer"></param>
        /// <param name="supervisor"></param>
        /// <param name="publisher"></param>
        /// <param name="gateway"></param>
        /// <param name="existing"></param>
        /// <param name="found"></param>
        /// <param name="registry"></param>
        /// <param name="countDevices"></param>
        /// <param name="fixup"></param>
        /// <param name="disable"></param>
        private void CreateFixtures(out string site, out string discoverer,
            out string supervisor, out string publisher, out string gateway,
            out List<ApplicationRegistrationModel> existing, out List<DiscoveryEventModel> found,
            out IoTHubMock registry, int countDevices = -1,
            Func<ApplicationRegistrationModel, ApplicationRegistrationModel> fixup = null,
            bool disable = false)
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlySet<>), typeof(HashSet<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyList<>), typeof(List<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyCollection<>), typeof(List<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(VariantValue), typeof(VariantValue)));
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            var sitex = site = fixture.Create<string>();

            gateway = fixture.Create<string>();
            var Gateway = new GatewayModel
            {
                SiteId = site,
                Id = gateway
            }.ToGatewayRegistration().ToDeviceTwin(TimeProvider.System);
            var module = fixture.Create<string>();
            var discovererx = discoverer = HubResource.Format(null, gateway, module);
            var Discoverer = new DiscovererModel
            {
                SiteId = site,
                Id = discovererx
            }.ToPublisherRegistration().ToDeviceTwin(TimeProvider.System);
            module = fixture.Create<string>();
            var supervisorx = supervisor = HubResource.Format(null, gateway, module);
            var Supervisor = new SupervisorModel
            {
                SiteId = site,
                Id = supervisorx
            }.ToPublisherRegistration().ToDeviceTwin(TimeProvider.System);
            module = fixture.Create<string>();
            var publisherx = publisher = HubResource.Format(null, gateway, module);
            var Publisher = new PublisherModel
            {
                SiteId = site,
                Id = publisherx
            }.ToPublisherRegistration().ToDeviceTwin(TimeProvider.System);

            var template = fixture
                .Build<ApplicationRegistrationModel>()
                .Without(x => x.Application)
                .Do(c => c.Application = fixture
                    .Build<ApplicationInfoModel>()
                    .Without(x => x.NotSeenSince)
                    .With(x => x.SiteId, sitex)
                    .With(x => x.DiscovererId, discovererx)
                    .Create())
                .Without(x => x.Endpoints)
                .Do(c => c.Endpoints = fixture
                    .Build<EndpointRegistrationModel>()
                    .With(x => x.SiteId, sitex)
                    .With(x => x.DiscovererId, discovererx)
                    .CreateMany(5)
                    .ToList())
                .CreateMany(5)
                .ToList();
            template.ForEach(a =>
                a.Application.ApplicationId =
                    ApplicationInfoModelEx.CreateApplicationId(a.Application)
            );

            // Create discovery results from template
            var i = 0;
            var now = DateTimeOffset.UtcNow;
            found = template
                 .SelectMany(a => a.Endpoints.Select(
                     e => new DiscoveryEventModel
                     {
                         Application = a.Application,
                         Registration = e,
                         Index = i++,
                         TimeStamp = now
                     }))
                 .ToList();

            // Clone and fixup existing applications as per test case
            existing = template
                .Select(e => e.Clone(TimeProvider.System))
                .Select(fixup ?? (a => a))
                .ToList();
            // and fill registry with them...
            var appdevices = existing
                .Select(a => a.Application.ToApplicationRegistration(disable))
                .Select(a => a.ToDeviceTwin(_serializer, TimeProvider.System));
            var epdevices = existing
                .SelectMany(a => a.Endpoints
                    .Select(e =>
                        new EndpointInfoModel
                        {
                            ApplicationId = a.Application.ApplicationId,
                            Registration = e
                        }.ToEndpointRegistration(disable))
                .Select(e => e.ToDeviceTwin(_serializer, TimeProvider.System)));
            appdevices = appdevices.Concat(epdevices);
            if (countDevices != -1)
            {
                appdevices = appdevices.Take(countDevices);
            }
            registry = IoTHubMock.Create(appdevices
                .Concat(Gateway.YieldReturn())
                .Concat(Discoverer.YieldReturn())
                .Concat(Supervisor.YieldReturn())
                .Concat(Publisher.YieldReturn()), _serializer);
        }

        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();
    }
}
