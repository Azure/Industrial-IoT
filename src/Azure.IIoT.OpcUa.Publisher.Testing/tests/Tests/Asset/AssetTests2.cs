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

    public class AssetTests2
    {
        /// <summary>
        /// Create configuration tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="verify"></param>
        public AssetTests2(Func<IPublishedNodesServices, IAssetConfiguration<Stream>> services,
            ConnectionModel connection, bool verify = true)
        {
            _verify = verify;
            _service = services;
            _connection = connection;

            _publishedNodesServices = new Mock<IPublishedNodesServices>();
            _createCall = _publishedNodesServices.Setup(s => s.CreateOrUpdateDataSetWriterEntryAsync(
                It.IsAny<PublishedNodesEntryModel>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _deleteCall = _publishedNodesServices.Setup(s => s.RemoveDataSetWriterEntryAsync(
                "WriterGroup1", It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public async Task ConfigureAsset1Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.DataSetWriterGroup = "WriterGroup1";
            entry.DataSetName = "Sim3264Asset";

            const string Asset1 = """
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
        },
        "VoltageL2-N": {
            "type": "number",
            "opcua:nodeId": "s=VoltageL-N",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "3",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL3-N": {
            "type": "number",
            "opcua:nodeId": "s=VoltageL-N",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "5",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL1-L2": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "7",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL2-L3": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "9",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL3-L1": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "11",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "CurrentL1": {
            "type": "number",
            "opcua:nodeId": "s=Current",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "13",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "CurrentL2": {
            "type": "number",
            "opcua:nodeId": "s=Current",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "15",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "CurrentL3": {
            "type": "number",
            "opcua:nodeId": "s=Current",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "17",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "PowerFactorL1": {
            "type": "number",
            "opcua:nodeId": "s=PowerFactor",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "19",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "PowerFactorL2": {
            "type": "number",
            "opcua:nodeId": "s=PowerFactor",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "139",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "PowerFactorL3": {
            "type": "number",
            "opcua:nodeId": "s=PowerFactor",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "141",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalApparentPower": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "63",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalActivePower": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "65",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalReactivePower": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "67",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalPowerFactor": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "69",
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
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Asset1));
            await using var _ = stream.ConfigureAwait(false);
            var request = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream,
                Entry = entry
            };

            _createCall.Verifiable(Times.Once);
            var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                request, ct).ConfigureAwait(false);

            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(16, result.Result.OpcNodes.Count);
            Assert.StartsWith("nsu=http://opcfoundation.org/AssetServer;i=",
                result.Result.DataSetWriterId, StringComparison.Ordinal);

            Verify();
        }

        public async Task ConfigureAsset2Async(CancellationToken ct = default)
        {
            var entry = _connection.ToPublishedNodesEntry();
            entry.DataSetWriterGroup = "WriterGroup1";
            entry.DataSetName = "Sim3265Asset";

            const string Asset2 = """
{
    "@context": [
        "https://www.w3.org/2022/wot/td/v1.1"
    ],
    "id": "urn:sim3265",
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
    "name": "sim-3265",
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
        },
        "VoltageL2-N": {
            "type": "number",
            "opcua:nodeId": "s=VoltageL-N",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "3",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL3-N": {
            "type": "number",
            "opcua:nodeId": "s=VoltageL-N",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "5",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL1-L2": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "7",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL2-L3": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "9",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "VoltageL3-L1": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "11",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "CurrentL1": {
            "type": "number",
            "opcua:nodeId": "s=Current",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "13",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "CurrentL2": {
            "type": "number",
            "opcua:nodeId": "s=Current",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "15",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "CurrentL3": {
            "type": "number",
            "opcua:nodeId": "s=Current",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "17",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "PowerFactorL1": {
            "type": "number",
            "opcua:nodeId": "s=PowerFactor",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "19",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "PowerFactorL2": {
            "type": "number",
            "opcua:nodeId": "s=PowerFactor",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "139",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "PowerFactorL3": {
            "type": "number",
            "opcua:nodeId": "s=PowerFactor",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "141",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalApparentPower": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "63",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalActivePower": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "65",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalReactivePower": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "67",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "sim:type": "Double",
                    "sim:pollingTime": 120
                }
            ]
        },
        "TotalPowerFactor": {
            "type": "number",
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "69",
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
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Asset2));
            await using var _ = stream.ConfigureAwait(false);
            var request = new PublishedNodeCreateAssetRequestModel<Stream>
            {
                Configuration = stream,
                Entry = entry
            };

            _createCall.Verifiable(Times.Once);
            _deleteCall.Verifiable(Times.Once);
            var result = await _service(_publishedNodesServices.Object).CreateOrUpdateAssetAsync(
                request, ct).ConfigureAwait(false);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.Result);
            Assert.NotNull(result.Result.OpcNodes);
            Assert.Equal(16, result.Result.OpcNodes.Count);
            Assert.StartsWith("nsu=http://opcfoundation.org/AssetServer;i=",
                result.Result.DataSetWriterId, StringComparison.Ordinal);

            var errorInfo = await _service(_publishedNodesServices.Object).DeleteAssetAsync(
                new PublishedNodeDeleteAssetRequestModel
                {
                    Entry = result.Result
                }, ct).ConfigureAwait(false);

            Assert.Null(result.ErrorInfo);

            Verify();
        }

        public async Task ConfigureAsset3Async(CancellationToken ct = default)
        {
            var service = _service(_publishedNodesServices.Object);
            const string Template = """
{
    "@context": [
        "https://www.w3.org/2022/wot/td/v1.1"
    ],
    "id": "urn:<<name>>",
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
    "name": "<<name>>",
    "base": "sim://<<name>>:443",
    "title": "Simulated Asset <<name>>",
    "properties": {
        "AssetTag": {
            "type": "number",
            "observable": true,
            "forms": [
                {
                    "href": "1",
                    "op": [
                        "readproperty",
                        "writeproperty",
                        "observeproperty"
                    ],
                    "sim:type": "String",
                    "sim:pollingTime": 1000
                }
            ]
        }
    }
}
""";
            const int NumberOfAssets = 50;
            const string AssetPrefix = "SimAsset__";
            _createCall.Verifiable(Times.Exactly(NumberOfAssets));
            _deleteCall.Verifiable(Times.Exactly(NumberOfAssets));

            var assets = new List<PublishedNodesEntryModel>();
            for (var i = 0; i < NumberOfAssets; i++)
            {
                var asset = _connection.ToPublishedNodesEntry();
                asset.DataSetWriterGroup = "WriterGroup1";
                asset.DataSetName = AssetPrefix + i;
                var t = Template.Replace("<<name>>", "sim" + i, StringComparison.Ordinal);

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(t));
                await using (var c1 = stream.ConfigureAwait(false))
                {
                    var request = new PublishedNodeCreateAssetRequestModel<Stream>
                    {
                        Configuration = stream,
                        Entry = asset
                    };

                    var result = await service.CreateOrUpdateAssetAsync(
                        request, ct).ConfigureAwait(false);

                    AssertResult(result);
                    assets.Add(result.Result!);
                }
            }

            var firstAsset = assets[0];
            foreach (var asset in assets.ToList())
            {
                var results = await service.GetAllAssetsAsync(firstAsset, ct: ct)
                    .ToListAsync(ct).ConfigureAwait(false);
                results = results.Where(e => e.Result?.DataSetName?
                    .StartsWith(AssetPrefix, StringComparison.Ordinal) ?? true).ToList();
                Assert.Equal(assets.Count, results.Count);
                Assert.All(results, AssertResult);

                var errorInfo = await service.DeleteAssetAsync(
                    new PublishedNodeDeleteAssetRequestModel
                    {
                        Entry = asset
                    }, ct).ConfigureAwait(false);

                Assert.NotNull(errorInfo);
                Assert.Equal(0u, errorInfo.StatusCode);
                assets.Remove(asset);
            }

            // Now none remaining
            var nothingLeft = await service.GetAllAssetsAsync(firstAsset, ct: ct)
                .ToListAsync(ct).ConfigureAwait(false);
            nothingLeft = nothingLeft.Where(e => e.Result?.DataSetName?
               .StartsWith(AssetPrefix, StringComparison.Ordinal) ?? true).ToList();
            Assert.Empty(nothingLeft);

            Verify();

            static void AssertResult(ServiceResponse<PublishedNodesEntryModel> result)
            {
                Assert.Null(result.ErrorInfo);
                Assert.NotNull(result.Result);
                Assert.NotNull(result.Result.OpcNodes);
                Assert.StartsWith(AssetPrefix, result.Result.DataSetName, StringComparison.Ordinal);
                var node = Assert.Single(result.Result.OpcNodes);
            }
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
        private readonly IReturnsResult<IPublishedNodesServices> _createCall;
        private readonly IReturnsResult<IPublishedNodesServices> _deleteCall;
        private readonly Func<IPublishedNodesServices, IAssetConfiguration<Stream>> _service;
        private readonly bool _verify;
    }
}
