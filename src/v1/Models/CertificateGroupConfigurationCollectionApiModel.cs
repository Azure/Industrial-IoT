// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models
{
    public sealed class CertificateGroupConfigurationCollectionApiModel
    {
        [JsonProperty(PropertyName = "Groups", Order = 10)]
        public CertificateGroupConfigurationApiModel [] Groups { get; set; }


        public CertificateGroupConfigurationCollectionApiModel(Opc.Ua.Gds.Server.CertificateGroupConfigurationCollection config)
        {
            var newGroups = new List<CertificateGroupConfigurationApiModel>();
            foreach (var group in config)
            {
                var newGroup = new CertificateGroupConfigurationApiModel( group.Id, group);
                newGroups.Add(newGroup);
            }
            this.Groups = newGroups.ToArray();
        }
    }
}
