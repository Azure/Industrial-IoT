// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using Moq.Language.Flow;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class AssetTests4
    {
        /// <summary>
        /// Create configuration tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="verify"></param>
        public AssetTests4(Func<IPublishedNodesServices, IAssetConfiguration<Stream>> services,
            ConnectionModel connection, bool verify = true)
        {
            _verify = verify;
            _service = services;
            _connection = connection;

            _publishedNodesServices = new Mock<IPublishedNodesServices>();
        }

        public async Task ConfigureDuplicateAssetFailsAsync(CancellationToken ct = default)
        {
            var asset1 = _connection.ToPublishedNodesEntry();
            asset1.DataSetWriterGroup = "WriterGroup1";
            asset1.DataSetName = "ConfigureDuplicateAssetFailsAsync";
            var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(TestAsset));
            await using (var s1 = stream1.ConfigureAwait(false))
            {
                var request1 = new PublishedNodeCreateAssetRequestModel<Stream>
                {
                    Configuration = stream1,
                    Entry = asset1
                };
                var createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                    It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                createCall.Verifiable(Times.Once);
                var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                    request1, ct).ConfigureAwait(false);
                Assert.Null(result.ErrorInfo);
                Assert.NotNull(result.Result);
                Assert.NotNull(result.Result.OpcNodes);
                var node = Assert.Single(result.Result.OpcNodes);
                Assert.StartsWith("nsu=http://opcfoundation.org/AssetServer;i=",
                    result.Result.DataSetWriterId, StringComparison.Ordinal);
            }

            var asset2 = _connection.ToPublishedNodesEntry();
            asset2.DataSetWriterGroup = "WriterGroup2";
            asset2.DataSetName = asset1.DataSetName;
            var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(TestAsset));
            await using (var s2 = stream2.ConfigureAwait(false))
            {
                var request2 = new PublishedNodeCreateAssetRequestModel<Stream>
                {
                    Configuration = stream2,
                    Entry = asset2
                };
                var dup = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                    request2, ct).ConfigureAwait(false);

                Assert.NotNull(dup.ErrorInfo);
                Assert.Equal(Opc.Ua.StatusCodes.BadBrowseNameDuplicated, dup.ErrorInfo.StatusCode);
            }
            Verify();
        }

        public async Task ConfigureWithBadStreamFails1Async(CancellationToken ct = default)
        {
            var asset = _connection.ToPublishedNodesEntry();
            asset.DataSetWriterGroup = "WriterGroup1";
            asset.DataSetName = "ConfigureWithBadStreamFails1Async";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(TestAsset));
            await stream.DisposeAsync().ConfigureAwait(false);
            var request = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream,
                Entry = asset
            };

            var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                request, ct).ConfigureAwait(false);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.Bad, result.ErrorInfo.StatusCode);

            Verify();
        }

        public async Task ConfigureWithBadStreamFails2Async(CancellationToken ct = default)
        {
            // Test passing a stream that throws on read
            var asset = _connection.ToPublishedNodesEntry();
            asset.DataSetWriterGroup = "WriterGroup1";
            asset.DataSetName = "ConfigureWithBadStreamFails2Async";

            var stream = new Mock<Stream>();
            stream.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new IOException());
            var request = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream.Object,
                Entry = asset
            };

            var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                request, ct).ConfigureAwait(false);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadUnexpectedError, result.ErrorInfo.StatusCode);

            Verify();
        }

        public async Task ConfigureWithBadStreamFails3Async(CancellationToken ct = default)
        {
            // Test passing a stream that throws on close
            var asset = _connection.ToPublishedNodesEntry();
            asset.DataSetWriterGroup = "WriterGroup1";
            asset.DataSetName = "ConfigureWithBadStreamFails3Async";

            var stream = new Mock<Stream>();
            stream.Setup(s => s.Close()).Throws(new IOException());
            var request = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream.Object,
                Entry = asset
            };

            var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                request, ct).ConfigureAwait(false);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadUnexpectedError, result.ErrorInfo.StatusCode);

            Verify();
        }

        public async Task ConfigureAssetFails1Async(CancellationToken ct = default)
        {
            // Throw in when calling create or update
            var asset = _connection.ToPublishedNodesEntry();
            asset.DataSetWriterGroup = "WriterGroup1";
            asset.DataSetName = "ConfigureAssetFails1Async";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(TestAsset));
            var request = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream,
                Entry = asset
            };

            var createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new IOException("Bad")));
            createCall.Verifiable(Times.Once);
            var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                request, ct).ConfigureAwait(false);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.Bad, result.ErrorInfo.StatusCode);

            Verify();
        }

        private void Verify()
        {
            if (_verify)
            {
                _publishedNodesServices.Verify();
                _publishedNodesServices.VerifyNoOtherCalls();
            }
        }

        private readonly ConnectionModel _connection;
        private readonly Mock<IPublishedNodesServices> _publishedNodesServices;
        private readonly Func<IPublishedNodesServices, IAssetConfiguration<Stream>> _service;
        private readonly bool _verify;
        private const string TestAsset = """
{
    "@context": [
        "https://www.w3.org/2022/wot/td/v1.1"
    ],
    "id": "urn:sim3264",
    "securityDefinitions": {
        "nosec_sc": {
            "scheme": "nosec"
        }
    },
    "security": [
        "nosec_sc"
    ],
    "@type": [
        "Thing"
    ],
    "name": "sim-3264",
    "base": "sim://simserver1:443",
    "title": "Simulated Asset",
    "properties": {
        "VoltageL1-N": {
            "type": "number",
            "opcua:nodeId": "s=VoltageL-N",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "1",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        }
    }
}
""";
    }
}
