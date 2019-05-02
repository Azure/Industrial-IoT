// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateGroupConfigurationCollectionApiModel
    {
        [JsonProperty(PropertyName = "groups", Order = 10)]
        public IList<CertificateGroupConfigurationApiModel> Groups { get; set; }

        public CertificateGroupConfigurationCollectionApiModel(IList<CertificateGroupConfigurationModel> config)
        {
            var newGroups = new List<CertificateGroupConfigurationApiModel>();
            foreach (var group in config)
            {
                var newGroup = new CertificateGroupConfigurationApiModel( group.Id, group);
                newGroups.Add(newGroup);
            }
            this.Groups = newGroups;
        }
    }
}
