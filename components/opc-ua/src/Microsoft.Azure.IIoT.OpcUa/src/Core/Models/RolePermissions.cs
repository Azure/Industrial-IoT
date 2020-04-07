// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Individual permissions assigned to a role
    /// </summary>
    [Flags]
    public enum RolePermissions {

        /// <summary>
        /// Gives role the Browse permissions.
        /// </summary>
        Browse = 0x1,

        /// <summary>
        /// Gives role the ReadRolePermissions permissions.
        /// </summary>
        ReadRolePermissions = 0x2,

        /// <summary>
        /// Gives role the WriteAttribute permissions.
        /// </summary>
        WriteAttribute = 0x4,

        /// <summary>
        /// Gives role the WriteRolePermissions permissions.
        /// </summary>
        WriteRolePermissions = 0x8,

        /// <summary>
        /// Gives role the WriteHistorizing permissions.
        /// </summary>
        WriteHistorizing = 0x10,

        /// <summary>
        /// Gives role the Read permissions.
        /// </summary>
        Read = 0x20,

        /// <summary>
        /// Gives role the Write value permissions.
        /// </summary>
        Write = 0x40,

        /// <summary>
        /// Gives role the ReadHistory permissions.
        /// </summary>
        ReadHistory = 0x80,

        /// <summary>
        /// Gives role the InsertHistory permissions.
        /// </summary>
        InsertHistory = 0x100,

        /// <summary>
        /// Gives role the ModifyHistory permissions.
        /// </summary>
        ModifyHistory = 0x200,

        /// <summary>
        /// Gives role the DeleteHistory permissions.
        /// </summary>
        DeleteHistory = 0x400,

        /// <summary>
        /// Gives role the ReceiveEvents permissions.
        /// </summary>
        ReceiveEvents = 0x800,

        /// <summary>
        /// Gives role the Call permissions.
        /// </summary>
        Call = 0x1000,

        /// <summary>
        /// Gives role the AddReference permissions.
        /// </summary>
        AddReference = 0x2000,

        /// <summary>
        /// Gives role the RemoveReference permissions.
        /// </summary>
        RemoveReference = 0x4000,

        /// <summary>
        /// Gives role the DeleteNode permissions.
        /// </summary>
        DeleteNode = 0x8000,

        /// <summary>
        /// Gives role the AddNode permissions.
        /// </summary>
        AddNode = 0x10000,
    }
}
