// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Moq;
    using Autofac.Extras.Moq;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Sdk;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    public class PublisherJobClientTests {

        [Fact]
        public async Task StartPublishTest1Async() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result);
                Assert.Single(list.Items);
                Assert.Null(list.ContinuationToken);
                Assert.Equal("i=2258", list.Items.Single().NodeId);
                Assert.Equal(TimeSpan.FromSeconds(2), list.Items.Single().PublishingInterval);
                Assert.Equal(TimeSpan.FromSeconds(1), list.Items.Single().SamplingInterval);
            }
        }

        [Fact]
        public async Task StartPublishTest2Async() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258"
                    }
                });

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result);
                Assert.Single(list.Items);
                Assert.Null(list.ContinuationToken);
                Assert.Equal("i=2258", list.Items.Single().NodeId);
                Assert.Null(list.Items.Single().PublishingInterval);
                Assert.Null(list.Items.Single().SamplingInterval);
            }
        }

        [Fact]
        public async Task StartStopPublishTestAsync() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });

                var result2 = await service.NodePublishStopAsync("endpoint1", new PublishStopRequestModel {
                    NodeId = "i=2258"
                });

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result);
                Assert.NotNull(result2);
                Assert.Empty(list.Items);
                Assert.Null(list.ContinuationToken);
            }
        }

        [Fact]
        public async Task StartTwicePublishTest1Async() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });
                result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });


                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result);
                Assert.Single(list.Items);
                Assert.Null(list.ContinuationToken);
                Assert.Equal("i=2258", list.Items.Single().NodeId);
                Assert.Equal(TimeSpan.FromSeconds(2), list.Items.Single().PublishingInterval);
                Assert.Equal(TimeSpan.FromSeconds(1), list.Items.Single().SamplingInterval);
            }
        }

        [Fact]
        public async Task StartTwicePublishTest2Async() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });
                result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2259",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });


                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result);
                Assert.Equal(2, list.Items.Count);
                Assert.Null(list.ContinuationToken);
                Assert.Equal(TimeSpan.FromSeconds(2), list.Items.First().PublishingInterval);
                Assert.Equal(TimeSpan.FromSeconds(1), list.Items.First().SamplingInterval);
            }
        }

        [Fact]
        public async Task StartTwicePublishTest3Async() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });
                result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(3),
                        SamplingInterval = TimeSpan.FromSeconds(2)
                    }
                });

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result);
                Assert.Single(list.Items);
                Assert.Null(list.ContinuationToken);
                Assert.Equal("i=2258", list.Items.Single().NodeId);
                Assert.Equal(TimeSpan.FromSeconds(3), list.Items.Single().PublishingInterval);
                Assert.Equal(TimeSpan.FromSeconds(2), list.Items.Single().SamplingInterval);
            }
        }

        [Fact]
        public async Task StartStopMultiplePublishTestAsync() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, (v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                });

                IPublishServices<string> service = mock.Create<PublisherJobClient>();

                // Run
                for (var i = 0; i < 100; i++) {
                    var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                        Item = new PublishedItemModel {
                            NodeId = "i=" + (i + 1000),
                            PublishingInterval = TimeSpan.FromSeconds(i),
                            SamplingInterval = TimeSpan.FromSeconds(i+1)
                        }
                    });
                    Assert.NotNull(result);
                }
                for (var i = 0; i < 50; i++) {
                    var result = await service.NodePublishStopAsync("endpoint1", new PublishStopRequestModel {
                        NodeId = "i=" + (i + 1000)
                    });
                    Assert.NotNull(result);
                }

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.Equal(50, list.Items.Count);
                Assert.Null(list.ContinuationToken);

                // Run
                for (var i = 0; i < 100; i++) {
                    var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                        Item = new PublishedItemModel {
                            NodeId = "i=" + (i + 2000),
                            PublishingInterval = TimeSpan.FromSeconds(i),
                            SamplingInterval = TimeSpan.FromSeconds(i + 1)
                        }
                    });
                    Assert.NotNull(result);
                }
                for (var i = 0; i < 50; i++) {
                    var result = await service.NodePublishStopAsync("endpoint1", new PublishStopRequestModel {
                        NodeId = "i=" + (i + 2000)
                    });
                    Assert.NotNull(result);
                }

                list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.Equal(100, list.Items.Count);
                Assert.Null(list.ContinuationToken);
            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="provider"></param>
        private static void Setup(AutoMock mock, Func<IEnumerable<IDocumentInfo<JObject>>,
            string, IEnumerable<IDocumentInfo<JObject>>> provider) {
            mock.Provide<IQueryEngine>(new QueryEngineAdapter(provider));
            mock.Provide<IDatabaseServer, MemoryDatabase>();
            mock.Provide<IJobDatabaseConfig, MockConfig>();
            mock.Provide<IJobRepository, JobDatabase>();
            mock.Provide<IJobScheduler, DefaultJobService>();
            mock.Provide<IJobSerializer, PublisherJobSerializer>();
            mock.Mock<IEndpointRegistry>()
                .Setup(e => e.GetEndpointAsync(It.IsAny<string>(), false, CancellationToken.None))
                .Returns(Task.FromResult(new EndpointInfoModel {
                    Registration = new EndpointRegistrationModel {
                        EndpointUrl = "fakeurl",
                        Id = "endpoint1"
                    }
                }));
            mock.Provide<IPublishServices<string>, PublisherJobClient>();
        }

        /// <summary>
        /// Mock
        /// </summary>
        public class MockConfig : IJobDatabaseConfig {
            public string ContainerName => "Test";
            public string DatabaseName => "Test";
        }

        public class MockRegistry : IEndpointRegistry {
            public Task ActivateEndpointAsync(string endpointId, RegistryOperationContextModel context = null, CancellationToken ct = default) {
                throw new NotImplementedException();
            }

            public Task DeactivateEndpointAsync(string endpointId, RegistryOperationContextModel context = null, CancellationToken ct = default) {
                throw new NotImplementedException();
            }

            public Task<EndpointInfoModel> GetEndpointAsync(string endpointId, bool onlyServerState = false, CancellationToken ct = default) {
                throw new NotImplementedException();
            }

            public Task<EndpointInfoListModel> ListEndpointsAsync(string continuation, bool onlyServerState = false, int? pageSize = null, CancellationToken ct = default) {
                throw new NotImplementedException();
            }

            public Task<EndpointInfoListModel> QueryEndpointsAsync(EndpointRegistrationQueryModel query, bool onlyServerState = false, int? pageSize = null, CancellationToken ct = default) {
                throw new NotImplementedException();
            }
        }

