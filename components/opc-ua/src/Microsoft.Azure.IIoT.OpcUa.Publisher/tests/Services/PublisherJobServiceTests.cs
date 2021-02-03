// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Services {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
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
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Configuration;
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

    public class PublisherJobServiceTests {

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
        public async Task StartPublishSameNodeWithDifferentCredentialsOnlyHasLastInListAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258"
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "abcdefg"
                        }
                    }
                });
                result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258"
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "123456"
                        }
                    }
                });
                result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258"
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "asdfasdf"
                        }
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
            }
        }

        [Fact]
        public async Task StartandStopPublishNodeWithDifferentCredentialsHasNoItemsInListAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result1 = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258"
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "abcdefg"
                        }
                    }
                });
                var result2 = await service.NodePublishStopAsync("endpoint1", new PublishStopRequestModel {
                    NodeId = "i=2258",
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "123456"
                        }
                    }
                });

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result1);
                Assert.NotNull(result2);
                Assert.Empty(list.Items);
                Assert.Null(list.ContinuationToken);
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

        [Fact]
        public async Task PublishServicesConfigTest1Async() {

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

                var jobRepository = mock.Container.Resolve<IJobRepository>();
                var jobSerializer = mock.Container.Resolve<IJobSerializer>();
                var jobInfoModel = await jobRepository.GetAsync("endpoint1");

                var publishJob = (WriterGroupJobModel)jobSerializer
                    .DeserializeJobConfiguration(
                        jobInfoModel.JobConfiguration,
                        jobInfoModel.JobConfigurationType
                    );

                // Check that defaults are set.
                Assert.Equal(TimeSpan.FromMilliseconds(500), publishJob.Engine.BatchTriggerInterval);
                Assert.Equal(50, publishJob.Engine.BatchSize);
                Assert.Equal(4096, publishJob.Engine.MaxEgressMessageQueue);
                Assert.Equal(MessagingMode.Samples, publishJob.MessagingMode);
                Assert.Equal(MessageEncoding.Json, publishJob.WriterGroup.MessageType);
                // hardcoded
                Assert.Equal(TimeSpan.FromSeconds(60), publishJob.Engine.DiagnosticsInterval);
                Assert.Equal(0, publishJob.Engine.MaxMessageSize);

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
        public async Task PublishServicesConfigTest2Async() {

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["PCS_DEFAULT_PUBLISH_JOB_BATCH_INTERVAL"] = "00:00:19";
            configuration["PCS_DEFAULT_PUBLISH_JOB_BATCH_SIZE"] = "765";
            configuration["PCS_DEFAULT_PUBLISH_MAX_EGRESS_MESSAGE_QUEUE"] = "512";
            configuration["PCS_DEFAULT_PUBLISH_MESSAGING_MODE"] = "PubSub";
            configuration["PCS_DEFAULT_PUBLISH_MESSAGE_ENCODING"] = "Uadp";

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            }, configuration)) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });

                var jobRepository = mock.Container.Resolve<IJobRepository>();
                var jobSerializer = mock.Container.Resolve<IJobSerializer>();
                var jobInfoModel = await jobRepository.GetAsync("endpoint1");

                var publishJob = (WriterGroupJobModel)jobSerializer
                    .DeserializeJobConfiguration(
                        jobInfoModel.JobConfiguration,
                        jobInfoModel.JobConfigurationType
                    );

                // Check that configured values are applied.
                Assert.Equal(TimeSpan.FromSeconds(19), publishJob.Engine.BatchTriggerInterval);
                Assert.Equal(765, publishJob.Engine.BatchSize);
                Assert.Equal(512, publishJob.Engine.MaxEgressMessageQueue);
                Assert.Equal(MessagingMode.PubSub, publishJob.MessagingMode);
                Assert.Equal(MessageEncoding.Uadp, publishJob.WriterGroup.MessageType);
                // hardcoded
                Assert.Equal(TimeSpan.FromSeconds(60), publishJob.Engine.DiagnosticsInterval);
                Assert.Equal(0, publishJob.Engine.MaxMessageSize);

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
        public async Task PublishServicesConfigTest3Async() {

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["Publisher:DefaultBatchTriggerInterval"] = "00:01:39";
            configuration["Publisher:DefaultBatchSize"] = "777";
            configuration["Publisher:DefaultMaxEgressMessageQueue"] = "20";
            configuration["Publisher:DefaultMessagingMode"] = "Samples";
            configuration["Publisher:DefaultMessageEncoding"] = "Json";

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            }, configuration)) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });

                var jobRepository = mock.Container.Resolve<IJobRepository>();
                var jobSerializer = mock.Container.Resolve<IJobSerializer>();
                var jobInfoModel = await jobRepository.GetAsync("endpoint1");

                var publishJob = (WriterGroupJobModel)jobSerializer
                    .DeserializeJobConfiguration(
                        jobInfoModel.JobConfiguration,
                        jobInfoModel.JobConfigurationType
                    );

                // Check that configured values are applied.
                Assert.Equal(TimeSpan.FromSeconds(99), publishJob.Engine.BatchTriggerInterval);
                Assert.Equal(777, publishJob.Engine.BatchSize);
                Assert.Equal(20, publishJob.Engine.MaxEgressMessageQueue);
                Assert.Equal(MessagingMode.Samples, publishJob.MessagingMode);
                Assert.Equal(MessageEncoding.Json, publishJob.WriterGroup.MessageType);
                // hardcoded
                Assert.Equal(TimeSpan.FromSeconds(60), publishJob.Engine.DiagnosticsInterval);
                Assert.Equal(0, publishJob.Engine.MaxMessageSize);

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
        public async Task PublishServicesConfigTest4Async() {

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["Publisher:DefaultBatchTriggerInterval"] = "00:01:39";
            configuration["Publisher:DefaultBatchSize"] = "777";
            // Deprecated, test backwards compat.
            configuration[PcsVariable.PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES] = "1234";
            configuration["Publisher:DefaultMessagingMode"] = "Samples";
            configuration["Publisher:DefaultMessageEncoding"] = "Json";

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            }, configuration)) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });

                var jobRepository = mock.Container.Resolve<IJobRepository>();
                var jobSerializer = mock.Container.Resolve<IJobSerializer>();
                var jobInfoModel = await jobRepository.GetAsync("endpoint1");

                var publishJob = (WriterGroupJobModel)jobSerializer
                    .DeserializeJobConfiguration(
                        jobInfoModel.JobConfiguration,
                        jobInfoModel.JobConfigurationType
                    );

                // Check that configured values are applied.
                Assert.Equal(TimeSpan.FromSeconds(99), publishJob.Engine.BatchTriggerInterval);
                Assert.Equal(777, publishJob.Engine.BatchSize);
                Assert.Equal(1234, publishJob.Engine.MaxEgressMessageQueue);
                Assert.Equal(MessagingMode.Samples, publishJob.MessagingMode);
                Assert.Equal(MessageEncoding.Json, publishJob.WriterGroup.MessageType);
                // hardcoded
                Assert.Equal(TimeSpan.FromSeconds(60), publishJob.Engine.DiagnosticsInterval);
                Assert.Equal(0, publishJob.Engine.MaxMessageSize);

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
        public async Task PublishServicesConfigTest5Async() {

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["Publisher:DefaultBatchTriggerInterval"] = "00:01:39";
            configuration["Publisher:DefaultBatchSize"] = "777";
            // Deprecated, test backwards compat.
            configuration[PcsVariable.PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES] = "1234";
            // Test new option overrides deprecated one.
            configuration[PcsVariable.PCS_DEFAULT_PUBLISH_MAX_EGRESS_MESSAGE_QUEUE] = "4321";
            configuration["Publisher:DefaultMessagingMode"] = "Samples";
            configuration["Publisher:DefaultMessageEncoding"] = "Json";

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            }, configuration)) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishStartAsync("endpoint1", new PublishStartRequestModel {
                    Item = new PublishedItemModel {
                        NodeId = "i=2258",
                        PublishingInterval = TimeSpan.FromSeconds(2),
                        SamplingInterval = TimeSpan.FromSeconds(1)
                    }
                });

                var jobRepository = mock.Container.Resolve<IJobRepository>();
                var jobSerializer = mock.Container.Resolve<IJobSerializer>();
                var jobInfoModel = await jobRepository.GetAsync("endpoint1");

                var publishJob = (WriterGroupJobModel)jobSerializer
                    .DeserializeJobConfiguration(
                        jobInfoModel.JobConfiguration,
                        jobInfoModel.JobConfigurationType
                    );

                // Check that configured values are applied.
                Assert.Equal(TimeSpan.FromSeconds(99), publishJob.Engine.BatchTriggerInterval);
                Assert.Equal(777, publishJob.Engine.BatchSize);
                Assert.Equal(4321, publishJob.Engine.MaxEgressMessageQueue);
                Assert.Equal(MessagingMode.Samples, publishJob.MessagingMode);
                Assert.Equal(MessageEncoding.Json, publishJob.WriterGroup.MessageType);
                // hardcoded
                Assert.Equal(TimeSpan.FromSeconds(60), publishJob.Engine.DiagnosticsInterval);
                Assert.Equal(0, publishJob.Engine.MaxMessageSize);

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
        public async Task BulkPublishTest1Async() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
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
        public async Task BulkPublishTest2Async() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                        }
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
        public async Task StartStopBulkPublishTestAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
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
        public async Task StartTwiceBulkPublishTest1Async() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
                    }
                });
                result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
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
        public async Task StartTwiceBulkPublishTest2Async() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
                    }
                });
                result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2259",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
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
        public async Task BulkPublishSameNodeWithDifferentCredentialsOnlyHasLastInListAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258"
                        }
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "abcdefg"
                        }
                    }
                });
                result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258"
                        }
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "123456"
                        }
                    }
                });
                result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258"
                        }
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "asdfasdf"
                        }
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
            }
        }

        [Fact]
        public async Task StartandStopBulkPublishNodeWithDifferentCredentialsHasNoItemsInListAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result1 = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258"
                        }
                    },
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "abcdefg"
                        }
                    }
                });
                var result2 = await service.NodePublishStopAsync("endpoint1", new PublishStopRequestModel {
                    NodeId = "i=2258",
                    Header = new RequestHeaderModel {
                        Elevation = new CredentialModel {
                            Type = CredentialType.UserName,
                            Value = "123456"
                        }
                    }
                });

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.NotNull(result1);
                Assert.NotNull(result2);
                Assert.Empty(list.Items);
                Assert.Null(list.ContinuationToken);
            }
        }

        [Fact]
        public async Task BulkTwicePublishTest3Async() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(2),
                            SamplingInterval = TimeSpan.FromSeconds(1)
                        }
                    }
                });
                result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel {
                            NodeId = "i=2258",
                            PublishingInterval = TimeSpan.FromSeconds(3),
                            SamplingInterval = TimeSpan.FromSeconds(2)
                        }
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
        public async Task StartStopMultipleBulkPublishTestAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                // Run
                for (var i = 0; i < 100; i++) {
                    var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                        NodesToAdd = new List<PublishedItemModel> {
                            new PublishedItemModel {
                                NodeId = "i=" + (i + 1000),
                                PublishingInterval = TimeSpan.FromSeconds(i),
                                SamplingInterval = TimeSpan.FromSeconds(i + 1)
                            }
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
                    var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                        NodesToAdd = new List<PublishedItemModel> {
                            new PublishedItemModel {
                                NodeId = "i=" + (i + 2000),
                                PublishingInterval = TimeSpan.FromSeconds(i),
                                SamplingInterval = TimeSpan.FromSeconds(i + 1)
                            }
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


        [Fact]
        public async Task AddRemoveMultipleBulkPublishTestAsync() {

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                var nodesToAdd = new List<PublishedItemModel>();
                var nodesToRemove = new List<string>();
                for (var i = 0; i < 100; i++) {
                    nodesToAdd.Add(new PublishedItemModel {
                        NodeId = "i=" + (i + 1000),
                        PublishingInterval = TimeSpan.FromSeconds(i),
                        SamplingInterval = TimeSpan.FromSeconds(i + 1)
                    });
                    if (i % 2 == 0) {
                        nodesToRemove.Add("i=" + (i + 1000));
                    }
                }

                var result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToAdd = nodesToAdd
                });

                Assert.NotNull(result);
                var nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToAddErrors);
                var nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToRemoveErrors);

                result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                    NodesToRemove = nodesToRemove
                });

                Assert.NotNull(result);
                nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToAddErrors);
                nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToRemoveErrors);

                var list = await service.NodePublishListAsync("endpoint1", new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Assert
                Assert.NotNull(list);
                Assert.Equal(50, list.Items.Count);
                Assert.Null(list.ContinuationToken);

                // Run
                for (var i = 0; i < 100; i++) {
                    result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                        NodesToAdd = new List<PublishedItemModel> {
                            new PublishedItemModel {
                                NodeId = "i=" + (i + 2000),
                                PublishingInterval = TimeSpan.FromSeconds(i),
                                SamplingInterval = TimeSpan.FromSeconds(i + 1)
                            }
                        }
                    });

                    Assert.NotNull(result);
                    nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                    Assert.Equal(0, nodesToAddErrors);
                    nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                    Assert.Equal(0, nodesToRemoveErrors);
                }

                for (var i = 0; i < 50; i++) {
                    result = await service.NodePublishBulkAsync("endpoint1", new PublishBulkRequestModel {
                        NodesToRemove = new List<string> {
                                "i=" + (i + 2000)
                            }
                    });

                    Assert.NotNull(result);
                    nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                    Assert.Equal(0, nodesToAddErrors);
                    nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                    Assert.Equal(0, nodesToRemoveErrors);
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

        [Fact]
        public async Task NodePublishStopNotExistingTest1Async() {
            // We will try to stop publishing a node that is not present in any jobs.
            // The call itself must throw ResourceNotFoundException exception.

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                var nodeId = "i=11211";
                async Task<PublishStopResultModel> action() => await service.NodePublishStopAsync(
                    "endpoint1",
                    new PublishStopRequestModel {
                        NodeId = nodeId
                    }
                );

                var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(action);
                Assert.Contains($"Job does not contain node id: {nodeId}", ex.Message);
            }
        }

        [Fact]
        public async Task NodePublishStopNotExistingTest2Async() {
            // We will try to start publishing a node and then stop publishing it twice.
            // The second call must throw ResourceNotFoundException exception.

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                var endpoint = "testEndpoint";
                var nodeId = "i=11211";

                await service.NodePublishStartAsync(
                    endpoint,
                    new PublishStartRequestModel {
                        Item = new PublishedItemModel {
                            NodeId = nodeId
                        }
                    }
                );

                async Task<PublishStopResultModel> action() => await service.NodePublishStopAsync(
                    endpoint,
                    new PublishStopRequestModel {
                        NodeId = nodeId
                    }
                );

                // First call to remove the node.
                await action();

                // Second call to remove the node. Should throw ResourceNotFoundException.
                var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(action);
                Assert.Contains($"Job does not contain node id: {nodeId}", ex.Message);
            }
        }

        [Fact]
        public async Task BulkPublishRemoveNodesTestAsync() {
            // Here we will check that correct errors are reported back when using
            // bulk publishing API to unpublish nodes that are not present in any jobs.

            using (var mock = Setup((v, q) => {
                throw new AssertActualExpectedException(null, q, "Query");
            })) {

                IPublishServices<string> service = mock.Create<PublisherJobService>();

                var endpoint = "testEndpoint";
                var node0 = "i=22580_0";
                var node1 = "i=22580_1";
                var node2 = "i=22580_2";

                var result = await service.NodePublishBulkAsync(endpoint, new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel { NodeId = node0 },
                        new PublishedItemModel { NodeId = node1 },
                    },
                    NodesToRemove = new List<string> {
                        node2
                    }
                });

                // Check NodePublishBulkAsync() call result
                Assert.NotNull(result);
                var nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToAddErrors);
                var nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                Assert.Equal(1, nodesToRemoveErrors);
                var nodesToRemoveNotFound = result.NodesToRemove.Count(el => el.Value.StatusCode == 404);
                Assert.Equal(1, nodesToRemoveErrors);

                var publishedNodesList = await service.NodePublishListAsync(endpoint, new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Check list of published nodes for the endpoint after NodePublishBulkAsync() call.
                Assert.NotNull(publishedNodesList);
                Assert.Equal(2, publishedNodesList.Items.Count());
                Assert.Equal(node0, publishedNodesList.Items[0].NodeId);
                Assert.Equal(node1, publishedNodesList.Items[1].NodeId);
                Assert.Null(publishedNodesList.ContinuationToken);

                result = await service.NodePublishBulkAsync(endpoint, new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {
                        new PublishedItemModel { NodeId = node0 },
                        new PublishedItemModel { NodeId = node2 },
                    },
                    NodesToRemove = new List<string> {
                        node1
                    }
                });

                // Check NodePublishBulkAsync() call result
                Assert.NotNull(result);
                nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToAddErrors);
                nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToRemoveErrors);

                publishedNodesList = await service.NodePublishListAsync(endpoint, new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Check list of published nodes for the endpoint after NodePublishBulkAsync() call.
                Assert.NotNull(publishedNodesList);
                Assert.Equal(2, publishedNodesList.Items.Count());
                Assert.Equal(node0, publishedNodesList.Items[0].NodeId);
                Assert.Equal(node2, publishedNodesList.Items[1].NodeId);
                Assert.Null(publishedNodesList.ContinuationToken);

                result = await service.NodePublishBulkAsync(endpoint, new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> {},
                    NodesToRemove = new List<string> {
                        node0,
                        node2
                    }
                });

                // Check NodePublishBulkAsync() call result
                Assert.NotNull(result);
                nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToAddErrors);
                nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToRemoveErrors);

                publishedNodesList = await service.NodePublishListAsync(endpoint, new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Check list of published nodes for the endpoint after NodePublishBulkAsync() call.
                Assert.NotNull(publishedNodesList);
                Assert.Empty(publishedNodesList.Items);

                // Let's call unpublish for the same nodes again.
                result = await service.NodePublishBulkAsync(endpoint, new PublishBulkRequestModel {
                    NodesToAdd = new List<PublishedItemModel> { },
                    NodesToRemove = new List<string> {
                        node0,
                        node2
                    }
                });

                // Check NodePublishBulkAsync() call result
                Assert.NotNull(result);
                nodesToAddErrors = result.NodesToAdd.Count(el => el.Value.StatusCode != null);
                Assert.Equal(0, nodesToAddErrors);
                nodesToRemoveErrors = result.NodesToRemove.Count(el => el.Value.StatusCode != null);
                Assert.Equal(2, nodesToRemoveErrors);

                publishedNodesList = await service.NodePublishListAsync(endpoint, new PublishedItemListRequestModel {
                    ContinuationToken = null
                });

                // Check list of published nodes for the endpoint after NodePublishBulkAsync() call.
                Assert.NotNull(publishedNodesList);
                Assert.Empty(publishedNodesList.Items);
            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="configuration"></param>
        private static AutoMock Setup(
            Func<
                IEnumerable<IDocumentInfo<VariantValue>>,
                string,
                IEnumerable<IDocumentInfo<VariantValue>>
            > provider,
            IConfiguration configuration = null
        ) {
            var mock = AutoMock.GetLoose(builder => {
                // Setup configuration
                var conf = configuration ?? new ConfigurationBuilder()
                    .AddInMemoryCollection()
                    .Build();
                builder.RegisterInstance(conf).As<IConfiguration>();

                builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonSoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(new QueryEngineAdapter(provider)).As<IQueryEngine>();
                builder.RegisterType<MemoryDatabase>().SingleInstance().As<IDatabaseServer>();
                builder.RegisterType<MockConfig>().As<IJobDatabaseConfig>();
                builder.RegisterType<PublishServicesConfig>().As<IPublishServicesConfig>();
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
