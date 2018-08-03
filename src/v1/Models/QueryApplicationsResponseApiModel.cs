// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models
{
    public sealed class QueryApplicationsResponseApiModel
    {
        [JsonProperty(PropertyName = "Applications", Order = 10)]
        public ApplicationRecordApiModel[] Applications { get; set; }

        [JsonProperty(PropertyName = "LastCounterResetTime", Required = Required.Always, Order = 20)]
        public DateTime LastCounterResetTime { get; set; }

        [JsonProperty(PropertyName = "NextRecordId", Required = Required.Always, Order = 30)]
        public int NextRecordId { get; set; }

        public QueryApplicationsResponseApiModel(QueryApplicationsResponseModel model)
        {
            var applicationsList = new List<ApplicationRecordApiModel>();
            foreach (Application application in model.Applications)
            {
                applicationsList.Add(new ApplicationRecordApiModel(application));
            }
            Applications = applicationsList.ToArray();
            LastCounterResetTime = model.LastCounterResetTime;
            NextRecordId = model.NextRecordId;
        }

    }


}
