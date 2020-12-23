// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Authentication information
    /// </summary>
    [DataContract]
    public class DeviceAuthenticationModel {

        /// <summary>
        /// Primary sas key
        /// </summary>
        [DataMember(Name = "primaryKey",
            EmitDefaultValue = false)]
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Secondary sas key
        /// </summary>
        [DataMember(Name = "secondaryKey",
            EmitDefaultValue = false)]
        public string SecondaryKey { get; set; }
    }
}
