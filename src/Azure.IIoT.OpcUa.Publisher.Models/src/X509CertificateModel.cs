// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Certificate model
    /// </summary>
    [DataContract]
    public sealed record class X509CertificateModel
    {
        /// <summary>
        /// Subject
        /// </summary>
        [DataMember(Name = "subject", Order = 0,
            EmitDefaultValue = false)]
        public string? Subject { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        [DataMember(Name = "thumbprint", Order = 1,
            EmitDefaultValue = false)]
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Serial number
        /// </summary>
        [DataMember(Name = "serialNumber", Order = 2,
            EmitDefaultValue = false)]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Not before validity
        /// </summary>
        [DataMember(Name = "notBeforeUtc", Order = 3,
            EmitDefaultValue = false)]
        public DateTime? NotBeforeUtc { get; set; }

        /// <summary>
        /// Not after validity
        /// </summary>
        [DataMember(Name = "notAfterUtc", Order = 4,
            EmitDefaultValue = false)]
        public DateTime? NotAfterUtc { get; set; }

        /// <summary>
        /// Self signed certificate
        /// </summary>
        [DataMember(Name = "selfSigned", Order = 5,
            EmitDefaultValue = false)]
        public bool? SelfSigned { get; set; }

        /// <summary>
        /// Certificate as Pkcs12
        /// </summary>
        [DataMember(Name = "pfx", Order = 6)]
        public IReadOnlyCollection<byte>? Pfx { get; set; }

        /// <summary>
        /// Contains private key
        /// </summary>
        [DataMember(Name = "hasPrivateKey", Order = 7,
            EmitDefaultValue = false)]
        public bool? HasPrivateKey { get; set; }
    }
}
