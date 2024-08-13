// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
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

        public async Task ExpandTest1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157",
                    BrowsePath = new[] {
                        "http://test.org/UA/Data/#Static"
                    }
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    ExcludeRootObject = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(24, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandTest2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157",
                    BrowsePath = new[] {
                        "http://test.org/UA/Data/#Static"
                    }
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    ExcludeRootObject = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    NoSubtypes = false,
                    CreateSingleWriter = true
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(258, result.Result.OpcNodes.Count);
        }

        public async Task ExpandTest3Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    ExcludeRootObject = true,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(110, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandTest4Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = "http://test.org/UA/Data/#i=10157"
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    ExcludeRootObject = true,
                    NoSubtypes = false,
                    CreateSingleWriter = true
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(1100, result.Result.OpcNodes.Count);
        }

        public async Task ExpandTest5Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    ExcludeRootObject = true,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(163, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandTest6Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    ExcludeRootObject = false,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(164, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandTest7Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectIds.Server.ToString()
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = 1,
                    ExcludeRootObject = false,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            var node = Assert.Single(result.Result.OpcNodes);
            Assert.Equal(Opc.Ua.VariableIds.Server_ServerStatus.ToString(), node.Id);
            Assert.Equal("i=2253/ServerStatus/0", node.DataSetFieldId);
        }

        public async Task ExpandTest8Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.OpcNodes = new[]
            {
                new OpcNodeModel
                {
                    Id = Opc.Ua.ObjectTypeIds.BaseObjectType.ToString()
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    ExcludeRootObject = false,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(324, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        public async Task ExpandTest9Async(CancellationToken ct = default)
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
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    StopAtFirstFoundObject = false,
                    MaxDepth = null,
                    ExcludeRootObject = false,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(324 + 164 + 110, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.True(r.Result.OpcNodes.Count > 0);
            });
        }

        private readonly ConnectionModel _connection;
        private readonly IConfigurationServices _service;
    }
}
