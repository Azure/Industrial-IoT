// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Published data items
    /// </summary>
    public class PublishedDataItemsModel {

        /// <summary>
        /// Data variables
        /// </summary>
        public List<PublishedDataSetVariableModel> PublishedData { get; set; }
    }
}