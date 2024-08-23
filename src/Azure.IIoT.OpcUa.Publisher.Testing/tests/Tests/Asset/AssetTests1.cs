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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class AssetTests1
    {
        /// <summary>
        /// Create configuration tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="verify"></param>
        public AssetTests1(Func<IPublishedNodesServices, IAssetConfiguration<Stream>> services,
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

        public async Task ConfigureAndDeleteAssetsAsync(CancellationToken ct = default)
        {
            var service = _service(_publishedNodesServices.Object);

            ServiceResponse<PublishedNodesEntryModel> result;
            var asset1 = _connection.ToPublishedNodesEntry();
            asset1.DataSetWriterGroup = "WriterGroup1";
            asset1.DataSetName = "Sim3264Asset";

            _createCall.Verifiable(Times.Exactly(2));
            _deleteCall.Verifiable(Times.Once);

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
            await using (var c1 = stream.ConfigureAwait(false))
            {
                var request = new PublishedNodeCreateAssetRequestModel<Stream>
                {
                    Configuration = stream,
                    Entry = asset1
                };

                result = await service.CreateOrUpdateAssetAsync(
                    request, ct).ConfigureAwait(false);

                Assert.Null(result.ErrorInfo);
                Assert.NotNull(result.Result);
                Assert.NotNull(result.Result.OpcNodes);
                Assert.Equal(16, result.Result.OpcNodes.Count);
                Assert.Equal("nsu=http://opcfoundation.org/AssetServer;i=1",
                    result.Result.DataSetWriterId);

                asset1 = result.Result;
            }

            var asset2 = _connection.ToPublishedNodesEntry();
            asset2.DataSetWriterGroup = "WriterGroup1";
            asset2.DataSetName = "Sim3265Asset";

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
            var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(Asset2));
            await using (var c2 = stream2.ConfigureAwait(false))
            {
                var request = new PublishedNodeCreateAssetRequestModel<Stream>
                {
                    Configuration = stream2,
                    Entry = asset2
                };

                result = await service.CreateOrUpdateAssetAsync(
                    request, ct).ConfigureAwait(false);
                Assert.Null(result.ErrorInfo);
                Assert.NotNull(result.Result);
                Assert.NotNull(result.Result.OpcNodes);
                Assert.Equal(16, result.Result.OpcNodes.Count);
                Assert.Equal("nsu=http://opcfoundation.org/AssetServer;i=24",
                    result.Result.DataSetWriterId);

                asset2 = result.Result;
            }

            // Now we have 2 assets created
            var results = await service.GetAllAssetsAsync(result.Result, ct: ct)
                .ToListAsync(ct).ConfigureAwait(false);

            Assert.Equal(2, results.Count);
            Assert.All(results, r =>
            {
                Assert.Null(r.ErrorInfo);
                Assert.NotNull(r.Result);
                Assert.NotNull(r.Result.OpcNodes);
                Assert.Equal(16, r.Result.OpcNodes.Count);
            });

            var errorInfo = await service.DeleteAssetAsync(
                new PublishedNodeDeleteAssetRequestModel
                {
                    Entry = asset2
                }, ct).ConfigureAwait(false);

            Assert.NotNull(errorInfo);
            Assert.Equal(0u, errorInfo.StatusCode);

            // Now we have 1 asset remaining
            results = await service.GetAllAssetsAsync(asset1, ct: ct)
                .ToListAsync(ct).ConfigureAwait(false);

            var remainingAsset = Assert.Single(results);
            Assert.Null(remainingAsset.ErrorInfo);
            Assert.NotNull(remainingAsset.Result);
            Assert.NotNull(remainingAsset.Result.OpcNodes);
            Assert.Equal(16, remainingAsset.Result.OpcNodes.Count);

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
        private readonly bool _verify;
        private readonly Mock<IPublishedNodesServices> _publishedNodesServices;
        private readonly IReturnsResult<IPublishedNodesServices> _createCall;
        private readonly IReturnsResult<IPublishedNodesServices> _deleteCall;
        private readonly Func<IPublishedNodesServices, IAssetConfiguration<Stream>> _service;
    }
}
