// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub {
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa;
    using Opc.Ua;

    /// <summary>
    /// Data set metadata announcement
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class UadpMetaDataMessage : UadpDiscoveryMessage {

        /// <summary>
        /// Create metadata message
        /// </summary>
        public UadpMetaDataMessage() {
            IsProbe = false;
            DiscoveryType = (byte)UADPDiscoveryAnnouncementType.DataSetMetaData;
        }
    }
}