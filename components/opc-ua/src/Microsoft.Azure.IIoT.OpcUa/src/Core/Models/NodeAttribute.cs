// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Node attribute identifiers
    /// </summary>
    public enum NodeAttribute {

        /// <summary>
        /// Node class
        /// </summary>
        NodeClass = 2,

        /// <summary>
        /// Browse name
        /// </summary>
        BrowseName,

        /// <summary>
        /// Display name
        /// </summary>
        DisplayName,

        /// <summary>
        /// Description
        /// </summary>
        Description,

        /// <summary>
        /// Node write mask
        /// </summary>
        WriteMask,

        /// <summary>
        /// User write mask
        /// </summary>
        UserWriteMask,

        /// <summary>
        /// Is abstract
        /// </summary>
        IsAbstract,

        /// <summary>
        /// Symmetric
        /// </summary>
        Symmetric,

        /// <summary>
        /// Inverse name
        /// </summary>
        InverseName,

        /// <summary>
        /// Contains no loop
        /// </summary>
        ContainsNoLoops,

        /// <summary>
        /// Event notifier
        /// </summary>
        EventNotifier,

        /// <summary>
        /// Value for variable
        /// </summary>
        Value,

        /// <summary>
        /// Datatype
        /// </summary>
        DataType,

        /// <summary>
        /// Value rank
        /// </summary>
        ValueRank,

        /// <summary>
        /// Array dimension
        /// </summary>
        ArrayDimensions,

        /// <summary>
        /// Accesslevel
        /// </summary>
        AccessLevel,

        /// <summary>
        /// User access level
        /// </summary>
        UserAccessLevel,

        /// <summary>
        /// Minimum sampling interval
        /// </summary>
        MinimumSamplingInterval,

        /// <summary>
        /// Whether node is historizing
        /// </summary>
        Historizing,

        /// <summary>
        /// Method can be called
        /// </summary>
        Executable,

        /// <summary>
        /// User can call method
        /// </summary>
        UserExecutable,

        /// <summary>
        /// Data type definition
        /// </summary>
        DataTypeDefinition,

        /// <summary>
        /// Role permissions
        /// </summary>
        RolePermissions,

        /// <summary>
        /// User role permissions
        /// </summary>
        UserRolePermissions,

        /// <summary>
        /// Access restrictions on node
        /// </summary>
        AccessRestrictions
    }
}
