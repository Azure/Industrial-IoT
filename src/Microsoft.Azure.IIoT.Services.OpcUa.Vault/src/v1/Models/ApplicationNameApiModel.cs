// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class ApplicationNameApiModel
    {
        [JsonProperty(PropertyName = "locale", NullValueHandling = NullValueHandling.Ignore)]
        public string Locale { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        public ApplicationNameApiModel()
        {
        }

        public ApplicationNameApiModel(ApplicationName applicationName)
        {
            this.Locale = applicationName.Locale;
            this.Text = applicationName.Text;
        }

        public ApplicationNameApiModel(string applicationName)
        {
            this.Locale = null;
            this.Text = applicationName;
        }

        public ApplicationName ToServiceModel()
        {
            var applicationName = new ApplicationName();
            applicationName.Locale = this.Locale;
            applicationName.Text = this.Text;
            return applicationName;
        }

    }
}
