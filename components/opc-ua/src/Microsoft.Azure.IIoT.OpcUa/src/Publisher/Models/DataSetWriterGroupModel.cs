// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Data set writer group
    /// </summary>
    public class DataSetWriterGroupModel {

        /// <summary>
        /// Dataset writer configuration - TODO List
        /// </summary>
        public DataSetWriterModel DataSetWriter { get; set; }

        /// <summary>
        /// Connection information
        /// </summary>
        public ConnectionModel Connection { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Whether to send change messages
        /// </summary>
        public bool? SendChangeMessages { get; set; }

        /// <summary>
        /// Publisher engine configuration
        /// </summary>
        public EngineConfigurationModel Engine { get; set; }
    }
}