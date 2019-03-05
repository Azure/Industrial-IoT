// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Insert historic data
    /// </summary>
    public class InsertValuesDetailsModel {

        /// <summary>
        /// Values to insert
        /// </summary>
        public List<HistoricValueModel> Values { get; set; }
    }
}
