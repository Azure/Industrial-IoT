// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Message modes
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MessagingMode {

        /// <summary>
        /// Network and dataset messages (default)
        /// </summary>
        PubSub,

        /// <summary>
        /// Monitored item samples
        /// </summary>
        Samples,

        /// <summary>
        /// PubSub message in binary encoding
        /// </summary>
        PubSubBinary,

        /// <summary>
        /// Monitored item samples in binary encoding 
        /// </summary>
        SamplesBinary 
    }

}