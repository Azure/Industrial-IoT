// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// List of device twins with continuation token
    /// </summary>
    [DataContract]
    public class DeviceTwinListModel {

        /// <summary>
        /// Continuation token to use for next call or null
        /// </summary>
        [DataMember(Name = "continuationToken",
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Items returned
        /// </summary>
        [DataMember(Name = "items")]
        public List<DeviceTwinModel> Items { get; set; }
    }
}
