// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Message modes
    /// </summary>
    public enum MessagingMode {

        /// <summary>
        /// Standard pub sub mode cloud message mode (default)
        /// </summary>
        PubSub,

        /// <summary>
        /// Monitored item sample mode
        /// </summary>
        Samples,

        /// <summary>
        /// Network and dataset messages fully featured
        /// </summary>
        FullNetworkMessages,

        /// <summary>
        /// Monitored item sample full mode
        /// </summary>
        FullSamples,

        /// <summary>
        /// Messages without network message header.
        /// </summary>
        DataSetMessages,

        /// <summary>
        /// Datasets are pure key value pairs without headers
        /// </summary>
        RawDataSets
    }
}