// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateGroupListApiModel
    {
        [JsonProperty(PropertyName = "groups", Order = 20)]
        public IList<string> Groups { get; set; }

        public CertificateGroupListApiModel(IList<string> groups)
        {
            this.Groups = groups;
        }
    }
}
