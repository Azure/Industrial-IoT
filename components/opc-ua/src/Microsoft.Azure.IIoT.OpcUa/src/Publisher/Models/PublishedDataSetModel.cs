﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Published data set describing metadata and dataset source
    /// </summary>
    public class PublishedDataSetModel {

        /// <summary>
        /// Name of the published dataset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data set source
        /// </summary>
        public PublishedDataSetSourceModel DataSetSource { get; set; }

        /// <summary>
        /// Dataset meta data to emit
        /// </summary>
        public DataSetMetaDataModel DataSetMetaData { get; set; }

        /// <summary>
        /// Extension fields
        /// </summary>
        public Dictionary<string, string> ExtensionFields { get; set; }
    }
}