// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Node attribute identifiers
    /// </summary>
    [DataContract]
    public enum NodeAttribute {

        /// <summary>
        /// Node class
        /// </summary>
        [EnumMember]
        NodeClass = 2,

        /// <summary>
        /// Browse name
        /// </summary>
        [EnumMember]
        BrowseName,

        /// <summary>
        /// Display name
        /// </summary>
        [EnumMember]
        DisplayName,

        /// <summary>
        /// Description
        /// </summary>
        [EnumMember]
        Description,

        /// <summary>
        /// Node write mask
        /// </summary>
        [EnumMember]
        WriteMask,

        /// <summary>
        /// User write mask
        /// </summary>
        [EnumMember]
        UserWriteMask,

        /// <summary>
        /// Is abstract
        /// </summary>
        [EnumMember]
        IsAbstract,

        /// <summary>
        /// Symmetric
        /// </summary>
        [EnumMember]
        Symmetric,

        /// <summary>
        /// Inverse name
        /// </summary>
        [EnumMember]
        InverseName,

        /// <summary>
        /// Contains no loop
        /// </summary>
        [EnumMember]
        ContainsNoLoops,

        /// <summary>
        /// Event notifier
        /// </summary>
        [EnumMember]
        EventNotifier,

        /// <summary>
        /// Value for variable
        /// </summary>
        [EnumMember]
        Value,

        /// <summary>
        /// Datatype
        /// </summary>
        [EnumMember]
        DataType,

        /// <summary>
        /// Value rank
        /// </summary>
        [EnumMember]
        ValueRank,

        /// <summary>
        /// Array dimension
        /// </summary>
        [EnumMember]
        ArrayDimensions,

        /// <summary>
        /// Accesslevel
        /// </summary>
        [EnumMember]
        AccessLevel,

        /// <summary>
        /// User access level
        /// </summary>
        [EnumMember]
        UserAccessLevel,

        /// <summary>
        /// Minimum sampling interval
        /// </summary>
        [EnumMember]
        MinimumSamplingInterval,

        /// <summary>
        /// Whether node is historizing
        /// </summary>
        [EnumMember]
        Historizing,

        /// <summary>
        /// Method can be called
        /// </summary>
        [EnumMember]
        Executable,

        /// <summary>
        /// User can call method
        /// </summary>
        [EnumMember]
        UserExecutable,

        /// <summary>
        /// Data type definition
        /// </summary>
        [EnumMember]
        DataTypeDefinition,

        /// <summary>
        /// Role permissions
        /// </summary>
        [EnumMember]
        RolePermissions,

        /// <summary>
        /// User role permissions
        /// </summary>
        [EnumMember]
        UserRolePermissions,

        /// <summary>
        /// Access restrictions on node
        /// </summary>
        [EnumMember]
        AccessRestrictions
    }
}