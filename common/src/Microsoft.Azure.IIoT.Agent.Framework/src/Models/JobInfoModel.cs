// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;

    /// <summary>
    /// Job info model
    /// </summary>
    public class JobInfoModel {

        /// <summary>
        /// Job id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job configuration
        /// </summary>
        public VariantValue JobConfiguration { get; set; }

        /// <summary>
        /// Demands
        /// </summary>
        public List<DemandModel> Demands { get; set; }

        /// <summary>
        /// Redundancy configuration
        /// </summary>
        public RedundancyConfigModel RedundancyConfig { get; set; }

        /// <summary>
        /// Job lifetime
        /// </summary>
        public JobLifetimeDataModel LifetimeData { get; set; }
    }
}