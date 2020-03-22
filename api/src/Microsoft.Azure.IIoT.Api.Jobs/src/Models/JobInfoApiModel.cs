// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Job info model
    /// </summary>
    [DataContract]
    public class JobInfoApiModel {

        /// <summary>
        /// Job id
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [DataMember(Name = "name",
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        [DataMember(Name = "jobConfigurationType",
            EmitDefaultValue = false)]
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job configuration
        /// </summary>
        [DataMember(Name = "jobConfiguration",
            EmitDefaultValue = false)]
        public VariantValue JobConfiguration { get; set; }

        /// <summary>
        /// Demands
        /// </summary>
        [DataMember(Name = "demands",
            EmitDefaultValue = false)]
        public List<DemandApiModel> Demands { get; set; }

        /// <summary>
        /// Redundancy configuration
        /// </summary>
        [DataMember(Name = "redundancyConfig",
            EmitDefaultValue = false)]
        public RedundancyConfigApiModel RedundancyConfig { get; set; }

        /// <summary>
        /// Job lifetime
        /// </summary>
        [DataMember(Name = "lifetimeData",
            EmitDefaultValue = false)]
        public JobLifetimeDataApiModel LifetimeData { get; set; }
    }
}