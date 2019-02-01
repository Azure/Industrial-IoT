// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models
{
    public enum ApplicationType : int
    {
        Server = 0,
        Client = 1,
        ClientAndServer = 2,
        DiscoveryServer = 3
    }

    public enum ApplicationState : int
    {
        New = 0,
        Approved = 1,
        Rejected = 2,
        Unregistered = 3,
        Deleted = 4
    }

    [Serializable]
    public class ApplicationName
    {
        public string Locale { get; set; }
        public string Text { get; set; }
    }

    [Serializable]
    public class Application
    {
        public static readonly string ClassTypeName = "Application";
        public Application()
        {
            this.ClassType = ClassTypeName;
        }

        [JsonProperty(PropertyName = "id")]
        public Guid ApplicationId { get; set; }
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }
        public string ClassType { get; set; }
        public int ID { get; set; }
        public ApplicationState ApplicationState { get; set; }
        public string ApplicationUri { get; set; }
        public string ApplicationName { get; set; }
        public ApplicationType ApplicationType { get; set; }
        public string ProductUri { get; set; }
        public string ServerCapabilities { get; set; }
        public ApplicationName[] ApplicationNames { get; set; }
        public string[] DiscoveryUrls { get; set; }
        public string GatewayServerUri { get; set; }
        public string DiscoveryProfileUri { get; set; }
        public string AuthorityId { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? ApproveTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public DateTime? DeleteTime { get; set; }
    }
}
