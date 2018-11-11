// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Collections.Generic;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class QueryApplicationsPageResponseApiModel
    {
        [JsonProperty(PropertyName = "Applications", Order = 10)]
        public ApplicationRecordApiModel[] Applications { get; set; }

        [JsonProperty(PropertyName = "NextPageLink", Order = 20)]
        public string NextPageLink { get; set; }

        public QueryApplicationsPageResponseApiModel(QueryApplicationsPageResponseModel model)
        {
            var applicationsList = new List<ApplicationRecordApiModel>();
            foreach (Application application in model.Applications)
            {
                applicationsList.Add(new ApplicationRecordApiModel(application));
            }
            Applications = applicationsList.ToArray();
            NextPageLink = model.NextPageLink;
        }

    }


}
