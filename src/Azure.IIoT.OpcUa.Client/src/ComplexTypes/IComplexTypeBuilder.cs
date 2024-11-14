// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.ComplexTypes
{
    using System;

    /// <summary>
    /// Interface to dynamically build custom
    /// enum types and structured types.
    /// </summary>
    public interface IComplexTypeBuilder
    {
        /// <summary>
        /// Target namespace information.
        /// </summary>
        string TargetNamespace { get; }

        /// <summary>
        /// Target namespace index.
        /// </summary>
        int TargetNamespaceIndex { get; }

        /// <summary>
        /// Create an enum type from an EnumDefinition in an ExtensionObject.
        /// Available since OPC UA V1.04 in the DataTypeDefinition attribute.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="enumDefinition"></param>
        Type AddEnumType(QualifiedName typeName, EnumDefinition enumDefinition);

        /// <summary>
        /// Create a complex type from a StructureDefinition.
        /// Available since OPC UA V1.04 in the DataTypeDefinition attribute.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureDefinition"></param>
        IComplexTypeFieldBuilder AddStructuredType(QualifiedName name,
            StructureDefinition structureDefinition);
    }
}
