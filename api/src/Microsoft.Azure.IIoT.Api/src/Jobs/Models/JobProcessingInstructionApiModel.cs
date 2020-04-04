// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Processing instructions
    /// </summary>
    [DataContract]
    public class JobProcessingInstructionApiModel {

        /// <summary>
        /// Processing mode
        /// </summary>
        [DataMember(Name = "processMode", Order = 0,
            EmitDefaultValue = false)]
        public ProcessMode? ProcessMode { get; set; }

        /// <summary>
        /// Job to process
        /// </summary>
        [DataMember(Name = "job", Order = 1,
            EmitDefaultValue = false)]
        public JobInfoApiModel Job { get; set; }
    }
}