// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class QueryApplicationsByIdResponseApiModel
    {
        [JsonProperty(PropertyName = "applications", Order = 10)]
        public IList<ApplicationRecordApiModel> Applications { get; set; }

        [JsonProperty(PropertyName = "lastCounterResetTime", Order = 20)]
        [Required]
        public DateTime LastCounterResetTime { get; set; }

        [JsonProperty(PropertyName = "nextRecordId", Order = 30)]
        [Required]
        public int NextRecordId { get; set; }

        public QueryApplicationsByIdResponseApiModel(QueryApplicationsByIdResponseModel model)
        {
            var applicationsList = new List<ApplicationRecordApiModel>();
            foreach (Application application in model.Applications)
            {
                applicationsList.Add(new ApplicationRecordApiModel(application));
            }
            Applications = applicationsList;
            LastCounterResetTime = model.LastCounterResetTime;
            NextRecordId = model.NextRecordId;
        }

    }


}
