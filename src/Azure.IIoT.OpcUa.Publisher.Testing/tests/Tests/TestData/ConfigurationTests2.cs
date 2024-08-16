// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using DeterministicAlarms.Configuration;
    using Furly.Exceptions;
    using Moq;
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
            _service= services;
            _connection = connection;
            _publishedNodesServices = new Mock<IPublishedNodesServices>();
            _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), default))
                .Returns(Task.CompletedTask);
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    StopAtFirstFoundInstance = false,
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    StopAtFirstFoundInstance = false,
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(623, result.Result.OpcNodes.Count);
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(73, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(74, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(920, result.Result.OpcNodes.Count);
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    MaxDepth = 1,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(8, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(77 + 74 + 25, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(1, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(1 + 77 + 74, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ConfigureFromVariableTypesTest1Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.PropertyType.ToString() }
            };
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var r = Assert.Single(results);
            Assert.Null(r.ErrorInfo);
            Assert.NotNull(r.Result);
            Assert.Equal(Opc.Ua.VariableTypeIds.PropertyType + "/Variables", r.Result.DataSetWriterId);
            Assert.NotNull(r.Result.OpcNodes);
            Assert.Equal(675, r.Result.OpcNodes.Count);
        }

        public async Task ConfigureFromVariableTypesTest2Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.DataItemType.ToString() }
            };
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var r = Assert.Single(results);
            Assert.Null(r.ErrorInfo);
            Assert.NotNull(r.Result);
            Assert.Equal(Opc.Ua.VariableTypeIds.DataItemType + "/Variables", r.Result.DataSetWriterId);
            Assert.NotNull(r.Result.OpcNodes);
            Assert.Equal(96, r.Result.OpcNodes.Count);
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
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    StopAtFirstFoundInstance = false,
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
                Assert.EndsWith("/Variables", r.Result.DataSetWriterId, StringComparison.InvariantCulture);
                total += r.Result.OpcNodes.Count;
            });
            Assert.Equal(96 + 675, total);
        }

        public async Task ConfigureFromObjectWithNoObjectsTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10791" }
            };
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
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
        }

        public async Task ConfigureFromObjectWithNoObjectsTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10791" }
            };
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = true,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false

                }, ct).ToListAsync(ct).ConfigureAwait(false);

            // Discard errors -> no errors
            Assert.Empty(results);
        }

        public async Task ConfigureFromEmptyEntryTest1Async(CancellationToken ct = default)
        {
            // Nothing should be returned when an empty entry passed
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = Array.Empty<OpcNodeModel>();
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
        }

        public async Task ConfigureFromEmptyEntryTest2Async(CancellationToken ct = default)
        {
            // Nothing should be returned when an empty entry passed
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = Array.Empty<OpcNodeModel>();
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
        }

        public async Task ConfigureFromBadNodeIdTest1Async(CancellationToken ct = default)
        {
            // Entry with bad node id should return error
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "s=bad" }
            };
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal("s=bad", Assert.Single(result.Result.OpcNodes).Id);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
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
            var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await _service(_publishedNodesServices.Object).ExpandAsync(
                entry, new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    StopAtFirstFoundInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.True(ex is MethodCallStatusException or BadRequestException);
        }

        public async Task ConfigureFromBadNodeIdTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            // Method or view node id passed results in not supported error.
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = MethodIds.Server_GetMonitoredItems.ToString() }
            };
            var results = await _service(_publishedNodesServices.Object).ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    StopAtFirstFoundInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.NotNull(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(MethodIds.Server_GetMonitoredItems.ToString(), Assert.Single(result.Result.OpcNodes).Id);
            Assert.Equal(StatusCodes.BadNotSupported, result.ErrorInfo.StatusCode);
        }

        //
        //  7. Object and maxdepth 0 -> max depth 1
        //  7b.Object and stop first found -> organizes is used
        //
        //  8. Object type with stop first found set uses organizes
        //
        //
        //  Case config:
        //  Mock configuration writer "CreateOrUpdateDataSetWriterEntryAsync"
        //
        //  1. Duplicate writer will override
        //  2. Save error will return error for entry
        //  3. Only good entries passed.

        private readonly ConnectionModel _connection;
        private readonly Mock<IPublishedNodesServices> _publishedNodesServices;
        private readonly Func<IPublishedNodesServices, IConfigurationServices> _service;
    }
}
