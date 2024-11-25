// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Moq;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeCacheTests
    {
        [Fact]
        public async Task FetchRemainingNodesAsyncShouldHandleErrorsAsync()
        {
            // Arrange
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchNodesAsync(
                    It.IsAny<IReadOnlyList<NodeId>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node> { new() },
                    Errors = new[] { new ServiceResult(StatusCodes.BadUnexpectedError) }
                })
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindAsync(new List<NodeId> { id }, default);

            // Assert
            Assert.Single(result);
            context.Verify();
        }

        [Fact]
        public async Task FindAsyncShouldHandleEmptyListAsync()
        {
            // Arrange
            var context = new Mock<INodeCacheContext>();
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindAsync(new List<NodeId>(), default);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task FindAsyncShouldReturnNodeFromCacheAsync()
        {
            // Arrange
            var expected = new Node();
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchNodeAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindAsync(id, default);

            // Assert
            Assert.Equal(expected, result);
            result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            context.Verify();
        }

        [Fact]
        public async Task FindNodeWithBrowsePathAsyncShouldHandleInvalidBrowsePathAsync()
        {
            // Arrange
            var id = new NodeId("test", 0);
            var browsePath = new QualifiedNameCollection { new QualifiedName("invalid") };
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == ReferenceTypeIds.HierarchicalReferences),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindNodeWithBrowsePathAsync(id, browsePath, default);

            // Assert
            Assert.Null(result);
            context.Verify();
        }

        [Fact]
        public async Task FindNodeWithBrowsePathAsyncShouldReturnNodeAsync()
        {
            // Arrange
            var id = new NodeId("test", 0);
            var expected = new VariableNode
            {
                BrowseName = new QualifiedName("child"),
                NodeId = id,
                NodeClass = NodeClass.Variable
            };
            var browsePath = new QualifiedNameCollection { new QualifiedName("child") };
            var references = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = new ExpandedNodeId(id),
                    BrowseName = new QualifiedName("child"),
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IsForward = true
                }
            };
            var context = new Mock<INodeCacheContext>();
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == ReferenceTypeIds.HierarchicalReferences),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(references))
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node> { expected },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);

            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindNodeWithBrowsePathAsync(id,
                browsePath, default);

            // Assert
            Assert.Equal(expected, result);

            // Act
            result = await nodeCache.FindNodeWithBrowsePathAsync(id,
                browsePath, default);

            // Assert
            Assert.Equal(expected, result);
            context.Verify();
        }

        [Fact]
        public async Task FindNodeWithBrowsePathAsyncShouldReturnNodeWithMultipleElementsAsync()
        {
            // Arrange
            var rootId = new NodeId("root", 0);
            var childId = new NodeId("child", 0);
            var grandChildId = new NodeId("grandChild", 0);
            var browsePath = new QualifiedNameCollection
            {
                new QualifiedName("child"),
                new QualifiedName("grandChild")
            };

            var rootReferences = new List<ReferenceDescription>
            {
                new  ()
                {
                    NodeId = new ExpandedNodeId(childId),
                    BrowseName = new QualifiedName("child"),
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IsForward = true
                }
            };
            var childNode = new VariableNode
            {
                BrowseName = new QualifiedName("child"),
                NodeId = childId,
                NodeClass = NodeClass.Variable
            };
            var childReferences = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = new ExpandedNodeId(grandChildId),
                    BrowseName = new QualifiedName("grandChild"),
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IsForward = true
                }
            };
            var expected = new VariableNode
            {
                BrowseName = new QualifiedName("grandChild"),
                NodeId = grandChildId,
                NodeClass = NodeClass.Variable
            };

            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == ReferenceTypeIds.HierarchicalReferences),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == rootId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(rootReferences))
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == childId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(childReferences))
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == childId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node> { childNode },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == grandChildId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node> { expected },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);

            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindNodeWithBrowsePathAsync(rootId, browsePath, default);

            // Assert
            Assert.Equal(expected, result);

            // Act
            result = await nodeCache.FindNodeWithBrowsePathAsync(rootId, browsePath, default);

            // Assert
            Assert.Equal(expected, result);
            context.Verify();
        }

        [Fact]
        public async Task FindReferencesAsyncShouldHandleEmptyListOfNodeIdsAsync()
        {
            // Arrange
            var context = new Mock<INodeCacheContext>();
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindReferencesAsync(new List<NodeId>(), new List<NodeId>(), false, false, default);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task FindReferencesAsyncShouldReturnReferencesFromCacheAsync()
        {
            // Arrange
            var referenceTypeId = new NodeId("referenceType", 0);
            var targetExpandedNodeId = new ExpandedNodeId("target", 0);
            var targetNodeId = new NodeId("target", 0);
            var expected = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = targetExpandedNodeId,
                    ReferenceTypeId = referenceTypeId,
                    IsForward = false
                }
            };
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(expected))
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == targetNodeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node>
                    {
                        new VariableNode
                        {
                            NodeId = targetNodeId,
                            NodeClass = NodeClass.Variable
                        }
                    },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);

            var nodeCache = new NodeCache(context.Object);

            // Act
            var result1 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, true, false, default);
            var result2 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, false, false, default);

            // Assert
            Assert.Single(result1);
            Assert.Empty(result2);
            // Act
            result1 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, true, false, default);
            result2 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, false, false, default);

            // Assert
            Assert.Single(result1);
            Assert.Empty(result2);
            context.Verify();
        }

        [Fact]
        public async Task FindReferencesAsyncWithSubtypesShouldReturnReferencesFromCacheAsync()
        {
            // Arrange
            var referenceTypeId = new NodeId("referenceType", 0);
            var targetExpandedNodeId = new ExpandedNodeId("target", 0);
            var targetNodeId = new NodeId("target", 0);
            var expected = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = targetExpandedNodeId,
                    ReferenceTypeId = referenceTypeId,
                    IsForward = false
                }
            };
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == referenceTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(expected))
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == targetNodeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node>
                    {
                        new VariableNode
                        {
                            NodeId = targetNodeId,
                            NodeClass = NodeClass.Variable
                        }
                    },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);

            var nodeCache = new NodeCache(context.Object);

            // Act
            var result1 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, true, true, default);
            var result2 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, false, true, default);

            // Assert
            Assert.Single(result1);
            Assert.Empty(result2);
            // Act
            result1 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, true, true, default);
            result2 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, false, true, default);

            // Assert
            Assert.Single(result1);
            Assert.Empty(result2);
            context.Verify();
        }

        [Fact]
        public async Task FindReferencesAsyncWithMoreThanOneSubtypeShouldReturnReferencesFromCacheAsync()
        {
            // Arrange
            var referenceTypeId = new NodeId("referenceType", 0);
            var referenceSubTypeId = new NodeId("referenceSubType", 0);
            var targetExpandedNodeId = new ExpandedNodeId("target", 0);
            var targetNodeId = new NodeId("target", 0);
            var expected = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = targetExpandedNodeId,
                    ReferenceTypeId = referenceSubTypeId,
                    IsForward = false
                }
            };
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == referenceTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection
                {
                    new ()
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                        BrowseName = new QualifiedName("HasSubtype"),
                        NodeId = new ExpandedNodeId(referenceSubTypeId),
                    }
                })
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == referenceSubTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node>
                    {
                        new ReferenceTypeNode
                        {
                            NodeId = referenceSubTypeId,
                            NodeClass = NodeClass.ReferenceType
                        }
                    },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == referenceSubTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection
                {
                    new ()
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                        IsForward = false,
                        BrowseName = new QualifiedName("HasSuperType"),
                        NodeId = new ExpandedNodeId(referenceTypeId),
                    }
                })
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(expected))
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == targetNodeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node>
                    {
                        new VariableNode
                        {
                            NodeId = targetNodeId,
                            NodeClass = NodeClass.Variable
                        }
                    },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);

            var nodeCache = new NodeCache(context.Object);

            // Act
            var result1 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, true, true, default);
            var result2 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, false, true, default);

            // Assert
            Assert.Single(result1);
            Assert.Empty(result2);
            // Act
            result1 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, true, true, default);
            result2 = await nodeCache.FindReferencesAsync(id,
                referenceTypeId, false, true, default);

            // Assert
            Assert.Single(result1);
            Assert.Empty(result2);
            context.Verify();
        }

        [Fact]
        public async Task FindSuperTypeAsyncShouldHandleNoSupertypeAsync()
        {
            // Arrange
            var typeId = new NodeId("type", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == typeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindSuperTypeAsync(typeId, default);

            // Assert
            Assert.Equal(NodeId.Null, result);
            context.Verify();
        }

        [Fact]
        public async Task FindSuperTypeAsyncShouldReturnSuperTypeAsync()
        {
            // Arrange
            var superTypeId = new NodeId("superType", 0);
            var subTypeId = new NodeId("subType", 0);
            var references = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = new ExpandedNodeId(superTypeId),
                    ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                    IsForward = false
                }
            };
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == subTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(references))
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.FindSuperTypeAsync(subTypeId, default);

            // Assert
            Assert.Equal(superTypeId, result);

            // Act
            result = await nodeCache.FindSuperTypeAsync(subTypeId, default);
            // Assert
            Assert.Equal(superTypeId, result);

            context.Verify();
        }

        [Fact]
        public async Task GetBuiltInTypeAsyncShouldHandleUnknownTypeAsync()
        {
            // Arrange
            var datatypeId = new NodeId("unknownType", 0);
            var context = new Mock<INodeCacheContext>();
            var nodeCache = new NodeCache(context.Object);

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == datatypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);

            // Act
            var result = await nodeCache.GetBuiltInTypeAsync(datatypeId, default);

            // Assert
            Assert.Equal(BuiltInType.Null, result);
            context.Verify();
        }

        [Fact]
        public async Task GetBuiltInTypeAsyncShouldReturnBuiltInTypeAsync()
        {
            // Arrange
            var datatypeId = new NodeId((uint)BuiltInType.Int32, 0);
            var context = new Mock<INodeCacheContext>();
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = await nodeCache.GetBuiltInTypeAsync(datatypeId, default);

            // Assert
            Assert.Equal(BuiltInType.Int32, result);
        }

        [Fact]
        public async Task GetNodeTestAsync()
        {
            var expected = new Node();
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchNodeAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .Returns<NodeId, CancellationToken>((nodeId, ct)
                    => Task.FromResult(expected))
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            var result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            context.Verify();
        }

        [Fact]
        public async Task GetNodeThrowsTestAsync()
        {
            var expected = new Node();
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchNodeAsync(
                    It.Is<NodeId>(i => i == id),
                    It.IsAny<CancellationToken>()))
                .Returns<NodeId, CancellationToken>((nodeId, ct)
                    => Task.FromException<Node>(new ServiceResultException()))
                .Verifiable(Times.Exactly(3));
            var nodeCache = new NodeCache(context.Object);

            await Assert.ThrowsAsync<ServiceResultException>(
                () => nodeCache.FindAsync(id, default).AsTask());
            await Assert.ThrowsAsync<ServiceResultException>(
                () => nodeCache.FindAsync(id, default).AsTask());
            await Assert.ThrowsAsync<ServiceResultException>(
                () => nodeCache.FindAsync(id, default).AsTask());
            context.Verify();
        }
        [Fact]
        public void IsTypeOfShouldHandleNoReferences()
        {
            // Arrange
            var superTypeId = new NodeId("superType", 0);
            var subTypeId = new NodeId("subType", 0);
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == subTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = nodeCache.IsTypeOf(subTypeId, superTypeId);

            // Assert
            Assert.False(result);
            context.Verify();
        }

        [Fact]
        public void IsTypeOfShouldReturnTrueForSuperType()
        {
            // Arrange
            var superTypeId = new NodeId("superType", 0);
            var subTypeId = new NodeId("subType", 0);
            var references = new List<ReferenceDescription>
            {
                new ()
                {
                    NodeId = new ExpandedNodeId(superTypeId),
                    ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                    IsForward = false
                }
            };
            var context = new Mock<INodeCacheContext>();

            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == subTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection(references))
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            var result = nodeCache.IsTypeOf(subTypeId, superTypeId);

            // Assert
            Assert.True(result);

            // Act
            result = nodeCache.IsTypeOf(subTypeId, superTypeId);

            // Assert
            Assert.True(result);
            context.Verify();
        }

        [Fact]
        public async Task LoadTypeHierarchyAyncShouldHandleNoSubtypesAsync()
        {
            // Arrange
            var typeId = new NodeId("type", 0);
            var context = new Mock<INodeCacheContext>();
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == typeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            await nodeCache.LoadTypeHierarchyAync(new List<NodeId> { typeId }, default);

            // Assert
            context.Verify();
        }

        [Fact]
        public async Task LoadTypeHierarchyAyncShouldLoadTypeHierarchyAsync()
        {
            // Arrange
            var typeId = new NodeId("type", 0);
            var subTypeId = new NodeId("subType", 0);
            var references = new ReferenceDescriptionCollection
            {
                new ()
                {
                    NodeId = new ExpandedNodeId(subTypeId),
                    ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                    IsForward = true
                }
            };
            var context = new Mock<INodeCacheContext>();
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == typeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(references)
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchReferencesAsync(
                    It.Is<NodeId>(i => i == subTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReferenceDescriptionCollection())
                .Verifiable(Times.Once);
            context
                .Setup(c => c.FetchNodesAsync(
                    It.Is<IReadOnlyList<NodeId>>(n => n.Count == 1 && n[0] == subTypeId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultSet<Node>
                {
                    Results = new List<Node>
                    {
                        new DataTypeNode
                        {
                            NodeId = subTypeId,
                            NodeClass = NodeClass.DataType
                        }
                    },
                    Errors = new[] { ServiceResult.Good }
                })
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            // Act
            await nodeCache.LoadTypeHierarchyAync(new List<NodeId> { typeId }, default);
            await nodeCache.LoadTypeHierarchyAync(new List<NodeId> { typeId }, default);
            await nodeCache.LoadTypeHierarchyAync(new List<NodeId> { typeId }, default);

            // Assert
            context.Verify();
        }
    }
}