#if FALSE

        [Fact]
        public void GetSupervisorThatExists() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var result = service.GetSupervisorAsync(supervisors.First().Id, false).Result;

                // Assert
                Assert.True(result.IsSameAs(supervisors.First()));
            }
        }

        [Fact]
        public void ListAllSupervisors() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.ListSupervisorsAsync(null, false, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void ListAllSupervisorsUsingQuery() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(null, false, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QuerySupervisorsByDiscoveryMode() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel {
                    Discovery = DiscoveryMode.Network
                }, false, null).Result;

                // Assert
                Assert.True(records.Items.Count == supervisors.Count(x => x.Discovery == DiscoveryMode.Network));
            }
        }

        [Fact]
        public void QuerySupervisorsBySiteId() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel {
                    SiteId = site
                }, false, null).Result;

                // Assert
                Assert.True(supervisors.IsSameAs(records.Items));
            }
        }

        [Fact]
        public void QuerySupervisorsByNoneExistantSiteId() {
            CreateSupervisorFixtures(out var site, out var supervisors, out var modules, true);

            using (var mock = AutoMock.GetLoose()) {
                mock.Provide<IIoTHubTwinServices>(IoTHubServices.Create(modules));
                ISupervisorRegistry service = mock.Create<SupervisorRegistry>();

                // Run
                var records = service.QuerySupervisorsAsync(new SupervisorQueryModel {
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
        private static void CreateSupervisorFixtures(out string site,
            out List<SupervisorModel> supervisors, out List<(DeviceTwinModel, DeviceModel)> modules,
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
                .Select(a => a.ToSupervisorRegistration())
                .Select(a => a.ToDeviceTwin())
                .Select(t => {
                    t.Properties.Reported = new Dictionary<string, JToken> {
                        [TwinProperty.Type] = "supervisor"
                    };
                    return t;
                })
                .Select(t => (t, new DeviceModel { Id = t.Id, ModuleId = t.ModuleId }))
                .ToList();
        }
#endif

    }
}
