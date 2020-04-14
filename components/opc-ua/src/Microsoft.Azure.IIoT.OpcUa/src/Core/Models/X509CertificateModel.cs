// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Certificate model
    /// </summary>
    public sealed class X509CertificateModel {

        /// <summary>
        /// Subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Not before validity
        /// </summary>
        public DateTime? NotBeforeUtc { get; set; }

        /// <summary>
        /// Not after validity
        /// </summary>
        public DateTime? NotAfterUtc { get; set; }

        /// <summary>
        /// Self signed certificate
        /// </summary>
        public bool? SelfSigned { get; set; }

        /// <summary>
        /// Raw data
        /// </summary>
        public byte[] Certificate { get; set; }
    }
}
