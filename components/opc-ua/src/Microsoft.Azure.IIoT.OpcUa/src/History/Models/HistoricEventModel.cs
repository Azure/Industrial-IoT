// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;

    /// <summary>
    /// Historic event
    /// </summary>
    public class HistoricEventModel {

        /// <summary>
        /// The selected fields of the event
        /// </summary>
        public List<VariantValue> EventFields { get; set; } // TODO: Update to concrete type
    }
}
