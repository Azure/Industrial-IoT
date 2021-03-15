// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Published event items
    /// </summary>
    public class PublishedEventItemsModel {

        /// <summary>
        /// Event variables
        /// </summary>
        public List<PublishedDataSetEventModel> PublishedData { get; set; }
    }
}