// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
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
            _serializer = new NewtonSoftJsonSerializer();
        }

        public async Task NodeMethodMetadataStaticArrayMethod1TestAsync() {

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
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("SByteIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("ByteIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("Int16In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("UInt16In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("Int32In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("UInt32In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("Int64In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("UInt64In", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("FloatIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("DoubleIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("BooleanOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Boolean", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Boolean", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("SByteOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("SByte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("SByte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("ByteOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Byte", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Byte", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("Int16Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("UInt16Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt16", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt16", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("Int32Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("UInt32Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt32", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt32", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("Int64Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Int64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Int64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("UInt64Out", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("UInt64", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("UInt64", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("FloatOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Float", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Float", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("DoubleOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Double", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Double", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }


        public async Task NodeMethodMetadataStaticArrayMethod2TestAsync() {

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
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("DateTimeIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("GuidIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("ByteStringIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("XmlElementIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("NodeIdIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("ExpandedNodeIdIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("QualifiedNameIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("LocalizedTextIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("StatusCodeIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("StringOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("String", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("String", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("DateTimeOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("DateTime", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("DateTime", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("GuidOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Guid", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Guid", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("ByteStringOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ByteString", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ByteString", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("XmlElementOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("XmlElement", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("XmlElement", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("NodeIdOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("NodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("NodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("ExpandedNodeIdOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExpandedNodeId", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("ExpandedNodeId", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("QualifiedNameOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("QualifiedName", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("QualifiedName", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("LocalizedTextOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("LocalizedText", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("LocalizedText", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("StatusCodeOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("StatusCode", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("StatusCode", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }


        public async Task NodeMethodMetadataStaticArrayMethod3TestAsync() {

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
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("EnumerationIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("StructureIn", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
            Assert.Collection(result.OutputArguments,
                arg => {
                    Assert.Equal("VariantOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Variant", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("BaseDataType", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("EnumerationOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("Enumeration", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Enumeration", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                },
                arg => {
                    Assert.Equal("StructureOut", arg.Name);
                    Assert.Equal(NodeValueRank.OneDimension, arg.ValueRank);
                    Assert.NotNull(arg.ArrayDimensions);
                    Assert.Single(arg.ArrayDimensions);
                    Assert.Equal(0u, arg.ArrayDimensions[0]);
                    Assert.Equal(NodeClass.DataType, arg.Type.NodeClass);
                    Assert.Equal("ExtensionObject", arg.Type.NodeId);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal("Structure", arg.Type.DisplayName);
                    Assert.True(arg.DefaultValue.IsNull());
                });
        }


        public async Task NodeMethodCallStaticArrayMethod1Test1Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = _serializer.FromObject(
                        new bool[] { true, false, true, true, false })
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = _serializer.FromObject(
                        new sbyte[] { 1, 2, 3, 4, 5, -1, -2, -3 })
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = _serializer.FromObject(
                        Encoding.UTF8.GetBytes("testtesttest"))
                },
                new MethodCallArgumentModel {
                    DataType = "Int16",
                    Value = _serializer.FromObject(
                        new short[] { short.MinValue, short.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "UInt16",
                    Value = _serializer.FromObject(
                        new ushort[] { ushort.MinValue, ushort.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = _serializer.FromObject(
                        new int[] { int.MinValue, int.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "uInt32",
                    Value = _serializer.FromObject(
                        new uint[] { uint.MinValue, uint.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "Int64",
                    Value = _serializer.FromObject(
                        new long[] { long.MinValue, long.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "uint64",
                    Value = _serializer.FromObject(
                        new ulong[] { ulong.MinValue, ulong.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "float",
                    Value = _serializer.FromObject(
                        new float[] { float.MinValue, float.MaxValue, 0, 2 })
                },
                new MethodCallArgumentModel {
                    DataType = "DOUBLE",
                    Value = _serializer.FromObject(
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
            Assert.Equal(input.Select(arg => arg.Value),
                result.Results.Select(arg => arg.Value));
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test2Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = _serializer.FromObject(
                        new bool[] { true, false, true, true, false })
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = _serializer.FromObject(
                        new sbyte[] { 1, 2, 3, 4, 5, -1, -2, -3 })
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = _serializer.FromObject(
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
                    Assert.Equal(input[0].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[1].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[2].Value, arg.Value);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                });
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test3Async() {

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
                arg => Assert.Empty(arg.Value.Values));
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test4Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = _serializer.FromObject(
                        new bool[] { true, false, true, true, false })
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = _serializer.FromObject(
                        new sbyte[] { 1, 2, 3, 4, 5, -1, -2, -3 })
                },
                new MethodCallArgumentModel {
                    DataType = "byte",
                    Value = _serializer.FromObject(
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
                    Value = _serializer.FromObject(
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
                    Assert.Equal(input[0].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[1].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(_serializer.FromObject(
                        new byte[] { 0, 1, 2, 3, 4, 5, 6, byte.MaxValue }),
                        arg.Value);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Equal(input[10].Value, arg.Value);
                });
            Assert.All(result.Results.Where(arg => arg.DataType != "ByteString"),
                arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod1Test5Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10765";
            var objectId = "http://test.org/UA/Data/#i=10755";
            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "boolean",
                    Value = _serializer.FromObject(Array.Empty<bool>())
                },
                new MethodCallArgumentModel {
                    DataType = "sbyte",
                    Value = _serializer.FromObject(Array.Empty<sbyte>())
                },
                new MethodCallArgumentModel {
                    DataType = "Byte",
                    Value = _serializer.FromObject("[]")
                },
                new MethodCallArgumentModel {
                    DataType = "Int16",
                    Value = _serializer.FromObject(Array.Empty<short>())
                },
                new MethodCallArgumentModel {
                    DataType = "UInt16",
                    Value = _serializer.FromObject(Array.Empty<ushort>())
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = _serializer.FromObject(Array.Empty<int>())
                },
                new MethodCallArgumentModel {
                    DataType = "uInt32",
                    Value = _serializer.FromObject(Array.Empty<uint>())
                },
                new MethodCallArgumentModel {
                    DataType = "Int64",
                    Value = _serializer.FromObject(Array.Empty<long>())
                },
                new MethodCallArgumentModel {
                    DataType = "uint64",
                    Value = _serializer.FromObject(Array.Empty<ulong>())
                },
                new MethodCallArgumentModel {
                    DataType = "float",
                    Value = _serializer.FromObject(Array.Empty<float>())
                },
                new MethodCallArgumentModel {
                    DataType = "DOUBLE",
                    Value = _serializer.FromObject(Array.Empty<double>())
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
                arg => Assert.True(arg.Value.IsListOfValues));
            Assert.Collection(result.Results,
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.True(arg.Value.IsNull());
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Empty(arg.Value.Values);
                },
                arg => {
                    Assert.Equal(input[10].Value, arg.Value);
                });
        }


        public async Task NodeMethodCallStaticArrayMethod2Test1Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "String",
                    Value = _serializer.FromObject(new string[] {
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
                    Value = _serializer.FromObject(new DateTime[] {
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        DateTime.UtcNow
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "Guid",
                    Value = _serializer.FromObject(new Guid[] {
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
                    Value = _serializer.FromObject(new byte[][] {
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
                    Value = _serializer.FromObject(Array.Empty<System.Xml.XmlElement>())
                },
                new MethodCallArgumentModel {
                    DataType = "NodeId",
                    Value = _serializer.FromObject(new string[]{
                        "byte",
                        "http://test.org/#i=23534",
                        "http://muh.test/#s=35645",
                        "http://test.org/test/#i=354"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "ExpandedNodeId",
                    Value = _serializer.FromObject(new string[] {
                        "byte",
                        "http://test.org/#i=23534",
                        "http://muh.test/#s=35645",
                        "http://test.org/test/#i=354"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "QualifiedName",
                    Value = _serializer.FromObject(new string[] {
                        "http://test.org/#qn1",
                        "http://test.org/#qn2",
                        "http://test.org/#qn3",
                        "test"
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "LocalizedText",
                    Value = _serializer.FromObject(new object[] {
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
                    Value = _serializer.FromObject(new object[] {
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
                    Assert.Equal(input[0].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[1].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[2].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[3].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[4].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[5].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[6].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[7].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[8].Value, arg.Value);
                },
                arg => {
                    Assert.Equal(input[9].Value, arg.Value);
                });
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod2Test2Async() {

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
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
            Assert.All(result.Results, arg => Assert.Empty(arg.Value.Values));
        }


        public async Task NodeMethodCallStaticArrayMethod2Test3Async() {

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
                    Value = _serializer.FromObject(new string[] {
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
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
            Assert.Equal(3, result.Results[8].Value.Count);
        }


        public async Task NodeMethodCallStaticArrayMethod2Test4Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10768";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "String",
                    Value = _serializer.FromObject(Array.Empty<string>())
                },
                new MethodCallArgumentModel {
                    DataType = "DateTime",
                    Value = _serializer.FromObject(Array.Empty<DateTime>())
                },
                new MethodCallArgumentModel {
                    DataType = "Guid",
                    Value = _serializer.FromObject(Array.Empty<Guid>())
                },
                new MethodCallArgumentModel {
                    DataType = "ByteString",
                    Value = _serializer.FromObject(new byte[0,0])
                },
                new MethodCallArgumentModel {
                    DataType = "XmlElement",
                    Value = _serializer.FromObject(Array.Empty<System.Xml.XmlElement>())
                },
                new MethodCallArgumentModel {
                    DataType = "NodeId",
                    Value = _serializer.FromObject(Array.Empty<string>())
                },
                new MethodCallArgumentModel {
                    DataType = "ExpandedNodeId",
                    Value = _serializer.FromObject(Array.Empty<string>())
                },
                new MethodCallArgumentModel {
                    DataType = "QualifiedName",
                    Value = _serializer.FromObject(Array.Empty<string>())
                },
                new MethodCallArgumentModel {
                    DataType = "LocalizedText",
                    Value = _serializer.FromObject(Array.Empty<object>())
                },
                new MethodCallArgumentModel {
                    DataType = "StatusCode",
                    Value = _serializer.FromObject(Array.Empty<int>())
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
            Assert.Equal(input.Select(arg => arg.Value),
                result.Results.Select(arg => arg.Value));
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
            Assert.All(result.Results, arg => Assert.Empty(arg.Value.Values));
        }


        public async Task NodeMethodCallStaticArrayMethod3Test1Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10771";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    DataType = "Variant",
                    Value = _serializer.FromArray()
                },
                new MethodCallArgumentModel {
                    DataType = "Enumeration",
                    Value = _serializer.FromArray()
                },
                new MethodCallArgumentModel {
                    DataType = "ExtensionObject",
                    Value = _serializer.FromArray()
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
            Assert.Equal(input.Select(arg => arg.Value),
                result.Results.Select(arg => arg.Value));
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod3Test2Async() {

            var service = _services();
            var methodId = "http://test.org/UA/Data/#i=10771";
            var objectId = "http://test.org/UA/Data/#i=10755";

            var input = new List<MethodCallArgumentModel> {
                new MethodCallArgumentModel {
                    Value = _serializer.FromObject(new object[] {
                        new {
                            Type = "UInt32",
                            Body = 500000
                        },
                        new {
                            Type = "String",
                            Body = "test"
                        },
                        new {
                            Type = "Float",
                            Body = 0.3f
                        },
                        new {
                            Type = "Byte",
                            Body = 50
                        }
                    })
                },
                new MethodCallArgumentModel {
                    DataType = "int32",
                    Value = _serializer.FromObject(new int[] { 1, 2, 3, 4, 5, 6 })
                },
                new MethodCallArgumentModel {
                    DataType = "ExtensionObject",
                    Value = _serializer.FromObject(new object[] {
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
            Assert.Equal(input.Select(arg => arg.Value),
                result.Results.Select(arg => arg.Value));
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
        }


        public async Task NodeMethodCallStaticArrayMethod3Test3Async() {

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
            Assert.All(result.Results, arg => Assert.True(arg.Value.IsListOfValues));
            Assert.All(result.Results, arg => Assert.Empty(arg.Value.Values));
        }

        private readonly T _endpoint;
        private readonly Func<INodeServices<T>> _services;
        private readonly ISerializer _serializer;
    }
}
