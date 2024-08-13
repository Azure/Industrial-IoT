// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ConfigurationTests
    {
        /// <summary>
        /// Create node configuration tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public ConfigurationTests(IConfigurationServices services, ConnectionModel connection)
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
                    Id = Opc.Ua.ObjectTypeIds.FileDirectoryType.ToString()
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    ExcludeRootObject = false,
                    StopAtFirstFoundObject = true,
                    MaxDepth = null,
                    NoSubtypes = false,
                    CreateSingleWriter = false
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(DriveInfo.GetDrives().Length, results.Count);
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
                    Id = Opc.Ua.ObjectTypeIds.FileDirectoryType.ToString()
                }
            };
            var results = await _service.ExpandAsync(
                new PublishedNodeExpansionRequestModel
                {
                    Entry = entry,
                    DiscardErrors = false,
                    ExcludeRootObject = false,
                    StopAtFirstFoundObject = true,
                    MaxDepth = null,
                    NoSubtypes = false,
                    CreateSingleWriter = true
                },
                false, ct).ToListAsync(ct).ConfigureAwait(false);

            var result = Assert.Single(results);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(DriveInfo.GetDrives().Length, result.Result.OpcNodes.Count);
        }

        private readonly ConnectionModel _connection;
        private readonly IConfigurationServices _service;
    }
}
