// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Moq;
    using Autofac.Extras.Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Sdk;
    using Autofac;

    public class PublisherJobClientTests {

        [Fact]
        public async Task StartPublishTest1Async() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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

            using (var mock = Setup((v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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

            using (var mock = Setup((v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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

            using (var mock = Setup((v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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

            using (var mock = Setup((v, q) => {
                    throw new AssertActualExpectedException(null, q, "Query");
                })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

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
        private static AutoMock Setup(Func<IEnumerable<IDocumentInfo<VariantValue>>,
            string, IEnumerable<IDocumentInfo<VariantValue>>> provider) {
            var mock = AutoMock.GetLoose(builder => {

                builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonSoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(new QueryEngineAdapter(provider)).As<IQueryEngine>();
                builder.RegisterType<MemoryDatabase>().SingleInstance().As<IDatabaseServer>();
                builder.RegisterType<MockConfig>().As<IJobDatabaseConfig>();
                builder.RegisterType<JobDatabase>().As<IJobRepository>();
                builder.RegisterType<DefaultJobService>().As<IJobScheduler>();
                builder.RegisterType<PublisherJobSerializer>().As<IJobSerializer>();
                var registry = new Mock<IEndpointRegistry>();
                registry
                    .Setup(e => e.GetEndpointAsync(It.IsAny<string>(), false, CancellationToken.None))
                    .Returns(Task.FromResult(new EndpointInfoModel {
                        Registration = new EndpointRegistrationModel {
                            EndpointUrl = "fakeurl",
                            Id = "endpoint1"
                        }
                    }));
                builder.RegisterMock(registry);
                builder.RegisterType<PublisherJobService>().As<IPublishServices<string>>();
            });
            return mock;
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

            public Task<X509CertificateChainModel> GetEndpointCertificateAsync(string endpointId, CancellationToken ct = default) {
                throw new NotImplementedException();
            }

            public Task<EndpointInfoListModel> ListEndpointsAsync(string continuation, bool onlyServerState = false, int? pageSize = null, CancellationToken ct = default) {
                throw new NotImplementedException();
            }

            public Task<EndpointInfoListModel> QueryEndpointsAsync(EndpointRegistrationQueryModel query, bool onlyServerState = false, int? pageSize = null, CancellationToken ct = default) {
                throw new NotImplementedException();
            }
        }
    }
}
