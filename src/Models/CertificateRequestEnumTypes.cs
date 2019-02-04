// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types
{
    /// <summary>
    /// The certificate request states.
    /// </summary>
    public enum CertificateRequestState
    {
        /// <summary>
        /// The request is new.
        /// </summary>
        New = 0,
        /// <summary>
        /// The request was approved.
        /// </summary>
        Approved = 1,
        /// <summary>
        /// The request was rejected.
        /// </summary>
        Rejected = 2,
        /// <summary>
        /// The requests was accepted.
        /// </summary>
        Accepted = 3,
        /// <summary>
        /// The request was deleted.
        /// </summary>
        Deleted = 4,
        /// <summary>
        /// The requests was revoked.
        /// </summary>
        Revoked = 5,
        /// <summary>
        /// The request was removed.
        /// </summary>
        Removed = 6
    }
}

