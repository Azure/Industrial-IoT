// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Individual permissions assigned to a role
    /// </summary>
    [Flags]
    [DataContract]
    public enum RolePermissions
    {
        /// <summary>
        /// No permissions
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0x0,

        /// <summary>
        /// Gives role the Browse permissions.
        /// </summary>
        [EnumMember(Value = "Browse")]
        Browse = 0x1,

        /// <summary>
        /// Gives role the ReadRolePermissions permissions.
        /// </summary>
        [EnumMember(Value = "ReadRolePermissions")]
        ReadRolePermissions = 0x2,

        /// <summary>
        /// Gives role the WriteAttribute permissions.
        /// </summary>
        [EnumMember(Value = "WriteAttribute")]
        WriteAttribute = 0x4,

        /// <summary>
        /// Gives role the WriteRolePermissions permissions.
        /// </summary>
        [EnumMember(Value = "WriteRolePermissions")]
        WriteRolePermissions = 0x8,

        /// <summary>
        /// Gives role the WriteHistorizing permissions.
        /// </summary>
        [EnumMember(Value = "WriteHistorizing")]
        WriteHistorizing = 0x10,

        /// <summary>
        /// Gives role the Read permissions.
        /// </summary>
        [EnumMember(Value = "Read")]
        Read = 0x20,

        /// <summary>
        /// Gives role the Write value permissions.
        /// </summary>
        [EnumMember(Value = "Write")]
        Write = 0x40,

        /// <summary>
        /// Gives role the ReadHistory permissions.
        /// </summary>
        [EnumMember(Value = "ReadHistory")]
        ReadHistory = 0x80,

        /// <summary>
        /// Gives role the InsertHistory permissions.
        /// </summary>
        [EnumMember(Value = "InsertHistory")]
        InsertHistory = 0x100,

        /// <summary>
        /// Gives role the ModifyHistory permissions.
        /// </summary>
        [EnumMember(Value = "ModifyHistory")]
        ModifyHistory = 0x200,

        /// <summary>
        /// Gives role the DeleteHistory permissions.
        /// </summary>
        [EnumMember(Value = "DeleteHistory")]
        DeleteHistory = 0x400,

        /// <summary>
        /// Gives role the ReceiveEvents permissions.
        /// </summary>
        [EnumMember(Value = "ReceiveEvents")]
        ReceiveEvents = 0x800,

        /// <summary>
        /// Gives role the Call permissions.
        /// </summary>
        [EnumMember(Value = "Call")]
        Call = 0x1000,

        /// <summary>
        /// Gives role the AddReference permissions.
        /// </summary>
        [EnumMember(Value = "AddReference")]
        AddReference = 0x2000,

        /// <summary>
        /// Gives role the RemoveReference permissions.
        /// </summary>
        [EnumMember(Value = "RemoveReference")]
        RemoveReference = 0x4000,

        /// <summary>
        /// Gives role the DeleteNode permissions.
        /// </summary>
        [EnumMember(Value = "DeleteNode")]
        DeleteNode = 0x8000,

        /// <summary>
        /// Gives role the AddNode permissions.
        /// </summary>
        [EnumMember(Value = "AddNode")]
        AddNode = 0x10000,
    }
}
