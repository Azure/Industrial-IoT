// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Dataset model
    /// </summary>
    [DataContract]
    public class PublishedDataSetApiModel {

        /// <summary>
        /// Name of dataset
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Data set source
        /// </summary>
        [DataMember(Name = "dataSetSource", Order = 1)]
        public PublishedDataSetSourceApiModel DataSetSource { get; set; }

        /// <summary>
        /// Dataset meta data to emit
        /// </summary>
        [DataMember(Name = "dataSetMetaData", Order = 2,
            EmitDefaultValue = false)]
        public DataSetMetaDataApiModel DataSetMetaData { get; set; }

        /// <summary>
        /// Extension fields
        /// </summary>
        [DataMember(Name = "extensionFields", Order = 3,
            EmitDefaultValue = false)]
        public Dictionary<string, string> ExtensionFields { get; set; }
    }
}