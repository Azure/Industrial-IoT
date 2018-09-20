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
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class AddressSpaceBrowseTests {

        [Fact]
        public async Task NodeBrowseInRootTest1() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseAsync(GetEndpoint(),
                new BrowseRequestModel());

            // Assert
            Assert.Equal("i=84", results.Node.Id);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.HasChildren);
            Assert.NotNull(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=35", reference.Id);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Objects", reference.BrowseName);
                    Assert.Equal("Objects", reference.Target.DisplayName);
                    Assert.Equal("i=85", reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("i=35", reference.Id);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Types", reference.BrowseName);
                    Assert.Equal("Types", reference.Target.DisplayName);
                    Assert.Equal("i=86", reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("i=35", reference.Id);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Views", reference.BrowseName);
                    Assert.Equal("Views", reference.Target.DisplayName);
                    Assert.Equal("i=87", reference.Target.Id);
                    Assert.False(reference.Target.HasChildren);
                });
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest1() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseFirstAsync(GetEndpoint(),
                new BrowseRequestModel {
                    TargetNodesOnly = false,
                    MaxReferencesToReturn = 1
                });

            // Assert
            Assert.Equal("i=84", results.Node.Id);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.HasChildren);
            Assert.NotNull(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);

            Assert.NotNull(results.ContinuationToken);
            Assert.True(results.References.Count == 1);
            Assert.Collection(results.References,
              reference => {
                  Assert.Equal("i=35", reference.Id);
                  Assert.Equal(BrowseDirection.Forward, reference.Direction);
                  Assert.Equal("Objects", reference.BrowseName);
                  Assert.Equal("Objects", reference.Target.DisplayName);
                  Assert.Equal("i=85", reference.Target.Id);
                  Assert.True(reference.Target.HasChildren);
              });
        }

        [Fact]
        public async Task NodeBrowseFirstInRootTest2() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseFirstAsync(GetEndpoint(),
                new BrowseRequestModel {
                    TargetNodesOnly = false,
                    MaxReferencesToReturn = 2
                });

            // Assert
            Assert.Equal("i=84", results.Node.Id);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.HasChildren);
            Assert.NotNull(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);

            Assert.NotNull(results.ContinuationToken);
            Assert.True(results.References.Count == 2);
            Assert.Collection(results.References,
              reference => {
                  Assert.Equal("i=35", reference.Id);
                  Assert.Equal(BrowseDirection.Forward, reference.Direction);
                  Assert.Equal("Objects", reference.BrowseName);
                  Assert.Equal("Objects", reference.Target.DisplayName);
                  Assert.Equal("i=85", reference.Target.Id);
                  Assert.True(reference.Target.HasChildren);
              },
            reference => {
                Assert.Equal("i=35", reference.Id);
                Assert.Equal(BrowseDirection.Forward, reference.Direction);
                Assert.Equal("Types", reference.BrowseName);
                Assert.Equal("Types", reference.Target.DisplayName);
                Assert.Equal("i=86", reference.Target.Id);
                Assert.True(reference.Target.HasChildren);
            });
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest1() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseAsync(GetEndpoint(),
                new BrowseRequestModel {
                    NodeId = "http://opcfoundation.org/UA/Boiler/#i=1240",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", results.Node.Id);
            Assert.Equal("Boilers", results.Node.DisplayName);
            Assert.Equal(true, results.Node.HasChildren);
            Assert.Equal(NodeEventNotifier.SubscribeToEvents, results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Null(reference.Id);
                    Assert.Null(reference.BrowseName);
                    Assert.Null(reference.DisplayName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Boiler #1", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Null(reference.Id);
                    Assert.Null(reference.BrowseName);
                    Assert.Null(reference.DisplayName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Boiler #2", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#i=1",
                        reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                });
        }

        [Fact]
        public async Task NodeBrowseBoilersObjectsTest2() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseAsync(GetEndpoint(),
                new BrowseRequestModel {
                    NodeId = "http://opcfoundation.org/UA/Boiler/#i=1240",
                    TargetNodesOnly = false
                });

            // Assert
            var test = JsonConvertEx.SerializeObjectPretty(results);

            Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", results.Node.Id);
            Assert.Equal("Boilers", results.Node.DisplayName);
            Assert.Equal(true, results.Node.HasChildren);
            Assert.Equal(NodeEventNotifier.SubscribeToEvents, results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=47", reference.Id);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#Boiler #1",
                        reference.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Boiler #1", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("i=35", reference.Id);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#Boiler #2",
                        reference.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Boiler #2", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#i=1",
                        reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("i=48", reference.Id);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#Boiler #1",
                        reference.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Boiler #1", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Target.Id);
                    Assert.True(reference.Target.HasChildren);
                });
        }

        [Fact]
        public async Task NodeBrowseStaticScalarVariablesTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseAsync(GetEndpoint(),
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10159",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10159", results.Node.Id);
            Assert.Equal("Scalar", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.HasChildren);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10216",
                        reference.Target.Id);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("BooleanValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10217",
                        reference.Target.Id);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10218",
                        reference.Target.Id);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10219",
                        reference.Target.Id);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                     Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10220",
                        reference.Target.Id);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10221",
                        reference.Target.Id);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10222",
                        reference.Target.Id);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10223",
                        reference.Target.Id);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10224",
                        reference.Target.Id);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10225",
                        reference.Target.Id);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10226",
                        reference.Target.Id);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10227",
                        reference.Target.Id);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10228",
                        reference.Target.Id);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10229",
                        reference.Target.Id);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10230",
                        reference.Target.Id);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10231", reference.Target.Id);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10232", reference.Target.Id);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10233",
                        reference.Target.Id);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10234",
                        reference.Target.Id);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10235",
                        reference.Target.Id);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10236",
                        reference.Target.Id);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10237",
                        reference.Target.Id);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10238",
                        reference.Target.Id);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10239",
                        reference.Target.Id);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                }
                , reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10240",
                        reference.Target.Id);
                    Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10241",
                        reference.Target.Id);
                    Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10242",
                        reference.Target.Id);
                    Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10160",
                        reference.Target.Id);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Target.Id);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.True(reference.Target.Executable);
                    Assert.True(reference.Target.UserExecutable);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10163",
                        reference.Target.Id);
                    Assert.Equal("CycleComplete", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Null(reference.Target.Executable);
                    Assert.Null(reference.Target.UserExecutable);
                    Assert.True(reference.Target.HasChildren);
                });
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseAsync(GetEndpoint(),
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10243",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10243", results.Node.Id);
            Assert.Equal("Array", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.HasChildren);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10300",
                        reference.Target.Id);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("BooleanValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10301",
                        reference.Target.Id);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10302",
                        reference.Target.Id);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10303",
                        reference.Target.Id);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10304",
                        reference.Target.Id);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10305",
                        reference.Target.Id);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10306",
                        reference.Target.Id);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10307",
                        reference.Target.Id);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10308",
                        reference.Target.Id);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10309",
                        reference.Target.Id);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10310",
                        reference.Target.Id);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10311",
                        reference.Target.Id);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10312",
                        reference.Target.Id);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10313",
                        reference.Target.Id);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10314",
                        reference.Target.Id);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10315",
                        reference.Target.Id);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10316",
                        reference.Target.Id);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10317",
                        reference.Target.Id);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10318",
                        reference.Target.Id);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10319",
                        reference.Target.Id);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10320",
                        reference.Target.Id);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10321",
                        reference.Target.Id);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10322",
                        reference.Target.Id);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10323",
                        reference.Target.Id);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10324",
                        reference.Target.Id);
                    Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10325",
                        reference.Target.Id);
                    Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10326",
                        reference.Target.Id);
                    Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead | NodeAccessLevel.CurrentWrite,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.OneDimension, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10244",
                        reference.Target.Id);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.CurrentRead,
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.Scalar, reference.Target.ValueRank);
                    Assert.Null(reference.Target.ArrayDimensions);
                    Assert.False(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10245",
                        reference.Target.Id);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                    Assert.True(reference.Target.Executable);
                    Assert.True(reference.Target.UserExecutable);
                    Assert.True(reference.Target.HasChildren);
                },
                reference => {
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10247",
                        reference.Target.Id);
                    Assert.Equal("CycleComplete", reference.Target.DisplayName);
                    Assert.Null(reference.Target.Executable);
                    Assert.Null(reference.Target.UserExecutable);
                    Assert.True(reference.Target.HasChildren);
                });
        }

        [Fact]
        public async Task NodeBrowseStaticArrayVariablesWithValuesTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeBrowseAsync(GetEndpoint(),
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10243",
                    TargetNodesOnly = true,
                    ReadVariableValues = true
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10243", results.Node.Id);
            Assert.Equal("Array", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.HasChildren);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10300",
                        reference.Target.Id);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("BooleanValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10301",
                        reference.Target.Id);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10302",
                        reference.Target.Id);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.String, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10303",
                        reference.Target.Id);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10304",
                        reference.Target.Id);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10305",
                        reference.Target.Id);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10306",
                        reference.Target.Id);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10307",
                        reference.Target.Id);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10308",
                        reference.Target.Id);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10309",
                        reference.Target.Id);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10310",
                        reference.Target.Id);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10311",
                        reference.Target.Id);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10312",
                        reference.Target.Id);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10313",
                        reference.Target.Id);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10314",
                        reference.Target.Id);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10315",
                        reference.Target.Id);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10316",
                        reference.Target.Id);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10317",
                        reference.Target.Id);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10318",
                        reference.Target.Id);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10319",
                        reference.Target.Id);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10320",
                        reference.Target.Id);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10321",
                        reference.Target.Id);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10322",
                        reference.Target.Id);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10323",
                        reference.Target.Id);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10324",
                        reference.Target.Id);
                    Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10325",
                        reference.Target.Id);
                    Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10326",
                        reference.Target.Id);
                    Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Array, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10244",
                        reference.Target.Id);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.NotNull(reference.Target.Value);
                    Assert.Equal(JTokenType.Boolean, reference.Target.Value.Type);
                },
                reference => {
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10245",
                        reference.Target.Id);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                },
                reference => {
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10247",
                        reference.Target.Id);
                    Assert.Equal("CycleComplete", reference.Target.DisplayName);
                });
        }

        public AddressSpaceBrowseTests(ServerFixture server) {
            _server = server;
        }

        private IBrowseServices<EndpointModel> GetServices() {
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
