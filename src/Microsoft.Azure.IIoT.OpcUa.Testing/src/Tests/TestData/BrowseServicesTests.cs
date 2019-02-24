// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using System;

    public class BrowseServicesTests<T> {

        /// <summary>
        /// Create browse services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        public BrowseServicesTests(Func<IBrowseServices<T>> services, T endpoint) {
            _services = services;
            _endpoint = endpoint;
        }

        public async Task NodeBrowseInRootTest1() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel());

            // Assert
            Assert.Equal("i=84", results.Node.NodeId);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.NotNull(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Objects", reference.Target.BrowseName);
                    Assert.Equal("Objects", reference.Target.DisplayName);
                    Assert.Equal("i=85", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Types", reference.Target.BrowseName);
                    Assert.Equal("Types", reference.Target.DisplayName);
                    Assert.Equal("i=86", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Views", reference.Target.BrowseName);
                    Assert.Equal("Views", reference.Target.DisplayName);
                    Assert.Equal("i=87", reference.Target.NodeId);
                    Assert.False(reference.Target.Children);
                });
        }


        public async Task NodeBrowseFirstInRootTest1() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseFirstAsync(_endpoint,
                new BrowseRequestModel {
                    TargetNodesOnly = false,
                    MaxReferencesToReturn = 1
                });

            // Assert
            Assert.Equal("i=84", results.Node.NodeId);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.NotNull(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);

            Assert.NotNull(results.ContinuationToken);
            Assert.True(results.References.Count == 1);
            Assert.Collection(results.References,
              reference => {
                  Assert.Equal("i=35", reference.ReferenceTypeId);
                  Assert.Equal(BrowseDirection.Forward, reference.Direction);
                  Assert.Equal("Objects", reference.Target.BrowseName);
                  Assert.Equal("Objects", reference.Target.DisplayName);
                  Assert.Equal("i=85", reference.Target.NodeId);
                  Assert.True(reference.Target.Children);
              });
        }


        public async Task NodeBrowseFirstInRootTest2() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseFirstAsync(_endpoint,
                new BrowseRequestModel {
                    TargetNodesOnly = false,
                    MaxReferencesToReturn = 2
                });

            // Assert
            Assert.Equal("i=84", results.Node.NodeId);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.NotNull(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);

            Assert.NotNull(results.ContinuationToken);
            Assert.True(results.References.Count == 2);
            Assert.Collection(results.References,
              reference => {
                  Assert.Equal("i=35", reference.ReferenceTypeId);
                  Assert.Equal(BrowseDirection.Forward, reference.Direction);
                  Assert.Equal("Objects", reference.Target.BrowseName);
                  Assert.Equal("Objects", reference.Target.DisplayName);
                  Assert.Equal("i=85", reference.Target.NodeId);
                  Assert.True(reference.Target.Children);
              },
            reference => {
                Assert.Equal("i=35", reference.ReferenceTypeId);
                Assert.Equal(BrowseDirection.Forward, reference.Direction);
                Assert.Equal("Types", reference.Target.BrowseName);
                Assert.Equal("Types", reference.Target.DisplayName);
                Assert.Equal("i=86", reference.Target.NodeId);
                Assert.True(reference.Target.Children);
            });
        }


        public async Task NodeBrowseBoilersObjectsTest1() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://opcfoundation.org/UA/Boiler/#i=1240",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", results.Node.NodeId);
            Assert.Equal("Boilers", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Equal(NodeEventNotifier.SubscribeToEvents, results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Boiler #1", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Boiler #2", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#i=1",
                        reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                });
        }


        public async Task NodeBrowseBoilersObjectsTest2() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://opcfoundation.org/UA/Boiler/#i=1240",
                    TargetNodesOnly = false
                });

            // Assert
            var test = JsonConvertEx.SerializeObjectPretty(results);

            Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", results.Node.NodeId);
            Assert.Equal("Boilers", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Equal(NodeEventNotifier.SubscribeToEvents, results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=47", reference.ReferenceTypeId);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#Boiler+%231",
                        reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Boiler #1", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#Boiler+%232",
                        reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Boiler #2", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#i=1",
                        reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=48", reference.ReferenceTypeId);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#Boiler+%231",
                        reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Boiler #1", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                });
        }


        public async Task NodeBrowseStaticScalarVariablesTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10159",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10159", results.Node.NodeId);
            Assert.Equal("Scalar", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.Children);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10216",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("BooleanValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10217",
                        reference.Target.NodeId);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10218",
                        reference.Target.NodeId);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10219",
                        reference.Target.NodeId);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                     Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10220",
                        reference.Target.NodeId);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10221",
                        reference.Target.NodeId);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10222",
                        reference.Target.NodeId);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10223",
                        reference.Target.NodeId);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10224",
                        reference.Target.NodeId);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10225",
                        reference.Target.NodeId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10226",
                        reference.Target.NodeId);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10227",
                        reference.Target.NodeId);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10228",
                        reference.Target.NodeId);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10229",
                        reference.Target.NodeId);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10230",
                        reference.Target.NodeId);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10231", reference.Target.NodeId);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10232", reference.Target.NodeId);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10233",
                        reference.Target.NodeId);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10234",
                        reference.Target.NodeId);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10235",
                        reference.Target.NodeId);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10236",
                        reference.Target.NodeId);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10237",
                        reference.Target.NodeId);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10238",
                        reference.Target.NodeId);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10239",
                        reference.Target.NodeId);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                }
                , reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10240",
                        reference.Target.NodeId);
                    Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10241",
                        reference.Target.NodeId);
                    Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10242",
                        reference.Target.NodeId);
                    Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10160",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Target.NodeId);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.True(reference.Target.Executable);
                    Assert.True(reference.Target.UserExecutable);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10163",
                        reference.Target.NodeId);
                    Assert.Equal("CycleComplete", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Null(reference.Target.Executable);
                    Assert.Null(reference.Target.UserExecutable);
                    Assert.True(reference.Target.Children);
                });
        }


        public async Task NodeBrowseStaticArrayVariablesTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10243",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10243", results.Node.NodeId);
            Assert.Equal("Array", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.Children);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10300",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("BooleanValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10301",
                        reference.Target.NodeId);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10302",
                        reference.Target.NodeId);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10303",
                        reference.Target.NodeId);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10304",
                        reference.Target.NodeId);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10305",
                        reference.Target.NodeId);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10306",
                        reference.Target.NodeId);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10307",
                        reference.Target.NodeId);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10308",
                        reference.Target.NodeId);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10309",
                        reference.Target.NodeId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10310",
                        reference.Target.NodeId);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10311",
                        reference.Target.NodeId);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10312",
                        reference.Target.NodeId);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10313",
                        reference.Target.NodeId);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10314",
                        reference.Target.NodeId);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10315",
                        reference.Target.NodeId);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10316",
                        reference.Target.NodeId);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10317",
                        reference.Target.NodeId);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10318",
                        reference.Target.NodeId);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10319",
                        reference.Target.NodeId);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10320",
                        reference.Target.NodeId);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10321",
                        reference.Target.NodeId);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10322",
                        reference.Target.NodeId);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10323",
                        reference.Target.NodeId);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10324",
                        reference.Target.NodeId);
                    Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10325",
                        reference.Target.NodeId);
                    Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10326",
                        reference.Target.NodeId);
                    Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10244",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10245",
                        reference.Target.NodeId);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                    Assert.True(reference.Target.Executable);
                    Assert.True(reference.Target.UserExecutable);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10247",
                        reference.Target.NodeId);
                    Assert.Equal("CycleComplete", reference.Target.DisplayName);
                    Assert.Null(reference.Target.Executable);
                    Assert.Null(reference.Target.UserExecutable);
                    Assert.True(reference.Target.Children);
                });
        }


        public async Task NodeBrowseStaticArrayVariablesWithValuesTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10243",
                    TargetNodesOnly = true,
                    ReadVariableValues = true
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10243", results.Node.NodeId);
            Assert.Equal("Array", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.Children);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10300",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("BooleanValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10301",
                        reference.Target.NodeId);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10302",
                        reference.Target.NodeId);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    if (reference.Target.Value.Type != JTokenType.Null) {
                        Assert.Equal(JTokenType.String, reference.Target.Value.Type);
                    }
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10303",
                        reference.Target.NodeId);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10304",
                        reference.Target.NodeId);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10305",
                        reference.Target.NodeId);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10306",
                        reference.Target.NodeId);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10307",
                        reference.Target.NodeId);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10308",
                        reference.Target.NodeId);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10309",
                        reference.Target.NodeId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10310",
                        reference.Target.NodeId);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10311",
                        reference.Target.NodeId);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10312",
                        reference.Target.NodeId);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10313",
                        reference.Target.NodeId);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10314",
                        reference.Target.NodeId);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10315",
                        reference.Target.NodeId);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10316",
                        reference.Target.NodeId);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10317",
                        reference.Target.NodeId);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10318",
                        reference.Target.NodeId);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10319",
                        reference.Target.NodeId);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10320",
                        reference.Target.NodeId);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10321",
                        reference.Target.NodeId);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10322",
                        reference.Target.NodeId);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10323",
                        reference.Target.NodeId);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10324",
                        reference.Target.NodeId);
                    // Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    // Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10325",
                        reference.Target.NodeId);
                    // Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    // Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10326",
                        reference.Target.NodeId);
                    // Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    // Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10244",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Boolean, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10245",
                        reference.Target.NodeId);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                },
                reference => {
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10247",
                        reference.Target.NodeId);
                    Assert.Equal("CycleComplete", reference.Target.DisplayName);
                });
        }



        public async Task NodeBrowseStaticArrayVariablesRawModeTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://opcfoundation.org/UA/Boiler/#i=1240",
                    NodeIdsOnly = true
                });

            // Assert
            Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", results.Node.NodeId);
            Assert.Null(results.Node.DisplayName);
            Assert.Null(results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=47", reference.ReferenceTypeId);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#Boiler+%231",
                        reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.NodeId);
                    Assert.NotNull(reference.Target.NodeClass);
                    Assert.Null(reference.Target.DataType);
                    Assert.Null(reference.Target.Description);
                    Assert.Null(reference.Target.Value);
                    Assert.Null(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#Boiler+%232",
                        reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#i=1",
                        reference.Target.NodeId);
                    Assert.NotNull(reference.Target.NodeClass);
                    Assert.Null(reference.Target.DataType);
                    Assert.Null(reference.Target.Description);
                    Assert.Null(reference.Target.Value);
                    Assert.Null(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=48", reference.ReferenceTypeId);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#Boiler+%231",
                        reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.NodeId);
                    Assert.NotNull(reference.Target.NodeClass);
                    Assert.Null(reference.Target.DataType);
                    Assert.Null(reference.Target.Description);
                    Assert.Null(reference.Target.Value);
                    Assert.Null(reference.Target.Children);
                });
        }


        public async Task NodeBrowsePathStaticScalarMethod3Test1() {
            var nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements = new[] {
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest",
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var browser = _services();

            // Act
            var results = await browser.NodeBrowsePathAsync(_endpoint,
                new BrowsePathRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    PathElements = pathElements
                });

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target => {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }


        public async Task NodeBrowsePathStaticScalarMethod3Test2() {
            var nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements = new[] {
                ".http://test.org/UA/Data/#Static",
                ".http://test.org/UA/Data/#MethodTest",
                ".http://test.org/UA/Data/#ScalarMethod3"
            };

            var browser = _services();

            // Act
            var results = await browser.NodeBrowsePathAsync(_endpoint,
                new BrowsePathRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    PathElements = pathElements
                });

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target => {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }


        public async Task NodeBrowsePathStaticScalarMethod3Test3() {
            var nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements = new[] {
                "<HasComponent>http://test.org/UA/Data/#Static",
                "<HasComponent>http://test.org/UA/Data/#MethodTest",
                "<HasComponent>http://test.org/UA/Data/#ScalarMethod3"
            };

            var browser = _services();

            // Act
            var results = await browser.NodeBrowsePathAsync(_endpoint,
                new BrowsePathRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    PathElements = pathElements
                });

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target => {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }


        public async Task NodeBrowseDiagnosticsNoneTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.NotNull(results.ErrorInfo);
            Assert.Null(results.ErrorInfo.Diagnostics);
            Assert.NotNull(results.ErrorInfo.StatusCode);
            Assert.Equal(results.ErrorInfo.StatusCode, Opc.Ua.StatusCodes.BadNodeIdUnknown);
        }


        public async Task NodeBrowseDiagnosticsStatusTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeBrowseDiagnosticsStatusTest),
                            TimeStamp = System.DateTime.Now,
                            Level = DiagnosticsLevel.Status
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.Equal(JTokenType.Array, results.ErrorInfo.Diagnostics.Type);
            Assert.Collection(results.ErrorInfo.Diagnostics, j => {
                Assert.Equal(JTokenType.String, j.Type);
                Assert.Equal("BadNodeIdUnknown", (string)j);
            });
        }


        public async Task NodeBrowseDiagnosticsOperationsTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            Level = DiagnosticsLevel.Operations
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.Equal(JTokenType.Object, results.ErrorInfo.Diagnostics.Type);
            Assert.Collection(results.ErrorInfo.Diagnostics,
                j => {
                    Assert.Equal(JTokenType.Property, j.Type);
                    Assert.Equal("BadNodeIdUnknown", ((JProperty)j).Name);
                    var item = ((JProperty)j).Value as JArray;
                    Assert.NotNull(item);
                    Assert.Equal("Browse_ns=4;s=unknown", (string)item[0]);
                });
        }


        public async Task NodeBrowseDiagnosticsVerboseTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            Level = DiagnosticsLevel.Verbose
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.Equal(JTokenType.Array, results.ErrorInfo.Diagnostics.Type);
        }

        private readonly T _endpoint;
        private readonly Func<IBrowseServices<T>> _services;
    }
}
