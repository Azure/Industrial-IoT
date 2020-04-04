// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// List of application sites
    /// </summary>
    [DataContract]
    public class ApplicationSiteListApiModel {

        /// <summary>
        /// Sites
        /// </summary>
        [DataMember(Name = "sites", Order = 0)]
        public List<string> Sites { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 1,
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }
    }
}
