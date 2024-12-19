// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Nodes.TypeSystem;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Opc.Ua.Client.Nodes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Opc.Ua;
using System.Xml;

public class DataTypeDescriptionCacheTests
{

    public DataTypeDescriptionCacheTests()
    {
        _nodeCacheMock = new Mock<INodeCache>();
        _contextMock = new Mock<IServiceMessageContext>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger<DataTypeDescriptionCache>>();
        _factoryMock = new Mock<IEncodeableFactory>();
        _loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
        _contextMock.Setup(c => c.Factory).Returns(_factoryMock.Object);
        _contextMock.Setup(c => c.NamespaceUris).Returns(new NamespaceTable());
    }

    [Fact]
    public void GetSystemTypeShouldReturnStructureValueTypeWhenTypeIsStructure()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1001");
        sut.Add(typeId, new StructureDefinition(), ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = sut.GetSystemType(typeId);

        // Assert
        result.Should().Be(typeof(StructureValue));
    }

    [Fact]
    public void GetSystemTypeShouldReturnEnumValueTypeWhenTypeIsEnum()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1002");
        sut.Add(typeId, new EnumDefinition(), ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = sut.GetSystemType(typeId);

        // Assert
        result.Should().Be(typeof(EnumValue));
    }

    [Fact]
    public void GetSystemTypeShouldReturnNullWhenTypeIsUnknown()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1003");

        // Act
        var result = sut.GetSystemType(typeId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddEncodeableTypeShouldAddTypeToFactory()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var systemType = typeof(StructureValue);

        // Act
        sut.AddEncodeableType(systemType);

        // Assert
        _factoryMock.Verify(f => f.AddEncodeableType(systemType), Times.Once);
    }

    [Fact]
    public void AddEncodeableTypeWithEncodingIdShouldAddTypeToFactory()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var encodingId = ExpandedNodeId.Parse("i=1004");
        var systemType = typeof(StructureValue);

        // Act
        sut.AddEncodeableType(encodingId, systemType);

