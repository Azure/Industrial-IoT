// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteCollection.Name)]
    public class AddressSpaceValueCallScalarTests {

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod1Test() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10756";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodGetMetadataAsync(GetEndpoint(),
                new MethodMetadataRequestModel {
                    MethodId = methodId
                });

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments,
                arg => {
                    Assert.Equal("BooleanIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("SByteIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int16In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt16In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int32In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt32In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int64In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt64In", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("FloatIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DoubleIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("BooleanOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("SByteOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int16Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt16Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int32Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt32Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int64Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt64Out", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("FloatOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DoubleOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod2Test() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10759";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodGetMetadataAsync(GetEndpoint(),
                new MethodMetadataRequestModel {
                    MethodId = methodId
                });

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments,
                arg => {
                    Assert.Equal("StringIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DateTimeIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("GuidIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteStringIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("XmlElementIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("NodeIdIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ExpandedNodeIdIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("QualifiedNameIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("LocalizedTextIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StatusCodeIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("StringOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DateTimeOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("GuidOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteStringOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("XmlElementOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("NodeIdOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ExpandedNodeIdOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("QualifiedNameOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("LocalizedTextOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StatusCodeOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
        }

        [Fact]
        public async Task NodeMethodMetadataStaticScalarMethod3Test() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10762";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodGetMetadataAsync(GetEndpoint(),
                new MethodMetadataRequestModel {
                    MethodId = methodId
                });

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments,
                arg => {
                    Assert.Equal("VariantIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("EnumerationIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StructureIn", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("VariantOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("EnumerationOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StructureOut", arg.Name);
                    Assert.Null(arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test1() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10756";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = true
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = -1
                },
                new MethodCallArgumentModel {
                    DataType = "byte",
                    Value = 244
                },
                new MethodCallArgumentModel {
                    DataType = "Int16",
                    Value = short.MinValue
                },
                new MethodCallArgumentModel {
                    DataType = "UInt16",
                    Value = 0
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = int.MinValue
                },
                new MethodCallArgumentModel {
                    DataType = "uInt32",
                    Value = uint.MaxValue
                },
                new MethodCallArgumentModel {
                    DataType = "Int64",
                    Value = -55555
                },
                new MethodCallArgumentModel {
                    DataType = "uint64",
                    Value = 55555
                },
                new MethodCallArgumentModel {
                    DataType = "float",
                    Value = 12.898345f
                },
                new MethodCallArgumentModel {
                    DataType = "DOUBLE",
                    Value = 1234.4567
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.True((bool)arg.Value);
                },
                arg => {
                    Assert.Equal((sbyte)input[1].Value, (sbyte)arg.Value);
                },
                arg => {
                    Assert.Equal((byte)input[2].Value, (byte)arg.Value);
                },
                arg => {
                    Assert.Equal(short.MinValue, (short)arg.Value);
                },
                arg => {
                    Assert.Equal((ushort)0, (ushort)arg.Value);
                },
                arg => {
                    Assert.Equal(int.MinValue, (int)arg.Value);
                },
                arg => {
                    Assert.Equal(uint.MaxValue, (uint)arg.Value);
                },
                arg => {
                    Assert.Equal((long)input[7].Value, (long)arg.Value);
                },
                arg => {
                    Assert.Equal((ulong)input[8].Value, (ulong)arg.Value);
                },
                arg => {
                    Assert.Equal((float)input[9].Value, (float)arg.Value);
                },
                arg => {
                    Assert.Equal((double)input[10].Value, (double)arg.Value);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test2() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10756";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = false
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = -100
                },
                new MethodCallArgumentModel {
                    DataType = "byte",
                    Value = 100
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.False((bool)arg.Value);
                },
                arg => {
                    Assert.Equal((sbyte)input[1].Value, (sbyte)arg.Value);
                },
                arg => {
                    Assert.Equal((byte)input[2].Value, (byte)arg.Value);
                },
                arg => {
                    Assert.Equal((short)0, (short)arg.Value);
                },
                arg => {
                    Assert.Equal((ushort)0, (ushort)arg.Value);
                },
                arg => {
                    Assert.Equal(0, (int)arg.Value);
                },
                arg => {
                    Assert.Equal((uint)0, (uint)arg.Value);
                },
                arg => {
                    Assert.Equal((long)0, (long)arg.Value);
                },
                arg => {
                    Assert.Equal((ulong)0, (ulong)arg.Value);
                },
                arg => {
                    Assert.Equal((float)0, (float)arg.Value);
                },
                arg => {
                    Assert.Equal((double)0, (double)arg.Value);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test3() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10756";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.False((bool)arg.Value);
                },
                arg => {
                    Assert.Equal((sbyte)0, (sbyte)arg.Value);
                },
                arg => {
                    Assert.Equal((byte)0, (byte)arg.Value);
                },
                arg => {
                    Assert.Equal((short)0, (short)arg.Value);
                },
                arg => {
                    Assert.Equal((ushort)0, (ushort)arg.Value);
                },
                arg => {
                    Assert.Equal(0, (int)arg.Value);
                },
                arg => {
                    Assert.Equal((uint)0, (uint)arg.Value);
                },
                arg => {
                    Assert.Equal((long)0, (long)arg.Value);
                },
                arg => {
                    Assert.Equal((ulong)0, (ulong)arg.Value);
                },
                arg => {
                    Assert.Equal((float)0, (float)arg.Value);
                },
                arg => {
                    Assert.Equal((double)0, (double)arg.Value);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod1Test4() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10756";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = true
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = -1
                },
                new MethodCallArgumentModel {
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
                new MethodCallArgumentModel {
                    DataType = "DOUBLE",
                    Value = 1234.4567
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.True((bool)arg.Value);
                },
                arg => {
                    Assert.Equal((sbyte)input[1].Value, (sbyte)arg.Value);
                },
                arg => {
                    Assert.Equal((byte)input[2].Value, (byte)arg.Value);
                },
                arg => {
                    Assert.Equal((short)0, (short)arg.Value);
                },
                arg => {
                    Assert.Equal((ushort)0, (ushort)arg.Value);
                },
                arg => {
                    Assert.Equal(0, (int)arg.Value);
                },
                arg => {
                    Assert.Equal((uint)0, (uint)arg.Value);
                },
                arg => {
                    Assert.Equal((long)0, (long)arg.Value);
                },
                arg => {
                    Assert.Equal((ulong)0, (ulong)arg.Value);
                },
                arg => {
                    Assert.Equal((float)0, (float)arg.Value);
                },
                arg => {
                    Assert.Equal((double)input[10].Value, (double)arg.Value);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test1() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10759";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "String",
                    Value = "test"
                },
                new MethodCallArgumentModel {
                    DataType = "DateTime",
                    Value = DateTime.UtcNow
                },
                new MethodCallArgumentModel {
                    DataType = "Guid",
                    Value = Guid.NewGuid().ToString()
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = Encoding.UTF32.GetBytes("asdfasdfadsfs")
                },
                new MethodCallArgumentModel {
                    DataType = "XmlElement",
                    Value = null
                },
                new MethodCallArgumentModel {
                    DataType = "NodeId",
                    Value = "http://test.org/#i=44"
                },
                new MethodCallArgumentModel {
                    DataType = "ExpandedNodeId",
                    Value = "http://test.org/#i=45"
                },
                new MethodCallArgumentModel {
                    DataType = "QualifiedName",
                    Value = "http://test.org/#name"
                },
                new MethodCallArgumentModel {
                    DataType = "LocalizedText",
                    Value = JToken.FromObject(new {
                        Locale = "de",
                        Text = "Hallo Welt"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "StatusCode",
                    Value = 8888888
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.Equal((string)input[0].Value, (string)arg.Value);
                    Assert.Equal(input[0].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal((string)input[1].Value, (string)arg.Value);
                    Assert.Equal(input[1].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal((string)input[2].Value, (string)arg.Value);
                    Assert.Equal(input[2].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal((string)input[3].Value, (string)arg.Value);
                    Assert.Equal(input[3].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(input[4].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal((string)input[5].Value, (string)arg.Value);
                    Assert.Equal(input[5].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal((string)input[6].Value, (string)arg.Value);
                    Assert.Equal(input[6].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal((string)input[7].Value, (string)arg.Value);
                    Assert.Equal(input[7].DataType, arg.DataType);
                },
                arg => {
                    Assert.True(JToken.DeepEquals(input[8].Value, arg.Value),
                        $"Expected: {input[8].Value} != Actual: {arg.Value}");
                    Assert.Equal(input[8].DataType, arg.DataType);
                },
                arg => {
                    Assert.Equal(8888888, (int)arg.Value);
                    Assert.Equal(input[9].DataType, arg.DataType);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod2Test2() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10759";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var types = new List<string> {
                "String", "DateTime", "Guid", "ByteString",
                "XmlElement", "NodeId", "ExpandedNodeId",
                "QualifiedName","LocalizedText","StatusCode" };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId
                });

            // Assert
           Assert.Collection(result.Results,
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[0], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[1], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[2], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[3], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[4], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[5], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[6], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[7], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[8], arg.DataType);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                    Assert.Equal(types[9], arg.DataType);
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test1() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10762";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "Variant",
                    Value = JToken.FromObject(new {
                        Type = "Uint32",
                        Body = 50
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "Enumeration",
                    Value = 8
                },
                new MethodCallArgumentModel {
                    DataType = "ExtensionObject",
                    Value = JToken.FromObject(new {
                        Encoding = "Xml",
                        TypeId = "http://test.org/#s=test",
                        Body = new Opc.Ua.Argument("test", Opc.Ua.DataTypes.String, -1, "desc")
                            .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext)
                    })
                },
            };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.Equal(50u, (uint)arg.Value);
                },
                arg => {
                    Assert.Equal(8, (int)arg.Value);
                },
                arg => {
                    Assert.True(JToken.DeepEquals(input[2].Value, arg.Value),
                        $"Expected: {input[2].Value} != Actual: {arg}");
                });
        }

        [Fact]
        public async Task NodeMethodCallStaticScalarMethod3Test2() {

            var service = GetServices();
            var methodId = "http://test.org/UA/Data/#i=10762";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "String",
                    Value = "varianttest"
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = 9999
                },
                new MethodCallArgumentModel {
                    DataType = "ExtensionObject",
                    Value = JToken.FromObject(new {
                        TypeId = "http://test.org/#s=test2",
                        Body = new Opc.Ua.Argument("test1", Opc.Ua.DataTypes.String, -1, "desc1")
                            .AsBinary(Opc.Ua.ServiceMessageContext.GlobalContext)
                    })
                },
            };

            // Act
            var result = await service.NodeMethodCallAsync(GetEndpoint(),
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Collection(result.Results,
                arg => {
                    Assert.Equal("varianttest", (string)arg.Value);
                },
                arg => {
                    Assert.Equal(9999, (int)arg.Value);
                },
                arg => {
                    Assert.Equal(input[2].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None));
                });
        }

        public AddressSpaceValueCallScalarTests(ServerFixture server) {
            _server = server;
        }

        private INodeServices<EndpointModel> GetServices() {
            return new AddressSpaceServices(_server.Client,
                new JsonVariantEncoder(), _server.Logger);
        }

        private EndpointModel GetEndpoint() {
            return new EndpointModel {
                Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
            };
        }

        private readonly ServerFixture _server;
    }
}
