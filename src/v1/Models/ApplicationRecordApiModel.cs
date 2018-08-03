// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models
{
    public sealed class ApplicationRecordApiModel
    {
        [JsonProperty(PropertyName = "ApplicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "ID", Order = 15)]
        public int? ID { get; }

        [JsonProperty(PropertyName = "ApplicationUri", Order = 20)]
        public string ApplicationUri { get; set; }

        [JsonProperty(PropertyName = "ApplicationName", Order = 30)]
        public string ApplicationName { get; set; }

        [JsonProperty(PropertyName = "ApplicationType", Order = 40)]
        public int ApplicationType { get; set; }

        [JsonProperty(PropertyName = "ApplicationNames", Order = 50)]
        public ApplicationNameApiModel[] ApplicationNames { get; set; }

        [JsonProperty(PropertyName = "ProductUri", Order = 60)]
        public string ProductUri { get; set; }

        [JsonProperty(PropertyName = "DiscoveryUrls", Order = 70)]
        public string[] DiscoveryUrls { get; set; }

        [JsonProperty(PropertyName = "ServerCapabilities", Order = 80)]
        public string ServerCapabilities { get; set; }

        [JsonProperty(PropertyName = "GatewayServerUri", Order = 90)]
        public string GatewayServerUri { get; set; }

        [JsonProperty(PropertyName = "DiscoveryProfileUri", Order = 100)]
        public string DiscoveryProfileUri { get; set; }


        public ApplicationRecordApiModel()
        {
        }

        public ApplicationRecordApiModel(Application application)
        {
            this.ApplicationId = application.ApplicationId != Guid.Empty ? application.ApplicationId.ToString() : null;
            this.ID = application.ID;
            this.ApplicationUri = application.ApplicationUri;
            this.ApplicationName = application.ApplicationName;
            this.ApplicationType = application.ApplicationType;
            var applicationNames = new List<ApplicationNameApiModel>();
            foreach (var applicationName in application.ApplicationNames)
            {
                var applicationNameModel = new ApplicationNameApiModel(applicationName);
                applicationNames.Add(applicationNameModel);
            }
            this.ApplicationNames = applicationNames.ToArray();
            this.ProductUri = application.ProductUri;
            this.DiscoveryUrls = application.DiscoveryUrls;
            this.ServerCapabilities = application.ServerCapabilities;
            this.GatewayServerUri = application.GatewayServerUri;
            this.DiscoveryProfileUri = application.DiscoveryProfileUri;
        }

        public Application ToServiceModel()
        {
            var application = new Application();
            application.ApplicationId = this.ApplicationId != null ? new Guid(this.ApplicationId) : Guid.Empty;
            application.ApplicationUri = this.ApplicationUri;
            application.ApplicationName = this.ApplicationName;
            application.ApplicationType = this.ApplicationType;
            if (this.ApplicationNames != null)
            {
                var applicationNames = new List<ApplicationName>();
                foreach (var applicationNameModel in this.ApplicationNames)
                {
                    applicationNames.Add(applicationNameModel.ToServiceModel());
                }
                application.ApplicationNames = applicationNames.ToArray();
            }
            application.ProductUri = this.ProductUri;
            application.DiscoveryUrls = this.DiscoveryUrls;
            application.ServerCapabilities = this.ServerCapabilities;
            application.GatewayServerUri = this.GatewayServerUri;
            application.DiscoveryProfileUri = this.DiscoveryProfileUri;
            return application;
        }

    }
}
