// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{

    public sealed class ApplicationRecordApiModel
    {
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int ID { get; }

        [JsonProperty(PropertyName = "state")]
        [Required]
        public ApplicationState State { get; }

        [JsonProperty(PropertyName = "applicationUri")]
        public string ApplicationUri { get; set; }

        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }

        [JsonProperty(PropertyName = "applicationType")]
        [Required]
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
            this.ID = 0;
            this.State = ApplicationState.New;
        }

        public ApplicationRecordApiModel(ApplicationRecordApiModel model)
        {
            this.ApplicationId = model.ApplicationId;
            this.ID = model.ID;
            this.State = model.State;
            this.ApplicationUri = model.ApplicationUri;
            this.ApplicationName = model.ApplicationName;
            this.ApplicationType = model.ApplicationType;
            this.ApplicationNames = model.ApplicationNames;
            this.ProductUri = model.ProductUri;
            this.DiscoveryUrls = model.DiscoveryUrls;
            this.ServerCapabilities = model.ServerCapabilities;
            this.GatewayServerUri = model.GatewayServerUri;
            this.DiscoveryProfileUri = model.DiscoveryProfileUri;
        }

        public ApplicationRecordApiModel(CosmosDB.Models.Application application)
        {
            this.ApplicationId = application.ApplicationId != Guid.Empty ? application.ApplicationId.ToString() : null;
            this.ID = application.ID;
            this.State = (ApplicationState)application.ApplicationState;
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

        public CosmosDB.Models.Application ToServiceModel()
        {
            var application = new CosmosDB.Models.Application();
            // ID and State are ignored, readonly
            application.ApplicationId = this.ApplicationId != null ? new Guid(this.ApplicationId) : Guid.Empty;
            application.ApplicationUri = this.ApplicationUri;
            application.ApplicationName = this.ApplicationName;
            application.ApplicationType = (Types.ApplicationType)this.ApplicationType;
            if (this.ApplicationNames != null)
            {
                var applicationNames = new List<CosmosDB.Models.ApplicationName>();
                foreach (var applicationNameModel in this.ApplicationNames)
                {
                    applicationNames.Add(applicationNameModel.ToServiceModel());
                }
                application.ApplicationNames = applicationNames.ToArray();
            }
            application.ProductUri = this.ProductUri;
            application.DiscoveryUrls = this.DiscoveryUrls != null ? this.DiscoveryUrls.ToArray() : null;
            application.ServerCapabilities = this.ServerCapabilities;
            application.GatewayServerUri = this.GatewayServerUri;
            application.DiscoveryProfileUri = this.DiscoveryProfileUri;
            return application;
        }

    }
}
