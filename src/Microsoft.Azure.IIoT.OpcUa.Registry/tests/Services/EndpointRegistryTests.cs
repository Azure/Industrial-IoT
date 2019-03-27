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

    public class EndpointRegistryTests {

        [Fact]
        public void GetTwinThatDoesNotExist() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var t = service.GetEndpointAsync("test", false);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetTwinThatExists() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);
            var first = endpoints.First();
            var id = EndpointInfoModelEx.CreateEndpointId(first.ApplicationId,
                first.Registration.EndpointUrl, first.Registration.Endpoint.SecurityMode,
                first.Registration.Endpoint.SecurityPolicy);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var result = service.GetEndpointAsync(id, false).Result;

                // Assert
                Assert.True(result.IsSameAs(endpoints.First()));
            }
        }

        [Fact]
        public void ListAllTwins() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.ListEndpointsAsync(null, false, null).Result;

                // Assert
                Assert.True(endpoints.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllTwinsUsingQuery() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(null, false, null).Result;

                // Assert
                Assert.True(endpoints.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryTwinsBySignSecurityMode() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);
            var count = endpoints.Count(x => x.Registration.Endpoint.SecurityMode == SecurityMode.Sign);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    SecurityMode = SecurityMode.Sign
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByActivation() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);
            var count = endpoints.Count(x => x.IsTwinActivated());

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    Activated = true
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByDeactivation() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);
            var count = endpoints.Count(x => !(x.IsTwinActivated()));

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    Activated = false
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByConnectivity() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);
            var count = endpoints.Count(x => x.IsTwinConnected());
            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    Connected = true
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsByDisconnectivity() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);
            var count = endpoints.Count(x => !(x.IsTwinConnected()));
            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    Connected = false
                }, false, null).Result;

                // Assert
                Assert.Equal(count, records.Items.Count);
            }
        }

        [Fact]
        public void QueryTwinsBySecurityPolicySameCase() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    SecurityPolicy = endpoints.First().Registration.Endpoint.SecurityPolicy
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count >= 1);
                Assert.True(records.Items.First().IsSameAs(endpoints.First()));
            }
        }

        [Fact]
        public void QueryTwinsBySecurityPolicyDifferentCase() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    SecurityPolicy = endpoints.First().Registration.Endpoint.SecurityPolicy.ToUpperInvariant()
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count == 0);
            }
        }

        [Fact]
        public void QueryTwinsByEndpointUrlDifferentCase() {
            CreateEndpointFixtures(out var site, out var super, out var endpoints, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(new IoTHubServices(devices));
                var service = mock.Create<RegistryServices>();

                // Run
                var records = service.QueryEndpointsAsync(new EndpointRegistrationQueryModel {
                    Url = endpoints.First().Registration.Endpoint.Url.ToUpperInvariant()
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count >= 1);
                Assert.True(records.Items.First().IsSameAs(endpoints.First()));
            }
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="super"></param>
        /// <param name="endpoints"></param>
        /// <param name="devices"></param>
        private static void CreateEndpointFixtures(out string site, out string super,
            out List<EndpointInfoModel> endpoints, out List<(DeviceTwinModel, DeviceModel)> devices,
            bool noSite = false) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var sitex = site = noSite ? null : fix.Create<string>();
            var superx = super = fix.Create<string>();
            endpoints = fix
                .Build<EndpointInfoModel>()
                .Without(x => x.Registration)
                .Do(x => x.Registration = fix
                    .Build<EndpointRegistrationModel>()
                    .With(y => y.SiteId, sitex)
                    .With(y => y.SupervisorId, superx)
                    .Create())
                .CreateMany(10)
                .ToList();

            devices = endpoints
                .Select(a => {
                    a.Registration.EndpointUrl = a.Registration.Endpoint.Url;
                    var r = EndpointRegistration.FromServiceModel(a);
                    var t = EndpointRegistration.Patch(null, r);
                    t.Properties.Reported = new Dictionary<string, JToken> {
                        [TwinProperty.kType] = "Twin"
                    };
                    if (a.Registration.SiteId != null) {
                        t.Properties.Reported.Add(TwinProperty.kSiteId, a.Registration.SiteId);
                    }
                    if (a.IsTwinConnected()) {
                        t.ConnectionState = "Connected";
                        t.Properties.Reported.Add(TwinProperty.kConnected, true);
                    }
                    return t;
                })
                .Select(t => (t, new DeviceModel { Id = t.Id }))
                .ToList();
        }
    }
}