        // Assert
        _factoryMock.Verify(f => f.AddEncodeableType(encodingId, systemType), Times.Once);
    }

    [Fact]
    public void AddEncodeableTypesWithAssemblyShouldAddTypesToFactory()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        sut.AddEncodeableTypes(assembly);

        // Assert
        _factoryMock.Verify(f => f.AddEncodeableTypes(assembly), Times.Once);
    }

    [Fact]
    public void AddEncodeableTypesWithSystemTypesShouldAddTypesToFactory()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var systemTypes = new List<Type> { typeof(StructureValue), typeof(EnumValue) };

        // Act
        sut.AddEncodeableTypes(systemTypes);

        // Assert
        _factoryMock.Verify(f => f.AddEncodeableTypes(systemTypes), Times.Once);
    }

    [Fact]
    public void GetStructureDescriptionShouldReturnStructureDescriptionWhenTypeIsKnown()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1005");
        var structureDescription = StructureDescription.Create(sut, typeId, new StructureDefinition(), new XmlQualifiedName(), ExpandedNodeId.Null, ExpandedNodeId.Null, ExpandedNodeId.Null, false);
        sut.Add(typeId, structureDescription.StructureDefinition, ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = sut.GetStructureDescription(typeId);

        // Assert
        result.Should().BeEquivalentTo(structureDescription);
    }

    [Fact]
    public void GetEnumDescriptionShouldReturnEnumDescriptionWhenTypeIsKnown()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1006");
        var enumDescription = new EnumDescription(typeId, new EnumDefinition(), new XmlQualifiedName(), false);
        sut.Add(typeId, enumDescription.EnumDefinition, ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = sut.GetEnumDescription(typeId);

        // Assert
        result.Should().BeEquivalentTo(enumDescription);
    }

    [Fact]
    public async Task GetDataTypeDescriptionAsyncShouldReturnDescriptionWhenTypeIsKnownAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1007");
        var structureDescription = StructureDescription.Create(sut, typeId, new StructureDefinition(), new XmlQualifiedName(), ExpandedNodeId.Null, ExpandedNodeId.Null, ExpandedNodeId.Null, false);
        sut.Add(typeId, structureDescription.StructureDefinition, ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = await sut.GetDataTypeDescriptionAsync(typeId, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(structureDescription);
    }

    [Fact]
    public async Task GetDataTypeDescriptionAsyncShouldReturnNullWhenTypeIsUnknownAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1008");

        // Act
        var result = await sut.GetDataTypeDescriptionAsync(typeId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PreloadDataTypeAsyncShouldLoadEnumDataTypeWhenTypeIsUnknownAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1009");
        var enumDefinition = new EnumDefinition();
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyEnum",
            DataTypeDefinition = new ExtensionObject(enumDefinition)
        };
        _nodeCacheMock
            .Setup(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTypeNode)
            .Verifiable(Times.Once);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                new NodeId[] { typeId },
                new NodeId[] { ReferenceTypeIds.HasSubtype },
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>())
            .Verifiable(Times.Once);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                typeId,
                ReferenceTypeIds.HasEncoding,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>())
            .Verifiable(Times.Once);

        // Act
        await sut.PreloadDataTypeAsync(typeId, true, CancellationToken.None);

        // Assert
        _nodeCacheMock.Verify();
    }

    [Fact]
    public async Task PreloadDataTypeAsyncShouldLoadStructureDataTypeWhenTypeIsUnknownAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1009");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field2", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field3", DataType = typeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyRecursiveStruct",
            DataTypeDefinition = new ExtensionObject(structureDefinition)
        };
        _nodeCacheMock
            .Setup(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTypeNode)
            .Verifiable(Times.Once);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                new NodeId[] { typeId },
                new NodeId[] { ReferenceTypeIds.HasSubtype },
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>())
            .Verifiable(Times.Once);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                typeId,
                ReferenceTypeIds.HasEncoding,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>())
            .Verifiable(Times.Once);

        // Act
        await sut.PreloadDataTypeAsync(typeId, true, CancellationToken.None);

        // Assert
        _nodeCacheMock.Verify();
    }

    [Fact]
    public async Task PreloadAllDataTypeAsyncShouldLoadUnknownTypesAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1009");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field2", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field3", DataType = typeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyRecursiveStruct",
            DataTypeDefinition = new ExtensionObject(structureDefinition)
        };
        _nodeCacheMock
            .SetupSequence(nc => nc.GetReferencesAsync(
                It.IsAny<NodeIdCollection>(),
                It.IsAny<IReadOnlyList<NodeId>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IReadOnlyList<INode>>(new List<INode> { dataTypeNode }))
            .Returns(new ValueTask<IReadOnlyList<INode>>(new List<INode> { dataTypeNode }))
            .Returns(new ValueTask<IReadOnlyList<INode>>(new List<INode>()));
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                typeId,
                ReferenceTypeIds.HasEncoding,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>())
            .Verifiable(Times.Once);

        // Act
        var result = await sut.PreloadAllDataTypeAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var added = sut.GetStructureDescription(typeId);
        added.Should().NotBeNull();
        added!.StructureDefinition.Should().BeEquivalentTo(structureDefinition);
        _nodeCacheMock.Verify();
    }

    [Fact]
    public async Task PreloadAllDataTypeAsyncShouldNotLoadAnyDataTypesThatAreAlreadyKnownAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        _contextMock.Setup(c => c.Factory).Returns(new EncodeableFactory()); // Could just mock GetSystemType
        var dataTypeNode = new DataTypeNode { NodeId = DataTypeIds.ReadAnnotationDataDetails };
        _nodeCacheMock
            .SetupSequence(nc => nc.GetReferencesAsync(
                It.IsAny<NodeIdCollection>(),
                It.IsAny<IReadOnlyList<NodeId>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IReadOnlyList<INode>>(new List<INode> { dataTypeNode }))
            .Returns(new ValueTask<IReadOnlyList<INode>>(new List<INode> { dataTypeNode }))
            .Returns(new ValueTask<IReadOnlyList<INode>>(new List<INode>()));

        // Act
        var result = await sut.PreloadAllDataTypeAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _nodeCacheMock.Verify();
    }

    [Fact]
    public void GetDataTypeDescriptionShouldReturnDescriptionWhenTypeIsKnown()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1011");
        var structureDescription = StructureDescription.Create(sut, typeId,
            new StructureDefinition(), new XmlQualifiedName(),
            ExpandedNodeId.Null, ExpandedNodeId.Null, ExpandedNodeId.Null, false);
        sut.Add(typeId, structureDescription.StructureDefinition,
            ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = sut.GetDataTypeDescription(typeId);

        // Assert
        result.Should().BeEquivalentTo(structureDescription);
    }

    [Fact]
    public void GetDataTypeDescriptionShouldReturnNullWhenTypeIsUnknown()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1012");

        // Act
        var result = sut.GetDataTypeDescription(typeId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PreloadDataTypeAsyncShouldLoadSubTypesWhenIncludeSubTypesIsTrueAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1013");
        var subTypeId = NodeId.Parse("i=1014");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field2", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field3", DataType = typeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyRecursiveStruct",
            DataTypeDefinition = new ExtensionObject(structureDefinition)
        };
        var subTypeStructureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields = []
        };
        var subTypeNode = new DataTypeNode
        {
            NodeId = subTypeId,
            BrowseName = "MySubTypeStruct",
            DataTypeDefinition = new ExtensionObject(subTypeStructureDefinition)
        };

        _nodeCacheMock.Setup(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTypeNode);
        _nodeCacheMock.Setup(nc => nc.GetNodeAsync(subTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subTypeNode);
        _nodeCacheMock.SetupSequence(nc => nc.GetReferencesAsync(
                It.IsAny<IReadOnlyList<NodeId>>(),
                new NodeId[] { ReferenceTypeIds.HasSubtype },
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode> { subTypeNode })
            .ReturnsAsync(new List<INode>());
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                It.IsAny<NodeId>(),
                ReferenceTypeIds.HasEncoding,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>());

        // Act
        await sut.PreloadDataTypeAsync(typeId, true, CancellationToken.None);

        // Assert
        _nodeCacheMock.Verify();
    }

    [Fact]
    public async Task PreloadDataTypeAsyncShouldNotLoadSubTypesWhenIncludeSubTypesIsFalseAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1015");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field2", DataType = typeId, ValueRank = ValueRanks.Scalar },
                new StructureField { Name = "Field3", DataType = typeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyStruct",
            DataTypeDefinition = new ExtensionObject(structureDefinition)
        };

        _nodeCacheMock.Setup(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTypeNode);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                It.IsAny<NodeId>(),
                ReferenceTypeIds.HasEncoding,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>())
            .Verifiable(Times.Once);

        // Act
        await sut.PreloadDataTypeAsync(typeId, false, CancellationToken.None);

        // Assert
        _nodeCacheMock.Verify(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()), Times.Once);
        _nodeCacheMock.Verify(nc => nc.GetReferencesAsync(
            new NodeId[] { typeId },
            new NodeId[] { ReferenceTypeIds.HasSubtype },
            false,
            false,
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreloadDataTypeAsyncShouldHandleRecursiveDataTypesAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1016");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = typeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyRecursiveStruct",
            DataTypeDefinition = new ExtensionObject(structureDefinition)
        };

        _nodeCacheMock.Setup(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTypeNode);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                new NodeId[] { typeId },
                new NodeId[] { ReferenceTypeIds.HasSubtype },
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>());
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                It.IsAny<NodeId>(),
                ReferenceTypeIds.HasEncoding,
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>());

        // Act
        await sut.PreloadDataTypeAsync(typeId, true, CancellationToken.None);

        // Assert
        _nodeCacheMock.Verify(nc => nc.GetNodeAsync(typeId, It.IsAny<CancellationToken>()), Times.Once);
        _nodeCacheMock.Verify(nc => nc.GetReferencesAsync(
            new NodeId[] { typeId },
            new NodeId[] { ReferenceTypeIds.HasSubtype },
            false,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDefinitionsAsyncShouldReturnAllDependentDefinitionsAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = ExpandedNodeId.Parse("i=1017");
        var dependentTypeId = NodeId.Parse("i=1018");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = dependentTypeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dependentStructureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields = []
        };

        sut.Add(typeId, structureDefinition, ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);
        sut.Add(dependentTypeId, dependentStructureDefinition, ExpandedNodeId.Null, ExpandedNodeId.Null, new XmlQualifiedName(), false);

        // Act
        var result = await sut.GetDefinitionsAsync(typeId, CancellationToken.None);

        // Assert
        result.Should().ContainKey(typeId);
        result.Should().ContainKey(dependentTypeId);
    }

    [Fact]
    public async Task GetDataTypeDescriptionAsyncShouldLoadTypeFromServerWhenTypeIsUnknownAsync()
    {
        // Arrange
        var sut = new DataTypeDescriptionCache(
            _nodeCacheMock.Object, _contextMock.Object, _loggerFactoryMock.Object);
        var typeId = NodeId.Parse("i=1019");
        var structureDefinition = new StructureDefinition
        {
            BaseDataType = NodeId.Parse("i=555"),
            Fields =
            [
                new StructureField { Name = "Field1", DataType = typeId, ValueRank = ValueRanks.Scalar }
            ]
        };
        var dataTypeNode = new DataTypeNode
        {
            NodeId = typeId,
            BrowseName = "MyStruct",
            DataTypeDefinition = new ExtensionObject(structureDefinition)
        };

        _nodeCacheMock.Setup(nc => nc.GetNodeAsync(It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTypeNode);
        _nodeCacheMock.Setup(nc => nc.GetReferencesAsync(
                It.IsAny<NodeId>(),
                It.IsAny<NodeId>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<INode>());

        // Act
        var result = await sut.GetDataTypeDescriptionAsync(typeId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<StructureDescription>();
        _nodeCacheMock.Verify(nc => nc.GetNodeAsync(It.IsAny<NodeId>(), It.IsAny<CancellationToken>()), Times.Once);
        _nodeCacheMock.Verify(nc => nc.GetReferencesAsync(
            It.IsAny<NodeId>(),
            It.IsAny<NodeId>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private readonly Mock<INodeCache> _nodeCacheMock;
    private readonly Mock<IServiceMessageContext> _contextMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<DataTypeDescriptionCache>> _loggerMock;
    private readonly Mock<IEncodeableFactory> _factoryMock;
}

