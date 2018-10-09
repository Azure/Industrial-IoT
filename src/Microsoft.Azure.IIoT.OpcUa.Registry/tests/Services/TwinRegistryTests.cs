// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class TwinRegistryTests {

        [Fact]
        public void GetTwinThatDoesNotExist() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var t = service.GetTwinAsync("test", false);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetTwinThatExists() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);
            var id = TwinInfoModelEx.CreateTwinId(twins.First().ApplicationId,
                twins.First().Registration.Endpoint);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var result = service.GetTwinAsync(id, false).Result;

                // Assert
                Assert.True(result.IsSameAs(twins.First()));
            }
        }

        [Fact]
        public void ListAllTwins() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.ListTwinsAsync(null, false, null).Result;

                // Assert
                Assert.True(twins.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllTwinsUsingQuery() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(null, false, null).Result;

                // Assert
                Assert.True(twins.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryTwinsBySignSecurityMode() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);
            var count = twins.Count(x => x.Registration.Endpoint.SecurityMode == SecurityMode.Sign);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    SecurityMode = SecurityMode.Sign
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByActivation() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);
            var count = twins.Count(x => x.Activated ?? false);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    Activated = true
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByDeactivation() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);
            var count = twins.Count(x => !(x.Activated ?? false));

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    Activated = false
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByConnectivity() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);
            var count = twins.Count(x => x.Connected ?? false);
            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    Connected = true
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByDisconnectivity() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);
            var count = twins.Count(x => !(x.Connected ?? false));
            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    Connected = false
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsBySecurityPolicySameCase() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    SecurityPolicy = twins.First().Registration.Endpoint.SecurityPolicy
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count >= 1);
                Assert.True(records.Items.First().IsSameAs(twins.First()));
            }
        }

        [Fact]
        public void QueryTwinsBySecurityPolicyDifferentCase() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    SecurityPolicy = twins.First().Registration.Endpoint.SecurityPolicy.ToUpperInvariant()
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count == 0);
            }
        }

        [Fact]
        public void QueryTwinsByEndpointUrlDifferentCase() {
            CreateTwinFixtures(out var site, out var super, out var twins, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubDeviceRegistry {
                    Devices = devices
                });
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryTwinsAsync(new TwinRegistrationQueryModel {
                    Url = twins.First().Registration.Endpoint.Url.ToUpperInvariant()
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count >= 1);
                Assert.True(records.Items.First().IsSameAs(twins.First()));
            }
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="super"></param>
        /// <param name="twins"></param>
        /// <param name="devices"></param>
        private static void CreateTwinFixtures(out string site, out string super,
            out List<TwinInfoModel> twins, out List<IoTHubDeviceModel> devices,
            bool noSite = false) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var sitex = site = noSite ? null : fix.Create<string>();
            var superx = super = fix.Create<string>();
            twins = fix
                .Build<TwinInfoModel>()
                .Without(x => x.Registration)
                .Do(x => x.Registration = fix
                    .Build<TwinRegistrationModel>()
                    .With(y => y.SiteId, sitex)
                    .With(y => y.SupervisorId, superx)
                    .Create())
                .CreateMany(10)
                .ToList();

            devices = twins
                .Select(a => {
                    var r = EndpointRegistration.FromServiceModel(a);
                    var t = EndpointRegistration.Patch(null, r);
                    t.Properties.Reported = new Dictionary<string, JToken> {
                        [BaseRegistration.kTypeProp] = "Twin"
                    };
                    if (a.Registration.SiteId != null) {
                        t.Properties.Reported.Add(BaseRegistration.kSiteIdProp, a.Registration.SiteId);
                    }
                    if (a.Connected ?? false) {
                        t.ConnectionState = "Connected";
                        t.Properties.Reported.Add(BaseRegistration.kConnectedProp, true);
                    }
                    return t;
                })
                .Select(t => new IoTHubDeviceModel {
                    Twin = t,
                    Device = new DeviceModel {
                        Id = t.Id
                    }
                })
                .ToList();
        }
    }
}
