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

    public class DiscovererRegistryTests {

        [Fact]
        public void GetDiscovererThatDoesNotExist() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var t = service.GetDiscovererAsync("test", false);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetDiscovererThatExists() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var result = service.GetDiscovererAsync(discoverers.First().Id, false).Result;

                // Assert
                Assert.True(result.IsSameAs(discoverers.First()));
            }
        }

        [Fact]
        public void ListAllDiscoverers() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.ListDiscoverersAsync(null, false, null).Result;

                // Assert
                Assert.True(discoverers.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllDiscoverersUsingQuery() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(null, false, null).Result;

                // Assert
                Assert.True(discoverers.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryDiscoverersByDiscoveryMode() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(new DiscovererQueryModel {
                    Discovery = DiscoveryMode.Network
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count == discoverers.Count(x => x.Discovery == DiscoveryMode.Network));
            }
        }

        [Fact]
        public void QueryDiscoverersBySiteId() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(new DiscovererQueryModel {
                    SiteId = site
                }, false, null).Result;

                // Assert
                Assert.True(discoverers.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryDiscoverersByNoneExistantSiteId() {
            CreateDiscovererFixtures(out var site, out var discoverers, out var modules, true);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                IDiscovererRegistry service = mock.Create<DiscovererRegistry>();

                // Run
                var records = service.QueryDiscoverersAsync(new DiscovererQueryModel {
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
        /// <param name="discoverers"></param>
        /// <param name="modules"></param>
        private static void CreateDiscovererFixtures(out string site,
            out List<DiscovererModel> discoverers, out List<(DeviceTwinModel, DeviceModel)> modules,
            bool noSite = false) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var sitex = site = noSite ? null : fix.Create<string>();
            discoverers = fix
                .Build<DiscovererModel>()
                .With(x => x.SiteId, sitex)
                .Without(x => x.Id)
                .Do(x => x.Id = DiscovererModelEx.CreateDiscovererId(
                    fix.Create<string>(), fix.Create<string>()))
                .CreateMany(10)
                .ToList();

            modules = discoverers
                .Select(a => a.ToDiscovererRegistration())
                .Select(a => a.ToDeviceTwin())
                .Select(t => {
                    t.Properties.Reported = new Dictionary<string, JToken> {
                        [TwinProperty.Type] = IdentityType.Discoverer
                    };
                    return t;
                })
                .Select(t => (t, new DeviceModel { Id = t.Id, ModuleId = t.ModuleId }))
                .ToList();
        }
    }
}
