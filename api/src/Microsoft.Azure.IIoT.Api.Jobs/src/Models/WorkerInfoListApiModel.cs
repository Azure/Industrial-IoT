// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Worker info list model
    /// </summary>
    [DataContract]
    public class WorkerInfoListApiModel {

        /// <summary>
        /// Continuation token
        /// </summary>
        [DataMember(Name = "continuationToken",
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Workers
        /// </summary>
        [DataMember(Name = "workers")]
        public List<WorkerInfoApiModel> Workers { get; set; }
    }
}