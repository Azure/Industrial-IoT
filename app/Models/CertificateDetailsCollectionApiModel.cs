// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    using Newtonsoft.Json;
    using System;

    public partial class CertificateDetailsCollectionApiModel
    {
        /// <summary>
        /// Initializes a new instance of the CertificateDetailsCollectionApiModel
        /// class.
        /// </summary>
        public CertificateDetailsCollectionApiModel(string id)
        {
            Name = id;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Certificates")]
        public CertificateDetailsApiModel [] Certificates { get; set; }

    }
}
