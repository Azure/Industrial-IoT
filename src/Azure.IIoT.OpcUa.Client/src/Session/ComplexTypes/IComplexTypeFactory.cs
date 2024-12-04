// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.ComplexTypes;

/// <summary>
/// Factory class for the complex type builder.
/// </summary>
public interface IComplexTypeFactory
{
    /// <summary>
    /// Create a new type builder instance for this factory.
    /// </summary>
    /// <param name="targetNamespace"></param>
    /// <param name="targetNamespaceIndex"></param>
    /// <param name="moduleName"></param>
    IComplexTypeBuilder Create(string targetNamespace,
        int targetNamespaceIndex, string? moduleName = null);
}
