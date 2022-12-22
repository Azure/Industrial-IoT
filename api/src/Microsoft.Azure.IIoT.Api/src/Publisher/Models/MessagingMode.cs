// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Message modes
    /// </summary>
    [DataContract]
    public enum MessagingMode {

        /// <summary>
        /// Standard pub sub mode cloud message mode (default)
        /// </summary>
        [EnumMember]
        PubSub,

        /// <summary>
        /// Same as PubSub
        /// </summary>
        [EnumMember]
        NetworkMessages = PubSub,

        /// <summary>
        /// Monitored item sample mode
        /// </summary>
        [EnumMember]
        Samples,

        /// <summary>
        /// Network and dataset messages fully featured
        /// </summary>
        [EnumMember]
        FullNetworkMessages,

        /// <summary>
        /// Monitored item sample full mode
        /// </summary>
        [EnumMember]
        FullSamples,

        /// <summary>
        /// Dataset messages but no network message header
        /// </summary>
        [EnumMember]
        DataSetMessages,
    }
}