// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Exceptions;
    using Moq;
    using Moq.Language.Flow;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ConfigurationTests2
    {
        /// <summary>
        /// Create configuration tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public ConfigurationTests2(Func<IPublishedNodesServices, IConfigurationServices> services,
            ConnectionModel connection)
        {
            _service = services;
            _connection = connection;
            _publishedNodesServices = new Mock<IPublishedNodesServices>();
            _createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public async Task ConfigureFromObjectErrorTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };

            // Save error will return error for entry
            var createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                .Throws<BadRequestException>();
            createCall.Verifiable(Times.Exactly(25));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(25, results.Count);
            Assert.All(results, r =>
            {
                Assert.NotNull(r.ErrorInfo);
                Assert.Equal(StatusCodes.BadInvalidArgument, r.ErrorInfo.StatusCode);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectErrorTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };

            // Save error will return error for entry
            var createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                .Throws<BadRequestException>();
            createCall.Verifiable(Times.Exactly(25));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = true,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectErrorTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };

            // Save error will return error for entry
            var createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                .Throws<BadRequestException>();
            createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);

            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(StatusCodes.BadInvalidArgument, result.ErrorInfo.StatusCode);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(623, result.Result.OpcNodes.Count);

            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectWithBrowsePathTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157",
                    BrowsePath = new[]
                    {
                        "http://test.org/UA/Data/#Static"
                    }
                }
            };
            _createCall.Verifiable(Times.Exactly(12));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(12, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectWithBrowsePathTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157",
                    BrowsePath = new[]
                    {
                        "http://test.org/UA/Data/#Static"
                    }
                }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(300, result.Result.OpcNodes.Count);
            Assert.All(result.Result.OpcNodes, result =>
            {
                Assert.NotNull(result.DataSetFieldId);
                Assert.NotNull(result.Id);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };
            _createCall.Verifiable(Times.Exactly(25));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(25, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(623, result.Result.OpcNodes.Count);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromServerObjectTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            _createCall.Verifiable(Times.Exactly(77));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(77, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromServerObjectTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            _createCall.Verifiable(Times.Exactly(78));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(78, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromServerObjectTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(940, result.Result.OpcNodes.Count);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromServerObjectTest4Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            _createCall.Verifiable(Times.Exactly(9));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    MaxDepth = 1,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(9, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromServerObjectTest5Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    MaxDepth = 0,
                    MaxLevelsToExpand = 1,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(7, result.Result.OpcNodes.Count);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromBaseObjectTypeTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString()
                }
            };
            _createCall.Verifiable(Times.Exactly(81));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    FlattenTypeInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(81, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromBaseObjectTypeTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString()
                }
            };
            _createCall.Verifiable(Times.Exactly(5));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    FlattenTypeInstance = true,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(5, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromBaseObjectsAndObjectTypesTestAsync(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString(),
                    DataSetFieldId = "type"
                },
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString(),
                    DataSetFieldId = "object"
                },
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157",
                    DataSetFieldId = "data"
                }
            };
            _createCall.Verifiable(Times.Exactly(184));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    FlattenTypeInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(184, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromVariablesTest1Async(CancellationToken ct = default)
        {
            // Test only variables as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10216" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10217" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10218" }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(6, result.Result.OpcNodes.Count);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromVariablesAndObjectsTest1Async(CancellationToken ct = default)
        {
            // Test mixing variables and objects in an entry to expand
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10217" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10216" },
                new OpcNodeModel { Id = Opc.Ua.ObjectIds.Server.ToString() },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10218" },
                new OpcNodeModel { Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString() }
            };
            _createCall.Verifiable(Times.Exactly(160));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    FlattenTypeInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(160, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromVariableTypesTest1Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.PropertyType.ToString() }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.Equal(Opc.Ua.VariableTypeIds.PropertyType + "/PropertyType", result.Result.DataSetWriterId);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(697, result.Result.OpcNodes.Count);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromVariableTypesTest2Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.DataItemType.ToString() }
            };
            _createCall.Verifiable(Times.Once);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.Equal(Opc.Ua.VariableTypeIds.DataItemType + "/DataItemType", result.Result.DataSetWriterId);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(96, result.Result.OpcNodes.Count);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromVariableTypesTest3Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.PropertyType.ToString() },
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.DataItemType.ToString() }
            };
            _createCall.Verifiable(Times.Exactly(2));
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(2, results.Count);
            var total = 0;
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                total += r.Result.OpcNodes.Count;
            });
            Assert.Equal(793, total);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectWithNoObjectsTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10791" }
            };
            _createCall.Verifiable(Times.Never);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            //  An object node that has no objects and is excluded should result
            //  in a service result model with "No objects resolved"
            var result = Assert.Single(results);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal("http://test.org/UA/Data/#i=10791",
                Assert.Single(result.Result.OpcNodes).Id);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal("No objects resolved.", result.ErrorInfo.ErrorMessage);
            Assert.Equal(StatusCodes.BadNotFound, result.ErrorInfo.StatusCode);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromObjectWithNoObjectsTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10791" }
            };
            _createCall.Verifiable(Times.Never);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = true,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            // Discard errors -> no errors
            Assert.Empty(results);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromEmptyEntryTest1Async(CancellationToken ct = default)
        {
            // Nothing should be returned when an empty entry passed
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = Array.Empty<OpcNodeModel>();
            _createCall.Verifiable(Times.Never);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromEmptyEntryTest2Async(CancellationToken ct = default)
        {
            // Nothing should be returned when an empty entry passed
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = Array.Empty<OpcNodeModel>();
            _createCall.Verifiable(Times.Never);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromBadNodeIdTest1Async(CancellationToken ct = default)
        {
            // Entry with bad node id should return error
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "s=bad" }
            };
            _createCall.Verifiable(Times.Never);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal("s=bad", Assert.Single(result.Result.OpcNodes).Id);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromBadNodeIdTest2Async(CancellationToken ct = default)
        {
            //  An entry with multiple nodes and duplicate field ids should return an error
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10157", DataSetFieldId = "test" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10158", DataSetFieldId = "test" }
            };
            _createCall.Verifiable(Times.Never);
            var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(
                entry, new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.True(ex is MethodCallStatusException or BadRequestException);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        public async Task ConfigureFromBadNodeIdTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            // Method or view node id passed results in not supported error.
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = MethodIds.Server_GetMonitoredItems.ToString() }
            };
            _createCall.Verifiable(Times.Never);
            var results = await _service(_publishedNodesServices.Object).CreateOrUpdateAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.NotNull(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(MethodIds.Server_GetMonitoredItems.ToString(), Assert.Single(result.Result.OpcNodes).Id);
            Assert.Equal(StatusCodes.BadNotSupported, result.ErrorInfo.StatusCode);
            _publishedNodesServices.Verify();
            _publishedNodesServices.VerifyNoOtherCalls();
        }

        private readonly ConnectionModel _connection;
        private readonly Mock<IPublishedNodesServices> _publishedNodesServices;
        private readonly IReturnsResult<IPublishedNodesServices> _createCall;
        private readonly Func<IPublishedNodesServices, IConfigurationServices> _service;
    }
}
