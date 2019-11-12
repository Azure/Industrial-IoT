// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Dataset writer model
    /// </summary>
    public class DataSetWriterModel {

        /// <summary>
        /// Dataset writer id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Datasets to publish
        /// </summary>
        public List<DataSetModel> DataSets { get; set; }

        /// <summary>
        /// Keyframe interval
        /// </summary>
        public TimeSpan? KeyframeMessageInterval { get; set; }

        /// <summary>
        /// Metadata message interval
        /// </summary>
        public TimeSpan? MetadataMessageInterval { get; set; }

        /// <summary>
        /// Content encoding
        /// </summary>
        public NetworkMessageEncoding? ContentEncoding { get; set; }

        /// <summary>
        /// Network message content mask
        /// </summary>
        public NetworkMessageContentMask? NetworkMessageContent { get; set; }

        /// <summary>
        /// Dataset message content mask
        /// </summary>
        public DataSetContentMask? DataSetContent { get; set; }

        /// <summary>
        /// Dataset field content mask
        /// </summary>
        public DataSetFieldContentMask? FieldContent { get; set; }
    }
}