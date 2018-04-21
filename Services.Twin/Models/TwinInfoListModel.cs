// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Twin info list
    /// </summary>
    public class TwinInfoListModel {

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Twin registrations
        /// </summary>
        public List<TwinInfoModel> Items { get; set; }
    }
}
