// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{

    [Serializable]
    public enum ApplicationType : int
    {
        [EnumMember(Value = "server")]
        Server = 0,
        [EnumMember(Value = "client")]
        Client = 1,
        [EnumMember(Value = "clientAndServer")]
        ClientAndServer = 2,
        [EnumMember(Value = "discoveryServer")]
        DiscoveryServer = 3
    }

    public sealed class ApplicationRecordApiModel
    {
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int? ID { get; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "applicationUri")]
        public string ApplicationUri { get; set; }

        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }

        [JsonProperty(PropertyName = "applicationType")]
        public ApplicationType ApplicationType { get; set; }

        [JsonProperty(PropertyName = "applicationNames")]
        public IList<ApplicationNameApiModel> ApplicationNames { get; set; }

        [JsonProperty(PropertyName = "productUri")]
        public string ProductUri { get; set; }

        [JsonProperty(PropertyName = "discoveryUrls")]
        public IList<string> DiscoveryUrls { get; set; }

        [JsonProperty(PropertyName = "serverCapabilities")]
        public string ServerCapabilities { get; set; }

        [JsonProperty(PropertyName = "gatewayServerUri")]
        public string GatewayServerUri { get; set; }

        [JsonProperty(PropertyName = "discoveryProfileUri")]
        public string DiscoveryProfileUri { get; set; }


        public ApplicationRecordApiModel()
        {
        }

        public ApplicationRecordApiModel(Application application)
        {
            this.ApplicationId = application.ApplicationId != Guid.Empty ? application.ApplicationId.ToString() : null;
            this.ID = application.ID;
            this.State = application.ApplicationState.ToString();
            this.ApplicationUri = application.ApplicationUri;
            this.ApplicationName = application.ApplicationName;
            this.ApplicationType = (ApplicationType)application.ApplicationType;
            var applicationNames = new List<ApplicationNameApiModel>();
            foreach (var applicationName in application.ApplicationNames)
            {
                var applicationNameModel = new ApplicationNameApiModel(applicationName);
                applicationNames.Add(applicationNameModel);
            }
            this.ApplicationNames = applicationNames;
            this.ProductUri = application.ProductUri;
            this.DiscoveryUrls = application.DiscoveryUrls;
            this.ServerCapabilities = application.ServerCapabilities;
            this.GatewayServerUri = application.GatewayServerUri;
            this.DiscoveryProfileUri = application.DiscoveryProfileUri;
        }

        public Application ToServiceModel()
        {
            var application = new Application();
            // ID and State are ignored, readonly
            application.ApplicationId = this.ApplicationId != null ? new Guid(this.ApplicationId) : Guid.Empty;
            application.ApplicationUri = this.ApplicationUri;
            application.ApplicationName = this.ApplicationName;
            application.ApplicationType = (CosmosDB.Models.ApplicationType)this.ApplicationType;
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
            application.DiscoveryUrls = this.DiscoveryUrls.ToArray();
            application.ServerCapabilities = this.ServerCapabilities;
            application.GatewayServerUri = this.GatewayServerUri;
            application.DiscoveryProfileUri = this.DiscoveryProfileUri;
            return application;
        }

    }
}
