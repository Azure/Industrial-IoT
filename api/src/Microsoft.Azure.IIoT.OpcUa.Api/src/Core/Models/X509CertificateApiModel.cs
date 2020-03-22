// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Certificate model
    /// </summary>
    [DataContract]
    public sealed class X509CertificateApiModel {

        /// <summary>
        /// Subject
        /// </summary>
        [DataMember(Name = "subject",
            EmitDefaultValue = false)]
        public string Subject { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        [DataMember(Name = "thumbprint",
            EmitDefaultValue = false)]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Serial number
        /// </summary>
        [DataMember(Name = "serialNumber",
            EmitDefaultValue = false)]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Not before validity
        /// </summary>
        [DataMember(Name = "notBeforeUtc",
            EmitDefaultValue = false)]
        public DateTime? NotBeforeUtc { get; set; }

        /// <summary>
        /// Not after validity
        /// </summary>
        [DataMember(Name = "notAfterUtc",
            EmitDefaultValue = false)]
        public DateTime? NotAfterUtc { get; set; }

        /// <summary>
        /// Self signed
        /// </summary>
        [DataMember(Name = "selfSigned",
            EmitDefaultValue = false)]
        public bool? SelfSigned { get; set; }

        /// <summary>
        /// Raw data
        /// </summary>
        [DataMember(Name = "certificate")]
        public byte[] Certificate { get; set; }
    }
}
