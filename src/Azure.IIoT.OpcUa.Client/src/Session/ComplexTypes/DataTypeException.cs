// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.ComplexTypes;

using System;

/// <summary>
/// DataType is not supported due to structure or value rank.
/// </summary>
[Serializable]
public class DataTypeNotSupportedException : Exception
{
    /// <summary>
    /// The nodeId of the data type.
    /// </summary>
    public ExpandedNodeId NodeId { get; }

    /// <summary>
    /// The name of the data type.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// Create the exception.
    /// </summary>
    /// <param name="nodeId">The nodeId of the data type.</param>
    public DataTypeNotSupportedException(ExpandedNodeId nodeId)
    {
        NodeId = nodeId;
    }

    /// <summary>
    /// Create the exception.
    /// </summary>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="message">The exception message.</param>
    public DataTypeNotSupportedException(string typeName, string message)
        : base(message)
    {
        NodeId = Ua.NodeId.Null;
        TypeName = typeName;
    }

    /// <summary>
    /// Create the exception.
    /// </summary>
    /// <param name="nodeId">The nodeId of the data type.</param>
    /// <param name="message">The exception message.</param>
    public DataTypeNotSupportedException(ExpandedNodeId nodeId,
        string message) : base(message)
    {
        NodeId = nodeId;
    }

    /// <summary>
    /// Create the exception.
    /// </summary>
    /// <param name="nodeId">The nodeId of the data type.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The inner exception.</param>
    public DataTypeNotSupportedException(ExpandedNodeId nodeId,
        string message, Exception inner) : base(message, inner)
    {
        NodeId = nodeId;
    }
}
