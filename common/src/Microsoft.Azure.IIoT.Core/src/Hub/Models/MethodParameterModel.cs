// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Twin services method params
    /// </summary>
    [DataContract]
    public class MethodParameterModel {

        /// <summary>
        /// Name of method
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Response timeout
        /// </summary>
        [DataMember(Name = "responseTimeout",
            EmitDefaultValue = false)]
        public TimeSpan? ResponseTimeout { get; set; }

        /// <summary>
        /// Connection timeout
        /// </summary>
        [DataMember(Name = "connectionTimeout",
            EmitDefaultValue = false)]
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// Json payload of the method request
        /// </summary>
        [DataMember(Name = "jsonPayload")]
        public string JsonPayload { get; set; }
    }
}
