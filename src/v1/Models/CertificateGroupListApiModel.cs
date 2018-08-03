// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models
{
    public sealed class CertificateGroupListApiModel
    {
        [JsonProperty(PropertyName = "Groups", Order = 20)]
        public string [] Groups { get; set; }

        public CertificateGroupListApiModel(string[] groups)
        {
            this.Groups = groups;
        }
    }
}
