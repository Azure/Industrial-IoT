// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="AttributeMap.GetDefaultValue"/>.
    /// </summary>
    public sealed class AttributeMapTests
    {
        // ── Out-of-range attribute id ─────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_AttributeIdAbove32_ReturnsNull()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, 33, false);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_AttributeIdZero_ReturnsNull()
        {
            // Attribute IDs start at 1; 0 is not a valid attribute
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, 0, false);
            Assert.Null(result);
        }

        // ── Invalid node class ────────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_InvalidNodeClass_ThrowsServiceResultException()
        {
            Assert.Throws<ServiceResultException>(() =>
                AttributeMap.GetDefaultValue((NodeClass)0xFF, Attributes.NodeId, false));
        }

        // ── Variable node class ───────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_Variable_NodeClass_ReturnsVariableNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.Variable, result);
        }

        [Fact]
        public void GetDefaultValue_Variable_Value_ReturnsVariantNull()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.Value, false);
            Assert.NotNull(result);
            Assert.IsType<Variant>(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_Historizing_ReturnsFalse()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.Historizing, false);
            Assert.Equal(false, result);
        }

        [Fact]
        public void GetDefaultValue_Variable_AccessLevel_ReturnsByte1()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.AccessLevel, false);
            Assert.Equal((byte)1, result);
        }

        [Fact]
        public void GetDefaultValue_Variable_ValueRank_ReturnsScalar()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.ValueRank, false);
            Assert.Equal(ValueRanks.Scalar, result);
        }

        [Fact]
        public void GetDefaultValue_Variable_Description_NotNull_ReturnDefaultWhenNotOptional()
        {
            // Description is optional; when returnNullIfOptional=false, returns the default
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.Description, false);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_Description_Optional_ReturnsNullWhenOptional()
        {
            // When returnNullIfOptional=true and attribute is optional, returns null
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.Description, true);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_ArrayDimensions_Optional_ReturnsNullWhenOptional()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.ArrayDimensions, true);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_ArrayDimensions_NotOptional_ReturnsValue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.ArrayDimensions, false);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_WriteMask_Optional_ReturnsNullWhenOptional()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.WriteMask, true);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_WriteMask_NotOptional_ReturnsZero()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.WriteMask, false);
            Assert.Equal((uint)0, result);
        }

        [Fact]
        public void GetDefaultValue_Variable_DisplayName_NotOptional_ReturnsDefault()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.DisplayName, false);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_BrowseName_NotOptional_ReturnsDefault()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.BrowseName, false);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetDefaultValue_Variable_NodeId_NotOptional_ReturnsDefault()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Variable, Attributes.NodeId, false);
            Assert.NotNull(result);
        }

        // ── Object node class ─────────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_Object_NodeClass_ReturnsObjectNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Object, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.Object, result);
        }

        [Fact]
        public void GetDefaultValue_Object_EventNotifier_ReturnsByte0()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Object, Attributes.EventNotifier, false);
            Assert.Equal((byte)0, result);
        }

        [Fact]
        public void GetDefaultValue_Object_Value_ReturnsNull()
        {
            // Objects don't have a Value attribute
            var result = AttributeMap.GetDefaultValue(NodeClass.Object, Attributes.Value, false);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_Object_Description_Optional_ReturnsNullWhenOptional()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Object, Attributes.Description, true);
            Assert.Null(result);
        }

        // ── Method node class ─────────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_Method_NodeClass_ReturnsMethodNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Method, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.Method, result);
        }

        [Fact]
        public void GetDefaultValue_Method_Executable_ReturnsFalse()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Method, Attributes.Executable, false);
            Assert.Equal(false, result);
        }

        [Fact]
        public void GetDefaultValue_Method_UserExecutable_ReturnsFalse()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.Method, Attributes.UserExecutable, false);
            Assert.Equal(false, result);
        }

        [Fact]
        public void GetDefaultValue_Method_Value_ReturnsNull()
        {
            // Methods don't have a Value attribute
            var result = AttributeMap.GetDefaultValue(NodeClass.Method, Attributes.Value, false);
            Assert.Null(result);
        }

        // ── ObjectType node class ─────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_ObjectType_NodeClass_ReturnsObjectTypeNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ObjectType, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.ObjectType, result);
        }

        [Fact]
        public void GetDefaultValue_ObjectType_IsAbstract_ReturnsTrue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ObjectType, Attributes.IsAbstract, false);
            Assert.Equal(true, result);
        }

        // ── VariableType node class ───────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_VariableType_NodeClass_ReturnsVariableTypeNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.VariableType, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.VariableType, result);
        }

        [Fact]
        public void GetDefaultValue_VariableType_IsAbstract_ReturnsTrue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.VariableType, Attributes.IsAbstract, false);
            Assert.Equal(true, result);
        }

        [Fact]
        public void GetDefaultValue_VariableType_ValueRank_ReturnsScalar()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.VariableType, Attributes.ValueRank, false);
            Assert.Equal(ValueRanks.Scalar, result);
        }

        [Fact]
        public void GetDefaultValue_VariableType_Value_Optional_ReturnsNullWhenOptional()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.VariableType, Attributes.Value, true);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_VariableType_Value_NotOptional_ReturnsVariantNull()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.VariableType, Attributes.Value, false);
            Assert.NotNull(result);
        }

        // ── ReferenceType node class ──────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_ReferenceType_NodeClass_ReturnsReferenceTypeNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ReferenceType, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.ReferenceType, result);
        }

        [Fact]
        public void GetDefaultValue_ReferenceType_IsAbstract_ReturnsTrue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ReferenceType, Attributes.IsAbstract, false);
            Assert.Equal(true, result);
        }

        [Fact]
        public void GetDefaultValue_ReferenceType_Symmetric_ReturnsTrue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ReferenceType, Attributes.Symmetric, false);
            Assert.Equal(true, result);
        }

        [Fact]
        public void GetDefaultValue_ReferenceType_InverseName_Optional_ReturnsNullWhenOptional()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ReferenceType, Attributes.InverseName, true);
            Assert.Null(result);
        }

        [Fact]
        public void GetDefaultValue_ReferenceType_InverseName_NotOptional_ReturnsDefault()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.ReferenceType, Attributes.InverseName, false);
            Assert.NotNull(result);
        }

        // ── DataType node class ───────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_DataType_NodeClass_ReturnsDataTypeNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.DataType, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.DataType, result);
        }

        [Fact]
        public void GetDefaultValue_DataType_IsAbstract_ReturnsTrue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.DataType, Attributes.IsAbstract, false);
            Assert.Equal(true, result);
        }

        [Fact]
        public void GetDefaultValue_DataType_DataTypeDefinition_Optional_ReturnsNullWhenOptional()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.DataType, Attributes.DataTypeDefinition, true);
            Assert.Null(result);
        }

        // ── View node class ───────────────────────────────────────────────────

        [Fact]
        public void GetDefaultValue_View_NodeClass_ReturnsViewNodeClass()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.View, Attributes.NodeClass, false);
            Assert.Equal(NodeClass.View, result);
        }

        [Fact]
        public void GetDefaultValue_View_EventNotifier_ReturnsByte0()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.View, Attributes.EventNotifier, false);
            Assert.Equal((byte)0, result);
        }

        [Fact]
        public void GetDefaultValue_View_ContainsNoLoops_ReturnsTrue()
        {
            var result = AttributeMap.GetDefaultValue(NodeClass.View, Attributes.ContainsNoLoops, false);
            Assert.Equal(true, result);
        }

        [Fact]
        public void GetDefaultValue_View_Value_ReturnsNull()
        {
            // Views don't have a Value attribute
            var result = AttributeMap.GetDefaultValue(NodeClass.View, Attributes.Value, false);
            Assert.Null(result);
        }

        // ── Node classes that share common attributes ─────────────────────────

        [Theory]
        [InlineData(NodeClass.Variable)]
        [InlineData(NodeClass.Object)]
        [InlineData(NodeClass.Method)]
        [InlineData(NodeClass.ObjectType)]
        [InlineData(NodeClass.VariableType)]
        [InlineData(NodeClass.ReferenceType)]
        [InlineData(NodeClass.DataType)]
        [InlineData(NodeClass.View)]
        public void GetDefaultValue_AllNodeClasses_NodeId_ReturnsNonNull(NodeClass nodeClass)
        {
            var result = AttributeMap.GetDefaultValue(nodeClass, Attributes.NodeId, false);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(NodeClass.Variable)]
        [InlineData(NodeClass.Object)]
        [InlineData(NodeClass.Method)]
        [InlineData(NodeClass.ObjectType)]
        [InlineData(NodeClass.VariableType)]
        [InlineData(NodeClass.ReferenceType)]
        [InlineData(NodeClass.DataType)]
        [InlineData(NodeClass.View)]
        public void GetDefaultValue_AllNodeClasses_BrowseName_ReturnsNonNull(NodeClass nodeClass)
        {
            var result = AttributeMap.GetDefaultValue(nodeClass, Attributes.BrowseName, false);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(NodeClass.Variable)]
        [InlineData(NodeClass.Object)]
        [InlineData(NodeClass.Method)]
        [InlineData(NodeClass.ObjectType)]
        [InlineData(NodeClass.VariableType)]
        [InlineData(NodeClass.ReferenceType)]
        [InlineData(NodeClass.DataType)]
        [InlineData(NodeClass.View)]
        public void GetDefaultValue_AllNodeClasses_WriteMask_Optional_ReturnsNullWhenOptional(
            NodeClass nodeClass)
        {
            var result = AttributeMap.GetDefaultValue(nodeClass, Attributes.WriteMask, true);
            Assert.Null(result);
        }

        [Theory]
        [InlineData(NodeClass.Variable)]
        [InlineData(NodeClass.Object)]
        [InlineData(NodeClass.Method)]
        [InlineData(NodeClass.ObjectType)]
        [InlineData(NodeClass.VariableType)]
        [InlineData(NodeClass.ReferenceType)]
        [InlineData(NodeClass.DataType)]
        [InlineData(NodeClass.View)]
        public void GetDefaultValue_AllNodeClasses_RolePermissions_Optional_ReturnsNullWhenOptional(
            NodeClass nodeClass)
        {
            var result = AttributeMap.GetDefaultValue(nodeClass, Attributes.RolePermissions, true);
            Assert.Null(result);
        }

        [Theory]
        [InlineData(NodeClass.Variable)]
        [InlineData(NodeClass.Object)]
        [InlineData(NodeClass.Method)]
        [InlineData(NodeClass.ObjectType)]
        [InlineData(NodeClass.VariableType)]
        [InlineData(NodeClass.ReferenceType)]
        [InlineData(NodeClass.DataType)]
        [InlineData(NodeClass.View)]
        public void GetDefaultValue_AllNodeClasses_AccessRestrictions_Optional_ReturnsNullWhenOptional(
            NodeClass nodeClass)
        {
            var result = AttributeMap.GetDefaultValue(nodeClass, Attributes.AccessRestrictions, true);
            Assert.Null(result);
        }

        [Theory]
        [InlineData(NodeClass.Variable)]
        [InlineData(NodeClass.Object)]
        [InlineData(NodeClass.Method)]
        [InlineData(NodeClass.ObjectType)]
        [InlineData(NodeClass.VariableType)]
        [InlineData(NodeClass.ReferenceType)]
        [InlineData(NodeClass.DataType)]
        [InlineData(NodeClass.View)]
        public void GetDefaultValue_AllNodeClasses_AccessRestrictions_NotOptional_ReturnsUShort0(
            NodeClass nodeClass)
        {
            var result = AttributeMap.GetDefaultValue(nodeClass, Attributes.AccessRestrictions, false);
            Assert.Equal((ushort)0, result);
        }
    }
}
