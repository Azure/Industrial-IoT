// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Capabilities
    /// </summary>
    [DataContract]
    public class DeviceCapabilitiesModel {

        /// <summary>
        /// iotedge device
        /// </summary>
        [DataMember(Name = "iotEdge",
            EmitDefaultValue = false)]
        public bool? IotEdge { get; set; }
    }
}
