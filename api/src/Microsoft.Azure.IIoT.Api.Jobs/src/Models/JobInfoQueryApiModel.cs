// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Job info query model
    /// </summary>
    [DataContract]
    public class JobInfoQueryApiModel {

        /// <summary>
        /// Name
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        [DataMember(Name = "jobConfigurationType", Order = 1,
           EmitDefaultValue = false)]
        public string JobConfigurationType { get; set; }

        /// <summary>
        /// Job status
        /// </summary>
        [DataMember(Name = "status", Order = 2,
            EmitDefaultValue = false)]
        public JobStatus? Status { get; set; }
    }
}