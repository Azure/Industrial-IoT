// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Nodes.TypeSystem;

using Opc.Ua;
using System.Xml;

/// <summary>
/// Base data type description
/// </summary>
public abstract class DataTypeDescription
{
    /// <summary>
    /// Type id
    /// </summary>
    public ExpandedNodeId TypeId { get; }

    /// <summary>
    /// Xml name
    /// </summary>
    public XmlQualifiedName XmlName { get; }

    /// <summary>
    /// Is abstract
    /// </summary>
    public bool IsAbstract { get; }

    /// <summary>
    /// Create data type description
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="xmlName"></param>
    /// <param name="isAbstract"></param>
    protected DataTypeDescription(ExpandedNodeId typeId,
        XmlQualifiedName xmlName, bool isAbstract = false)
    {
        TypeId = typeId;
        XmlName = xmlName;
        IsAbstract = isAbstract;
    }

    /// <summary>
    /// Create data type description
    /// </summary>
    protected DataTypeDescription()
    {
        TypeId = ExpandedNodeId.Null;
        XmlName = new XmlQualifiedName();
    }
}
