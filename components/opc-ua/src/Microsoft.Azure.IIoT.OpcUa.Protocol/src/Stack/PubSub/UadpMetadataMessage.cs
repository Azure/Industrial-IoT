// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
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