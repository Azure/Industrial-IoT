// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {

    /// <summary>
    /// Event type
    /// </summary>
    public enum EndpointEventType {

        /// <summary>
        /// New
        /// </summary>
        New,

        /// <summary>
        /// Enabled
        /// </summary>
        Enabled,

        /// <summary>
        /// Disabled
        /// </summary>
        Disabled,

        /// <summary>
        /// Deactivated
        /// </summary>
        Deactivated,

        /// <summary>
        /// Activated
        /// </summary>
        Activated,

        /// <summary>
        /// Updated
        /// </summary>
        Updated,

        /// <summary>
        /// Deleted
        /// </summary>
        Deleted,
    }
}