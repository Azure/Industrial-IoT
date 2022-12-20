// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Metadata for the published dataset
    /// </summary>
    public class DataSetMetaDataModel {

        /// <summary>
        /// Name of the dataset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the dataset
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Dataset class id
        /// </summary>
        public Guid DataSetClassId { get; set; }
    }
}
