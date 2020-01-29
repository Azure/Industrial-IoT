// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Certificate model
    /// </summary>
    public sealed class X509CertificateApiModel {

        /// <summary>
        /// Subject
        /// </summary>
        [JsonProperty(PropertyName = "subject",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Subject { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        [JsonProperty(PropertyName = "thumbprint",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Serial number
        /// </summary>
        [JsonProperty(PropertyName = "serialNumber",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Not before validity
        /// </summary>
        [JsonProperty(PropertyName = "notBeforeUtc",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? NotBeforeUtc { get; set; }

        /// <summary>
        /// Not after validity
        /// </summary>
        [JsonProperty(PropertyName = "notAfterUtc",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? NotAfterUtc { get; set; }

        /// <summary>
        /// Self signed
        /// </summary>
        [JsonProperty(PropertyName = "selfSigned",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? SelfSigned { get; set; }

        /// <summary>
        /// Raw data
        /// </summary>
        [JsonProperty(PropertyName = "certificate")]
        public byte[] Certificate { get; set; }
    }
}
