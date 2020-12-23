// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Model of device registry document
    /// </summary>
    [DataContract]
    public class DeviceModel {

        /// <summary>
        /// Etag for comparison
        /// </summary>
        [DataMember(Name = "etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Device id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [DataMember(Name = "moduleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// Authentication information
        /// </summary>
        [DataMember(Name = "authentication")]
        public DeviceAuthenticationModel Authentication { get; set; }

        /// <summary>
        /// Corresponding Device's ConnectionState
        /// </summary>
        [DataMember(Name = "connectionState",
            EmitDefaultValue = false)]
        public string ConnectionState { get; set; }
    }
}
