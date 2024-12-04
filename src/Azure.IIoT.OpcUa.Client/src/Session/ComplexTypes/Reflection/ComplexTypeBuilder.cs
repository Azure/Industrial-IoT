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
/// Build an assembly with custom enum types and
/// complex types based on the BaseComplexType class
/// using System.Reflection.Emit.
/// </summary>
public class ComplexTypeBuilder : IComplexTypeBuilder
{
    /// <summary>
    /// The target namespace of the type builder.
    /// </summary>
    public string TargetNamespace { get; }

    /// <summary>
    /// The target namespace index of the type builder.
    /// </summary>
    public int TargetNamespaceIndex { get; }

    /// <summary>
    /// Initializes the object with default values.
    /// </summary>
    /// <param name="moduleFactory"></param>
    /// <param name="targetNamespace"></param>
    /// <param name="targetNamespaceIndex"></param>
    /// <param name="moduleName"></param>
    public ComplexTypeBuilder(AssemblyModule moduleFactory,
        string targetNamespace, int targetNamespaceIndex,
        string? moduleName = null)
    {
        TargetNamespace = targetNamespace;
        TargetNamespaceIndex = targetNamespaceIndex;
        _moduleName = FindModuleName(moduleName,
            targetNamespace);
        _moduleBuilder = moduleFactory.ModuleBuilder;
    }

    /// <summary>
    /// Create an enum type from an EnumDefinition in an ExtensionObject.
    /// Available since OPC UA V1.04 in the DataTypeDefinition attribute.
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="enumDefinition"></param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="enumDefinition"/> is <c>null</c>.</exception>
    public Type AddEnumType(QualifiedName typeName, EnumDefinition enumDefinition)
    {
        ArgumentNullException.ThrowIfNull(enumDefinition);

        var enumBuilder = _moduleBuilder.DefineEnum(
            GetFullQualifiedTypeName(typeName),
            TypeAttributes.Public,
            typeof(int));
        enumBuilder.DataContractAttribute(TargetNamespace);
        foreach (var enumValue in enumDefinition.Fields)
        {
            var newEnum = enumBuilder.DefineLiteral(
                enumValue.Name, (int)enumValue.Value);
            newEnum.EnumMemberAttribute(enumValue.Name, (int)enumValue.Value);
        }
        return enumBuilder.CreateTypeInfo();
    }

    /// <summary>
    /// Create a complex type from a StructureDefinition.
    /// Available since OPC UA V1.04 in the DataTypeDefinition attribute.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="structureDefinition"></param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="structureDefinition"/> is <c>null</c>.</exception>
    /// <exception cref="DataTypeNotSupportedException"></exception>
    public IComplexTypeFieldBuilder AddStructuredType(
        QualifiedName name,
        StructureDefinition structureDefinition)
    {
        ArgumentNullException.ThrowIfNull(structureDefinition);
        Type baseType;
        switch (structureDefinition.StructureType)
        {
            case StructureType.StructureWithOptionalFields:
                baseType = typeof(OptionalFieldsComplexType);
                break;
            case StructureType.UnionWithSubtypedValues:
            case StructureType.Union:
                baseType = typeof(UnionComplexType);
                break;
            case StructureType.StructureWithSubtypedValues:
            case StructureType.Structure:
                baseType = typeof(BaseComplexType);
                break;
            default:
                throw new DataTypeNotSupportedException("Unsupported structure type");
        }
        var structureBuilder = _moduleBuilder.DefineType(
            GetFullQualifiedTypeName(name),
            TypeAttributes.Public | TypeAttributes.Class,
            baseType);
        structureBuilder.DataContractAttribute(TargetNamespace);
        structureBuilder.StructureDefinitionAttribute(structureDefinition);
        return new ComplexTypeFieldBuilder(structureBuilder,
            structureDefinition.StructureType);
    }

    /// <summary>
    /// Create a unique namespace module name for the type.
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="targetNamespace"></param>
    private static string? FindModuleName(string? moduleName, string targetNamespace)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            // remove space chars in malformed namespace url
            var tempNamespace = targetNamespace.Replace(" ", "", StringComparison.Ordinal);
            var uri = new Uri(tempNamespace, UriKind.RelativeOrAbsolute);
            var tempName = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.ToString();

            tempName = tempName.Replace("/", "", StringComparison.Ordinal);
            var splitName = tempName.Split(':');
            moduleName = splitName[^1];
        }
        return moduleName;
    }

    /// <summary>
    /// Creates a unique full qualified type name for the assembly.
    /// </summary>
    /// <param name="browseName">The browse name of the type.</param>
    private string GetFullQualifiedTypeName(QualifiedName browseName)
    {
        var result = "Opc.Ua.ComplexTypes." + _moduleName + ".";
        if (browseName.NamespaceIndex > 1)
        {
            result += browseName.NamespaceIndex + ".";
        }
        return result + browseName.Name;
    }

    private readonly ModuleBuilder _moduleBuilder;
    private readonly string? _moduleName;
}
