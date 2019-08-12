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
    using System.Threading.Tasks;
    using System.Linq;
    using Xunit;

    public class ApplicationRegistryTests {

        [Fact]
        public void GetApplicationThatDoesNotExist() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var t = service.GetApplicationAsync("test", false);

                // Assert
                Assert.NotNull(t.Exception);
                Assert.IsType<AggregateException>(t.Exception);
                Assert.IsType<ResourceNotFoundException>(t.Exception.InnerException);
            }
        }

        [Fact]
        public void GetApplicationThatExists() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);
            var first = apps.First();

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var result = service.GetApplicationAsync(
                    ApplicationInfoModelEx.CreateApplicationId(site,
                    first.ApplicationUri, first.ApplicationType), false).Result;

                // Assert
                Assert.True(result.Application.IsSameAs(apps.First()));
                Assert.True(result.Endpoints.Count == 0);
            }
        }

        [Fact]
        public void ListAllApplications() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.ListApplicationsAsync(null, null).Result;

                // Assert
                Assert.True(apps.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllApplicationsUsingQuery() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(null, null).Result;

                // Assert
                Assert.True(apps.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryApplicationsByClientAndServerApplicationType() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationType = ApplicationType.ClientAndServer
                }, null).Result;

                // Assert
                Assert.Equal(apps.Count(x =>
                    x.ApplicationType == ApplicationType.ClientAndServer), records.Items.Count);
            }
        }

        [Fact]
        public void QueryApplicationsByServerApplicationType() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationType = ApplicationType.Server
                }, null).Result;

                // Assert
                Assert.Equal(apps.Count(x => x.ApplicationType != ApplicationType.Client), records.Items.Count);
            }
        }

        [Fact]
        public void QueryApplicationsByDiscoveryServerApplicationType() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationType = ApplicationType.DiscoveryServer
                }, null).Result;

                // Assert
                Assert.Equal(apps.Count(x => x.ApplicationType == ApplicationType.DiscoveryServer), records.Items.Count);
            }
        }

        [Fact]
        public void QueryApplicationsBySiteId() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    SiteOrSupervisorId = site
                }, null).Result;

                // Assert
                Assert.True(apps.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QueryApplicationsBySupervisorId() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices, true);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    SiteOrSupervisorId = super
                }, null).Result;

                // Assert
                Assert.True(apps.IsSameAs(records.Items));
            }
        }


        [Fact]
        public void QueryApplicationsByClientApplicationType() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationType = ApplicationType.Client
                }, null).Result;

                // Assert
                Assert.Equal(apps.Count(x =>
                    x.ApplicationType != ApplicationType.Server &&
                    x.ApplicationType != ApplicationType.DiscoveryServer), records.Items.Count);
            }
        }

        [Fact]
        public void QueryApplicationsByApplicationNameSameCase() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationName = apps.First().ApplicationName
                }, null).Result;

                // Assert
                Assert.True(records.Items.Count >= 1);
                Assert.True(records.Items.First().IsSameAs(apps.First()));
            }
        }

        [Fact]
        public void QueryApplicationsByApplicationNameDifferentCase() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationName = apps.First().ApplicationName.ToUpperInvariant()
                }, null).Result;

                // Assert
                Assert.True(records.Items.Count == 0);
            }
        }

        [Fact]
        public void QueryApplicationsByApplicationUriDifferentCase() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(devices));
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                var records = service.QueryApplicationsAsync(new ApplicationRegistrationQueryModel {
                    ApplicationUri = apps.First().ApplicationUri.ToUpperInvariant()
                }, null).Result;

                // Assert
                Assert.True(records.Items.Count >= 1);
                Assert.True(records.Items.First().IsSameAs(apps.First()));
            }
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [Fact]
        public void RegisterApplication() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                var hub = new IoTHubServices();
                mock.Provide<IIoTHubTwinServices>(hub);
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                foreach (var app in apps) {
                    var record = service.RegisterApplicationAsync(
                        app.ToRegistrationRequest()).Result;
                }

                // Assert
                Assert.Equal(apps.Count, hub.Devices.Count());
                var records = service.ListApplicationsAsync(null, null).Result;

                // Assert
                Assert.True(apps.IsSameAs(records.Items));
            }
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [Fact]
        public void UnregisterApplications() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                var hub = IoTHubServices.Create(devices);
                mock.Provide<IIoTHubTwinServices>(hub);
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                // Run
                foreach (var app in apps) {
                    service.UnregisterApplicationAsync(app.ApplicationId, null).Wait();
                }

                // Assert
                Assert.Empty(hub.Devices);
            }
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [Fact]
        public async Task BadArgShouldThrowExceptions() {
            using (var mock = AutoMock.GetLoose()) {
                var hub = new IoTHubServices();
                mock.Provide<IIoTHubTwinServices>(hub);
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                await Assert.ThrowsAsync<ArgumentNullException>(
                    () => service.RegisterApplicationAsync(null));
                await Assert.ThrowsAsync<ArgumentNullException>(
                    () => service.GetApplicationAsync(null, false));
                await Assert.ThrowsAsync<ArgumentNullException>(
                    () => service.GetApplicationAsync("", false));
                await Assert.ThrowsAsync<ResourceNotFoundException>(
                    () => service.GetApplicationAsync("abc", false));
                await Assert.ThrowsAsync<ResourceNotFoundException>(
                    () => service.GetApplicationAsync(Guid.NewGuid().ToString(), false));
            }
        }

        [Fact]
        public void DisableEnableApplication() {
            CreateAppFixtures(out var site, out var super, out var apps, out var devices);

            using (var mock = AutoMock.GetLoose()) {
                var hub = IoTHubServices.Create(devices);
                mock.Provide<IIoTHubTwinServices>(hub);
                mock.Provide<IApplicationRepository, ApplicationTwins>();
                IApplicationRegistry service = mock.Create<ApplicationRegistry>();

                var app = apps.First();
                service.DisableApplicationAsync(app.ApplicationId, null).Wait();
                var registration = service.GetApplicationAsync(app.ApplicationId, false).Result;
                Assert.NotNull(registration.Application.NotSeenSince);
                service.EnableApplicationAsync(app.ApplicationId, null).Wait();
                registration = service.GetApplicationAsync(app.ApplicationId, false).Result;
                Assert.Null(registration.Application.NotSeenSince);
            }
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="site"></param>
        /// <param name="super"></param>
        /// <param name="apps"></param>
        /// <param name="devices"></param>
        private static void CreateAppFixtures(out string site, out string super,
            out List<ApplicationInfoModel> apps, out List<(DeviceTwinModel, DeviceModel)> devices,
            bool noSite = false) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var sitex = site = noSite ? null : fix.Create<string>();
            var superx = super = fix.Create<string>();
            apps = fix
                .Build<ApplicationInfoModel>()
                .Without(x => x.NotSeenSince)
                .With(x => x.SiteId, sitex)
                .With(x => x.SupervisorId, superx)
                .CreateMany(10)
                .ToList();
            apps.ForEach(x => x.ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                 sitex, x.ApplicationUri, x.ApplicationType));
            devices = apps
                .Select(a => a.ToApplicationRegistration())
                .Select(a => a.ToDeviceTwin())
                .Select(t => (t, new DeviceModel { Id = t.Id }))
                .ToList();
        }
    }
}
