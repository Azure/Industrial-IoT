// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Memory;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class OpcUaSupervisorRegistryTests {

        [Fact]
        public void GetSupervisorThatDoesNotExist() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var t = service.GetSupervisorAsync("test");

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetSupervisorThatExists() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var result = service.GetSupervisorAsync(supervisors.First().Id).Result;

                // Assert
                Assert.True(result.IsSameAs(supervisors.First()));
            }
        }

        [Fact]
        public void ListAllSupervisors() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var records = service.ListSupervisorsAsync(null, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllSupervisorsUsingQuery() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var records = service.QuerySupervisorsAsync(null, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QuerySupervisorsByDiscoveryMode() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel {
                    Discovery = DiscoveryMode.Network
                }, null).Result;

                // Assert
                Assert.True(records.Items.Count == supervisors.Count(x => x.Discovery == DiscoveryMode.Network));
            }
        }

        [Fact]
        public void QuerySupervisorsBySiteId() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel {
                    SiteId = site
                }, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QuerySupervisorsByNoneExistantSiteId() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules, true);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Modules = modules
                });
                var service = mock.Create<OpcUaRegistryServices>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel {
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
        /// <param name="supervisors"></param>
        /// <param name="modules"></param>
        private static void CreateSupervisorFixtures(out string site,
            out List<SupervisorModel> supervisors, out List<IoTHubDeviceModel> modules,
            bool noSite = false) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var sitex = site = noSite ? null : fix.Create<string>();
            supervisors = fix
                .Build<SupervisorModel>()
                .With(x => x.SiteId, sitex)
                .Without(x => x.Id)
                .Do(x => x.Id = SupervisorModelEx.CreateSupervisorId(
                    fix.Create<string>(), fix.Create<string>()))
                .CreateMany(10)
                .ToList();

            modules = supervisors
                .Select(a => OpcUaSupervisorRegistration.FromServiceModel(a))
                .Select(a => OpcUaSupervisorRegistration.Patch(null, a))
                .Select(t => {
                    t.Properties.Reported = new Dictionary<string, JToken> {
                        [OpcUaTwinRegistration.kTypeProp] = "supervisor"
                    };
                    return t;
                })
                .Select(t => new IoTHubDeviceModel {
                    Twin = t,
                    Device = new DeviceModel {
                        Id = t.Id,
                        ModuleId = t.ModuleId
                    }
                })
                .ToList();
        }
    }
}
