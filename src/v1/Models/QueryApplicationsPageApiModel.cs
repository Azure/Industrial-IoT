// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class QueryApplicationsPageApiModel
    {
        [JsonProperty(PropertyName = "ApplicationName", Order = 20)]
        public string ApplicationName { get; set; }

        [JsonProperty(PropertyName = "ApplicationUri", Order = 30)]
        public string ApplicationUri { get; set; }

        [JsonProperty(PropertyName = "ApplicationType", Order = 40)]
        public uint ApplicationType { get; set; }

        [JsonProperty(PropertyName = "ProductUri", Order = 50)]
        public string ProductUri { get; set; }

        [JsonProperty(PropertyName = "ServerCapabilities", Order = 60)]
        public string [] ServerCapabilities { get; set; }

        [JsonProperty(PropertyName = "NextPageLink", Order = 70)]
        public string NextPageLink { get; set; }

        [JsonProperty(PropertyName = "MaxRecordsToReturn", Order = 80)]
        public int MaxRecordsToReturn { get; set; }

        public QueryApplicationsPageApiModel(
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            IList<string> serverCapabilities,
            string nextPageLink = null,
            int maxRecordsToReturn = -1
            )
        {
            this.ApplicationName = applicationName;
            this.ApplicationUri = applicationUri;
            this.ApplicationType = applicationType;
            this.ProductUri = productUri;
            this.ServerCapabilities = serverCapabilities?.ToArray();
            this.NextPageLink = nextPageLink;
            this.MaxRecordsToReturn = maxRecordsToReturn;
        }

    }
}
