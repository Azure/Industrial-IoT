// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    /// <summary>
    /// A X509 certificate revocation list.
    /// </summary>
    public sealed class X509CrlApiModel
    {
        /// <summary>
        /// The Issuer name of the revocation list.
        /// </summary>
        [JsonProperty(PropertyName = "issuer", Order = 10)]
        public string Issuer { get; set; }
        /// <summary>
        /// The base64 encoded X509 certificate revocation list.
        /// </summary>
        [JsonProperty(PropertyName = "crl", Order = 20)]
        public string Crl { get; set; }

        public X509CrlApiModel(Opc.Ua.X509CRL crl)
        {
            this.Crl = Convert.ToBase64String(crl.RawData);
            this.Issuer = crl.Issuer;
        }

        public Opc.Ua.X509CRL ToServiceModel()
        {
            return new Opc.Ua.X509CRL(Convert.FromBase64String(Crl));
        }

    }
}
