// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {

    /// <summary>
    /// Event type
    /// </summary>
    public enum DeviceTwinEventType {

        /// <summary>
        /// New device
        /// </summary>
        New,

        /// <summary>
        /// Device twin created
        /// </summary>
        Create,

        /// <summary>
        /// Device twin updated
        /// </summary>
        Update,

        /// <summary>
        /// Device deleted
        /// </summary>
        Delete
    }
}
