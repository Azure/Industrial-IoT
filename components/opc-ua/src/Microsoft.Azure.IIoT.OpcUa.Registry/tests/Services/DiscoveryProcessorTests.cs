// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Autofac;

    public class DiscoveryProcessorTests {

        [Fact]
        public void ProcessDiscoveryWithNoResultsAndNoExistingApplications() {
            var found = new List<DiscoveryEventModel>();
            var fix = new Fixture();

            var gateway = fix.Create<string>();
            var Gateway = (new GatewayModel {
                Id = gateway
            }.ToGatewayRegistration().ToDeviceTwin(),
                    new DeviceModel { Id = gateway });
            var module = fix.Create<string>();
            var discoverer = DiscovererModelEx.CreateDiscovererId(gateway, module);
            var Discoverer = (new DiscovererModel {
                Id = discoverer
            }.ToDiscovererRegistration().ToDeviceTwin(_serializer),
                    new DeviceModel { Id = gateway, ModuleId = module });
            module = fix.Create<string>();
            var supervisor = SupervisorModelEx.CreateSupervisorId(gateway, module);
            var Supervisor = (new SupervisorModel {
                Id = supervisor
            }.ToSupervisorRegistration().ToDeviceTwin(_serializer),
                    new DeviceModel { Id = gateway, ModuleId = module });
            module = fix.Create<string>();
            var publisher = PublisherModelEx.CreatePublisherId(gateway, module);
            var Publisher = (new PublisherModel {
                Id = publisher
            }.ToPublisherRegistration().ToDeviceTwin(_serializer),
                    new DeviceModel { Id = gateway, ModuleId = module });

            var registry = IoTHubServices.Create(Gateway.YieldReturn() // Single device
                .Append(Discoverer)
                .Append(Supervisor)
                .Append(Publisher));

            using (var mock = AutoMock.GetLoose(builder => {
                // Setup
                builder.RegisterInstance(registry).As<IIoTHubTwinServices>();
                builder.RegisterType<ApplicationTwins>().As<IApplicationRepository>();
                builder.RegisterType<DiscovererRegistry>().As<IDiscovererRegistry>();
                builder.RegisterType<SupervisorRegistry>().As<ISupervisorRegistry>();
                builder.RegisterType<PublisherRegistry>().As<IPublisherRegistry>();
                builder.RegisterType<GatewayRegistry>().As<IGatewayRegistry>();
                builder.RegisterType<ApplicationRegistry>().As<IApplicationBulkProcessor>();
                builder.RegisterType<EndpointRegistry>().As<IEndpointBulkProcessor>();
            })) {
                var service = mock.Create<DiscoveryProcessor>();

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                Assert.Single(registry.Devices);
                Assert.Equal(gateway, registry.Devices.First().Device.Id);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithAlreadyExistingApplications() {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);

            using (var mock = AutoMock.GetLoose(builder => {
                // Setup
                builder.RegisterInstance(registry).As<IIoTHubTwinServices>();
                builder.RegisterType<ApplicationTwins>().As<IApplicationRepository>();
                builder.RegisterType<DiscovererRegistry>().As<IDiscovererRegistry>();
                builder.RegisterType<SupervisorRegistry>().As<ISupervisorRegistry>();
                builder.RegisterType<PublisherRegistry>().As<IPublisherRegistry>();
                builder.RegisterType<GatewayRegistry>().As<IGatewayRegistry>();
                builder.RegisterType<ApplicationRegistry>().As<IApplicationBulkProcessor>();
                builder.RegisterType<EndpointRegistry>().As<IEndpointBulkProcessor>();
            })) {
                var service = mock.Create<DiscoveryProcessor>();

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoExistingApplications() {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var created,
                out var found, out var registry, 0);

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(created));
            }
        }

        [Fact]
        public void ProcessDiscoveryThrowsWithMultipleSites() {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);
            found[found.Count / 2].Application.SiteId = "aaaaaaaaaaaa";

            using (var mock = Setup(registry, out var service)) {

                // Run
                var t = service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ArgumentException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithOneExistingApplication() {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var created,
                out var found, out var registry, 1);

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(created));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithDifferentDiscoverersSameSiteApplications() {
            var fix = new Fixture();
            var discoverer2 = DiscovererModelEx.CreateDiscovererId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x => {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                });

            // Assert no changes

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessOneDiscoveryWithDifferentDiscoverersFromExisting() {
            var fix = new Fixture();
            var discoverer2 = DiscovererModelEx.CreateDiscovererId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x => {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                });

            // Found one item
            found = new List<DiscoveryEventModel> { found.First() };
            // Assert there is still the same content as originally

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithDifferentDiscoverersFromExistingWhenExistingDisabled() {
            var fix = new Fixture();
            var discoverer2 = DiscovererModelEx.CreateDiscovererId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x => {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                }, true);

            // Assert disabled items are now enabled
            var count = registry.Devices.Count();

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

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
        public void ProcessOneDiscoveryWithDifferentDiscoverersFromExistingWhenExistingDisabled() {
            var fix = new Fixture();
            var discoverer2 = DiscovererModelEx.CreateDiscovererId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x => {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                }, true);

            // Found one app and endpoint
            found = new List<DiscoveryEventModel> { found.First() };
            var count = registry.Devices.Count();
            // Assert disabled items are now enabled

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.Equal(count, registry.Devices.Count());
                Assert.False(inreg.IsSameAs(existing));
                Assert.Equal(discoverer, inreg.First().Application.DiscovererId);
                Assert.Null(inreg.First().Application.NotSeenSince);
                Assert.Equal(discoverer, inreg.First().Endpoints.First().DiscovererId);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoResultsWithDifferentDiscoverersFromExisting() {
            var fix = new Fixture();
            var discoverer2 = DiscovererModelEx.CreateDiscovererId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different Discoverer...
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry, -1, x => {
                    x.Application.DiscovererId = discoverer2;
                    x.Endpoints.ForEach(e => e.DiscovererId = discoverer2);
                    return x;
                });

            // Found nothing
            found = new List<DiscoveryEventModel>();
            // Assert there is still the same content as originally

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert

                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoResultsAndExisting() {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);

            // Found nothing
            found = new List<DiscoveryEventModel>();
            var count = registry.Devices.Count();
            // Assert there is still the same content as originally but now disabled

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.True(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count());
                Assert.All(inreg, a => Assert.NotNull(a.Application.NotSeenSince));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithOneEndpointResultsAndExisting() {
            CreateFixtures(out var site, out var discoverer, out var supervisor,
                out var publisher, out var gateway, out var existing,
                out var found, out var registry);

            // Found single endpoints
            found = found
                .GroupBy(a => a.Application.ApplicationId)
                .Select(x => x.First()).ToList();
            var count = registry.Devices.Count();

            // All applications, but only one endpoint each is enabled

            using (var mock = Setup(registry, out var service)) {

                // Run
                service.ProcessDiscoveryResultsAsync(discoverer, new DiscoveryResultModel(), found).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.True(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count());
                var disabled = registry.Devices.Count(d => {
                    if (!d.Twin.Tags.ContainsKey("IsDisabled")) {
                        return false;
                    }
                    return (bool?)d.Twin.Tags["IsDisabled"] == true;
                });
                Assert.Equal(count - (inreg.Count * 2) - 1, disabled);
            }
        }

        /// <summary>
        /// Setup all services used in processing
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="registry"></param>
        private static AutoMock Setup(IoTHubServices registry, out IDiscoveryResultProcessor processor) {
            var mock = AutoMock.GetLoose(builder => {
                builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonSoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(registry).As<IIoTHubTwinServices>();
                builder.RegisterType<ApplicationTwins>().As<IApplicationRepository>();
                builder.RegisterType<DiscovererRegistry>().As<IDiscovererRegistry>();
                builder.RegisterType<SupervisorRegistry>().As<ISupervisorRegistry>();
                builder.RegisterType<PublisherRegistry>().As<IPublisherRegistry>();
                builder.RegisterType<GatewayRegistry>().As<IGatewayRegistry>();
                builder.RegisterType<EndpointRegistry>().As<IEndpointBulkProcessor>();
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
        private static List<ApplicationRegistrationModel> ApplicationsIn(IoTHubServices registry) {
            var registrations = registry.Devices
                .Select(d => d.Twin)
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
                .Select(a => new ApplicationRegistrationModel {
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
        /// <param name="existing"></param>
        /// <param name="found"></param>
        /// <param name="registry"></param>
        /// <param name="countDevices"></param>
        /// <param name="fixup"></param>
        /// <param name="disable"></param>
        private void CreateFixtures(out string site, out string discoverer,
            out string supervisor, out string publisher, out string gateway,
            out List<ApplicationRegistrationModel> existing, out List<DiscoveryEventModel> found,
            out IoTHubServices registry, int countDevices = -1,
            Func<ApplicationRegistrationModel, ApplicationRegistrationModel> fixup = null,
            bool disable = false) {
            var fix = new Fixture();

            // Create template applications and endpoints
            fix.Customizations.Add(new TypeRelay(typeof(VariantValue), typeof(VariantValue)));
            fix.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fix.Behaviors.Remove(b));
            fix.Behaviors.Add(new OmitOnRecursionBehavior());
            var sitex = site = fix.Create<string>();

            gateway = fix.Create<string>();
            var Gateway = (new GatewayModel {
                SiteId = site,
                Id = gateway
            }.ToGatewayRegistration().ToDeviceTwin(),
                    new DeviceModel { Id = gateway });
            var module = fix.Create<string>();
            var discovererx = discoverer = DiscovererModelEx.CreateDiscovererId(gateway, module);
            var Discoverer = (new DiscovererModel {
                SiteId = site,
                Id = discovererx
            }.ToDiscovererRegistration().ToDeviceTwin(_serializer),
                    new DeviceModel { Id = gateway, ModuleId = module });
            module = fix.Create<string>();
            var supervisorx = supervisor = SupervisorModelEx.CreateSupervisorId(gateway, module);
            var Supervisor = (new SupervisorModel {
                SiteId = site,
                Id = supervisorx
            }.ToSupervisorRegistration().ToDeviceTwin(_serializer),
                    new DeviceModel { Id = gateway, ModuleId = module });
            module = fix.Create<string>();
            var publisherx = publisher = PublisherModelEx.CreatePublisherId(gateway, module);
            var Publisher = (new PublisherModel {
                SiteId = site,
                Id = publisherx
            }.ToPublisherRegistration().ToDeviceTwin(_serializer),
                    new DeviceModel { Id = gateway, ModuleId = module });

            var template = fix
                .Build<ApplicationRegistrationModel>()
                .Without(x => x.Application)
                .Do(c => c.Application = fix
                    .Build<ApplicationInfoModel>()
                    .Without(x => x.NotSeenSince)
                    .With(x => x.SiteId, sitex)
                    .With(x => x.DiscovererId, discovererx)
                    .Create())
                .Without(x => x.Endpoints)
                .Do(c => c.Endpoints = fix
                    .Build<EndpointRegistrationModel>()
                    .With(x => x.SiteId, sitex)
                    .With(x => x.DiscovererId, discovererx)
                    .With(x => x.SupervisorId, supervisorx)
                    .CreateMany(5)
                    .ToList())
                .CreateMany(5)
                .ToList();
            template.ForEach(a =>
                a.Application.ApplicationId =
                    ApplicationInfoModelEx.CreateApplicationId(a.Application)
            );

            // Create discovery results from template
            var i = 0; var now = DateTime.UtcNow;
            found = template
                 .SelectMany(a => a.Endpoints.Select(
                     e => new DiscoveryEventModel {
                         Application = a.Application,
                         Registration = e,
                         Index = i++,
                         TimeStamp = now
                     }))
                 .ToList();

            // Clone and fixup existing applications as per test case
            existing = template
                .Select(e => e.Clone())
                .Select(fixup ?? (a => a))
                .ToList();
            // and fill registry with them...
            var appdevices = existing
                .Select(a => a.Application.ToApplicationRegistration(disable))
                .Select(a => a.ToDeviceTwin(_serializer))
                .Select(d => (d, new DeviceModel { Id = d.Id }));
            var epdevices = existing
                .SelectMany(a => a.Endpoints
                    .Select(e =>
                        new EndpointInfoModel {
                            ApplicationId = a.Application.ApplicationId,
                            Registration = e
                        }.ToEndpointRegistration(_serializer, disable))
                .Select(e => e.ToDeviceTwin(_serializer)))
                .Select(d => (d, new DeviceModel { Id = d.Id }));
            appdevices = appdevices.Concat(epdevices);
            if (countDevices != -1) {
                appdevices = appdevices.Take(countDevices);
            }
            registry = IoTHubServices.Create(appdevices
                .Concat(Gateway.YieldReturn())
                .Concat(Discoverer.YieldReturn())
                .Concat(Supervisor.YieldReturn())
                .Concat(Publisher.YieldReturn()));
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
