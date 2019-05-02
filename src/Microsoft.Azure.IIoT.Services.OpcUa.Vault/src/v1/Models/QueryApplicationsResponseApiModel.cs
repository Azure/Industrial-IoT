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
    public sealed class QueryApplicationsResponseApiModel
    {
        [JsonProperty(PropertyName = "applications", Order = 10)]
        public IList<ApplicationRecordApiModel> Applications { get; set; }

        [JsonProperty(PropertyName = "nextPageLink", Order = 20)]
        public string NextPageLink { get; set; }

        public QueryApplicationsResponseApiModel(QueryApplicationsResponseModel model)
        {
            var applicationsList = new List<ApplicationRecordApiModel>();
            foreach (Application application in model.Applications)
            {
                applicationsList.Add(new ApplicationRecordApiModel(application));
            }
            Applications = applicationsList;
            NextPageLink = model.NextPageLink;
        }

        public QueryApplicationsResponseApiModel(IList<Application> applications, string nextPageLink = null)
        {
            var applicationsList = new List<ApplicationRecordApiModel>();
            foreach (Application application in applications)
            {
                applicationsList.Add(new ApplicationRecordApiModel(application));
            }
            Applications = applicationsList;
            NextPageLink = nextPageLink;
        }
        public QueryApplicationsResponseApiModel(IList<ApplicationRecordApiModel> applications, string nextPageLink = null)
        {
            Applications = applications;
            NextPageLink = nextPageLink;
        }

    }


}
