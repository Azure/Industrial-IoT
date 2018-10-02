// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#if CERTSIGNER
using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class SigningRequestApiModel
    {
        [JsonProperty(PropertyName = "ApplicationURI", Order = 10)]
        public string ApplicationURI { get; set; }

        [JsonProperty(PropertyName = "Csr", Order = 20)]
        public string Csr { get; set; }

        public SigningRequestApiModel(string applicationURI, byte [] csr)
        {
            this.Csr = Convert.ToBase64String(csr);
            this.ApplicationURI = applicationURI;
        }

        public byte [] ToServiceModel()
        {
            return Convert.FromBase64String(Csr);
        }

    }
}
#endif
