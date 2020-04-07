// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Threading.Tasks;
    using Xunit;
    using System;
    using System.Collections.Generic;

    public class BrowseServicesTests<T> {

        /// <summary>
        /// Create browse services tests
        /// </summary>
        public BrowseServicesTests(Func<IBrowseServices<T>> services, T endpoint) {
            _services = services;
            _endpoint = endpoint;
        }

        public async Task NodeBrowseInRootTest1Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel());

            // Assert
            Assert.Equal("i=84", results.Node.NodeId);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Objects", reference.Target.BrowseName);
                    Assert.Equal("Objects", reference.Target.DisplayName);
                    Assert.Equal("i=85", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Types", reference.Target.BrowseName);
                    Assert.Equal("Types", reference.Target.DisplayName);
                    Assert.Equal("i=86", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);
                    Assert.Equal("Views", reference.Target.BrowseName);
                    Assert.Equal("Views", reference.Target.DisplayName);
                    Assert.Equal("i=87", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.False(reference.Target.Children);
                });
        }

        public async Task NodeBrowseInRootTest2Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    TargetNodesOnly = true,
                    ReadVariableValues = true
                });

            // Assert
            Assert.Equal("i=84", results.Node.NodeId);
            Assert.Equal("Root", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Null(reference.Direction);
                    Assert.Equal("Objects", reference.Target.BrowseName);
                    Assert.Equal("Objects", reference.Target.DisplayName);
                    Assert.Equal("i=85", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Null(reference.Direction);
                    Assert.Equal("Types", reference.Target.BrowseName);
                    Assert.Equal("Types", reference.Target.DisplayName);
                    Assert.Equal("i=86", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Null(reference.Direction);
                    Assert.Equal("Views", reference.Target.BrowseName);
                    Assert.Equal("Views", reference.Target.DisplayName);
                    Assert.Equal("i=87", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.False(reference.Target.Children);
                });
        }


        public async Task NodeBrowseFirstInRootTest1Async() {

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
            Assert.Null(results.Node.Description);
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


        public async Task NodeBrowseFirstInRootTest2Async() {

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
            Assert.Null(results.Node.Description);
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


        public async Task NodeBrowseBoilersObjectsTest1Async() {

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


        public async Task NodeBrowseDataAccessObjectsTest1Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "nsu=DataAccess;s=0:TestData/Static",
                    TargetNodesOnly = false
                });

            // Assert

            Assert.Equal("nsu=DataAccess;s=0:TestData/Static", results.Node.NodeId);
            Assert.Equal("Static", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC1001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("FC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC1001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("LC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC1001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC1001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("CC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC1001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC2001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("FC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC2001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC2001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("LC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC2001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC2001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("CC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC2001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                });
        }

        public async Task NodeBrowseDataAccessObjectsTest2Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "DataAccess#s=0:TestData/Static",
                    TargetNodesOnly = false
                });

            // Assert

            Assert.Equal("nsu=DataAccess;s=0:TestData/Static", results.Node.NodeId);
            Assert.Equal("Static", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC1001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("FC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC1001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("LC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC1001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC1001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("CC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC1001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC2001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("FC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC2001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC2001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("LC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC2001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=35", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC2001", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("CC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC2001", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                });
        }

        public async Task NodeBrowseDataAccessObjectsTest3Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "nsu=DataAccess;s=0:TestData/Static",
                    TargetNodesOnly = true,
                    ReadVariableValues = true
                });

            // Assert

            Assert.Equal("nsu=DataAccess;s=0:TestData/Static", results.Node.NodeId);
            Assert.Equal("Static", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC1001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("FC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC1001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("LC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC1001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC1001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("CC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC1001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC2001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("FC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC2001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC2001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("LC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC2001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC2001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("CC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC2001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                });
        }

        public async Task NodeBrowseDataAccessObjectsTest4Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "DataAccess#s=0:TestData/Static",
                    TargetNodesOnly = true,
                    ReadVariableValues = true
                });

            // Assert

            Assert.Equal("nsu=DataAccess;s=0:TestData/Static", results.Node.NodeId);
            Assert.Equal("Static", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC1001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("FC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC1001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("LC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC1001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC1001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("CC1001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC1001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC2001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("FC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC2001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC2001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("LC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:LC2001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC2001", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("CC2001", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Object, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:CC2001", reference.Target.NodeId);
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                });
        }


        public async Task NodeBrowseDataAccessFC1001Test1Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "nsu=DataAccess;s=1:FC1001",
                    TargetNodesOnly = false
                });

            // Assert

            Assert.Equal("nsu=DataAccess;s=1:FC1001", results.Node.NodeId);
            Assert.Equal("FC1001", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("i=47", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#SetPoint", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("SetPoint", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?SetPoint", reference.Target.NodeId);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=47", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#Measurement", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Measurement", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?Measurement", reference.Target.NodeId);
                    Assert.Equal("i=2365", reference.Target.TypeDefinitionId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=47", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#Output", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Output", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?Output", reference.Target.NodeId);
                    Assert.Equal("i=2365", reference.Target.TypeDefinitionId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Equal("i=47", reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#Status", reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Direction);

                    Assert.Equal("Status", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?Status", reference.Target.NodeId);
                    Assert.Equal("i=2376", reference.Target.TypeDefinitionId);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.True(reference.Target.Children);
                });
        }

        public async Task NodeBrowseDataAccessFC1001Test2Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "nsu=DataAccess;s=1:FC1001",
                    TargetNodesOnly = true,
                    ReadVariableValues = true
                });

            // Assert

            Assert.Equal("nsu=DataAccess;s=1:FC1001", results.Node.NodeId);
            Assert.Equal("FC1001", results.Node.DisplayName);
            Assert.Equal(true, results.Node.Children);
            Assert.Null(results.Node.EventNotifier);
            Assert.Null(results.Node.Description);
            Assert.Null(results.Node.AccessRestrictions);
            Assert.Null(results.ContinuationToken);
            Assert.Collection(results.References,
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#SetPoint", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("SetPoint", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?SetPoint", reference.Target.NodeId);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#Measurement", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Measurement", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?Measurement", reference.Target.NodeId);
                    Assert.Equal("i=2365", reference.Target.TypeDefinitionId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#Output", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Output", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?Output", reference.Target.NodeId);
                    Assert.Equal("i=2365", reference.Target.TypeDefinitionId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                },
                reference => {
                    Assert.Null(reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#Status", reference.Target.BrowseName);
                    Assert.Null(reference.Direction);

                    Assert.Equal("Status", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001?Status", reference.Target.NodeId);
                    Assert.Equal("i=2376", reference.Target.TypeDefinitionId);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Children);
                });
        }

        public async Task NodeBrowseBoilersObjectsTest2Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://opcfoundation.org/UA/Boiler/#i=1240",
                    TargetNodesOnly = false
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


        public async Task NodeBrowseStaticScalarVariablesTestAsync() {

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


        public async Task NodeBrowseStaticScalarVariablesTestWithFilter1Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10159",
                    TargetNodesOnly = true,
                    NodeClassFilter = new List<NodeClass> {
                        NodeClass.Method,
                        NodeClass.Object
                    }
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10159", results.Node.NodeId);
            Assert.Equal("Scalar", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.Children);
            Assert.Collection(results.References,
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

        public async Task NodeBrowseStaticScalarVariablesTestWithFilter2Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://test.org/UA/Data/#i=10159",
                    TargetNodesOnly = true,
                    NodeClassFilter = new List<NodeClass> {
                        NodeClass.Method
                    }
                });

            // Assert
            Assert.Null(results.ContinuationToken);
            Assert.Equal("http://test.org/UA/Data/#i=10159", results.Node.NodeId);
            Assert.Equal("Scalar", results.Node.DisplayName);
            Assert.Equal(NodeClass.Object, results.Node.NodeClass);
            Assert.True(results.Node.Children);
            Assert.Collection(results.References,
                reference => {
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Target.NodeId);
                    Assert.Equal("GenerateValues", reference.Target.DisplayName);
                    Assert.Equal(NodeClass.Method, reference.Target.NodeClass);
                    Assert.True(reference.Target.Executable);
                    Assert.True(reference.Target.UserExecutable);
                    Assert.True(reference.Target.Children);
                });
        }

        public async Task NodeBrowseStaticArrayVariablesTestAsync() {

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


        public async Task NodeBrowseStaticArrayVariablesWithValuesTestAsync() {

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
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10301",
                        reference.Target.NodeId);
                    Assert.Equal("SByte", reference.Target.DataType);
                    Assert.Equal("SByteValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10302",
                        reference.Target.NodeId);
                    Assert.Equal("Byte", reference.Target.DataType);
                    Assert.Equal("ByteValue", reference.Target.DisplayName);
                    // Assert.False(reference.Target.Value.IsNull());
                    if (!VariantValueEx.IsNull(reference.Target.Value)) {
                        Assert.True(reference.Target.Value.IsString);
                    }
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10303",
                        reference.Target.NodeId);
                    Assert.Equal("Int16", reference.Target.DataType);
                    Assert.Equal("Int16Value", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10304",
                        reference.Target.NodeId);
                    Assert.Equal("UInt16", reference.Target.DataType);
                    Assert.Equal("UInt16Value", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10305",
                        reference.Target.NodeId);
                    Assert.Equal("Int32", reference.Target.DataType);
                    Assert.Equal("Int32Value", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10306",
                        reference.Target.NodeId);
                    Assert.Equal("UInt32", reference.Target.DataType);
                    Assert.Equal("UInt32Value", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10307",
                        reference.Target.NodeId);
                    Assert.Equal("Int64", reference.Target.DataType);
                    Assert.Equal("Int64Value", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10308",
                        reference.Target.NodeId);
                    Assert.Equal("UInt64", reference.Target.DataType);
                    Assert.Equal("UInt64Value", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10309",
                        reference.Target.NodeId);
                    Assert.Equal("Float", reference.Target.DataType);
                    Assert.Equal("FloatValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10310",
                        reference.Target.NodeId);
                    Assert.Equal("Double", reference.Target.DataType);
                    Assert.Equal("DoubleValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10311",
                        reference.Target.NodeId);
                    Assert.Equal("String", reference.Target.DataType);
                    Assert.Equal("StringValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10312",
                        reference.Target.NodeId);
                    Assert.Equal("DateTime", reference.Target.DataType);
                    Assert.Equal("DateTimeValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10313",
                        reference.Target.NodeId);
                    Assert.Equal("Guid", reference.Target.DataType);
                    Assert.Equal("GuidValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10314",
                        reference.Target.NodeId);
                    Assert.Equal("ByteString", reference.Target.DataType);
                    Assert.Equal("ByteStringValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10315",
                        reference.Target.NodeId);
                    Assert.Equal("XmlElement", reference.Target.DataType);
                    Assert.Equal("XmlElementValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10316",
                        reference.Target.NodeId);
                    Assert.Equal("NodeId", reference.Target.DataType);
                    Assert.Equal("NodeIdValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10317",
                        reference.Target.NodeId);
                    Assert.Equal("ExpandedNodeId", reference.Target.DataType);
                    Assert.Equal("ExpandedNodeIdValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10318",
                        reference.Target.NodeId);
                    Assert.Equal("QualifiedName", reference.Target.DataType);
                    Assert.Equal("QualifiedNameValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10319",
                        reference.Target.NodeId);
                    Assert.Equal("LocalizedText", reference.Target.DataType);
                    Assert.Equal("LocalizedTextValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10320",
                        reference.Target.NodeId);
                    Assert.Equal("StatusCode", reference.Target.DataType);
                    Assert.Equal("StatusCodeValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10321",
                        reference.Target.NodeId);
                    Assert.Equal("Variant", reference.Target.DataType);
                    Assert.Equal("VariantValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10322",
                        reference.Target.NodeId);
                    Assert.Equal("Enumeration", reference.Target.DataType);
                    Assert.Equal("EnumerationValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10323",
                        reference.Target.NodeId);
                    Assert.Equal("ExtensionObject", reference.Target.DataType);
                    Assert.Equal("StructureValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsListOfValues);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10324",
                        reference.Target.NodeId);
                    // Assert.Equal("Number", reference.Target.DataType);
                    Assert.Equal("NumberValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    // Assert.True(reference.Target.Value.IsArray);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10325",
                        reference.Target.NodeId);
                    // Assert.Equal("Integer", reference.Target.DataType);
                    Assert.Equal("IntegerValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    // Assert.True(reference.Target.Value.IsArray);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10326",
                        reference.Target.NodeId);
                    // Assert.Equal("UInteger", reference.Target.DataType);
                    Assert.Equal("UIntegerValue", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    // Assert.True(reference.Target.Value.IsArray);
                },
                reference => {
                    Assert.Equal(NodeClass.Variable, reference.Target.NodeClass);
                    Assert.Equal("http://test.org/UA/Data/#i=10244",
                        reference.Target.NodeId);
                    Assert.Equal("Boolean", reference.Target.DataType);
                    Assert.Equal("SimulationActive", reference.Target.DisplayName);
                    Assert.False(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.True(reference.Target.Value.IsBoolean);
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



        public async Task NodeBrowseStaticArrayVariablesRawModeTestAsync() {

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
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
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
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
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
                    Assert.True(VariantValueEx.IsNull(reference.Target.Value));
                    Assert.Null(reference.Target.Children);
                });
        }

        public async Task NodeBrowseContinuationTest1Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseFirstAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://samples.org/UA/memorybuffer/Instance#s=UInt32",
                    MaxReferencesToReturn = 5
                });

            Assert.Null(results.ErrorInfo);
            Assert.NotNull(results.ContinuationToken);
            Assert.Equal(5, results.References.Count);

            // Act
            var cont = await browser.NodeBrowseNextAsync(_endpoint,
                new BrowseNextRequestModel {
                    ContinuationToken = results.ContinuationToken
                });

            Assert.Null(results.ErrorInfo);
            Assert.NotNull(results.ContinuationToken);
            Assert.Equal(5, results.References.Count);
        }


        public async Task NodeBrowseContinuationTest2Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseFirstAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://samples.org/UA/memorybuffer/Instance#s=UInt32",
                    MaxReferencesToReturn = 200
                });

            Assert.Null(results.ErrorInfo);
            Assert.NotNull(results.ContinuationToken);
            Assert.Equal(200, results.References.Count);

            // Act
            var cont = await browser.NodeBrowseNextAsync(_endpoint,
                new BrowseNextRequestModel {
                    ContinuationToken = results.ContinuationToken
                });

            Assert.Null(results.ErrorInfo);
            Assert.NotNull(results.ContinuationToken);
            Assert.Equal(200, results.References.Count);
        }

        public async Task NodeBrowseContinuationTest3Async() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseFirstAsync(_endpoint,
                new BrowseRequestModel {
                    NodeId = "http://samples.org/UA/memorybuffer/Instance#s=UInt32",
                    MaxReferencesToReturn = 1,
                    NodeIdsOnly = true
                });

            Assert.NotNull(results.ContinuationToken);
            Assert.Single(results.References);
        }


        public async Task NodeBrowsePathStaticScalarMethod3Test1Async() {
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
                    BrowsePaths = new List<string[]> { pathElements }
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


        public async Task NodeBrowsePathStaticScalarMethod3Test2Async() {
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
                    BrowsePaths = new List<string[]> { pathElements }
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


        public async Task NodeBrowsePathStaticScalarMethod3Test3Async() {
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
                    BrowsePaths = new List<string[]> { pathElements }
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


        public async Task NodeBrowsePathStaticScalarMethodsTestAsync() {
            var nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements3 = new[] {
                ".http://test.org/UA/Data/#Static",
                ".http://test.org/UA/Data/#MethodTest",
                ".http://test.org/UA/Data/#ScalarMethod3"
            };
            var pathElements2 = new[] {
                ".http://test.org/UA/Data/#Static",
                ".http://test.org/UA/Data/#MethodTest",
                ".http://test.org/UA/Data/#ScalarMethod2"
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
                    BrowsePaths = new List<string[]> { pathElements3, pathElements2 }
                });

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target => {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            }, target => {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod2", target.Target.BrowseName);
                Assert.Equal("ScalarMethod2", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10759", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }


        public async Task NodeBrowseDiagnosticsNoneTestAsync() {

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


        public async Task NodeBrowseDiagnosticsStatusTestAsync() {

            var browser = _services();

            // Act
            var results = await browser.NodeBrowseAsync(_endpoint,
                new BrowseRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeBrowseDiagnosticsStatusTestAsync),
                            TimeStamp = DateTime.Now,
                            Level = DiagnosticsLevel.Status
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown",
                    TargetNodesOnly = true
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.True(results.ErrorInfo.Diagnostics.IsListOfValues);
            Assert.Collection(results.ErrorInfo.Diagnostics.Values, j => {
                Assert.True(j.IsString);
                Assert.Equal("BadNodeIdUnknown", (string)j);
            });
        }


        public async Task NodeBrowseDiagnosticsOperationsTestAsync() {

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
            Assert.True(results.ErrorInfo.Diagnostics.IsObject);
            results.ErrorInfo.Diagnostics.TryGetProperty("BadNodeIdUnknown", out var item);
            Assert.NotNull(item);
            Assert.True(item.IsListOfValues);
            Assert.NotEqual(0, item.Count);
            Assert.NotEmpty(item.Values);
            Assert.Equal("Browse_ns=9;s=unknown", (string)item[0]);
        }


        public async Task NodeBrowseDiagnosticsVerboseTestAsync() {

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
            Assert.True(results.ErrorInfo.Diagnostics.IsListOfValues);
        }

        private readonly T _endpoint;
        private readonly Func<IBrowseServices<T>> _services;
    }
}
