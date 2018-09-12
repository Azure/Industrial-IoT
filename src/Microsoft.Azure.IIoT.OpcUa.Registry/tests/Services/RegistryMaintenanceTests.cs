// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class RegistryMaintenanceTests {

        [Fact]
        public void ProcessDiscoveryWithNoResultsAndNoExistingApplications() {
            var found = new List<DiscoveryEventModel>();
            var fix = new Fixture();

            var device = fix.Create<string>();
            var module = fix.Create<string>();
            var super = SupervisorModelEx.CreateSupervisorId(device, module);
            var supervisor = new IoTHubDeviceModel {
                Device = new DeviceModel {
                    Id = device,
                    ModuleId = module
                },
                Twin = SupervisorRegistration.Patch(null,
                    SupervisorRegistration.FromServiceModel(new SupervisorModel {
                        Id = super
                    }))
            };

            var registry = new IoTHubDeviceRegistry {
                Modules = supervisor.YieldReturn().ToList()
            };

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                Assert.Empty(registry.Devices);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithAlreadyExistingApplications() {
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry);

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoExistingApplications() {
            CreateFixtures(out var site, out var super, out var created,
                out var found, out var registry);
            registry.Devices.Clear();

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(created));
            }
        }

        [Fact]
        public void ProcessDiscoveryThrowsWithMultipleSites() {
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry);
            found[found.Count / 2].Application.SiteId = "aaaaaaaaaaaa";

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                var t = service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ArgumentException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithOneExistingApplication() {
            CreateFixtures(out var site, out var super, out var created,
                out var found, out var registry);

            registry.Devices = registry.Devices.First().YieldReturn().ToList();

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(created));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithDifferentSupervisorsSameSiteApplications() {
            var fix = new Fixture();
            var super2 = SupervisorModelEx.CreateSupervisorId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different supervisor...
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry, x => {
                    x.Application.SupervisorId = super2;
                    x.Endpoints.ForEach(e => e.SupervisorId = super2);
                    return x;
                });

            // Assert no changes

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessOneDiscoveryWithDifferentSupervisorsFromExisting() {
            var fix = new Fixture();
            var super2 = SupervisorModelEx.CreateSupervisorId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different supervisor...
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry, x => {
                    x.Application.SupervisorId = super2;
                    x.Endpoints.ForEach(e => e.SupervisorId = super2);
                    return x;
                });

            // Found one item
            found = new List<DiscoveryEventModel> { found.First() };
            // Assert there is still the same content as originally

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithDifferentSupervisorsFromExistingWhenExistingDisabled() {
            var fix = new Fixture();
            var super2 = SupervisorModelEx.CreateSupervisorId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different supervisor...
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry, x => {
                    x.Application.SupervisorId = super2;
                    x.Endpoints.ForEach(e => e.SupervisorId = super2);
                    return x;
                }, true);

            // Assert disabled items are now enabled
            var count = registry.Devices.Count;

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.False(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count);
                Assert.All(inreg, a => Assert.Equal(super, a.Application.SupervisorId));
                Assert.All(inreg, a => Assert.Null(a.Application.NotSeenSince));
                Assert.All(inreg, a => Assert.All(a.Endpoints, e => Assert.Equal(super, e.SupervisorId)));
            }
        }

        [Fact]
        public void ProcessOneDiscoveryWithDifferentSupervisorsFromExistingWhenExistingDisabled() {
            var fix = new Fixture();
            var super2 = SupervisorModelEx.CreateSupervisorId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different supervisor...
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry, x => {
                    x.Application.SupervisorId = super2;
                    x.Endpoints.ForEach(e => e.SupervisorId = super2);
                    return x;
                }, true);

            // Found one app and endpoint
            found = new List<DiscoveryEventModel> { found.First() };
            var count = registry.Devices.Count;
            // Assert disabled items are now enabled

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.Equal(count, registry.Devices.Count);
                Assert.False(inreg.IsSameAs(existing));
                Assert.Equal(super, inreg.First().Application.SupervisorId);
                Assert.Null(inreg.First().Application.NotSeenSince);
                Assert.Equal(super, inreg.First().Endpoints.First().SupervisorId);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoResultsWithDifferentSupervisorsFromExisting() {
            var fix = new Fixture();
            var super2 = SupervisorModelEx.CreateSupervisorId(fix.Create<string>(), fix.Create<string>());

            // Readjust existing to be reported from different supervisor...
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry, x => {
                    x.Application.SupervisorId = super2;
                    x.Endpoints.ForEach(e => e.SupervisorId = super2);
                    return x;
                });

            // Found nothing
            found = new List<DiscoveryEventModel>();
            // Assert there is still the same content as originally

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert

                Assert.True(ApplicationsIn(registry).IsSameAs(existing));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoResultsAndExisting() {
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry);

            // Found nothing
            found = new List<DiscoveryEventModel>();
            var count = registry.Devices.Count;
            // Assert there is still the same content as originally but now disabled

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.True(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count);
                Assert.All(inreg, a => Assert.NotNull(a.Application.NotSeenSince));
            }
        }

        [Fact]
        public void ProcessDiscoveryWithOneEndpointResultsAndExisting() {
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry);

            // Found single endpoints
            found = found
                .GroupBy(a => a.Application.ApplicationId)
                .Select(x => x.First()).ToList();
            var count = registry.Devices.Count;

            // All applications, but only one endpoint each is enabled

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, false).Wait();

                // Assert
                var inreg = ApplicationsIn(registry);
                Assert.True(inreg.IsSameAs(existing));
                Assert.Equal(count, registry.Devices.Count);
                var disabled = registry.Devices.Count(d => (bool?)d.Twin.Tags["IsDisabled"] == true);
                Assert.Equal(count - (inreg.Count * 2), disabled);
            }
        }

        [Fact]
        public void ProcessDiscoveryWithNoResultsAndExistingAndHardDelete() {
            CreateFixtures(out var site, out var super, out var existing,
                out var found, out var registry);

            // Found nothing
            found = new List<DiscoveryEventModel>();
            var count = registry.Devices.Count;
            // Assert there is still the same content as originally but now disabled

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                mock.Provide<IIoTHubTwinServices>(registry);
                var service = mock.Create<RegistryServices>();

                // Run
                service.ProcessDiscoveryAsync(super, new DiscoveryResultModel(), found, true).Wait();

                // Assert
                Assert.Empty(ApplicationsIn(registry));
            }
        }

        /// <summary>
        /// Extract application registrations from registry
        /// </summary>
        /// <param name="registry"></param>
        /// <returns></returns>
        private static List<ApplicationRegistrationModel> ApplicationsIn(IoTHubDeviceRegistry registry) {
            var registrations = registry.Devices
                .Select(d => d.Twin)
                .Select(BaseRegistration.ToRegistration)
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
        /// <param name="super"></param>
        /// <param name="existing"></param>
        /// <param name="found"></param>
        private static void CreateFixtures(out string site, out string super,
            out List<ApplicationRegistrationModel> existing, out List<DiscoveryEventModel> found,
            out IoTHubDeviceRegistry registry,
            Func<ApplicationRegistrationModel, ApplicationRegistrationModel> fixup = null,
            bool disable = false) {
            var fix = new Fixture();

            // Create template applications and endpoints
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var sitex = site = fix.Create<string>();

            var module = fix.Create<string>();
            var device = fix.Create<string>();
            var superx = super = SupervisorModelEx.CreateSupervisorId(device, module);

            var supervisor = new IoTHubDeviceModel {
                Device = new DeviceModel {
                    Id = device,
                    ModuleId = module
                },
                Twin = SupervisorRegistration.Patch(null,
                    SupervisorRegistration.FromServiceModel(new SupervisorModel {
                        SiteId = site,
                        Id = superx
                    }))
            };

            var template = fix
                .Build<ApplicationRegistrationModel>()
                .Without(x => x.Application)
                .Do(c => c.Application = fix
                    .Build<ApplicationInfoModel>()
                    .Without(x => x.NotSeenSince)
                    .With(x => x.SiteId, sitex)
                    .With(x => x.SupervisorId, superx)
                    .Create())
                .Without(x => x.Endpoints)
                .Do(c => c.Endpoints = fix
                    .Build<TwinRegistrationModel>()
                    .With(x => x.SiteId, sitex)
                    .With(x => x.SupervisorId, superx)
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
                .Select(a => ApplicationRegistration.FromServiceModel(a.Application, disable))
                .Select(a => ApplicationRegistration.Patch(null, a))
                .Select(d => new IoTHubDeviceModel {
                    Device = new DeviceModel {
                        Id = d.Id
                    },
                    Twin = d
                });
            var epdevices = existing
                .SelectMany(a => a.Endpoints
                    .Select(e => EndpointRegistration.FromServiceModel(
                        new TwinInfoModel {
                            ApplicationId = a.Application.ApplicationId,
                            Registration = e
                        }, disable)))
                .Select(e => EndpointRegistration.Patch(null, e))
                .Select(d => new IoTHubDeviceModel {
                    Device = new DeviceModel {
                        Id = d.Id
                    },
                    Twin = d
                });
            registry = new IoTHubDeviceRegistry {
                Devices = appdevices.Concat(epdevices).ToList(),
                Modules = supervisor.YieldReturn().ToList()
            };
        }
    }
}
