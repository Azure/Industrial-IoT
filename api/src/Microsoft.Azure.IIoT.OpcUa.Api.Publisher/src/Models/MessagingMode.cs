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
        /// Network and dataset messages (default)
        /// </summary>
        [EnumMember]
        PubSub,

        /// <summary>
        /// Monitored item samples
        /// </summary>
        [EnumMember]
        Samples
    }
}