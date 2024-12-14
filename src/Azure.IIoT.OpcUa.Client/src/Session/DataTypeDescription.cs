// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

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
    /// Binary encoding id
    /// </summary>
    public ExpandedNodeId BinaryEncodingId { get; }

    /// <summary>
    /// Xml encoding id
    /// </summary>
    public ExpandedNodeId XmlEncodingId { get; }

    /// <summary>
    /// Json encoding id
    /// </summary>
    public ExpandedNodeId JsonEncodingId { get; }

    /// <summary>
    /// Create data type description
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="xmlName"></param>
    /// <param name="binaryEncodingId"></param>
    /// <param name="xmlEncodingId"></param>
    /// <param name="jsonEncodingId"></param>
    protected DataTypeDescription(ExpandedNodeId typeId,
        XmlQualifiedName xmlName, ExpandedNodeId binaryEncodingId,
        ExpandedNodeId xmlEncodingId, ExpandedNodeId jsonEncodingId)
    {
        TypeId = typeId;
        XmlName = xmlName;
        BinaryEncodingId = binaryEncodingId;
        XmlEncodingId = xmlEncodingId;
        JsonEncodingId = jsonEncodingId;
    }

    /// <summary>
    /// Create data type description
    /// </summary>
    protected DataTypeDescription()
    {
        TypeId = ExpandedNodeId.Null;
        XmlName = new XmlQualifiedName();
        BinaryEncodingId = ExpandedNodeId.Null;
        XmlEncodingId = ExpandedNodeId.Null;
        JsonEncodingId = ExpandedNodeId.Null;
    }
}
