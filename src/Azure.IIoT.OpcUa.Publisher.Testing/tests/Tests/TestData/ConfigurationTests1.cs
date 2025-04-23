// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Exceptions;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ConfigurationTests1
    {
        /// <summary>
        /// Create configuration tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public ConfigurationTests1(IConfigurationServices services, ConnectionModel connection)
        {
            _service = services;
            _connection = connection;
        }

        public async Task ExpandObjectWithBrowsePathTest1Async(CancellationToken ct = default)
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
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandObjectWithBrowsePathTest2Async(CancellationToken ct = default)
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
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandObjectTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandObjectTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandServerObjectTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(75, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandServerObjectTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(76, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandServerObjectTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
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
            Assert.Equal(921, result.Result.OpcNodes.Count);
        }

        public async Task ExpandServerObjectTest4Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
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

        public async Task ExpandServerObjectTest5Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandBaseObjectTypeTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    FlattenTypeInstance = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(79, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandBaseObjectTypeTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString()
                }
            };
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    FlattenTypeInstance = true,
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
        }

        public async Task ExpandBaseObjectsAndObjectTypesTestAsync(CancellationToken ct = default)
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
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    FlattenTypeInstance = false,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(180, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandVariablesTest1Async(CancellationToken ct = default)
        {
            // Test only variables as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10216" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10217" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10218" }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandVariablesAndObjectsTest1Async(CancellationToken ct = default)
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
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = false,
                    NoSubTypesOfTypeNodes = false,
                    FlattenTypeInstance = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(156, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandVariableTypesTest1Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.PropertyType.ToString() }
            };
            var results = await _service.ExpandAsync(entry,
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
            Assert.Equal(678, result.Result.OpcNodes.Count);
        }

        public async Task ExpandVariableTypesTest2Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.DataItemType.ToString() }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandVariableTypesTest3Async(CancellationToken ct = default)
        {
            // Test only variable types as node ids in an entry
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.PropertyType.ToString() },
                new OpcNodeModel { Id = Opc.Ua.VariableTypeIds.DataItemType.ToString() }
            };
            var results = await _service.ExpandAsync(entry,
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
            Assert.Equal(774, total);
        }

        public async Task ExpandObjectWithNoObjectsTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10791" }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandObjectWithNoObjectsTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10791" }
            };
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = true,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            // Discard errors -> no errors
            Assert.Empty(results);
        }

        public async Task ExpandEmptyEntryTest1Async(CancellationToken ct = default)
        {
            // Nothing should be returned when an empty entry passed
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = Array.Empty<OpcNodeModel>();
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
        }

        public async Task ExpandEmptyEntryTest2Async(CancellationToken ct = default)
        {
            // Nothing should be returned when an empty entry passed
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = Array.Empty<OpcNodeModel>();
            var results = await _service.ExpandAsync(entry,
                new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = true
                }, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Empty(results);
        }

        public async Task ExpandBadNodeIdTest1Async(CancellationToken ct = default)
        {
            // Entry with bad node id should return error
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "s=bad" }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        public async Task ExpandBadNodeIdTest2Async(CancellationToken ct = default)
        {
            //  An entry with multiple nodes and duplicate field ids should return an error
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10157", DataSetFieldId = "test" },
                new OpcNodeModel { Id = "http://test.org/UA/Data/#i=10158", DataSetFieldId = "test" }
            };
            var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await _service.ExpandAsync(
                entry, new PublishedNodeExpansionModel
                {
                    DiscardErrors = false,
                    ExcludeRootIfInstanceNode = true,
                    NoSubTypesOfTypeNodes = false,
                    CreateSingleWriter = false
                }, ct).ToListAsync(ct).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.True(ex is MethodCallStatusException or BadRequestException);
        }

        public async Task ExpandBadNodeIdTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            // Method or view node id passed results in not supported error.
            entry.OpcNodes = new[]
            {
                new OpcNodeModel { Id = MethodIds.Server_GetMonitoredItems.ToString() }
            };
            var results = await _service.ExpandAsync(entry,
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
        }

        //
        //  7. Object and maxdepth 0 -> max depth 1
        //  7b.Object and stop first found -> organizes is used
        //  8. Object type with stop first found set uses organizes
        //

        private readonly ConnectionModel _connection;
        private readonly IConfigurationServices _service;
    }
}
