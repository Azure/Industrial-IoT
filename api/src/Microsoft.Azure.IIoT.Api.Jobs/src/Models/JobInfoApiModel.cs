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
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [DataMember(Name = "name", Order = 1,
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        [DataMember(Name = "jobConfigurationType", Order = 2,
            EmitDefaultValue = false)]
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job configuration
        /// </summary>
        [DataMember(Name = "jobConfiguration", Order = 3,
            EmitDefaultValue = false)]
        public VariantValue JobConfiguration { get; set; }

        /// <summary>
        /// Demands
        /// </summary>
        [DataMember(Name = "demands", Order = 4,
            EmitDefaultValue = false)]
        public List<DemandApiModel> Demands { get; set; }

        /// <summary>
        /// Redundancy configuration
        /// </summary>
        [DataMember(Name = "redundancyConfig", Order = 5,
            EmitDefaultValue = false)]
        public RedundancyConfigApiModel RedundancyConfig { get; set; }

        /// <summary>
        /// Job lifetime
        /// </summary>
        [DataMember(Name = "lifetimeData", Order = 6,
            EmitDefaultValue = false)]
        public JobLifetimeDataApiModel LifetimeData { get; set; }
    }
}