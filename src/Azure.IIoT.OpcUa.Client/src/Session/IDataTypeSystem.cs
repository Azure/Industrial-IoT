// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Internal Data type system interface
/// </summary>
internal interface IDataTypeSystem
{
    /// <summary>
    /// Get the data type definition and dependent definitions for a
    /// data type node id. Recursive through the cache to find all
    /// dependent types for strutures fields contained in the cache.
    /// </summary>
    /// <param name="dataTypeId"></param>
    IDictionary<ExpandedNodeId, DataTypeDefinition> GetDataTypeDefinitions(
        ExpandedNodeId dataTypeId);

    /// <summary>
    /// Get the information about the enum type
    /// </summary>
    /// <param name="dataTypeId"></param>
    /// <returns></returns>
    EnumDescription? GetEnumDescription(ExpandedNodeId dataTypeId);

    /// <summary>
    /// Get information for the structure type
    /// </summary>
    /// <param name="dataTypeId"></param>
    /// <returns></returns>
    StructureDescription? GetStructureDescription(ExpandedNodeId dataTypeId);

    /// <summary>
    /// Returns the system type for the specified type id.
    /// </summary>
    /// <param name="dataTypeId"></param>
    /// <returns></returns>
    Type? GetSystemType(ExpandedNodeId dataTypeId);
}
