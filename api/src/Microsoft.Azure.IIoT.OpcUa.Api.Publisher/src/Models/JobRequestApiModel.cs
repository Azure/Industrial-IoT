// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Get job processing instructions from orchestrator
    /// </summary>
    [DataContract]
    public class JobRequestApiModel {

        /// <summary>
        /// Capabilities to match
        /// </summary>
        [DataMember(Name = "capabilities", Order = 0,
            EmitDefaultValue = false)]
        public Dictionary<string, string> Capabilities { get; set; }
    }
}