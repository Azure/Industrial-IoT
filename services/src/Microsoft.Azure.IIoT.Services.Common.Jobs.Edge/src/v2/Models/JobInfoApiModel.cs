// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Job info model
    /// </summary>
    public class JobInfoApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public JobInfoApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public JobInfoApiModel(JobInfoModel model) {
            LifetimeData = model.LifetimeData == null ? null :
                new JobLifetimeDataApiModel(model.LifetimeData);
            RedundancyConfig = model.RedundancyConfig == null ? null :
                new RedundancyConfigApiModel(model.RedundancyConfig);
            Demands = model.Demands?
                .Select(d => new DemandApiModel(d)).ToList();
            JobConfiguration = model.JobConfiguration?.DeepClone();
            JobConfigurationType = model.JobConfigurationType;
            Name = model.Name;
            Id = model.Id;
        }

        /// <summary>
        /// Job id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        [JsonProperty(PropertyName = "jobConfigurationType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job configuration
        /// </summary>
        [JsonProperty(PropertyName = "jobConfiguration",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken JobConfiguration { get; set; }

        /// <summary>
        /// Demands
        /// </summary>
        [JsonProperty(PropertyName = "demands",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<DemandApiModel> Demands { get; set; }

        /// <summary>
        /// Redundancy configuration
        /// </summary>
        [JsonProperty(PropertyName = "redundancyConfig",
            NullValueHandling = NullValueHandling.Ignore)]
        public RedundancyConfigApiModel RedundancyConfig { get; set; }

        /// <summary>
        /// Job lifetime
        /// </summary>
        [JsonProperty(PropertyName = "lifetimeData",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobLifetimeDataApiModel LifetimeData { get; set; }
    }
}