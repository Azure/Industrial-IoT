// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class X509Certificate2CollectionApiModel
    {
        [JsonProperty(PropertyName = "Chain", Order = 10)]
        public X509Certificate2ApiModel[] Chain { get; set; }
        [JsonProperty(PropertyName = "NextPageLink", Order = 20)]
        public string NextPageLink { get; set; }


        public X509Certificate2CollectionApiModel(X509Certificate2Collection certificateCollection, string nextPageLink = null)
        {
            var chain = new List<X509Certificate2ApiModel>();
            foreach (var cert in certificateCollection)
            {
                var certApiModel = new X509Certificate2ApiModel(cert);
                chain.Add(certApiModel);
            }
            this.Chain = chain.ToArray();
            this.NextPageLink = nextPageLink;
        }

    }
}
