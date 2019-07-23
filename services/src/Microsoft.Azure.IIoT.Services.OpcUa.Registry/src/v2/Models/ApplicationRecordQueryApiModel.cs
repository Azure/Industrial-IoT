// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Query by id
    /// </summary>
    public sealed class ApplicationRecordQueryApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRecordQueryApiModel() {
        }

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRecordQueryApiModel(ApplicationRecordQueryModel model) {
            ApplicationName = model.ApplicationName;
            ApplicationUri = model.ApplicationUri;
            ApplicationType = model.ApplicationType;
            ProductUri = model.ProductUri;
            ServerCapabilities = model.ServerCapabilities;
            MaxRecordsToReturn = model.MaxRecordsToReturn;
            StartingRecordId = model.StartingRecordId;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public ApplicationRecordQueryModel ToServiceModel() {
            return new ApplicationRecordQueryModel {
                ApplicationName = ApplicationName,
                ApplicationUri = ApplicationUri,
                ApplicationType = ApplicationType,
                ProductUri = ProductUri,
                ServerCapabilities = ServerCapabilities,
                MaxRecordsToReturn = MaxRecordsToReturn,
                StartingRecordId = StartingRecordId
            };
        }

        /// <summary>
        /// Starting record id
        /// </summary>
        [JsonProperty(PropertyName = "startingRecordId")]
        public uint? StartingRecordId { get; set; }

        /// <summary>
        /// Max records to return
        /// </summary>
        [JsonProperty(PropertyName = "maxRecordsToReturn")]
        public uint? MaxRecordsToReturn { get; set; }

        /// <summary>
        /// Application name
        /// </summary>
        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri")]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Application type
        /// </summary>
        [JsonProperty(PropertyName = "applicationType")]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri")]
        public string ProductUri { get; set; }

        /// <summary>
        /// Server capabilities
        /// </summary>
        [JsonProperty(PropertyName = "serverCapabilities")]
        public List<string> ServerCapabilities { get; set; }
    }
}
