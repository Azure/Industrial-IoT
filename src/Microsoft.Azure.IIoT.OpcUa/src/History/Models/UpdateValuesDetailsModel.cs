// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Update historic data
    /// </summary>
    public class UpdateValuesDetailsModel {
        
        /// <summary>
        /// Whether to perform an insert or replacement
        /// </summary>
        public HistoryUpdateOperation PerformInsertReplace { get; set; }

        /// <summary>
        /// Values to insert or replace
        /// </summary>
        public List<HistoricValueModel> UpdateValues { get; set; }
    }
}
