// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class QueryApplicationsApiModel
    {
        [JsonProperty(PropertyName = "applicationName", Order = 20)]
        public string ApplicationName { get; set; }

        [JsonProperty(PropertyName = "applicationUri", Order = 30)]
        public string ApplicationUri { get; set; }

        [JsonProperty(PropertyName = "applicationType", Order = 40)]
        public QueryApplicationType ApplicationType { get; set; }

        [JsonProperty(PropertyName = "productUri", Order = 50)]
        public string ProductUri { get; set; }

        [JsonProperty(PropertyName = "serverCapabilities", Order = 60)]
        public IList<string> ServerCapabilities { get; set; }

        [JsonProperty(PropertyName = "applicationState", Order = 80)]
        public QueryApplicationState? ApplicationState { get; set; }


        public QueryApplicationsApiModel(
            string applicationName,
            string applicationUri,
            QueryApplicationType applicationType,
            string productUri,
            IList<string> serverCapabilities,
            QueryApplicationState? applicationState
            )
        {
            this.ApplicationName = applicationName;
            this.ApplicationUri = applicationUri;
            this.ApplicationType = applicationType;
            this.ProductUri = productUri;
            this.ServerCapabilities = serverCapabilities;
            this.ApplicationState = applicationState;
        }

    }
}
