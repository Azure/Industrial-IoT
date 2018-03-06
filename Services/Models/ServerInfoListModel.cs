// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// List of registered servers
    /// </summary>
    public class ServerInfoListModel {

        /// <summary>
        /// Continuation token
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Items
        /// </summary>
        public List<ServerInfoModel> Items { get; set; }
    }
}
