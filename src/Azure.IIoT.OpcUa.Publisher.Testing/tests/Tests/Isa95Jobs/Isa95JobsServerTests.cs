// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UAModel.ISA95_JOBCONTROL_V2;
    using Xunit;

    /// <summary>
    /// ISA-95 Job Control server node tests.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Isa95JobsServerTests<T>
    {
        public Isa95JobsServerTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task BrowseJobResponseDataTypeAsync(CancellationToken ct = default)
        {
            var response = await _services().BrowseFirstAsync(_connection, new BrowseFirstRequestModel
            {
                NodeId = GetNodeId(DataTypes.ISA95JobResponseDataType),
                TargetNodesOnly = false
            }, ct).ConfigureAwait(false);

            Assert.NotNull(response);
            Assert.Null(response.ErrorInfo);
            Assert.NotNull(response.Node);
            Assert.Equal(GetNodeId(DataTypes.ISA95JobResponseDataType), response.Node.NodeId);
            Assert.Equal("ISA95JobResponseDataType", response.Node.BrowseName);
            Assert.NotEmpty(response.References!);
        }

        public async Task ReadJobResponseDataTypeAttributesAsync(CancellationToken ct = default)
        {
            var nodeId = GetNodeId(DataTypes.ISA95JobResponseDataType);
            IReadOnlyList<AttributeReadRequestModel> attributes =
            [
                new()
                {
                    NodeId = nodeId,
                    Attribute = NodeAttribute.NodeClass
                },
                new()
                {
                    NodeId = nodeId,
                    Attribute = NodeAttribute.BrowseName
                }
            ];

            var response = await _services().ReadAsync(_connection, new ReadRequestModel
            {
                Attributes = attributes
            }, ct).ConfigureAwait(false);

            Assert.NotNull(response);
            Assert.Null(response.ErrorInfo);
            Assert.Equal(2, response.Results.Count);
            Assert.All(response.Results, result => Assert.Null(result.ErrorInfo));
            Assert.Equal((int)Opc.Ua.NodeClass.DataType, (int)response.Results[0].Value);
            Assert.Equal("ISA95JobResponseDataType", (string)response.Results[1].Value);
        }

        public async Task GetJobResponseDataTypeMetadataAsync(CancellationToken ct = default)
        {
            var response = await _services().GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = GetNodeId(DataTypes.ISA95JobResponseDataType)
            }, ct).ConfigureAwait(false);

            Assert.NotNull(response);
            Assert.Null(response.ErrorInfo);
            Assert.Equal((int)Opc.Ua.NodeClass.DataType, (int)response.NodeClass);
            Assert.NotNull(response.DataTypeMetadata);
        }

        public async Task GetStoreMethodMetadataAsync(CancellationToken ct = default)
        {
            var response = await _services().GetMethodMetadataAsync(_connection, new MethodMetadataRequestModel
            {
                MethodId = GetNodeId(Methods.ISA95JobOrderReceiverObjectType_Store)
            }, ct).ConfigureAwait(false);

            Assert.NotNull(response);
            Assert.Null(response.ErrorInfo);
            Assert.NotNull(response.ObjectId);
        }

        public Task EncodeDecodeJobResponseDataTypeAsync()
        {
            var source = new ISA95JobResponseDataType
            {
                EncodingMask = (uint)(
                    ISA95JobResponseDataTypeFields.StartTime |
                    ISA95JobResponseDataTypeFields.EquipmentActuals |
                    ISA95JobResponseDataTypeFields.MaterialActuals),
                JobOrderID = "job-42",
                JobResponseID = "response-42",
                StartTime = Opc.Ua.DateTimeUtc.Now,
                EquipmentActuals =
                [
                    new ISA95EquipmentDataType
                    {
                        EncodingMask = (uint)(
                            ISA95EquipmentDataTypeFields.EquipmentUse |
                            ISA95EquipmentDataTypeFields.Quantity),
                        EquipmentUse = "consumable",
                        Quantity = "500"
                    }
                ],
                MaterialActuals =
                [
                    new ISA95MaterialDataType
                    {
                        EncodingMask = (uint)(
                            ISA95MaterialDataTypeFields.MaterialClassID |
                            ISA95MaterialDataTypeFields.MaterialUse |
                            ISA95MaterialDataTypeFields.Quantity),
                        MaterialClassID = "material-42",
                        MaterialUse = "consumable",
                        Quantity = "1"
                    }
                ]
            };
            using var stream = new MemoryStream();
            using (var encoder = new Opc.Ua.BinaryEncoder(
                stream, new Opc.Ua.ServiceMessageContext(), leaveOpen: true))
            {
                source.Encode(encoder);
            }
            stream.Position = 0;
            var decoded = new ISA95JobResponseDataType();
            using (var decoder = new Opc.Ua.BinaryDecoder(
                stream, new Opc.Ua.ServiceMessageContext(), leaveOpen: true))
            {
                decoded.Decode(decoder);
            }

            Assert.True(source.IsEqual(decoded));
            return Task.CompletedTask;
        }

        private static string GetNodeId(uint identifier)
        {
            return $"{Namespaces.ISA95_JOBCONTROL_V2}#i={identifier}";
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
