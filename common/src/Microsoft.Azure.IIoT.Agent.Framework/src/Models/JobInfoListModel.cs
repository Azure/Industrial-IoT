// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Job info list model
    /// </summary>
    public class JobInfoListModel {

        /// <summary>
        /// Jobs
        /// </summary>
        public List<JobInfoModel> Jobs { get; set; }

        /// <summary>
        /// Continuation
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}