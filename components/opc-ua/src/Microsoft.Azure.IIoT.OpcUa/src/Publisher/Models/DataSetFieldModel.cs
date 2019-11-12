// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Dataset Field model
    /// </summary>
    public class DataSetFieldModel {

        /// <summary>
        /// Node id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Optional sampling configuration
        /// </summary>
        public DataSetFieldSamplingModel Configuration { get; set; }
    }
}