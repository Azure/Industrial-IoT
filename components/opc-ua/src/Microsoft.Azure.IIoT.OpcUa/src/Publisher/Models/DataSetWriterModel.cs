// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;

    /// <summary>
    /// Data set writer
    /// </summary>
    public class DataSetWriterModel {
        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string DataSetWriterName { get; set; }

        /// <summary>
        /// Published dataset inline definition
        /// </summary>
        public PublishedDataSetModel DataSet { get; set; }

        /// <summary>
        /// Dataset field content mask
        /// </summary>
        public DataSetFieldContentMask? DataSetFieldContentMask { get; set; }

        /// <summary>
        /// Data set message settings
        /// </summary>
        public DataSetWriterMessageSettingsModel MessageSettings { get; set; }

        /// <summary>
        /// Keyframe count
        /// </summary>
        public uint? KeyFrameCount { get; set; }

        /// <summary>
        /// Metadata message sending interval (publisher extension)
        /// </summary>
        public TimeSpan? DataSetMetaDataSendInterval { get; set; }

        /// <summary>
        /// Dataset writer id property for backwards compatibility.
        /// TODO: Remove when removing orchestrated mode
        /// </summary>
        public string DataSetWriterId { set => DataSetWriterName = value; }
    }
}
