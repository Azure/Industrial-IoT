// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published data set describing metadata and dataset source
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetModel
    {
        /// <summary>
        /// Name of the published dataset
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public string? Name { get; set; }

        /// <summary>
        /// Data set source
        /// </summary>
        [DataMember(Name = "dataSetSource", Order = 1)]
        public PublishedDataSetSourceModel? DataSetSource { get; set; }

        /// <summary>
        /// Provides context of the dataset meta data that is to
        /// be emitted. If set to null no dataset metadata is emitted.
        /// </summary>
        [DataMember(Name = "dataSetMetaData", Order = 2,
            EmitDefaultValue = false)]
        public DataSetMetaDataModel? DataSetMetaData { get; set; }

        /// <summary>
        /// Extension fields
        /// </summary>
        [DataMember(Name = "extensionFields", Order = 3,
            EmitDefaultValue = false)]
        public IDictionary<string, VariantValue>? ExtensionFields { get; set; }

        /// <summary>
        /// Send keep alive messages for the data set
        /// </summary>
        [DataMember(Name = "sendKeepAlive", Order = 4,
            EmitDefaultValue = false)]
        public bool? SendKeepAlive { get; set; }
    }
}
