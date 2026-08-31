// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

extern alias Quickstarts;

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Text.Json.Nodes;
    using MemoryBuffer = Quickstarts::MemoryBuffer;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class CallScalarMethodTests<T>
    {
        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="newMetadata"></param>
        public CallScalarMethodTests(Func<INodeServices<T>> services, T connection, bool newMetadata = false)
        {
            _services = services;
            _connection = connection;
            _newMetadata = newMetadata;
        }

        public async Task NodeMethodMetadataStaticScalarMethod1TestAsync(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2709";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            // Act
            MethodMetadataModel result;
            if (!_newMetadata)
            {
                result = await service.GetMethodMetadataAsync(_connection, new MethodMetadataRequestModel
                {
                    MethodId = methodId
                }, ct).ConfigureAwait(false);
            }
            else
            {
                var metadata = await service.GetMetadataAsync(_connection, new NodeMetadataRequestModel
                {
                    NodeId = methodId
                }, ct).ConfigureAwait(false);
                result = metadata.MethodMetadata!;
            }

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments!,
                arg =>
                {
                    Assert.Equal("BooleanIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("SByteIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("ByteIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("Int16In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("UInt16In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("Int32In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("UInt32In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("Int64In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("UInt64In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("FloatIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("DoubleIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments!,
                arg =>
                {
                    Assert.Equal("BooleanOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("SByteOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("ByteOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("Int16Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("UInt16Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("Int32Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("UInt32Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("Int64Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("UInt64Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("FloatOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("DoubleOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }

        public async Task NodeMethodMetadataStaticScalarMethod2TestAsync(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2712";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            // Act
            MethodMetadataModel result;
            if (!_newMetadata)
            {
                result = await service.GetMethodMetadataAsync(_connection, new MethodMetadataRequestModel
                {
                    MethodId = methodId
                }, ct).ConfigureAwait(false);
            }
            else
            {
                var metadata = await service.GetMetadataAsync(_connection, new NodeMetadataRequestModel
                {
                    NodeId = methodId
                }, ct).ConfigureAwait(false);
                result = metadata.MethodMetadata!;
            }

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments!,
                arg =>
                {
                    Assert.Equal("StringIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("DateTimeIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("GuidIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("ByteStringIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("XmlElementIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("NodeIdIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("ExpandedNodeIdIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("QualifiedNameIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("LocalizedTextIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StatusCodeIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments!,
                arg =>
                {
                    Assert.Equal("StringOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("DateTimeOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("GuidOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("ByteStringOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("XmlElementOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("NodeIdOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("ExpandedNodeIdOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("QualifiedNameOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("LocalizedTextOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StatusCodeOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }

        public async Task NodeMethodMetadataStaticScalarMethod3TestAsync(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2715";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            // Act
            MethodMetadataModel result;
            if (!_newMetadata)
            {
                result = await service.GetMethodMetadataAsync(_connection, new MethodMetadataRequestModel
                {
                    MethodId = methodId
                }, ct).ConfigureAwait(false);
            }
            else
            {
                var metadata = await service.GetMetadataAsync(_connection, new NodeMetadataRequestModel
                {
                    NodeId = methodId
                }, ct).ConfigureAwait(false);
                result = metadata.MethodMetadata!;
            }

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments!,
                arg =>
                {
                    Assert.Equal("VariantIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("EnumerationIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StructureIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments!,
                arg =>
                {
                    Assert.Equal("VariantOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("EnumerationOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StructureOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }

        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest1Async(CancellationToken ct = default)
        {
            var service = _services();
            const string objectId = "http://test.org/UA/Data/#i=2708";
            var path = new[] {
                ".http://test.org/UA/Data/#ScalarMethod3"
            };

            // Act
            MethodMetadataModel result;
            if (!_newMetadata)
            {
                result = await service.GetMethodMetadataAsync(_connection, new MethodMetadataRequestModel
                {
                    MethodId = objectId,
                    MethodBrowsePath = path
                }, ct).ConfigureAwait(false);
            }
            else
            {
                var metadata = await service.GetMetadataAsync(_connection, new NodeMetadataRequestModel
                {
                    NodeId = objectId,
                    BrowsePath = path
                }, ct).ConfigureAwait(false);
                result = metadata.MethodMetadata!;
            }

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments!,
                arg =>
                {
                    Assert.Equal("VariantIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("EnumerationIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StructureIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments!,
                arg =>
                {
                    Assert.Equal("VariantOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("EnumerationOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StructureOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }

        public async Task NodeMethodMetadataStaticScalarMethod3WithBrowsePathTest2Async(CancellationToken ct = default)
        {
            var service = _services();
            var path = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest",
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            // Act
            MethodMetadataModel result;
            if (!_newMetadata)
            {
                result = await service.GetMethodMetadataAsync(_connection, new MethodMetadataRequestModel
                {
                    MethodBrowsePath = path
                }, ct).ConfigureAwait(false);
            }
            else
            {
                var metadata = await service.GetMetadataAsync(_connection, new NodeMetadataRequestModel
                {
                    BrowsePath = path
                }, ct).ConfigureAwait(false);
                result = metadata.MethodMetadata!;
            }

            // Assert
            Assert.Equal("http://test.org/UA/Data/#i=2708", result.ObjectId);
            Assert.Collection(result.InputArguments!,
                arg =>
                {
                    Assert.Equal("VariantIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("EnumerationIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StructureIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments!,
                arg =>
                {
                    Assert.Equal("VariantOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("EnumerationOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg =>
                {
                    Assert.Equal("StructureOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Null(arg.ArrayDimensions);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }

        public async Task NodeMethodCallStaticScalarMethod1Test1Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2709";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "boolean",
                    Value = true
                },
                new() {
                    DataType = "sbyte",
                    Value = -1
                },
                new() {
                    DataType = "byte",
                    Value = 244
                },
                new() {
                    DataType = "Int16",
                    Value = short.MinValue
                },
                new() {
                    DataType = "UInt16",
                    Value = 0
                },
                new() {
                    DataType = "int32",
                    Value = int.MinValue
                },
                new() {
                    DataType = "uInt32",
                    Value = uint.MaxValue
                },
                new() {
                    DataType = "Int64",
                    Value = -55555
                },
                new() {
                    DataType = "uint64",
                    Value = 55555
                },
                new() {
                    DataType = "float",
                    Value = 12.898345f
                },
                new() {
                    DataType = "DOUBLE",
                    Value = 1234.4567
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.True((bool)arg.Value!),
                arg => Assert.Equal((sbyte)-1, (sbyte)arg.Value!),
                arg => Assert.Equal((byte)244, (byte)arg.Value!),
                arg => Assert.Equal(short.MinValue, (short)arg.Value!),
                arg => Assert.Equal((ushort)0, (ushort)arg.Value!),
                arg => Assert.Equal(int.MinValue, (int)arg.Value!),
                arg => Assert.Equal(uint.MaxValue, (uint)arg.Value!),
                arg => Assert.Equal(-55555, (long)arg.Value!),
                arg => Assert.Equal((ulong)55555, (ulong)arg.Value!),
                arg => Assert.Equal(12.898345f, (float)arg.Value!),
                arg => Assert.Equal(1234.4567, (double)arg.Value!));
        }

        public async Task NodeMethodCallStaticScalarMethod1Test2Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2709";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "boolean",
                    Value = false
                },
                new() {
                    DataType = "sbyte",
                    Value = -100
                },
                new() {
                    DataType = "byte",
                    Value = 100
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.False((bool)arg.Value!),
                arg => Assert.Equal((sbyte)-100, (sbyte)arg.Value!),
                arg => Assert.Equal((byte)100, (byte)arg.Value!),
                arg => Assert.Equal((short)0, (short)arg.Value!),
                arg => Assert.Equal((ushort)0, (ushort)arg.Value!),
                arg => Assert.Equal(0, (int)arg.Value!),
                arg => Assert.Equal((uint)0, (uint)arg.Value!),
                arg => Assert.Equal(0, (long)arg.Value!),
                arg => Assert.Equal((ulong)0, (ulong)arg.Value!),
                arg => Assert.Equal(0, (float)arg.Value!),
                arg => Assert.Equal(0, (double)arg.Value!));
        }

        public async Task NodeMethodCallStaticScalarMethod1Test3Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2709";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.False((bool)arg.Value!),
                arg => Assert.Equal((sbyte)0, (sbyte)arg.Value!),
                arg => Assert.Equal((byte)0, (byte)arg.Value!),
                arg => Assert.Equal((short)0, (short)arg.Value!),
                arg => Assert.Equal((ushort)0, (ushort)arg.Value!),
                arg => Assert.Equal(0, (int)arg.Value!),
                arg => Assert.Equal((uint)0, (uint)arg.Value!),
                arg => Assert.Equal(0, (long)arg.Value!),
                arg => Assert.Equal((ulong)0, (ulong)arg.Value!),
                arg => Assert.Equal(0, (float)arg.Value!),
                arg => Assert.Equal(0, (double)arg.Value!));
        }

        public async Task NodeMethodCallStaticScalarMethod1Test4Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2709";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = new List<MethodCallArgumentModel>()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.False((bool)arg.Value!),
                arg => Assert.Equal((sbyte)0, (sbyte)arg.Value!),
                arg => Assert.Equal((byte)0, (byte)arg.Value!),
                arg => Assert.Equal((short)0, (short)arg.Value!),
                arg => Assert.Equal((ushort)0, (ushort)arg.Value!),
                arg => Assert.Equal(0, (int)arg.Value!),
                arg => Assert.Equal((uint)0, (uint)arg.Value!),
                arg => Assert.Equal(0, (long)arg.Value!),
                arg => Assert.Equal((ulong)0, (ulong)arg.Value!),
                arg => Assert.Equal(0, (float)arg.Value!),
                arg => Assert.Equal(0, (double)arg.Value!));
        }

        public async Task NodeMethodCallStaticScalarMethod1Test5Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2709";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var input = new List<MethodCallArgumentModel?> {
                new() {
                    DataType = "boolean",
                    Value = true
                },
                new() {
                    DataType = "sbyte",
                    Value = -1
                },
                new() {
                    DataType = "byte",
                    Value = 244
                },
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new() {
                    DataType = "DOUBLE",
                    Value = 1234.4567
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.True((bool)arg.Value!),
                arg => Assert.Equal((sbyte)-1, (sbyte)arg.Value!),
                arg => Assert.Equal((byte)244, (byte)arg.Value!),
                arg => Assert.Equal((short)0, (short)arg.Value!),
                arg => Assert.Equal((ushort)0, (ushort)arg.Value!),
                arg => Assert.Equal(0, (int)arg.Value!),
                arg => Assert.Equal((uint)0, (uint)arg.Value!),
                arg => Assert.Equal(0, (long)arg.Value!),
                arg => Assert.Equal((ulong)0, (ulong)arg.Value!),
                arg => Assert.Equal(0, (float)arg.Value!),
                arg => Assert.Equal((double)input[10]!.Value!, (double)arg.Value!));
        }

        public async Task NodeMethodCallStaticScalarMethod2Test1Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2712";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "String",
                    Value = "test"
                },
                new() {
                    DataType = "DateTime",
                    Value = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                },
                new() {
                    DataType = "Guid",
                    Value = Guid.NewGuid().ToString()
                },
                new() {
                    DataType = "ByteString",
                    Value = JsonNodeValueExtensions.FromObject(Encoding.UTF32.GetBytes("asdfasdfadsfs"))
                },
                new() {
                    DataType = "XmlElement",
                    Value = JsonNodeValueExtensions.FromObject(XmlElementEx.SerializeObject(
                        new MemoryBuffer.MemoryBufferInstance{
                            Name = "test",
                            TagCount = 333,
                            DataType ="Byte"
                        }))
                },
                new() {
                    DataType = "NodeId",
                    Value = "nsu=http://test.org/UA/Data/;i=44"
                },
                new() {
                    DataType = "ExpandedNodeId",
                    Value = "nsu=http://test.org/UA/Data/;i=45"
                },
                new() {
                    DataType = "QualifiedName",
                    Value = "nsu=http://test.org/UA/Data/;name"
                },
                new() {
                    DataType = "LocalizedText",
                    Value = new JsonObject {
                        ["Locale"] = "de",
                        ["Text"] = "Hallo Welt"
                    }
                },
                new() {
                    DataType = "StatusCode",
                    Value = 8888888
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[0].Value, arg.Value, "result[0]");
                    Assert.Equal(input[0].DataType, arg.DataType);
                },
                arg =>
                {
                    Assert.Equal(
                        DateTime.Parse(input[1].Value!.GetValue<string>(),
                            CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        DateTime.Parse(arg.Value!.GetValue<string>(),
                            CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                    Assert.Equal(input[1].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[2].Value, arg.Value, "result[2]");
                    Assert.Equal(input[2].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[3].Value, arg.Value, "result[3]");
                    Assert.Equal(input[3].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[4].Value, arg.Value, "result[4]");
                    Assert.Equal(input[4].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[5].Value, arg.Value, "result[5]");
                    Assert.Equal(input[5].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[6].Value, arg.Value, "result[6]");
                    Assert.Equal(input[6].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[7].Value, arg.Value, "result[7]");
                    Assert.Equal(input[7].DataType, arg.DataType);
                },
                arg =>
                {
                    JsonNodeAssert.AreEqual(input[8].Value, arg.Value, "result[8]");
                    Assert.Equal(input[8].DataType, arg.DataType);
                },
                arg =>
                {
                    Assert.Equal(8888888, arg.Value!["Code"]!.GetValue<int>());
                    Assert.Equal(input[9].DataType, arg.DataType);
                });
        }

        public async Task NodeMethodCallStaticScalarMethod2Test2Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2712";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var types = new List<string> {
                "String", "DateTime", "Guid", "ByteString",
                "XmlElement", "NodeId", "ExpandedNodeId",
                "QualifiedName","LocalizedText","StatusCode" };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[0], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.Equal("0001-01-01T00:00:00Z", (string)arg.Value!);
                     Assert.Equal(types[1], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[2], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[3], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[4], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[5], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[6], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[7], arg.DataType);
                 },
                 arg =>
                 {
                     Assert.True(arg.Value.IsNull());
                     Assert.Equal(types[8], arg.DataType);
                 },
                 arg =>
                 {
                     var value = Assert.IsType<JsonObject>(arg.Value);
                     Assert.Empty(value);
                     Assert.Equal(types[9], arg.DataType);
                 });
        }

        public async Task NodeMethodCallStaticScalarMethod3Test1Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2715";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "Variant",
                    Value = JsonNodeValueExtensions.FromObject(new {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new() {
                    DataType = "Int32",
                    Value = 8
                },
                new() {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 2,
                        new Opc.Ua.Argument("test",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.Collection(result.Results,
                arg => Assert.Equal(50u, (uint)arg.Value!),
                arg => Assert.Equal(8, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test", -1, "desc", "result[2]"));
        }

        public async Task NodeMethodCallStaticScalarMethod3Test2Async(CancellationToken ct = default)
        {
            var service = _services();
            const string methodId = "http://test.org/UA/Data/#i=2715";
            const string objectId = "http://test.org/UA/Data/#i=2708";

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "String",
                    Value = "varianttest"
                },
                new() {
                    DataType = "int32",
                    Value = 9999
                },
                new() {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 1,
                        new Opc.Ua.Argument("test1",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc1")
                                .AsBinary(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.Equal("varianttest", (string)arg.Value!),
                arg => Assert.Equal(9999, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test1", -1, "desc1", "result[2]"));
        }

        public async Task NodeMethodCallStaticScalarMethod3WithBrowsePathNoIdsTestAsync(CancellationToken ct = default)
        {
            var service = _services();
            var objectPath = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest"
            };

            var methodPath = new[] {
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "Variant",
                    Value = JsonNodeValueExtensions.FromObject(new {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new() {
                    DataType = "Int32",
                    Value = 8
                },
                new() {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 2,
                        new Opc.Ua.Argument("test",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodBrowsePath = methodPath,
                ObjectBrowsePath = objectPath,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.Equal(50u, (uint)arg.Value!),
                arg => Assert.Equal(8, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test", -1, "desc", "result[2]"));
        }

        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndBrowsePathTestAsync(CancellationToken ct = default)
        {
            var service = _services();
            const string objectId = "http://test.org/UA/Data/#i=2708";
            var methodPath = new[] {
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "Variant",
                    Value = JsonNodeValueExtensions.FromObject(new {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new() {
                    DataType = "Int32",
                    Value = 8
                },
                new() {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 2,
                        new Opc.Ua.Argument("test",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodBrowsePath = methodPath,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.Equal(50u, (uint)arg.Value!),
                arg => Assert.Equal(8, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test", -1, "desc", "result[2]"));
        }

        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndMethodIdAndBrowsePathTestAsync(CancellationToken ct = default)
        {
            var service = _services();
            const string objectId = "http://test.org/UA/Data/#i=2708";

            const string methodId = "http://test.org/UA/Data/#i=1974"; // Data
            var methodPath = new[] {
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest",
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "Variant",
                    Value = JsonNodeValueExtensions.FromObject(new {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new() {
                    DataType = "Int32",
                    Value = 8
                },
                new() {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 2,
                        new Opc.Ua.Argument("test",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodBrowsePath = methodPath,
                ObjectId = objectId,
                MethodId = methodId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.Equal(50u, (uint)arg.Value!),
                arg => Assert.Equal(8, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test", -1, "desc", "result[2]"));
        }

        public async Task NodeMethodCallStaticScalarMethod3WithObjectPathAndMethodIdAndBrowsePathTestAsync(CancellationToken ct = default)
        {
            var service = _services();
            var objectPath = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest"
            };

            const string methodId = "http://test.org/UA/Data/#i=1974"; // Data
            var methodPath = new[] {
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest",
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var input = new List<MethodCallArgumentModel> {
                new() {
                    DataType = "Variant",
                    Value = JsonNodeValueExtensions.FromObject(new {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new() {
                    DataType = "Int32",
                    Value = 8
                },
                new() {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 2,
                        new Opc.Ua.Argument("test",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodBrowsePath = methodPath,
                ObjectBrowsePath = objectPath,
                MethodId = methodId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.Equal(50u, (uint)arg.Value!),
                arg => Assert.Equal(8, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test", -1, "desc", "result[2]"));
        }

        public async Task NodeMethodCallStaticScalarMethod3WithObjectIdAndPathAndMethodIdAndPathTestAsync(CancellationToken ct = default)
        {
            var service = _services();
            const string objectId = "http://test.org/UA/Data/#i=1974"; // Data
            var objectPath = new[] {
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest"
            };

            const string methodId = "http://test.org/UA/Data/#i=1974"; // Data
            var methodPath = new[] {
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest",
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var input = new List<MethodCallArgumentModel>
            {
                new()
                {
                    DataType = "Variant",
                    Value = JsonNodeValueExtensions.FromObject(new
                    {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new()
                {
                    DataType = "Int32",
                    Value = 8
                },
                new()
                {
                    DataType = "ExtensionObject",
                    Value = JsonNodeValueExtensions.FromExtensionObject(
                        "i=296", 2,
                        new Opc.Ua.Argument("test",
                            new Opc.Ua.NodeId(Opc.Ua.DataTypes.String), -1, "desc")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext))
                }
            };

            // Act
            var result = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodBrowsePath = methodPath,
                ObjectBrowsePath = objectPath,
                MethodId = methodId,
                ObjectId = objectId,
                Arguments = input
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.Results,
                arg => Assert.Equal(50u, (uint)arg.Value!),
                arg => Assert.Equal(8, (int)arg.Value!),
                arg => JsonNodeAssert.IsArgument(
                    arg.Value, "test", -1, "desc", "result[2]"));
        }

        public async Task NodeMethodCallBoiler2ResetTestAsync(CancellationToken ct = default)
        {
            const string objectId = "nsu=http://opcfoundation.org/UA/Boiler/;i=1285";
            const string startMethodId = "nsu=http://opcfoundation.org/UA/Boiler/;i=1317";
            const string haltMethodId = "nsu=http://opcfoundation.org/UA/Boiler/;i=1320";
            const string resetMethodId = "nsu=http://opcfoundation.org/UA/Boiler/;i=1321";

            await ResetBoilerAsync(objectId, startMethodId,
                haltMethodId, resetMethodId, ct).ConfigureAwait(false);
        }

        public async Task NodeMethodCallBoiler1ResetTestAsync(CancellationToken ct = default)
        {
            const string objectId = "http://opcfoundation.org/UA/Boiler/#i=1285";
            const string startMethodId = "http://opcfoundation.org/UA/Boiler/#i=1317";
            const string haltMethodId = "http://opcfoundation.org/UA/Boiler/#i=1320";
            const string resetMethodId = "http://opcfoundation.org/UA/Boiler/#i=1321";

            await ResetBoilerAsync(objectId, startMethodId,
                haltMethodId, resetMethodId, ct).ConfigureAwait(false);
        }

        private async Task ResetBoilerAsync(string objectId,
            string startMethodId, string haltMethodId, string resetMethodId,
            CancellationToken ct)
        {
            var service = _services();
            var start = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = startMethodId,
                ObjectId = objectId
            }, ct).ConfigureAwait(false);
            if (start.ErrorInfo is not null)
            {
                Assert.Equal("BadNotExecutable", start.ErrorInfo.SymbolicId);
            }
            Assert.Empty(start.Results);

            var halt = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = haltMethodId,
                ObjectId = objectId
            }, ct).ConfigureAwait(false);
            Assert.Empty(halt.Results);
            Assert.Null(halt.ErrorInfo);

            var reset = await service.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                MethodId = resetMethodId,
                ObjectId = objectId
            }, ct).ConfigureAwait(false);
            Assert.Empty(reset.Results);
            Assert.Null(reset.ErrorInfo);
        }

        private readonly bool _newMetadata;
        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }

    internal static class JsonNodeAssert
    {
        public static void IsArgument(JsonNode? actual, string name,
            int valueRank, string description, string? context = null)
        {
            var expected = new JsonObject
            {
                ["Name"] = name,
                ["DataType"] = "i=12",
                ["ValueRank"] = valueRank,
                ["ArrayDimensions"] = null,
                ["Description"] = new JsonObject
                {
                    ["Text"] = description
                }
            };
            AreEqual(expected, actual, context);
        }

        public static void AreEqual(JsonNode? expected, JsonNode? actual, string? context = null)
        {
            var prefix = string.IsNullOrEmpty(context) ? string.Empty : $"{context}: ";

            Assert.True(JsonNode.DeepEquals(expected, actual),
                $"{prefix}Expected: {expected} != Actual: {actual}");
        }

        public static void AreSequenceEqual(
            IEnumerable<JsonNode?> expected,
            IEnumerable<JsonNode?> actual,
            string? context = null)
        {
            var expectedNodes = expected.ToArray();
            var actualNodes = actual.ToArray();
            var prefix = string.IsNullOrEmpty(context) ? string.Empty : $"{context}: ";

            Assert.True(expectedNodes.Length == actualNodes.Length,
                $"{prefix}Expected {expectedNodes.Length} item(s) but found {actualNodes.Length}.");

            for (var index = 0; index < expectedNodes.Length; index++)
            {
                var itemContext = string.IsNullOrEmpty(context) ? $"[{index}]" : $"{context}[{index}]";

                AreEqual(expectedNodes[index], actualNodes[index], itemContext);
            }
        }
    }
}
