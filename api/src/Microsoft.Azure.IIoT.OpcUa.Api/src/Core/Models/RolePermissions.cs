// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Individual permissions assigned to a role
    /// </summary>
    [Flags]
    [DataContract]
    public enum RolePermissions {

        /// <summary>
        /// Gives role the Browse permissions.
        /// </summary>
        [EnumMember]
        Browse = 0x1,

        /// <summary>
        /// Gives role the ReadRolePermissions permissions.
        /// </summary>
        [EnumMember]
        ReadRolePermissions = 0x2,

        /// <summary>
        /// Gives role the WriteAttribute permissions.
        /// </summary>
        [EnumMember]
        WriteAttribute = 0x4,

        /// <summary>
        /// Gives role the WriteRolePermissions permissions.
        /// </summary>
        [EnumMember]
        WriteRolePermissions = 0x8,

        /// <summary>
        /// Gives role the WriteHistorizing permissions.
        /// </summary>
        [EnumMember]
        WriteHistorizing = 0x10,

        /// <summary>
        /// Gives role the Read permissions.
        /// </summary>
        [EnumMember]
        Read = 0x20,

        /// <summary>
        /// Gives role the Write value permissions.
        /// </summary>
        [EnumMember]
        Write = 0x40,

        /// <summary>
        /// Gives role the ReadHistory permissions.
        /// </summary>
        [EnumMember]
        ReadHistory = 0x80,

        /// <summary>
        /// Gives role the InsertHistory permissions.
        /// </summary>
        [EnumMember]
        InsertHistory = 0x100,

        /// <summary>
        /// Gives role the ModifyHistory permissions.
        /// </summary>
        [EnumMember]
        ModifyHistory = 0x200,

        /// <summary>
        /// Gives role the DeleteHistory permissions.
        /// </summary>
        [EnumMember]
        DeleteHistory = 0x400,

        /// <summary>
        /// Gives role the ReceiveEvents permissions.
        /// </summary>
        [EnumMember]
        ReceiveEvents = 0x800,

        /// <summary>
        /// Gives role the Call permissions.
        /// </summary>
        [EnumMember]
        Call = 0x1000,

        /// <summary>
        /// Gives role the AddReference permissions.
        /// </summary>
        [EnumMember]
        AddReference = 0x2000,

        /// <summary>
        /// Gives role the RemoveReference permissions.
        /// </summary>
        [EnumMember]
        RemoveReference = 0x4000,

        /// <summary>
        /// Gives role the DeleteNode permissions.
        /// </summary>
        [EnumMember]
        DeleteNode = 0x8000,

        /// <summary>
        /// Gives role the AddNode permissions.
        /// </summary>
        [EnumMember]
        AddNode = 0x10000,
    }
}
