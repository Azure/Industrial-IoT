// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// List of registered applications
    /// </summary>
    [DataContract]
    public class ApplicationInfoListApiModel {

        /// <summary>
        /// Application infos
        /// </summary>
        [DataMember(Name = "items")]
        public List<ApplicationInfoApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [DataMember(Name = "continuationToken",
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }
    }
}
