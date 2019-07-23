// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;

    /// <summary>
    /// Certificate Request event
    /// </summary>
    public class CertificateRequestEventModel {

        /// <summary>
        /// Event type
        /// </summary>
        public CertificateRequestEventType EventType { get; set; }

        /// <summary>
        /// Request
        /// </summary>
        public CertificateRequestModel Request { get; set; }
    }
}