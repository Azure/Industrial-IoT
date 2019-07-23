// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Certificate model
    /// </summary>
    public sealed class X509CertificateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public X509CertificateApiModel() {
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public X509CertificateApiModel(X509CertificateModel model) {
            Certificate = model.Certificate;
            Thumbprint = model.Thumbprint;
            SerialNumber = model.SerialNumber;
            NotBeforeUtc = model.NotBeforeUtc;
            NotAfterUtc = model.NotAfterUtc;
            Subject = model.Subject;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public X509CertificateModel ToServiceModel() {
            return new X509CertificateModel {
                Certificate = Certificate,
                NotAfterUtc = NotAfterUtc,
                NotBeforeUtc = NotBeforeUtc,
                SerialNumber = SerialNumber,
                Subject = Subject,
                Thumbprint = Thumbprint
            };
        }

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
        /// Raw data
        /// </summary>
        [JsonProperty(PropertyName = "certificate")]
        [Required]
        public JToken Certificate { get; set; }
    }
}
