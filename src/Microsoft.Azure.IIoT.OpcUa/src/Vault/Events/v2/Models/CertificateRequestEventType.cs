// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Events.v2.Models {

    /// <summary>
    /// Certificate Request event type
    /// </summary>
    public enum CertificateRequestEventType {

        /// <summary>
        /// New
        /// </summary>
        Submitted,

        /// <summary>
        /// Approved
        /// </summary>
        Approved,

        /// <summary>
        /// Completed (success, failed, rejected)
        /// </summary>
        Completed,

        /// <summary>
        /// Request response accepted
        /// </summary>
        Accepted,

        /// <summary>
        /// Deleted (revoked, removed)
        /// </summary>
        Deleted,
    }
}