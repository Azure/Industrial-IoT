// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class CallArrayMethodTests<T> {

        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        public CallArrayMethodTests(Func<INodeServices<T>> services, T endpoint) {
            _services = services;
            _endpoint = endpoint;
        }

        public async Task NodeMethodMetadataStaticArrayMethod1Test() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodGetMetadataAsync(_endpoint,
                new MethodMetadataRequestModel {
                    MethodId = methodId
                });

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments,
                arg => {
                    Assert.Equal("BooleanIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("SByteIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int16In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt16In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int32In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt32In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int64In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt64In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("FloatIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DoubleIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("BooleanOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("SByteOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int16Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt16Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int32Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt32Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("Int64Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("UInt64Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("FloatOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DoubleOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
        }


        public async Task NodeMethodMetadataStaticArrayMethod2Test() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodGetMetadataAsync(_endpoint,
                new MethodMetadataRequestModel {
                    MethodId = methodId
                });

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments,
                arg => {
                    Assert.Equal("StringIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DateTimeIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("GuidIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteStringIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("XmlElementIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("NodeIdIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ExpandedNodeIdIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("QualifiedNameIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("LocalizedTextIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StatusCodeIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("StringOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("DateTimeOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("GuidOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ByteStringOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("XmlElementOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("NodeIdOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("ExpandedNodeIdOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("QualifiedNameOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("LocalizedTextOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StatusCodeOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
        }


        public async Task NodeMethodMetadataStaticArrayMethod3Test() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10771";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodGetMetadataAsync(_endpoint,
                new MethodMetadataRequestModel {
                    MethodId = methodId
                });

            // Assert
            Assert.Equal(objectId, result.ObjectId);
            Assert.Collection(result.InputArguments,
                arg => {
                    Assert.Equal("VariantIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("EnumerationIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StructureIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("VariantOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("EnumerationOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                },
                arg => {
                    Assert.Equal("StructureOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.Equal("[]", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                });
        }


        public async Task NodeMethodCallStaticArrayMethod1Test1() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = JToken.FromObject(
                        new bool[] { true, false, true, true, false })
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = JToken.FromObject(
                        new sbyte[] { 1, 2, 3, 4, 5, -1, -2, -3 })
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = JToken.FromObject(
                        Encoding.UTF8.GetBytes("testtesttest"))
                },
                new MethodCallArgumentModel {
                    DataType = "Int16",
                    Value = JToken.FromObject(
                        new short[] { short.MinValue, short.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "UInt16",
                    Value = JToken.FromObject(
                        new ushort[] { ushort.MinValue, ushort.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = JToken.FromObject(
                        new int[] { int.MinValue, int.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "uInt32",
                    Value = JToken.FromObject(
                        new uint[] { uint.MinValue, uint.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "Int64",
                    Value = JToken.FromObject(
                        new long[] { long.MinValue, long.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "uint64",
                    Value = JToken.FromObject(
                        new ulong[] { ulong.MinValue, ulong.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "float",
                    Value = JToken.FromObject(
                        new float[] { float.MinValue, float.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "DOUBLE",
                    Value = JToken.FromObject(
                        new double[] { double.MinValue, double.MaxValue, 0, 2 })
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "Boolean", "SByte", "ByteString", "Int16", "UInt16",
                "Int32", "UInt32", "Int64", "UInt64", "Float", "Double"
            }, result.Results.Select(arg => arg.DataType));
            Assert.Equal(input.Select(arg => arg.Value.ToString(Formatting.None)),
                result.Results.Select(arg => arg.Value.ToString(Formatting.None)),
                StringComparer.InvariantCulture);
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test2() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = JToken.FromObject(
                        new bool[] { true, false, true, true, false })
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = JToken.FromObject(
                        new sbyte[] { 1, 2, 3, 4, 5, -1, -2, -3 })
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = JToken.FromObject(
                        Encoding.UTF8.GetBytes("testtesttest"))
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "Boolean", "SByte", "ByteString", "Int16", "UInt16",
                "Int32", "UInt32", "Int64", "UInt64", "Float", "Double"
            }, result.Results.Select(arg => arg.DataType));
            Assert.Collection(result.Results,
                arg => {
                    Assert.Equal(input[0].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None));
                },
                arg => {
                    Assert.Equal(input[1].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None));
                },
                arg => {
                    Assert.Equal(input[2].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None));
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                });
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test3() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId
                });

            // Assert
            Assert.Equal(new List<string> {
                "Boolean", "SByte", "ByteString", "Int16", "UInt16",
                "Int32", "UInt32", "Int64", "UInt64", "Float", "Double"
            }, result.Results.Select(arg => arg.DataType));
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.Empty((JArray)arg.Value));
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test4() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = JToken.FromObject(
                        new bool[] { true, false, true, true, false })
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = JToken.FromObject(
                        new sbyte[] { 1, 2, 3, 4, 5, -1, -2, -3 })
                },
                new MethodCallArgumentModel {
                    DataType = "byte",
                    Value = JToken.FromObject(
                        new ushort[] { 0, 1, 2, 3, 4, 5, 6, byte.MaxValue })
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
                    Value = JToken.FromObject(
                        new double[] { 1234.4567, 23.34, 33 })
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "Boolean", "SByte", "ByteString", "Int16", "UInt16",
                "Int32", "UInt32", "Int64", "UInt64", "Float", "Double"
            }, result.Results.Select(arg => arg.DataType));
            Assert.Collection(result.Results,
                arg => {
                    Assert.Equal(input[0].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[1].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(JToken.FromObject(
                        new byte[] { 0, 1, 2, 3, 4, 5, 6, byte.MaxValue }),
                        arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Equal(input[10].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                });
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test5() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";
            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = JToken.FromObject(new bool[0])
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = JToken.FromObject(new sbyte[0])
                },
                new MethodCallArgumentModel {
                    DataType = "Byte",
                    Value = "[]"
                },
                new MethodCallArgumentModel {
                    DataType = "Int16",
                    Value = JToken.FromObject(new short[0])
                },
                new MethodCallArgumentModel {
                    DataType = "UInt16",
                    Value = JToken.FromObject(new ushort[0])
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = JToken.FromObject(new int[0])
                },
                new MethodCallArgumentModel {
                    DataType = "uInt32",
                    Value = JToken.FromObject(new uint[0])
                },
                new MethodCallArgumentModel {
                    DataType = "Int64",
                    Value = JToken.FromObject(new long[0])
                },
                new MethodCallArgumentModel {
                    DataType = "uint64",
                    Value = JToken.FromObject(new ulong[0])
                },
                new MethodCallArgumentModel {
                    DataType = "float",
                    Value = JToken.FromObject(new float[0])
                },
                new MethodCallArgumentModel {
                    DataType = "DOUBLE",
                    Value = JToken.FromObject(new double[0])
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "Boolean", "SByte", "ByteString", "Int16", "UInt16",
                "Int32", "UInt32", "Int64", "UInt64", "Float", "Double"
            }, result.Results.Select(arg => arg.DataType));
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
            Assert.Collection(result.Results,
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Equal(JTokenType.Null, arg.Value.Type);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Empty((JArray)arg.Value);
                },
                arg => {
                    Assert.Equal(input[10].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                });
        }


        public async Task NodeMethodCallStaticArrayMethod2Test1() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "String",
                    Value = JToken.FromObject(new string[] {
                        "!adfasdfsdf!",
                        "!46!",
                        "!asdf!",
                        "!adfasdfsdf!",
                        "sfdgsfdgs",
                        "ÜÖÄ"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "DateTime",
                    Value = JToken.FromObject(new DateTime[] {
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        DateTime.UtcNow
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "Guid",
                    Value = JToken.FromObject(new Guid[] {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid()
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = JToken.FromObject(new byte[][] {
                        Encoding.UTF8.GetBytes("!adfasdfsdf!"),
                        Encoding.UTF8.GetBytes("!46!"),
                        Encoding.UTF8.GetBytes("!asdf!"),
                        Encoding.UTF8.GetBytes("!adfasdfsdf!"),
                        Encoding.UTF8.GetBytes("sfdgsfdgs"),
                        Encoding.UTF8.GetBytes("ÜÖÄ")
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "XmlElement",
                    Value = JToken.FromObject(new System.Xml.XmlElement[] {
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "NodeId",
                    Value = JToken.FromObject(new string[]{
                        "byte",
                        "http://test.org/#i=23534",
                        "http://muh.test/#s=35645",
                        "http://test.org/test/#i=354"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "ExpandedNodeId",
                    Value = JToken.FromObject(new string[] {
                        "byte",
                        "http://test.org/#i=23534",
                        "http://muh.test/#s=35645",
                        "http://test.org/test/#i=354"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "QualifiedName",
                    Value = JToken.FromObject(new string[] {
                        "http://test.org/#qn1",
                        "http://test.org/#qn2",
                        "http://test.org/#qn3",
                        "test"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "LocalizedText",
                    Value = JToken.FromObject(new object[] {
                        new {
                            Text = "Hällö",
                            Locale = "de"
                        },
                        new {
                            Text = "Hallo"
                        },
                        new {
                            Text = "Hello",
                            Locale = "en"
                        }
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "StatusCode",
                    Value = JToken.FromObject(new object[] {
                        new {
                            Symbol = "BadEndOfStream",
                            Code = 0x80B00000
                        },
                        new {
                            Symbol = "BadWaitingForResponse",
                            Code = 0x80B20000
                        },
                        new {
                            Symbol = "BadOperationAbandoned",
                            Code = 0x80B30000
                        }
                    })
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "String", "DateTime", "Guid", "ByteString",
                "XmlElement", "NodeId", "ExpandedNodeId",
                "QualifiedName","LocalizedText","StatusCode" },
                result.Results.Select(arg => arg.DataType));
            Assert.Collection(result.Results,
                arg => {
                    Assert.Equal(input[0].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[1].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[2].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[3].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[4].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[5].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[6].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[7].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[8].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                },
                arg => {
                    Assert.Equal(input[9].Value.ToString(Formatting.None),
                        arg.Value.ToString(Formatting.None), true);
                });
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod2Test2() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId
                });

            // Assert
            Assert.Equal(new List<string> {
                "String", "DateTime", "Guid", "ByteString",
                "XmlElement", "NodeId", "ExpandedNodeId",
                "QualifiedName","LocalizedText","StatusCode" },
                result.Results.Select(arg => arg.DataType));
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
            Assert.All(result.Results, arg => Assert.Empty((JArray)arg.Value));
        }


        public async Task NodeMethodCallStaticArrayMethod2Test3() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new MethodCallArgumentModel {
                    DataType = "LocalizedText",
                    Value = JToken.FromObject(new string[] {
                        "unloc1",
                        "unloc2",
                        "unloc3"
                    })
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "String", "DateTime", "Guid", "ByteString",
                "XmlElement", "NodeId", "ExpandedNodeId",
                "QualifiedName","LocalizedText","StatusCode" },
                result.Results.Select(arg => arg.DataType));
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
            Assert.Equal(3, ((JArray)result.Results[8].Value).Count);
        }


        public async Task NodeMethodCallStaticArrayMethod2Test4() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "String",
                    Value = JToken.FromObject(new string[0])
                },
                new MethodCallArgumentModel {
                    DataType = "DateTime",
                    Value = JToken.FromObject(new DateTime[0])
                },
                new MethodCallArgumentModel {
                    DataType = "Guid",
                    Value = JToken.FromObject(new Guid[0])
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = JToken.FromObject(new byte[0,0])
                },
                new MethodCallArgumentModel {
                    DataType = "XmlElement",
                    Value = JToken.FromObject(new System.Xml.XmlElement[0])
                },
                new MethodCallArgumentModel {
                    DataType = "NodeId",
                    Value = JToken.FromObject(new string[0])
                },
                new MethodCallArgumentModel {
                    DataType = "ExpandedNodeId",
                    Value = JToken.FromObject(new string[0])
                },
                new MethodCallArgumentModel {
                    DataType = "QualifiedName",
                    Value = JToken.FromObject(new string[0])
                },
                new MethodCallArgumentModel {
                    DataType = "LocalizedText",
                    Value = JToken.FromObject(new object[0])
                },
                new MethodCallArgumentModel {
                    DataType = "StatusCode",
                    Value = JToken.FromObject(new int[0])
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "String", "DateTime", "Guid", "ByteString",
                "XmlElement", "NodeId", "ExpandedNodeId",
                "QualifiedName", "LocalizedText", "StatusCode" },
                result.Results.Select(arg => arg.DataType));
            Assert.Equal(input.Select(arg => arg.Value.ToString(Formatting.None)),
                result.Results.Select(arg => arg.Value.ToString(Formatting.None)),
                StringComparer.InvariantCulture);
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
            Assert.All(result.Results, arg => Assert.Empty((JArray)arg.Value));
        }


        public async Task NodeMethodCallStaticArrayMethod3Test1() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10771";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "Variant",
                    Value = new JArray()
                },
                new MethodCallArgumentModel {
                    DataType = "Enumeration",
                    Value = new JArray()
                },
                new MethodCallArgumentModel {
                    DataType = "ExtensionObject",
                    Value = new JArray()
                }
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "Variant", "Int32", "ExtensionObject"
            }, result.Results.Select(arg => arg.DataType));
            Assert.Equal(input.Select(arg => arg.Value.ToString(Formatting.None)),
                result.Results.Select(arg => arg.Value.ToString(Formatting.None)),
                StringComparer.InvariantCulture);
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod3Test2() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10771";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    Value = JToken.FromObject(new object[] {
                        new {
                            Type = "Uint32",
                            Body = 500000
                        },
                        new {
                            Type = "string",
                            Body = "test"
                        },
                        new {
                            Type = "Float",
                            Body = 0.3f
                        },
                        new {
                            Type = "byte",
                            Body = 50
                        }
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = JToken.FromObject(new int[] { 1, 2, 3, 4, 5, 6 })
                },
                new MethodCallArgumentModel {
                    DataType = "ExtensionObject",
                    Value = JToken.FromObject(new object[] {
                        new {
                            TypeId = "http://test.org/#s=test2",
                            Body = new Opc.Ua.Argument("test1", Opc.Ua.DataTypes.String, -1, "desc1")
                                .AsBinary(Opc.Ua.ServiceMessageContext.GlobalContext)
                        },
                        new {
                            TypeId = "http://test.org/#s=test55",
                            Encoding = "Xml",
                            Body = new Opc.Ua.Argument("test2", Opc.Ua.DataTypes.String, -2, "desc1")
                                .AsXmlElement(Opc.Ua.ServiceMessageContext.GlobalContext)
                        }
                    })
                },
            };

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId,
                    Arguments = input
                });

            // Assert
            Assert.Equal(new List<string> {
                "Variant", "Int32", "ExtensionObject"
            }, result.Results.Select(arg => arg.DataType));
            Assert.Equal(input.Select(arg => arg.Value.ToString(Formatting.None)),
                result.Results.Select(arg => arg.Value.ToString(Formatting.None)),
                StringComparer.InvariantCultureIgnoreCase);
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
        }


        public async Task NodeMethodCallStaticArrayMethod3Test3() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10771";
            var objectId = "http://test.org/UA/Data/#i=10755";

            // Act
            var result = await service.NodeMethodCallAsync(_endpoint,
                new MethodCallRequestModel {
                    MethodId = methodId,
                    ObjectId = objectId
                });

            // Assert
            Assert.Equal(new List<string> {
                "Variant", "Int32", "ExtensionObject"
            }, result.Results.Select(arg => arg.DataType));
            Assert.All(result.Results, arg => Assert.Equal(JTokenType.Array, arg.Value.Type));
            Assert.All(result.Results, arg => Assert.Empty((JArray)arg.Value));
        }

        private readonly T _endpoint;
        private readonly Func<INodeServices<T>> _services;
    }
}
