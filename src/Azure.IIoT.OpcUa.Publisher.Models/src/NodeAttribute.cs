// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Node attribute identifiers
    /// </summary>
    [DataContract]
    public enum NodeAttribute
    {
        /// <summary>
        /// Node identifier
        /// </summary>
        [EnumMember(Value = "NodeId")]
        NodeId = 1,

        /// <summary>
        /// Node class
        /// </summary>
        [EnumMember(Value = "NodeClass")]
        NodeClass,

        /// <summary>
        /// Browse name
        /// </summary>
        [EnumMember(Value = "BrowseName")]
        BrowseName,

        /// <summary>
        /// Display name
        /// </summary>
        [EnumMember(Value = "DisplayName")]
        DisplayName,

        /// <summary>
        /// Description
        /// </summary>
        [EnumMember(Value = "Description")]
        Description,

        /// <summary>
        /// Node write mask
        /// </summary>
        [EnumMember(Value = "WriteMask")]
        WriteMask,

        /// <summary>
        /// User write mask
        /// </summary>
        [EnumMember(Value = "UserWriteMask")]
        UserWriteMask,

        /// <summary>
        /// Is abstract
        /// </summary>
        [EnumMember(Value = "IsAbstract")]
        IsAbstract,

        /// <summary>
        /// Symmetric
        /// </summary>
        [EnumMember(Value = "Symmetric")]
        Symmetric,

        /// <summary>
        /// Inverse name
        /// </summary>
        [EnumMember(Value = "InverseName")]
        InverseName,

        /// <summary>
        /// Contains no loop
        /// </summary>
        [EnumMember(Value = "ContainsNoLoops")]
        ContainsNoLoops,

        /// <summary>
        /// Event notifier
        /// </summary>
        [EnumMember(Value = "EventNotifier")]
        EventNotifier,

        /// <summary>
        /// Value for variable
        /// </summary>
        [EnumMember(Value = "Value")]
        Value,

        /// <summary>
        /// Datatype
        /// </summary>
        [EnumMember(Value = "DataType")]
        DataType,

        /// <summary>
        /// Value rank
        /// </summary>
        [EnumMember(Value = "ValueRank")]
        ValueRank,

        /// <summary>
        /// Array dimension
        /// </summary>
        [EnumMember(Value = "ArrayDimensions")]
        ArrayDimensions,

        /// <summary>
        /// Accesslevel
        /// </summary>
        [EnumMember(Value = "AccessLevel")]
        AccessLevel,

        /// <summary>
        /// User access level
        /// </summary>
        [EnumMember(Value = "UserAccessLevel")]
        UserAccessLevel,

        /// <summary>
        /// Minimum sampling interval
        /// </summary>
        [EnumMember(Value = "MinimumSamplingInterval")]
        MinimumSamplingInterval,

        /// <summary>
        /// Whether node is historizing
        /// </summary>
        [EnumMember(Value = "Historizing")]
        Historizing,

        /// <summary>
        /// Method can be called
        /// </summary>
        [EnumMember(Value = "Executable")]
        Executable,

        /// <summary>
        /// User can call method
        /// </summary>
        [EnumMember(Value = "UserExecutable")]
        UserExecutable,

        /// <summary>
        /// Data type definition
        /// </summary>
        [EnumMember(Value = "DataTypeDefinition")]
        DataTypeDefinition,

        /// <summary>
        /// Role permissions
        /// </summary>
        [EnumMember(Value = "RolePermissions")]
        RolePermissions,

        /// <summary>
        /// User role permissions
        /// </summary>
        [EnumMember(Value = "UserRolePermissions")]
        UserRolePermissions,

        /// <summary>
        /// Access restrictions on node
        /// </summary>
        [EnumMember(Value = "AccessRestrictions")]
        AccessRestrictions
    }
}
