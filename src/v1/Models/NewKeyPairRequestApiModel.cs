// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


// TODO: remove / cleanup
#if CERTSIGNER
using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class NewKeyPairRequestApiModel
    {
        [JsonProperty(PropertyName = "ApplicationURI", Order = 10)]
        public string ApplicationURI { get; set; }

        [JsonProperty(PropertyName = "SubjectName", Order = 20)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "DomainNames", Order = 30)]
        public string [] DomainNames { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyFormat", Order = 40)]
        public string PrivateKeyFormat { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyPassword", Order = 50)]
        public string PrivateKeyPassword { get; set; }

        public NewKeyPairRequestApiModel(
            string applicationURI, 
            string subjectName, 
            string[] domainNames,
            string PrivateKeyFormat,
            string PrivateKeyPassword)
        {
            this.ApplicationURI = applicationURI;
            this.SubjectName = subjectName;
            this.DomainNames = domainNames;
            this.PrivateKeyFormat = PrivateKeyFormat;
            this.PrivateKeyPassword = PrivateKeyPassword;
        }

    }
}
#endif
