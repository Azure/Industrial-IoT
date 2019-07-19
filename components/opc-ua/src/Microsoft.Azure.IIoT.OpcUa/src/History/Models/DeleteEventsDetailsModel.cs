// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System.Collections.Generic;

    /// <summary>
    /// The events to delete
    /// </summary>
    public class DeleteEventsDetailsModel {

        /// <summary>
        /// Events to delete
        /// </summary>
        public List<byte[]> EventIds { get; set; }
    }
}
