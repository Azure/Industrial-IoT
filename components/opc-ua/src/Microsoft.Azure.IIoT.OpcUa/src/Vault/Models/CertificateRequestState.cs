// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    /// <summary>
    /// The certificate request states.
    /// </summary>
    public enum CertificateRequestState {

        /// <summary>
        /// The request is new.
        /// </summary>
        New,

        /// <summary>
        /// The request was approved.
        /// </summary>
        Approved,

        /// <summary>
        /// The request was rejected.
        /// </summary>
        Rejected,

        /// <summary>
        /// The request failed
        /// </summary>
        Failure,

        /// <summary>
        /// The request is finished.
        /// </summary>
        Completed,

        /// <summary>
        /// The client has accepted result
        /// </summary>
        Accepted
    }
}

