/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.ComplexTypes.Reflection;

using System;
using System.Reflection;
using System.Reflection.Emit;

/// <summary>
/// Builder for property fields.
/// </summary>
public class ComplexTypeFieldBuilder : IComplexTypeFieldBuilder
{
    /// <summary>
    /// The field builder for a complex type.
    /// </summary>
    /// <param name="structureBuilder">The type builder to use.</param>
    /// <param name="structureType">The structure type.</param>
    public ComplexTypeFieldBuilder(TypeBuilder structureBuilder,
        StructureType structureType)
    {
        _structureBuilder = structureBuilder;
        _structureType = structureType;
    }

    /// <inheritdoc/>
    public void AddTypeIdAttribute(ExpandedNodeId complexTypeId,
        ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId)
    {
        _structureBuilder.StructureTypeIdAttribute(complexTypeId,
            binaryEncodingId, xmlEncodingId);
    }

    /// <inheritdoc/>
    public void AddField(StructureField field, Type fieldType, int order)
    {
        var fieldBuilder = _structureBuilder.DefineField("_" + field.Name,
            fieldType, FieldAttributes.Private);
        var propertyBuilder = _structureBuilder.DefineProperty(
            field.Name,
            PropertyAttributes.None,
            fieldType,
            null);
        const MethodAttributes methodAttributes =
            System.Reflection.MethodAttributes.Public |
            System.Reflection.MethodAttributes.HideBySig |
            System.Reflection.MethodAttributes.Virtual;

        var setBuilder = _structureBuilder.DefineMethod("set_" + field.Name,
            methodAttributes, null, [fieldType]);
        var setIl = setBuilder.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, fieldBuilder);
        if (_structureType is StructureType.Union or
            StructureType.UnionWithSubtypedValues)
        {
            // set the union selector to the new field index
            var unionField = typeof(UnionComplexType).GetField(
                "_switchField",
                BindingFlags.NonPublic |
                BindingFlags.Instance)!;
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldc_I4, order);
            setIl.Emit(OpCodes.Stfld, unionField);
        }
        setIl.Emit(OpCodes.Ret);

        var getBuilder = _structureBuilder.DefineMethod("get_" + field.Name,
            methodAttributes, fieldType, Type.EmptyTypes);
        var getIl = getBuilder.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, fieldBuilder);
        getIl.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getBuilder);
        propertyBuilder.SetSetMethod(setBuilder);
        propertyBuilder.DataMemberAttribute(field.Name, false, order);
        propertyBuilder.StructureFieldAttribute(field);
    }

    /// <inheritdoc/>
    public Type CreateType()
    {
        return _structureBuilder.CreateType();
    }

    /// <inheritdoc/>
    public Type GetStructureType(int valueRank)
    {
        return valueRank >= ValueRanks.OneDimension ?
            _structureBuilder.MakeArrayType(valueRank) :
            _structureBuilder;
    }

    private readonly TypeBuilder _structureBuilder;
    private readonly StructureType _structureType;
}
