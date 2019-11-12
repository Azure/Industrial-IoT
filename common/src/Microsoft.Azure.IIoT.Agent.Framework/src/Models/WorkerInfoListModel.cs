// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Worker list model
    /// </summary>
    public class WorkerInfoListModel {

        /// <summary>
        /// Continuation token
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Workers
        /// </summary>
        public List<WorkerInfoModel> Workers { get; set; }
    }
}