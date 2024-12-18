// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Nodes.TypeSystem;

using FluentAssertions;
using Opc.Ua;
using System.Collections.Generic;
using System.Xml;
using Xunit;

public class EnumDescriptionTests
{
    [Fact]
    public void DecodeWithEmptyEnumDefinitionShouldReturnNull()
    {
        // Arrange
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection() };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("""{"TestField": 1}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DecodeWithIntegerShouldReturnEnumValueWithIntegerCode()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 },
            new EnumField { Name = "TestField4", Value = 4 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("""{"TestField":4}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField4");
        enumValue.Value.Should().Be(4);
    }

    [Fact]
    public void DecodeWithIntegerShouldReturnEnumValueWithSymbolAndIntegerCode()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 },
            new EnumField { Name = "TestField4", Value = 4 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName("ssss"));

        var jsonDecoder = new JsonDecoder("""{"TestField":"TestField_4"}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        enumDescription.XmlName.Name.Should().Be("ssss");
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField4");
        enumValue.Value.Should().Be(4);
    }

    [Fact]
    public void DecodeWithInvalidJsonTokenShouldReturnFirstEnumField1()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("""{"TestField": "InvalidToken"}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }

    [Fact]
    public void DecodeWithInvalidJsonTokenShouldReturnFirstEnumField2()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("""{"TestField": [1, 2, 3]}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }
    [Fact]
    public void DecodeWithInvalidJsonTokenShouldReturnFirstEnumField3()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("{}", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }

    [Fact]
    public void DecodeWithUnknownFieldNameShouldReturnFirstEnumField()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("""{"UnknownField": 1}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "UnknownField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }

    [Fact]
    public void EncodeWithNoFieldsAndNullEnumValueShouldContainZeroWithNonReversibleEncoding()
    {
        // Arrange
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection() };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonEncoder = new JsonEncoder(new ServiceMessageContext(), true);

        // Act
        enumDescription.Encode(jsonEncoder, "TestField", null);

        // Assert
        var json = jsonEncoder.CloseAndReturnText();
        json.Should().Contain("""
            "TestField":0
            """);
    }

    [Fact]
    public void EncodeWithNoFieldsAndNullEnumValueShouldContainZeroWithReversibleEncoding()
    {
        // Arrange
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection() };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonEncoder = new JsonEncoder(new ServiceMessageContext(), false);

        // Act
        enumDescription.Encode(jsonEncoder, "TestField", null);

        // Assert
        var json = jsonEncoder.CloseAndReturnText();
        json.Should().Contain("""
            "TestField":"0"
            """);
    }

    [Fact]
    public void EncodeWithNullEnumValueShouldEncodeDefaultWithNonReversibleEncoding()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonEncoder = new JsonEncoder(new ServiceMessageContext(), true);

        // Act
        enumDescription.Encode(jsonEncoder, "TestField", null);

        // Assert
        var json = jsonEncoder.CloseAndReturnText();
        json.Should().Contain("""
            "TestField":1
            """);
    }

    [Fact]
    public void EncodeWithNullEnumValueShouldEncodeDefaultWithReversibleEncoding()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonEncoder = new JsonEncoder(new ServiceMessageContext(), false);

        // Act
        enumDescription.Encode(jsonEncoder, "TestField", null);

        // Assert
        var json = jsonEncoder.CloseAndReturnText();
        json.Should().Contain("""
            "TestField":"TestField1"
            """);
    }
    [Fact]
    public void EnumDescriptionDecodeDefaultDecoderShouldReturnEnumValue()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var binaryDecoder = new BinaryDecoder(new byte[] { 1, 0, 0, 0 }, new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(binaryDecoder, "TestField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }

    [Fact]
    public void EnumDescriptionDecodeJsonDecoderShouldReturnEnumValue()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonDecoder = new JsonDecoder("""{"TestField": 1}""", new ServiceMessageContext());

        // Act
        var result = enumDescription.Decode(jsonDecoder, "TestField");

        // Assert
        result.Should().NotBeNull().And.BeOfType<EnumValue>();
        Assert.NotNull(result);
        var enumValue = (EnumValue)result;
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }

    [Fact]
    public void EnumDescriptionEncodeDefaultEncoderShouldEncodeEnumValue()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var binaryEncoder = new BinaryEncoder(new ServiceMessageContext());

        // Act
        enumDescription.Encode(binaryEncoder, "TestField", new EnumValue("TestField1", 1));

        // Assert
        var encodedBytes = binaryEncoder.CloseAndReturnBuffer();
        encodedBytes.Should().Equal(new byte[] { 1, 0, 0, 0 });
    }

    [Fact]
    public void EnumDescriptionEncodeJsonEncoderShouldEncodeEnumValueWithNonReversibleEncoding()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonEncoder = new JsonEncoder(new ServiceMessageContext(), true);

        // Act
        enumDescription.Encode(jsonEncoder, "TestField", new EnumValue("TestField1", 1));

        // Assert
        var json = jsonEncoder.CloseAndReturnText();
        json.Should().Contain("""
            "TestField":1
            """);
    }

    [Fact]
    public void EnumDescriptionEncodeJsonEncoderShouldEncodeEnumValueWithReversibleEncoding()
    {
        // Arrange
        var enumFields = new List<EnumField>
        {
            new EnumField { Name = "TestField1", Value = 1 },
            new EnumField { Name = "TestField2", Value = 2 }
        };
        var enumDefinition = new EnumDefinition { Fields = new EnumFieldCollection(enumFields) };
        var enumDescription = new EnumDescription(new ExpandedNodeId(333), enumDefinition, new XmlQualifiedName());

        var jsonEncoder = new JsonEncoder(new ServiceMessageContext(), false);

        // Act
        enumDescription.Encode(jsonEncoder, "TestField", new EnumValue("TestField1", 1));

        // Assert
        var json = jsonEncoder.CloseAndReturnText();
        json.Should().Contain("""
            "TestField":"TestField1"
            """);
    }
    [Fact]
    public void EnumDescriptionNullInstanceShouldReturnNull()
    {
        // Arrange
        var nullEnumDescription = EnumDescription.Null;

        // Act
        var result = nullEnumDescription.Decode(null!, "TestField");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnumValueInstancesWithDifferentValuesShouldNotBeEqual()
    {
        // Arrange
        var enumValue1 = new EnumValue("TestField1", 1);
        var enumValue2 = new EnumValue("TestField2", 2);

        // Act & Assert
        enumValue1.Should().NotBe(enumValue2);
    }

    [Fact]
    public void EnumValueInstancesWithSameValuesShouldBeEqual()
    {
        // Arrange
        var enumValue1 = new EnumValue("TestField1", 1);
        var enumValue2 = new EnumValue("TestField1", 1);

        // Act & Assert
        enumValue1.Should().Be(enumValue2);
    }

    [Fact]
    public void EnumValuePropertiesShouldBeSetCorrectly()
    {
        // Arrange
        var enumValue = new EnumValue("TestField1", 1);

        // Act & Assert
        enumValue.Symbol.Should().Be("TestField1");
        enumValue.Value.Should().Be(1);
    }
}
