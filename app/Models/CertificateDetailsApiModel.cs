// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    using Newtonsoft.Json;
    using System;

    public partial class CertificateDetailsApiModel
    {
        /// <summary>
        /// Initializes a new instance of the CertificateDetailsCollectionApiModel
        /// class.
        /// </summary>
        public CertificateDetailsApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "Subject")]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = "Issuer")]
        public string Issuer { get; set; }

        [JsonProperty(PropertyName = "Thumbprint")]
        public string Thumbprint { get; set; }

        [JsonProperty(PropertyName = "SerialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty(PropertyName = "NotBefore")]
        public DateTime NotBefore { get; set; }

        [JsonProperty(PropertyName = "NotAfter")]
        public DateTime NotAfter { get; set; }

    }
}
